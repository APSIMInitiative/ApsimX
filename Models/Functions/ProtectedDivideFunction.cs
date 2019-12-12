using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Functions
{
    /// <summary>
    /// Returns the value of the first child function divided by the
    /// value of the seocond child function. Returns special values if
    /// the numerator is 0 or if the denominator is 0.
    /// </summary>
    /// <remarks>
    /// Currently used in sorghum/maize code to mimic divide functions
    /// in old apsim which return 10 if the denominator is 0 or 0 if
    /// the numerator is 0.
    /// </remarks>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class ProtectedDivideFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>Value to return if numerator is 0.</summary>
        [Description("Value to return if numerator is 0:")]
        public double NumeratorErrVal { get; set; }

        /// <summary>Value to return if denominator is 0.</summary>
        [Description("Value to return if denominator is 0:")]
        public double DenominatorErrVal { get; set; }

        /// <summary>
        /// Returns the value of the function.
        /// </summary>
        public double Value(int arrayIndex = -1)
        {
            IFunction[] children = Apsim.Children(this, typeof(IFunction)).Cast<IFunction>().ToArray();
            int n = children?.Length ?? 0;
            if (n < 2)
                throw new Exception($"Error in ProtectedDivideFunction {Name}: 2 child functions required, only found {n}");

            double numerator = children[0].Value(arrayIndex);
            double denominator = children[1].Value(arrayIndex);

            if (MathUtilities.FloatsAreEqual(numerator, 0))
                return NumeratorErrVal;

            if (MathUtilities.FloatsAreEqual(denominator, 0))
                return DenominatorErrVal;

            return numerator / denominator;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                SubtractFunction.DocumentMathFunction(this, '/', tags, headingLevel, indent);
            }
        }
    }
}
