using System;

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
                    return "Castrate";
                else
                {
                    if (!IsAbleToBreed)
                    {
                        return "PreBreeder";
                    }
                    else
                    {
                        if (Attributes.Exists("Sire"))
                        {
                            return "Sire";
                        }
                        return "WildBreeder";
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
                if (!IsSterilised && Attributes.Exists("Sire"))
                    if (AgeInDays >= Parameters.General.MaleMinimumAge1stMating.InDays & Weight.HighestAttained >= Parameters.General.MaleMinimumSize1stMating * Weight.StandardReferenceWeight)
                    {
                        IsReplacementBreeder = false;
                        return true;
                    }
                return false;
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
                return (IsWeaned && !IsAbleToBreed);
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
        public override bool IsAbleToBreed {  get { return !IsSterilised && ((Weight.HighestAttained >= Parameters.General.MaleMinimumSize1stMating * Weight.StandardReferenceWeight) & (AgeInDays >= Parameters.General.MaleMinimumAge1stMating.InDays)); } }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantMale(RuminantParameters setParams, DateTime date, int setAge, double birthScalar, double setWeight)
            : base(setParams, setAge, birthScalar, setWeight, date)
        {
            // needed for female specific actions
        }

        /// <summary>
        /// Report protein required for maintenance pregnancy and lactationsaved from reduced lactation (kg)
        /// </summary>
        public override double ProteinRequiredBeforeGrowth { get { return Weight.Protein.ForMaintenence + Weight.Protein.ForWool; } }

    }
}
