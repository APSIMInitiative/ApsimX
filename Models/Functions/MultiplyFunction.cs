using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Documentation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>A class that returns the product of its child functions.</summary>
    [Serializable]
    public class MultiplyFunction : Model, IFunction//
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double returnValue = 1.0;

            foreach (IFunction F in ChildFunctions)
                returnValue = returnValue * F.Value(arrayIndex);
            return returnValue;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public static IEnumerable<ITag> DocumentMathFunction(char op, string name, IEnumerable<IModel> children)
        {
            foreach (var child in children.Where(c => c is Memo))
                foreach (var tag in child.Document())
                    yield return tag;

            var writer = new StringBuilder();
            writer.Append($"*{name}* = ");
            var childrenToDocument = new List<IModel>();
            bool addOperator = false;
            foreach (var child in children.Where(c => !(c is Memo)))
            {
                if (addOperator)
                    writer.Append($" {op} ");

                if (child is VariableReference varRef)
                    writer.Append(varRef.VariableName);
                else if (child is Constant c && NameEqualsValue(c.Name, c.FixedValue))
                    writer.Append(c.FixedValue);
                else
                {
                    writer.Append($"*" + child.Name + "*");
                    childrenToDocument.Add(child);
                }
                addOperator = true;
            }
            yield return new Paragraph(writer.ToString());

            foreach (var child in children.Where(c => childrenToDocument.Contains(c)))
                foreach (var tag in child.Document())
                    yield return tag;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (var tag in DocumentMathFunction('x', Name, Children))
                yield return tag;
        }

        private static bool NameEqualsValue(string name, double value)
        {
            return name.Equals("zero", StringComparison.InvariantCultureIgnoreCase) && value == 0 ||
                   name.Equals("one", StringComparison.InvariantCultureIgnoreCase) && value == 1 ||
                   name.Equals("two", StringComparison.InvariantCultureIgnoreCase) && value == 2 ||
                   name.Equals("three", StringComparison.InvariantCultureIgnoreCase) && value == 3 ||
                   name.Equals("four", StringComparison.InvariantCultureIgnoreCase) && value == 4 ||
                   name.Equals("five", StringComparison.InvariantCultureIgnoreCase) && value == 5 ||
                   name.Equals("six", StringComparison.InvariantCultureIgnoreCase) && value == 6 ||
                   name.Equals("seven", StringComparison.InvariantCultureIgnoreCase) && value == 7 ||
                   name.Equals("eight", StringComparison.InvariantCultureIgnoreCase) && value == 8 ||
                   name.Equals("nine", StringComparison.InvariantCultureIgnoreCase) && value == 9 ||
                   name.Equals("ten", StringComparison.InvariantCultureIgnoreCase) && value == 10 ||
                   name.Equals("constant", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}