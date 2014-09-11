namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Runtime.Serialization;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Reflection;

    /// <summary>
    /// A class of static model functions.
    /// </summary>
    public class ModelFunctions
    {

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
        
        /// <summary>
        /// Parent all child models.
        /// </summary>
        public static void ParentAllChildren(Model model)
        {
            if (model != null)
                foreach (Model child in model.Children)
                {
                    child.Parent = model;
                    ParentAllChildren(child);
                }
        }
    }
}
