using System;
using System.Collections.Generic;
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
        /// <summary>Value to multiply demand for.  Use to switch demand on and off</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        [Description("Multiplies calculated demand.  Use to switch demand on and off")]
        [Units("unitless")]
        public IFunction multiplier = null;

        private Organ parentOrgan = null;

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
            if ((this.Name == "Structural") && (this.Parent.Parent.Name == "Nitrogen"))
            {
                return parentOrgan.Carbon.DemandsAllocated.Total * parentOrgan.Nitrogen.ConcentrationOrProportion.Structural;
            }
            
            if ((this.Name == "Structural") && (this.Parent.Parent.Name == "Carbon"))
            {
                return parentOrgan.totalDMDemand * parentOrgan.Carbon.ConcentrationOrProportion.Structural;
            }

            double deficit = Double.NaN;
            double PotentialWt = (parentOrgan.StartLive.Carbon.Total + parentOrgan.Carbon.DemandsAllocated.Total)/parentOrgan.Cconc;
            
            if ((this.Name == "Metabolic") && (this.Parent.Parent.Name == "Nitrogen"))
            {
                double targetMetabolicN = (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrProportion.Metabolic) - (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrProportion.Structural);
                deficit = targetMetabolicN - parentOrgan.Live.Nitrogen.Metabolic;
            }

            double potentialStructuralC = parentOrgan.Live.Carbon.Structural + parentOrgan.totalDMDemand * parentOrgan.Carbon.ConcentrationOrProportion.Structural;
            double potentialTotalC = potentialStructuralC / parentOrgan.Carbon.ConcentrationOrProportion.Structural;

            if ((this.Name == "Metabolic") && (this.Parent.Parent.Name == "Carbon"))
            {
                double targetMetabolicC = potentialTotalC * parentOrgan.Carbon.ConcentrationOrProportion.Metabolic;
                deficit = targetMetabolicC - parentOrgan.Live.Carbon.Metabolic;
            }
            
            else if ((this.Name == "Storage") && (this.Parent.Parent.Name == "Nitrogen"))
            {
                double targetStorageN = (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrProportion.Storage) - (PotentialWt * parentOrgan.Nitrogen.ConcentrationOrProportion.Metabolic);
                deficit = targetStorageN - parentOrgan.Live.Nitrogen.Storage;
            }
            
            else if ((this.Name == "Storage") && (this.Parent.Parent.Name == "Carbon"))
            {
                double targetStorageC = potentialTotalC * parentOrgan.Carbon.ConcentrationOrProportion.Storage; 
                deficit = targetStorageC - parentOrgan.Live.Nitrogen.Storage;
            }
            
            if (Double.IsNaN(deficit))
                    throw new Exception(this.FullPath + " Must be named Metabolic or Structural to represent the pool it is parameterising and be placed on a NutrientDemand Object which is on Carbon or Nitrogen OrganNutrienDeltaObject");

            // Deficit is limited to max of zero as it cases where ConcentrationsOrProportions change deficits can become negative.
            if (multiplier != null)
                return Math.Max(0, deficit * multiplier.Value());
            else
                return Math.Max(0, deficit);
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
