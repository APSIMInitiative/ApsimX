using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM
{

    /// <summary>
    /// Crop store style
    /// </summary>
    public enum StoresForCrops
    {
        /// <summary>
        /// Food Store for Humans
        /// </summary>
        HumanFoodStore,
        /// <summary>
        /// Food Store for Animals
        /// </summary>
        AnimalFoodStore,
        /// <summary>
        /// Store for forage/pasture crops
        /// </summary>
        GrazeFoodStore,
        /// <summary>
        /// Store for inedible crop products
        /// </summary>
        ProductStore,
    }

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
        /// Individual died due to loss of weight
        /// </summary>
        DiedUnderweight,
        /// <summary>
        /// Individual died due to mortality rate
        /// </summary>
        DiedMortality,
        /// <summary>
        /// Individual born
        /// </summary>
        Born,
        /// <summary>
        /// Individual sold as marked for sale
        /// </summary>
        MarkedSale,
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
        /// Excess heifer sold
        /// </summary>
        ExcessHeiferSale,
        /// <summary>
        /// Excess sire sold
        /// </summary>
        ExcessSireSale,
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
        /// Breeder purchased
        /// </summary>
        BreederPurchase,
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
        InitialHerd,
        /// <summary>
        /// Fill initial herd to management levels
        /// </summary>
        FillInitialHerd,
        /// <summary>
        /// Reduce initial herd to management levels
        /// </summary>
        ReduceInitialHerd
    }

    /// <summary>
    /// Mustering timing type
    /// </summary>
    public enum MusterTimingType
    {
        /// <summary>
        /// At start of time step
        /// </summary>
        StartOfTimestep,
        /// <summary>
        /// At end of time step
        /// </summary>
        EndOfTimeStep
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
    /// Animal purchase or sale price style
    /// </summary>
    public enum PurchaseOrSalePricingStyleType
    {
        /// <summary>
        /// Both purchase and sale price
        /// </summary>
        Both,
        /// <summary>
        /// Purchase price
        /// </summary>
        Purchase,
        /// <summary>
        /// Sale price
        /// </summary>
        Sale
    }

    /// <summary>
    /// Crop payment style
    /// </summary>
    public enum CropPaymentStyleType
    {
        /// <summary>
        /// Fixed price
        /// </summary>
        Fixed,
        /// <summary>
        /// Amount per hectare
        /// </summary>
        perHa,
        /// <summary>
        /// Amount per tree
        /// </summary>
        perTree,
    }

    /// <summary>
    /// Crop payment style
    /// </summary>
    public enum ResourcePaymentStyleType
    {
        /// <summary>
        /// Fixed price
        /// </summary>
        Fixed,
        /// <summary>
        /// Amount per unit of resource
        /// </summary>
        perUnit,
        /// <summary>
        /// Amount per block of resource
        /// </summary>
        perBlock,
    }

    /// <summary>
    /// Animal payment style
    /// </summary>
    public enum AnimalPaymentStyleType
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
        /// Labour per hectare
        /// </summary>
        perHa,
        /// <summary>
        /// Labour per Tree
        /// </summary>
        perTree,
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
        /// <summary>
        /// Labour per unit
        /// </summary>
        perUnit,
    }

    /// <summary>
    /// Ruminant feeding styles
    /// </summary>
    public enum RuminantFeedActivityTypes
    {
        /// <summary>
        /// Feed specified amount daily to all individuals
        /// </summary>
        SpecifiedDailyAmount,
        /// <summary>
        /// Feed specified amount daily to each individual
        /// </summary>
        SpecifiedDailyAmountPerIndividual,
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
    /// Ruminant feeding styles
    /// </summary>
    public enum LabourFeedActivityTypes
    {
        /// <summary>
        /// Feed specified amount daily to each individual
        /// </summary>
        SpecifiedDailyAmountPerIndividual,
        /// <summary>
        /// Feed specified amount daily per AE
        /// </summary>
        SpecifiedDailyAmountPerAE,
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
    /// Style of HTML reporting
    /// </summary>
    public enum HTMLSummaryStyle
    {
        /// <summary>
        /// Determine best match
        /// </summary>
        Default,
        /// <summary>
        /// Main resource
        /// </summary>
        Resource,
        /// <summary>
        /// Sub resource
        /// </summary>
        SubResource,
        /// <summary>
        /// Sub resource nested
        /// </summary>
        SubResourceLevel2,
        /// <summary>
        /// Main activity
        /// </summary>
        Activity,
        /// <summary>
        /// Sub activity
        /// </summary>
        SubActivity,
        /// <summary>
        /// Sub activity nested
        /// </summary>
        SubActivityLevel2,
        /// <summary>
        /// Helper model
        /// </summary>
        Helper,
        /// <summary>
        /// FileReader model
        /// </summary>
        FileReader
    }

    /// <summary>
    /// Style of weaning rules
    /// </summary>
    public enum WeaningStyle
    {
        /// <summary>
        /// Age or weight achieved
        /// </summary>
        AgeOrWeight,
        /// <summary>
        /// Age achieved
        /// </summary>
        AgeOnly,
        /// <summary>
        /// Weight achieved
        /// </summary>
        WeightOnly
    }

    /// <summary>
    /// Method to use in determining a value of y from a given x in relationships 
    /// </summary>
    public enum RelationshipCalculationMethod
    {
        /// <summary>
        /// Use fixed values
        /// </summary>
        UseSpecifiedValues,
        /// <summary>
        /// Use linear interpolation
        /// </summary>
        Interpolation
    }

    /// <summary>
    /// Months of the year
    /// </summary>
    public enum MonthsOfYear
    {
        /// <summary>
        /// Not set
        /// </summary>
        NotSet = 0,
        /// <summary>
        /// Janyary
        /// </summary>
        January = 1,
        /// <summary>
        /// February
        /// </summary>
        February = 2,
        /// <summary>
        /// March
        /// </summary>
        March = 3,
        /// <summary>
        /// April
        /// </summary>
        April = 4,
        /// <summary>
        /// May
        /// </summary>
        May = 5,
        /// <summary>
        /// June
        /// </summary>
        June = 6,
        /// <summary>
        /// July
        /// </summary>
        July = 7,
        /// <summary>
        /// August
        /// </summary>
        August = 8,
        /// <summary>
        /// September
        /// </summary>
        September = 9,
        /// <summary>
        /// October
        /// </summary>
        October = 10,
        /// <summary>
        /// November
        /// </summary>
        November = 11,
        /// <summary>
        /// December
        /// </summary>
        December = 12
    }
}
