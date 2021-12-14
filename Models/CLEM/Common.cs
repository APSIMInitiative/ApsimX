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
        ExcessPreBreederSale,
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
    /// Reasons link to herd change for use with manual mark for sale
    /// </summary>
    public enum MarkForSaleReason
    {
        /// <summary>
        /// Reason not provided
        /// </summary>
        NotProvided = 0,
        /// <summary>
        /// Individual sold as marked for sale
        /// </summary>
        MarkedSale = 4,
        /// <summary>
        /// Individual reached sale weight or age
        /// </summary>
        AgeWeightSale = 12,
        /// <summary>
        /// Individual consumed by household
        /// </summary>
        Consumed = 15,
        /// <summary>
        /// Destocking sale
        /// </summary>
        DestockSale = 16,
        /// <summary>
        /// Dry breeder sold
        /// </summary>
        DryBreederSale = 6,
        /// <summary>
        /// Individual reached maximim age and sold
        /// </summary>
        MaxAgeSale = 10,
        /// <summary>
        /// Trade individual sold weight/age
        /// </summary>
        TradeSale = 5 
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
        perKg,
        /// <summary>
        /// Value per adult equivalent
        /// </summary>
        perAE,
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
        Fixed = 0,
        /// <summary>
        /// Amount per unit of land
        /// </summary>
        perUnitOfLand = 3,
        /// <summary>
        /// Amount per hectare
        /// </summary>
        perHa = 1,
        /// <summary>
        /// Amount per tree
        /// </summary>
        perTree = 2,
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
        perHa,
        /// <summary>
        /// Amount per unit of land
        /// </summary>
        perUnitOfLand
    }

    /// <summary>
    /// Labour allocation unit type
    /// </summary>
    public enum LabourUnitType
    {
        /// <summary>
        /// Fixed price
        /// </summary>
        Fixed = 0,
        /// <summary>
        /// Labour per unit of land
        /// </summary>
        perUnitOfLand = 7,
        /// <summary>
        /// Labour per hectare
        /// </summary>
        perHa = 1,
        /// <summary>
        /// Labour per Tree
        /// </summary>
        perTree = 2,
        /// <summary>
        /// Labour per head
        /// </summary>
        perHead = 3,
        /// <summary>
        /// Labour per adult equivilant
        /// </summary>
        perAE = 4,
        /// <summary>
        /// Labour per kg
        /// </summary>
        perKg = 5,
        /// <summary>
        /// Labour per unit
        /// </summary>
        perUnit = 6,
    }

    /// <summary>
    /// Labour limit type calculation type
    /// </summary>
    public enum LabourLimitType
    {
        /// <summary>
        /// Represents a rate or fixed days specified
        /// </summary>
        AsDaysRequired,
        /// <summary>
        /// Relates to the total days allowed
        /// </summary>
        AsTotalAllowed,
        /// <summary>
        /// As proportion of the days required
        /// </summary>
        ProportionOfDaysRequired,
    }

    /// <summary>
    /// Style to calculate hired labour payment
    /// </summary>
    public enum PayHiredLabourCalculationStyle
    {
        /// <summary>
        /// Use labour available in LabourAvailability for all hired labour
        /// </summary>
        ByAvailableLabour,
        /// <summary>
        /// Use the hired labour used in timestep
        /// </summary>
        ByLabourUsedInTimeStep
    }

    /// <summary>
    /// Ruminant feeding styles
    /// </summary>
    public enum RuminantFeedActivityTypes
    {
        /// <summary>
        /// A specified amount daily to all individuals
        /// </summary>
        SpecifiedDailyAmount,
        /// <summary>
        /// A specified amount daily to each individual
        /// </summary>
        SpecifiedDailyAmountPerIndividual,
        /// <summary>
        /// The proportion of animal weight in selected months
        /// </summary>
        ProportionOfWeight,
        /// <summary>
        /// The proportion of potential intake
        /// </summary>
        ProportionOfPotentialIntake,
        /// <summary>
        /// The proportion of remaining amount required
        /// </summary>
        ProportionOfRemainingIntakeRequired,
        /// <summary>
        /// A proportion of the feed pool available
        /// </summary>
        ProportionOfFeedAvailable
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
        FileReader,
        /// <summary>
        /// Filter model
        /// </summary>
        Filter
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

    /// <summary>
    /// Style selling resource
    /// </summary>
    public enum ResourceSellStyle
    {
        /// <summary>
        /// Specified amount
        /// </summary>
        SpecifiedAmount,
        /// <summary>
        /// Proportion of store
        /// </summary>
        ProportionOfStore,
        /// <summary>
        /// Proportion of last gain transaction
        /// </summary>
        ProportionOfLastGain,
        /// <summary>
        /// Reserve amount
        /// </summary>
        ReserveAmount,
        /// <summary>
        /// Reserve proportion
        /// </summary>
        ReserveProportion
    }

    /// <summary>
    /// Style of ruminant tag application
    /// </summary>
    public enum TagApplicationStyle
    {
        /// <summary>
        /// Add tag
        /// </summary>
        Add,
        /// <summary>
        /// Remove tag
        /// </summary>
        Remove
    }

    /// <summary>
    /// Style to identify different ruminant groups needed by activities
    /// </summary>
    public enum RuminantGroupStyle
    {
        /// <summary>
        /// No style specified
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Remove
        /// </summary>
        Remove = 5,

        /// <summary>
        /// Select females to remove
        /// </summary>
        RemoveFemales = 10,

        /// <summary>
        /// Female breeders to remove
        /// </summary>
        RemoveFemaleBreeders = 12,

        /// <summary>
        /// Female pre-breeders to remove
        /// </summary>
        RemoveFemalePreBreeders = 14,

        /// <summary>
        /// Select males to remove
        /// </summary>
        RemoveMales = 20,

        /// <summary>
        /// Male breeders to remove
        /// </summary>
        RemoveMaleBreeders = 22,

        /// <summary>
        /// Male pre-breeders to remove
        /// </summary>
        RemoveMalePreBreeders = 24,

        /// <summary>
        /// Select
        /// </summary>
        Select = 55,

        /// <summary>
        /// Select females
        /// </summary>
        SelectFemales = 60,

        /// <summary>
        /// Select female breeders
        /// </summary>
        SelectFemaleBreeders = 62,

        /// <summary>
        /// Select female pre-breeders
        /// </summary>
        SelectFemalePreBreeders = 64,

        /// <summary>
        /// Select females
        /// </summary>
        SelectMales = 70,

        /// <summary>
        /// Select female breeders
        /// </summary>
        SelectMaleBreeders = 72,

        /// <summary>
        /// Select female pre-breeders
        /// </summary>
        SelectMalePreBreeders = 74,
    }

    /// <summary>
    /// Style of inheriting ruminant attributes from parents
    /// </summary>
    public enum AttributeInheritanceStyle
    {
        /// <summary>
        /// Not inheritated
        /// </summary>
        None = 0,
        /// <summary>
        /// From mother's value if present
        /// </summary>
        Maternal = 5,
        /// <summary>
        /// From father's value if present
        /// </summary>
        Paternal = 10,
        /// <summary>
        /// At least one parent has attribute or least of both parents
        /// </summary>
        LeastParentValue = 15,
        /// <summary>
        /// At least one parent has attribute or greatest of both parents
        /// </summary>
        GreatestParentValue = 20,
        /// <summary>
        /// Both parents must have attribute and the least value is used
        /// </summary>
        LeastBothParents = 25,
        /// <summary>
        /// Both parents must have attribute and the greatest value is used
        /// </summary>
        GreatestBothParents = 30,
        /// <summary>
        /// Mean of the attribute value of parents using zero for those without attribute
        /// </summary>
        MeanValueZeroAbsent = 35,
        /// <summary>
        /// Mean of the attribute value of parents ignoring those without attribute
        /// </summary>
        MeanValueIgnoreAbsent = 40,
        /// <summary>
        /// Rules for single genetic trait (punnett square)
        /// </summary>
        AsGeneticTrait = 45
    }

    /// <summary>
    /// Type of ledger transaction (gain or loss)
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Loss of resource
        /// </summary>
        Loss = 0,
        /// <summary>
        /// Gain in resource
        /// </summary>
        Gain = 1
    }

    /// <summary>
    /// Style transaction reporting in resource ledger (style and amount) or (gain and loss)
    /// </summary>
    public enum ReportTransactionStyle
    {
        /// <summary>
        /// Reports transaction type and amount
        /// </summary>
        TypeAndAmountColumns = 1,
        /// <summary>
        /// Reports both gain and loss columns for transaction
        /// </summary>
        GainAndLossColumns = 0
    }

    /// <summary>
    /// The style of assessing an Attribute for filtering
    /// </summary>
    public enum AttributeFilterStyle
    {
        /// <summary>
        /// Use the value associated with the attribute
        /// </summary>
        ByValue,
        /// <summary>
        /// Use boolean of whether the attribute exists on the individual
        /// </summary>
        Exists
    }

    /// <summary>
    /// The style of accessing date
    /// </summary>
    public enum DateStyle
    {
        /// <summary>
        /// Accept single datestamp (CulturalInvariant)
        /// </summary>
        DateStamp,
        /// <summary>
        /// Use Year and Month entries
        /// </summary>
        YearAndMonth
    }

    /// <summary>
    /// Style to report transactions involving individuals in herd
    /// </summary>
    public enum RuminantTransactionsGroupingStyle
    {
        /// <summary>
        /// Combine all individuals
        /// </summary>
        Combined,
        /// <summary>
        /// Grouped by pricing groups
        /// </summary>
        ByPriceGroup,
        /// <summary>
        /// Grouped by class
        /// </summary>
        ByClass,
        /// <summary>
        /// Grouped by class and sex
        /// </summary>
        BySexAndClass,
    }

    /// <summary>
    /// General classes of ruminants
    /// </summary>
    public enum RuminantClass
    {
        /// <summary>
        /// Calf
        /// </summary>
        Calf,
        /// <summary>
        /// Weaner
        /// </summary>
        Weaner,
        /// <summary>
        /// PreBreeder
        /// </summary>
        PreBreeder,
        /// <summary>
        /// Breeder
        /// </summary>
        Breeder,
        /// <summary>
        /// Castrate
        /// </summary>
        Castrate,
        /// <summary>
        /// Sire
        /// </summary>
        Sire
    }

    /// <summary>
    /// Style of Transmute
    /// </summary>
    public enum TransmuteStyle
    {
        /// <summary>
        /// Direct transmute resource (B) to shortfall resource (A) e.g. barter
        /// </summary>
        Direct,
        /// <summary>
        /// Use pricing details of transmute resource (B) and shortfall resource (A) to calculate exchange rate
        /// </summary>
        UsePricing
    }

    /// <summary>
    /// Style of taking individuals from a filter group
    /// </summary>
    public enum TakeFromFilterStyle
    {
        /// <summary>
        /// Take a proportion of the group selected
        /// </summary>
        Proportion,
        /// <summary>
        /// Take a set number of individuals
        /// </summary>
        Individuals
    }
}