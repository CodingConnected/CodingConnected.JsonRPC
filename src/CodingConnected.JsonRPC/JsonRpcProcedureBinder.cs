using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// Class for binding the relevant methods of a given object to the json-rpc service.
    /// </summary>
    public class JsonRpcProcedureBinder : IJsonRpcProcedureBinder
    {
        #region Fields

        private static readonly object Locker = new object();
        private static volatile IJsonRpcProcedureBinder _default;

        #endregion // Fields

        #region Properties

        /// <summary>
        /// Default implementation of IJsonRpcProcedureBinder.
        /// </summary>
        public static IJsonRpcProcedureBinder Default
        {
            get
            {
                if (_default != null) return _default;
                lock (Locker)
                {
                    if (_default == null)
                    {
                        _default = new JsonRpcProcedureBinder();
                    }
                }
                return _default;
            }
        }

        #endregion // Properties

        #region IProcedureBinder

        /// <summary>
        /// Gets a JsonRpcService instance for a given object, with all relevant
        /// methods bound to the service.
        /// </summary>
        /// <param name="instance">Object whose methods (if tagged with [JsonRpcMethod] will be
        /// bound to the service instance</param>
        /// <returns>A new instance of JsonRpcService</returns>
        public JsonRpcService GetInstanceService(object instance, ILogger logger)
        {
            var service = new JsonRpcService(logger);
            
            var methods = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false).Length > 0);

            var methodInfos = methods as MethodInfo[] ?? methods.ToArray();
            
            foreach(var m in methodInfos)
            {
                string mname = null;
                var mattrib = m.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false).FirstOrDefault();
                if (mattrib != null)
                {
                    mname = ((JsonRpcMethodAttribute)mattrib).MethodName;
                }
                if (string.IsNullOrWhiteSpace(mname))
                {
                    mname = m.Name;
                }

                var prmsDict = new Dictionary<string, Type>();
                var prms = m.GetParameters();
                foreach (var prm in prms)
                {
                    string prmname = null;
                    var prmattrib = prm.GetCustomAttributes(typeof(JsonRpcParameterAttribute), false).FirstOrDefault();
                    if(prmattrib != null)
                    {
                        prmname = ((JsonRpcParameterAttribute)prmattrib).ParameterName;
                    }
                    if(string.IsNullOrWhiteSpace(prmname))
                    {
                        prmname = prm.Name;
                    }
                    prmsDict.Add(prmname, prm.ParameterType);
                }

                var returntype = m.ReturnType;
                prmsDict.Add("returns", returntype);

                var delt = System.Linq.Expressions.Expression.GetDelegateType(prmsDict.Values.ToArray());
                var del = Delegate.CreateDelegate(delt, instance, m);
                service.Procedures.Add(new JsonRpcProcedure(mname, del, prmsDict));
            }

            return service;
        }

        #endregion // IProcedureBinder

        #region Constructor

        #endregion // Constructor
    }
}
