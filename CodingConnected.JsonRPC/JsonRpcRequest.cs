using Newtonsoft.Json;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Represents a json-rpc request. Instances of this class are used to map
    /// remote requests to local methods.
    /// </summary>
    public class JsonRpcRequest
    {
        #region Properties

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "params")]
        public object Params { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id")]
        public object Id { get; set; }

        #endregion // Properties

        #region Constructor

        #endregion // Constructor
    }
}
