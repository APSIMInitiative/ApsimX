using System;
using Models.Core;
using System.Xml.Serialization;
using System.IO;
using Models.Functions;

namespace Models.PMF.Phen
{

    /// <summary>It is the end phase in phenology and the crop will sit, unchanging, in this phase until it is harvested or removed by other method</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class EndPhase : Model, IPhase
    {
        /// <summary>The thermal time</summary>
        [Link]
        public IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Gets the t tin phase.</summary>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>Gets the tt for today.</summary>
        [XmlIgnore]
        public double TTForToday { get { return ThermalTime.Value(); } }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get { return 0.0; }
            set { throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it"); }
        }

        //6. Public methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public double DoTimeStep(double PropOfDayToUse)
        {
            TTinPhase += ThermalTime.Value();
            return 0;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { TTinPhase = 0; }

        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer)
        { writer.WriteLine("      " + Name); }
        
        //7. Private methods
        //-----------------------------------------------------------------------------------------------------------------
        
        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
    }
}

      
      
