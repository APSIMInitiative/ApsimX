using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Documentation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>This class calculates the minimum of all child functions.</summary>
    [Serializable]
    [Description("Returns the Minimum value of all children functions")]
    public class MinimumFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double ReturnValue = 999999999;
            foreach (IFunction F in ChildFunctions)
            {
                ReturnValue = Math.Min(ReturnValue, F.Value(arrayIndex));
            }
            return ReturnValue;
        }

        /// <summary>String list of child functions</summary>
        public string ChildFunctionList
        {
            get
            {
                return AutoDocumentation.ChildFunctionList(this.FindAllChildren<IFunction>().ToList());
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public static IEnumerable<ITag> DocumentMinMaxFunction(string functionName, string name, IEnumerable<IModel> children)
        {
            foreach (var child in children.Where(c => c is Memo))
                foreach (var tag in child.Document())
                    yield return tag;

            var writer = new StringBuilder();
            writer.Append($"*{name}* = {functionName}(");

            bool addComma = false;
            foreach (var child in children.Where(c => !(c is Memo)))
            {
                if (addComma)
                    writer.Append($", ");
                writer.Append($"*" + child.Name + "*");
                addComma = true;
            }
            writer.Append(')');
            yield return new Paragraph(writer.ToString());

            yield return new Paragraph("Where:");

            foreach (var child in children.Where(c => !(c is Memo)))
                foreach (var tag in child.Document())
                    yield return tag;
        }

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (ITag tag in DocumentMinMaxFunction("Min", Name, Children))
                yield return tag;
        }
    }
}