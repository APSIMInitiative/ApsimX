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
    [Description("This function calculates supplies of nutrients from metabolic or storage pools")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(NutrientPoolFunctions))]
    public class MobilisationSupplyFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>Value to multiply demand for.  Use to switch demand on and off</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        [Description("Multiplies calculated supply.  Use to switch ReAllocation on and off or to throttle ReTranslocation")]
        [Units("unitless")]
        public IFunction multiplier = null;

        private Organ parentOrgan = null;

        // Declare a delegate type for calculating the IFunction return value:
        private delegate double DeficitCalculation();
        private DeficitCalculation CalcValue;

        private Organ FindParentOrgan(IModel model)
        {
            if (model is Organ) return model as Organ;

            if (model is IPlant)
                throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");

            return FindParentOrgan(model.Parent);
        }
        private OrganNutrientDelta FindPoolSource(string NutrientType, Organ parentOrgan)
        {
            var nutrientDelta = parentOrgan.FindChild(NutrientType) as OrganNutrientDelta;
            if (nutrientDelta == null)
            {
                throw new Exception("Error Finding Nutrient Source in "+ parentOrgan.Name + ":" + this.Parent.Parent.Parent.Name +  "SupplyFunctions." + this.Parent.Name + "." + this.Name);
            }
            return nutrientDelta;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parentOrgan = FindParentOrgan(this.Parent);
            var nutrientSource = FindPoolSource(this.Parent.Parent.Parent.Name, parentOrgan);

            //nutrientSource source should contain the current state at the beginning of the day (Live) of this Model's parent type ie: Carbon or Nitrogen
            //Live doesn't exists at Simulation start - so can't use it until the function is actually called
            if (this.Parent.Name.Equals("ReAllocation", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (true)
                {
                    case bool b when this.Name.Equals("Structural", StringComparison.InvariantCultureIgnoreCase): 
                        CalcValue = () => CalcAmountToReallocate(nutrientSource.Live.Structural); 
                        break;
                    case bool b when this.Name.Equals("Metabolic", StringComparison.InvariantCultureIgnoreCase):
                        CalcValue = () => CalcAmountToReallocate(nutrientSource.Live.Metabolic); 
                        break;
                    case bool b when this.Name.Equals("Storage", StringComparison.InvariantCultureIgnoreCase):
                        CalcValue = () => CalcAmountToReallocate(nutrientSource.Live.Storage); 
                        break;
                };
            }
            else if (this.Parent.Name.Equals("ReTranslocation", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (true)
                {
                    case bool b when this.Name.Equals("Structural", StringComparison.InvariantCultureIgnoreCase):
                        CalcValue = () => CalcAmountToReTranslocate(nutrientSource.Live.Structural); 
                        break;
                    case bool b when this.Name.Equals("Metabolic", StringComparison.InvariantCultureIgnoreCase):
                        CalcValue = () => CalcAmountToReTranslocate(nutrientSource.Live.Metabolic); 
                        break;
                    case bool b when this.Name.Equals("Storage", StringComparison.InvariantCultureIgnoreCase):
                        CalcValue = () => CalcAmountToReTranslocate(nutrientSource.Live.Storage); 
                        break;
                };
            }
        }
        private double CalcAmountToReallocate(double sourceAmount)
        {
            //sourceAmount ie: Leaf.Carbon.Live.Metabolic
            return sourceAmount * multiplier.Value() * parentOrgan.senescenceRate;
        }
        private double CalcAmountToReTranslocate(double sourceAmount)
        {
            //sourceAmount ie: Leaf.Carbon.Live.Metabolic
            return sourceAmount * multiplier.Value();
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return CalcValue();
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

                parentOrgan = FindParentOrgan(this.Parent);

                // add a description of the equation for this function
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = [" + parentOrgan.Name + "].maximumNconc × (["
                    + parentOrgan.Name + "].Live.Wt + potentialAllocationWt) - [" + parentOrgan.Name + "].Live.N</i>", indent));
                tags.Add(new AutoDocumentation.Paragraph("The demand for storage N is further reduced by a factor specified by the [" 
                    + parentOrgan.Name + "].NitrogenDemandSwitch.", indent));
            }
        }
    }
}
