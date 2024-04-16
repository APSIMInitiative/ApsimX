using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>Value returned is determined according to given criteria</summary>
    [Serializable]
    [Description("Tests if value of the first child is less than value of second child. Returns third child if true and forth if false")]
    public class LessThanFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private List<IFunction> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            ChildFunctions = FindAllChildren<IFunction>().ToList();

            double TestVariable = 0.0;
            double LessThanCriteria = 0.0;
            double IfTrue = 0.0;
            double IfFalse = 0.0;

            IFunction F = null;

            for (int i = 0; i < ChildFunctions.Count; i++)
            {
                F = ChildFunctions[i] as IFunction;
                if (i == 0)
                    TestVariable = F.Value(arrayIndex);
                if (i == 1)
                    LessThanCriteria = F.Value(arrayIndex);
                if (i == 2)
                    IfTrue = F.Value(arrayIndex);
                if (i == 3)
                    IfFalse = F.Value(arrayIndex);
            }

            if (TestVariable < LessThanCriteria)
                return IfTrue;
            else
                return IfFalse;
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            // Write memos.
            foreach (var tag in DocumentChildren<Memo>())
                yield return tag;

            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            if (ChildFunctions == null || ChildFunctions.Count < 1)
                yield break;

            string lhs;
            if (ChildFunctions[0] is VariableReference)
                lhs = (ChildFunctions[0] as VariableReference).VariableName;
            else if (ChildFunctions[0] is IModel model)
                lhs = model.Name;
            else
                throw new Exception($"Unknown model type '{ChildFunctions[0].GetType().Name}'");

            string rhs;
            if (ChildFunctions[1] is VariableReference)
                rhs = (ChildFunctions[1] as VariableReference).VariableName;
            else if (ChildFunctions[1] is IModel model)
                rhs = model.Name;
            else
                throw new Exception($"Unknown model type '{ChildFunctions[1].GetType().Name}'");

            yield return new Paragraph($"IF {lhs} < {rhs} THEN");
            foreach (ITag tag in ChildFunctions[2].Document())
                yield return tag;
            yield return new Paragraph("ELSE");
            foreach (ITag tag in ChildFunctions[3].Document())
                yield return tag;
        }
    }
}
