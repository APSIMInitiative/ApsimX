using System;
using System.Xml.Serialization;
using Model.Core;

namespace Model.Components
{
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [AllowDropOn("Simulation")]
    public class Clock
    {
        // Links
        [Link] private Simulation Simulation = null;
        [Link] private ISummary Summary = null;

        // Parameters serialised
        public string Name { get; set; }
        
        [Description("The start date of the simulation")]
        public DateTime StartDate { get; set; }

        [Description("The end date of the simulation")]
        public DateTime EndDate { get; set; }
        
        // Public events that we're going to publish.
        public delegate void TimeDelegate(DateTime Data);
        public event TimeDelegate Tick;
        public event NullTypeDelegate StartOfDay;
        public event NullTypeDelegate MiddleOfDay;
        public event NullTypeDelegate EndOfDay;

        // Public properties available to other models.
        [XmlIgnore]
        public DateTime Today { get; private set; }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        public void OnInitialised()
        {
            Simulation.Commenced += OnCommence;
            Simulation.Completed += OnCompleted;
            Today = StartDate;
            Summary.WriteProperty("Start date", StartDate.ToString());
            Summary.WriteProperty("End date", EndDate.ToString());
        }

        /// <summary>
        /// An event handler to signal start of a simulation.
        /// </summary>
        private void OnCommence()
        {
            while (Today <= EndDate)
            {
                if (Tick != null)
                    Tick.Invoke(Today);
                if (StartOfDay != null)
                    StartOfDay.Invoke();
                if (MiddleOfDay != null)
                    MiddleOfDay.Invoke();
                if (EndOfDay != null)
                    EndOfDay.Invoke();

                Today = Today.AddDays(1);
            }

            //DataStore.WriteMessage(Simulation.Name, "Clock", "Simulation terminated normally", Today);
            Summary.WriteMessage("Simulation terminated normally");
        }

        /// <summary>
        /// An event handler to perform model cleanup.
        /// </summary>
        private void OnCompleted()
        {
            Simulation.Commenced -= OnCommence;
            Simulation.Completed -= OnCompleted;
        }
    }
}