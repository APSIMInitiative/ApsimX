using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This manages all ruminant parameters for a ruminant Type
    /// </summary>
    [Serializable]
    public class RuminantParameters
    {
        private RuminantParametersBreed breeding;

        /// <summary>
        /// Parameters for the Breed activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersBreed Breeding 
        { 
            get
            {
                if (breeding is null)
                    throw new Exception("Unable to find any Breed parameters");
                return breeding;
            }
            set { breeding = value; }
        }

        /// <summary>
        /// Find base mortality rate across possible locations
        /// </summary>
        public  double FindBaseMortalityRate 
        {
            get
            {
                return Grow?.MortalityBase ?? (GrowSCA?.BasalMortalityRate_CD1 ?? 0 * 365);
            }
        }

        /// <summary>
        /// Feed parameters defining the RuminantType
        /// </summary>
        [JsonIgnore]
        public RuminantParametersFeed Feeding { get; set; }

        /// <summary>
        /// General parameters defining the RuminantType
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGeneral General { get; set; }

        /// <summary>
        /// Parameters for the Grazing activities
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrazing Grazing { get; set; }

        /// <summary>
        /// Parameters for the Grow activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow Grow { get; set; }

        /// <summary>
        /// Parameters for the GrowSCA activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrowSCA GrowSCA { get; set; }

        /// <summary>
        /// Initialise by finding available RuminantParameters
        /// </summary>
        /// <param name="ruminantType"></param>
        public void Initialise(RuminantType ruminantType)
        {
            breeding = ruminantType.FindChild<RuminantParametersBreed>();
            Feeding = ruminantType.FindChild<RuminantParametersFeed>();
            General = ruminantType.FindChild<RuminantParametersGeneral>();
            Grazing = ruminantType.FindChild<RuminantParametersGrazing>();
            Grow = ruminantType.FindChild<RuminantParametersGrow>();
            GrowSCA = ruminantType.FindChild<RuminantParametersGrowSCA>();
        }
    }
}
