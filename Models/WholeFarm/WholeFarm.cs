using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>
	/// WholeFarm Zone to control simulation
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Simulation))]
	public class WholeFarm: Zone
	{
		[Link]
		ISummary Summary = null;
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Seed for random number generator (0 uses clock)
		/// </summary>
		[Description("Random number generator seed (0 to use clock)")]
		public int RandomSeed { get; set; }

		private static Random randomGenerator;

		/// <summary>
		/// Access the WholeFarm random number generator
		/// </summary>
		[Description("Random number generator for simulation")]
		public static Random RandomGenerator { get { return randomGenerator; } }

		/// <summary>
		/// Index of the simulation Climate Region
		/// </summary>
		[Description("Climate region index")]
		public int ClimateRegion { get; set; }

		/// <summary>
		/// Ecological indicators calculation interval (in months, 1 monthly, 12 annual)
		/// </summary>
		[Description("Ecological indicators calculation interval (in months, 1 monthly, 12 annual)")]
		public int EcologicalIndicatorsCalculationInterval { get; set; }

		/// <summary>
		/// End of month to calculate ecological indicators
		/// </summary>
		[Description("End of month to calculate ecological indicators")]
		public int EcologicalIndicatorsCalculationMonth { get; set; }

		/// <summary>
		/// Month this overhead is next due.
		/// </summary>
		[XmlIgnore]
		public DateTime EcologicalIndicatorsNextDueDate { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			if (RandomSeed < Int32.MinValue || RandomSeed > Int32.MaxValue)
			{
				string error = String.Format("Random generator seed (WholeFarm Zone) must be an integer in range of Int32."+ Environment.NewLine+"Using clock to set random number generator");
				Summary.WriteWarning(this, error);
				randomGenerator = new Random();
			}
			else if (RandomSeed==0)
			{
				randomGenerator = new Random();
			}
			else
			{
				randomGenerator = new Random(RandomSeed);
			}

			if(Clock.StartDate.Day != 1)
			{
				string error = String.Format("WholeFarm must commence on the first day of a month. Invalid start date {0}", Clock.StartDate.ToShortDateString());
				Summary.WriteWarning(this, error);
			}

			if (EcologicalIndicatorsCalculationMonth <= 0)
			{
				string error = "Calculation month for Ecological indicators must be between 1 and 12 in (WholeFarm)";
				throw new Exception(error);
			}
			if (EcologicalIndicatorsCalculationInterval <= 0)
			{
				string error = "Interval (months) for calculation of Ecological indicators must be greater or equal to 1 in (WholeFarm)";
				throw new Exception(error);
			}

			if (EcologicalIndicatorsCalculationMonth >= Clock.StartDate.Month)
			{
				EcologicalIndicatorsNextDueDate = new DateTime(Clock.StartDate.Year, EcologicalIndicatorsCalculationMonth, Clock.StartDate.Day);
			}
			else
			{
				EcologicalIndicatorsNextDueDate = new DateTime(Clock.StartDate.Year, EcologicalIndicatorsCalculationMonth, Clock.StartDate.Day);
				while (Clock.StartDate > EcologicalIndicatorsNextDueDate)
				{
					EcologicalIndicatorsNextDueDate = EcologicalIndicatorsNextDueDate.AddMonths(EcologicalIndicatorsCalculationInterval);
				}
			}

		}

		/// <summary>
		/// Method to determine if this is the month to calculate ecological indicators
		/// </summary>
		/// <returns></returns>
		public bool IsEcologicalIndicatorsCalculationMonth()
		{
			return this.EcologicalIndicatorsNextDueDate.Year == Clock.Today.Year & this.EcologicalIndicatorsNextDueDate.Month == Clock.Today.Month;
		}

		/// <summary>Data stores to clear at start of month</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("EndOfMonth")]
		private void OnEndOfMonth(object sender, EventArgs e)
		{
			if(IsEcologicalIndicatorsCalculationMonth())
			{
				this.EcologicalIndicatorsNextDueDate = this.EcologicalIndicatorsNextDueDate.AddMonths(this.EcologicalIndicatorsCalculationInterval);
			}
		}


	}
}
