using System;
using System.Collections.Generic;

namespace Models.CLEM
{
    /// <summary>
    /// Status of activity
    /// </summary>
    public enum ActivityStatus
    {
        /// <summary>
        /// Performed with all resources available
        /// </summary>
        Success,
        /// <summary>
        /// Performed with partial resources available
        /// </summary>
        Partial,
        /// <summary>
        /// Insufficient resources so activity ignored
        /// </summary>
        Ignored,
        /// <summary>
        /// Insufficient resources so simulation stopped
        /// </summary>
        Critical,
        /// <summary>
        /// Indicates a timer occurred successfully
        /// </summary>
        Timer,
        /// <summary>
        /// Indicates a calculation event occurred
        /// </summary>
        Calculation,
        /// <summary>
        /// Indicates activity occurred but was not needed
        /// </summary>
        NotNeeded,
        /// <summary>
        /// Indicates activity caused a warning and was not performed
        /// </summary>
        Warning,
        /// <summary>
        /// Indicates activity was place holder or parent activity
        /// </summary>
        NoTask,
        /// <summary>
        /// Insufficient resources so activity skipped
        /// </summary>
        Skipped,
    }

    /// <summary>
    /// Status of activity
    /// </summary>
    public enum ResourceAllocationStyle
    {
        /// <summary>
        /// Automatically perform in CLEMGetResourcesRequired
        /// </summary>
        Automatic,
        /// <summary>
        /// Manually perform in activity code.
        /// </summary>
        Manual,
        /// <summary>
        /// Controlled by parent activity.
        /// </summary>
        ByParent,
    }

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
        ReduceInitialHerd,
        /// <summary>
        /// Sale of weaner
        /// </summary>
        WeanerSale
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
    /// Labour limit type calculation type
    /// </summary>
    public enum LabourLimitType
    {
        /// <summary>
        /// Represents a rate or fixed days per units specified
        /// </summary>
        AsRatePerUnitsAllowed,
        /// <summary>
        /// Relates to the total days allowed
        /// </summary>
        AsTotalDaysAllowed,
        /// <summary>
        /// As proportion of the days required
        /// </summary>
        ProportionOfDaysRequired
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
    public enum OtherAnimalsFeedActivityTypes
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
    /// Possible actions when only partial resources requested are available
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
        /// Use available resources to perform activity
        /// </summary>
        UseAvailableResources,
        /// <summary>
        /// Use available resources with shortfall influencing other activities
        /// </summary>
        UseAvailableWithImplications,
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
    public enum RuminantGroupStyleSecondary
    {
        /// <summary>
        /// No style specified
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Select females to remove
        /// </summary>
        Females = 100,

        /// <summary>
        /// Female pre-breeders to remove
        /// </summary>
        FemalePreBreeders = 110,

        /// <summary>
        /// Female breeders to remove
        /// </summary>
        FemaleBreeders = 120,

        /// <summary>
        /// Select males to remove
        /// </summary>
        Males = 200,

        /// <summary>
        /// Male pre-breeders to remove
        /// </summary>
        MalePreBreeders = 210,

        /// <summary>
        /// Male breeders to remove
        /// </summary>
        MaleBreeders = 220,
    }

    /// <summary>
    /// Tertiary style to identify different ruminant groups needed by activities
    /// </summary>
    public enum RuminantGroupStyleTertiary
    {
        /// <summary>
        /// No style specified
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// From the specified herd available
        /// </summary>
        FromHerd = 10,

        /// <summary>
        /// From only individuals flagged for sale
        /// </summary>
        FromSales = 20,

        /// <summary>
        /// From the current list of potential purchases
        /// </summary>
        FromPurchases = 30,
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
        /// Suckling
        /// </summary>
        Suckling,
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
        TakeProportion,
        /// <summary>
        /// Take a set number of individuals
        /// </summary>
        TakeIndividuals,
        /// <summary>
        /// Skip a proportion of the group selected and return the remainder
        /// </summary>
        SkipProportion,
        /// <summary>
        /// Skip a set number of individuals and return the remainder
        /// </summary>
        SkipIndividuals
    }

    /// <summary>
    /// Position for reducing individuals from a filter group
    /// </summary>
    public enum TakeFromFilteredPositionStyle
    {
        /// <summary>
        /// Take/Skip from start
        /// </summary>
        Start,
        /// <summary>
        /// Take/Skip from end
        /// </summary>
        End
    }

    /// <summary>
    /// The overall style of ruminants required
    /// </summary>
    public enum GetRuminantHerdSelectionStyle
    {
        /// <summary>
        /// All individuals currently in herd both including marked for sale
        /// </summary>
        AllOnFarm,
        /// <summary>
        /// Individuals in purchase list yet to be bought
        /// </summary>
        ForPurchase,
        /// <summary>
        /// Individuals currently marked for sale in the herd
        /// </summary>
        MarkedForSale,
        /// <summary>
        /// Individuals not marked for sale in the herd
        /// </summary>
        NotMarkedForSale,
    }

    /// <summary>
    /// The types of labels provided for use by companion models
    /// </summary>
    public enum CompanionModelLabelType
    {
        /// <summary>
        /// The child identifiers available
        /// </summary>
        Identifiers,
        /// <summary>
        /// The resource measures available
        /// </summary>
        Measure
    }

    /// <summary>
    /// Reporting style for Memos in Descriptive summary
    /// </summary>
    public enum DescriptiveSummaryMemoReportingType
    {
        /// <summary>
        /// Present where they occur in the tree structure
        /// </summary>
        InPlace,
        /// <summary>
        /// Present at the top of the property list
        /// </summary>
        AtTop,
        /// <summary>
        /// Present at the bottom of the property list
        /// </summary>
        AtBottom,
        /// <summary>
        /// Do not present Memos
        /// </summary>
        Ignore
    }


    /// <summary>
    /// Style of reporting age
    /// </summary>
    public enum ReportAgeType
    {
        /// <summary>
        /// Do not report
        /// </summary>
        None,
        /// <summary>
        /// Age in months
        /// </summary>
        Months,
        /// <summary>
        /// Age in years with decimale fractions
        /// </summary>
        FractionOfYears,
        /// <summary>
        /// Age in truncated whole years
        /// </summary>
        WholeYears
    }

    /// <summary>
    /// The style of calculation to use for the Activity Timer based on ruminant herd level
    /// </summary>
    public enum ActivityTimerRuminantLevelStyle
    {
        /// <summary>
        /// Number of individuals selected
        /// </summary>
        NumberOfIndividuals,
        /// <summary>
        /// Sum of property across all individuals selected
        /// </summary>
        SumOfProperty,
        /// <summary>
        /// Mean of property across all individuals selected
        /// </summary>
        MeanOfProperty,
        /// <summary>
        /// Minimum of property across all individuals selected
        /// </summary>
        MinimumOfProperty,
        /// <summary>
        /// Maximum of property across all individuals selected
        /// </summary>
        MaximumOfProperty
    }

    /// <summary>
    /// Approaches available to calculate additional mortality based on animal condition
    /// </summary>
    public enum ConditionBasedCalculationStyle
    {
        /// <summary>
        /// Use weight as proportion of max weight cutoff (Depreciated)
        /// </summary>
        ProportionOfMaxWeightToSurvive,
        /// <summary>
        /// Use relative condition cutoff
        /// </summary>
        RelativeCondition,
        /// <summary>
        /// Use Body Condition Score cutoff
        /// </summary>
        BodyConditionScore,
        /// <summary>
        /// Ignore condtion-based calculation.
        /// </summary>
        None
    }

    /// <summary>
    /// Activity timing within time-step
    /// </summary>
    public enum WithinTimeStepTimingStyle
    {
        /// <summary>
        /// Early in ythe time-step before related activities
        /// </summary>
        Early,
        /// <summary>
        /// Perform at GetResourcesRequired with other competing activities
        /// </summary>
        Normal,
        /// <summary>
        /// Late in the time-step after related activites
        /// </summary>
        Late
    }

    /// <summary>
    /// A list of labels used for communication between an activity and companion models
    /// </summary>
    [Serializable]
    public struct LabelsForCompanionModels
    {
        /// <summary>
        /// List of available identifiers
        /// </summary>
        public List<string> Identifiers;
        /// <summary>
        /// List of available measures
        /// </summary>
        public List<string> Measures;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifiers"></param>
        /// <param name="measures"></param>
        public LabelsForCompanionModels(List<string> identifiers, List<string> measures)
        {
            Identifiers = identifiers;
            Measures = measures;
        }
    }

    /// <summary>
    /// Additional linq extensions
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Method to extend linq and allow DistinctBy for unions
        /// Provided by MoreLinQ
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
         (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

    }

}