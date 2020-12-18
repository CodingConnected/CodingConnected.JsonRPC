using Newtonsoft.Json;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Represents a json-rpc response, which contains the result of a call to a
    /// local method to be returned to a remote client.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class JsonRpcResponse
    {
        #region Properties

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "result")]
        public object Result { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "error")]
        public JsonRpcException Error { get; set; }

        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }

        #endregion // Properties
    }
}
