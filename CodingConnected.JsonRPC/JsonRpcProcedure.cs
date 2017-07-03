using System;
using System.Collections.Generic;
using System.Linq;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Representation of methods that may be called remotely
    /// </summary>
    public class JsonRpcProcedure
    {
        #region Fields

        #endregion // Fields

        #region Properties

        /// <summary>
        /// Method name as used during json encoding.
        /// Note: this may differ from the local method name, through the use of [JsonRpcMethod]
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// List of parameters used by the procedure
        /// </summary>
        public JsonRpcProcedureParameter[] Parameters { get; }

        /// <summary>
        /// A delegate to the local method that will be invoked upon request
        /// </summary>
        public Delegate Method { get; }

        #endregion // Properties

        #region Constructor

        /// <summary>
        /// Instantiates JsonRpcProcedure with the given arguments
        /// </summary>
        public JsonRpcProcedure(string methodname, Delegate method, Dictionary<string, Type> prmsdict)
        {
            MethodName = methodname;
            Parameters = new JsonRpcProcedureParameter[prmsdict.Count];
            var dict = prmsdict.ToArray();
            for(var i = 0; i < dict.Length; ++i)
            {
                Parameters[i] = new JsonRpcProcedureParameter(dict[i].Key, dict[i].Value);
            }
            Method = method;
        }

        #endregion // Constructor
    }
}
