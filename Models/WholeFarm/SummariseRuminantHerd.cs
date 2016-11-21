using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>Ruminant summary</summary>
	/// <summary>This activity summarizes ruminant herds for reporting</summary>
	/// <summary>Remove if you do not need herd summaries</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class SummariseRuminantHerd:Model
	{
		[Link]
		private Resources Resources = null;

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
		/// Function to age individuals and remove those that died in timestep
		/// This needs to be undertaken prior to herd management
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("EndOfMonth")]
		private void OnEndOfMonth(object sender, EventArgs e)
		{
			// summary report.

			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			// group by breed
			foreach (var breedGroup in herd.GroupBy(a => a.Breed))
			{
				foreach (var herdGroup in breedGroup.GroupBy(a => a.HerdName))
				{
					foreach (var sexGroup in herdGroup.GroupBy(a => a.Gender))
					{
						//// sucklings
						//var sucklings = sexGroup.Where(a => !a.Weaned);
						//if(sucklings.Count() > 0)
						//{
						//}

						// weaned
						foreach (var ageGroup in sexGroup.OrderBy(a => a.Age).GroupBy(a => Math.Truncate(a.Age / 12.0)))
						{
							ReportDetails = new HerdReportItemGeneratedEventArgs();
							ReportDetails.Breed = breedGroup.Key;
							ReportDetails.Herd = herdGroup.Key;
							ReportDetails.Age = Convert.ToInt32(ageGroup.Key);
							ReportDetails.Sex = sexGroup.Key.ToString().Substring(0,1);
							ReportDetails.Number = ageGroup.Sum(a => a.Number);
							ReportDetails.AverageWeight = ageGroup.Average(a => a.Weight);
							ReportDetails.AverageWeightGain = ageGroup.Average(a => a.WeightGain);
							ReportDetails.AverageIntake = ageGroup.Average(a => (a.Intake))/30.4;
							ReportDetails.AdultEquivalents = ageGroup.Sum(a => a.AdultEquivalent);
							if(sexGroup.Key== Sex.Female)
							{
								ReportDetails.NumberPregnant = ageGroup.Cast<RuminantFemale>().Where(a => a.IsPregnant).Count();
								ReportDetails.NumberOfBirths = ageGroup.Cast<RuminantFemale>().Where(a => a.BirthDue).Sum(a => a.SucklingOffspring.Count());
							}
							else
							{
								ReportDetails.NumberPregnant = 0;
								ReportDetails.NumberOfBirths = 0;
							}
							ReportItemGenerated(ReportDetails);
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
	}

}
