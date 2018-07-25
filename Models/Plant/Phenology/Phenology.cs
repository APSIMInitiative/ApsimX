using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Functions;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using APSIM.Shared.Utilities;
using System.Data;
using System.Linq;

namespace Models.PMF.Phen
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PhaseChangedType : EventArgs
    {
        /// <summary>The old phase name</summary>
        public String OldPhaseName = "";
        /// <summary>The new phase name</summary>
        public String NewPhaseName = "";
        /// <summary>The stage at phase change</summary>
        public String EventStageName = "";
    }
    /// <summary>
    /// This model simulates the development of the crop through successive developmental <i>phases</i>. Each phase is bound by distinct growth <i>stages</i>. Phases often require a target to be reached to signal movement to the next phase. Differences between cultivars are specified by changing the values of the default parameters shown below.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class Phenology : Model, ICustomDocumentation
    {
        [Link]
        private Plant Plant = null;

        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>The rewind due to biomass removed</summary>
        [ChildLinkByName(IsOptional = true)]
        private IFunction RewindDueToBiomassRemoved = null;

        /// <summary>Occurs when [phase changed].</summary>
        public event EventHandler<PhaseChangedType> PhaseChanged;

        /// <summary>Occurs when phase is rewound.</summary>
        public event EventHandler PhaseRewind;

        /// <summary>Occurs when [growth stage].</summary>
        public event NullTypeDelegate GrowthStage;

        /// <summary>Occurs when daily phenology timestep completed</summary>
        public event EventHandler PostPhenology;

        /// <summary>The thermal time</summary>
        [Link]
        public IFunction ThermalTime = null;

        /// <summary>The phases</summary>
        [XmlIgnore]
        //public IPhase[] Phases { get; private set; }
        private List<IPhase> Phases = new List<IPhase>();


        /// <summary>The current phase index</summary>
        [XmlIgnore]
        private int CurrentPhaseIndex;
        /// <summary>The Thermal time accumulated tt</summary>
        [XmlIgnore]
        public double AccumulatedTT {get; set;}
        /// <summary>The Thermal time accumulated tt following emergence</summary>
        [XmlIgnore]
        public double AccumulatedEmergedTT { get; set; }
        /// <summary>The currently on first day of phase.  This is an array that lists all the stages that are pased on this day</summary>
        [XmlIgnore]
        private string[] CurrentlyOnFirstDayOfPhase = new string[] {"","","","","",""};
        /// <summary>The number of stages that have been passed today</summary>
        [XmlIgnore]
        private int StagesPassedToday = 0;
        /// <summary>The just initialised</summary>
        [XmlIgnore]
        private bool JustInitialised = true;
        /// <summary>The fraction biomass removed</summary>
        [XmlIgnore]
        public double FractionBiomassRemoved { get; set; }
        /// <summary>The sow date</summary>
        [XmlIgnore]
        private DateTime SowDate = DateTime.MinValue;
        /// <summary>The emerged</summary>
        [XmlIgnore]
        public bool Emerged = false;
        /// <summary>Germinated test</summary>
        [XmlIgnore]
        public bool Germinated = false;

        //Fixme.  This currently wont be working because the phases are all IPhase types now.  Each phase needs a property to say if it can be rewound
        /// <summary> Phases that can't be rewound (e.g. due to grazing)   /// </summary>
        [XmlIgnore]
        private static Type[] phasesThatWontRewind = new Type[] { typeof(NodeNumberPhase), typeof(LeafDeathPhase), typeof(LeafAppearancePhase), typeof(GotoPhase), typeof(GerminatingPhase), typeof(EndPhase) };

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
            set
            {
                int PhaseIndex = IndexOfPhase(value);
                if (PhaseIndex == -1)
                    throw new Exception("Cannot jump to phenology phase: " + value + ". Phase not found.");
                CurrentPhase = Phases[PhaseIndex];
                Summary.WriteMessage(this, string.Format(this + " has set phase to " + CurrentPhase.Name));
            }
        }

        /// <summary>Return current stage name.</summary>
        [XmlIgnore]
        public string CurrentStageName
        {
            get
            {
                if (OnDayOf(CurrentPhase.Start))
                    return CurrentPhase.Start;
                else
                    return "?";
            }
        }


        /// <summary>Gets the fraction in current phase.</summary>
        [XmlIgnore]
        public double FractionInCurrentPhase
        {
            get
            {
                return Stage - (int)Stage;
            }
        }

        /// <summary>Gets the days after sowing.</summary>
        [XmlIgnore]
        public int DaysAfterSowing { get; set; }



        /// <summary>Gets the tt in above ground phase.</summary>
        /// <value>The tt in above ground phase.</value>
        /// <exception cref="System.Exception">Cannot find Phenology.AboveGroundPeriod function in xml file</exception>
        private double TTInAboveGroundPhase
        {
            get
            {
                double TTInPhase = 0.0;
                for (int i = IndexOfFirstAboveGroundPhase(); i < Phases.Count; i++)
                    TTInPhase += Phases[i].TTinPhase;
                return TTInPhase;
            }
        }

        /// <summary>Initialize the phase list of phenology.</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded(object sender, LoadedEventArgs args)
        {
            List<IPhase> phases = new List<IPhase>();

            foreach (IPhase phase in Apsim.Children(this, typeof(IPhase)))
            {
                phases.Add(phase);
            }

            Phases = phases;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
                Clear();
            }

        /// <summary>Called when crop is being harvested.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Harvesting")]
        private void OnHarvesting(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                //Jump phenology to the end
                int EndPhase = Phases.Count;
                CurrentPhaseName = Phases[EndPhase - 1].Name;
            }
        }

        /// <summary>Called when crop is being prunned.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Pruning")]
        private void OnPruning(object sender, EventArgs e)
        {
            Germinated = false;
            Emerged = false;
            
        }


        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
        }
  
         /// <summary>Called at the start of each day</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            //reset all members to the CurrentlyOnFirstDayOfPhase array to nothing so new stages passed today can be inserted
            for (int i = 0; i < CurrentlyOnFirstDayOfPhase.Length; i++)
                CurrentlyOnFirstDayOfPhase[i] = "";
            //reset StagesPassedToday to zero to restart count for the new day
            StagesPassedToday = 0;
            if (PlantIsAlive)
                DaysAfterSowing += 1;
        }


        /// <summary>Look for a particular phase and return it's index or -1 if not found.</summary>
        /// <param name="Name">The name.</param>
        /// <returns></returns>
        public int IndexOfPhase(string Name)
        {
            for (int P = 0; P < Phases.Count; P++)
                if (String.Equals(Phases[P].Name, Name, StringComparison.OrdinalIgnoreCase))
                    return P;
            return -1;
        }

        /// <summary>
        /// A helper property that checks the parent plant (old or new) to see if it is alive.
        /// </summary>
        /// <value><c>true</c> if plant is alive; otherwise, <c>false</c>.</value>
        private bool PlantIsAlive
        {
            get
            {
                if (Plant != null && Plant.IsAlive)
                    return true;
                return false;
            }
        }

        /// <summary>Called by sequencer to perform phenology.</summary>
        /// <remarks>
        /// Perform our daily timestep function. Get the current phase to do its
        /// development for the day. If TT is leftover after Phase is progressed,
        /// and the timestep for the subsequent phase is calculated using leftover TT
        /// </remarks>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.Exception">Cannot transition to the next phase. No more phases exist</exception>
        [EventSubscribe("DoPhenology")]
        private void OnDoPhenology(object sender, EventArgs e)
        {
            if (PlantIsAlive)
            {
                if(ThermalTime.Value() < 0)
                    throw new Exception("Negative Thermal Time, check the set up of the ThermalTime Function in" + this);
                // If this is the first time through here then setup some variables.
                if (Phases == null || Phases.Count == 0)
                    OnSimulationCommencing(null, null);

                    if (CurrentlyOnFirstDayOfPhase[0] == "")
                        if (JustInitialised)
                        {
                            CurrentlyOnFirstDayOfPhase[0] = Phases[0].Start;
                            JustInitialised = false;
                        }

                double FractionOfDayLeftOver = CurrentPhase.DoTimeStep(1.0);

                if (FractionOfDayLeftOver > 0)
                {
                     while (FractionOfDayLeftOver > 0)// Transition to the next phase.
                    {
                        if (CurrentPhaseIndex + 1 >= Phases.Count)
                            throw new Exception("Cannot transition to the next phase. No more phases exist");

                        if (Stage >= 1)
                            Germinated = true;

                        CurrentPhase = Phases[CurrentPhaseIndex + 1];
                        if (GrowthStage != null)
                            GrowthStage.Invoke();

                       // run the next phase with the left over time step from the phase we have just completed
                        FractionOfDayLeftOver = CurrentPhase.DoTimeStep(FractionOfDayLeftOver);
                       
                        Stage = (CurrentPhaseIndex + 1) + CurrentPhase.FractionComplete;
                    }
                }
                else
                {
                    Stage = (CurrentPhaseIndex + 1) + CurrentPhase.FractionComplete;
                }

                AccumulatedTT += CurrentPhase.TTForToday;

                if (Emerged)
                    AccumulatedEmergedTT += CurrentPhase.TTForToday;

                if (Plant != null)
                    if (Plant.IsAlive && PostPhenology != null)
                        PostPhenology.Invoke(this, new EventArgs());
            }
        }

        /// <summary>A utility property to return the current phase.</summary>
        /// <value>The current phase.</value>
        /// <exception cref="System.Exception">
        /// Cannot jump to phenology phase:  + value + . Phase not found.
        /// or
        /// Cannot goto phase:  + GotoP.PhaseNameToGoto + . Phase not found.
        /// </exception>
        [XmlIgnore]
        public IPhase CurrentPhase
        {
            get
            {
                if (Phases == null || CurrentPhaseIndex >= Phases.Count)
                    return null;
                else
                    return Phases[CurrentPhaseIndex];
            }
                    
            private set
            {
                string oldPhaseName = CurrentPhase.Name;
                string stageOnEvent = CurrentPhase.End;
                //double TTRewound;
                double OldPhaseINdex = IndexOfPhase(CurrentPhase.Name);
                CurrentPhaseIndex = IndexOfPhase(value.Name);
                bool HarvestCall = false;
                if (CurrentPhaseIndex == Phases.Count - 1)
                    HarvestCall = true;
                if (CurrentPhaseIndex == -1)
                    throw new Exception("Cannot jump to phenology phase: " + value + ". Phase not found.");

                CurrentlyOnFirstDayOfPhase[StagesPassedToday] = CurrentPhase.Start;
                StagesPassedToday += 1;

                // If the new phase is a rewind or going ahead more that one phase(comming from a GoToPhase or PhaseSet Function), then reinitialise 
                // all phases that are being wound back over.
                if (((CurrentPhaseIndex <= OldPhaseINdex)&&HarvestCall==false)||(CurrentPhaseIndex - OldPhaseINdex > 1)||(Phases[CurrentPhaseIndex]is GotoPhase))
                {
                    foreach (IPhase P in Phases)
                    {
                        //Work out how much tt was accumulated at the stage we are resetting to and adjust accumulated TT accordingly
                        if (Phases[CurrentPhaseIndex] is GotoPhase)
                        { //Dont rewind thermal time for Goto phase.  Although it is moving phenology back it is a ongoing progression in phenology of the plant so TT accumulates
                        }
                        else if (IndexOfPhase(P.Name) >= CurrentPhaseIndex)
                        {//for Phase Set function we rewind phenology.  This is called by cut or graze which removes biomass and changes plant phenology so TT rewinds
                            AccumulatedTT -= P.TTinPhase;
                            if (IndexOfPhase(P.Name) >= 2)
                                AccumulatedEmergedTT -= P.TTinPhase;
                        }
                        
                        //Reset phases we are rewinding over.
                        if (IndexOfPhase(P.Name) >= CurrentPhaseIndex)
                            P.ResetPhase();
                    }
                    if (Phases[CurrentPhaseIndex] is GotoPhase)
                    {
                        GotoPhase GotoP = (GotoPhase)Phases[CurrentPhaseIndex];
                        CurrentPhaseIndex = IndexOfPhase(GotoP.PhaseNameToGoto);
                        if (CurrentPhaseIndex == -1)
                            throw new Exception("Cannot goto phase: " + GotoP.PhaseNameToGoto + ". Phase not found.");
                    }
                }
                CurrentPhase.ResetPhase();
                // Send a PhaseChanged event.
                if (PhaseChanged != null)
                {
                    //_AccumulatedTT += CurrentPhase.TTinPhase;
                    PhaseChangedType PhaseChangedData = new PhaseChangedType();
                    PhaseChangedData.OldPhaseName = oldPhaseName;
                    PhaseChangedData.NewPhaseName = CurrentPhase.Name;
                    PhaseChangedData.EventStageName = stageOnEvent;
                    PhaseChanged.Invoke(Plant, PhaseChangedData);
                }
            }
        }

        /// <summary>A function that resets phenology to a specified stage</summary>
        /// <value>The current phase.</value>
        /// <exception cref="System.Exception">
        /// Cannot jump to phenology phase:  + value + . Phase not found.
        /// or
        /// Cannot goto phase:  + GotoP.PhaseNameToGoto + . Phase not found.
        /// </exception>
        public void ReSetToStage (double NewStage)
        {
            if (NewStage == 0)
                throw new Exception(this + "Must pass positive stage to set to");
            int SetPhaseIndex = Convert.ToInt32(Math.Floor(NewStage))-1;
            CurrentPhase = Phases[SetPhaseIndex];
            IPhase Current = Phases[CurrentPhaseIndex];
            double proportionOfPhase = NewStage - CurrentPhaseIndex - 1;
            Current.FractionComplete = proportionOfPhase;
            if(PhaseRewind != null)
            PhaseRewind.Invoke(this, new EventArgs());
        }

        /// <summary> /// Find the first phase that is beyond germination i.e. plant is above ground.  /// </summary>
        private int IndexOfFirstAboveGroundPhase()
        {
            int index = Phases.FindIndex(p => p.End == "Germination");
            if (index == -1)
                return 0;
            else
                return index + 1;
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            DaysAfterSowing = 0;
            Stage = 1;
            AccumulatedTT = 0;
            AccumulatedEmergedTT = 0;
            JustInitialised = true;
            Emerged = false;
            Germinated = false;
            SowDate = Clock.Today;
            CurrentlyOnFirstDayOfPhase = new string[] { "", "", "", "", "", "" };
            CurrentPhaseIndex = 0;
            FractionBiomassRemoved = 0;
            foreach (IPhase phase in Phases)
                phase.ResetPhase();
        }

        /// <summary>
        /// A utility function to return true if the simulation is on the first day of the
        /// specified stage.
        /// </summary>
        /// <param name="StageName">Name of the stage.</param>
        /// <returns></returns>
        public bool OnDayOf(String StageName)
        {
            bool StageToday = false;
            for (int i = 0; i < CurrentlyOnFirstDayOfPhase.Length; i++)
                if (CurrentlyOnFirstDayOfPhase[i] == StageName)
                    StageToday = true;
            
            return StageToday;
            //return (StageName.Equals(CurrentlyOnFirstDayOfPhase, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// A utility function to return true if the simulation is currently in the
        /// specified phase.
        /// </summary>
        /// <param name="PhaseName">Name of the phase.</param>
        /// <returns></returns>
        public bool InPhase(String PhaseName)
        {
            return String.Equals(CurrentPhase.Name, PhaseName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A utility function to return true if the simulation is currently between
        /// the specified start and end stages.
        /// </summary>
        /// <param name="Start">The start.</param>
        /// <param name="End">The end.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot test between stages  + Start +   + End</exception>
        public bool Between(String Start, String End)
        {
            if (Phases == null)
                return false;

            string StartFractionSt = StringUtilities.SplitOffBracketedValue(ref Start, '(', ')');
            double StartFraction = 0;
            if (StartFractionSt != "")
                StartFraction = Convert.ToDouble(StartFractionSt, 
                                                 System.Globalization.CultureInfo.InvariantCulture);

            int StartPhaseIndex = -1;
            int EndPhaseIndex = -1;
            for (int i = 0; i < Phases.Count; i++)
            {
                if (Phases[i].Start == Start)
                    StartPhaseIndex = i;
                if (Phases[i].End == End)
                    EndPhaseIndex = i;
            }
            if (StartPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + Start);
            if (EndPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + End);
            if (StartPhaseIndex > EndPhaseIndex)
                throw new Exception("Start phase " + Start + " is after phase " + End);

            if (StartPhaseIndex == -1 || EndPhaseIndex == -1)
                throw new Exception("Cannot test between stages " + Start + " " + End);
            
            if (CurrentPhaseIndex == StartPhaseIndex && StartFraction > 0)
                return Stage >= Math.Truncate(Stage) + StartFraction;
  
            else
                return CurrentPhaseIndex >= StartPhaseIndex && CurrentPhaseIndex <= EndPhaseIndex;
        }

        /// <summary>
        /// A utility function to return true if the simulation is at or past
        /// the specified startstage.
        /// </summary>
        /// <param name="Start">The start.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot test between stages  + Start +   + End</exception>
        public bool Beyond(String Start)
        {
            string StartFractionSt = StringUtilities.SplitOffBracketedValue(ref Start, '(', ')');
            double StartFraction = 0;
            if (StartFractionSt != "")
                StartFraction = double.Parse(StartFractionSt.ToString(), 
                                             System.Globalization.CultureInfo.InvariantCulture);
            int StartPhaseIndex = Phases.IndexOf(PhaseStartingWith(Start));

            CurrentPhaseIndex = IndexOfPhase(CurrentPhase.Name);
            
            if (CurrentPhaseIndex >= StartPhaseIndex)
                return true;
            else
                return false;
        }

        /// <summary>
        /// A utility function to return the phenological phase that starts with
        /// the specified start stage name.
        /// </summary>
        /// <param name="Start">The start.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unable to find phase starting with  + Start</exception>
        public IPhase PhaseStartingWith(String Start)
        {
            foreach (IPhase P in Phases)
                if (P.Start == Start)
                    return P;
            throw new Exception("Unable to find phase starting with " + Start);
        }

        /// <summary>
        /// A utility function to return the phenological phase that ends with
        /// the specified start stage name.
        /// </summary>
        /// <param name="End">The end.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unable to find phase ending with  + End</exception>
        public IPhase PhaseEndingWith(String End)
        {
            foreach (IPhase P in Phases)
                if (P.End == End)
                    return P;
            throw new Exception("Unable to find phase ending with " + End);
        }

        /// <summary>A utility function to return true if a phenological phase is valid.</summary>
        /// <param name="Start">The start.</param>
        /// <returns></returns>
        public bool IsValidPhase(String Start)
        {
            foreach (IPhase P in Phases)
                if (P.Start == Start)
                    return true;
            return false;
        }

        /// <summary>A utility function to return true if a phenological phase is valid.</summary>
        /// <param name="PhaseName">Name of the phase.</param>
        /// <returns></returns>
        public bool IsValidPhase2(String PhaseName)
        {
            foreach (IPhase P in Phases)
                if (P.Name == PhaseName)
                    return true;
            return false;
        }

        /// <summary>Write phenology info to summary file.</summary>
        /// <param name="writer">The writer.</param>
        internal void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("   Phases:");
            foreach (IPhase P in Phases)
                P.WriteSummary(writer);
        }

        /// <summary>Biomass has been removed. Optionally rewind phenology.</summary>
        /// <param name="fractionRemoved">The fraction of biomass that was removed</param>
        public void BiomassRemoved(double fractionRemoved)
        {
            int existingPhaseIndex = CurrentPhaseIndex;
            if (RewindDueToBiomassRemoved != null && !phasesThatWontRewind.Contains(CurrentPhase.GetType()))
            {
                FractionBiomassRemoved = fractionRemoved; // The RewindDueToBiomassRemoved function will use this.

                double ttCritical = TTInAboveGroundPhase;
                double removeFractPheno = RewindDueToBiomassRemoved.Value();
                double removeTTPheno = ttCritical * removeFractPheno;

                //string msg;
                //msg = "Phenology change:-\r\n";
                //msg += "    Fraction DM removed  = " + fractionRemoved.ToString() + "\r\n";
                //msg += "    Fraction TT removed  = " + removeFractPheno.ToString() + "\r\n";
                //msg += "    Critical TT          = " + ttCritical.ToString() + "\r\n";
                //msg += "    Remove TT            = " + removeTTPheno.ToString() + "\r\n";
                //Summary.WriteMessage(this, msg);
                double ttRemaining = removeTTPheno;
                for (int i = Phases.Count - 1; i >= 0; i--)
                {
                    IPhase Phase = Phases[i];
                    if (Phase.TTinPhase > 0 && !phasesThatWontRewind.Contains(Phase.GetType()))
                    {
                        double ttCurrentPhase = Phase.TTinPhase;
                        if (ttRemaining > ttCurrentPhase)
                        {
                            Phase.ResetPhase();
                            ttRemaining -= ttCurrentPhase;
                            CurrentPhaseIndex -= 1;
                            if (CurrentPhaseIndex < 4)  //FIXME - hack to stop onEmergence being fired which initialises biomass parts
                            {
                                CurrentPhaseIndex = 4;
                                break;
                            }
                        }
                        else
                        {
                            Phase.Add(-ttRemaining);
                            // Return fraction of thermal time we are through the current
                            // phenological phase (0-1)
                            double frac = Phase.FractionComplete;
                            //if (frac > 0.0 && frac < 1.0)  // Don't skip out of this stage - some have very low targets, eg 1.0 in "maturity"
                            //    currentStage = frac + floor(currentStage);

                            break;
                        }
                    }
                    else
                    { // phase is empty - not interested in it
                    }
                }
                Stage = (CurrentPhaseIndex + 1) + CurrentPhase.FractionComplete;

                if (existingPhaseIndex != CurrentPhaseIndex)
                {
                    PhaseChangedType PhaseChangedData = new PhaseChangedType();
                    PhaseChangedData.OldPhaseName = Phases[existingPhaseIndex].Name;
                    PhaseChangedData.NewPhaseName = Phases[CurrentPhaseIndex].Name;
                    PhaseChanged.Invoke(Plant, PhaseChangedData);
                    //Fixme MyPaddock.Publish(CurrentPhase.Start);
                }
            }
        }



        /// <summary>Constructor</summary>
        public Phenology() { }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
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
