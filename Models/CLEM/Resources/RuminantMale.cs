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
        /// <summary>
        /// Indicates if individual is breeding sire
        /// </summary>
        public bool Sire { get; set; }

        /// <summary>
        /// Indicates if individual is breeding sire
        /// </summary>
        public bool IsSire 
        {
            get
            {
                if(ReplacementBreeder)
                {
                    if(Age >= BreedParams.MinimumAge1stMating)
                    {
                        Sire = true;
                        ReplacementBreeder = false;
                    }
                }
                return Sire;
            }
        }

        /// <summary>
        /// Indicates if individual is castrated
        /// </summary>
        public bool IsCastrated { get; set; }


        /// <summary>
        /// Indicates if individual is draught animal
        /// </summary>
        public bool IsDraught { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantMale(double setAge, Sex setGender, double setWeight, RuminantType setParams): base(setAge, setGender, setWeight, setParams)
        {
            Sire = false;
            IsDraught = false;
            IsCastrated = false;
        }

    }
}
