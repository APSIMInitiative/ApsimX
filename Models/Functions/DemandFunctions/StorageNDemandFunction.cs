using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// The partitioning of daily growth to storage biomass is based on the maximum N concentration.
    /// </summary>
    [Serializable]
    [Description("This function calculates...")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StorageNDemandFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The maximum N concentration of the organ</summary>
        [Description("The maximum N concentration of the organ")]
        [Link]
        private IFunction maxNConc = null;

        /// <summary>Switch to modulate N demand</summary>
        [Description("Switch to modulate N demand")]
        [Link]
        private IFunction nitrogenDemandSwitch = null;

        private IArbitration parentOrgan = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            bool ParentOrganIdentified = false;
            IModel ParentClass = this.Parent;
            while (!ParentOrganIdentified)
            {
                if (ParentClass is IArbitration)
                {
                    parentOrgan = ParentClass as IArbitration;
                    ParentOrganIdentified = true;
                    if (ParentClass is IPlant)
                        throw new Exception(Name + "cannot find parent organ to get Structural and storage DM status");
                }
                ParentClass = ParentClass.Parent;
            }
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double potentialAllocation = parentOrgan.potentialDMAllocation.Structural + parentOrgan.potentialDMAllocation.Metabolic;
            double NDeficit = Math.Max(0.0, maxNConc.Value() * (parentOrgan.Live.Wt + potentialAllocation) - parentOrgan.Live.N);
            NDeficit *= nitrogenDemandSwitch.Value();

            return Math.Max(0, NDeficit - parentOrgan.NDemand.Structural - parentOrgan.NDemand.Metabolic);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // get description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
