// -----------------------------------------------------------------------
// <copyright file="VariableExpression.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------

namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class VariableExpression : IVariable
    {
        /// <summary>
        /// The expression string.
        /// </summary>
        private string expression;

        /// <summary>
        /// An instance of the expression evaluator once the expression has been parsed.
        /// </summary>
        private Utility.ExpressionEvaluator fn = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableExpression" /> class.
        /// </summary>
        /// <param name="expression">The string expression</param>
        public VariableExpression(string expression, Model model)
        {
            this.expression = expression;
            this.Object = model;

            // Perform initial parsing.
            this.fn = new Utility.ExpressionEvaluator();
            this.fn.Parse(this.expression.Trim());
            this.fn.Infix2Postfix();
        }

        /// <summary>
        /// A reference to the variables class so that getting of variable values is possible.
        /// </summary>
        public override Object Object { get; set; }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public override string Name
        {
            get
            {
                return expression;
            }
        }

        /// <summary>
        /// Gets or sets the value of the property.
        /// </summary>
        public override object Value
        {
            get
            {
                this.FillVariableNames();
                fn.EvaluatePostfix();
                if (fn.Error)
                {
                    throw new Exception(fn.ErrorDescription);
                }
                if (fn.Results != null)
                {
                    return fn.Results;
                }
                else
                {
                    return fn.Result;
                }
            }

            set
            {
                throw new Exception("Cannot set the value of an expression: " + expression);
            }
        }

        /// <summary>
        /// Gets a description of the property or null if not found.
        /// </summary>
        public override string Description
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the units of the property (in brackets) or null if not found.
        /// </summary>
        public override string Units
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        /// <summary>
        /// Fill the function variables with names.
        /// </summary>
        private void FillVariableNames()
        {
            ArrayList variablesToFill = fn.Variables;
            for (int i = 0; i < variablesToFill.Count; i++)
            {
                Utility.Symbol sym = (Utility.Symbol) variablesToFill[i];
                sym.m_values = null;
                sym.m_value = 0;
                object sometypeofobject = Apsim.Get(Object as Model, sym.m_name.Trim());
                if (sometypeofobject == null)
                    throw new Exception("Cannot find variable: " + sym.m_name + " while evaluating expression: " + expression);
                if (sometypeofobject is double)
                {
                    sym.m_value = (double)sometypeofobject;
                }
                else if (sometypeofobject is double[])
                {
                    sym.m_values = (double[])sometypeofobject;
                }
                variablesToFill[i] = sym;
            }
            fn.Variables = variablesToFill;
        }

    }
}
