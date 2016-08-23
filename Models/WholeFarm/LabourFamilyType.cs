using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a person who can do labour 
    ///  who is a family member.
    /// eg. AdultMale, AdultFemale etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourFamily))]
    public class LabourFamilyType : Model
    {

        /// <summary>
        /// Get the Clock.
        /// </summary>
        [Link]
        Clock Clock = null;


        [Link]
        ISummary Summary = null;


        event EventHandler LabourChanged;


        /// <summary>
        /// Age in years.
        /// </summary>
        [Description("Initial Age")]
        public double InitialAge { get; set; }

        /// <summary>
        /// Male or Female
        /// </summary>
        [Description("Gender")]
        public string Gender { get; set; }

        /// <summary>
        /// Name of each column in the grid. Used as the column header.
        /// </summary>
        [Description("Column Names")]
        public string[] ColumnNames { get; set; }

        /// <summary>
        /// Maximum Labour Supply (in days) for each month of the year. 
        /// </summary>
        [Description("Max Labour Supply (in days) for each month of the year")]
        public double[] MaxLabourSupply { get; set; }






        /// <summary>
        /// Does this family member do Non Farm labour ?
        /// </summary>
        [Description("Does Non Farm Labour ?")]
        public bool DoesNonFarmLabour { get; set; }

        /// <summary>
        /// If this family member does Non Farm labour
        /// then what is their default Non Farm pay rate.
        /// </summary>
        [Description("Default Non Farm Pay rate")]
        public double DefaultNonFarmPayRate { get; set; }




        /// <summary>
        /// Age in years.
        /// </summary>
        [XmlIgnore]
        public double Age { get; set; }


        /// <summary>
        /// Available Labour (in days) in the current month. 
        /// </summary>
        [XmlIgnore]
        public double AvailableDays { get { return _AvailableDays; } }

        private double _AvailableDays;



        /// <summary>
        /// Add Fodder
        /// </summary>
        /// <param name="AddAmount"></param>
        public void Add(double AddAmount)
        {
            this._AvailableDays = this._AvailableDays + AddAmount;

            if (LabourChanged != null)
                LabourChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Remove Fodder
        /// </summary>
        /// <param name="RemoveAmount"></param>
        public void Remove(double RemoveAmount)
        {
            if (this._AvailableDays - RemoveAmount < 0)
            {
                string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
                    + "Current Amount: " + this._AvailableDays + Environment.NewLine
                    + "Tried to Remove: " + RemoveAmount;
                Summary.WriteWarning(this, message);
                this._AvailableDays = 0;
            }
            else
            {
                this._AvailableDays = this._AvailableDays - RemoveAmount;
            }

            if (LabourChanged != null)
                LabourChanged.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Set Amount of Fodder
        /// </summary>
        /// <param name="NewValue"></param>
        public void Set(double NewValue)
        {
            this._AvailableDays = NewValue;

            if (LabourChanged != null)
                LabourChanged.Invoke(this, new EventArgs());
        }


        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
            this.Age = this.InitialAge;
            ResetAvailabilityEachMonth();
        }


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Initialise();
        }


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            ResetAvailabilityEachMonth();
        }


        /// <summary>
        /// Reset the Available Labour (in days) in the current month 
        /// to the appropriate value for this month.
        /// </summary>
        private void ResetAvailabilityEachMonth()
        {
            int currentmonth = Clock.Today.Month;
            this._AvailableDays = this.MaxLabourSupply[currentmonth - 1];
        }


    }


}