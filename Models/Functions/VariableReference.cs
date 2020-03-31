using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using APSIM.Shared;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// # [Name]
    /// Return the value of a nominated internal \ref Models.PMF.Plant "Plant" numerical variable
    /// </summary>
    /// \warning You have to specify the full path of numerical variable, which starts from the child of \ref Models.PMF.Plant "Plant".
    /// For example,  <b>[Phenology].ThermalTime.Value</b> refers to value of ThermalTime under phenology function.
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of a nominated internal Plant numerical variable")]
    public class VariableReference : Model, IFunction, ICustomDocumentation
    {
        [Link]
        ILocator locator = null;

        /// <summary>The variable name</summary>
        [Description("Specify an internal Plant variable")]
        public string VariableName { get; set; }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            object o = locator.Get(VariableName.Trim());
            if (o == null)
                throw new Exception("Unable to locate " + VariableName.Trim() + " called from the variable reference function " + Apsim.FullPath(this));

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

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);


                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + StringUtilities.RemoveTrailingString(VariableName, ".Value()") + "</i>", indent));
            }
        }

    }
}