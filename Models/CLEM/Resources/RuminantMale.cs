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
        public bool BreedingSire { get; set; }

        /// <summary>
        /// Indicates if individual is draught animal
        /// </summary>
        public bool Draught { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantMale(double setAge, Sex setGender, double setWeight, RuminantType setParams): base(setAge, setGender, setWeight, setParams)
        {
            BreedingSire = false;
            Draught = false;
        }

    }
}
