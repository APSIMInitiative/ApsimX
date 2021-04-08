using Models.Core;
using System.Collections.Generic;
using Models.Functions;
using Models.PMF.Interfaces;
using System;
using APSIM.Services.Documentation;

namespace Models.PMF
{
    /// <summary>
    /// This class holds the functions for calculating the demands for each biomass fraction
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

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // get description of this class.
                tags.Add(new AutoDocumentation.Paragraph("This is the collection of functions for calculating the demands for each of the biomass pools (Structural, Metabolic, and Storage).", indent));

                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children.
                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
