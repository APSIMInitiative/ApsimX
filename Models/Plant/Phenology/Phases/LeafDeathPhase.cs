using System;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Struct;
using System.IO;
using Models.Functions;


namespace Models.PMF.Phen
{
    /// <summary>
    /// It proceeds until the last leaf on the main-stem has fully senessced.  Therefore its duration depends on the number of main-stem leaves that are produced and the rate at which they seness following final leaf appearance.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LeafDeathPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private Leaf leaf = null;

        [Link]
        private Structure structure = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------
        private double DeadNodeNoAtStart = 0;
        private bool First = true;


        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------
  
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                double F = (leaf.DeadCohortNo - DeadNodeNoAtStart) / (structure.FinalLeafNumber.Value() - DeadNodeNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return F;
            }
            set
            {
                throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
        }

        /// <summary>Gets the tt for today.</summary>
        public double TTForToday { get; set; }

        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        [XmlIgnore]
        public double TTinPhase { get; set; }


        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public double DoTimeStep(double PropOfDayToUse)
        {
            TTForToday = structure.ThermalTime.Value() * PropOfDayToUse;
            TTinPhase += TTForToday;

            if (First)
            {
                DeadNodeNoAtStart = leaf.DeadCohortNo;
                First = false;
            }

            if ((leaf.DeadCohortNo >= structure.FinalLeafNumber.Value()) || (leaf.CohortsInitialised == false))
                return 0.00001;
            else
                return 0;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            TTinPhase = 0;
            DeadNodeNoAtStart = 0;
            First = true;
        }

        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer) { writer.WriteLine("      " + Name); }
              
        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
    }
}

      
      
