namespace Models.Core
{
    using System;
    using System.Reflection;
    using Models.Core;

    /// <summary>
    /// This abstract base class encapsulates the interface for a variable from a Model.
    /// source code.
    /// </summary>
    public interface IVariable
    {
        /// <summary>
        /// Return the name of the property.
        /// </summary>
        string Name { get;}

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        object Value { get;}

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        object DefaultValue { get;}

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        string Description { get;}

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        string Units { get; }

    }

    /// <summary>
    /// This class encapsulates a single property of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    public class VariableObject : IVariable
    {
        private object Obj;

        /// <summary>
        /// Constructor.
        /// </summary>
        public VariableObject(object model)
        {
            Obj = model;
        }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public string Name
        {
            get
            {
                if (Obj is Model) return (Obj as Model).Name;
                else throw new ApsimXException("", "Cannot get the name of object with type '" + Obj.GetType().Name + "' in VariableObject class");
            }
        }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public object Value
        {
            get
            {
                return Obj;
            }
        }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public object DefaultValue
        {
            get
            {
                if (Obj is Model)
                {
                    Model model = (Obj as Model);
                    return model.DefaultModel;
                }
                else
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public string Description
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public string Units
        {
            get
            {
                return null;
            }
        }

    }

    /// <summary>
    /// This class encapsulates a single field of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    public class VariableField : IVariable
    {
        private object Obj;
        private FieldInfo FieldInfo;

        /// <summary>
        /// Constructor
        /// </summary>
        public VariableField(object model, FieldInfo fieldInfo)
        {
            if (model == null || fieldInfo == null)
                throw new ApsimXException("", "Cannot create an instance of class VariableField with a null model or fieldInfo");
            Obj = model;
            FieldInfo = fieldInfo;
        }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public string Name 
        { 
            get 
            {
                if (FieldInfo.Name.Contains("BackingField"))
                {
                    string st = FieldInfo.Name;
                    return "[" + Utility.String.SplitOffBracketedValue(ref st, '<', '>') + "]";
                }
                return FieldInfo.Name; 
            } 
        }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public object Value { get { return FieldInfo.GetValue(Obj); } }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public object DefaultValue 
        { 
            get 
            { 
                if (Obj is Model)
                    return FieldInfo.GetValue((Obj as Model).DefaultModel); 
                else
                    throw new ApsimXException("", "Cannot return a default value for object type '" + Obj.GetType().Name + "' in VariableField class");
            } 
        }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public string Description
        {
            get
            {
                Description descriptionAttribute = Utility.Reflection.GetAttribute(FieldInfo, typeof(Description), false) as Description;
                if (descriptionAttribute != null && descriptionAttribute.ToString() != "")
                    return descriptionAttribute.ToString();
                return null;
            }
        }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public string Units
        {
            get
            {
                Units unitsAttribute = Utility.Reflection.GetAttribute(FieldInfo, typeof(Units), false) as Units;
                if (unitsAttribute != null)
                    return "(" + unitsAttribute.UnitsString + ")";
                return null;
            }
        }

    }


    /// <summary>
    /// This class encapsulates a single property of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    public class VariableProperty : IVariable
    {
        private object Obj;
        private PropertyInfo PropertyInfo;

        /// <summary>
        /// Constructor
        /// </summary>
        public VariableProperty(object model, PropertyInfo propertyInfo)
        {
            if (model == null || propertyInfo == null)
                throw new ApsimXException("", "Cannot create an instance of class VariableProperty with a null model or propertyInfo");
            Obj = model;
            PropertyInfo = propertyInfo;
        }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public string Name { get { return PropertyInfo.Name; } }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public object Value { get { return PropertyInfo.GetValue(Obj, null); } }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public object DefaultValue
        {
            get
            {
                if (Obj is Model)
                    return PropertyInfo.GetValue((Obj as Model).DefaultModel, null);
                else
                    throw new ApsimXException("", "Cannot return a default value for object type '" + Obj.GetType().Name + "' in VariableField class");
            }
        }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public string Description
        {
            get
            {
                Description descriptionAttribute = Utility.Reflection.GetAttribute(PropertyInfo, typeof(Description), false) as Description;
                if (descriptionAttribute != null && descriptionAttribute.ToString() != "")
                    return descriptionAttribute.ToString();
                return null;
            }
        }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public string Units
        {
            get
            {
                Units unitsAttribute = Utility.Reflection.GetAttribute(PropertyInfo, typeof(Units), false) as Units;
                if (unitsAttribute != null)
                    return "(" + unitsAttribute.UnitsString + ")";
                return null;
            }
        }

    }


}
