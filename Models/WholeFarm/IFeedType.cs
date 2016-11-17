using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Interface for feet types
	/// </summary>
	public interface IFeedType: IResourceType
	{
		/// <summary>
		/// Dry Matter (%)
		/// </summary>
		[Description("Dry Matter (%)")]
		double DryMatter { get; set; }

		/// <summary>
		/// Dry Matter Digestibility (%)
		/// </summary>
		[Description("Dry Matter Digestibility (%)")]
		double DMD { get; set; }

		/// <summary>
		/// Nitrogen (%)
		/// </summary>
		[Description("Nitrogen (%)")]
		double Nitrogen { get; set; }

		/// <summary>
		/// Starting Amount (kg)
		/// </summary>
		[Description("Starting Amount (kg)")]
		double StartingAmount { get; set; }

		/// <summary>
		/// Amount (kg)
		/// </summary>
		double Amount { get; }
	}
}
