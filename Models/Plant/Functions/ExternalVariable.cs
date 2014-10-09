using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the value of a nominated external APSIM numerical variable.
    /// Note: This should be merged with the variable function when naming convention
    /// to refer to internal and external variable is standardized. FIXME
    /// </summary>
    [Serializable]
    [Description("Returns the value of a nominated external APSIM numerical variable")]
    public class ExternalVariable : Function
    {
        /// <summary>The variable name</summary>
        public string VariableName = "";



        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception"></exception>
        public override double Value
        {
            get
            {
                object val = Apsim.Get(this, VariableName);

                if (val != null)
                    return Convert.ToDouble(val);
                else
                    throw new Exception(Name + ": External value for " + VariableName.Trim() + " not found");
            }
        }

    }
}