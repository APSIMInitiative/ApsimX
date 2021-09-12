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
    [Description("This function calculates supplies of nutrients from metabolic or storage pools")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(NutrientPoolFunctions))]
    public class MobilisationSupplyFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        private double multiplier { get; set; }

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
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            multiplier = 1.0;

            foreach (IFunction F in ChildFunctions)
                multiplier *= F.Value(arrayIndex);

            if (this.Parent.Parent.Parent.Name == "Nitrogen")
            {
                if (this.Parent.Name == "ReAllocation")
                {
                    if (this.Name == "Metabolic")
                    {
                        return parentOrgan.Live.Nitrogen.Metabolic * Math.Max(0, Math.Min(1, multiplier)) * parentOrgan.senescenceRate;
                    }
                    if (this.Name == "Storage")
                    {
                        return parentOrgan.Live.Nitrogen.Storage * Math.Max(0, Math.Min(1, multiplier)) * parentOrgan.senescenceRate;
                    }
                }
                if (this.Parent.Name == "ReTranslocation")
                {
                    if (this.Name == "Metabolic")
                    {
                        return parentOrgan.Live.Nitrogen.Metabolic * Math.Max(0, Math.Min(1, multiplier));
                    }
                    if (this.Name == "Storage")
                    {
                        return parentOrgan.Live.Nitrogen.Storage * Math.Max(0, Math.Min(1, multiplier));
                    }
                }
            }

            if (this.Parent.Parent.Parent.Name == "Carbon")
            {
                if (this.Parent.Name == "ReAllocation")
                {
                    if (this.Name == "Metabolic")
                    {
                        return parentOrgan.Live.Carbon.Metabolic * Math.Max(0, Math.Min(1, multiplier)) * parentOrgan.senescenceRate;
                    }
                    if (this.Name == "Storage")
                    {
                        return parentOrgan.Live.Carbon.Storage * Math.Max(0, Math.Min(1, multiplier)) * parentOrgan.senescenceRate;
                    }
                }
                if (this.Parent.Name == "ReTranslocation")
                {
                    if (this.Name == "Metabolic")
                    {
                        return parentOrgan.Live.Carbon.Metabolic * Math.Max(0, Math.Min(1, multiplier));
                    }
                    if (this.Name == "Storage")
                    {
                        return parentOrgan.Live.Carbon.Storage * Math.Max(0, Math.Min(1, multiplier));
                    }
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
