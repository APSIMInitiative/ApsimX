namespace Models.Core
{
    using System;
    using System.Reflection;
    using Models.Core;


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
                return Utility.Reflection.GetValueOfFieldOrProperty("Name", Obj) as string;
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
                DescriptionAttribute descriptionAttribute = Utility.Reflection.GetAttribute(FieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
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
                UnitsAttribute unitsAttribute = Utility.Reflection.GetAttribute(FieldInfo, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return "(" + unitsAttribute.ToString() + ")";
                return null;
            }
        }
    }
}
