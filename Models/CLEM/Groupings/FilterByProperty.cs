using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Filter using property or method (without arguments) of the IFilterable individual
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines a filter rule using properties and methods of the individual")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Version(1, 0, 0, "")]
    public class FilterByProperty : Filter, IValidatableObject
    {
        [NonSerialized]
        private PropertyInfo propertyInfo;
        private bool useSimpleApporach = false;
        private bool validOperator = true;

        /// <summary>
        /// The property or method to filter by
        /// </summary>
        [Description("Property or method")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Property or method required")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters))]
        public string PropertyOfIndividual { get; set; }
        
        private IEnumerable<string> GetParameters() => Parent?.Parameters.OrderBy(k => k);

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterByProperty()
        {
            base.SetDefaults();
        }

        ///<inheritdoc/>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Initialise();
            if (!CheckValidOperator(propertyInfo, out string errorMessage))
                throw new ApsimXException(this, errorMessage);
        }

        /// <summary>
        /// Initialise this filter by property 
        /// </summary>
        public override void Initialise()
        {
            if (PropertyOfIndividual != null && PropertyOfIndividual != "")
            {
                propertyInfo = Parent.GetProperty(PropertyOfIndividual);
                useSimpleApporach = IsSimpleRuminantProperty();
                validOperator = CheckValidOperator(propertyInfo, out string _);
            }
        }

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            if (!validOperator) return f => false;
            if (useSimpleApporach)
            {
                Func<T, bool> simple = t => {
                    var simpleFilterParam = Expression.Parameter(typeof(T));
                    var thisOperator = Operator;

                    // Try convert the Value into the same data type as the property
                    ConstantExpression simpleCompareVal = null;
                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        try
                        {
                            var testEnumOK = Enum.Parse(propertyInfo.PropertyType, Value.ToString(), true) != null;
                        }
                        catch
                        {
                            return (Operator == ExpressionType.NotEqual || Operator == ExpressionType.IsFalse);
                        }
                        simpleCompareVal = Expression.Constant(Enum.Parse(propertyInfo.PropertyType, Value.ToString()), propertyInfo.PropertyType);
                    }
                    else
                        simpleCompareVal = Expression.Constant(Convert.ChangeType(Value ?? 0, propertyInfo.PropertyType));

                    bool success = GetSimpleRuminantProperty(t, out ConstantExpression propertyValue);

                    if (!success)
                        return false;

                    BinaryExpression simpleBinary;
                    if (thisOperator == ExpressionType.IsTrue | thisOperator == ExpressionType.IsFalse)
                    {
                        // Allow for IsTrue and IsFalse operator
                        if (propertyInfo.PropertyType == typeof(bool))
                            simpleCompareVal = Expression.Constant((thisOperator == ExpressionType.IsTrue), typeof(bool));
                        simpleBinary = Expression.MakeBinary(ExpressionType.Equal, propertyValue, simpleCompareVal);
                    }
                    else
                        simpleBinary = Expression.MakeBinary(thisOperator, propertyValue, simpleCompareVal);

                    var simpleLambda = Expression.Lambda<Func<T, bool>>(simpleBinary, simpleFilterParam).Compile();
                    return simpleLambda(t);
                };
                return simple;
            }

            // Check that the filter applies to objects of type T
            var filterParam = Expression.Parameter(typeof(T));

            // check if the parameter passes can inherit the declaring type
            // this will not allow females to check male properties
            // convert parameter to the type of the property, null if fails
            var filterInherit = Expression.TypeAs(filterParam, propertyInfo.DeclaringType);
            var typeis = Expression.TypeIs(filterParam, propertyInfo.DeclaringType);

            // Look for the property
            var key = Expression.Property(filterInherit, propertyInfo.Name);

            // Try convert the Value into the same data type as the property
            var ce = propertyInfo.PropertyType.IsEnum ? Enum.Parse(propertyInfo.PropertyType, Value.ToString(), true) : Convert.ChangeType(Value??0, propertyInfo.PropertyType);
            var value = Expression.Constant(ce);

            // Create a lambda that compares the filter value to the property on T
            // using the provided operator
            Expression binary; 
            if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
            {
                // Allow for IsTrue and IsFalse operator
                ce = (Operator == ExpressionType.IsTrue);
                binary = Expression.MakeBinary(ExpressionType.Equal, key, Expression.Constant(ce));
            }
            else
                binary = Expression.MakeBinary(Operator, key, value);

            // only perfom if the type is a match to the type of property
            var body = Expression.Condition(
                typeis,
                binary,
                Expression.Constant(false)
                );

            var lambda = Expression.Lambda<Func<T, bool>>(body, filterParam).Compile();
            return lambda;
        }

        private bool IsSimpleRuminantProperty()
        {
            if (propertyInfo != null && (propertyInfo.DeclaringType == typeof(Ruminant) || propertyInfo.DeclaringType.IsSubclassOf(typeof(Ruminant))))
            {
                switch (propertyInfo.Name)
                {
                    case "Age":
                    case "Weight":
                    case "Sex":
                    case "IsWeaner":
                    case "IsSire":
                    case "IsBreeder":
                    case "IsAbleToBreed":
                    case "IsHeifer":
                    case "IsLactating":
                    case "IsPreBreeder":
                    case "IsPregnant":
                    case "IsCalf":
                    case "Breed":
                    case "Class":
                    case "EnergyBalance":
                    case "HerdName":
                    case "HighWeight":
                    case "Location":
                    case "ProportionOfHighWeight":
                    case "ProportionOfNormalisedWeight":
                    case "AdultEquivalent":
                    case "HealthScore":
                    case "ProportionOfSRW":
                    case "ReadyForSale":
                    case "RelativeCondition":
                    case "RelativeSize":
                    case "ReplacementBreeder":
                    case "SaleFlag":
                    case "Weaned":
                    case "WeightGain":
                    case "Cashmere":
                    case "Wool":
                    case "IsWildBreeder":
                    case "DaysLactating":
                    case "MonthsSinceLastBirth":
                    case "NumberOfBirths":
                    case "NumberOfBreedingMonths":
                    case "NumberOfConceptions":
                    case "NumberOfOffspring":
                    case "NumberOfWeaned":
                    case "SuccessfulPregnancy":
                        return true;
                    default:
                        return false;
                }
            }
            else if (propertyInfo != null && (propertyInfo.DeclaringType == typeof(LabourType)))
            {
                switch (propertyInfo.Name)
                {
                    case "Age":
                    case "Sex":
                    case "Hired":
                    case "Name":
                        return true;
                    default:
                        return false;

                }
            }
            return false;
        }

        private bool GetSimpleRuminantProperty(IFilterable individual, out ConstantExpression propertyValue)
        {
            propertyValue = null;
            if (!propertyInfo.DeclaringType.IsAssignableFrom(individual.GetType()))
                return false;

            if (propertyInfo != null && (propertyInfo.DeclaringType == typeof(Ruminant) || propertyInfo.DeclaringType.IsSubclassOf(typeof(Ruminant))))
            {
                switch (propertyInfo.Name)
                {
                    case "Age":
                        propertyValue = Expression.Constant((individual as Ruminant).Age, propertyInfo.PropertyType);
                        break;
                    case "Weight":
                        propertyValue = Expression.Constant((individual as Ruminant).Weight, propertyInfo.PropertyType);
                        break;
                    case "Sex":
                        propertyValue = Expression.Constant((individual as Ruminant).Sex, propertyInfo.PropertyType);
                        break;
                    case "IsWeaner":
                        propertyValue = Expression.Constant((individual as Ruminant).IsWeaner, propertyInfo.PropertyType);
                        break;
                    case "IsSire":
                        propertyValue = Expression.Constant((individual as RuminantMale).IsSire, propertyInfo.PropertyType);
                        break;
                    case "IsAbleToBreed":
                        propertyValue = Expression.Constant((individual as Ruminant).IsAbleToBreed, propertyInfo.PropertyType);
                        break;
                    case "IsHeifer":
                        propertyValue = Expression.Constant((individual as RuminantFemale).IsHeifer, propertyInfo.PropertyType);
                        break;
                    case "IsLactating":
                        propertyValue = Expression.Constant((individual as RuminantFemale).IsLactating, propertyInfo.PropertyType);
                        break;
                    case "IsPreBreeder":
                        propertyValue = Expression.Constant((individual as RuminantFemale).IsPreBreeder, propertyInfo.PropertyType);
                        break;
                    case "IsPregnant":
                        propertyValue = Expression.Constant((individual as RuminantFemale).IsPregnant, propertyInfo.PropertyType);
                        break;
                    case "IsCalf":
                        propertyValue = Expression.Constant((individual as Ruminant).IsCalf, propertyInfo.PropertyType);
                        break;
                    case "Breed":
                        propertyValue = Expression.Constant((individual as Ruminant).Breed, propertyInfo.PropertyType);
                        break;
                    case "Class":
                        propertyValue = Expression.Constant((individual as Ruminant).Class, propertyInfo.PropertyType);
                        break;
                    case "EnergyBalance":
                        propertyValue = Expression.Constant((individual as Ruminant).EnergyBalance, propertyInfo.PropertyType);
                        break;
                    case "HerdName":
                        propertyValue = Expression.Constant((individual as Ruminant).HerdName, propertyInfo.PropertyType);
                        break;
                    case "HighWeight":
                        propertyValue = Expression.Constant((individual as Ruminant).HighWeight, propertyInfo.PropertyType);
                        break;
                    case "Location":
                        propertyValue = Expression.Constant((individual as Ruminant).Location, propertyInfo.PropertyType);
                        break;
                    case "ProportionOfHighWeight":
                        propertyValue = Expression.Constant((individual as Ruminant).ProportionOfHighWeight, propertyInfo.PropertyType);
                        break;
                    case "ProportionOfNormalisedWeight":
                        propertyValue = Expression.Constant((individual as Ruminant).ProportionOfNormalisedWeight, propertyInfo.PropertyType);
                        break;
                    case "AdultEquivalent":
                        propertyValue = Expression.Constant((individual as Ruminant).AdultEquivalent, propertyInfo.PropertyType);
                        break;
                    case "HealthScore":
                        propertyValue = Expression.Constant((individual as Ruminant).HealthScore, propertyInfo.PropertyType);
                        break;
                    case "ProportionOfSRW":
                        propertyValue = Expression.Constant((individual as Ruminant).ProportionOfSRW, propertyInfo.PropertyType);
                        break;
                    case "ReadyForSale":
                        propertyValue = Expression.Constant((individual as Ruminant).ReadyForSale, propertyInfo.PropertyType);
                        break;
                    case "RelativeCondition":
                        propertyValue = Expression.Constant((individual as Ruminant).RelativeCondition, propertyInfo.PropertyType);
                        break;
                    case "RelativeSize":
                        propertyValue = Expression.Constant((individual as Ruminant).RelativeSize, propertyInfo.PropertyType);
                        break;
                    case "ReplacementBreeder":
                        propertyValue = Expression.Constant((individual as Ruminant).ReplacementBreeder, propertyInfo.PropertyType);
                        break;
                    case "SaleFlag":
                        propertyValue = Expression.Constant((individual as Ruminant).SaleFlag, propertyInfo.PropertyType);
                        break;
                    case "Weaned":
                        propertyValue = Expression.Constant((individual as Ruminant).Weaned, propertyInfo.PropertyType);
                        break;
                    case "WeightGain":
                        propertyValue = Expression.Constant((individual as Ruminant).WeightGain, propertyInfo.PropertyType);
                        break;
                    case "Cashmere":
                        propertyValue = Expression.Constant((individual as Ruminant).Cashmere, propertyInfo.PropertyType);
                        break;
                    case "Wool":
                        propertyValue = Expression.Constant((individual as Ruminant).Wool, propertyInfo.PropertyType);
                        break;
                    case "IsWildBreeder":
                        propertyValue = Expression.Constant((individual as RuminantMale).IsWildBreeder, propertyInfo.PropertyType);
                        break;
                    case "DaysLactating":
                        propertyValue = Expression.Constant((individual as RuminantFemale).DaysLactating, propertyInfo.PropertyType);
                        break;
                    case "MonthsSinceLastBirth":
                        propertyValue = Expression.Constant((individual as RuminantFemale).MonthsSinceLastBirth, propertyInfo.PropertyType);
                        break;
                    case "NumberOfBirths":
                        propertyValue = Expression.Constant((individual as RuminantFemale).NumberOfBirths, propertyInfo.PropertyType);
                        break;
                    case "NumberOfBreedingMonths":
                        propertyValue = Expression.Constant((individual as RuminantFemale).NumberOfBreedingMonths, propertyInfo.PropertyType);
                        break;
                    case "NumberOfConceptions":
                        propertyValue = Expression.Constant((individual as RuminantFemale).NumberOfConceptions, propertyInfo.PropertyType);
                        break;
                    case "NumberOfOffspring":
                        propertyValue = Expression.Constant((individual as RuminantFemale).NumberOfOffspring, propertyInfo.PropertyType);
                        break;
                    case "NumberOfWeaned":
                        propertyValue = Expression.Constant((individual as RuminantFemale).NumberOfWeaned, propertyInfo.PropertyType);
                        break;
                    case "SuccessfulPregnancy":
                        propertyValue = Expression.Constant((individual as RuminantFemale).SuccessfulPregnancy, propertyInfo.PropertyType);
                        break;
                    case "IsBreeder":
                        propertyValue = Expression.Constant((individual as RuminantFemale).IsBreeder, propertyInfo.PropertyType);
                        break;
                }
            }
            else if (propertyInfo != null && (propertyInfo.DeclaringType == typeof(LabourType)))
            {
                switch (propertyInfo.Name)
                {
                    case "Age":
                        propertyValue = Expression.Constant((individual as LabourType).Age, propertyInfo.PropertyType);
                        break;
                    case "Sex":
                        propertyValue = Expression.Constant((individual as LabourType).Sex, propertyInfo.PropertyType);
                        break;
                    case "Hired":
                        propertyValue = Expression.Constant((individual as LabourType).Hired, propertyInfo.PropertyType);
                        break;
                    case "Name":
                        propertyValue = Expression.Constant((individual as LabourType).Name, propertyInfo.PropertyType);
                        break;
                }
            }
            return (propertyValue != null);
        }

        /// <summary>
        /// Check if the specified operator is valid for the selected property
        /// </summary>
        /// <param name="property">PropertyInfo of the property</param>
        /// <param name="errorMessage">Error message returned for reporting</param>
        /// <returns>True if operator is valid</returns>
        public bool CheckValidOperator(PropertyInfo property, out string errorMessage)
        {
            errorMessage = "";
            switch (property.PropertyType.IsEnum?"enum":property.PropertyType.Name)
            {
                case "Boolean":
                case "enum":
                case "String":
                    switch (Operator)
                    {
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                            errorMessage = $"Invalid operator of type [{OperatorToSymbol()}] for [{property.PropertyType.Name}] property [{property.Name}] in [{this.NameWithParent}] ";
                            return false;
                        default:
                            break;
                    };
                    break;
                case "int":
                case "double":
                case "Single":
                    switch (Operator)
                    {
                        case ExpressionType.IsFalse:
                        case ExpressionType.IsTrue:
                            errorMessage = $"Invalid operator of type [{OperatorToSymbol()}] for [{property.PropertyType.Name}] property [{property.Name}] in [{this.NameWithParent}] ";
                            return false;
                        default:
                            break;
                    };
                    break;
                default:
                    break;
            }
            return true;
        }

        /// <summary>
        /// Convert filter to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FilterString(false);
        }

        /// <summary>
        /// Convert filter to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            return FilterString(true);
        }

        private string FilterString(bool htmltags)
        {
            Initialise();

            using (StringWriter filterWriter = new StringWriter())
            {
                if (propertyInfo is null)
                {
                    filterWriter.Write($"Filter:");
                    string errorlink = (htmltags) ? "<span class=\"errorlink\">" : "";
                    string spanclose = (htmltags) ? "</span>" : "";
                    string message = (PropertyOfIndividual == null || PropertyOfIndividual == "") ? "Not Set" : $"Unknown: {PropertyOfIndividual}";
                    filterWriter.Write($"{errorlink}{message}{spanclose}");
                    return filterWriter.ToString();
                }

                filterWriter.Write($"Filter:");
                bool truefalse = IsOperatorTrueFalseTest();
                if (truefalse | (propertyInfo != null && propertyInfo.PropertyType.IsEnum))
                {
                    if(propertyInfo.PropertyType == typeof(bool))
                    {
                        if (Operator == ExpressionType.IsFalse || Value?.ToString().ToLower() == "false")
                            filterWriter.Write(" not");
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                    }
                    else
                    {
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                        if (validOperator)
                            filterWriter.Write((Operator == ExpressionType.IsFalse || Value?.ToString().ToLower() == "false") ? " not" : " is");
                        else
                        {
                            string errorlink = (htmltags) ? "<span class=\"errorlink\">" : "";
                            string spanclose = (htmltags) ? "</span>" : "";
                            filterWriter.Write($"{errorlink}{OperatorToSymbol()} is invalid for property {propertyInfo.PropertyType.Name}{spanclose}");
                        }
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                    }
                }
                else
                {
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");

                    if (propertyInfo != null)
                    {
                        if (validOperator)
                            filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                        else
                        {
                            string errorlink = (htmltags) ? "<span class=\"errorlink\">" : "";
                            string spanclose = (htmltags) ? "</span>" : "";
                            filterWriter.Write($"{errorlink}{OperatorToSymbol()} is invalid for property {propertyInfo.PropertyType.Name}{spanclose}");
                        }
                    }
                    else
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");

                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                }
                return filterWriter.ToString();
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if((Value is null || Value.ToString() == "") & !(Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse))
            {
                string[] memberNames = new string[] { "Missing filter compare value" };
                results.Add(new ValidationResult($"A value to compare with the Property is required for [f={Name}] in [f={Parent.Name}]", memberNames));
            }

            // check valid operator
            if(!CheckValidOperator(propertyInfo, out _))
            {
                string[] memberNames = new string[] { "Invalid operator" };
                results.Add(new ValidationResult($"The operator provided for [f={Name}] in [f={Parent.Name}] is not valid for the property type [{propertyInfo.Name}]", memberNames));
            }

            // check valid property value.
            // valid for enum
            if (propertyInfo.PropertyType.IsEnum)
            {
                try
                {
                    Enum.Parse(propertyInfo.PropertyType, Value.ToString());
                }
                catch
                {
                    string[] memberNames = new string[] { "Invalid compare value" };
                    results.Add(new ValidationResult($"The value to compare [{Value}] provided for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] is not valid for the property type [{propertyInfo.Name}]{System.Environment.NewLine}Valid entries are [{String.Join(",", Enum.GetNames(propertyInfo.PropertyType))}]", memberNames));
                }
            }

            // valid for true / false bool
            if (propertyInfo.PropertyType == typeof(bool))
            {
                // blank entry is permitted if using isTrue or isFalse otherwise check value
                if (!(Value is null & (Operator == ExpressionType.IsTrue || Operator == ExpressionType.IsFalse)))
                {
                    try
                    {
                        Boolean.Parse(Value.ToString());
                    }
                    catch
                    {
                        string[] memberNames = new string[] { "Invalid compare value" };
                        results.Add(new ValidationResult($"The value to compare [{Value}] provided for [f={Name}] in [f={Parent.Name}] is not valid for the property type [Boolean]{System.Environment.NewLine}Valid entries are [True, true, False, false, 1, 0]", memberNames));
                    }
                }
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return $"<div class=\"filter\" style=\"opacity: {((Enabled) ? "1" : "0.4")}\">{ToHTMLString()}</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion
    }

}
