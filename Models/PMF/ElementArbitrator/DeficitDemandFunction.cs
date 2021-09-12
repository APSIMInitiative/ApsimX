using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Functions;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Organs;

namespace Models.PMF
{
    /// <summary>
    /// Calculates the Deficit of a given labile nutrient pool and returns it to use for a demand.
    /// </summary>
    [Serializable]
    [Description("This function calculates demands for metabolic and storage pools based on the size of the potential deficits of these pools.  For nutrients is uses maximum, critical and minimum concentration thresholds and for carbon it uses structural, metabolic and storage partitioning proportions to ")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(NutrientDemandFunctions))]
    public class DeficitDemandFunction : Model, IFunction, ICustomDocumentation
    {
        private Organ parentOrgan = null;

        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        private double multiplier { get; set; }

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
                if (ParentClass is Organ)
                {
                    parentOrgan = ParentClass as Organ;
                    ParentOrganIdentified = true;
                    if (ParentClass is IPlant)
                        throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");
                }
                ParentClass = ParentClass.Parent;
            }
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            multiplier = 1.0;

            foreach (IFunction F in ChildFunctions)
                multiplier *=  F.Value(arrayIndex);
            
            if (this.Parent.Parent.Name == "Nitrogen")
            {
                if (this.Name == "Structural")
                {
                    return parentOrgan.Carbon.DemandsAllocated.Total / parentOrgan.Cconc * parentOrgan.Nitrogen.ConcentrationOrFraction.Structural;
                }

                double PotentialWt = (parentOrgan.Live.Carbon.Total + parentOrgan.Carbon.DemandsAllocated.Total) / parentOrgan.Cconc;

                if (this.Name == "Metabolic") 
                {
                    double targetMetabolicN = (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrFraction.Metabolic) - (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrFraction.Structural);
                    return Math.Max(0,(targetMetabolicN - parentOrgan.Live.Nitrogen.Metabolic) * multiplier);
                }

                if (this.Name == "Storage") 
                {
                    double targetStorageN = (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrFraction.Storage) - (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrFraction.Metabolic);
                    return Math.Max(0,(targetStorageN - parentOrgan.Live.Nitrogen.Storage) * multiplier);
                }
            }

            if (this.Parent.Parent.Name == "Carbon")
            {
                if (this.Name == "Structural")
                {
                    return parentOrgan.totalDMDemand * parentOrgan.Cconc * parentOrgan.Carbon.ConcentrationOrFraction.Structural;
                }

                double potentialStructuralC = parentOrgan.Live.Carbon.Structural + (parentOrgan.totalDMDemand * parentOrgan.Cconc * parentOrgan.Carbon.ConcentrationOrFraction.Structural);
                double potentialTotalC = potentialStructuralC / parentOrgan.Carbon.ConcentrationOrFraction.Structural;

                if (this.Name == "Metabolic")
                {
                    double targetMetabolicC = potentialTotalC * parentOrgan.Carbon.ConcentrationOrFraction.Metabolic;
                    return Math.Max(0,(targetMetabolicC - parentOrgan.Live.Carbon.Metabolic) * multiplier);
                }

                if (this.Name == "Storage") 
                {
                    double targetStorageC = potentialTotalC * parentOrgan.Carbon.ConcentrationOrFraction.Storage;
                    return Math.Max(0, (targetStorageC - parentOrgan.Live.Carbon.Storage)* multiplier);
                }
            }
            
            throw new Exception(this.FullPath + " Must be named Metabolic or Structural to represent the pool it is parameterising and be placed on a NutrientDemand Object which is on Carbon or Nitrogen OrganNutrienDeltaObject");
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
