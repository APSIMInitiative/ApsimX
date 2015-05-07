using System.Xml.Serialization;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Reflection;
using System.Linq;

namespace Models.Core
{


    //=========================================================================
    /// <summary>A generic system that can have children</summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class Zone : Model
    {
        /// <summary>Area of the zone.</summary>
        /// <value>The area.</value>
        [Description("Area of zone (ha)")]
        virtual public double Area { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        /// <value>The slope.</value>
        [Description("Slope (deg)")]
        public double Slope { get; set; }


        /// <summary>Gets an array of plant models that are in scope.</summary>
        /// <value>The plants.</value>
        [XmlIgnore]
        public List<ICrop2> Plants
        {
            get
            {
                var plants = new List<ICrop2>();
                foreach (var plant in Apsim.FindAll(this, typeof(ICrop2)))
                {
                    plants.Add(plant as ICrop2);
                }

                return plants;
            }
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
        private Locater Locator()
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