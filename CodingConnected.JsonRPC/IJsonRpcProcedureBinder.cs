namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Interface to allow overriding default JsonRpcProcedureBinder, to ease unit testing
    /// </summary>
    public interface IJsonRpcProcedureBinder
    {
        JsonRpcService GetInstanceService(object instance);
    }
}
