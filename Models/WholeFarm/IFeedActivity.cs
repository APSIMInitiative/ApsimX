using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Interface of a Feed activity.
	/// </summary>
	public interface IFeedActivity: IModel
	{
		/// <summary>
		/// Feeding priority (1 high, 10 low)
		/// </summary>
		[Description("Feeding priority")]
		int FeedPriority { get; set; }

		/// <summary>
		/// Feed type link
		/// </summary>
		IFeedType FeedType { get; set; }
	}
}
