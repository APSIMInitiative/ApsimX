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
        /// Converts a filter expression to a rule
        /// </summary>
        /// <param name="filter">The filter to be converted</param>
        private static Rule ToRule(this IFilter filter)
        {
            ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
            // create rule list
            return new Rule(filter.ParameterName, op, filter.Value);
        }

        /// <summary>
        /// Filter the source using any valid models in the given model
        /// </summary>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, IModel model)
        {
            var rules = model.FindAllChildren<IFilter>().Select(ToRule);
            var combined = (model as IFilterGroup)?.CombinedRules ?? CompileRule<T>(rules);

            if (combined is List<Func<T, bool>> predicates && predicates.Any())
                return GetItemsThatMatchAll(source, predicates);
            else
                return source;
        }

        /// <summary>
        /// Filter a collection of ruminants by the parameters defined in the filter group
        /// </summary>
        public static IEnumerable<Ruminant> FilterRuminants(this IEnumerable<Ruminant> individuals, IFilterGroup group)
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
            group.CombinedRules = filters.Select(f => f.ToRule());

            // There will be no ruminants with parameters belonging to both genders
            IEnumerable<Ruminant> result = new List<Ruminant>();

            if (genders == "Either")
                result = individuals;

            else if (genders == "Female")
                result = individuals.OfType<RuminantFemale>();

            else if (genders == "Male")
                result = individuals.OfType<RuminantMale>();

            else
                return result;

            double proportion = group.Proportion <= 0 ? 1 : group.Proportion;
            int number = Convert.ToInt32(Math.Ceiling(proportion * individuals.Count()));

            var sorts = group.FindAllChildren<ISort>();

            return result.Filter(group).Sort(sorts).Take(number);
        }

        /// <summary>
        /// Order a collection based on the given sorting parameters
        /// </summary>
        /// <param name="source">The items to sort</param>
        /// <param name="sorts">The parameters to sort by</param>
        /// <returns></returns>
        public static IOrderedEnumerable<T> Sort<T>(this IEnumerable<T> source, IEnumerable<ISort> sorts)
        {
            var sorted = source.OrderBy(i => 1);

            if (!sorts.Any())
                return sorted;

            foreach (ISort sort in sorts)
                sorted = (sort.SortDirection == System.ComponentModel.ListSortDirection.Ascending) ? sorted.ThenBy(sort.OrderRule) : sorted.ThenByDescending(sort.OrderRule);

            return sorted;
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
            return source?.Where(t => predicates.Any(predicate => predicate(t)));
        }

        private static IEnumerable<T> GetItemsThatMatchAll<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
        {
            
            return source?.Where(t => predicates.All(predicate => predicate(t)));
        }

        private static List<Func<T, bool>> CompileRule<T>(IEnumerable<Rule> rules)
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
