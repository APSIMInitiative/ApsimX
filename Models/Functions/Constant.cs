using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// A constant function (name=value)
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Constant : Model, IFunction
    {
        /// <summary>Gets the value.</summary>
        [Description("The value of the constant")]
        public double FixedValue { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("The optional units of the constant")]
        public string Units { get; set; }

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            return FixedValue;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write memos
            foreach (var tag in DocumentChildren<Memo>())
                yield return tag;

            // Write description of this class.
            yield return new Paragraph($"{Name} = {FixedValue} {FindUnits()}");
        }

        private string FindUnits()
        {
            if (!string.IsNullOrEmpty(Units))
                return $"({Units})";

            var parentType = Parent.GetType();
            var property = parentType.GetProperty(Name);
            if (property != null)
            {
                var unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return $"({unitsAttribute.ToString()})";
            }
            return null;
        }

    }
}