using APSIM.Numerics;
using Models.CLEM.Interfaces;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Topten.RichTextKit.Utils;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Manages tracking of Ruminant intake quality and quantity.
    /// </summary>
    [Serializable]
    public class RuminantIntake
    {
        private readonly Dictionary<string, FoodResourceStore> feedTypeStoreDict = [];
        private double dpls = 0;

        /// <summary>
        /// The potential and actual milk intake of the individual.
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer MilkDaily { get; set; } = new ExpectedActualContainer();

        /// <summary>
        /// The potential and actual solids intake of the individual (kg).
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer SolidsDaily { get; set; } = new ExpectedActualContainer();

        /// <summary>
        /// A function to add intake and track rumen totals of N, CP, DMD, Fat and energy on daily basis.
        /// </summary>
        /// <param name="store">Feed packet containing intake information kg, %N, DMD.</param>
        /// <param name="groupID">ID to mix multiple feed entries</param>
        /// <param name="bypassPotIntakeLimits">A switch to force animals to eat the amount provided.</param>
        /// <param name="specifyAmount">Specify the amount to add rather than obtain from the FoodResourcePacket.Amount</param>
        /// <returns>The excess feed to the individual</returns>
        public double AddFeed(FoodResourceStore store, string groupID = "", bool bypassPotIntakeLimits = false, double specifyAmount = double.NaN)
        {
            double excess = 0;

            if (double.IsNaN(specifyAmount))
            {
                specifyAmount = store.Details.Amount;
            }

            if (specifyAmount <= 0)
            {
                return excess;
            }

            if (groupID == "")
            {
                groupID = store.Details.TypeOfFeed.ToString();
            }

            if (!bypassPotIntakeLimits && store.Details.TypeOfFeed != FeedType.Milk)
            {
                // limit feed to animals maximum intake.
                excess = StdMath.DIM(specifyAmount, SolidsDaily.Required);
                if (excess > 0 && excess < 1e-5)
                {
                    excess = 0;
                }
                specifyAmount -= excess;
            }

            if (!feedTypeStoreDict.TryGetValue(groupID, out FoodResourceStore frs))
            {
                frs = new FoodResourceStore(store, specifyAmount);
                feedTypeStoreDict[groupID] = frs;
            }
            else
            {
                frs.Add(store.Details, specifyAmount);
            }

            if (store.Details.TypeOfFeed == FeedType.Milk)
                MilkDaily.Received += specifyAmount;
            else
                SolidsDaily.Received += specifyAmount;

            return excess;
        }

        /// <summary>
        /// Adjust intake consumed based on feed quality
        /// </summary>
        public void AdjustIntakeBasedOnFeedQuality(bool isLactating, Ruminant ind)
        {
            if (ind.Parameters.GrowPF_CI.IgnoreFeedQualityIntakeAdjustment)
                return;

            // ========================================================================================================================
            // Freer et al. (2012) The GRAZPLAN animal biology model for sheep and cattle and the GrazFeed decision support tool
            // Equations 14-21
            // ========================================================================================================================

            // CLEM does not currently include legumes in pastures so there is no need to differentiate feed types when applying quality affect.
            // handling legume was the only reason for type based assessment for the PastureTemperate and PastureTropical
            double sumFs = 0;
            double iReduction = 0;

            // this should include any previous reductions based on RDP gut biome shortfalls
            double solidIntake = SolidsDaily.Actual;

            // for each food type
            // sorted by DMD descending and concentrate first (if same DMD) to ensure quality reduction is applied to lower quality feeds first, and concentrates are last to be reduced (as they are often the highest quality feed).
            foreach (var item in feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk).OrderByDescending(a => a.Value.Details.DryMatterDigestibility).OrderByDescending(a => a.Value.Details.TypeOfFeed == FeedType.Concentrate))
            {
                // RQ is relative quality
                // FS relative availability of the feed
                double FS = 0;
                double RS = 0;
                double RQ = Math.Min(1.0, 1 - ind.Parameters.GrowPF_CI.DigestibilitySlope_CR3 * (ind.Parameters.GrowPF_CI.DigestibilityPeak_CR1 - (item.Value.Details.DryMatterDigestibility/100.0)));

                double offered_adj = (item.Value.Details.Amount/solidIntake)/RQ;
                double unsatisfied_adj = Math.Max(0, 1-sumFs);
                double quality_adj = (isLactating? ind.Parameters.GrowPF_CI.QualityIntakeSubsititutionFactorLactating_CR20:ind.Parameters.GrowPF_CI.QualityIntakeSubsititutionFactorNonLactating_CR11)/item.Value.Details.MEContent;

                // Added to remove heavy reduction on concentrates when the only item in the intake pool. but leave the RQ quality reduction.
                if (feedTypeStoreDict.Count == 1)
                {
                    quality_adj = 1.0;
                }

                FS =  Math.Min(offered_adj, Math.Min(unsatisfied_adj, quality_adj));
                RS = FS * RQ;

                iReduction = StdMath.DIM(item.Value.Details.Amount, RS * solidIntake);
                if (iReduction > 0)
                {
                    item.Value.ReturnPending(iReduction);
                    SolidsDaily.Unneeded += iReduction;
                }
                sumFs += FS;
            }
        }

        /// <summary>
        /// Provides the amount of solid intake in diet.
        /// </summary>
        [FilterByProperty]
        public double SolidIntake 
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk).Sum(a => a.Value.Details.Amount);
            }
        }

        /// <summary>
        /// Gut fill. The proportion of live weight that is accounted for by gut contents
        /// </summary>
        public double GutFill { get; private set; } = 0.04;

        /// <summary>
        /// Calculate the average weighted gut fill from current diet.
        /// </summary>
        public void UpdateGutFill()
        {
            if (SolidsDaily.Expected == 0 || feedTypeStoreDict.Count <= 0)
                return;

            double proportionAttained = (SolidsDaily.Actual + SolidsDaily.Unneeded) / SolidsDaily.Expected;
            double shortfallModifier = proportionAttained + (1 - proportionAttained) * 0.5;
            GutFill = feedTypeStoreDict.Sum(a => a.Value.Details.GutFill * a.Value.Details.Amount) / feedTypeStoreDict.Sum(a => a.Value.Details.Amount) * shortfallModifier;
        }

        /// <summary>
        /// Provides the proportion of milk in the diet.
        /// </summary>
        public double ProportionMilk
        {
            get
            {
                if (MilkDaily.Actual + SolidsDaily.Actual <= 0)
                    return 0;
                return MilkDaily.Actual / (MilkDaily.Actual + SolidsDaily.Actual);
            }
        }

        /// <summary>
        /// Determines the proportion of the potential intake actually achieved.
        /// </summary>
        public double ProportionOfPotentialIntakeObtained
        {
            get
            {
                if (SolidsDaily.Expected + MilkDaily.Expected <= 0)
                    return 0;
                return (SolidsDaily.Actual + MilkDaily.Actual) / (SolidsDaily.Expected + MilkDaily.Expected);
            }
        }

        /// <summary>
        /// Get the details of a food resource store identified by feed type .
        /// </summary>
        /// <param name="feedType">Feed type of the required store.</param>
        /// <returns>The food resource store or null if not found.</returns>
        public FoodResourcePacket GetStoreDetails(FeedType feedType)
        {
            return GetStore(feedType)?.Details;
        }

        /// <summary>
        /// Get the food resource store identified by feed type.
        /// </summary>
        /// <param name="feedType">Feed type of the required store.</param>
        /// <returns>The food resource packet containing feed store details.</returns>
        public FoodResourceStore GetStore(FeedType feedType)
        {
            if (feedTypeStoreDict.TryGetValue(feedType.ToString(), out FoodResourceStore frs))
                return frs;
            return null;
        }

        /// <summary>
        /// Get all food stores available
        /// </summary>
        public Dictionary<string, FoodResourceStore> GetAllStores { get { return feedTypeStoreDict; } }

        /// <summary>
        /// Reset all intake values.
        /// </summary>
        public void Reset()
        {
            // ToDo: consider clearing dictionary. Is there any benefit keeping old stores for loops?
            // clearing each time-step (for each individual e.g. 30,0000 head)
            // * would remove milk store after weaning (could add special clear)
            // * clearing would allow smallest number of feed types each step and variable feeding rules to be efficient
            // * reduce looping for kg and FMEI (across number of types fed in time-step)
            // * multiple garbage collection required for all ruminants every time-step (expensive, memory and cpu)
            // * ? how much are time step feed types really likely to vary?
            // * just how many feed types are expected in most runs? I expect only few.
            // leaving them only means they don't have be be recreated each time fed (time step), but 
            // * check if exists (happens regardless, may be faster with fewer entries), dictionary should be optimised
            // * saves .. if not present create object and add to dictionary
            // * saves loops over types and resetting
            // Outcome
            // * I think leaving them might be most optimised
            // * ensure they are cleared and removed on disposal of individual.

            feedTypeStoreDict.Clear();

            //foreach (var item in feedTypeStoreDict)
            //{
            //    item.Value.Reset();
            //}
            dpls = double.NaN;
            kDPLS = double.NaN;
        }

        /// <summary>
        /// Metabolisable energy from intake.
        /// </summary>
        public double ME
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.ME);
            }
        }

        /// <summary>
        /// Method to calculate running ME Average for today and last time step
        /// </summary>
        public void UpdateMEAverage()
        {
            if (MEAverage == 0)
                MEAverage = ME;
            else
                MEAverage = (MEAverage + ME)/ 2.0;
        }

        /// <summary>
        /// Track average ME over today and yesterday.
        /// </summary>
        public double MEAverage { get; set; } = 0.0;

        /// <summary>
        /// Metabolisable energy from Milk intake.
        /// </summary>
        public double MilkME
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed == FeedType.Milk).Sum(a => a.Value.ME);
            }
        }

        /// <summary>
        /// Metabolisable energy from solids (non-milk) intake.
        /// </summary>
        public double SolidsME
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk).Sum(a => a.Value.ME);
            }
        }


        /// <summary>
        /// Metabolisable energy density from solids intake.
        /// </summary>
        public double MDSolid
        {
            get
            {
                if (MathUtilities.IsGreaterThan(SolidIntake, 0))
                {
                    return feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk).Sum(a => a.Value.ME) / SolidIntake;
                }
                return 0;
            }
        }

        /// <summary>
        /// Dry matter digestibility of solid (non-milk) intake.
        /// </summary>
        public double DMD
        {
            get
            {
                double sumDMD = 0;
                double sumAmount = 0;
                foreach (var item in feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk))
                {
                    sumDMD += item.Value.Details.DryMatterDigestibility * item.Value.Details.Amount;
                    sumAmount += item.Value.Details.Amount;
                }
                if (sumAmount <= 0)
                    return 0;

                return sumDMD/sumAmount;
            }
        }

        /// <summary>
        /// Calculate Nitrogen content of solid (non-milk) intake as percentage.
        /// </summary>
        public double NitrogenPercent
        {
            get
            {
                var total = feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk).Sum(a => a.Value.Details.Amount);
                var totalN = feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk).Sum(a => a.Value.Details.NitrogenPercent * a.Value.Details.Amount);
                if(total > 0)
                    return totalN / total;
                return 0;
            }
        }

        /// <summary>
        /// Total degradable protein (kg/timestep).
        /// </summary>
        public double DegradableProtein
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.DegradableCrudeProtein);
            }
        }

        /// <summary>
        /// Total crude protein (kg/timestep).
        /// </summary>
        public double CrudeProtein
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.CrudeProtein);
            }
        }

        /// <summary>
        /// Rumen Degradable Protein.
        /// </summary>
        public double RDP
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.DegradableCrudeProtein);
            }
        }

        /// <summary>
        /// Method to calculate the digestible protein leaving the stomach (Metabolisable Protein) based on RDP required.
        /// </summary>
        /// <param name="rdpRequired">The Rumen Digestible Protein (RDP) requirement</param>
        /// <param name="milkProteinDigestibility">The milk protein digestibility of the breed</param>
        /// <returns></returns>
        public void CalculateDigestibleProteinLeavingStomach(double rdpRequired, double milkProteinDigestibility)
        {
            // DPLS is simply 0.6 of the minimum of DPP from intake and RDP required from rumen.
            dpls = 0;
            RDPRequired = Math.Min(RDP, rdpRequired); 
            dpls = (0.6 * RDPRequired); // microbe component.
            foreach (var item in feedTypeStoreDict)
            {
                if(item.Value.Details.TypeOfFeed == FeedType.Milk)
                {
                    dpls += milkProteinDigestibility * item.Value.CrudeProtein; // CA5 = 0.92
                }
                else
                {
                    dpls += item.Value.DUDP * item.Value.UndegradableCrudeProtein;
                }
            }
        }

        /// <summary>
        /// Rumen Digestible Protein Required
        /// </summary>
        public double RDPRequired { get; set; }

        /// <summary>
        /// Digestible Protein Leaving the Stomach.
        /// </summary>
        public double DPLS
        {
            get
            {
                return dpls;
            }
        }

        /// <summary>
        /// Efficiency of use of digestible protein leaving the stomach
        /// </summary>
        public double kDPLS { get; set; }

        /// <summary>
        /// Digestible Protein Leaving the Stomach times the kDPLS efficiency term.
        /// </summary>
        public double UsableDPLS
        {
            get
            {
                return dpls * kDPLS;
            }
        }

        /// <summary>
        /// Rumen Undegradable Protein (RUP = UDP).
        /// </summary>
        public double UDP {
            get
            {
                return CrudeProtein - RDP;
            }
        }

        /// <summary>
        /// FME Intake
        /// </summary>
        public double FMEI
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.FME);
            }
        }

        /// <summary>
        /// Causes a proportional reduction in intake (and RPD)
        /// </summary>
        /// <param name="proportion">Proportional reduction</param>
        public void ReduceIntakeByProportion(double proportion)
        {
            double amountReduced = 0;
            foreach (var item in feedTypeStoreDict.Where(a => a.Value.Details.TypeOfFeed != FeedType.Milk))
            {
                amountReduced += item.Value.ReduceByProportion(proportion);
            }
            SolidsDaily.Unneeded += amountReduced;
        }

        /// <summary>
        /// Indigestible undegradable protein.
        /// </summary>
        public double IndigestibleUDP
        {
            get
            {
                double sumAmount = 0;
                foreach (var item in feedTypeStoreDict)
                {
                    sumAmount += (1-item.Value.DUDP) * item.Value.UndegradableCrudeProtein;
                }
                return sumAmount;
            }
        }

        /// <summary>
        /// Identifies the individual as unfed.
        /// </summary>
        public bool IsUnfed
        {
            get
            {
                return MathUtilities.IsPositive(SolidsDaily.Expected + MilkDaily.Expected) & (MathUtilities.IsPositive(SolidsDaily.Actual + MilkDaily.Actual) == false);
            }
        }
    }
}
