namespace Utility
{
    using System;
    using System.Reflection;
    using Models.Core;

    /// <summary>
    /// This abstract base class encapsulates the interface for a variable from a Model.
    /// source code.
    /// </summary>
    [Serializable]
    public abstract class IVariable
    {
        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public abstract string Units { get; }

        /// <summary>
        /// If the variable is an array then returns the type of the elements in the array.
        /// Returns null if variable is not an array.
        /// </summary>
        public Type ArrayType
        {
            get
            {
                Type[] arguments = Value.GetType().GetGenericArguments();
                if (Value.GetType().GetInterface("IList") != null &&
                    arguments != null && arguments.Length > 0)
                    return arguments[0];
                else
                    return null;
            }
        }

        public virtual bool IsParameter { get { return false; } }
        public virtual bool IsState { get { return false; } }
        public virtual bool IsModel { get { return false; } }
    }

    /// <summary>
    /// This class encapsulates a single property of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    [Serializable]
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
        public override string Name
        {
            get
            {
                return Reflection.GetValueOfFieldOrProperty("Name", Obj) as string;
            }
        }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public override object Value
        {
            get
            {
                return Obj;
            }
            set
            {
                Obj = value;
            }
        }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public override string Description
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public override string Units
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
    [Serializable]
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
                throw new Exception("Cannot create an instance of class VariableField with a null model or fieldInfo");
            Obj = model;
            FieldInfo = fieldInfo;
        }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public override string Name 
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
        public override object Value 
        { 
            get 
            { 
                return FieldInfo.GetValue(Obj); 
            }
            set
            {
                FieldInfo.SetValue(Obj, value);
            }
        }


        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public override string Description
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
        public override string Units
        {
            get
            {
                Units unitsAttribute = Utility.Reflection.GetAttribute(FieldInfo, typeof(Units), false) as Units;
                if (unitsAttribute != null)
                    return "(" + unitsAttribute.UnitsString + ")";
                return null;
            }
        }

        public override bool IsState
        {
            get
            {
                bool ignoreProperty = FieldInfo.DeclaringType == typeof(Model);
                ignoreProperty |= FieldInfo.FieldType == typeof(Model);
                ignoreProperty |= FieldInfo.FieldType.IsSubclassOf(typeof(Model));
                ignoreProperty |= Utility.Reflection.GetAttribute(FieldInfo, typeof(Link), false) != null;
                ignoreProperty |= FieldInfo.Name.Contains("BackingField");
                return !ignoreProperty;
            }
        }
    }


    /// <summary>
    /// This class encapsulates a single property of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    [Serializable]
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
        public override string Name { get { return PropertyInfo.Name; } }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public override  object Value 
        { 
            get 
            { 
                return PropertyInfo.GetValue(Obj, null); 
            }
            set
            {
                PropertyInfo.SetValue(Obj, value, null);
            }
        }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public override string Description
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
        public override string Units
        {
            get
            {
                Units unitsAttribute = Utility.Reflection.GetAttribute(PropertyInfo, typeof(Units), false) as Units;
                if (unitsAttribute != null)
                    return "(" + unitsAttribute.UnitsString + ")";
                return null;
            }
        }


        public override bool IsParameter
        {
            get
            {
                Attribute XmlIgnore = Utility.Reflection.GetAttribute(PropertyInfo, typeof(System.Xml.Serialization.XmlIgnoreAttribute), true);

                MethodInfo[] accessors = PropertyInfo.GetAccessors();
                if (accessors.Length == 2 && accessors[0].IsPublic && accessors[1].IsPublic)
                {
                    bool ignoreProperty = XmlIgnore != null;                                     // No [XmlIgnore]
                    ignoreProperty |= IsModel;
                    return !ignoreProperty;
                }
                else
                    return false; // not a public property.
            }
        }

        public override bool IsModel
        {
            get
            {
                if (PropertyInfo.PropertyType.GetInterface("IList") != null && PropertyInfo.PropertyType.FullName.Contains("Models."))
                    return true;

                else if (PropertyInfo.PropertyType.Name == "Model" || PropertyInfo.PropertyType.IsSubclassOf(typeof(Model)))
                    return true;
                else
                    return false;
            }
        }


        


    }


}
