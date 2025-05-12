using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.LifeCycle;
using System;
using System.Collections.Generic;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual male Ruminant.
    /// </summary>
    [Serializable]
    public class RuminantMale : Ruminant
    {
        /// <inheritdoc/>
        public override Sex Sex { get { return Sex.Male; } }

        /// <inheritdoc/>
        public override string BreederClass
        {
            get
            {
                if (IsSterilised)
                {
                    return "Castrate";
                }
                else
                {
                    if (!IsMature)
                    {
                        return "PreBreeder";
                    }

                    if (Attributes.Exists("Sire"))
                    {
                        return "Sire";
                    }
                    return "WildBreeder";
                }
            }
        }

        /// <inheritdoc/>
        public override void UpdateBreedingDetails()
        {
            // This method is called on any update to age
            // This will occur at the start of each time-step called by the RuminanTType resource before any activities.

            if (IsSterilised)
            {
                return;
            }

            if (!IsMature)
            {
                CheckWeanedStatus();
                // check age
                if (AgeInDays >= Parameters.General.MaleMinimumAge1stMating.InDays)
                {
                    int daysAgo = AgeInDays - Parameters.General.MaleMinimumAge1stMating.InDays;
                    // check size
                    if (Weight.HighestAttained >= Parameters.General.MaleMinimumSize1stMating * Weight.StandardReferenceWeight)
                    {
                        SetMature();
                        IsReplacementBreeder = false;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates if individual is breeding sire
        /// Represents any uncastrated male of breeding age
        /// </summary>
        [FilterByProperty]
        public bool IsSire
        {
            get
            {
                return (!IsSterilised && IsMature && Attributes.Exists("Sire"));
            }
        }

        /// <summary>
        /// Indicates if this male is a weaned but less than age and size at first mating 
        /// </summary>
        [FilterByProperty]
        public bool IsPreBreeder
        {
            get
            {
                return (IsWeaned && !IsMature);
            }
        }

        /// <summary>
        /// Indicates if individual is able to breed but not specified as a breeding sire (e.g. wild breeder, "Mickey")
        /// Represents any uncastrated male of breeding age that is not assigned sire
        /// </summary>
        [FilterByProperty]
        public bool IsWildBreeder
        {
            get
            {
                return (IsWeaned && !IsSterilised && !IsPreBreeder && !Attributes.Exists("Sire"));
            }
        }

        /// <summary>
        /// Indicates if individual is castrated
        /// </summary>
        [FilterByProperty]
        public bool IsCastrated { get { return IsSterilised; }}

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public override bool IsAbleToBreed {  get { return IsMature & !IsSterilised; } }

        /// <inheritdoc/>
        [FilterByProperty]
        public override string BreedingStatus
        {
            get
            {
                if (IsAbleToBreed)
                    return "Ready";
                else
                    return "NotReady";
            }
        }

        /// <summary>
        /// Constructor based on cohort details
        /// </summary>
        public RuminantMale(DateTime date, RuminantParameters setParams, int setAge, double setWeight, int? id = null, RuminantTypeCohort cohortDetails = null, IEnumerable<ISetAttribute> initialAttributes = null)
            : base(date, setParams, setAge, setWeight, id, cohortDetails, initialAttributes)
        {
            // needed for male specific actions
            if (cohortDetails.Sire)
            {
                Attributes.Add("Sire");
            }

        }

        /// <summary>
        /// Constructor for new born male ruminant
        /// </summary>
        public RuminantMale(DateTime date, int id, RuminantFemale mother, IRuminantActivityGrow growActivity)
            : base(date, id, mother, growActivity)
        {
            // needed for male specific actions
        }

        /// <summary>
        /// Report protein required for maintenance pregnancy and lactationsaved from reduced lactation (kg)
        /// </summary>
        public override double ProteinRequiredBeforeGrowth { get { return Weight.Protein.ForMaintenence; } }

    }
}
