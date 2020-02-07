namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// # [Name]
    /// A generic system that can have children
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Agroforestry.AgroforestrySystem))]
    [ScopedModel]
    public class Zone : Model, ICustomDocumentation
    {
        /// <summary>Area of the zone.</summary>
        [Description("Area of zone (ha)")]
        virtual public double Area { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        [Description("Slope angle (degrees)")]
        virtual public double Slope { get; set; }

        /// <summary>Angle of the aspect, from south (degrees).</summary>
        [Description("Aspect (degrees from north)")]
        public double AspectAngle { get; set; }

        /// <summary>Local altitude (meters above sea level).</summary>
        [Description("Local altitude (meters above sea level)")]
        public double Altitude { get; set; } = 50;

        /// <summary>Return a list of plant models.</summary>
        [XmlIgnore]
        public List<IPlant> Plants { get { return Apsim.Children(this, typeof(IPlant)).Cast<IPlant>().ToList(); } }

        /// <summary>Return the index of this paddock</summary>
        public int Index {  get { return Parent.Children.IndexOf(this); } }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (Area <= 0)
                throw new Exception("Zone area must be greater than zero.  See Zone: " + Name);
        }

        /// <summary>Gets the value of a variable or model.</summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            return Locator().Get(namePath, this);
        }

        /// <summary>Get the underlying variable object for the given path.</summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetVariableObject(string namePath)
        {
            return Locator().GetInternal(namePath, this);
        }

        /// <summary>Sets the value of a variable. Will throw if variable doesn't exist.</summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            Locator().Set(namePath, this, value);
        }


        /// <summary>Gets the locater model for the specified model.</summary>
        /// <returns>The an instance of a locater class for the specified model. Never returns null.</returns>
        public Locater Locator()
        {
            var simulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
            if (simulation == null)
            {
                // Simulation can be null if this model is not under a simulation e.g. DataStore.
                return new Locater();
            }
            else
            {
                return simulation.Locater;
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public virtual void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // document children
                foreach (IModel child in Children)
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}