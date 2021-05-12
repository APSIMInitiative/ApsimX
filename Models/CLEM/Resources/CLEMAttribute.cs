using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Interface for all resource attributes
    /// </summary>
    public interface ICLEMAttribute
    {
        /// <summary>
        /// The value of the attribute
        /// </summary>
        object storedValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        object storedMateValue { get; set; }

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
    public class CLEMAttribute: ICLEMAttribute
    {
        /// <summary>
        /// Value object of attribute
        /// </summary>
        public object storedValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        public object storedMateValue { get; set; }

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
                if(float.TryParse(storedValue.ToString(), out float val))
                {
                    return val;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Mate's Value as a float
        /// </summary>
        public float MateValue
        {
            get
            {
                if (float.TryParse(storedValue.ToString(), out float val))
                {
                    return val;
                }
                else
                {
                    return 0;
                }
            }
        }


        /// <summary>
        /// Get the attribute inherited by an offspring given both parent attribute values stored for a breeder
        /// </summary>
        /// <returns>A ruminant attribute to supply the offspring</returns>
        public object GetInheritedAttribute()
        {
            CLEMAttribute newAttribute = new CLEMAttribute()
            {
                InheritanceStyle = this.InheritanceStyle
            };

            switch (InheritanceStyle)
            {
                case AttributeInheritanceStyle.None:
                    return null;
                case AttributeInheritanceStyle.Maternal:
                    newAttribute.storedValue = storedValue;
                    break;
                case AttributeInheritanceStyle.Paternal:
                    newAttribute.storedValue = storedMateValue;
                    break;
                case AttributeInheritanceStyle.LeastParentValue:
                    if (this.Value <= this.MateValue)
                    {
                        newAttribute.storedValue = storedValue;
                    }
                    else
                    {
                        newAttribute.storedValue = storedMateValue;
                    }
                    break;
                case AttributeInheritanceStyle.GreatestParentValue:
                    if (this.Value >= this.MateValue)
                    {
                        newAttribute.storedValue = storedValue;
                    }
                    else
                    {
                        newAttribute.storedValue = storedMateValue;
                    }
                    break;
                case AttributeInheritanceStyle.LeastBothParents:
                    if (storedValue != null & storedMateValue != null)
                    {
                        if (this.Value <= this.MateValue)
                        {
                            newAttribute.storedValue = storedValue;
                        }
                        else
                        {
                            newAttribute.storedValue = storedMateValue;
                        }
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case AttributeInheritanceStyle.GreatestBothParents:
                    if (storedValue != null & storedValue != null)
                    {
                        if (Value >= MateValue)
                        {
                            newAttribute.storedValue = storedValue;
                        }
                        else
                        {
                            newAttribute.storedValue = storedMateValue;
                        }
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case AttributeInheritanceStyle.MeanValueZeroAbsent:
                    float offSpringValue = 0;
                    if (storedValue != null)
                    {
                        offSpringValue += Value;
                    }
                    if (storedMateValue != null)
                    {
                        offSpringValue += MateValue;
                    }
                    newAttribute.storedValue = (offSpringValue / 2.0f);
                    break;
                case AttributeInheritanceStyle.MeanValueIgnoreAbsent:
                    offSpringValue = 0;
                    int cnt = 0;
                    if (storedValue != null)
                    {
                        offSpringValue += Value;
                        cnt++;
                    }
                    if (storedMateValue != null)
                    {
                        offSpringValue += MateValue;
                        cnt++;
                    }
                    newAttribute.storedValue = storedValue = (offSpringValue / (float)cnt);
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
    public class CLEMGenotypeAttribute : ICLEMAttribute
    {
        /// <summary>
        /// Value object of attribute
        /// </summary>
        public object storedValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        public  object storedMateValue { get; set; }

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
                return storedValue.ToString();
            }
        }

        /// <summary>
        /// Value as string from mate recorded by breeder
        /// </summary>
        public string MateValue
        {
            get
            {
                return storedMateValue.ToString();
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
