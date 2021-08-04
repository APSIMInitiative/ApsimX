using System;
using System.Collections.Generic;
using Models.Core;
using System.Linq;
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
            var combined = (model as IFilterGroup)?.CombinedRules ?? rules;

            if (combined is List<Func<T, bool>> predicates && predicates.Any())
                return GetItemsThatMatchAll(source, predicates);
            else
                return source;
        }

        /// <summary>
        /// Return some proportion of a ruminant collection after filtering
        /// </summary>
        public static IEnumerable<Ruminant> FilterProportion(this IEnumerable<Ruminant> individuals, IFilterGroup group)
        {
            double proportion = group.Proportion <= 0 ? 1 : group.Proportion;
            int number = Convert.ToInt32(Math.Ceiling(proportion * individuals.Count()));            

            return individuals.FilterRuminants(group).Take(number);
        }

        /// <summary>
        /// Filter a collection of ruminants by the parameters defined in the filter group
        /// </summary>
        public static IEnumerable<Ruminant> FilterRuminants(this IEnumerable<Ruminant> ruminants, IFilterGroup group)
        {
            var filters = group.FindAllChildren<Filter>();

            string TestGender(Filter filter, string gender)
            {
                if (!(filter is FilterByProperty f))
                    return "Either";

                string sex;
                switch (f.Parameter)
                {
                    case "Gender":
                        sex = f.Value.ToString();
                        break;

                    case "IsDraught":
                    case "IsSire":
                    case "IsCastrate":
                        sex = "Male";
                        break;

                    case "IsBreeder":
                    case "IsPregnant":
                    case "IsLactating":
                    case "IsPreBreeder":
                    case "MonthsSinceLastBirth":
                        sex = "Female";
                        break;

                    default:
                        sex = "Either";
                        break;
                }

                /* CONDITIONAL LOGIC IS ORDER DEPENDENT, DO NOT REARRANGE */

                // Gender is already determined
                if (gender == "Both")
                    return gender;

                // If gender is undetermined, use the filter gender
                if (gender == "Either")
                    return sex;

                // No need to change gender if parameter is genderless
                if (sex == "Either")
                    return gender;

                // If the genders do not match, return both
                if (sex != gender)
                    return "Both";
                // If the genders match, return the current gender
                else
                    return gender;
            }

            // Which gender do the parameters belong to
            string genders = filters.Aggregate("Either", (s, f) => TestGender(f, s));
            var rules = filters.Select(f => f.CompileRule<Ruminant>());
            group.CombinedRules = rules;

            var sorts = group.FindAllChildren<ISort>();

            if (genders == "Either")
                return ruminants.Filter(group).Sort(sorts);

            else if (genders == "Female")
                return ruminants.OfType<RuminantFemale>().Filter(group).Sort(sorts);

            else if (genders == "Male")
                return ruminants.OfType<RuminantMale>().Filter(group).Sort(sorts);

            // There will be no ruminants with parameters belonging to both genders
            else
                return new List<Ruminant>();
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

        private static IEnumerable<T> GetItemsThatMatchAll<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
        {
            
            return source?.Where(t => predicates.All(predicate => predicate(t)));
        }
    }
}