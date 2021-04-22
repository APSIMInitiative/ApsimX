using System;
using System.Collections;
using Models.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Models.PMF
{
    /// <summary>
    /// This is a composite biomass class, representing the sum of 1 or more biomass objects.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeBiomass : Biomass, ICustomDocumentation
    {
        /// <summary>The propertys</summary>
        [Description("List of organs to agregate into composite biomass")]
        public string[] Propertys {get; set;}

        /// <summary>Update this biomass object.</summary>
        public void Update()
        {
            base.Clear();

            foreach (string PropertyName in Propertys)
            {
                object v = this.FindByPath(PropertyName)?.Value;
                if (v == null)
                    throw new Exception("Cannot find: " + PropertyName + " in composite biomass: " + this.Name);

                if (v is IEnumerable)
                {
                    foreach (object i in v as IEnumerable)
                    {
                        if (i is CompositeBiomass)
                            (i as CompositeBiomass).Update();
                        if (!(i is Biomass))
                            throw new Exception("Elements in the array: " + PropertyName + " are not Biomass objects in composite biomass: " + this.Name);
                        Add(i as Biomass);
                    }
                }
                else
                {
                    if (!(v is Biomass))
                        throw new Exception("Property: " + PropertyName + " is not a Biomass object in composite biomass: " + this.Name);
                    if (v is CompositeBiomass)
                        (v as CompositeBiomass).Update();
                    Add(v as Biomass);
                }
            }
        }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Gets or sets the non structural n.</summary>
        /// <value>The non structural n.</value>
        /// <exception cref="System.Exception">Cannot set StorageN in CompositeBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        override public double StorageN
        {
            get { Update(); return base.StorageN; }
            set { throw new Exception("Cannot set StorageN in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the structural n.</summary>
        /// <value>The structural n.</value>
        /// <exception cref="System.Exception">Cannot set StructuralN in CompositeBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        override public double StructuralN
        {
            get { Update(); return base.StructuralN; }
            set { throw new Exception("Cannot set StructuralN in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the non structural wt.</summary>
        /// <value>The non structural wt.</value>
        /// <exception cref="System.Exception">Cannot set StorageWt in CompositeBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        override public double StorageWt
        {
            get { Update(); return base.StorageWt; }
            set { throw new Exception("Cannot set StorageWt in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the structural wt.</summary>
        /// <value>The structural wt.</value>
        /// <exception cref="System.Exception">Cannot set StructuralWt in CompositeBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        override public double StructuralWt
        {
            get { Update(); return base.StructuralWt; }
            set { throw new Exception("Cannot set StructuralWt in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the metabolic n.</summary>
        /// <value>The metabolic n.</value>
        /// <exception cref="System.Exception">Cannot set MetabolicN in CompositeBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        override public double MetabolicN
        {
            get { Update(); return base.MetabolicN; }
            set { throw new Exception("Cannot set MetabolicN in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the metabolic wt.</summary>
        /// <value>The metabolic wt.</value>
        /// <exception cref="System.Exception">Cannot set MetabolicWt in CompositeBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        override public double MetabolicWt
        {
            get { Update(); return base.MetabolicWt; }
            set { throw new Exception("Cannot set MetabolicWt in CompositeBiomass"); }
        }

        /// <summary>Gets the wt.</summary>
        /// <value>The wt.</value>
        [Units("g/m^2")]
        override public double Wt
        {
            get
            {
                Update();
                return _StructuralWt + _StorageWt + _MetabolicWt;
            }
        }


        /// <summary>Gets the n.</summary>
        /// <value>The n.</value>
        [Units("g/m^2")]
        override public double N
        {
            get
            {
                Update();
                return _StructuralN + _StorageN + _MetabolicN;
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
                if (Propertys != null)
                    foreach (string PropertyName in Propertys)
                        st += Environment.NewLine + "* " + PropertyName;
                tags.Add(new AutoDocumentation.Paragraph(st, indent));
            }
        }
    }
}