using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    #region Filter parameters
    /// <summary>
    /// 
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// 
        /// </summary>
        Ruminant,
        
        /// <summary>
        /// 
        /// </summary>
        Labour,
        
        /// <summary>
        /// 
        /// </summary>
        Other
    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum RuminantFilterParameters
    {
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age = 3,
        /// <summary>
        /// Breed of ruminant
        /// </summary>
        Breed = 0,
        /// <summary>
        /// Is male breeding sire
        /// </summary>
        IsSire = 15,
        /// <summary>
        /// Is male draught individual
        /// </summary>
        IsDraught = 14,
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender = 2,
        /// <summary>
        /// Herd individuals belong to
        /// </summary>
        HerdName = 1,
        /// <summary>
        /// ID of individuals
        /// </summary>
        ID = 4,
        /// <summary>
        /// Determines if within breeding ages
        /// </summary>
        IsBreedingCondition = 15,
        /// <summary>
        /// Determines if within breeding ages
        /// </summary>
        IsBreeder = 17,
        /// <summary>
        /// Identified as a replacement breeder growing up
        /// </summary>
        ReplacementBreeder = 18,
        /// <summary>
        /// Is female a pre-breeder (weaned, less than set age, up to first birth)
        /// </summary>
        IsPreBreeder = 13,
        /// <summary>
        /// Is female lactating
        /// </summary>
        IsLactating = 11,
        /// <summary>
        /// Is female pregnant
        /// </summary>
        IsPregnant = 12,
        /// <summary>
        /// Current grazing location
        /// </summary>
        Location = 8,
        /// <summary>
        /// The number of months since last birth for a breeder
        /// </summary>
        MonthsSinceLastBirth = 16,
        /// <summary>
        /// Weight as proportion of High weight achieved
        /// </summary>
        ProportionOfHighWeight = 6,
        /// <summary>
        /// Weight as proportion of Standard Reference Weight
        /// </summary>
        ProportionOfSRW = 7,
        /// <summary>
        /// Weaned status
        /// </summary>
        Weaned = 9,
        /// <summary>
        /// Is individual a weaner (weaned, but less than 12 months)
        /// </summary>
        IsWeaner = 10,
        /// <summary>
        /// Weight of individuals
        /// </summary>
        Weight = 5,
        /// <summary>
        /// HealthScore
        /// </summary>
        HealthScore = 6,
        /// <summary>
        /// Is individual a calf (not weaned)
        /// </summary>
        IsCalf = 18,
        /// <summary>
        /// Is individual castrated
        /// </summary>
        IsCastrate = 30,
        /// <summary>
        /// Stage cateogry
        /// </summary>
        Category = 40,
    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum LabourFilterParameters
    {
        /// <summary>
        /// Name of individual
        /// </summary>
        Name,
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender,
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age,
        /// <summary>
        /// Is hired labour
        /// </summary>
        Hired
    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum OtherAnimalsFilterParameters
    {
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender,
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age
    }
    #endregion

    ///<summary>
    /// Individual filter term for ruminant group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Description("An individual filter for a filter group. Multiple filters are additive.")]
    [Version(1, 0, 1, "")]
    public class Filter : CLEMModel
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("Filter type")]
        public FilterType Type { get; set; }

        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Parameter to filter by")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters))]
        public string ParameterName { get; set; }

        private string[] GetParameters()
        {
            switch (Type)
            {
                case FilterType.Ruminant:
                    return Enum.GetNames(typeof(RuminantFilterParameters));

                case FilterType.Labour:
                    return Enum.GetNames(typeof(LabourFilterParameters));

                case FilterType.Other:
                default:
                    return Enum.GetNames(typeof(OtherAnimalsFilterParameters));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public object Parameter
        {
            get
            {
                switch (Type)
                {
                    case FilterType.Ruminant:
                        return Enum.Parse(typeof(RuminantFilterParameters), ParameterName);

                    case FilterType.Labour:
                        return Enum.Parse(typeof(LabourFilterParameters), ParameterName);

                    case FilterType.Other:
                    default:
                        return Enum.Parse(typeof(OtherAnimalsFilterParameters), ParameterName);
                }
            }
            set
            {
                /* FOR CONVERTING OLD FILES - ONLY ENABLE IF READING FOR FIRST TIME*/
                //if (ParameterName is null)
                //{
                //    switch (Type)
                //    {
                //        case FilterType.Ruminant:
                //            ParameterName = Enum.Parse(typeof(RuminantFilterParameters), value.ToString()).ToString();
                //            return;

                //        case FilterType.Labour:
                //            ParameterName = Enum.Parse(typeof(LabourFilterParameters), value.ToString()).ToString();
                //            return;

                //        case FilterType.Other:
                //        default:
                //            ParameterName = Enum.Parse(typeof(OtherAnimalsFilterParameters), value.ToString()).ToString();
                //            return;
                //    }
                //}

                if (Enum.IsDefined(typeof(RuminantFilterParameters), value))
                    Type = FilterType.Ruminant;
                else if (Enum.IsDefined(typeof(LabourFilterParameters), value))
                    Type = FilterType.Labour;
                else if (Enum.IsDefined(typeof(OtherAnimalsFilterParameters), value))
                    Type = FilterType.Other;
                else
                    throw new Exception("Given object is not a recognised filter parameter");
                
                ParameterName = value.ToString();
            }
        }

        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        public FilterOperators Operator { get; set; }

        /// <summary>
        /// Value to check for filter
        /// </summary>
        [Description("Value to filter by")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value to filter by required")]
        public string Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString() => $"{Parameter}{Operator.ToSymbol()}{Value}";

        /// <inheritdoc/>
        public Func<T, bool> CompileRule<T>()
        {
            // Credit for this function goes to Cole Francis, Architect
            // The pre-compiled rules type
            // https://mobiusstraits.com/2015/08/12/expression-trees/

            var rule = ToRule();
            
            var genericType = Expression.Parameter(typeof(T));
            var key = Expression.Property(genericType, rule.ComparisonPredicate);
            var propertyType = typeof(T).GetProperty(rule.ComparisonPredicate).PropertyType;

            object ce = propertyType.BaseType.Name == "Enum"
                ? Enum.Parse(propertyType, rule.ComparisonValue, true)
                : Convert.ChangeType(rule.ComparisonValue, propertyType);

            var value = Expression.Constant(ce);
            var binaryExpression = Expression.MakeBinary(rule.ComparisonOperator, key, value);

            return Expression.Lambda<Func<T, bool>>(binaryExpression, genericType).Compile();
        }

        private Rule ToRule()
        {
            ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), Operator.ToString());
            // create rule list
            return new Rule(Parameter.ToString(), op, Value);
        }

        private class Rule
        {
            public string ComparisonPredicate { get; set; }
            public System.Linq.Expressions.ExpressionType ComparisonOperator { get; set; }
            public string ComparisonValue { get; set; }

            public Rule(string comparisonPredicate, System.Linq.Expressions.ExpressionType comparisonOperator, string comparisonValue)
            {
                ComparisonPredicate = comparisonPredicate;
                ComparisonOperator = comparisonOperator;
                ComparisonValue = comparisonValue;
            }
        }
    }

}
