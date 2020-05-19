using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.Xml.Serialization;
using System.IO;
using System.Data;
using System.Linq;
using Models.PMF.Struct;
using System.Globalization;

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
        private Plant plant = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction thermalTime = null;

        [Link(IsOptional = true)]
        private Structure structure = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The phases</summary>
        private List<IPhase> phases = new List<IPhase>();
        
        /// <summary>The current phase index</summary>
        private int currentPhaseIndex;

        /// <summary>This lists all the stages that are pased on this day</summary>
        private List<string> stagesPassedToday = new List<string>();

        
        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Occurs when [phase changed].</summary>
        public event EventHandler<PhaseChangedType> PhaseChanged;

        /// <summary>Occurs when phase is set externally.</summary>
        public event EventHandler StageWasReset;

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
        }

        /// <summary>Return current stage name.</summary>
        [XmlIgnore]
        public string CurrentStageName
        {
            get
            {
                if (OnStartDayOf(CurrentPhase.Start))
                    return CurrentPhase.Start;
                else
                    return "";
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
                if (phases == null || currentPhaseIndex >= phases.Count)
                    return null;
                else
                    return phases[currentPhaseIndex];
            }
        }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Look for a particular phase and return it's index or -1 if not found.</summary>
        public int IndexFromPhaseName(string name)
        {
            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
                if (String.Equals(phases[phaseIndex].Name, name, StringComparison.OrdinalIgnoreCase))
                    return phaseIndex;
            return -1;
        }

        /// <summary>Look for a particular stage and return it's index or -1 if not found.</summary>
        public int StartStagePhaseIndex(string stageName)
        {
            int startPhaseIndex = -1;
            int i = 0;
            while (startPhaseIndex == -1 && i < phases.Count())
            {
                if (phases[i].Start == stageName)
                    startPhaseIndex = i;
                i += 1;
            }
            if (startPhaseIndex == -1)
                throw new Exception("Cannot find phase beginning with: " + stageName);
            return startPhaseIndex;
        }

        /// <summary>Look for a particular stage and return it's index or -1 if not found.</summary>
        public int EndStagePhaseIndex(string stageName)
        {
            int endPhaseIndex = -1;
            int i = 0;
            while (endPhaseIndex == -1 && i < phases.Count())
            {
                if (phases[i].End == stageName)
                    endPhaseIndex = i;
                i += 1;
            }
            if (endPhaseIndex == -1)
                throw new Exception("Cannot find phase ending with: " + stageName);
            return endPhaseIndex;
        }

        /// <summary>A function that resets phenology to a specified stage</summary>
        public void SetToStage(double newStage)
        {
            int oldPhaseIndex = IndexFromPhaseName(CurrentPhase.Name);
            stagesPassedToday.Clear();

            if (newStage <= 0)
                throw new Exception(this + "Must pass positive stage to set to");
            if (newStage > phases.Count()+1)
                throw new Exception(this + " Trying to set to non-existant stage");

            currentPhaseIndex = Convert.ToInt32(Math.Floor(newStage), CultureInfo.InvariantCulture) - 1;

            if (newStage < Stage) 
            {
                //Make a list of phases to rewind
                List<IPhase> phasesToRewind = new List<IPhase>();
                foreach (IPhase phase in phases)
                {
                    if ((IndexFromPhaseName(phase.Name) >= currentPhaseIndex)&&(IndexFromPhaseName(phase.Name)<=oldPhaseIndex))
                        phasesToRewind.Add(phase);
                }

                foreach (IPhase phase in phasesToRewind)
                {
                    if(!(phase is IPhaseWithTarget) && !(phase is GotoPhase) && !(phase is EndPhase) && !(phase is PhotoperiodPhase) && !(phase is LeafDeathPhase) && !(phase is DAWSPhase))
                        { throw new Exception("Can not rewind over phase of type " + phase.GetType()); }
                    if (phase is IPhaseWithTarget)
                    {
                        IPhaseWithTarget rewindingPhase = phase as IPhaseWithTarget;
                        AccumulatedTT -= rewindingPhase.ProgressThroughPhase;
                        AccumulatedEmergedTT -= rewindingPhase.ProgressThroughPhase;
                        phase.ResetPhase();
                    }
                    else
                        phase.ResetPhase();
                }
                AccumulatedEmergedTT = Math.Max(0, AccumulatedEmergedTT);

            }
            else
            {
                //Make a list of phases to fast forward
                List<IPhase> phasesToFastForward = new List<IPhase>();
                foreach (IPhase phase in phases)
                {
                    if (IndexFromPhaseName(phase.Name) >= oldPhaseIndex)
                        phasesToFastForward.Add(phase);
                }
                foreach (IPhase phase in phasesToFastForward)
                {
                    if (phase is EndPhase)
                    {
                        stagesPassedToday.Add(phase.Start); //Fixme.  This is a pretty ordinary bit of programming to get around the fact we use a phenological stage to match observed values. We should change this so plant has a harvest tag to match on.
                    }
                    stagesPassedToday.Add(phase.End);
                    PhaseChangedType PhaseChangedData = new PhaseChangedType();
                    PhaseChangedData.StageName = phase.End;
                    PhaseChanged?.Invoke(plant, PhaseChangedData);
                }
            }

            if (phases[currentPhaseIndex] is IPhaseWithTarget)
            {
                IPhaseWithTarget currentPhase = (phases[currentPhaseIndex] as IPhaseWithTarget);
                currentPhase.ProgressThroughPhase = currentPhase.Target * (newStage - currentPhaseIndex - 1);

                if (currentPhase.ProgressThroughPhase == 0)
                    stagesPassedToday.Add(currentPhase.Start);
            }
            if ((phases[currentPhaseIndex] is PhotoperiodPhase) || (phases[currentPhaseIndex] is LeafDeathPhase))
                stagesPassedToday.Add(phases[currentPhaseIndex].Start);

            StageWasReset?.Invoke(this, new EventArgs());
        }

        /// <summary> A utility function to return true if the simulation is on the first day of the specified stage. </summary>
        public bool OnStartDayOf(String stageName)
        {
            if (stagesPassedToday.Contains(stageName))
                return true;
            else
                return false;
        }

        /// <summary> A utility function to return true if the simulation is currently in the specified phase. </summary>
        public bool InPhase(String phaseName)
        {
            return String.Equals(CurrentPhase.Name, phaseName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary> A utility function to return true if the simulation is currently between the specified start and end stages. </summary>
        public bool Between(int startPhaseIndex, int endPhaseIndex)
        {
            if (phases == null)
                return false;
            
            if (startPhaseIndex > endPhaseIndex)
                throw new Exception("Start phase " + startPhaseIndex + " is after phase " + endPhaseIndex);

            return currentPhaseIndex >= startPhaseIndex && currentPhaseIndex <= endPhaseIndex;
        }

        /// <summary> A utility function to return true if the simulation is currently betweenthe specified start and end stages. </summary>
        public bool Between(String start, String end)
        {
            if (phases == null)
                return false;

            int startPhaseIndex = -1;
            int endPhaseIndex = -1;
            int i = 0;
            while (endPhaseIndex == -1 && i < phases.Count())
            {
                if (phases[i].Start == start)
                    startPhaseIndex = i;
                if (phases[i].End == end)
                    endPhaseIndex = i;
                i += 1;
            }

            if (startPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + start);
            if (endPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + end);
            if (startPhaseIndex > endPhaseIndex)
                throw new Exception("Start phase " + start + " is after phase " + end);

            return currentPhaseIndex >= startPhaseIndex && currentPhaseIndex <= endPhaseIndex;
        }

        /// <summary> A utility function to return true if the simulation is at or past the specified startstage.</summary>
        public bool Beyond(String start)
        {
            if (currentPhaseIndex >= phases.IndexOf(PhaseStartingWith(start)))
                return true;
            else
                return false;
        }

        /// <summary>A utility function to return the phenological phase that starts with the specified start stage name.</summary>
        public IPhase PhaseStartingWith(String start)
        {
            foreach (IPhase P in phases)
                if (P.Start == start)
                    return P;
            throw new Exception("Unable to find phase starting with " + start);
        }


        // 7. Private methods
        // -----------------------------------------------------------------------------------------------------------
        //

        /// <summary>
        /// Refreshes the list of phases.
        /// </summary>
        private void RefreshPhases()
        {
            if (phases == null)
                phases = new List<IPhase>();
            else
                phases.Clear();

            foreach (IPhase phase in Apsim.Children(this, typeof(IPhase)))
                phases.Add(phase);
        }
        /// <summary>Called when model has been created.</summary>
        public override void OnCreated()
        {
            RefreshPhases();
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            RefreshPhases();
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            Clear();
            stagesPassedToday.Add(phases[0].Start);
        }

        /// <summary>Called by sequencer to perform phenology.</summary>
        [EventSubscribe("DoPhenology")]
        private void OnDoPhenology(object sender, EventArgs e)
        {
            if (PlantIsAlive)
            {
                if (thermalTime.Value() < 0)
                    throw new Exception("Negative Thermal Time, check the set up of the ThermalTime Function in" + this);
               
                // Calculate progression through current phase
                double propOfDayToUse = 1;
                bool incrementPhase = CurrentPhase.DoTimeStep(ref propOfDayToUse);

                while (incrementPhase)
                {
                    if ((CurrentPhase is EmergingPhase) || (CurrentPhase.End == structure?.LeafInitialisationStage)|| (CurrentPhase is DAWSPhase))
                    {
                         Emerged = true;
                    }

                    stagesPassedToday.Add(CurrentPhase.End);
                    if (currentPhaseIndex + 1 >= phases.Count)
                        throw new Exception("Cannot transition to the next phase. No more phases exist");

                    currentPhaseIndex = currentPhaseIndex + 1;

                    PhaseChangedType PhaseChangedData = new PhaseChangedType();
                        PhaseChangedData.StageName = CurrentPhase.Start;
                        PhaseChanged?.Invoke(plant, PhaseChangedData);

                    incrementPhase = CurrentPhase.DoTimeStep(ref propOfDayToUse);
                }

                AccumulatedTT += thermalTime.Value();
                if (Emerged)
                    AccumulatedEmergedTT += thermalTime.Value();

                Stage = (currentPhaseIndex + 1) + CurrentPhase.FractionComplete;

                if (plant != null && plant.IsAlive && PostPhenology != null)
                        PostPhenology.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Called when crop is being harvested.</summary>
        [EventSubscribe("Harvesting")]
        private void OnHarvesting(object sender, EventArgs e)
        {
            //Jump phenology to the end
             if(this.Parent.Name != "SimpleFruitTree") //Unless you are a perennial fruit tree.  There must be a better way of doing this
                SetToStage((double)(phases.Count));
        }

        /// <summary>Called when crop is being prunned.</summary>
        [EventSubscribe("Pruning")]
        private void OnPruning(object sender, EventArgs e)
        {
             Emerged = false;            
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }
  
        /// <summary>Called at the start of each day</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            stagesPassedToday.Clear();
            //reset StagesPassedToday to zero to restart count for the new day
            if (PlantIsAlive)
                DaysAfterSowing += 1;
        }
        
        /// <summary> /// A helper property that checks the parent plant (old or new) to see if it is alive. /// </summary>
        private bool PlantIsAlive
        {
            get
            {
                if (plant != null && plant.IsAlive)
                    return true;
                return false;
            }
        }
        
         private void Clear()
        {
            DaysAfterSowing = 0;
            Stage = 1;
            AccumulatedTT = 0;
            AccumulatedEmergedTT = 0;
            Emerged = false;
            stagesPassedToday.Clear();
            currentPhaseIndex = 0;
            foreach (IPhase phase in phases)
                phase.ResetPhase();
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
                tableData.Columns.Add("Phase Number", typeof(int));
                tableData.Columns.Add("Phase Name", typeof(string));
                tableData.Columns.Add("Initial Stage", typeof(string));
                tableData.Columns.Add("Final Stage", typeof(string));

                int N = 1;
                foreach (IModel child in Apsim.Children(this, typeof(IPhase)))
                {
                    DataRow row;
                    row = tableData.NewRow();
                    row[0] = N;
                    row[1] = child.Name;
                    row[2] = (child as IPhase).Start;
                    row[3] = (child as IPhase).End;
                    if (child is GotoPhase)
                        row[3] = (child as GotoPhase).PhaseNameToGoto;
                    tableData.Rows.Add(row);
                    N++;
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
