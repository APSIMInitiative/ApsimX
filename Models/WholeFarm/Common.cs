using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Reasons for a change in herd
	/// </summary>
	public enum HerdChangeReason
	{
		/// <summary>
		/// This individual remains in herd
		/// </summary>
		None,
		/// <summary>
		/// Individual died
		/// </summary>
		Died,
		/// <summary>
		/// Individual born
		/// </summary>
		Born,
		/// <summary>
		/// Trade individual sold weight/age
		/// </summary>
		TradeSale,
		/// <summary>
		/// Dry breeder sold
		/// </summary>
		DryBreederSale,
		/// <summary>
		/// Excess breeder sold
		/// </summary>
		ExcessBreederSale,
		/// <summary>
		/// Excess bull sold
		/// </summary>
		ExcessBullSale,
		/// <summary>
		/// Individual reached maximim age and sold
		/// </summary>
		MaxAgeSale,
		/// <summary>
		/// Individual reached sale weight or age
		/// </summary>
		AgeWeightSale,
		/// <summary>
		/// Trade individual purchased
		/// </summary>
		TradePurchase,
		/// <summary>
		/// Heifer purchased
		/// </summary>
		HeiferPurchase,
		/// <summary>
		/// Breeding sire purchased
		/// </summary>
		SirePurchase,
		/// <summary>
		/// Individual consumed by household
		/// </summary>
		Consumed,
		/// <summary>
		/// Destocking sale
		/// </summary>
		DestockSale,
		/// <summary>
		/// Restocking purchase
		/// </summary>
		RestockPurchase,
		/// <summary>
		/// Initial herd
		/// </summary>
		InitialHerd
	}

	/// <summary>
	/// Animal pricing style
	/// </summary>
	public enum PricingStyleType
	{
		/// <summary>
		/// Value per head
		/// </summary>
		perHead,
		/// <summary>
		/// Value per kg live weight
		/// </summary>
		perKg
	}

	/// <summary>
	/// Animal payment style
	/// </summary>
	public enum PaymentStyleType
	{
		/// <summary>
		/// Fixed price
		/// </summary>
		Fixed,
		/// <summary>
		/// Amount per head
		/// </summary>
		perHead,
		/// <summary>
		/// Amount per adult equivilant
		/// </summary>
		perAE,
		/// <summary>
		/// Proportion of total sales
		/// </summary>
		ProportionOfTotalSales,
		/// <summary>
		/// Amount per hectare
		/// </summary>
		perHa
	}

	/// <summary>
	/// Labour allocation unit type
	/// </summary>
	public enum LabourUnitType
	{
		/// <summary>
		/// Fixed price
		/// </summary>
		Fixed,
		/// <summary>
		/// Labour per head
		/// </summary>
		perHead,
		/// <summary>
		/// Labour per adult equivilant
		/// </summary>
		perAE,
		/// <summary>
		/// Labour per kg
		/// </summary>
		perKg,
	}

	/// <summary>
	/// Types of measures of area with value representing the number of hectares
	/// </summary>
	public enum UnitsOfAreaType
	{
		/// <summary>
		/// Hectares
		/// </summary>
		Hectares = 1,
		/// <summary>
		/// Square kilometres
		/// </summary>
		SquareKilometres = 100
	}

	/// <summary>
	/// Ruminant feeding styles
	/// </summary>
	public enum RuminantFeedActivityTypes
	{
		/// <summary>
		/// Feed specified amount daily in selected months
		/// </summary>
		SpecifiedDailyAmount,
		/// <summary>
		/// Feed proportion of animal weight in selected months
		/// </summary>
		ProportionOfWeight,
		/// <summary>
		/// Feed proportion of potential intake
		/// </summary>
		ProportionOfPotentialIntake,
		/// <summary>
		/// Feed proportion of remaining amount required
		/// </summary>
		ProportionOfRemainingIntakeRequired
	}

	/// <summary>
	/// Possible actions when only partial requested resources are available
	/// </summary>
	public enum OnPartialResourcesAvailableActionTypes
	{
		/// <summary>
		/// Report error and stop simulation
		/// </summary>
		ReportErrorAndStop,
		/// <summary>
		/// Do not perform activity in this time step
		/// </summary>
		SkipActivity,
		/// <summary>
		/// Receive resources available and perform activity
		/// </summary>
		UseResourcesAvailable
	}

	/// <summary>
	/// Possible actions when only partial requested resources are available
	/// </summary>
	public enum OnMissingResourceActionTypes
	{
		/// <summary>
		/// Report error and stop simulation
		/// </summary>
		ReportErrorAndStop,
		/// <summary>
		/// Report warning to summary
		/// </summary>
		ReportWarning,
		/// <summary>
		/// Ignore missing resources and return null
		/// </summary>
		Ignore
	}

	/// <summary>
	/// Types of units of erea to use.
	/// </summary>
	public enum UnitsOfAreaTypes
	{
		/// <summary>
		/// Square km
		/// </summary>
		Squarekm,
		/// <summary>
		/// Hectares
		/// </summary>
		Hectares
	}
}
