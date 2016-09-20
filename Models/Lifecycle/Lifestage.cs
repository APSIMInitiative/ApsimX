using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Lifecycle
{
    /// <summary>
    /// 
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
        public Cohort CurrentCohort;

        private Lifecycle OwningCycle;
        private List<Cohort> CohortList;
        private List<Lifestage> StageLinks;

        /// <summary>
        /// 
        /// </summary>
        public Lifestage(Lifecycle owner)
        {
            OwningCycle = owner;
            CohortList = new List<Cohort>();
            StageLinks = new List<Lifestage>();
        }

        /// <summary>
        /// Process the lifestage which involves configured functions and promoting cohorts to linked stages.
        /// </summary>
        public void Process()
        {
            //process this stage
            foreach (Cohort currentCohort in CohortList)
            {
                //apply functions to cohort

                currentCohort.AgeCohort();
            }

            PromoteGraduates();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Lifestage NextLifeStage()
        {
            if (StageLinks.Count > 0)
                return StageLinks[0];
            else
                return null;
        }

        /// <summary>
        /// Link to another stage
        /// </summary>
        /// <param name="link"></param>
        public void LinkNext(Lifestage link)
        {
            StageLinks.Add(link);
        }

        /// <summary>
        /// Move cohort(s) on to the next stage
        /// </summary>
        public void PromoteGraduates()
        {
            //some logic required here to determine what gets promoted to the next stage


            if (StageLinks[0] != null)
            {
                //move the first arrived cohort to the next lifestage
                Cohort current = CohortList[0];
                Cohort newCohort = StageLinks[0].NewCohort();
                newCohort.ChronoAge = current.ChronoAge;
                newCohort.PhenoAge = 0;
                newCohort.Count = current.Count;
                CohortList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Cohort NewCohort()
        {
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
        /// 
        /// </summary>
        public void Clear()
        {
            CohortList.Clear();
            StageLinks.Clear();
        }
    }
}
