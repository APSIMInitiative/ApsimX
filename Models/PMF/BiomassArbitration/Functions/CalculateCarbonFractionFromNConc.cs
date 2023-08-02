using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{
    /// <summary>
    /// Calculates the Deficit of a given labile nutrient pool and returns it to use for a demand.
    /// </summary>
    [Serializable]
    [Description("This function calculates demands for metabolic and storage pools based on the size of the potential deficits of these pools.  For nutrients is uses maximum, critical and minimum concentration thresholds and for carbon it uses structural, metabolic and storage partitioning proportions to ")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(NutrientProportionFunctions))]
    public class CalculateCarbonFractionFromNConc : Model, IFunction
    {
        /// <summary>Value to multiply demand for.  Use to switch demand on and off</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        [Description("Multiplies calculated demand.  Use to switch demand on and off")]
        [Units("unitless")]
        public IFunction multiplier = null;

        private Organ parentOrgan = null;
        private OrganNutrientDelta dNitrogen = null;

        // Declare a delegate type for calculating the IFunction return value:
        private delegate double DeficitCalculation();
        private DeficitCalculation CalcFraction;

        private Organ FindParentOrgan(IModel model)
        {
            if (model is Organ) return model as Organ;

            if (model is IPlant)
                throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");

            return FindParentOrgan(model.Parent);
        }
        private OrganNutrientDelta FindNutrientDelta(string NutrientType, Organ parentOrgan)
        {
            var nutrientDelta = parentOrgan.FindChild(NutrientType) as OrganNutrientDelta;
            //should we throw an exception if the delta is missing?
            return nutrientDelta;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parentOrgan = FindParentOrgan(this.Parent);
            dNitrogen = FindNutrientDelta("Nitrogen", parentOrgan);

            //setup a function pointer (delegate) at simulation commencing so it isn't performing multiple if statements every day
            //cleaner method would probably be to use classes
            var nutrientName = this.Parent.Parent.Name;
            switch (this.Name)
            {
                case "Structural":
                    CalcFraction = calcStructuralFraction;
                    break;
                case "Metabolic":
                    CalcFraction = calcMetabolicFraction;
                    break;
                case "Storage":
                    CalcFraction = calcStorageFraction;
                    break;
            }
        }

        private double calcStructuralFraction()
        {
            return MathUtilities.Divide(dNitrogen.ConcentrationOrFraction.Structural, dNitrogen.ConcentrationOrFraction.Storage, 0);
        }

        private double calcMetabolicFraction()
        {
            double critFrac = MathUtilities.Divide(dNitrogen.ConcentrationOrFraction.Metabolic, dNitrogen.ConcentrationOrFraction.Storage, 0);
            return critFrac - calcStructuralFraction();
        }

        private double calcStorageFraction()
        {
            return 1.0 - calcStructuralFraction() - calcMetabolicFraction();
        }




        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double fraction = CalcFraction();

            if (Double.IsNaN(fraction))
                throw new Exception(this.FullPath + " Must be named Metabolic or Structural to represent the pool it is parameterising and be placed on a NutrientDemand Object which is on Carbon or Nitrogen OrganNutrienDeltaObject");
            return fraction;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
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
