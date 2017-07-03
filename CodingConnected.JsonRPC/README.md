# CodingConnected.JsonRPC

## Overview

A compact library to ease using json-rpc in .NET.

This library was inspired by the JSON-RPC.NET project by Austin Harris, that can be https://github.com/Astn/JSON-RPC.NET. Compared to JSON-RPC.NET, this library is somewhat limited when it comes to the server side. Specifically, default function arguments of local functions are not supported, named arguments in json requests are currently ignored, and batch calls are not supported from within the library. Also, the API is different, as described below.

## Usage

### Server side

To build a json-rpc server using the library, create a class that will hold the methods to expose remotely, and decorate the relevant methods with the `JsonRpcMethod` attribute as follows:

    public class SomeJsonRpcHandler
    {
        [JsonRpcMethod]
        public string GreetMethod(string name)
        {
            return "Hello, " + name + "!";
        }
    }

Now, create an instance of `JsonRpcService` which will expose the decorated methods, using the default instance of `JsonRpcProcedureBinder`:

    var service = JsonRpcProcedureBinder.Default.GetInstanceService(rpcHandlerClassInstance);

By using `this` as an argument to `GetInstanceService()`, a service object could be a member of the class that holds the methods to be exposed.

Finally, on receiving data from a connected client, forward that data to the service, and send the result to the client. For example, supposing there is an object `tcpClient` that raises an event `DataReceived` when it has detected a full json-rpc message is received over TCP, we can do this (assuming e is a string):

    var _jsonRpcMethodRegex = new Regex(@"['""]method['""]", RegexOptions.Compiled);
    tcpClient.DataReceived += async (o, e) =>
    {
        if (!_jsonRpcMethodRegex.IsMatch(e)) return;
        var result = _service?.HandleRpc(e);
        if (result != null)
        {
            await tcpClient.SendDataAsync(result, token);
        }
    };

That's it!

### Client side

The library offers a client object to ease consumption of a remote json-rpc service: `JsonRpcClient`. To use the class, instantiate it, and do the following:
- Set its TcpClient property, either by parsing it to the constructor, or setting is seperately. The TcpClient property must reference an object that implements the CodingConnected.JsonRPC.ITcpClient interface, which allows the JsonRpcClient to send data over TCP.
- Make sure the `HandleDataReceived()` method is called when (potentially) relevant data is received. The method will check if the string contains relevant json data, and then deserialize the string as a json-rpc result, or do nothing if the data is found to be irrelevant.
 - This method has the signasure to allow subscription to an event of type `Eventhandler<string>`. For example, build a small wrapper around a NetworkStream, to filter complete json response messages, and raise an event to which this method is subscribed once a complete message has been received.
 - Alternatively, call it directly with the sender argument set to null, and the e argument set to the string containing the received data.

As an example, here is a code fragment instantiating JsonRpcClient:

    var rpcClient = new JsonRpcClient();
    _rpcClient.TcpClient = SomeTcpClientWrapper;
    SomeTcpClientWrapper.DataReceived += _rpcClient.HandleDataReceived;

Now, we can call remote methods on the client via the `InvokeAsync()` method:

    await _rpcClient.InvokeAsync<string>("GreetMethod", "Menno", 1000, token);

This makes it easy to construct local proxy classes that represent remote object whose methods will be called. For example:

    public class GreetGetterProxy
    {
        private static readonly JsonRpcClient _rpcClient;

        public async Task<string> GreetMethodAsynd(string name, CancellationToken token)
        {
            try
            {
                return await _rpcClient.InvokeAsync<string>("GreetMethod", "Menno", 1000, token);
            }
            catch (JsonRpcException e)
            {
                // handle exception
            }
            catch (Exception e)
            {
                // handle exception
            }
            return null;
        }

        public GreetGetter(SomeTcpClientWrapper client)
        {
            _rpcClient = new JsonRpcClient((ITcpClient)client);
            _rpcClient.TcpClient = client;
            client.DataReceived += _rpcClient.HandleDataReceived;
        }
    }

Subsequently, it is easy to interface with the proxy, replace with a dummy class during unit testing, etc. That's it!

# Disclaimer

The library comes *as is*, without warrenty of any kind. Please note that the library has been extensively tested and used in a project, but has not (yet) been unit tested.