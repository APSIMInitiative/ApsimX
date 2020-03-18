namespace UserInterface.Commands
{
    using System;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    using Models.Core;

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
            if (obj is IModel && (obj as IModel).ReadOnly)
                throw new ApsimXException(obj as IModel, string.Format("Unable to modify {0} - it is read-only.", (obj as IModel).Name));
            Property property = new Property(obj, name, value);

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
            /// Initializes a new instance of the <see cref="Property" /> class.
            /// </summary>
            /// <param name="obj">The object containing the property</param>
            /// <param name="name">The name of the property</param>
            /// <param name="value">The new value of the property</param>
            public Property(object obj, string name, object value)
            {
                if (obj is IModel && (obj as IModel).ReadOnly && name != "ReadOnly")
                    throw new ApsimXException(obj as IModel, string.Format("Unable to modify {0} - it is read-only.", (obj as IModel).Name));
                this.Obj = obj;
                this.Name = name;
                this.NewValue = value;
            }

            /// <summary>
            /// Perform the property change
            /// </summary>
            /// <returns>Returns true if the property was successfully modified.</returns>
            public bool Do()
            {
                try
                {
                    // Get original value of property so that we can restore it in Undo if needed.
                    this.oldValue = ReflectionUtilities.GetValueOfFieldOrProperty(this.Name, this.Obj);

                    if (this.oldValue != null && this.oldValue.Equals(this.NewValue))
                    {
                        this.wasModified = false;
                    }
                    else
                    {
                        this.wasModified = ReflectionUtilities.SetValueOfFieldOrProperty(this.Name, this.Obj, this.NewValue);
                    }
                }
                catch (Exception e)
                {
                    this.wasModified = false;
                    Exception rethrow;
                    if (e is System.Reflection.TargetInvocationException)
                        rethrow = (e as System.Reflection.TargetInvocationException).InnerException;
                    else
                        rethrow = e;
                    throw rethrow; // Pass the exception on for further handling
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
                    ReflectionUtilities.SetValueOfProperty(this.Name, this.Obj, this.oldValue);
                }

                return this.wasModified;
            }
        }
    }
}
