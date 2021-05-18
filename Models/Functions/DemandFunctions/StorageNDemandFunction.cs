using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// The partitioning of daily N supply to storage N attempts to bring the organ's N content to the maximum concentration.
    /// </summary>
    [Serializable]
    [Description("This function calculates...")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StorageNDemandFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The maximum N concentration of the organ</summary>
        [Description("The maximum N concentration of the organ")]
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction maxNConc = null;

        /// <summary>Switch to modulate N demand</summary>
        [Description("Switch to modulate N demand")]
        [Link(Type = LinkType.Child, ByName = true)]
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
                        throw new Exception(Name + "cannot find parent organ to get Structural and Storage N status");
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
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // get description of this class
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // find parent organ's name
                if (Parent == null)
                    return;
                string organName = "";
                bool seekingParentOrgan = true;
                IModel parentClass = this.Parent;
                while (seekingParentOrgan)
                {
                    if (parentClass is IOrgan)
                    {
                        seekingParentOrgan = false;
                        organName = (parentClass as IOrgan).Name;
                        if (parentClass is IPlant)
                            throw new Exception(Name + "cannot find parent organ to get Structural and Storage N status");
                    }
                    parentClass = parentClass.Parent;
                }

                // add a description of the equation for this function
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = [" + organName + "].maximumNconc × (["
                    + organName + "].Live.Wt + potentialAllocationWt) - [" + organName + "].Live.N</i>", indent));
                tags.Add(new AutoDocumentation.Paragraph("The demand for storage N is further reduced by a factor specified by the [" 
                    + organName + "].NitrogenDemandSwitch.", indent));
            }
        }
    }
}
