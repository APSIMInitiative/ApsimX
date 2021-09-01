using System;
using System.Collections;
using Models.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using Models.PMF.Organs;

namespace Models.PMF
{
    /// <summary>
    /// This is a composite biomass class, representing the sum of 1 or more biomass objects.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeStates : OrganNutrientStates, ICustomDocumentation
    {
        private List<OrganNutrientStates> components = new List<OrganNutrientStates>();

        /// <summary> The concentraion of carbon in total dry weight</summary>
        [Description("Carbon Concentration of biomass")]
        public new double CarbonConcentration { get; set; } = 0.4;

        /// <summary>List of Organ states to include in composite state</summary>
        [Description("List of organs to agregate into composite biomass.")]
        public string[] Propertys { get; set; }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (string PropertyName in Propertys)
            {
                OrganNutrientStates c = (OrganNutrientStates)(this.FindByPath(PropertyName)?.Value);
                if (c == null)
                    throw new Exception("Cannot find: " + PropertyName + " in composite state: " + this.Name);
            }
        }

        /// <summary>/// Add components together to give composite/// </summary>

        [EventSubscribe("PartitioningComplete")]
        public void onPartitioningComplete(object sender, EventArgs e)
        {
            Clear();
            foreach (string PropertyName in Propertys)
                {
                    OrganNutrientStates c = (OrganNutrientStates)(this.FindByPath(PropertyName)?.Value);
                    AddDelta(c);
                }
        }

        /// <summary>/// The constructor </summary>
        public CompositeStates() : base(0.4){ }
        
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name + " Biomass", headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in this.FindAllChildren<IModel>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);

                tags.Add(new AutoDocumentation.Paragraph(this.Name + " summarises the following biomass objects:", indent));
            }
        }
    }
}