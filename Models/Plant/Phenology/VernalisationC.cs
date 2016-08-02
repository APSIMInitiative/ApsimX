using Models.Core;
using Models.PMF.Functions;
using System;
using System.Linq;
using Models.Interfaces;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Vernalisation model
    /// </summary>
    [Serializable]
    [Description("Adds the number of vernalising minus devernalising days between start and end phases")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class VernalisationC : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The model providing the number of vernalising days</summary>
        [Link]
        AirTemperatureFunction VernalisingDays = null;

        /// <summary>The model providing the number of de-vernalising days</summary>
        [Link(IsOptional = true)]
        AirTemperatureFunction DevernalisingDays = null;

        /// <summary>Number of days to stabilise vernalisation</summary>
        [Link(IsOptional = true)]
        Constant DaysToStabilise = null;

        /// <summary>The start stage</summary>
        [Description("Stage name to start accumulating vernalising days")]
        public string StartStage { get; set; }

        /// <summary>The end stage</summary>
        [Description("Stage name to stop accumulating vernalising days")]
        public string EndStage { get; set; }

        /// <summary>The end stage</summary>
        [Description("Stage name to reset the number of vernalising days")]
        public string ResetStage { get; set; }

        /// <summary>The fraction of day effectivelly vernalised today</summary>
        private double vernalisedToday = 0.0;

        /// <summary>The cumulative number of vernalising days</summary>
        private double permanentVernalisedDays = 0.0;

        /// <summary>The amount of vernalising days within the stabilising period</summary>
        private double temporaryVernalisingDays = 0.0;

        /// <summary>Record of vernalising days during stabilisation period</summary>
        private double[] vernalisingRecord;

        private int numberOfDaysToStabilise = 1;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (VernalisingDays == null)
                throw new ApsimXException(this, "Cannot find VernalisingDays");

            if (DaysToStabilise != null)
                numberOfDaysToStabilise = (int)DaysToStabilise.Value;

            vernalisingRecord = new double[numberOfDaysToStabilise];
            permanentVernalisedDays = 0.0;
        }

        /// <summary>Trap the DoDailyInitialisation event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Phenology.Between(StartStage, EndStage))
                DoVernalisation();
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhaseChange">The phase change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            if (PhaseChange.EventStageName == EndStage)
                vernalisedToday = 0.0;

            if (PhaseChange.EventStageName == ResetStage)
            {
                permanentVernalisedDays = 0.0;
                temporaryVernalisingDays = 0.0;
            }
        }
        
        /// <summary>Gets the value vernalisation days.</summary>
        /// <value>The value.</value>
        public double TodaysVernalisation
        {
            get { return vernalisedToday; }
        }

        /// <summary>Gets the cummulative number of days vernalised.</summary>
        /// <value>The value.</value>
        public double DaysVernalised
        {
            get { return permanentVernalisedDays; }
        }

        /// <summary>Gets the value number of days under temporary vernalisation.</summary>
        /// <value>The value.</value>
        public double DaysVernalising
        {
            get { return temporaryVernalisingDays; }
        }

        /// <summary>Compute the vernalisation</summary>
        /// <remarks>
        /// This is the sum of days the plant experienced vernalising temperatures minus the
        /// days experienced at devernalising temperatures, providing that devernalising temperatures
        /// occured within a given stabilising period. At the end of the stabilising period the
        /// vernalisation effect is permanent.
        /// </remarks>
        /// <exception cref="ApsimXException">Cannot find VernalisingDays</exception>
        public void DoVernalisation()
        {
            // get today's vernalisation
            vernalisedToday = vernalisingRecord[0];
            permanentVernalisedDays += vernalisedToday;

            // get today's devernalisation
            double todaysDevernalisation = 0.0;
            if (DevernalisingDays != null)
                todaysDevernalisation = DevernalisingDays.Value;

            // update the temporary vernalisation record
            int i;
            for (i = 0; i < numberOfDaysToStabilise - 1; i++)
                vernalisingRecord[i] = vernalisingRecord[i + 1];

            vernalisingRecord[i] = VernalisingDays.Value;

            // account for any devernalisation
            if (todaysDevernalisation > vernalisingRecord.Sum())
            {
                temporaryVernalisingDays = 0.0;
                Array.Clear(vernalisingRecord, 0, numberOfDaysToStabilise);
            }
            else
            {
                temporaryVernalisingDays = Math.Max(0.0, vernalisingRecord.Sum() + todaysDevernalisation);
                while (todaysDevernalisation > 0.0)
                {
                    if (vernalisingRecord[i] - todaysDevernalisation < 0.0)
                    {
                        todaysDevernalisation -= vernalisingRecord[i];
                        vernalisingRecord[i] = 0.0;
                        if (i > 0)
                            i -= 1;
                        else
                            todaysDevernalisation = 0.0;
                    }
                    else
                    {
                        vernalisingRecord[i] -= todaysDevernalisation;
                        todaysDevernalisation = 0.0;
                    }
                }
            }
        }
    }
}
