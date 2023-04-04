using Models.Core;
using Models.Functions;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Vernalisation model
    /// </summary>
    [Serializable]
    [Description("Adds the number of vernalising minus devernalising days between start and end phases")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class Vernalisation : Model
    {
        [Link]
        Phenology phenology = null;

        [Link(Type = LinkType.Child, ByName = true)]
        SubDailyInterpolation vernalisingDays = null;

        [Link(Type = LinkType.Child, ByName = true)]
        SubDailyInterpolation DevernalisingDays = null;

        [Link(Type = LinkType.Child, ByName = true)]
        Constant DaysToStabilise = null;

        private int startStageIndex;

        private int endStageIndex;

        /// <summary>Record of vernalising days during stabilisation period</summary>
        private double[] vernalisingRecord;

        /// <summary>The start stage</summary>
        [Description("Stage name to start accumulating vernalising days")]
        public string StartStage { get; set; }

        /// <summary>The end stage</summary>
        [Description("Stage name to stop accumulating vernalising days")]
        public string EndStage { get; set; }

        /// <summary>The end stage</summary>
        [Description("Stage name to reset the number of vernalising days")]
        public string ResetStage { get; set; }

        /// <summary>Gets the value vernalisation days.</summary>
        [JsonIgnore]
        public double TodaysVernalisation { get; set; }

        /// <summary>Gets the cummulative number of days vernalised.</summary>
        [JsonIgnore]
        public double DaysVernalised { get; set; }

        /// <summary>Gets the value number of days under temporary vernalisation.</summary>
        [JsonIgnore]
        public double DaysVernalising { get; set; }

 
        /// <summary>Compute the vernalisation</summary>
        public void DoVernalisation()
        {
            // get today's vernalisation
            TodaysVernalisation = vernalisingRecord[0];
            DaysVernalised += TodaysVernalisation;

            // get today's devernalisation
            double todaysDevernalisation = DevernalisingDays.Value();

            // update the temporary vernalisation record
            int i;
            for (i = 0; i < (int)DaysToStabilise.FixedValue - 1; i++)
                vernalisingRecord[i] = vernalisingRecord[i + 1];

            vernalisingRecord[i] = vernalisingDays.Value();

            // account for any devernalisation
            if (todaysDevernalisation > vernalisingRecord.Sum())
            {
                DaysVernalising = 0.0;
                Array.Clear(vernalisingRecord, 0, (int)DaysToStabilise.FixedValue);
            }
            else
            {
                DaysVernalising = Math.Max(0.0, vernalisingRecord.Sum() + todaysDevernalisation);
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

        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (vernalisingDays == null)
                throw new ApsimXException(this, "Cannot find VernalisingDays");

            vernalisingRecord = new double[(int)DaysToStabilise.FixedValue];
            DaysVernalised = 0.0;
            startStageIndex = phenology.StartStagePhaseIndex(StartStage);
            endStageIndex = phenology.EndStagePhaseIndex(EndStage);
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (phenology.Between(startStageIndex, endStageIndex))
                DoVernalisation();
        }

        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == EndStage)
                TodaysVernalisation = 0.0;

            if (phaseChange.StageName == ResetStage)
            {
                DaysVernalised = 0.0;
                DaysVernalising = 0.0;
            }
        }
    }
}
