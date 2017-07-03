using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Representation of the json-rpc service that is exposed for usage by remote clients. 
    /// This class typically gets instantiated by a call to GetInstanceService() on an 
    /// instance of IJsonRpcProcedureBinder. Subsequently, HandleRpc() should be called
    /// on the service instance, parsing the received requests as a string. 
    /// The class will take care of executing bound local methods, and return the data to be
    /// sent to remote clients as a resonpse to json-rpc calls.
    /// </summary>
    public class JsonRpcService
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _isJson20;

        #endregion // Fields

        #region Properties

        /// <summary>
        /// List of bound local methods that can be called remotely
        /// </summary>
        public List<JsonRpcProcedure> Procedures { get; }

        /// <summary>
        /// Boolean to set json-rpc protocol version.
        /// The default is 2.0; set this property after constructing to override that.
        /// </summary>
        public bool IsJson20
        {
            get => _isJson20;
            set => _isJson20 = value;
        }

        #endregion // Properties

        #region Public Methods

        /// <summary>
        /// Handles a json-rpc request by locating the appropriate local method to call, invoking it,
        /// and returning the result, or null if the local method was of type void.
        /// </summary>
        /// <param name="request">The json-rpc request in the form of a single string</param>
        /// <returns>The json-rpc response in the form of a single string</returns>
        public string HandleRpc(string request)
        {
            try
            {
                var result = HandleRpcInternal(JsonConvert.DeserializeObject<JsonRpcRequest>(request));
                return JsonConvert.SerializeObject(result);
            }
            catch (JsonReaderException)
            {
                var result = new JsonRpcResponse()
                {
                    JsonRpc = "2.0",
                    Result = null,
                    Error = new JsonRpcException(-32700, "Invalid JSON", "An error occurred on the server while parsing the JSON text. Request was: " + request),
                    Id = null
                };
                return JsonConvert.SerializeObject(result);
            }
        }

        /// <summary>
        /// Handles a json-rpc request by locating the appropriate local method to call, invoking it,
        /// and returning the result, or null if the local method was of type void.
        /// </summary>
        /// <param name="request">The json-rpc request in the form of an instance of JsonRpcRequest</param>
        /// <returns>The json-rpc response in the form of an instance of JsonRpcResonse</returns>
        public JsonRpcResponse HandleRpc(JsonRpcRequest request)
        {
            return HandleRpcInternal(request);
        }

        #endregion // Public Methods

        #region Private methods

        private JsonRpcResponse HandleRpcInternal(JsonRpcRequest request)
        {
            if(_isJson20 && request.JsonRpc != "2.0")
            {
                return new JsonRpcResponse()
                {
                    JsonRpc = "2.0",
                    Result = null,
                    Error = new JsonRpcException(-32600, "Invalid request", "This server only supports JSON-RPC 2.0."),
                    Id = request.Id
                };
            }

            var rpcproc = Procedures.FirstOrDefault(x => x.MethodName == request.Method);
            if(rpcproc == null)
            {
                return new JsonRpcResponse()
                {
                    JsonRpc = "2.0",
                    Result = null,
                    Error = new JsonRpcException(-32601, "Method not found", "The method does not exist / is not available."),
                    Id = request.Id
                };
            }

            var prmcount = rpcproc.Parameters.Length - 1;

            var parameters = new object[prmcount];

#warning Add handling of named parameters!

            var args = request.Params as JArray;
            if (args != null)
            {
                var reqprmscol = (ICollection) request.Params;
                var reqprmscount = 0;
                if (reqprmscol != null)
                {
                    reqprmscount = reqprmscol.Count;
                }
                if (reqprmscount != prmcount)
                {
                    return new JsonRpcResponse()
                    {
                        JsonRpc = "2.0",
                        Result = null,
                        Error = new JsonRpcException(-32602,
                            "Invalid params",
                            $"Expecting {prmcount} parameters, and received {reqprmscount}"),
                        Id = request.Id
                    };
                }
                for (var i = 0; i < prmcount; i++)
                {
                    parameters[i] = JsonConvert.DeserializeObject(args[i].ToString(), rpcproc.Parameters[i].Type);
                }
            }
            else if(request.Params is JObject arg)
            {
                if (prmcount != 1)
                {
                    return new JsonRpcResponse
                    {
                        JsonRpc = "2.0",
                        Result = null,
                        Error = new JsonRpcException(-32602,
                            "Invalid params",
                            $"Expecting {prmcount} parameters, and received 1"),
                        Id = request.Id
                    };
                }
                parameters[0] = JsonConvert.DeserializeObject(arg.ToString(), rpcproc.Parameters[0].Type);
            }

            try
            {
                if(rpcproc.Parameters.Last().Type == typeof(void))
                {
                    rpcproc.Method.DynamicInvoke(parameters);
                    return null;
                }
                var result = rpcproc.Method.DynamicInvoke(parameters);
                if (result == null)
                {
                    _logger.Warn("DynamicInvoke on method {0} returned null. This will cause a JSON-RPC error.", request.Method);
                }
                return new JsonRpcResponse() { JsonRpc = "2.0", Result = result, Error = null, Id = request.Id };
            }
            catch (Exception e)
            {
                // We really dont care about the TargetInvocationException, just pass on the inner exception
                var error = e as JsonRpcException;
                if (error != null)
                {
                    return new JsonRpcResponse() { Error = error };
                }
                var exception = e.InnerException as JsonRpcException;
                if (exception != null)
                {
                    return new JsonRpcResponse() { Error = exception };
                }
                return 
                    e.InnerException != null ? 
                    new JsonRpcResponse() { Error = new JsonRpcException(-32603, "Internal Error", e.InnerException) } : 
                    new JsonRpcResponse() { Error = new JsonRpcException(-32603, "Internal Error", e) };
            }
        }

        #endregion // Private methods

        #region Constructor

        public JsonRpcService()
        {
            _isJson20 = true;
            Procedures = new List<JsonRpcProcedure>();
        }

        #endregion // Constructor
    }
}
