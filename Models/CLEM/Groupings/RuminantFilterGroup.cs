using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.CLEM
{
    /// <summary>
    /// Implements IFilterGroup for a specific set of filter parameters
    /// </summary>
    [Serializable]
    public class RuminantFilterGroup : FilterGroup<Ruminant>
    {
        /// <summary>
        /// Return some proportion of a ruminant collection after filtering
        /// </summary>
        public IEnumerable<Ruminant> FilterProportion(IEnumerable<Ruminant> individuals)
        {
            double proportion = Proportion <= 0 ? 1 : Proportion;
            int number = Convert.ToInt32(Math.Ceiling(proportion * individuals.Count()));

            return Filter(individuals).Take(number);
        }

        /// <inheritdoc/>
        public new IEnumerable<T> Filter<T>(IEnumerable<T> ruminants)
            where T : Ruminant
        {
            var filters = FindAllChildren<Filter>();

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

            var sorts = FindAllChildren<ISort>();

            if (genders == "Both")
                return new List<T>();
            else
                return ruminants.Where(r => rules.All(rule => rule(r))).Sort(sorts);

        }
    }

}
