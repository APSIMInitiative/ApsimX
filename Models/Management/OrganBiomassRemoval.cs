using System;
using Models.Core;
using Models.Functions;


namespace Models.Management
{
    /// <summary>
    /// Add functions of name: LiveToRemove, DeadToRemove, LiveToResidue, DeadToResidue to specify each removal fraction.  
    /// All 4 removal fractions may be sepcified by a child funciton and will be treated as zero if child function of matching name is absent   
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(RemoveBiomassSettings))]
    public class OrganBiomassRemoval : Model
    {
        /// <summary>Fraction of live biomass to remove from organ (0-1) </summary>
        [Link(Type = LinkType.Child, ByName = true,IsOptional =true)]
        [Units("g/m2")]
        public IFunction liveToRemove = null;

        /// <summary>Fraction of dea biomass to remove from organ (0-1) </summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("g/m2")]
        public IFunction deadToRemove = null;

        /// <summary>Fraction of live biomass to remove from organ and send to residues (0-1) </summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("g/m2")]
        public IFunction liveToResidue = null;

        /// <summary>Fraction of live biomass to remove from organ and send to residue pool (0-1) </summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("g/m2")]
        public IFunction deadToResidue = null;
    }
}
