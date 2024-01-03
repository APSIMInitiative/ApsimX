using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// This class holds the functions for calculating the absolute demands and priorities for each biomass fraction. 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassDemandAndPriority : Model
    {
        /// <summary>The demand for the structural fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Structural = null;

        /// <summary>The demand for the metabolic fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Metabolic = null;

        /// <summary>The demand for the storage fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Storage = null;

        /// <summary>Factor for Structural biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QStructuralPriority = null;

        /// <summary>Factor for Metabolic biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QMetabolicPriority = null;

        /// <summary>Factor for Storage biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QStoragePriority = null;

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            // add a heading
            yield return new Section(Name, GetTags());
        }

        /// <summary>
        /// Get the tags used in this model's description.
        /// </summary>
        private IEnumerable<ITag> GetTags()
        {
            // Write the model description.
            foreach (ITag tag in GetModelDescription())
                yield return tag;

            // Write memos.
            foreach (ITag tag in DocumentChildren<Memo>())
                yield return tag;

            // Write child functions.
            foreach (ITag tag in DocumentChildren<IFunction>())
                yield return tag;
        }
    }
}
