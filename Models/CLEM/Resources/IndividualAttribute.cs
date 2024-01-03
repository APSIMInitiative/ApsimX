using Models.CLEM.Interfaces;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A ruminant attribute that stores an associated object
    /// </summary>
    [Serializable]
    public class IndividualAttribute : IIndividualAttribute
    {
        /// <summary>
        /// Value object of attribute
        /// </summary>
        public object StoredValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        public object StoredMateValue { get; set; }

        /// <summary>
        /// The style of inheritance of the attribute
        /// </summary>
        public AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// The variability (s.d.) for Mendelian inheritance of attribute
        /// </summary>
        public ISetAttribute SetAttributeSettings { get; set; }

        /// <summary>
        /// Value as a float
        /// </summary>
        public float Value
        {
            get
            {
                if (float.TryParse((StoredValue ?? -9999).ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
                    return val;
                else
                    return -9999;
            }
        }

        /// <summary>
        /// Mate's Value as a float
        /// </summary>
        public float MateValue
        {
            get
            {
                if (float.TryParse((StoredMateValue ?? -9999).ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
                    return val;
                else
                    return -9999;
            }
        }

        /// <summary>
        /// Get the attribute inherited by an offspring given both parent attribute values stored for a breeder
        /// </summary>
        /// <returns>A ruminant attribute to supply the offspring</returns>
        public object GetInheritedAttribute()
        {
            IndividualAttribute newAttribute = new IndividualAttribute()
            {
                InheritanceStyle = this.InheritanceStyle,
                SetAttributeSettings = this.SetAttributeSettings
            };

            switch (InheritanceStyle)
            {
                case AttributeInheritanceStyle.None:
                    return null;
                case AttributeInheritanceStyle.Maternal:
                    newAttribute.StoredValue = StoredValue;
                    break;
                case AttributeInheritanceStyle.Paternal:
                    newAttribute.StoredValue = StoredMateValue;
                    break;
                case AttributeInheritanceStyle.LeastParentValue:
                    if (this.Value <= this.MateValue)
                        newAttribute.StoredValue = StoredValue;
                    else
                        newAttribute.StoredValue = StoredMateValue;
                    break;
                case AttributeInheritanceStyle.GreatestParentValue:
                    if (this.Value >= this.MateValue)
                        newAttribute.StoredValue = StoredValue;
                    else
                        newAttribute.StoredValue = StoredMateValue;
                    break;
                case AttributeInheritanceStyle.LeastBothParents:
                    if (StoredValue != null & StoredMateValue != null)
                    {
                        if (this.Value <= this.MateValue)
                            newAttribute.StoredValue = StoredValue;
                        else
                            newAttribute.StoredValue = StoredMateValue;
                    }
                    else
                        return null;
                    break;
                case AttributeInheritanceStyle.GreatestBothParents:
                    if (StoredValue != null & StoredValue != null)
                    {
                        if (Value >= MateValue)
                            newAttribute.StoredValue = StoredValue;
                        else
                            newAttribute.StoredValue = StoredMateValue;
                    }
                    else
                        return null;
                    break;
                case AttributeInheritanceStyle.MeanValueZeroAbsent:
                    float offSpringValue = 0;
                    if (StoredValue != null)
                        offSpringValue += Value;

                    if (StoredMateValue != null)
                        offSpringValue += MateValue;

                    newAttribute.StoredValue = (offSpringValue / 2.0f);
                    break;
                case AttributeInheritanceStyle.MeanValueIgnoreAbsent:
                    offSpringValue = 0;
                    int cnt = 0;
                    if (StoredValue != null)
                    {
                        offSpringValue += Value;
                        cnt++;
                    }
                    if (StoredMateValue != null)
                    {
                        offSpringValue += MateValue;
                        cnt++;
                    }
                    newAttribute.StoredValue = StoredValue = (offSpringValue / (float)cnt);
                    break;
                case AttributeInheritanceStyle.AsGeneticTrait:
                    throw new NotImplementedException();
                default:
                    return null;
            }
            // Apply mendelian variability from the SetAttributeWithValue component using the standard deviation provided from the mother.
            if(SetAttributeSettings is not null && SetAttributeSettings is SetAttributeWithValue)
            {
                newAttribute.StoredValue = (SetAttributeSettings as SetAttributeWithValue).ApplyVariabilityToAttributeValue(Value, true, false);
            }
            return newAttribute;
        }
    }

}
