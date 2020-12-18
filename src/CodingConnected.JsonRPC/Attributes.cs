using System;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Indicates a method is meant to be consumed remotely, by being exposed through
    /// an instance of JsonRpcService.
    /// Optionally, the method name to used in json encoding can be set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class JsonRpcMethodAttribute : Attribute
    {
        public string MethodName { get; }

        public JsonRpcMethodAttribute(string methodname = "")
        {
            MethodName = methodname;
        }
    }

    /// <summary>
    /// Can be used to override the name of a method parameter to be used 
    /// during json encoding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonRpcParameterAttribute : Attribute
    {
        public string ParameterName { get; }

        public JsonRpcParameterAttribute(string parametername = "")
        {
            ParameterName = parametername;
        }
    }
}
