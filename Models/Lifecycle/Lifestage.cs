namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// # [Name]
    /// A lifestage is a developmental segment of a lifecycle. It contains cohorts.
    ///
    ///|Property          .|Type    .|Units  .|Description              .| 
    ///|---|---|---|:---|
    ///|CurrentCohort     |Cohort   |  |Reference to the current cohort    |
    ///|OwningCycle       |LifeCycle|  |The owning LifeCycle |
    ///|CohortCount       |int      |  |The count of cohorts in this LifeStage |
    ///|TotalPopulation   |double   |  |Population of all the cohorts in this LifeStage |
    ///|Populations       |double[] |  |Gets the array of cohort populations for this LifeStage |
    ///|Mortality         |double   |  |The current mortality numbers for this time step |
    ///|Migrants          |double   |  |The number of organisms migrating to another LifeStage for this time step |
    ///
    /// **Cohort**
    /// 
    ///|Property          .|Type    .|Units  .|Description              .| 
    ///|---|---|---|:---|
    ///|PhenoAge     |double   |time steps  |Developmental level (within a LifeStage)    |
    ///|ChronoAge    |double   |time steps  |Period of existence since start of egg(?) stage |
    ///|PhysiologicalAge |double |0-1 |The fraction of maturity for the cohort |
    ///|Count        |double |  |Count of organisms in this cohort |
    ///|Fecundity    |double |  |The fecundity for the time step |
    ///|Mortality    |double |  |The mortality for the time step |
    ///|OwningStage  |LifeStage|  |The LifeStage that owns this cohort |
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCycle))]
    public class LifeStage : Model
    {
        /// <summary>
        /// Reference to the current cohort
        /// </summary>
        [XmlIgnore]
        public Cohort CurrentCohort { get; set; }

        /// <summary>
        /// Owning lifecycle
        /// </summary>
        public LifeCycle OwningCycle;

        /// <summary>
        /// The list of cohorts in this stage.
        /// </summary>
        [JsonIgnore]
        public List<Cohort> Cohorts { get; set; }

        [NonSerialized]
        private double stepMortality;

        [NonSerialized]
        private double stepMigrants;

        [NonSerialized]
        private List<ILifeStageProcess> ProcessList;

        /// <summary>
        /// Default LifeStage constructor
        /// </summary>
        public LifeStage()
        {

        }

        /// <summary>
        /// Return the count of cohorts in this Lifestage
        /// </summary>
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

        /// <summary>
        /// Population of all the cohorts in this lifestage
        /// </summary>
        public double TotalPopulation
        {
            get
            {
                double sum = 0;
                if (Cohorts != null)
                {
                    foreach (Cohort aCohort in Cohorts)
                    {
                        sum += aCohort.Count;
                    }
                }
                return sum;
            }
        }

        /// <summary>
        /// Gets the array of cohort populations for this LifeStage
        /// </summary>
        public double[] Populations
        {
            get
            {
                double[] populations = new double[Cohorts.Count];
                for (int i = 0; i < Cohorts.Count; i++)
                {
                    populations[i] = Cohorts[i].Count;
                }

                return populations;
            }
        }

        /// <summary>
        /// The current mortality numbers for this timestep
        /// </summary>
        public double Mortality
        {
            get
            {
                return stepMortality;
            }
        }

        /// <summary>
        /// The number of organisms migrating to another lifestage for this timestep
        /// </summary>
        public double Migrants
        {
            get { return stepMigrants; }
        }

        /// <summary>
        /// Add the number of immigrants to this LifeStage
        /// </summary>
        /// <param name="number"></param>
        public void AddImmigrants(double number)
        {
            //create a new cohort
            Cohort immigrants = NewCohort();
            immigrants.Count = number;
            //get the state for a new cohort based on any pre-existing ones in this LifeStage
            if (Cohorts != null)
            {
                if (CohortCount > 1)
                {
                    immigrants.ChronoAge = Cohorts[0].ChronoAge;
                    immigrants.PhysiologicalAge = Cohorts[0].PhysiologicalAge;
                    immigrants.PhenoAge = Cohorts[0].PhenoAge;
                }
            }
        }

        /// <summary>
        /// Process the lifestage which involves configured functions and promoting cohorts to linked stages.
        /// </summary>
        public void Process()
        {
            foreach (ILifeStageProcess proc in ProcessList)
            {

                proc.Process(this);     // any pre processing/immigration
            }

            if (Cohorts != null)
            {
                Cohort aCohort;
                int count = Cohorts.Count;

                stepMigrants = 0;
                stepMortality = 0;
                // for each cohort in the lifestage
                for (int i = 0; i < count; i++)
                {
                    aCohort = Cohorts[i];
                    aCohort.Mortality = 0;              // can have multiple mortality processes in this stage
                    //apply functions to cohort
                    CurrentCohort = aCohort;
                    //iterate throught the process children of this lifestage
                    foreach (ILifeStageProcess proc in ProcessList)
                    {
                        proc.ProcessCohort(aCohort);    // execute process function and may include transfer to another lifestage
                    }
                    aCohort.AgeCohort();                // finally age the creatures in the cohort
                    stepMortality += aCohort.Mortality;
                }

                RemoveEmptyCohorts();                   // remove any empty cohorts from the cohortlist
            }
        }

        /// <summary>
        /// Move cohort on to the next stage
        /// </summary>
        public void PromoteGraduates(Cohort srcCohort, LifeStage destStage, double count)
        {
            if (destStage != null)
            {
                //move the count creatures into a new cohort
                Cohort newCohort = destStage.NewCohort();
                newCohort.ChronoAge = srcCohort.ChronoAge;
                newCohort.PhenoAge = 0;
                newCohort.PhysiologicalAge = 0;
                newCohort.Count = count;
                stepMigrants += count;
                srcCohort.Count -= count;
            }
            else
            {
                throw new Exception("Destination stage is incorrectly specified");
            }
        }

        /// <summary>
        /// The source cohort reproduces and sends count creatures to the destination stage.
        /// </summary>
        /// <param name="srcCohort">The source cohort</param>
        /// <param name="destStage">The destination LifeStage</param>
        /// <param name="count">The population for the migrated cohort</param>
        public void Reproduce(Cohort srcCohort, LifeStage destStage, double count)
        {
            if (destStage != null)
            {
                // move the count creatures into a new cohort
                Cohort newCohort = null;
                // find a cohort in the destStage that has PhenoAge = 0
                int i = 0;
                while (i < destStage.Cohorts.Count)
                {
                    if (destStage.Cohorts[i].PhenoAge <= 0)
                    {
                        newCohort = destStage.Cohorts[i];
                        i = destStage.Cohorts.Count; // terminate loop
                    }
                    i++;
                }

                if (newCohort == null)
                    newCohort = destStage.NewCohort();
                newCohort.ChronoAge = 0;
                newCohort.PhenoAge = 0;
                newCohort.PhysiologicalAge = 0;
                newCohort.Count += count;
            }
            else
            {
                throw new Exception("Destination stage is incorrectly specified");
            }
        }
        /// <summary>
        /// Construct a new cohort, add it to the list and return a reference to it.
        /// </summary>
        /// <returns>A new initialised cohort object</returns>
        public Cohort NewCohort()
        {
            if (Cohorts == null)
                Cohorts = new List<Cohort>();

            Cohort a = new Cohort(this);
            a.Count = 0;
            a.ChronoAge = 0;
            a.PhysiologicalAge = 0;
            Cohorts.Add(a);
            return a;
        }

        /// <summary>
        /// Remove the cohort at the end of the list
        /// </summary>
        public void RemoveLastCohort()
        {
            Cohorts.RemoveAt(Cohorts.Count - 1);
        }

        /// <summary>
        /// Cleanup the list of cohorts by removing any that have no population.
        /// </summary>
        public void RemoveEmptyCohorts()
        {
            int i = Cohorts.Count - 1;
            while (i >= 0)
            {
                if (Cohorts[i].Count < 0.001)
                    Cohorts.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// Remove a specified cohort item
        /// </summary>
        /// <param name="aCohort">The cohort object to be removed</param>
        public void Remove(Cohort aCohort)
        {
            Cohorts.Remove(aCohort);
        }

        /// <summary>
        /// Empty the cohort list
        /// </summary>
        public void Clear()
        {
            Cohorts.Clear();
        }

        /// <summary>
        /// Handle the start event and add LifeStage processes to the internal list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ProcessList = new List<ILifeStageProcess>();
            foreach (ILifeStageProcess proc in Apsim.Children(this, typeof(ILifeStageProcess)))
            {
                ProcessList.Add(proc);
            }
        }
    }
}
