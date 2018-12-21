using System;
using System.Collections.Generic;
using Models.Core;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;

namespace Models.CLEM.Groupings
{
    /// <summary>
    /// Herd list extensions
    /// </summary>
    public static class ListFilterExtensions
    {
        /// <summary>
        /// Filter extensions for labour
        /// </summary>
        public static List<LabourType> Filter(this IEnumerable<LabourType> individuals, Model filterGroup)
        {
            var rules = new List<Rule>();
            foreach (LabourFilter filter in Apsim.Children(filterGroup, typeof(LabourFilter)))
            {
                ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
                // create rule list
                rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
            }

            var compiledRulesList = CompileRule(new List<LabourType>(), rules);
            return GetItemsThatMatchAll<LabourType>(individuals, compiledRulesList).ToList<LabourType>();
        }

        /// <summary>
        /// Filter extensions for herd list
        /// </summary>
        public static List<Ruminant> Filter(this IEnumerable<Ruminant> individuals, Model filterGroup)
        {
            bool femaleProperties = false;
            bool maleProperties = false;
            var rules = new List<Rule>();
            foreach (RuminantFilter filter in Apsim.Children(filterGroup, typeof(RuminantFilter)))
            {
                ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
                // create rule list
                string gender = "";
                switch (filter.Parameter)
                {
                    case RuminantFilterParameters.Draught:
                    case RuminantFilterParameters.BreedingSire:
                        maleProperties = true;
                        gender = "Male";
                        break;
                    case RuminantFilterParameters.IsPregnant:
                    case RuminantFilterParameters.IsLactating:
                    case RuminantFilterParameters.IsHeifer:
                        femaleProperties = true;
                        gender = "Female";
                        break;
                    default:
                        break;
                }
                if(gender != "")
                {
                    RuminantFilter fltr = new RuminantFilter()
                    {
                        Parameter = RuminantFilterParameters.Gender,
                        Operator = FilterOperators.Equal,
                        Value = gender
                    };
                    rules.Add(new Rule(fltr.Parameter.ToString(), (ExpressionType)Enum.Parse(typeof(ExpressionType), fltr.Operator.ToString()), fltr.Value));
                }
                rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
            }

            if (femaleProperties && maleProperties)
            {
                return new List<Ruminant>();
            }
            else
            {
                if (femaleProperties)
                {
                    var compiledRulesList = CompileRule(new List<RuminantFemale>(), rules);
                    return GetItemsThatMatchAll<RuminantFemale>(individuals.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>(), compiledRulesList).ToList<Ruminant>();
                }
                else if (maleProperties)
                {
                    var compiledRulesList = CompileRule(new List<RuminantMale>(), rules);
                    return GetItemsThatMatchAll<RuminantMale>(individuals.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>(), compiledRulesList).ToList<Ruminant>();
                }
                else
                {
                    var compiledRulesList = CompileRule(new List<Ruminant>(), rules);
                    return GetItemsThatMatchAll<Ruminant>(individuals, compiledRulesList).ToList<Ruminant>();
                }
            }
        }

        /// <summary>
        /// Filter extensions for other animals cohort list
        /// </summary>
        public static List<OtherAnimalsTypeCohort> Filter(this IEnumerable<OtherAnimalsTypeCohort> individuals, Model filterGroup)
        {
            var rules = new List<Rule>();
            foreach (OtherAnimalsFilter filter in Apsim.Children(filterGroup, typeof(OtherAnimalsFilter)))
            {
                ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
                // create rule list
                rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
            }

            var compiledRulesList = CompileRule(new List<OtherAnimalsTypeCohort>(), rules);
            return GetItemsThatMatchAll<OtherAnimalsTypeCohort>(individuals, compiledRulesList).ToList<OtherAnimalsTypeCohort>();
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

        private static IEnumerable<T> GetItemsThatMatchAny<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
        {
            return source.Where(t => predicates.Any(predicate => predicate(t)));
        }

        private static IEnumerable<T> GetItemsThatMatchAll<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
        {
            return source.Where(t => predicates.All(predicate => predicate(t)));
        }

        private static List<Func<T, bool>> CompileRule<T>(List<T> targetEntity, List<Rule> rules)
        {
            // Credit for this function goes to Cole Francis, Architect
            // The pre-compiled rules type
            // https://mobiusstraits.com/2015/08/12/expression-trees/

            var compiledRules = new List<Func<T, bool>>();

            // Loop through the rules and compile them against the properties of the supplied shallow object 
            rules.ForEach(rule =>
            {
                var genericType = Expression.Parameter(typeof(T));
                var key = Expression.Property(genericType, rule.ComparisonPredicate);
                var propertyType = typeof(T).GetProperty(rule.ComparisonPredicate).PropertyType;
                object ce;
                if (propertyType.BaseType.Name == "Enum")
                {
                    ce = Enum.Parse(propertyType, rule.ComparisonValue, true);
                }
                else
                {
                    ce = Convert.ChangeType(rule.ComparisonValue, propertyType);
                }
                var value = Expression.Constant(ce);
                var binaryExpression = Expression.MakeBinary(rule.ComparisonOperator, key, value);

                compiledRules.Add(Expression.Lambda<Func<T, bool>>(binaryExpression, genericType).Compile());
            });

            // Return the compiled rules to the caller
            return compiledRules;
        }

    }
}
