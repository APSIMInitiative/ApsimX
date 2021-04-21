using System;
using System.Collections.Generic;
using Models.Core;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Groupings
{   
    /// <summary>
    /// Herd list extensions
    /// </summary>
    public static class ListFilterExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private static Rule ToRule(this IFilter filter, string parameter)
        {
            ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
            // create rule list
            return new Rule(parameter, op, filter.Value);
        }

        /// <summary>
        /// Filter extensions for labour
        /// </summary>
        public static List<LabourType> Filter(this IEnumerable<LabourType> individuals, IModel model)
        {
            var rules = model.FindAllChildren<LabourFilter>().Select(f => f.ToRule(f.Parameter.ToString()));

            var combined = (model as IFilterGroup)?.CombinedRules ?? CompileRule(new List<LabourType>(), rules);

            return GetItemsThatMatchAll(individuals, combined as List<Func<LabourType, bool>>).ToList();
        }

        /// <summary>
        /// Filter extensions for herd list
        /// </summary>
        public static IEnumerable<Ruminant> Filter(this IEnumerable<Ruminant> individuals, IFilterGroup group)
        {
            var filters = group.FindAllChildren<RuminantFilter>();
            
            string TestGender(RuminantFilter filter, string gender)
            {
                /* CONDITIONAL LOGIC IS ORDER DEPENDENT, DO NOT REARRANGE */

                // Gender is already determined
                if (gender == "Both")
                    return gender;

                // If gender is undetermined, use the filter gender
                if (gender == "Either")
                    return filter.Gender;

                // No need to change gender if parameter is genderless
                if (filter.Gender == "Either")
                    return gender;

                // If the genders do not match, return both
                if (filter.Gender != gender)
                    return "Both";
                // If the genders match, return the current gender
                else
                    return gender;
            }

            // Which gender do the parameters belong to
            string genders = filters.Aggregate("Either", (s, f) => TestGender(f, s));

            var rules = filters.Select(f => f.ToRule(f.Parameter.ToString()));

            // There will be no ruminants with parameters belonging to both genders
            if (genders == "Both")
                return new List<Ruminant>();

            // If all parameters are genderless
            if (genders == "Either")
                return individuals.ExecuteFilter(rules, group).ToList();

            // Add a rule to filter by gender
            var genderRule = new RuminantFilter
            {
                Parameter = RuminantFilterParameters.Gender,
                Operator = FilterOperators.Equal,
                Value = genders
            }.ToRule(RuminantFilterParameters.Gender.ToString());

            rules.Append(genderRule);            

            if (genders == "Female")
                return individuals.Where(a => a.Gender == Sex.Female).ExecuteFilter(rules, group);
            else // genders == "Male"
                return individuals.Where(a => a.Gender == Sex.Male).ExecuteFilter(rules, group);            
        }

        private static IEnumerable<TRuminant> ExecuteFilter<TRuminant>(this IEnumerable<TRuminant> source, IEnumerable<Rule> rules, IFilterGroup group)
            where TRuminant : Ruminant
        {
            double proportionToUse = group.Proportion <= 0 ? group.Proportion : 1;

            group.CombinedRules = group.CombinedRules ?? CompileRule(new List<TRuminant>(), rules);

            var result = GetItemsThatMatchAll(source, group.CombinedRules as List<Func<TRuminant, bool>>);

            if (proportionToUse >= 1)
                return result.ToList();

            int numberToUse = Convert.ToInt32(Math.Ceiling(result.Count() / proportionToUse));

            return result.OrderBy(x => RandomNumberGenerator.Generator.Next()).Take(numberToUse).ToList();
        }

        /// <summary>
        /// Filter extensions for other animals cohort list
        /// </summary>
        public static List<OtherAnimalsTypeCohort> Filter(this IEnumerable<OtherAnimalsTypeCohort> individuals, Model filterGroup)
        {
            var rules = new List<Rule>();
            foreach (OtherAnimalsFilter filter in filterGroup.FindAllChildren<OtherAnimalsFilter>())
            {
                ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
                // create rule list
                rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
            }

            if ((filterGroup as IFilterGroup).CombinedRules is null)
            {
                (filterGroup as IFilterGroup).CombinedRules = CompileRule(new List<OtherAnimalsTypeCohort>(), rules);
            }
            return GetItemsThatMatchAll<OtherAnimalsTypeCohort>(individuals, (filterGroup as IFilterGroup).CombinedRules as List<Func<OtherAnimalsTypeCohort, bool>>).ToList<OtherAnimalsTypeCohort>();
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

        private static List<Func<T, bool>> CompileRule<T>(List<T> targetEntity, IEnumerable<Rule> rules)
        {
            // Credit for this function goes to Cole Francis, Architect
            // The pre-compiled rules type
            // https://mobiusstraits.com/2015/08/12/expression-trees/

            // Loop through the rules and compile them against the properties of the supplied shallow object
            var compiledRules = rules.Select(rule =>
            {
                var genericType = Expression.Parameter(typeof(T));
                var key = Expression.Property(genericType, rule.ComparisonPredicate);
                var propertyType = typeof(T).GetProperty(rule.ComparisonPredicate).PropertyType;

                object ce = propertyType.BaseType.Name == "Enum"                
                    ? Enum.Parse(propertyType, rule.ComparisonValue, true)         
                    : Convert.ChangeType(rule.ComparisonValue, propertyType);
                
                var value = Expression.Constant(ce);
                var binaryExpression = Expression.MakeBinary(rule.ComparisonOperator, key, value);

                return Expression.Lambda<Func<T, bool>>(binaryExpression, genericType).Compile();
            });

            // Return the compiled rules to the caller
            return compiledRules.ToList();
        }

    }
}
