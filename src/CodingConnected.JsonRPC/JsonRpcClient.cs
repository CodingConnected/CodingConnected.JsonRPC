using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// This class is meant to easily consume a remote JSON-RPC service, by allowing invokation 
    /// of remote procedures through simple method calls.
    /// Which the class, it is easy to write proxy classes that expose remote methods as normal
    /// class methods locally, for easy consumption in a project.
    /// 
    /// To use the class: 
    /// - create an instance
    /// - set the TcpClient property to a an instance of a class that implements ITcpClient
    ///   (or use the appropriate constructor to parse it)
    /// - make sure HandleDataReceived() will be called when relevant (ie. when data is received),
    ///   for example by using it to subscribe to an event
    /// - use InvokeAsync to call remote methods
    /// </summary>
    public class JsonRpcClient
    {
        #region Private classes

        /// <summary>
        /// Helper class to facilitate calling remote methods without a return value
        /// </summary>
        private class Nil
        {

        }

        #endregion // Private classes

        #region Fields
        
        private readonly Regex _jsonRpcResultRegex = new Regex(@"['""]result['""]", RegexOptions.Compiled);
        private readonly Regex _jsonRpcErrorRegex = new Regex(@"['""]error['""]", RegexOptions.Compiled);
        private readonly List<Tuple<ulong, JsonRpcResponse, AutoResetEvent>> _isWaitingForReply = new List<Tuple<ulong, JsonRpcResponse, AutoResetEvent>>();
        private ulong _nextId;

        #endregion // Fields

        #region Properties
        
        private ILogger Logger { get; }

        /// <summary>
        /// An instance of a class that implements ITcpClient, which is used to send data to the
        /// remote server.
        /// </summary>
        public ITcpClient TcpClient { get; set; }

        #endregion // Properties

        #region Public Methods

        /// <summary>
        /// This method is meant for calling non-returning procedures (ie.: "Notifications") on the remote server.
        /// </summary>
        /// <param name="methodname">The remote method name</param>
        /// <param name="param">The argument(s) for the method call</param>
        /// <param name="timeout">Timeout after which waiting for a response will be cancelled</param>
        /// <param name="token">Cancellation token to cancel any async operations</param>
        /// <returns>A Task representing the promise to remotely invoke the method</returns>
        public async Task InvokeAsync(string methodname, object param, int timeout, CancellationToken token)
        {
            if (TcpClient == null)
            {
                throw new NullReferenceException("TcpClient must be set before calling InvokeAsync");
            }
            await InvokeAsync<Nil>(methodname, param, timeout, token);
        }

        /// <summary>
        /// This method is meant for calling procedures on the remote server that will return a result of
        /// type T.
        /// </summary>
        /// <typeparam name="T">The type which the returned result is expected to have</typeparam>
        /// <param name="methodname">The remote method name</param>
        /// <param name="param">The argument(s) for the method call</param>
        /// <param name="timeout">Timeout after which waiting for a response will be cancelled</param>
        /// <param name="token">Cancellation token to cancel any async operations</param>
        /// <returns>A Task representing the promise to return an instance of type T</returns>
        public async Task<T> InvokeAsync<T>(string methodname, object param, int timeout, CancellationToken token) where T : class
        {
            if (TcpClient == null)
            {
                throw new NullReferenceException("TcpClient must be set before calling InvokeAsync");
            }

            if (methodname == null)
            {
                Logger?.Log(LogLevel.Warning, "InvokeAsync was call with a null value for the methodname. Returning without taking any action.");
                return null;
            }

            string rpcrequest;
            ulong? id = null;
            if (typeof(T) == typeof(Nil))
            {
                rpcrequest = GetInvokeString(methodname, param);
            }
            else
            {
                rpcrequest = GetInvokeString(methodname, param, out ulong tid);
                id = tid;
            }

            using (var e = new AutoResetEvent(false))
            {
                if (id.HasValue)
                {
                    await SendRequestAsync(rpcrequest, id.Value, e, token);
                    var delay = 0;
                    var succes = false;
                    while (!succes && delay < timeout)
                    {
                        token.ThrowIfCancellationRequested();
                        succes = e.WaitOne(100);
                        delay += 100;
                    }
                    var waiter = _isWaitingForReply.FirstOrDefault(x => x.Item1 == id);
                    if (!succes)
                    {
                        throw new ApplicationException(
                            $"Calling {methodname} failed or took longer than {timeout} miliseconds");
                    }
                    T result = null;
                    JsonRpcException error = null;
                    if (waiter?.Item2 != null)
                    {
                        if (waiter.Item2.Result != null)
                        {
                            result = (T) ((JObject) waiter.Item2.Result).ToObject(typeof(T));
                        }
                        if (waiter.Item2.Error != null)
                        {
                            error = waiter.Item2.Error;
                        }
                        _isWaitingForReply.Remove(waiter);
                    }
                    if (result != null)
                    {
                        return result;
                    }
                    if (error != null)
                    {
                        throw error;
                    }
                    throw new ApplicationException(
                        $"Calling {methodname} failed; both result and error were null after the call returned");
                }
                await SendRequestAsync(rpcrequest, token);
                return null;
            }
        }

        /// <summary>
        /// This method is meant to be called when (potentially) relevant data is received from the
        /// remote server, which may hold the result of a prior rpc call.
        /// Note: the data parsed must contain a complete (and valid) json-rpc response.
        /// </summary>
        /// <param name="sender">The sender object; mostly there for sematic reasons, 
        /// cause the method is typically used to subscribe to an event)</param>
        /// <param name="e">The received data</param>
        public void HandleDataReceived(object sender, string e)
        {
            if (!_jsonRpcResultRegex.IsMatch(e) && !_jsonRpcErrorRegex.IsMatch(e)) return;
            try
            {
                var response = JsonConvert.DeserializeObject<JsonRpcResponse>(e, new JsonSerializerSettings());
                if (response?.Id != null)
                {
                    var waiter = _isWaitingForReply.FirstOrDefault(x => x.Item1 == (ulong)((long)response.Id));
                    if (waiter == null) return;
                    _isWaitingForReply.Remove(waiter);
                    _isWaitingForReply.Add(new Tuple<ulong, JsonRpcResponse, AutoResetEvent>(waiter.Item1, response, waiter.Item3));
                    waiter.Item3.Set();
                }
                else if (response?.Error != null)
                {
                    Logger?.Log(LogLevel.Error, response.Error, "Error with JSON-RPC call, without ID.");
                    throw response.Error;
                }
            }
            catch
            {
                // ignored
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private ulong GetNextId()
        {
            return _nextId++;
        }

        private string GetInvokeString(string procedure, object arg, out ulong id)
        {
            id = GetNextId();
            var request = new JsonRpcRequest()
            {
                JsonRpc = "2.0",
                Id = id,
                Method = procedure,
                Params = arg
            };
            return JsonConvert.SerializeObject(request);
        }

        private string GetInvokeString(string procedure, object arg)
        {
            var request = new JsonRpcRequest()
            {
                JsonRpc = "2.0",
                Id = null,
                Method = procedure,
                Params = arg
            };
            var s = JsonConvert.SerializeObject(request);
            return s;
        }

        private async Task SendRequestAsync(string request, ulong id, AutoResetEvent e, CancellationToken token)
        {
            _isWaitingForReply.Add(new Tuple<ulong, JsonRpcResponse, AutoResetEvent>(id, null, e));
            await TcpClient.SendDataAsync(request, token);
        }

        private async Task SendRequestAsync(string request, CancellationToken token)
        {
            await TcpClient.SendDataAsync(request, token);
        }

        #endregion // Private Methods

        #region Constructor

        public JsonRpcClient()
        {
        }

        public JsonRpcClient(ITcpClient tcpClient, ILogger logger)
        {
            TcpClient = tcpClient;
            Logger = logger;
        }

        #endregion // Constructor
    }
}
