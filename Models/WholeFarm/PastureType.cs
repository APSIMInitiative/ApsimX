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
    [ValidParent(ParentType = typeof(Pasture))]
    public class PastureType : Model
    {
        [Link]
        ISummary Summary = null;

        event EventHandler PastureChanged;

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
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        public double StartingAmount { get; set; }




        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount { get { return _Amount; } }

        private double _Amount;



        /// <summary>
        /// Add Pasture
        /// </summary>
        /// <param name="AddAmount"></param>
        public void Add(double AddAmount)
        {
            this._Amount = this._Amount + AddAmount;

            if (PastureChanged != null)
                PastureChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Remove Pasture
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

                if (PastureChanged != null)
                    PastureChanged.Invoke(this, new EventArgs());
            }
            else
            {
                this._Amount = this._Amount - RemoveAmount;

                if (PastureChanged != null)
                    PastureChanged.Invoke(this, new EventArgs());
            }

        }

        /// <summary>
        /// Set Amount of Pasture
        /// </summary>
        /// <param name="NewValue"></param>
        public void Set(double NewValue)
        {
            this._Amount = NewValue;

            if (PastureChanged != null)
                PastureChanged.Invoke(this, new EventArgs());
        }




        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
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