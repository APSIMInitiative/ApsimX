using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Reflection;
using System.Collections;
namespace Utility
{


    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ModelFunctions
    {


        #region Parameter functions

        /// <summary>
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/writtable
        /// </summary>
        public static IVariable[] Parameters(object model)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                if (property.CanRead && property.CanWrite)
                {
                    Attribute XmlIgnore = Utility.Reflection.GetAttribute(property, typeof(System.Xml.Serialization.XmlIgnoreAttribute), true);

                    bool ignoreProperty = XmlIgnore != null;                                 // No [XmlIgnore]
                    ignoreProperty |= property.PropertyType.GetInterface("IList") != null;   // No List<T>
                    ignoreProperty |= property.PropertyType.IsSubclassOf(typeof(Model));     // Nothing derived from Model.
                    ignoreProperty |= property.Name == "Name";                               // No Name properties.

                    if (!ignoreProperty)
                        allProperties.Add(new VariableProperty(model, property));
                }
            }
            return allProperties.ToArray();
        }

        /// <summary>
        /// Return a complete list of state variables (public and private) for the specified model.
        /// </summary>
        public static IVariable[] States(object model)
        {
            List<IVariable> variables = new List<IVariable>();
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                if (field.DeclaringType != typeof(Model) && field.FieldType != typeof(Model) && !field.FieldType.IsSubclassOf(typeof(Model)) &&
                    !field.FieldType.Name.Contains("Delegate"))
                    variables.Add(new VariableField(model, field));
            }

            return variables.ToArray();
        }

        /// <summary>
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/writtable
        /// </summary>
        public static IVariable[] FieldsAndProperties(object model, BindingFlags flags)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().UnderlyingSystemType.GetProperties(flags))
            {
                if (property.CanRead)
                    allProperties.Add(new VariableProperty(model, property));
            }
            foreach (FieldInfo field in model.GetType().UnderlyingSystemType.GetFields(flags))
                    allProperties.Add(new VariableField(model, field));
            return allProperties.ToArray();
        }

      

        #endregion
    }
}
