using System;
using System.Collections;
using Models.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using Models.PMF.Interfaces;
using System.Linq;

namespace Models.PMF
{
    /// <summary>This is a composite biomass class, representing the sum of 1 or more biomass objects from one or more organs.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeBiomass : Model, ICustomDocumentation
    {
        private List<IOrganDamage> organs;

        /// <summary>List of organs to agregate.</summary>
        [Description("List of organs to agregate.")]
        public string[] OrganNames {get; set;}

        /// <summary>Include live material?</summary>
        [Description("Include live material?")]
        public bool IncludeLive { get; set; }

        /// <summary>Include dead material?</summary>
        [Description("Include dead material?")]
        public bool IncludeDead { get; set; }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            organs = new List<IOrganDamage>();
            var parentPlant = Parent as Plant;
            if (parentPlant == null)
                throw new Exception("CompositeBiomass can only be dropped on a plant.");
            foreach (var organName in OrganNames)
            {
                var organ = parentPlant.Organs.FirstOrDefault(o => o.Name == organName);
                if (organ == null && !(organ is IOrganDamage))
                    throw new Exception($"In {Name}, cannot find a plant organ called {organName}");
                organs.Add(organ as IOrganDamage);
            }
        }

        /// <summary>Gets the mass.</summary>
        [Units("g/m^2")]
        public double Wt
        {
            get
            {
                double wt = 0;
                foreach (var organ in organs)
                {
                    if (IncludeLive)
                        wt += organ.Live.Wt;
                    if (IncludeDead)
                        wt += organ.Dead.Wt;
                }

                return wt;
            }
        }

        /// <summary>Gets the nitrogen content.</summary>
        [Units("g/m^2")]
        public double N
        {
            get 
            {
                double n = 0;
                foreach (var organ in organs)
                {
                    if (IncludeLive)
                        n += organ.Live.StorageN;
                    if (IncludeDead)
                        n += organ.Dead.StorageN;
                }

                return n; 
            }
        }

        /// <summary>Gets the nitrogen concentration.</summary>
        [Units("g/g")]
        public double NConc
        {
            get
            {
                if (Wt > 0)
                    return N / Wt;
                else
                    return 0.0;
            }
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
                tags.Add(new AutoDocumentation.Heading(Name + " Biomass", headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in this.FindAllChildren<IModel>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);

                tags.Add(new AutoDocumentation.Paragraph(this.Name + " summarises the following biomass objects:", indent));

                string st = string.Empty;
                if (OrganNames != null)
                    foreach (string organName in OrganNames)
                        st += Environment.NewLine + "* " + organName;
                tags.Add(new AutoDocumentation.Paragraph(st, indent));
            }
        }
    }
}