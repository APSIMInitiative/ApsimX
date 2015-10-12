using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Reflection;

using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.PMF.Functions
{

    /// <summary>
    /// A mathematical expression is evaluated using variables exposed within the Plant Modelling Framework.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ExpressionFunction : Model, IFunction
    {
        /// <summary>The expression</summary>
        [Core.Description("Expression")]
        public string Expression { get; set; }

        /// <summary>The function</summary>
        private ExpressionEvaluator fn = new ExpressionEvaluator();
        /// <summary>The parsed</summary>
        private bool parsed = false;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (!parsed)
                {
                    Parse(fn, Expression);
                    parsed = true;
                }
                FillVariableNames(fn, this);
                Evaluate(fn);
                return fn.Result;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionFunction()
        {
            Expression = string.Empty;
        }

        /// <summary>Parses the specified function.</summary>
        /// <param name="fn">The function.</param>
        /// <param name="ExpressionProperty">The expression property.</param>
        private static void Parse(ExpressionEvaluator fn, string ExpressionProperty)
        {
            fn.Parse(ExpressionProperty.Trim());
            fn.Infix2Postfix();
        }

        /// <summary>Fills the variable names.</summary>
        /// <param name="fn">The function.</param>
        /// <param name="RelativeTo">The relative to.</param>
        /// <exception cref="System.Exception">Cannot find variable:  + sym.m_name +  in function:  + RelativeTo.Name</exception>
        private static void FillVariableNames(ExpressionEvaluator fn, Model RelativeTo)
        {
            ArrayList varUnfilled = fn.Variables;
            ArrayList varFilled = new ArrayList();
            Symbol symFilled;
            foreach (Symbol sym in varUnfilled)
            {
                symFilled.m_name = sym.m_name;
                symFilled.m_type = ExpressionType.Variable;
                symFilled.m_values = null;
                symFilled.m_value = 0;
                object sometypeofobject = Apsim.Get(RelativeTo, sym.m_name.Trim());
                if (sometypeofobject == null)
                    throw new Exception("Cannot find variable: " + sym.m_name + " in function: " + RelativeTo.Name);
                symFilled.m_value = Convert.ToDouble(sometypeofobject);
                varFilled.Add(symFilled);
            }
            fn.Variables = varFilled;
        }

        /// <summary>Evaluates the specified function.</summary>
        /// <param name="fn">The function.</param>
        /// <exception cref="System.Exception"></exception>
        private static void Evaluate(ExpressionEvaluator fn)
        {
            fn.EvaluatePostfix();
            if (fn.Error)
            {
                throw new Exception(fn.ErrorDescription);
            }
        }

        /// <summary>
        /// Return the value of the specified property as an object. The PropertyName
        /// is relative to the RelativeTo argument (usually Plant).
        /// Format:
        /// [function(]VariableName[)]
        /// Where:
        /// function is optional and can be one of Sum, subtract, multiply, divide, max, min
        /// VariableName is the name of a Plant variable. It can also include an array. Array can
        /// have a filter inside of square brackets. Filter can be an array index (0 based)
        /// or be the name of a class or base class.
        /// e.g. Leaf.MinT
        /// sum(Leaf.Leaves[].Live.Wt)              - sums all live weights of all objects in leaves array.
        /// subtract(Leaf.Leaves[].Live.Wt)         - subtracts all live weights of all objects in leaves array.
        /// Leaf.Leaves[1].Live.Wt                  - returns the live weight of the 2nd element of the leaves array.
        /// sum(Leaf.Leaves[AboveGround].Live.Wt)   - returns the live weight of the 2nd element of the leaves array.
        /// </summary>
        /// <param name="Expression">The expression.</param>
        /// <param name="RelativeTo">The relative to.</param>
        /// <returns></returns>
        public static object Evaluate(string Expression, Model RelativeTo)
        {
            ExpressionEvaluator fn = new ExpressionEvaluator();
            Parse(fn, Expression);
            FillVariableNames(fn, RelativeTo);
            Evaluate(fn);
            if (fn.Results != null)
                return fn.Results;
            else
                return fn.Result;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            tags.Add(new AutoDocumentation.Paragraph("Value = "+Expression,indent));

        }

    }
}


