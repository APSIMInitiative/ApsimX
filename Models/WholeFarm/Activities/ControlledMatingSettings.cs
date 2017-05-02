using Models.Core;
using Models.WholeFarm.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant breeding activity</summary>
	/// <summary>This activity provides all functionality for ruminant breeding up until natural weaning</summary>
	/// <summary>It will be applied to the supplied herd if males and females are located together</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityBreed))]
	public class ControlledMatingSettings: WFModel
	{
		/// <summary>
		/// Get the Clock.
		/// </summary>
		[XmlIgnore]
		[Link]
		Clock Clock = null;

		[XmlIgnore]
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// The payment interval (in months, 1 monthly, 12 annual)
		/// </summary>
		[Description("Breeding interval (in months, 1 monthly, 12 annual)")]
		public int BreedInterval { get; set; }

		/// <summary>
		/// First month to pay overhead
		/// </summary>
		[Description("First month to perform breeding (1-12)")]
		public int MonthDue { get; set; }

		/// <summary>
		/// Month this overhead is next due.
		/// </summary>
		[XmlIgnore]
		public DateTime NextDueDate { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// check payment interval > 0
			if (BreedInterval <= 0)
			{
				Summary.WriteWarning(this, String.Format("Controlled mating interval must be greater than 1 ({0})", this.Name));
				throw new Exception(String.Format("Invalid controlled mating interval supplied for overhead {0}", this.Name));
			}

			if (MonthDue >= Clock.StartDate.Month)
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, MonthDue, Clock.StartDate.Day);
			}
			else
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, MonthDue, Clock.StartDate.Day);
				while (Clock.StartDate > NextDueDate)
				{
					NextDueDate = NextDueDate.AddMonths(BreedInterval);
				}
			}
		}

		/// <summary>
		/// Determines if this is the due month for controlled mating
		/// </summary>
		public bool IsDueDate()
		{
			return (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month);
		}

		/// <summary>
		/// Updates to next due date
		/// </summary>
		public void UpdateDueDate()
		{
			this.NextDueDate = this.NextDueDate.AddMonths(this.BreedInterval);
		}

	}
}
