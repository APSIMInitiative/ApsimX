// -----------------------------------------------------------------------
// <copyright file="IVariable.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class encapsulates a list of IVariables that are evaluated when
    /// the Value property is called.
    /// source code.
    /// </summary>
    [Serializable]
    public class VariableComposite : IVariable
    {
        /// <summary>
        /// The name of the composite variable
        /// </summary>
        private string name;

        /// <summary>
        /// The list of variables to be evaluated
        /// </summary>
        private List<IVariable> variables;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableComposite" /> class.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="variables">The list of variables to be evaluated</param>
        public VariableComposite(string name, List<IVariable> variables)
        {
            this.name = name;
            this.variables = variables;
        }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public override string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets or sets the object this variable is relative to
        /// </summary>
        public override object Object { get; set; }

        /// <summary>
        /// Gets or sets the value of the property.
        /// </summary>
        public override object Value
        {
            get
            {
                object relativeTo = null;
                foreach (IVariable variable in this.variables)
                {
                    if (relativeTo != null)
                    {
                        variable.Object = relativeTo;
                    }

                    relativeTo = variable.Value;
                    if (relativeTo == null)
                    {
                        return null;
                    }
                }

                return relativeTo;
            }

            set
            {
                object relativeTo = null;
                int i;
                for (i = 0; i < this.variables.Count-1; i++)
                {
                    if (relativeTo != null)
                    {
                        variables[i].Object = relativeTo;
                    }

                    relativeTo = variables[i].Value;
                    if (relativeTo == null)
                    {
                        return;
                    }
                }

                variables[i].Object = relativeTo;
                variables[i].Value = value;
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
                if (this.variables.Count == 0)
                    return string.Empty;

                return variables[variables.Count - 1].Units;
            }

            set
            {

            }
        }
    }
} 
