

namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Encapsulates either a ModelNode or a object/ property combination. The GetProperty method
    /// returns one of these.
    /// </summary>
    public class Property
    {
        /// <summary></summary>
        private object model;
        private PropertyInfo property;

        /// <summary>Constructor</summary>
        /// <param name="model">A ModelNode starting node</param>
        internal Property(ModelWrapper model) { this.model = model; }

        /// <summary>
        /// Try assuming that model is a ModelNode and look for a child model of the specified name.
        /// If found, the this property will be set to the child model.
        /// </summary>
        /// <param name="childName">The child name to look for.</param>
        /// <param name="compareType">The type of comparison to perform.</param>
        /// <returns>Returns true if the property was set to a child model.</returns>
        internal bool SetToChildModel(string childName, StringComparison compareType)
        {
            if (model != null && model is ModelWrapper)
            {
                object localModel = (model as ModelWrapper).Children.Find(m => m.Name.Equals(childName, compareType));
                if (localModel != null)
                {
                    model = localModel;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Try looking for a PropertyInfo in the model. If found then set this property accordingly.
        /// </summary>
        /// <param name="propertyName">The property name to look for.</param>
        /// <param name="bindingFlags">The binding flags to use.</param>
        /// <returns>Returns true if the property info was found.</returns>
        internal bool SetToProperty(string propertyName, BindingFlags bindingFlags)
        {
            if (model != null && model is ModelWrapper)
            {
                PropertyInfo localProperty = (model as ModelWrapper).Model.GetType().GetProperty(propertyName, bindingFlags);
                if (localProperty != null)
                {
                    property = localProperty;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Gets the value of this property.</summary>
        public object Get()
        {
            if (property == null)
            {
                if (model is ModelWrapper)
                    return (model as ModelWrapper).Model;
                else
                    return model;
            }
            else if (model is ModelWrapper)
                return property.GetValue((model as ModelWrapper).Model, null);
            else
                return property.GetValue(model, null);
        }
        /// <summary>Sets the value of this property. Returns true if value was set.</summary>
        public bool Set(object value)
        {
            if (property != null)
            {
                if (model is ModelWrapper)
                    property.SetValue((model as ModelWrapper).Model, value, null);
                else
                    property.SetValue(model, value, null);
                return true;
            }
            return false;
        }

    }
}
