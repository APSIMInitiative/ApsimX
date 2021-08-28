using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// The partitioning of daily growth to storage biomass is based on a storage fraction.
    /// </summary>
    [Serializable]
    [Description("This function calculates...")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StorageDMDemandFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The Storage Fraction</summary>
        [Description("StorageFraction")]
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction storageFraction = null;

        private IArbitration parentOrgan = null;

        private IAmOrganHearMeRoar parentSimpleOrgan = null;

        private string parentOrganType = "";
 
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
                    parentOrganType = "IArbitration";
                    if (ParentClass is IPlant)
                        throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");
                }
                if (ParentClass is IAmOrganHearMeRoar)
                {
                    parentSimpleOrgan = ParentClass as IAmOrganHearMeRoar;
                    ParentOrganIdentified = true;
                    parentOrganType = "ISubscribeToBiomassArbitration";
                    if (ParentClass is IPlant)
                        throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");
                }
                ParentClass = ParentClass.Parent;
            }
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (parentOrganType == "IArbitration")
            {
                double structuralWt = parentOrgan.Live.StructuralWt + parentOrgan.DMDemand.Structural;
                double MaximumDM = MathUtilities.Divide(structuralWt, 1 - storageFraction.Value(), 0);
                double AlreadyAllocated = structuralWt + parentOrgan.Live.StorageWt;
                return MaximumDM - AlreadyAllocated;
            }
            if (parentOrganType == "ISubscribeToBiomassArbitration")
            {
                double structuralWt = parentSimpleOrgan.Live.Weight.Structural + parentSimpleOrgan.Carbon.Deltas.Demands.Structural;
                double MaximumDM = MathUtilities.Divide(structuralWt, 1 - storageFraction.Value(), 0);
                double AlreadyAllocated = structuralWt + parentSimpleOrgan.Live.Weight.Storage;
                return MaximumDM - AlreadyAllocated;
            }
            else
                throw new Exception("Could not locate parent organ");
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
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children.
                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
