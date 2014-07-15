// -----------------------------------------------------------------------
// <copyright file="VariableProperty.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Models.Soils;

    /// <summary>
    /// Encapsulates a discovered property of a model. Provides properties for
    /// returning information about the property. 
    /// </summary>
    [Serializable]
    public class VariableProperty : IVariable
    {
        /// <summary>
        /// Gets or sets the underlying model that this property belongs to.
        /// </summary>
        private object model;

        /// <summary>
        /// Gets or sets the PropertyInfo for this property.
        /// </summary>
        private PropertyInfo property;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableProperty" /> class.
        /// </summary>
        /// <param name="model">The underlying model for the property</param>
        /// <param name="property">The PropertyInfo for this property</param>
        public VariableProperty(object model, PropertyInfo property)
        {
            if (model == null || property == null)
            {
                throw new ApsimXException(string.Empty, "Cannot create an instance of class VariableProperty with a null model or propertyInfo");
            }
            
            this.model = model;
            this.property = property;
        }
        
        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public override string Name 
        { 
            get 
            {
                return this.property.Name; 
            } 
        }

        /// <summary>
        /// Gets the description of the property
        /// </summary>
        public override string Description
        {
            get
            {
                Description descriptionAttribute = Utility.Reflection.GetAttribute(this.property, typeof(Description), false) as Description;
                if (descriptionAttribute == null)
                {
                    return null;
                }

                if (this.model is SoilCrop)
                {
                    return (this.model as SoilCrop).Name + " " + descriptionAttribute.ToString();
                }

                return descriptionAttribute.ToString();
            }
        }

        /// <summary>
        /// Gets the units of the property
        /// </summary>
        public override string Units
        {
            get
            {
                // Get units from property
                string unitString = null;
                Units unitsAttribute = Utility.Reflection.GetAttribute(this.property, typeof(Units), false) as Units;
                PropertyInfo unitsInfo = this.model.GetType().GetProperty(this.property.Name + "Units");
                MethodInfo unitsToStringInfo = this.model.GetType().GetMethod(this.property.Name + "UnitsToString");
                if (unitsAttribute != null)
                {
                    unitString = unitsAttribute.UnitsString;
                }
                else if (unitsToStringInfo != null)
                {
                    unitString = (string)unitsToStringInfo.Invoke(this.model, new object[] { null });
                }

                return unitString;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the property is readonly.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                if (!this.property.CanWrite)
                {
                    return true;
                }

                if (this.Metadata.Contains("Estimated") || this.Metadata.Contains("Calculated"))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the metadata for each layer. Returns new string[0] if none available.
        /// </summary>
        public string[] Metadata
        {
            get
            {
                PropertyInfo metadataInfo = this.model.GetType().GetProperty(this.property.Name + "Metadata");
                if (metadataInfo != null)
                {
                    string[] metadata = metadataInfo.GetValue(this.model, null) as string[];
                    if (metadata != null)
                    {
                        return metadata;
                    }
                }

                return new string[0];
            }
        }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public Type DataType
        {
            get
            {
                return this.property.PropertyType;
            }
        }

        /// <summary>
        /// Gets the values of the property
        /// </summary>
        public override object Value
        {
            get
            {
                return this.property.GetValue(this.model, null);
            }

            set
            {
                this.property.SetValue(this.model, value, null);
            }
        }

        /// <summary>
        /// Gets the display format for this property e.g. 'N3'
        /// </summary>
        public string Format
        {
            get
            {
                DisplayFormat displayFormatAttribute = Utility.Reflection.GetAttribute(this.property, typeof(DisplayFormat), false) as DisplayFormat;
                if (displayFormatAttribute != null)
                {
                    return displayFormatAttribute.Format;
                }

                return "N3";
            }
        }

        /// <summary>
        /// Gets the crop name of the property or null if this property isn't a crop one.
        /// </summary>
        public string CropName
        {
            get
            {
                if (this.model is SoilCrop)
                {
                    return (this.model as SoilCrop).Name;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the sum of all values in this array property if the property has been 
        /// marked as [DisplayTotal]. Otherwise return double.Nan
        /// </summary>
        public double Total
        {
            get
            {
                bool hasDisplayTotal = this.property.IsDefined(typeof(DisplayTotal), false);
                if (hasDisplayTotal)
                {
                    double sum = 0.0;
                    foreach (double doubleValue in this.Value as IEnumerable<double>)
                    {
                        sum += doubleValue;
                    }

                    return sum;
                }

                return double.NaN;
            }
        }
    }
}
