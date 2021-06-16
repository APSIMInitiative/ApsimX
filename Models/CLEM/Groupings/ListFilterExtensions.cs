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
        /// Filter the source using any valid models in the given model
        /// </summary>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, IModel model)
        {
            var rules = model.FindAllChildren<Filter>().Select(f => f.CompileRule<T>());
            var combined = (model as FilterGroup)?.CombinedRules ?? rules;

            if (combined is List<Func<T, bool>> predicates && predicates.Any())
                return GetItemsThatMatchAll(source, predicates);
            else
                return source;
        }

        /// <summary>
        /// Filter a collection of ruminants by the parameters defined in the filter group
        /// </summary>
        public static IEnumerable<Ruminant> FilterRuminants(this IEnumerable<Ruminant> individuals, FilterGroup group)
        {
            var filters = group.FindAllChildren<Filter>();
            
            string TestGender(string parameter, string gender)
            {
                string filter;
                switch (parameter)
                {
                    case "IsDraught":
                    case "IsSire":
                    case "IsCastrate":
                        filter = "Male";
                        break;

                    case "IsBreeder":
                    case "IsPregnant":
                    case "IsLactating":
                    case "IsPreBreeder":
                    case "MonthsSinceLastBirth":
                        filter = "Female";
                        break;

                    default:
                        filter = "Either";
                        break;
                }

                /* CONDITIONAL LOGIC IS ORDER DEPENDENT, DO NOT REARRANGE */

                // Gender is already determined
                if (gender == "Both")
                    return gender;

                // If gender is undetermined, use the filter gender
                if (gender == "Either")
                    return filter;

                // No need to change gender if parameter is genderless
                if (filter == "Either")
                    return gender;

                // If the genders do not match, return both
                if (filter != gender)
                    return "Both";
                // If the genders match, return the current gender
                else
                    return gender;
            }

            // Which gender do the parameters belong to
            string genders = filters.Aggregate("Either", (s, f) => TestGender(f.ParameterName, s));
            var rules = filters.Select(f => f.CompileRule<Ruminant>());
            group.CombinedRules = rules;

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

        

        private static IEnumerable<T> GetItemsThatMatchAny<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
        {
            return source?.Where(t => predicates.Any(predicate => predicate(t)));
        }

        private static IEnumerable<T> GetItemsThatMatchAll<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
        {
            
            return source?.Where(t => predicates.All(predicate => predicate(t)));
        }
    }
}
