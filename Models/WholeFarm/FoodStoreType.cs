using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a Home Food Store type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(FoodStore))]
    public class FoodStoreType : Model, IResourceType
    {
        [Link]
        ISummary Summary = null;


        event EventHandler FoodStoreChanged;


        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        [Description("Dry Matter (%)")]
        public double DryMatter { get; set; }


        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        public double DMD { get; set; }



        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        public double Nitrogen { get; set; }



        /// <summary>
        /// Starting Age of the Fodder (Months)
        /// </summary>
        [Description("Starting Age of Human Food (Months)")]
        public double StartingAge { get; set; }


        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        public double StartingAmount { get; set; }


        /// <summary>
        /// Age of this Human Food (Months)
        /// </summary>
        [XmlIgnore]
        public double Age { get; set; } 


        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount { get { return _Amount; } }

        private double _Amount;




        /// <summary>
        /// Add Fodder
        /// </summary>
        /// <param name="AddAmount"></param>
        public void Add(double AddAmount)
        {
            this._Amount = this._Amount + AddAmount;

            if (FoodStoreChanged != null)
                FoodStoreChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Remove Fodder
        /// </summary>
        /// <param name="RemoveAmount"></param>
        public void Remove(double RemoveAmount)
        {
            if (this._Amount - RemoveAmount < 0)
            {
                string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
                    + "Current Amount: " + this._Amount + Environment.NewLine
                    + "Tried to Remove: " + RemoveAmount;
                Summary.WriteWarning(this, message);
                this._Amount = 0;
            }
            else
            {
                this._Amount = this._Amount - RemoveAmount;
            }

            if (FoodStoreChanged != null)
                FoodStoreChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Set Amount of Fodder
        /// </summary>
        /// <param name="NewValue"></param>
        public void Set(double NewValue)
        {
            this._Amount = NewValue;

            if (FoodStoreChanged != null)
                FoodStoreChanged.Invoke(this, new EventArgs());
        }


        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
            this.Age = this.StartingAge;
            this._Amount = this.StartingAmount;
        }


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Initialise();
        }

    }



}