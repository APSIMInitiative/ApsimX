using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual male Ruminant.
    /// </summary>
    [Serializable]
    public class RuminantMale : Ruminant
    {
        /// <summary>
        /// Sex of individual
        /// </summary>
        public override Sex Sex { get { return Sex.Male; } }

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
                    if (Age >= BreedParams.MinimumAge1stMating)
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
                    if (Age >= BreedParams.MinimumAge1stMating)
                        return true;
                return false;
            }
        }

        /// <inheritdoc/>
        [FilterByProperty]
        public override bool IsSterilised { get { return IsCastrated; } }

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
        public RuminantMale(RuminantType setParams, double setAge, double setWeight)
            : base(setParams, setAge, setWeight)
        {
        }

    }
}
