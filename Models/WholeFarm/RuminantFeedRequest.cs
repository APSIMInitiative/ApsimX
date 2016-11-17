using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.WholeFarm
{
	/// <summary>
	/// Feed request item
	/// </summary>
	[Serializable]
	public class RuminantFeedRequest
	{
		/// <summary>
		/// Requesting Feed Activity model
		/// This is used to return the model name for output reporting
		/// </summary>
		public IFeedActivity FeedActivity { get; set; }

		/// <summary>
		/// Amount requested
		/// </summary>
		[Units("kg")]
		public double Amount { get; set; }

		/// <summary>
		/// Requesting Ruminant individual/cohort
		/// </summary>
		public Ruminant Requestor { get; set; }

		///// <summary>
		///// Fodder limits provided for pasture feeding
		///// </summary>
		//public FodderLimitsFilterGroup FodderLimitsValues { get; set; }
	}
}
