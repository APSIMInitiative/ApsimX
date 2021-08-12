using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual male Ruminant.
    /// </summary>
    public class RuminantMale: Ruminant
    {
        /// <inheritdoc/>
        public override Sex Sex => Sex.Male;

        /// <summary>
        /// Indicates if individual is breeding sire
        /// Represents any uncastrated male of breeding age
        /// </summary>
        public bool IsSire 
        {
            get
            {
                if(Attributes.Exists("Sire") & !Attributes.Exists("Castrated"))
                {
                    if (Age >= BreedParams.MinimumAge1stMating)
                    {
                        ReplacementBreeder = false;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Indicates if individual is breeding sire
        /// Represents any uncastrated male of breeding age that is assigned sire and therefroe may have improved genetics/price
        /// </summary>
        public bool IsWildBreeder
        {
            get
            {
                if (!Attributes.Exists("Sire") & !Attributes.Exists("Castrated"))
                {
                    if (Age >= BreedParams.MinimumAge1stMating)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Indicates if individual is castrated
        /// </summary>
        public bool IsCastrated 
        { 
            get
            {
                return Attributes.Exists("Castrated");
            }
        }

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public override bool IsAbleToBreed
        {
            get
            {
                return this.IsSire | this.IsWildBreeder;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantMale(RuminantType setParams, double setAge, double setWeight)
            : base(setParams, setAge, setWeight)
        {
        }

    }
}
