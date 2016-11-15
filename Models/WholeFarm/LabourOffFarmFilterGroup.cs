using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	///<summary>
	/// Contains a group of filters to identify individual ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(LabourActivityOffFarm))]
	public class LabourOffFarmFilterGroup : Model ,ILabourFilterGroup
	{
		/// <summary>
		/// Amount provided from resource or arbitrator
		/// </summary>
		[XmlIgnore]
		public double AmountProvided { get; set; }

		/// <summary>
		/// Daily labour rate
		/// </summary>
		[Description("Daily labour rate")]
		public double DailyRate { get; set; }

		/// <summary>
		/// Days worked
		/// </summary>
		[Description("Days work available each month")]
		public double[] DaysWorkAvailableEachMonth { get; set; }


		/// <summary>
		/// Number of people
		/// </summary>
		[Description("Number of people")]
		public double NumberOfPeople { get; set; }

		/// <summary>
		/// Labour priority (lower the value the greater the priority)
		/// </summary>
		public int Priority { get; set; }
	}
}
