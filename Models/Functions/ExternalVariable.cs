using System;
using System.Globalization;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Returns the value of a nominated external APSIM numerical variable.
    /// Note: This should be merged with the variable function when naming convention
    /// to refer to internal and external variable is standardized. FIXME
    /// </summary>
    [Serializable]
    [Description("Returns the value of a nominated external APSIM numerical variable")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ExternalVariable : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The variable name</summary>
        [Description("VariableName")]
        public string VariableName { get; set; }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            object val = Structure.GetObject(VariableName)?.Value;

            if (val != null)
            {
                if (val is Array && arrayIndex > -1)
                    return Convert.ToDouble((val as Array).GetValue(arrayIndex), CultureInfo.InvariantCulture);
                else
                    return Convert.ToDouble(val, CultureInfo.InvariantCulture);
            }
            else
                throw new Exception(Name + ": External value for " + VariableName.Trim() + " not found");
        }

    }
}