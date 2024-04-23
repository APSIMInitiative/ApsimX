﻿using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Functions;

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
    public class MobilisationSupplyFunction : Model, IFunction
    {
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

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parentOrgan = FindParentOrgan(this.Parent);
            var childFunctions = FindAllChildren<IFunction>().ToList();
            var lookup = new Dictionary<string, Dictionary<string, Dictionary<string, DeficitCalculation>>>(StringComparer.InvariantCultureIgnoreCase);

            var carbonReAllocationFuntions = new Dictionary<string, DeficitCalculation>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"Structural", () => CalcAmountToReallocate(parentOrgan.Live.Carbon.Structural, childFunctions) },
                {"Metabolic", () => CalcAmountToReallocate(parentOrgan.Live.Carbon.Metabolic, childFunctions) },
                {"Storage", () => CalcAmountToReallocate(parentOrgan.Live.Carbon.Storage, childFunctions) },
            };
            var nitrogenReAllocationFunctions = new Dictionary<string, DeficitCalculation>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"Structural", () => CalcAmountToReallocate(parentOrgan.Live.Nitrogen.Structural, childFunctions) },
                {"Metabolic", () => CalcAmountToReallocate(parentOrgan.Live.Nitrogen.Metabolic, childFunctions) },
                {"Storage", () => CalcAmountToReallocate(parentOrgan.Live.Nitrogen.Storage, childFunctions) },
            };
            var carbonReTranslocationFuntions = new Dictionary<string, DeficitCalculation>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"Structural", () => CalcAmountToReTranslocate(parentOrgan.Live.Carbon.Structural, childFunctions) },
                {"Metabolic", () => CalcAmountToReTranslocate(parentOrgan.Live.Carbon.Metabolic, childFunctions) },
                {"Storage", () => CalcAmountToReTranslocate(parentOrgan.Live.Carbon.Storage, childFunctions) },
            };
            var nitrogenReTranslocationFuntions = new Dictionary<string, DeficitCalculation>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"Structural", () => CalcAmountToReTranslocate(parentOrgan.Live.Nitrogen.Structural, childFunctions) },
                {"Metabolic", () => CalcAmountToReTranslocate(parentOrgan.Live.Nitrogen.Metabolic, childFunctions) },
                {"Storage", () => CalcAmountToReTranslocate(parentOrgan.Live.Nitrogen.Storage, childFunctions) },
            };

            lookup.Add("ReAllocation", new Dictionary<string, Dictionary<string, DeficitCalculation>>(StringComparer.InvariantCultureIgnoreCase));
            lookup["ReAllocation"].Add("Carbon", carbonReAllocationFuntions);
            lookup["ReAllocation"].Add("Nitrogen", nitrogenReAllocationFunctions);

            lookup.Add("ReTranslocation", new Dictionary<string, Dictionary<string, DeficitCalculation>>(StringComparer.InvariantCultureIgnoreCase));
            lookup["ReTranslocation"].Add("Carbon", carbonReTranslocationFuntions);
            lookup["ReTranslocation"].Add("Nitrogen", nitrogenReTranslocationFuntions);

            CalcValue = lookup[Parent.Name][this.Parent.Parent.Parent.Name][this.Name];

        }
        private double CalcAmountToReallocate(double sourceAmount, List<IFunction> childFunctions)
        {
            var multiplier = 1.0;
            foreach (IFunction F in childFunctions)
                multiplier *= F.Value();
            multiplier = Math.Max(0, Math.Min(1, multiplier));

            //sourceAmount ie: Leaf.Carbon.Live.Metabolic
            double reAllocationsupply = sourceAmount * multiplier * parentOrgan.senescenceRate;
            if (reAllocationsupply < -1e-12)
                throw new Exception(this.FullPath + " gave a negative ReAllocation supply");
            return reAllocationsupply;
        }
        private double CalcAmountToReTranslocate(double sourceAmount, List<IFunction> childFunctions)
        {
            var multiplier = 1.0;
            foreach (IFunction F in childFunctions)
                multiplier *= F.Value();
            multiplier = Math.Max(0, Math.Min(1, multiplier));

            //sourceAmount ie: Leaf.Carbon.Live.Metabolic
            double reTranslocationSupply = sourceAmount * multiplier;
            if (reTranslocationSupply < 0)
                throw new Exception(this.FullPath + " gave a negative ReTranslocation supply");
            return reTranslocationSupply;
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
