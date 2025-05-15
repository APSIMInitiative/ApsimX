using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace APSIM.Core
{
    /// <summary>
    /// Encapsulates a static list of models that have been discovered.
    /// </summary>
    public class ModelTypes
    {
        /// <summary>Known model types</summary>
        private static List<Type> modelTypes = null;

        /// <summary>Get a list of known model types.</summary>
        public static List<Type> GetModelTypes()
        {
            if (modelTypes == null)
            {
                modelTypes = new List<Type>();
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!a.IsDynamic && Path.GetFileName(a.Location) == "Models.exe")
                    {
                        foreach (Type t in a.GetTypes())
                        {
                            if (t.IsPublic && !t.IsInterface &&
                                t.GetInterface("IModel") != null)
                                modelTypes.Add(t);
                        }
                    }
                }
            }

            return modelTypes;
        }
    }
}
