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
        [Link]
        private Phenology phenology = null;

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
        public double TTForTimeStep { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get { return 0.0; }
        }

        /// <summary>Thermal time target.</summary>
        [XmlIgnore]
        public double Target { get { return 0; } }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double PropOfDayToUse)
        {
            TTForTimeStep = phenology.ThermalTime.Value() * PropOfDayToUse;
            TTinPhase += TTForTimeStep;
            
            return false;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()  { TTinPhase = 0; }
        

        /// <summary>Writes the summary.</summary>
        public void WriteSummary(TextWriter writer)  { writer.WriteLine("      " + Name); }
        
    }
}

      
      
