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
        /// Represents any uncastrated male of breeding age
        /// </summary>
        public bool IsSire 
        {
            get
            {
                if(!AttributeExists("Castrated"))
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
        /// Indicates if individual is castrated
        /// </summary>
        public bool IsCastrated 
        { 
            get
            {
                return AttributeExists("Castrated");
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantMale(double setAge, Sex setGender, double setWeight, RuminantType setParams): base(setAge, setGender, setWeight, setParams)
        {
        }

    }
}
