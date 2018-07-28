using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.Xml.Serialization;
using System.IO;
using APSIM.Shared.Utilities;
using System.Data;
using System.Linq;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model simulates the development of the crop through successive developmental <i>phases</i>. Each phase is bound by distinct growth <i>stages</i>. Phases often require a target to be reached to signal movement to the next phase. Differences between cultivars are specified by changing the values of the default parameters shown below.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class Phenology : Model, ICustomDocumentation
    {

        ///1. Links
        ///------------------------------------------------------------------------------------------------
        
        [Link]
        private Plant Plant = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>The thermal time</summary>
        [Link]
        public IFunction ThermalTime = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The phases</summary>
        //public IPhase[] Phases { get; private set; }
        private List<IPhase> phases = new List<IPhase>();
        
        /// <summary>The current phase index</summary>
        private int CurrentPhaseIndex;

        /// <summary>This lists all the stages that are pased on this day</summary>
        private List<string> StagesPassedToday = new List<string>();

        
        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Occurs when [phase changed].</summary>
        public event EventHandler<PhaseChangedType> PhaseChanged;

        /// <summary>Occurs when phase is set externally.</summary>
        public event EventHandler ExternalPhaseSet;

        /// <summary>Occurs when daily phenology timestep completed</summary>
        public event EventHandler PostPhenology;


        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>The Thermal time accumulated tt</summary>
        [XmlIgnore]
        public double AccumulatedTT {get; set;}
      
        /// <summary>The Thermal time accumulated tt following emergence</summary>
        [XmlIgnore]
        public double AccumulatedEmergedTT { get; set; }

        /// <summary>The emerged</summary>
        [XmlIgnore]
        public bool Emerged { get; set; } = false;

        /// <summary>Germinated test</summary>
        [XmlIgnore]
        public bool Germinated { get; set; } = false;
                
        /// <summary>A one based stage number.</summary>
        [XmlIgnore]
        public double Stage { get; set; }

        /// <summary>This property is used to retrieve or set the current phase name.</summary>
        [XmlIgnore]
        public string CurrentPhaseName
        {
            get
            {
                if (CurrentPhase == null)
                    return "";
                else
                    return CurrentPhase.Name;
            }
            private set
            {
                CurrentPhaseIndex = IndexOfPhase(value);
                if (CurrentPhaseIndex == -1)
                    throw new Exception("Cannot jump to phenology phase: " + value + ". Phase not found.");
                Summary.WriteMessage(this, string.Format(this + " has set phase to " + CurrentPhase.Name));
            }
        }

        /// <summary>Return current stage name.</summary>
        public string CurrentStageName
        {
            get
            {
                if (OnStartDayOf(CurrentPhase.Start))
                    return CurrentPhase.Start;
                else
                    return "?";
            }
        }
        
        /// <summary>Gets the fraction in current phase.</summary>
        public double FractionInCurrentPhase
        {
            get
            {
                return CurrentPhase.FractionComplete;
            }
        }

        /// <summary>Gets the days after sowing.</summary>
        [XmlIgnore]
        public int DaysAfterSowing { get; set; }

        /// <summary>A utility property to return the current phase.</summary>
        [XmlIgnore]
        public IPhase CurrentPhase
        {
            get
            {
                if (phases == null || CurrentPhaseIndex >= phases.Count)
                    return null;
                else
                    return phases[CurrentPhaseIndex];
            }

            private set
            {
                CurrentPhaseIndex = IndexOfPhase(value.Name);
            }
        }


        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Look for a particular phase and return it's index or -1 if not found.</summary>
        public int IndexOfPhase(string Name)
        {
            for (int P = 0; P < phases.Count; P++)
                if (String.Equals(phases[P].Name, Name, StringComparison.OrdinalIgnoreCase))
                    return P;
            return -1;
        }

        /// <summary>A function that resets phenology to a specified stage</summary>
        public void ReSetToStage(double NewStage, bool RewindAccumTT)
        {
            string stageOnEvent = CurrentPhase.End;
            double oldPhaseIndex = IndexOfPhase(CurrentPhase.Name);

            if (NewStage <= 0)
                throw new Exception(this + "Must pass positive stage to set to");
            if (NewStage > phases.Count()+1)
                throw new Exception(this + " Trying to set to non-existant stage");

            int NewPhaseIndex = Convert.ToInt32(Math.Floor(NewStage)) - 1;
            CurrentPhase = phases[NewPhaseIndex];

            if (CurrentPhaseIndex <= oldPhaseIndex)
            {
                //Make a list of phases to rewind
                List<IPhase> PhasesToRewind = new List<IPhase>();
                foreach (IPhase P in phases)
                {
                    if (IndexOfPhase(P.Name) >= CurrentPhaseIndex)
                        PhasesToRewind.Add(P);
                }

                foreach (IPhase P in PhasesToRewind)
                {
                    if (RewindAccumTT)
                    {
                        AccumulatedTT -= P.TTinPhase;
                        if (IndexOfPhase(P.Name) >= 2)
                            AccumulatedEmergedTT -= P.TTinPhase;
                    }
                    P.ResetPhase();
                }
            }
            else
            {
                //Make a list of phases fast forward
                List<IPhase> PhasesToFastForward = new List<IPhase>();
                foreach (IPhase P in phases)
                {
                    if (IndexOfPhase(P.Name) >= oldPhaseIndex)
                        PhasesToFastForward.Add(P);
                }
                foreach (IPhase P in PhasesToFastForward)
                {
                    if (PhaseChanged != null)
                    {
                        PhaseChangedType PhaseChangedData = new PhaseChangedType();
                        PhaseChangedData.StageName = P.End;
                        PhaseChanged.Invoke(Plant, PhaseChangedData);
                    }
                }
            }

            IPhase currentPhase = phases[CurrentPhaseIndex];
            currentPhase.FractionComplete = NewStage - CurrentPhaseIndex - 1;
            currentPhase.TTinPhase = currentPhase.Target * currentPhase.FractionComplete;

            if (ExternalPhaseSet != null)
                ExternalPhaseSet.Invoke(this, new EventArgs());
        }

        /// <summary> A utility function to return true if the simulation is on the first day of the specified stage. </summary>
        public bool OnStartDayOf(String StageName)
        {
            if (StagesPassedToday.Contains(StageName))
                return true;
            else
                return false;
        }

        /// <summary> A utility function to return true if the simulation is currently in the specified phase. </summary>
        public bool InPhase(String PhaseName)
        {
            return String.Equals(CurrentPhase.Name, PhaseName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary> A utility function to return true if the simulation is currently betweenthe specified start and end stages. </summary>
        public bool Between(String Start, String End)
        {
            if (phases == null)
                return false;

            int StartPhaseIndex = -1;
            int EndPhaseIndex = -1;
            int i= 0;
            while ((EndPhaseIndex == -1) || (i<phases.Count()))
            {
                if (phases[i].Start == Start)
                    StartPhaseIndex = i;
                if (phases[i].End == End)
                    EndPhaseIndex = i;
                i += 1;
            }
            
            if (StartPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + Start);
            if (EndPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + End);
            if (StartPhaseIndex > EndPhaseIndex)
                throw new Exception("Start phase " + Start + " is after phase " + End);

            return CurrentPhaseIndex >= StartPhaseIndex && CurrentPhaseIndex <= EndPhaseIndex;
        }

        /// <summary> A utility function to return true if the simulation is at or past the specified startstage.</summary>
        public bool Beyond(String Start)
        {
            if (CurrentPhaseIndex >= phases.IndexOf(PhaseStartingWith(Start)))
                return true;
            else
                return false;
        }

        /// <summary>A utility function to return the phenological phase that starts with the specified start stage name.</summary>
        public IPhase PhaseStartingWith(String Start)
        {
            foreach (IPhase P in phases)
                if (P.Start == Start)
                    return P;
            throw new Exception("Unable to find phase starting with " + Start);
        }


        ///7. Private methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Initialize the phase list of phenology.</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded(object sender, LoadedEventArgs args)
        {
            if (phases.Count() == 0) //Need this test to ensure the phases are colated only once
            foreach (IPhase phase in Apsim.Children(this, typeof(IPhase)))
            {
                phases.Add(phase);
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
                Clear();
            StagesPassedToday.Add(phases[0].Start);
        }

        /// <summary>Called by sequencer to perform phenology.</summary>
        [EventSubscribe("DoPhenology")]
        private void OnDoPhenology(object sender, EventArgs e)
        {
            if (PlantIsAlive)
            {
                if (ThermalTime.Value() < 0)
                    throw new Exception("Negative Thermal Time, check the set up of the ThermalTime Function in" + this);
               
                // Calculate progression through current phase
                double propOfDayToUse = 1;
                bool incrementPhase = CurrentPhase.DoTimeStep(ref propOfDayToUse);
                AccumulateTT(CurrentPhase.TTForTimeStep);

                while (incrementPhase)
                {
                    StagesPassedToday.Add(CurrentPhase.End);
                    if (CurrentPhaseIndex + 1 >= phases.Count)
                        throw new Exception("Cannot transition to the next phase. No more phases exist");

                    CurrentPhase = phases[CurrentPhaseIndex + 1];

                    if (PhaseChanged != null)
                    {
                        PhaseChangedType PhaseChangedData = new PhaseChangedType();
                        PhaseChangedData.StageName = CurrentPhase.Start;
                        PhaseChanged.Invoke(Plant, PhaseChangedData);
                    }

                    incrementPhase = CurrentPhase.DoTimeStep(ref propOfDayToUse);
                    AccumulateTT(CurrentPhase.TTForTimeStep);
                }
                
               Stage = (CurrentPhaseIndex + 1) + CurrentPhase.FractionComplete;

               if (Plant != null)
                    if (Plant.IsAlive && PostPhenology != null)
                        PostPhenology.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Called when crop is being harvested.</summary>
        [EventSubscribe("Harvesting")]
        private void OnHarvesting(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                //Jump phenology to the end
                CurrentPhaseName = phases[phases.Count - 1].Name;
            }
        }

        /// <summary>Called when crop is being prunned.</summary>
        [EventSubscribe("Pruning")]
        private void OnPruning(object sender, EventArgs e)
        {
            Germinated = false;
            Emerged = false;            
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
        }
  
        /// <summary>Called at the start of each day</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            StagesPassedToday.Clear();
            //reset StagesPassedToday to zero to restart count for the new day
            if (PlantIsAlive)
                DaysAfterSowing += 1;
        }
        
        /// <summary> /// A helper property that checks the parent plant (old or new) to see if it is alive. /// </summary>
        private bool PlantIsAlive
        {
            get
            {
                if (Plant != null && Plant.IsAlive)
                    return true;
                return false;
            }
        }
        
        private void AccumulateTT(double TTinTimeStep)
        {
            AccumulatedTT += TTinTimeStep;
            if (Emerged)
                AccumulatedEmergedTT += TTinTimeStep;
        }
                
         private void Clear()
        {
            DaysAfterSowing = 0;
            Stage = 1;
            AccumulatedTT = 0;
            AccumulatedEmergedTT = 0;
            Emerged = false;
            Germinated = false;
            StagesPassedToday.Clear();
            CurrentPhaseIndex = 0;
            foreach (IPhase phase in phases)
                phase.ResetPhase();
        }
       
        /// <summary>Write phenology info to summary file.</summary>
        internal void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("   Phases:");
            foreach (IPhase P in phases)
                P.WriteSummary(writer);
        }
        
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);

                // Write Phase Table
                tags.Add(new AutoDocumentation.Paragraph(" **List of stages and phases used in the simulation of crop phenological development**", indent));

                DataTable tableData = new DataTable();
                tableData.Columns.Add("Stage Number", typeof(int));
                tableData.Columns.Add("Stage Name", typeof(string));
                tableData.Columns.Add("Phase Name", typeof(string));

                int N = 0;
                foreach (IModel child in Apsim.Children(this, typeof(IPhase)))
                {
                    DataRow row;
                    if (N == 0)
                    {
                        N++;
                        row = tableData.NewRow();
                        row[0] = N;
                        row[1] = (child as IPhase).Start;
                        tableData.Rows.Add(row);
                    }
                    row = tableData.NewRow();
                    row[2] = child.Name;
                    tableData.Rows.Add(row);
                    N++;
                    row = tableData.NewRow();
                    row[0] = N;
                    row[1] = (child as IPhase).End;
                    tableData.Rows.Add(row);
                }
                tags.Add(new AutoDocumentation.Table(tableData, indent));
                tags.Add(new AutoDocumentation.Paragraph(System.Environment.NewLine, indent));


                // add a heading.
                tags.Add(new AutoDocumentation.Heading("Phenological Phases", headingLevel + 1));
                foreach (IModel child in Apsim.Children(this, typeof(IPhase)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 2, indent);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    if (child.GetType() != typeof(Memo) && !typeof(IPhase).IsAssignableFrom(child.GetType()))
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
