using System;
using System.Xml.Serialization;
using Models.Core;

namespace Models
{
    [Serializable]
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
        public override void OnCommencing()
        {
            Today = StartDate;
        }

        /// <summary>
        /// An event handler to signal start of a simulation.
        /// </summary>
        [EventSubscribe("Commenced")]
        private void OnCommenced(object sender, EventArgs e)
        {
            System.ComponentModel.BackgroundWorker bw = sender as System.ComponentModel.BackgroundWorker;

            while (Today <= EndDate)
            {
                // If this is being run on a background worker thread then check for cancellation
                if (bw != null && bw.CancellationPending)
                {
                    Summary.WriteMessage(FullPath, "Simulation cancelled");
                    return;
                }

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


            Summary.WriteMessage(FullPath, "Simulation terminated normally");
        }
    }
}