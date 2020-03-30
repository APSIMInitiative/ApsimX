namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using Models.Functions;

    /// <summary>
    /// # [Name]
    /// A LifeCyclePhase represents a distinct period in the development or an organisum.
    /// Each LifeCyclePhase assembles an arbitary number of cohorts which represent individuals
    /// that entered this phase at the same time and will have the same PhysiologicalAge.
    /// Each day the LifeCycle phase loops through each of its cohorts determining the increase 
    /// PhysiologicalAge, the number of mortalities in that cohort and the number of progeny the
    /// cohort produces.
    /// LifeCyclePhases are parameterised with three essential Properties:
    ///  1. Development.  Returns the change in Physiological Age (0-1) of each cohort each day,
    ///  2. Mortality. Returns the number of individuals that die in each cohort each day,
    ///  3. Reproduction. Returns the number of progeny that each cohort will produce each day.
    ///  4. Migration. Returns the number of migrants that will each cohort each day.
    /// Each of these properties can be parameterised with any agregation of Ifunctions and
    /// the code takes the values from these IFunctions and adds or subtracts them from the
    /// corresponding property in each Cohort.
    /// The LifeCycle class calls the Process() method in each LifeCyclePhase and these then loop
    /// through each of their cohorts and apply the values of the Development, Mortality and Reproduction
    /// Functions in turn.  LifeCyclePhase has a CurrentCohort Property wihch is set at each loop 
    /// and may be referenced by functions to get cohort specific properties (eg Physiological age or Population)
    /// so Functions return different values for each cohort.
    /// When PhysiologicalAge of a cohort reaches 1 the members of this cohort graduate and a new
    /// of this many individuals in added to the next LifeCyclePhase and removed from the current 
    /// LifeCyclePhase.  If it is the final LifeCyclePhase the individuals of cohorts with 
    /// PhysiologicalAge of 1 will die and the cohort will be removed.
    /// Each LifeCyclePhase specifies a NameOfPhaseForProgeny and when Reproduciton returns a positive,
    /// a cohort of this many individuals is initiated in the corresponding LifeCyclePhaseForProgeny.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCycle))]
    public class LifeCyclePhase : Model
    {
        /// <summary>Returns change (0-1) in PhysiologicalAge of the cohort being processed</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction development = null;

        /// <summary> Returns number of mortalities from  cohort being processed</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction mortality = null;

        /// <summary> Returns number of progeny created by cohort being processed</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction reproduction = null;

        /// <summary> Returns number of migrants leaving the cohort being processed</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction migration = null;

        /// <summary>the destination LifeCyclePhase that graduates from this LifeCyclePhase will be moved to</summary>
        public LifeCyclePhase LifeCyclePhaseForGraduates { get; set; }

        /// <summary>The list of ProgenyDestinationPhases.</summary>
        [JsonIgnore]
        public List<ProgenyDestinationPhase> ProgenyDestinations { get; private set; }

        /// <summary>The list of MitrantDestinationPhases.</summary>
        [JsonIgnore]
        public List<MigrantDestinationPhase> MigrantDestinations { get; private set; }

        /// <summary>The list of cohorts in this LifeCyclePhase.</summary>
        [JsonIgnore]
        public List<Cohort> Cohorts { get; private set; }

        /// <summary>Returns the count of cohorts in this LifeCyclePhase</summary>
        public int CohortCount
        {
            get
            {
                if (Cohorts != null)
                {
                    return Cohorts.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>Returns the total number of individuals in this LifeCyclePhase (Summed over all cohorts)</summary>
        public double TotalPopulation
        {
            get
            {
                double sum = 0;
                if (Cohorts != null)
                {
                    if (Cohorts != null)
                    {
                        foreach (Cohort aCohort in Cohorts)
                        {
                            sum += aCohort.Population;
                        }
                    }
                }
                return sum;
            }
        }

        /// <summary>Returns and array of populations for each cohort in this LifeCyclePhase</summary>
        public double[] Populations
        {
            get
            {
                List<double> populations = new List<double>();
                if (Cohorts != null)
                {
                    for (int i = 0; i < Cohorts.Count; i++)
                    {
                        populations.Add(Cohorts[i].Population);
                    }
                }
                return populations.ToArray();
            }
        }

        /// <summary>Returns an array of PhysiologicalAges for each cohort in this LifeCyclePhase</summary>
        public double[] PhysiologicalAges
        {
            get
            {
                List<double> pAges = new List<double>();
                if (Cohorts != null)
                {
                    for (int i = 0; i < Cohorts.Count; i++)
                    {
                        pAges.Add(Cohorts[i].PhysiologicalAge);
                    }
                }
                return pAges.ToArray();
            }
        }

        /// <summary>The cohort currently being processed</summary>
        public Cohort CurrentCohort { get; set; }

        /// <summary>The number of individules added today by Infest() method (Summed across all new cohorts)</summary>
        public double Immigrants { get; set; }

        /// <summary>The rate (0-1) that cohorts progress toward maturity</summary>
        public double DevelopmentRate { get; set; }

        /// <summary>The number of individules expiring (Summed across all cohorts)</summary>
        public double Mortalities { get; set; } 

        /// <summary>The number of individuals moved to the next LifeCyclePhase (Summed across all graduating cohorts</summary>
        public double Graduates { get; set; }

        /// <summary>The number of individules deposited in LifeCyclePhaseForProgeny (Sum of progeny across all cohorts</summary>
        public double Progeny { get; set; }

        /// <summary>The number of individules in LifeCyclePhaseForProgeny departing this population (Sum of progeny across all cohorts) </summary>
        public double Migrants { get; set; }

        /// <summary>At the start of the simulation construct the list of LifeCyclePhase</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ProgenyDestinations = new List<ProgenyDestinationPhase>();
            foreach (ProgenyDestinationPhase pdest in Apsim.Children(this, typeof(ProgenyDestinationPhase)))
                ProgenyDestinations.Add(pdest);
            MigrantDestinations = new List<MigrantDestinationPhase>();
            foreach (MigrantDestinationPhase mdest in Apsim.Children(this, typeof(MigrantDestinationPhase)))
                MigrantDestinations.Add(mdest);
        }

        /// <summary>Loop through each cohort in this LifeCyclePhase to calculate development, mortality, graduation and reproduciton</summary>
        public void Process()
        {
            if (Cohorts?.Count > 500)  //Check cohort number are not becomming silly
                throw new Exception(Apsim.FullPath(this) + " has over 500 cohorts which is to many really.  This is why your simulation is slow and the data store is about to chuck it in.  Check your " + this.Parent.Name.ToString() + " model to ensure development and mortality are sufficient.");

            ZeorDeltas(); //Zero reporting properties for daily summing

            foreach (ProgenyDestinationPhase pdest in ProgenyDestinations)
                pdest.ProgenyToDestination = 0;

            if (Cohorts != null)
            {
                // Calculate daily deltas
                foreach (Cohort c in Cohorts)
                {
                    CurrentCohort = c;
                    c.ChronologicalAge += 1;
                    //Do development for each cohort
                    c.PhysiologicalAge = Math.Min(1.0, c.PhysiologicalAge + development.Value());
                    //Do mortality for each cohort
                    c.Mortalities = mortality.Value();
                    Mortalities += c.Mortalities;
                    c.Population = Math.Max(0.0, c.Population - c.Mortalities);
                    //Do reproduction for each cohort
                    Progeny += reproduction.Value();
                    foreach (ProgenyDestinationPhase dest in ProgenyDestinations)
                        dest.ProgenyToDestination += reproduction.Value() * dest.ProportionOfProgeny;
                    //Do migration for each cohort
                    Migrants += migration.Value();
                    if (Migrants > 0)
                    {
                        if (MigrantDestinations.Count == 0)
                            throw new Exception(Apsim.FullPath(this) + " is predicting values for migration but has no MigrationDestinationPhase specified");
                        double MtotalProportion = 0;
                        foreach (MigrantDestinationPhase mdest in MigrantDestinations)
                        {
                            double cohortMigrants = migration.Value() * mdest.ProportionOfMigrants;
                            if ((MigrantDestinations.Count == 0) && (cohortMigrants > 0))
                                throw new Exception(Apsim.FullPath(this) + " is predicting values for migration but has not MigrantDestinationPhase specified");
                            if (cohortMigrants > 0)
                            {
                                IModel zone = Apsim.Parent(this.Parent, typeof(Zone));
                                LifeCycle mDestinationCycle = Apsim.Find(zone, mdest.NameOfLifeCycleForMigrants) as LifeCycle;
                                if (mDestinationCycle == null)
                                    throw new Exception(Apsim.FullPath(this) + " could not find a destination LifeCycle for migrants called " + mdest.NameOfLifeCycleForMigrants);
                                LifeCyclePhase mDestinationPhase = Apsim.Child(mDestinationCycle, mdest.NameOfPhaseForMigrants) as LifeCyclePhase;
                                if (mDestinationPhase == null)
                                    throw new Exception(Apsim.FullPath(this) + " could not find a destination LifeCyclePhase for migrants called " + mdest.NameOfPhaseForMigrants);
                                mDestinationPhase.NewCohort(cohortMigrants, c.ChronologicalAge, c.PhysiologicalAge);
                                MtotalProportion += mdest.ProportionOfMigrants;
                            }
                        }
                        if ((MtotalProportion > 1.001) && (Migrants > 0))
                            throw new Exception("The sum of ProportionOfMigrants values in " + Apsim.FullPath(this) + " ProgenyDestinationPhases is greater than 1.0");
                    }
                }

                // Add progeny into destination phase
                if ((ProgenyDestinations.Count == 0)&&(Progeny > 0))
                        throw new Exception(Apsim.FullPath(this) + " is predicting values for reproduction but has no ProgenyDestinationPhase specified");
                double PtotalProportion = 0;
                foreach (ProgenyDestinationPhase pdest in ProgenyDestinations)
                {
                    if (pdest.ProgenyToDestination>0)
                    {
                        IModel zone = Apsim.Parent(this.Parent, typeof(Zone));
                        LifeCycle pDestinationCylce = Apsim.Find(zone, pdest.NameOfLifeCycleForProgeny) as LifeCycle;
                        if (pDestinationCylce == null)
                            throw new Exception(Apsim.FullPath(this) + " could not find a destination LifeCycle for progeny called " + pdest.NameOfLifeCycleForProgeny);
                        LifeCyclePhase pDestinationPhase = Apsim.Child(pDestinationCylce, pdest.NameOfPhaseForProgeny) as LifeCyclePhase;
                        if (pDestinationPhase == null)
                            throw new Exception(Apsim.FullPath(this) + " could not find a destination LifeCyclePhase for progeny called " + pdest.NameOfPhaseForProgeny);
                        pDestinationPhase.NewCohort(pdest.ProgenyToDestination, 0, 0);
                        PtotalProportion += pdest.ProportionOfProgeny;
                    }
                }
                if (((PtotalProportion < 0.999) || (PtotalProportion > 1.001))&&(Progeny > 0))
                    throw new Exception("The sum of ProportionOfProgeny values in " + Apsim.FullPath(this) + " ProgenyDestinationPhases does not equal 1.0");


                // Move garduates to destination phase
                foreach (Cohort c in Cohorts.ToArray())
                {
                    if (c.PhysiologicalAge >= 1.0) //Members ready to graduate or die
                    {
                        if (LifeCyclePhaseForGraduates != null)
                            Graduates += c.Population; //Members graduate
                        else
                            Mortalities += c.Population; //Members die
                        Cohorts.Remove(c); //Remove mature cohort
                    }

                    if (c.Population < 0.001)  //Remove cohort if all members dead
                        Cohorts.Remove(c);  
                }

                if (Graduates > 0)  //Promote graduates to cohort in next LifeCyclePhase
                    LifeCyclePhaseForGraduates?.NewCohort(Graduates, 0.0, 0.0);
            }
        }

        /// <summary>Construct a new cohort and add it to Cohorts</summary>
       public void NewCohort(double population, double chronologicalAge, double physiologicalAge)
        {
            if (Cohorts == null)
                Cohorts = new List<Cohort>();
            Cohort a = new Cohort(this);
            a.Population = population;
            a.ChronologicalAge = chronologicalAge;
            a.PhysiologicalAge = physiologicalAge;
            this.Cohorts.Add(a);
        }
        
        /// <summary>Zero all time step variables</summary>
        public void ZeorDeltas()
        {
            Immigrants = 0;
            DevelopmentRate = 0;
            Mortalities = 0;
            Graduates = 0;
            Progeny = 0;
            Migrants = 0;
        }
    }
}
