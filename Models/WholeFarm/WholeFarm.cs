using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			if (RandomSeed < Int32.MinValue || RandomSeed > Int32.MaxValue)
			{
				string error = String.Format("Random generator seed (WholeFarm Zone) must be an integer in range of Int32");
				Summary.WriteWarning(this, error);
			}

			if (RandomSeed==0)
			{
				randomGenerator = new Random();
			}
			else
			{
				randomGenerator = new Random(RandomSeed);
			}

			if(Clock.StartDate.Day != 1)
			{
				string error = String.Format("WholeFarm must commence on the first day of a month. Invalid start date"+Clock.StartDate.ToShortDateString());
				Summary.WriteWarning(this, error);
			}
		}
	}
}
