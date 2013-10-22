using System;
using System.Xml.Serialization;
using Models.Core;

namespace Models
{
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [AllowDropOn("Simulation")]
    public class Clock : Model
    {
        // Links
        [Link]
        private ISummary Summary = null;

        [Description("The start date of the simulation")]
        public DateTime StartDate { get; set; }

        [Description("The end date of the simulation")]
        public DateTime EndDate { get; set; }

        // Public events that we're going to publish.
        public event EventHandler Tick;
        public event EventHandler StartOfDay;
        public event EventHandler MiddleOfDay;
        public event EventHandler EndOfDay;

        // Public properties available to other models.
        [XmlIgnore]
        public DateTime Today { get; private set; }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            Today = StartDate;
            Summary.WriteProperty("Start date", StartDate.ToString());
            Summary.WriteProperty("End date", EndDate.ToString());
        }

        /// <summary>
        /// An event handler to signal start of a simulation.
        /// </summary>
        [EventSubscribe("Commenced")]
        private void OnCommenced(object sender, EventArgs e)
        {
            while (Today <= EndDate)
            {
                if (Tick != null)
                    Tick.Invoke(this, new EventArgs());
                if (StartOfDay != null)
                    StartOfDay.Invoke(this, new EventArgs());
                if (MiddleOfDay != null)
                    MiddleOfDay.Invoke(this, new EventArgs());
                if (EndOfDay != null)
                    EndOfDay.Invoke(this, new EventArgs());

                Today = Today.AddDays(1);
            }

            //DataStore.WriteMessage(Simulation.Name, "Clock", "Simulation terminated normally", Today);
            Summary.WriteMessage("Simulation terminated normally");
        }
    }
}