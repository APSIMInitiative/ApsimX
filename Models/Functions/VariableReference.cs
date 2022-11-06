using System;
using APSIM.Shared.Documentation;
using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// Return the value of a nominated internal \ref Models.PMF.Plant "Plant" numerical variable
    /// </summary>
    /// \warning You have to specify the full path of numerical variable, which starts from the child of \ref Models.PMF.Plant "Plant".
    /// For example,  <b>[Phenology].ThermalTime.Value</b> refers to value of ThermalTime under phenology function.
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of a nominated internal Plant numerical variable")]
    public class VariableReference : Model, IFunction
    {

        /// <summary>The variable name</summary>
        [Description("Specify an internal Plant variable")]
        public string VariableName
        {
            get
            { return trimmedVariableName; }
            set
            { trimmedVariableName = value.Trim(); }
        }

        private string trimmedVariableName = "";


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            object o;
            try
            {
                o = Locator.Get(trimmedVariableName, LocatorFlags.ThrowOnError);
            }
            catch (Exception err)
            {
                throw new Exception($"Error while locating variable '{VariableName}' in variable reference '{this.FullPath}'", err);
            }

            // This should never happen: the locator is supposed to throw
            // if the variable cannot be found.
            if (o == null)
                throw new Exception("Unable to locate " + trimmedVariableName + " called from the variable reference function " + FullPath);

            if (o is IFunction)
                return (o as IFunction).Value(arrayIndex);
            else if (o is Array)
                return Convert.ToDouble((o as Array).GetValue(arrayIndex), 
                                        System.Globalization.CultureInfo.InvariantCulture);
            else
            {
                double doubleValue = Convert.ToDouble(o, System.Globalization.CultureInfo.InvariantCulture);
                if (double.IsNaN(doubleValue))
                    throw new Exception("NaN (not a number) found when getting variable: " + VariableName);
                return doubleValue;
            }
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            string variable = StringUtilities.RemoveTrailingString(VariableName, ".Value()");
            yield return new Paragraph($"*{Name} = {variable}*");
        }

    }
}