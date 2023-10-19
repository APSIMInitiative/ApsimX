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
                if ((this as RuminantMale).IsSire)
                    return "Sire";
                else if ((this as RuminantMale).IsCastrated)
                    return "Castrate";
                else
                {
                    if ((this as RuminantMale).IsWildBreeder)
                    {
                        return "Breeder";
                    }
                    else
                    {
                        return "PreBreeder";
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
                if (Attributes.Exists("Sire") & !Attributes.Exists("Castrated"))
                    if (AgeInDays >= BreedParams.MinimumAge1stMating.InDays)
                    {
                        ReplacementBreeder = false;
                        return true;
                    }
                return false;
            }
        }

        /// <summary>
        /// Indicates if individual is breeding sire
        /// Represents any uncastrated male of breeding age that is assigned sire and therefroe may have improved genetics/price
        /// </summary>
        [FilterByProperty]
        public bool IsWildBreeder
        {
            get
            {
                if (!Attributes.Exists("Sire") & !Attributes.Exists("Castrated"))
                    if (AgeInDays >= BreedParams.MinimumAge1stMating.InDays)
                        return true;
                return false;
            }
        }

        /// <inheritdoc/>
        [FilterByProperty]
        public override bool Sterilised { get { return IsCastrated; } }

        /// <summary>
        /// Indicates if individual is castrated
        /// </summary>
        [FilterByProperty]
        public bool IsCastrated { get { return Attributes.Exists("Castrated"); }}

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public override bool IsAbleToBreed {  get { return this.IsSire | this.IsWildBreeder; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantMale(RuminantType setParams, DateTime date, int setAge, double setWeight)
            : base(setParams, setAge, setWeight, date)
        {
        }

    }
}
