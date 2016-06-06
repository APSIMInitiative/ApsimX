using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;

using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF
{
    /// <summary>
    /// A composite biomass i.e. a biomass made up of 1 or more biomass objects.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CompositeBiomass : Biomass
    {
        /// <summary>The propertys</summary>
        [Description("List of organs to agregate into composite biomass")]
        public string[] Propertys {get; set;}

        /// <summary>Update this biomass object.</summary>
        /// <exception cref="System.Exception">
        /// Cannot find:  + PropertyName +  in composite biomass:  + this.Name
        /// or
        /// Elements in the array:  + PropertyName +  are not Biomass objects in composition biomass:  + this.Name
        /// or
        /// Property:  + PropertyName +  is not a Biomass object in composition biomass:  + this.Name
        /// </exception>
        public void Update()
        {
            base.Clear();

            foreach (string PropertyName in Propertys)
            {
                object v = Apsim.Get(this, PropertyName);
                if (v == null)
                    throw new Exception("Cannot find: " + PropertyName + " in composite biomass: " + this.Name);

                if (v is IEnumerable)
                {
                    foreach (object i in v as IEnumerable)
                    {
                        if (!(i is Biomass))
                            throw new Exception("Elements in the array: " + PropertyName + " are not Biomass objects in composition biomass: " + this.Name);
                        Add(i as Biomass);
                    }
                }
                else
                {

                    if (!(v is Biomass))
                        throw new Exception("Property: " + PropertyName + " is not a Biomass object in composition biomass: " + this.Name);
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
        /// <exception cref="System.Exception">Cannot set NonStructuralN in CompositeBiomass</exception>
        [XmlIgnore]
        [Units("g/m^2")]
        override public double NonStructuralN
        {
            get { Update(); return base.NonStructuralN; }
            set { throw new Exception("Cannot set NonStructuralN in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the structural n.</summary>
        /// <value>The structural n.</value>
        /// <exception cref="System.Exception">Cannot set StructuralN in CompositeBiomass</exception>
        [XmlIgnore]
        [Units("g/m^2")]
        override public double StructuralN
        {
            get { Update(); return base.StructuralN; }
            set { throw new Exception("Cannot set StructuralN in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the non structural wt.</summary>
        /// <value>The non structural wt.</value>
        /// <exception cref="System.Exception">Cannot set NonStructuralWt in CompositeBiomass</exception>
        [XmlIgnore]
        [Units("g/m^2")]
        override public double NonStructuralWt
        {
            get { Update(); return base.NonStructuralWt; }
            set { throw new Exception("Cannot set NonStructuralWt in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the structural wt.</summary>
        /// <value>The structural wt.</value>
        /// <exception cref="System.Exception">Cannot set StructuralWt in CompositeBiomass</exception>
        [XmlIgnore]
        [Units("g/m^2")]
        override public double StructuralWt
        {
            get { Update(); return base.StructuralWt; }
            set { throw new Exception("Cannot set StructuralWt in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the metabolic n.</summary>
        /// <value>The metabolic n.</value>
        /// <exception cref="System.Exception">Cannot set MetabolicN in CompositeBiomass</exception>
        [XmlIgnore]
        [Units("g/m^2")]
        override public double MetabolicN
        {
            get { Update(); return base.MetabolicN; }
            set { throw new Exception("Cannot set MetabolicN in CompositeBiomass"); }
        }

        /// <summary>Gets or sets the metabolic wt.</summary>
        /// <value>The metabolic wt.</value>
        /// <exception cref="System.Exception">Cannot set MetabolicWt in CompositeBiomass</exception>
        [XmlIgnore]
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
                return _StructuralWt + _NonStructuralWt + _MetabolicWt;
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
                return _StructuralN + _NonStructuralN + _MetabolicN;
            }
        }

    }
}