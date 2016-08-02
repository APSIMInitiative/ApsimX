using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for land
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Land))]
    public class LandType : Model
    {

        [Link]
        ISummary Summary = null;


        event EventHandler LandChanged;


        /// <summary>
        /// Total Area (ha)
        /// </summary>
        [Description("Land Area (ha)")]
        public double LandArea { get; set; }


        /// <summary>
        /// Unusable Portion - Buildings, paths etc. (%)
        /// </summary>
        [Description("Buildings - proportion taken up with bldgs, paths (%)")]
        public double UnusablePortion { get; set; }

        /// <summary>
        /// Portion Bunded (%)
        /// </summary>
        [Description("Portion bunded (%)")]
        public double BundedPortion { get; set; }

        /// <summary>
        /// Soil Type (1-5) 
        /// </summary>
        [Description("Soil Type (1-5)")]
        public int SoilType { get; set; }

        /// <summary>
        /// Fertility - N Decline Yield
        /// </summary>
        [Description("Fertility - N Decline yld")]
        public double NDecline { get; set; }



        /// <summary>
        /// Area not currently being used (ha)
        /// </summary>
        [XmlIgnore]
        public double AreaAvailable { get { return _AreaAvailable; } }

        private double _AreaAvailable;



        /// <summary>
        /// Area already used (ha)
        /// </summary>
        [XmlIgnore]
        public double AreaUsed { get { return this.LandArea - _AreaAvailable; } }





        /// <summary>
        /// Add Available Land
        /// </summary>
        /// <param name="AddAmount"></param>
        public void Add(double AddAmount)
        {
            if (this._AreaAvailable + AddAmount > this.LandArea)
            {
                string message = "Tried to add more available land to " + this.Name + " than exists." + Environment.NewLine
                    + "Current Amount: " + this._AreaAvailable + Environment.NewLine
                    + "Tried to Remove: " + AddAmount;
                Summary.WriteWarning(this, message);
                this._AreaAvailable = this.LandArea;
            }
            else
            {
                this._AreaAvailable = this._AreaAvailable + AddAmount;
            }

            if (LandChanged != null)
                LandChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Remove Available Land
        /// </summary>
        /// <param name="RemoveAmount"></param>
        public void Remove(double RemoveAmount)
        {
            if (this._AreaAvailable - RemoveAmount < 0)
            {
                string message = "Tried to remove more available land to " + this.Name + " than exists." + Environment.NewLine
                    + "Current Amount: " + this._AreaAvailable + Environment.NewLine
                    + "Tried to Remove: " + RemoveAmount;
                Summary.WriteWarning(this, message);
                this._AreaAvailable = 0;
            }
            else
            {
                this._AreaAvailable = this._AreaAvailable - RemoveAmount;
            }

            if (LandChanged != null)
                LandChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Set Amount of Available Land
        /// </summary>
        /// <param name="NewValue"></param>
        public void Set(double NewValue)
        {
            if ((NewValue < 0) || (NewValue > this.LandArea))
            {
                Summary.WriteMessage(this, "Tried to Set Available Land to Invalid New Amount." + Environment.NewLine
                    + "New Value must be between 0 and the Land Area.");
            }
            else
            {
                this._AreaAvailable = NewValue;

                if (LandChanged != null)
                    LandChanged.Invoke(this, new EventArgs());
            }
        }


        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
            this._AreaAvailable = this.LandArea;
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