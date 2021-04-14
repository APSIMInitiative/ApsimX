using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A component to specify an attribute to be applied to initial herd/individual cohort
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantInitialCohorts))]
    [ValidParent(ParentType = typeof(RuminantTypeCohort))]
    [Description("This component defines an attribute for the herd")]
    [HelpUri(@"Content/Features/Resources/SetRuminantAttributeByValue.htm")]
    [Version(1, 0, 1, "")]
    public class SetRuminantAttributeWithValue : CLEMModel, IValidatableObject, ISetRuminantAttribute
    {
        /// <summary>
        /// Name of attribute
        /// </summary>
        [Description("Name of attribute")]
        [Required(AllowEmptyStrings = false)]
        public string AttributeName { get; set; }

        /// <summary>
        /// Attribute value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Value of attribute")]
        [GreaterThanEqual("Value", ErrorMessage = "Value must be greater than or equal to minimum value")]
        [Required]
        public float Value { get; set; }

        /// <summary>
        /// Minumum of value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Minumum value")]
        [Required]
        public float MinimumValue { get; set; }

        /// <summary>
        /// Minumum of value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(100)]
        [Description("Maximum value")]
        [Required]
        [GreaterThanEqual("Value", ErrorMessage ="Maximum value must be greater than or equal to value")]
        public float MaximumValue { get; set; }

        /// <summary>
        /// Standard deviation as spread when applied to population
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Standard deviation of individuals")]
        [Required]
        public float StandardDeviation { get; set; }

        /// <summary>
        /// Inheritance style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Style of inheritance")]
        [Required]
        public RuminantAttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Mandatory attribute
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Mandatory attribute")]
        [Required]
        public bool Mandatory { get; set; }

        /// <summary>
        /// Get a random realisation of the set value based on Value and Standard deviation 
        /// </summary>
        public RuminantAttribute GetRandomSetAttribute
        {
            get
            {
                double value = Value;
                double randStdNormal = 0;

                if (StandardDeviation > 0)
                {
                    double u1 = RandomNumberGenerator.Generator.NextDouble();
                    double u2 = RandomNumberGenerator.Generator.NextDouble();
                    randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                    Math.Sin(2.0 * Math.PI * u2);
                }
                value = (float)Math.Min(MaximumValue, Math.Max(MinimumValue, value + StandardDeviation * randStdNormal));

                return new RuminantAttribute()
                {
                    InheritanceStyle = InheritanceStyle,
                    storedValue = value
                };
            }
        }

        /// <summary>
        /// Get the attribute inherited by an offspring given parent attrubutes
        /// </summary>
        /// <param name="Mother">Mother's attribute</param>
        /// <param name="Father">Father's attribute</param>
        /// <returns>A ruminant attribute to supply the offspring</returns>
        public static RuminantAttribute GetInheritedValue(RuminantAttributeInheritanceStyle Style, RuminantAttribute Mother, RuminantAttribute Father)
        {
            switch (Style)
            {
                case RuminantAttributeInheritanceStyle.None:
                    return null;
                case RuminantAttributeInheritanceStyle.Maternal:
                    return Mother;
                case RuminantAttributeInheritanceStyle.Paternal:
                    return Father;
                case RuminantAttributeInheritanceStyle.LeastParentValue:
                    if (Mother.Value <= Father.Value)
                    {
                        return Mother;
                    }
                    else
                    {
                        return Father;
                    }
                    break;
                case RuminantAttributeInheritanceStyle.GreatestParentValue:
                    if (Mother.Value >= Father.Value)
                    {
                        return Mother;
                    }
                    else
                    {
                        return Father;
                    }
                    break;
                case RuminantAttributeInheritanceStyle.LeastBothParents:
                    if (Mother != null & Father != null)
                    {
                        if (Mother.Value <= Father.Value)
                        {
                            return Mother;
                        }
                        else
                        {
                            return Father;
                        }
                    }
                    else
                        return null;
                case RuminantAttributeInheritanceStyle.GreatestBothParents:
                    if (Mother != null & Father != null)
                    {
                        if (Mother.Value >= Father.Value)
                        {
                            return Mother;
                        }
                        else
                        {
                            return Father;
                        }
                    }
                    else
                        return null;
                    break;
                case RuminantAttributeInheritanceStyle.MeanValueZeroAbsent:
                    float offSpringValue = 0;
                    if (Mother != null)
                    {
                        offSpringValue += Mother.Value;
                    }
                    if (Father != null)
                    {
                        offSpringValue += Father.Value;
                    }
                    return new RuminantAttribute()
                    {
                        InheritanceStyle = RuminantAttributeInheritanceStyle.MeanValueZeroAbsent,
                        storedValue = (offSpringValue / 2.0f)
                    };
                case RuminantAttributeInheritanceStyle.MeanValueIgnoreAbsent:
                    offSpringValue = 0;
                    int cnt = 0;
                    if (Mother != null)
                    {
                        offSpringValue += Mother.Value;
                        cnt++;
                    }
                    if (Father != null)
                    {
                        offSpringValue += Father.Value;
                        cnt++;
                    }
                    return new RuminantAttribute()
                    {
                        InheritanceStyle = RuminantAttributeInheritanceStyle.MeanValueIgnoreAbsent,
                        storedValue = (offSpringValue / (float)cnt)
                    };
                    break;
                case RuminantAttributeInheritanceStyle.AsGeneticTrait:
                    throw new NotImplementedException();
                default:
                    return null;
            }
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }
        #endregion
    }
}
