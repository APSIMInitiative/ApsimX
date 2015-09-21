using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.IO;

namespace Models.PMF.Functions
{
    /// <summary>Multiplies the values of the children of this node</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Product of value of all children of this node. Return 1 if no child.
    [Serializable]
    [Description("Returns the product of all children function values")]
    public class MultiplyFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                double returnValue = 1.0;

                foreach (IFunction F in ChildFunctions)
                    returnValue = returnValue * F.Value;
                return returnValue;
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            SubtractFunction.DocumentMathFunction(this, 'x', tags, headingLevel, indent);
        }
    }
}