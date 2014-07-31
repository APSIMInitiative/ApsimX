// -----------------------------------------------------------------------
// <copyright file="ChangeProperty.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Commands
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Perform one or more changes to properties in objects.
    /// </summary>
    public class ChangeProperty : ICommand
    {
        /// <summary>
        /// The list of all properties that need changing
        /// </summary>
        private IEnumerable<Property> properties = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeProperty" /> class.
        /// </summary>
        /// <param name="obj">The object containing the property</param>
        /// <param name="name">The name of the property</param>
        /// <param name="value">The new value of the property</param>
        public ChangeProperty(object obj, string name, object value)
        {
            Property property = new Property();
            property.Obj = obj;
            property.Name = name;
            property.NewValue = value;

            List<Property> listOfProperties = new List<Property>();
            listOfProperties.Add(property);
            this.properties = listOfProperties;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeProperty" /> class.
        /// </summary>
        /// <param name="properties">A list of properties that need changing</param>
        public ChangeProperty(IEnumerable<Property> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Perform the change.
        /// </summary>
        /// <param name="commandHistory">The parent command history object</param>
        public void Do(CommandHistory commandHistory)
        {
            // Maintain a list of objects that have been changed.
            List<object> objectsChanged = new List<object>();

            // Change all properties.
            foreach (Property property in this.properties)
            {
                if (property.Do())
                {
                    if (!objectsChanged.Contains(property.Obj))
                    {
                        objectsChanged.Add(property.Obj);
                    }
                }
            }
            
            // Loop through all changed objects and invoke a model changed event for each.
            foreach (object obj in objectsChanged)
            {
                commandHistory.InvokeModelChanged(obj);
            }
        }

        /// <summary>
        /// Undo all property changes
        /// </summary>
        /// <param name="commandHistory">The parent command history object</param>
        public void Undo(CommandHistory commandHistory)
        {
            // Maintain a list of objects that have been changed.
            List<object> objectsChanged = new List<object>();

            // Undo all property changes
            foreach (Property property in this.properties)
            {
                if (property.UnDo() && !objectsChanged.Contains(property.Obj))
                {
                    objectsChanged.Add(property.Obj);
                }
            }

            // Loop through all changed objects and invoke a model changed event for each.
            foreach (object obj in objectsChanged)
            {
                commandHistory.InvokeModelChanged(obj);
            }
        }

        /// <summary>
        /// A helper class for specifying a property change.
        /// </summary>
        public class Property
        {
            /// <summary>
            /// The old value of the property, before the change.
            /// </summary>
            private object oldValue;

            /// <summary>
            /// A value indicating whether the property was modified.
            /// </summary>
            private bool wasModified;

            /// <summary>
            /// Gets or sets the object that contains the property.
            /// </summary>
            public object Obj { get; set; }

            /// <summary>
            /// Gets or sets the property name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the new property value
            /// </summary>
            public object NewValue { get; set; }

            /// <summary>
            /// Perform the property change
            /// </summary>
            /// <returns>Returns true if the property was successfully modified.</returns>
            public bool Do()
            {
                try
                {
                    // Get original value of property so that we can restore it in Undo if needed.
                    this.oldValue = Utility.Reflection.GetValueOfFieldOrProperty(this.Name, this.Obj);

                    if (this.oldValue != null && this.oldValue.Equals(this.NewValue))
                    {
                        this.wasModified = false;
                    }
                    else
                    {
                        this.wasModified = Utility.Reflection.SetValueOfProperty(this.Name, this.Obj, this.NewValue);
                    }
                }
                catch (Exception)
                {
                    this.wasModified = false;
                }

                return this.wasModified;
            }

            /// <summary>
            /// Perform the property change
            /// </summary>
            /// <returns>True if the property change was undone successfully</returns>
            public bool UnDo()
            {
                if (this.wasModified)
                {
                    Utility.Reflection.SetValueOfProperty(this.Name, this.Obj, this.oldValue);
                }

                return this.wasModified;
            }
        }
    }
}
