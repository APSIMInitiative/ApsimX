using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Interface for all resource attributes
    /// </summary>
    public interface IIndividualAttribute
    {
        /// <summary>
        /// The value of the attribute
        /// </summary>
        object StoredValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        object StoredMateValue { get; set; }

        /// <summary>
        /// The style for inheritance of attribute
        /// </summary>
        AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Creates an attribute of parent type and returns for new offspring
        /// </summary>
        /// <returns>A new attribute inherited from parents</returns>
        object GetInheritedAttribute();
    }

    /// <summary>
    /// A ruminant attribute that stores an associated object
    /// </summary>
    [Serializable]
    public class IndividualAttribute: IIndividualAttribute
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
        /// Value as a float
        /// </summary>
        public float Value
        {
            get
            {
                if(float.TryParse((StoredValue??-9999).ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
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
                if (float.TryParse((StoredMateValue??-9999).ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
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
                InheritanceStyle = this.InheritanceStyle
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
            return newAttribute;
        }
    }

    /// <summary>
    /// A ruminant attribute that stores an associated geneotype
    /// </summary>
    public class CLEMGenotypeAttribute : IIndividualAttribute
    {
        /// <summary>
        /// Value object of attribute
        /// </summary>
        public object StoredValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        public  object StoredMateValue { get; set; }

        /// <summary>
        /// The style of inheritance of the attribute
        /// </summary>
        public AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Value as a string (e.g Bb)
        /// </summary>
        public string Value
        {
            get
            {
                return StoredValue.ToString();
            }
        }

        /// <summary>
        /// Value as string from mate recorded by breeder
        /// </summary>
        public string MateValue
        {
            get
            {
                return StoredMateValue.ToString();
            }
        }

        /// <summary>
        /// Get the attribute inherited by an offspring given both parent attribute values stored for a breeder
        /// </summary>
        /// <returns>A ruminant attribute to supply the offspring</returns>
        public object GetInheritedAttribute()
        {
            CLEMGenotypeAttribute newAttribute = new CLEMGenotypeAttribute()
            {
                InheritanceStyle = this.InheritanceStyle
            };

            throw new NotImplementedException("Inheritance of Genotype attributes has not been implemented");
        }
    }

}
