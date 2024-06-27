using Models.Core;
using System;
using Newtonsoft.Json;

namespace Models.Management
{
    /// <summary>
    /// List of possible biomass removal types
    /// </summary>
    public enum BiomassRemovalType
    {
        /// <summary>Biomass is cut</summary>
        Cutting,
        /// <summary>Biomass is grazed</summary>
        Grazing,
        /// <summary>Biomass is harvested</summary>
        Harvesting,
        /// <summary>Biomass is pruned</summary>
        Pruning,
        /// <summary>Biomass is Allremoved</summary>
        EndCrop,
        /// <summary>No biomass is removed</summary>
        None,

    }

    /// <summary>Stores a row of Biomass Removal Fractions</summary>
    [Serializable]
    public class BiomassRemovalOfPlantOrganType
    {
        /// <summary>Name of the Crop this removal applies to</summary>
        public string PlantName { get; set; }

        /// <summary>Name of the Organ in the given crop that this removal applies to</summary>
        public string OrganName { get; set; }

        /// <summary>The type of removal this is</summary>
        [JsonIgnore]
        public BiomassRemovalType Type { get { return Enum.Parse<BiomassRemovalType>(TypeString); } }

        /// <summary></summary>
        public string TypeString { get; set; }

        /// <summary>Fraction of live biomass to remove from organ (0-1) </summary>
        [Units("g/m2")]
        public double LiveToRemove { get; set; }

        /// <summary>Fraction of dea biomass to remove from organ (0-1) </summary>
        [Units("g/m2")]
        public double DeadToRemove { get; set; }

        /// <summary>Fraction of live biomass to remove from organ and send to residues (0-1) </summary>
        [Units("g/m2")]
        public double LiveToResidue { get; set; }

        /// <summary>Fraction of live biomass to remove from organ and send to residue pool (0-1) </summary>
        [Units("g/m2")]
        public double DeadToResidue { get; set; }

        /// <summary>Default Constructor</summary>
        public BiomassRemovalOfPlantOrganType()
        {
            this.PlantName = null;
            this.OrganName = null;
            this.TypeString = null;
            this.LiveToRemove = double.NaN;
            this.DeadToRemove = double.NaN;
            this.LiveToResidue = double.NaN;
            this.DeadToResidue = double.NaN;
        }

        /// <summary></summary>
        public BiomassRemovalOfPlantOrganType(string plantName, string organName, string type, double liveToRemove, double deadToRemove, double liveToResidue, double deadToResidue)
        {
            this.PlantName = plantName;
            this.OrganName = organName;
            this.TypeString = type;
            this.LiveToRemove = liveToRemove;
            this.DeadToRemove = deadToRemove;
            this.LiveToResidue = liveToResidue;
            this.DeadToResidue = deadToResidue;
        }
    }
}