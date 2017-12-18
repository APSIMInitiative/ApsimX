using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.CLEM.Activities;

namespace Models.CLEM
{
    /// <summary>Ruminant summary</summary>
    /// <summary>This activity summarizes ruminant herds for reporting</summary>
    /// <summary>Remove if you do not need monthly herd summaries</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("This component will generate a herd summary report. It uses the current timing rules and herd filters applied to its branch of the user interface tree. It also requires a suitable report object to be present.")]
    public class SummariseRuminantHerd: CLEMModel
    {
        [Link]
        private ResourcesHolder Resources = null;
        private int timestep = 0;
        /// <summary>
        /// Report item was generated event handler
        /// </summary>
        public event EventHandler OnReportItemGenerated;

        /// <summary>
        /// The details of the summary group for reporting
        /// </summary>
        [XmlIgnore]
        public HerdReportItemGeneratedEventArgs ReportDetails { get; set; }

        /// <summary>
        /// Report item generated and ready for reporting 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ReportItemGenerated(HerdReportItemGeneratedEventArgs e)
        {
            if (OnReportItemGenerated != null)
                OnReportItemGenerated(this, e);
        }

        /// <summary>
        /// Function to summarise the herd based on cohorts each month
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            timestep++;
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> herd = ruminantHerd.Herd;

            // one individual
            herd = herd.ToList();

            // group by breed
            foreach (var breedGroup in herd.GroupBy(a => a.Breed))
            {
                // group by herd
                foreach (var herdGroup in breedGroup.GroupBy(a => a.HerdName))
                {
                    // group by sex
                    foreach (var sexGroup in herdGroup.GroupBy(a => a.Gender))
                    {
                        // weaned
                        foreach (var ageGroup in sexGroup.OrderBy(a => a.Age).GroupBy(a => Math.Truncate(a.Age / 12.0)))
                        {
                            ReportDetails = new HerdReportItemGeneratedEventArgs();
                            ReportDetails.TimeStep = timestep;
                            ReportDetails.Breed = breedGroup.Key;
                            ReportDetails.Herd = herdGroup.Key;
                            ReportDetails.Age = Convert.ToInt32(ageGroup.Key);
                            ReportDetails.Sex = sexGroup.Key.ToString().Substring(0,1);
                            ReportDetails.Number = ageGroup.Sum(a => a.Number);
                            ReportDetails.AverageWeight = ageGroup.Average(a => a.Weight);
                            ReportDetails.AverageWeightGain = ageGroup.Average(a => a.WeightGain);
                            ReportDetails.AverageIntake = ageGroup.Average(a => (a.Intake+a.MilkIntake)); //now daily/30.4;
                            ReportDetails.AdultEquivalents = ageGroup.Sum(a => a.AdultEquivalent);
                            if(sexGroup.Key== Sex.Female)
                            {
                                ReportDetails.NumberPregnant = ageGroup.Cast<RuminantFemale>().Where(a => a.IsPregnant).Count();
                                ReportDetails.NumberLactating = ageGroup.Cast<RuminantFemale>().Where(a => a.IsLactating).Count();
                                ReportDetails.NumberOfBirths = ageGroup.Cast<RuminantFemale>().Sum(a => a.NumberOfBirthsThisTimestep);
                            }
                            else
                            {
                                ReportDetails.NumberPregnant = 0;
                                ReportDetails.NumberLactating = 0;
                                ReportDetails.NumberOfBirths = 0;
                            }
                            
                            ReportItemGenerated(ReportDetails);

                            // reset birth count
                            if (sexGroup.Key == Sex.Female)
                            {
                                ageGroup.Cast<RuminantFemale>().ToList().ForEach(a => a.NumberOfBirthsThisTimestep = 0);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// New herd report item generated event args
    /// </summary>
    [Serializable]
    public class HerdReportItemGeneratedEventArgs : EventArgs
    {
        /// <summary>
        /// Timestep
        /// </summary>
        public int TimeStep { get; set; }
        /// <summary>
        /// Breed of individuals
        /// </summary>
        public string Breed { get; set; }
        /// <summary>
        /// Herd of individuals
        /// </summary>
        public string Herd { get; set; }
        /// <summary>
        /// Age of individuals (lower bound of year class)
        /// </summary>
        public double Age { get; set; }
        /// <summary>
        /// Sex of individuals
        /// </summary>
        public string Sex { get; set; }
        /// <summary>
        /// Number of individuals
        /// </summary>
        public double Number { get; set; }
        /// <summary>
        /// Average weight of individuals
        /// </summary>
        public double AverageWeight { get; set; }
        /// <summary>
        /// Average weight gain of individuals
        /// </summary>
        public double AverageWeightGain { get; set; }
        /// <summary>
        /// Average intake of individuals
        /// </summary>
        public double AverageIntake { get; set; }
        /// <summary>
        /// Adult equivalent of individuals
        /// </summary>
        public double AdultEquivalents { get; set; }
        /// <summary>
        /// Births of individual
        /// </summary>
        public int NumberOfBirths { get; set; }
        /// <summary>
        /// Number pregnant
        /// </summary>
        public int NumberPregnant { get; set; }
        /// <summary>
        /// Number lactating
        /// </summary>
        public int NumberLactating { get; set; }
    }

}
