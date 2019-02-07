using System.Xml.Serialization;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Reflection;
using System.Linq;
using Models;

namespace Models.Core
{
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
    public class Zone : Model
    {
        /// <summary>Area of the zone.</summary>
        /// <value>The area.</value>
        [Description("Area of zone (ha)")]
        virtual public double Area { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        /// <value>The slope.</value>
        [Description("Slope (deg)")]
        virtual public double Slope { get; set; }

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

    }
}