// -----------------------------------------------------------------------
// <copyright file="ExpressionFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A mathematical expression is evaluated using variables exposed within the Plant Modelling Framework.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ExpressionFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The function</summary>
        private ExpressionEvaluator fn = new ExpressionEvaluator();
        
        /// <summary>The parsed</summary>
        private bool parsed = false;

        /// <summary>The expression</summary>
        [Description("Expression")]
        public string Expression { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (!parsed)
            {
                Parse(fn, Expression);
                parsed = true;
            }
            FillVariableNames(fn, this);
            Evaluate(fn);
            if (fn.Results != null)
                return fn.Results;
            else
                return new double[] { fn.Result };
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
        /// <param name="expressionProperty">The expression property.</param>
        private static void Parse(ExpressionEvaluator fn, string expressionProperty)
        {
            fn.Parse(expressionProperty.Trim());
            fn.Infix2Postfix();
        }

        /// <summary>Fills the variable names.</summary>
        /// <param name="fn">The function.</param>
        /// <param name="relativeTo">The relative to.</param>
        /// <exception cref="System.Exception">Cannot find variable:  + sym.m_name +  in function:  + RelativeTo.Name</exception>
        private static void FillVariableNames(ExpressionEvaluator fn, Model relativeTo)
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
                object sometypeofobject = Apsim.Get(relativeTo, sym.m_name.Trim());
                if (sometypeofobject == null)
                    throw new Exception("Cannot find variable: " + sym.m_name + " in function: " + relativeTo.Name);
                if (sometypeofobject is IFunction)
                    sometypeofobject = (sometypeofobject as IFunction).Values();

                if (sometypeofobject is double[])
                    symFilled.m_values = (double[])sometypeofobject;
                else
                    symFilled.m_value = (double)sometypeofobject;
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
               // throw new Exception(fn.ErrorDescription);
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
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
                string st = Expression.Replace(".Value()", "");
                st = st.Replace("*", "x");
                tags.Add(new AutoDocumentation.Paragraph(Name + " = " + st, indent));
            }
        }

    }
}


