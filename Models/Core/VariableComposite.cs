﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Core
{

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
        public List<IVariable> Variables { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableComposite" /> class.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="variables">The list of variables to be evaluated</param>
        public VariableComposite(string name, List<IVariable> variables)
        {
            this.name = name;
            this.Variables = variables;
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
                foreach (IVariable variable in this.Variables)
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
                for (i = 0; i < this.Variables.Count - 1; i++)
                {
                    if (relativeTo != null)
                    {
                        Variables[i].Object = relativeTo;
                    }

                    relativeTo = Variables[i].Value;
                    if (relativeTo == null)
                    {
                        return;
                    }
                }

                Variables[i].Object = relativeTo;
                Variables[i].Value = value;
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
        /// Gets the text to use as a label for the property.
        /// </summary>
        public override string Caption
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the units of the property or null if not found.
        /// </summary>
        public override string Units
        {
            get
            {
                if (this.Variables.Count == 0)
                    return string.Empty;

                return Variables[Variables.Count - 1].Units;
            }

            set
            {

            }
        }

        /// <summary>
        /// Gets the units of the property as formmatted for display (in parentheses) or null if not found.
        /// </summary>
        public override string UnitsLabel
        {
            get
            {
                if (this.Variables.Count == 0)
                    return string.Empty;

                return Variables[Variables.Count - 1].UnitsLabel;
            }
        }

        /// <summary>
        /// Gets the associated display type for the related property.
        /// </summary>
        public override DisplayAttribute Display { get { return null; } }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public override Type DataType
        {
            get
            {
                if (Object != null)
                    return Object.GetType();

                if (Variables != null && Variables.Count > 0)
                    return Variables.Last().DataType;

                throw new Exception("Variable is null");
            }
        }

        /// <summary>
        /// Gets or sets the value of the specified property with arrays converted to comma separated strings.
        /// </summary>
        public override object ValueWithArrayHandling
        {
            get
            {
                return Object;
            }
        }

        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public override bool Writable { get { return true; } }

        /// <summary>
        /// Return an attribute
        /// </summary>
        /// <param name="attributeType">Type of attribute to find</param>
        /// <returns>The attribute or null if not found</returns>
        public override Attribute GetAttribute(Type attributeType) { return null; }

        /// <summary>Return the summary comments from the source code.</summary>
        public override string Summary
        {
            get
            {
                if (Variables.Last() is VariableProperty)
                    return (Variables.Last() as VariableProperty).Summary;
                else
                    return null;
            }
        }

        /// <summary>Return the remarks comments from the source code.</summary>
        public override string Remarks
        {
            get
            {
                if (Variables.Last() is VariableProperty)
                    return (Variables.Last() as VariableProperty).Remarks;
                else
                    return null;
            }
        }
    }
}
