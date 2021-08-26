using APSIM.Services.Documentation;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;

namespace Models.PMF
{
    /// <summary>
    /// This is the collection of functions for calculating the demands for
    /// each of the biomass pools (Structural, Metabolic, and Storage).
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassDemand : Model
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

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write description of this class from summary and remarks XML documentation.
            foreach (var tag in GetModelDescription())
                yield return tag;

            foreach (var tag in DocumentChildren<IModel>())
                yield return tag;
        }
    }
}
