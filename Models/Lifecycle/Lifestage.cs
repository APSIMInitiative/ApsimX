using System;
using System.Collections.Generic;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Lifecycle
{
    /// <summary>
    /// A lifestage is a developmental segment of a lifecycle. It contains cohorts.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Lifecycle))]
    public class Lifestage: Model
    {
        /// <summary>
        /// Reference to the current cohort
        /// </summary>
        [XmlIgnore]
        public Cohort CurrentCohort { get; set; }

        /// <summary>
        /// Owning lifecycle
        /// </summary>
        public Lifecycle OwningCycle;

        [NonSerialized]
        private List<Cohort> CohortList;

        [NonSerialized]
        private List<LifestageProcess> ProcessList;

        /// <summary>
        /// 
        /// </summary>
        public Lifestage()
        {
            
        }

        /// <summary>
        /// Return the count of cohorts in this Lifestage
        /// </summary>
        public int CohortCount
        {
            get
            {
                if (CohortList != null)
                {
                    return CohortList.Count;
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
                if (CohortList != null)
                {
                    foreach (Cohort aCohort in CohortList)
                    {
                        sum += aCohort.Count;
                    }
                }
                return sum;
            }
        }

        /// <summary>
        /// Process the lifestage which involves configured functions and promoting cohorts to linked stages.
        /// </summary>
        public void Process()
        {
            if (CohortList != null)
            {
                Cohort aCohort;
                int count = CohortList.Count;

                // for each cohort in the lifestage
                for (int i = 0; i < count; i++)
                {
                    aCohort = CohortList[i];
                    //apply functions to cohort
                    CurrentCohort = aCohort;
                    //iterate throught the process children of this lifestage
                    foreach (LifestageProcess proc in ProcessList)
                    {
                        proc.ProcessCohort(aCohort);    // execute process function and may include transfer to another lifestage
                    }
                    aCohort.AgeCohort();                // finally age the creatures in the cohort
                }
                RemoveEmptyCohorts();                   // remove any empty cohorts from the cohortlist
            }
        }

        /// <summary>
        /// Move cohort on to the next stage
        /// </summary>
        public void PromoteGraduates(Cohort srcCohort, Lifestage destStage, double count)
        {
            if (destStage != null)
            {
                //move the count creatures into a new cohort
                Cohort newCohort = destStage.NewCohort();
                newCohort.ChronoAge = srcCohort.ChronoAge;
                newCohort.PhenoAge = 0;
                newCohort.PhysiologicalAge = 0;
                newCohort.Count = count;
                srcCohort.Count = srcCohort.Count - count;
            }
            else
            {
                throw new Exception("Destination stage is incorrectly specified");
            }
        }

        /// <summary>
        /// The source cohort reproduces and sends count creatures to the destination stage.
        /// </summary>
        /// <param name="srcCohort"></param>
        /// <param name="destStage"></param>
        /// <param name="count"></param>
        public void Reproduce(Cohort srcCohort, Lifestage destStage, double count)
        {
            if (destStage != null)
            {
                // move the count creatures into a new cohort
                Cohort newCohort = null;
                // find a cohort in the destStage that has PhenoAge = 0
                int i = 0;
                while (i < destStage.CohortList.Count)
                {
                    if (destStage.CohortList[i].PhenoAge == 0)
                    {
                        newCohort = destStage.CohortList[i];
                        i = destStage.CohortList.Count; // terminate loop
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
        /// <returns></returns>
        public Cohort NewCohort()
        {
            if (CohortList == null)
                CohortList = new List<Cohort>();

            Cohort a = new Cohort(this);
            CohortList.Add(a);
            return a;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveLastCohort()
        {
            CohortList.RemoveAt(CohortList.Count - 1);
        }

        /// <summary>
        /// Cleanup the list of cohorts by removing any that have no population.
        /// </summary>
        public void RemoveEmptyCohorts()
        {
            int i = CohortList.Count - 1;
            while (i >= 0)
            {
                if (CohortList[i].Count < 0.001)
                    CohortList.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aCohort"></param>
        public void Remove(Cohort aCohort)
        {
            CohortList.Remove(aCohort);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            CohortList.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ProcessList = new List<LifestageProcess>();
            foreach (LifestageProcess proc in Apsim.Children(this, typeof(LifestageProcess)))
            {
                ProcessList.Add(proc);
            }
        }
    }
}
