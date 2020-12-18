using System;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Representation of a parameter for a JsonRpcProcedure
    /// </summary>
    public class JsonRpcProcedureParameter
    {
        #region Properties

        /// <summary>
        /// Name of the parameter.
        /// Note: this may be different from the local name, through the use of [JsonRpcParameter]
        /// </summary>
        public string Name { get; }
        public Type Type { get; }

        #endregion // Properties

        #region Constructor

        /// <summary>
        /// Instantiates JsonRpcProcedureParameter with the given arguments
        /// </summary>
        public JsonRpcProcedureParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        #endregion // Constructor
    }
}
