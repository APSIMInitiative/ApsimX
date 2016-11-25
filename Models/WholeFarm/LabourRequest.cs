using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Labour request item
	/// </summary>
	[Serializable]
	public class LabourRequest
	{
		/// <summary>
		/// Requesting Labour Activity model name
		/// </summary>
		public Model Activity { get; set; }

		/// <summary>
		/// Amount requested
		/// </summary>
		public double Amount { get; set; }

		/// <summary>
		/// Requesting Ruminant individual/cohort
		/// </summary>
		public ILabourFilterGroup Requestor { get; set; }
	}
}
