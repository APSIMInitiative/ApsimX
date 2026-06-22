using APSIM.Shared.Extensions.Collections;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.ForageDigestibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a GrazeFoodType that links directly to an APSIM paddock containing forages for
    /// grazing
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [Description("This resource represents a link to an APSIM paddock with forages")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreAPSIMLink.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrazing) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class GrazeFoodStoreAPSIMLink : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFeed, IGrazeFoodStoreType, IValidatableObject
    {
        private const double gm2TokgHa = 10.0;
        private double biomassAddedThisYear;
        private double biomassConsumed;
        private List<ModelWithDigestibleBiomass> forageModels = [];
        private Forages forages;
        private Zone paddock;
        private List<FoodResourceStore> intakeStoreRequests = [];
        private double consumableWt = 0;
        private double totalWt = 0;

        /// <inheritdoc/>
        [Description("Type of pasture or forage")]
        [Category("Farm", "Paddock")]
        [Required]
        public FeedType TypeOfFeed { get; set; } = FeedType.PastureTemperate;

        /// <inheritdoc/>
        [Description("Name of APSIM paddock")]
        [Category("Farm", "Paddock")]
        [Required]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetModelsAvailableByType", ValuesArgs = new object[] { new Type[] { typeof(Zone) } })]
        public string PaddockName { get; set; }

        /// <inheritdoc/>
        [Description("Gross energy content (MJ/kg DM)")]
        [Category("Farm", "Quality")]
        [Units("MJ/kg digestible DM")]
        [Required, GreaterThanValue(0)]
        public double GrossEnergyContent { get; set; } = 18.4;

        private double rumenDegradableProteinPercent = 58;

        /// <summary>
        /// Highest expected sward Dry Matter Digestibility (%)
        /// </summary>
        [Category("Farm", "Gut fill")]
        [Description("Highest Dry Matter Digestibility expected")]
        [Units("%")]
        [Required, Percentage, GreaterThanValue(0)]
        public double HighestDMD { get; set; } = 58;

        /// <summary>
        /// Lowest expected sward Dry Matter Digestibility (%)
        /// </summary>
        [Category("Farm", "Gut fill")]
        [Description("Minimum Dry Matter Digestibility expected")]
        [Required, Percentage]
        [Units("%")]
        public double LowestDMD { get; set; } = 42;

        /// <summary>
        /// Value of gut fill for highest quality green pasture
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill high quality (Highest DMD)")]
        public double GutFillHighQuality { get; set; } = 0.08;

        /// <summary>
        /// Value of gut fill for lowest quality cured pasture at min DMD
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill low quality (Lowest DMD)")]
        public double GutFillLowQuality { get; set; } = 0.2;

        /// <inheritdoc/>
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
        [Units("MJ/kg DM")]
        public double MetabolisableEnergyContent { get; set; } = 0.0;

        /// <inheritdoc/>
        [JsonIgnore]
        public double NitrogenPercent { get; set; }

        /// <summary>
        /// Style of providing the dry matter digestibility of pasture
        /// </summary>
        [JsonIgnore]
        public DryMatterDigestibilityStyle DMDStyle { get; set; } = DryMatterDigestibilityStyle.EstimateFromNitrogenContent;

        /// <inheritdoc/>
        [JsonIgnore]
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public double AcidDetergentInsolubleProtein { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public double CrudeProteinPercent { get; set; }

        /// <inheritdoc/>
        public double FatPercent { get; set; } = 1.9;

        /// <inheritdoc/>
        public double RumenDegradableProteinPercent
        {
            get
            {
                return rumenDegradableProteinPercent;
            }
            set
            {
                rumenDegradableProteinPercent = value;
                AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(rumenDegradableProteinPercent, TypeOfFeed);
            }
        }

        /// <summary>
        /// The number of intake stores
        /// </summary>
        public int NumberOfItakeStores => intakeStoreRequests.Count;

        /// <summary>
        /// Get the name of a specified food store
        /// </summary>
        /// <param name="index">0 based index of store in list</param>
        /// <returns></returns>
        public string GetStoreName(int index)
        {
            if (index < 0 || !intakeStoreRequests.Any() || index > intakeStoreRequests.Count - 1)
                return "Invalid";
            return intakeStoreRequests.ElementAt(index).Name;
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public double GutFill
        {
            get
            {
                return CalculateGutFill(DryMatterDigestibility);
            }
            set
            {
            }
        }

        /// <inheritdoc/>
        public double CalculateGutFill(double dmd)
        {
            if (dmd <= LowestDMD)
            {
                return GutFillLowQuality;
            }
            if (dmd >= HighestDMD)
            {
                return GutFillHighQuality;
            }
            return GutFillLowQuality + ((dmd - LowestDMD) / (HighestDMD - LowestDMD)) * (GutFillHighQuality - GutFillLowQuality);
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public double OverallPastureBiomass { get; set; }

        ///// <summary>
        ///// Coefficient to adjust intake for tropical herbage quality
        ///// </summary>
        //[Category("Advanced", "Intake")]
        //[Description("Coefficient to adjust intake for tropical herbage quality")]
        //[Required]
        //public double IntakeTropicalQualityCoefficient { get; set; } = 0.16;

        ///// <summary>
        ///// Coefficient to adjust intake for herbage quality
        ///// </summary>
        //[Category("Advanced", "Intake")]
        //[Description("Coefficient to adjust intake for herbage quality")]
        //[Required]
        //public double IntakeQualityCoefficient { get; set; } = 1.7;

        /// <summary>
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa
        {
            get
            {
                if (paddock is null)
                {
                    return 0;
                }
                return AmountAvailable / paddock.Area;
            }
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectareStartOfTimeStep { get; set; }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectare
        {
            get
            {
                if (paddock is null)
                {
                    return 0;
                }
                return KilogramsPerHa / 1000.0;
            }
        }

        /// <summary>
        /// Set the current pasture biomass for analysis
        /// </summary>
        public void SetCurrentBiomass()
        {
            OverallPastureBiomass = KilogramsPerHa;
        }

        /// <summary>
        /// Get the new growth from the pasture model
        /// </summary>
        public void GetNewGrowth()
        {
            // todo: this will need to come from the pasture models... need to work out how
            biomassAddedThisYear = 0; 
        }

        /// <summary>
        /// Get the biomass consumed from the pasture model
        /// </summary>
        public double GetConsumed() => biomassConsumed;

        /// <summary>
        /// Percent utilisation
        /// </summary>
        public double PercentUtilisation
        {
            get
            {
                if (biomassAddedThisYear == 0)
                {
                    return (biomassConsumed > 0) ? 100 : 0;
                }

                return biomassConsumed == 0 ? 0 : Math.Min(biomassConsumed / biomassAddedThisYear * 100, 100);
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            forages = Node.Find<Forages>();
            paddock = Node.Find<Zone>(name: PaddockName);
            if (forages is null || paddock is null)
            {
                throw new ApsimXException(this, $"Cannot find [Forages] or the paddock named [{PaddockName}] in the simulation tree.");
            }

            // do not include surface organic matter in feed pools
            forageModels = [.. forages.ModelsWithDigestibleBiomass.Where(m => m.Zone == paddock && !m.Material.FirstOrDefault().Name.Contains("SurfaceOrganic") )];
        }

        /// <summary>
        /// Store amount of consumable pasture available for everyone at the start of the step (kg per hectare)
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPastureReady")]
        private void OnCLEMPastureReady(object sender, EventArgs e)
        {
            totalWt = 0;
            consumableWt = 0;
            biomassConsumed = 0;
            foreach (var biomassModel in forageModels)
            {
                totalWt += biomassModel.Material.Where(m => m.IsLive)
                                        .Sum(m => m.Total.Wt) * gm2TokgHa;
                totalWt += biomassModel.Material.Where(m => !m.IsLive)
                                        .Sum(m => m.Total.Wt) * gm2TokgHa; 
                consumableWt += biomassModel.Material.Where(m => !m.IsLive)
                                        .Sum(m => m.Consumable.Wt) * gm2TokgHa;
                consumableWt += biomassModel.Material.Where(m => m.IsLive)
                                        .Sum(m => m.Consumable.Wt) * gm2TokgHa;
            }

            // ToDo: check units. Consumable (PMF.Biomass) says it is kg/ha not g/m^2 as in Stock code.
            // ToDo: the PMF.Biomass object being used states g/m^2 which means we need *10 to kg/ha then * area (ha) to get to paddock but this value is already 10x more than APSIM report saying paddock biomass in kg (maybe this is kg/ha not total paddock biomass)

            Set(totalWt * paddock.Area);
            SetUnavailable(AmountTotal - (consumableWt * paddock.Area)); // non-consumable set aside and available for reporting

            // do not return zero as there is always something there and zero affects calculations.
            TonnesPerHectareStartOfTimeStep = Math.Max(TonnesPerHectare, 0.01);

            // Update the link's quality properties from forage model values so reports can read current paddock quality.
            // UpdatePaddockQuality();
            // report pasture growth
        }
        
        /// <summary>
        /// Event to remove the current daily intake from forage when CLEM is determining daily consumption for time
        /// step
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDailyASPIMForageTake")]
        private void OnCLEMTakeAPSIMForage(object sender, EventArgs e)
        {
            foreach (FoodResourceStore foodStore in intakeStoreRequests)
            {
                // for each pool in the food store, remove the proportional amount from the corresponding pool in the forage model per m2 per day
                foreach (var pool in foodStore.Pools.Cast<GrazeAPSIMForagePool>())
                {
                    double poolAmountToRemove = pool.AmountPending / paddock.Area / foodStore.NumberOfDaysInTimestep; // this is expected in g/m^2 and we are working in kg/ha/day
                    var check = pool.BiomassModel.RemoveBiomass(poolAmountToRemove, 1.0, 1.0, Summary);
                }
            }
        }

        /// <inheritdoc/>
        public List<FoodResourceStore> GenerateIntakeGroups(int numberOfTimesteps, int greenAge = -1, int dmdStep = 10)
        {
            //string devStyle = "ByDMD";
            //string devStyle = "BySpecies";
            string devStyle = "ByForageVSurfaceOM";

            if (forageModels == null || !forageModels.Any())
                return [];

            var pools = forageModels
                .Where(model =>
                {
                    return (model.Material?.Sum(m => m.Consumable.Wt) ?? 0.0) > 0.0;

                    //var material = model.Material;
                    //if (material == null)
                    //    return false;

                    //return material.Sum(m => m.Consumable.Wt) > 0;
                    //double live = material.Where(m => m.IsLive).Sum(m => m.Total.Wt);
                    //double dead = material.Where(m => !m.IsLive).Sum(m => m.Total.Wt);
                    //return (live + dead) > 0.0;
                    //return wt > 0.0;
                })
                .Select(provider => new GrazeAPSIMForagePool(this, provider, forages, paddock.Area));

            switch (devStyle)
            {
                case "ByDMD":
                    // pools ordered by DMD steps (e.g. 0-10, 10-20, etc.) and grouped into FoodResourceStore for feeding to animals            
                    return pools
                    .GroupBy(s => Convert.ToInt32(s.DryMatterDigestibility / dmdStep) * dmdStep) // now group into DMD steps and place in ResourceFoodStore to feed to animals
                    .Select(groups => new FoodResourceStore(
                        [.. groups],
                        greenAge,
                        numberOfTimesteps,
                        groups.Key.ToString()
                        )
                    ).OrderByDescending(a => a.Details.DryMatterDigestibility).ToList();
                case "BySpecies":
                    // pools ordered by species and grouped into FoodResourceStore for feeding to animals            
                    return pools
                    .GroupBy(s => s.Name.Split('.').First())
                    .Select(groups => new FoodResourceStore(
                        [.. groups],
                        greenAge,
                        numberOfTimesteps,
                        groups.Key
                        )
                    ).OrderByDescending(a => a.Details.DryMatterDigestibility).ToList();
                case "ByForageVSurfaceOM":
                    // all pools combined and excluding surface organic matter
                    return pools
                    .GroupBy(s => s.Name.Contains("Surface") == false)
                    .Select(groups => new FoodResourceStore(
                        [.. groups],
                        greenAge,
                        numberOfTimesteps, 
                        (groups.Key ? "SurfaceOrganicMatter" : "MixedSward")
                        )
                    ).OrderByDescending(a => a.Details.DryMatterDigestibility).ToList();
                default:
                    return [];
            }

            // think about different approaches
            // 1. whole avearge pasture pool (DMD step = 100)
            // 2. select by DMD - current DMD step (e.g. 10)
            // 3. proportional with weighting toward green
            // 4. CLEM green biomass limit - implemented
            // 5. CLEM low biomass intake limited - implemented

            // individual selective ability proceedures can be actioned in GeneratePoolGroups and thus the list and order of pools the animals feed from.
        }

        /// <summary>
        /// Method to provide conversion factor to tonnes and/or hectares
        /// </summary>
        public double Report(string grazeProperty, bool tonnes = false, bool hectares = false, int age = -1)
        {
            double convertToKg = 10.0 * paddock.Area;
            double convert = (tonnes ? 1000 : 1) * (hectares ? paddock.Area : 1);
            double valueToUse = 0;
            switch (grazeProperty)
            {
                case "Amount":
                    if (age < 0)
                    {
                        valueToUse = intakeStoreRequests.SelectMany(a => a.Pools).Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Total.Wt)) * convertToKg / convert;
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Pools.Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Total.Wt)) * convertToKg / convert;
                    }
                    break;
                case "AmountConsumable":
                    if (age < 0)
                    {
                        valueToUse = intakeStoreRequests.SelectMany(a => a.Pools).Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Consumable.Wt)) * convertToKg / convert;
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Pools.Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Consumable.Wt)) * convertToKg / convert;
                    }
                    break;
                case "Growth":
                    valueToUse = double.NaN;
                    break;
                case "Consumed":
                    if (age < 0)
                    {
                        valueToUse = GetConsumed() / convert;
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Pools.Cast<GrazeAPSIMForagePool>().Sum(p => p.Consumed) / convert;
                    }
                    break;
                case "Detached":
                    valueToUse = double.NaN;
                    break;
                case "Nitrogen":
                    if (age < 0)
                    {
                        return intakeStoreRequests.Sum(a => a.Details.NitrogenPercent * a.Details.Amount) / intakeStoreRequests.Sum(a => a.Details.Amount);
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Details.NitrogenPercent;
                    }
                    return valueToUse;
                case "DMD":
                    if (age < 0)
                    {
                        return intakeStoreRequests.Sum(a => a.Details.DryMatterDigestibility * a.Details.Amount)/intakeStoreRequests.Sum(a => a.Details.Amount);
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Details.DryMatterDigestibility;
                    }
                    return valueToUse;
                case "Age":
                    return double.NaN;
                default:
                    throw new ApsimXException(this, $"Property [{grazeProperty}] not available for reporting pools");
            }
            // convert biomass to units specified kg,tonnes & farm,per/hectare
            return valueToUse;
        }


        ///// <summary>
        ///// Method to set the daily forage take by grazing animals
        ///// </summary>
        ///// <param name="request">The resource request passed from the grazing activity</param>
        //public void SetDailyForageTakeFromGrazing(ResourceRequest request)
        //{
        //    if (request == null)
        //    {
        //        return;
        //    }

        //    double shortfall = (AmountAvailable > 0) ? 1.0 : 0.0;
        //    //CurrentGrazingRequest.Provided = request.Required;
        //    //if (Amount > 0 && request.Required < Amount)
        //    //{
        //    //    CurrentGrazingRequest.Provided = Amount;
        //    //    shortfall = request.Required / Amount;
        //    //}

        //    // build snapshot of grazing inputs (kg/ha units)
        //    Array.Resize(ref grazingInputs, paddockInfo.Forages.Count());
        //    for (int jdx = 0; jdx <= paddockInfo.Forages.Count() - 1; jdx++)
        //    {
        //        if (grazingInputs[jdx] == null)
        //            grazingInputs[jdx] = new GrazType.GrazingInputs();
        //        grazingInputs[jdx] = paddockInfo.Forages.ByIndex(jdx).AvailForage();
        //    }

        //    // quick exit if nothing required
        //    double required = (request.AdditionalDetails as RuminantActivityGrazePastureHerd).DailyPastureRequired * shortfall;
        //    if (required <= 1.0e-9)
        //    {
        //        // clear stored daily removals
        //        dailyRemovalByClass = [];
        //        dailyRemovalSeed = new double[0, 0];
        //        dailyPaddockPacket = null;
        //        return;
        //    }

        //    int classMax = GrazType.DigClassNo;
        //    int maxSpecies = GrazType.MaxPlantSpp;
        //    int ripeMax = GrazType.RIPE;
        //    double area = paddockInfo.Area; // hectares

        //    // Total availability (kg) aggregated across providers
        //    double[] totalAvailableByClass = new double[classMax + 1];
        //    double[,] totalSeedAvailable = new double[maxSpecies + 1, ripeMax + 1];

        //    for (int p = 0; p < grazingInputs.Length; p++)
        //    {
        //        var g = grazingInputs[p];
        //        if (g == null) continue;

        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double availPerHa = g.Herbage[c].Biomass; // kg/ha
        //            double availKg = availPerHa * area;
        //            totalAvailableByClass[c] += availKg;
        //        }

        //        for (int sp = 1; sp <= maxSpecies; sp++)
        //            for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            {
        //                double availPerHa = g.Seeds[sp, rp].Biomass;
        //                double availKg = availPerHa * area;
        //                totalSeedAvailable[sp, rp] += availKg;
        //            }
        //    }

        //    // compute simple selection scores per class (availability weighted by avg DMD & height in previous implementation)
        //    // Reuse previous weighting concept to allocate required across classes & seeds

        //    // compute avg heightRatio & digest for each class
        //    double[] heightWeightedByClass = new double[classMax + 1];
        //    double[] digestWeightedByClass = new double[classMax + 1];
        //    double[] avgHeightRatio = new double[classMax + 1];
        //    double[] avgDigest = new double[classMax + 1];

        //    for (int p = 0; p < grazingInputs.Length; p++)
        //    {
        //        var g = grazingInputs[p];
        //        if (g == null) continue;
        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double availPerHa = g.Herbage[c].Biomass;
        //            double availKg = availPerHa * area;
        //            heightWeightedByClass[c] += availKg * g.Herbage[c].HeightRatio;
        //            digestWeightedByClass[c] += availKg * g.Herbage[c].Digestibility;
        //        }
        //    }
        //    for (int c = 1; c <= classMax; c++)
        //    {
        //        if (totalAvailableByClass[c] > 0.0)
        //        {
        //            avgHeightRatio[c] = heightWeightedByClass[c] / totalAvailableByClass[c];
        //            avgDigest[c] = digestWeightedByClass[c] / totalAvailableByClass[c];
        //        }
        //    }

        //    // class scores
        //    double qualityCoeff = this.IntakeQualityCoefficient;
        //    double[] classScore = new double[classMax + 1];
        //    double totalClassScore = 0.0;
        //    for (int c = 1; c <= classMax; c++)
        //    {
        //        double hr = Math.Max(0.000001, avgHeightRatio[c]);
        //        double score = totalAvailableByClass[c] * hr * (1.0 + qualityCoeff * avgDigest[c]);
        //        classScore[c] = score;
        //        totalClassScore += score;
        //    }

        //    // seed scores
        //    double[,] seedScore = new double[maxSpecies + 1, ripeMax + 1];
        //    double totalSeedScore = 0.0;
        //    for (int sp = 1; sp <= maxSpecies; sp++)
        //        for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //        {
        //            double score = totalSeedAvailable[sp, rp] * (1.0 + qualityCoeff * 0.0); // no seed digest weighting available here
        //            seedScore[sp, rp] = score;
        //            totalSeedScore += score;
        //        }

        //    // allocate required DM across classes first, then seeds if remainder exists
        //    double[] toRemoveByClass = new double[classMax + 1];
        //    if (totalClassScore > 0.0)
        //    {
        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double alloc = required * (classScore[c] / totalClassScore);
        //            toRemoveByClass[c] = Math.Min(alloc, totalAvailableByClass[c]);
        //        }
        //    }

        //    double removedSoFar = toRemoveByClass.Sum();
        //    double remainingReq = Math.Max(0.0, required - removedSoFar);

        //    double[,] toRemoveSeed = new double[maxSpecies + 1, ripeMax + 1];
        //    if (remainingReq > 0.0 && totalSeedScore > 0.0)
        //    {
        //        for (int sp = 1; sp <= maxSpecies; sp++)
        //            for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            {
        //                double alloc = remainingReq * (seedScore[sp, rp] / totalSeedScore);
        //                toRemoveSeed[sp, rp] = Math.Min(alloc, totalSeedAvailable[sp, rp]);
        //            }
        //    }

        //    // store the daily removal arrays for later application (kg)
        //    dailyRemovalByClass = new double[classMax + 1];
        //    Array.Copy(toRemoveByClass, dailyRemovalByClass, classMax + 1);
        //    dailyRemovalSeed = new double[maxSpecies + 1, ripeMax + 1];
        //    for (int sp = 1; sp <= maxSpecies; sp++)
        //        for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            dailyRemovalSeed[sp, rp] = toRemoveSeed[sp, rp];

        //    // Build combined paddock-level packet from weighted averages of the removed material
        //    double totalRemoved = 0.0;
        //    double sumDMDTimesAmt = 0.0; // DMD fraction * kg
        //    double sumCPTimesAmt = 0.0;  // CP percent * kg

        //    // accumulate herbage classes contribution to weighted averages
        //    for (int p = 0; p < grazingInputs.Length; p++)
        //    {
        //        var g = grazingInputs[p];
        //        if (g == null) continue;

        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double providerAvailKg = g.Herbage[c].Biomass * area;
        //            if (providerAvailKg <= 0.0 || totalAvailableByClass[c] <= 0.0) continue;
        //            double share = providerAvailKg / totalAvailableByClass[c];
        //            double removedKg = toRemoveByClass[c] * share;
        //            removedKg = Math.Min(removedKg, providerAvailKg);
        //            if (removedKg <= 0.0) continue;

        //            double dmd = g.Herbage[c].Digestibility; // 0..1
        //            double cp = g.Herbage[c].CrudeProtein;   // percent
        //            totalRemoved += removedKg;
        //            sumDMDTimesAmt += dmd * removedKg;
        //            sumCPTimesAmt += cp * removedKg;
        //        }

        //        // seeds
        //        for (int sp = 1; sp <= maxSpecies; sp++)
        //            for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            {
        //                double providerAvailKg = g.Seeds[sp, rp].Biomass * area;
        //                if (providerAvailKg <= 0.0 || totalSeedAvailable[sp, rp] <= 0.0) continue;
        //                double share = providerAvailKg / totalSeedAvailable[sp, rp];
        //                double removedKg = toRemoveSeed[sp, rp] * share;
        //                removedKg = Math.Min(removedKg, providerAvailKg);
        //                if (removedKg <= 0.0) continue;

        //                double dmd = g.Seeds[sp, rp].Digestibility;
        //                double cp = g.Seeds[sp, rp].CrudeProtein;
        //                totalRemoved += removedKg;
        //                sumDMDTimesAmt += dmd * removedKg;
        //                sumCPTimesAmt += cp * removedKg;
        //            }
        //    }

        //    //request.Provided = totalRemoved * events.Interval;

        //    //if (AggregationMode == FeedAggregationMode.CombineByForageType)
        //    //{
        //    //    // todo: still to implement as list of FoodStorePackets
        //    //    // build individual packets per provider if requested
        //    //    // (not currently used as the grazing activity does not take advantage of per-provider details, but could be in future or for reporting)
        //    //}
        //    //else
        //    //{
        //    //    // combine into single paddock level food store
        //    //    dailyPaddockPacket = new FoodResourcePacket();
        //    //    dailyPaddockPacket.TypeOfFeed = this.TypeOfFeed;
        //    //    dailyPaddockPacket.AddAmount(totalRemoved);
        //    //    double avgDMD = sumDMDTimesAmt / totalRemoved; // fraction
        //    //    double avgCP = sumCPTimesAmt / totalRemoved;   // percent
        //    //    dailyPaddockPacket.DryMatterDigestibility = avgDMD * 100.0;
        //    //    dailyPaddockPacket.CrudeProteinPercent = avgCP * 100.0;
        //    //    dailyPaddockPacket.NitrogenPercent = dailyPaddockPacket.CrudeProteinPercent / FoodResourcePacket.FeedProteinToNitrogenFactor;
        //    //    dailyPaddockPacket.FatPercent = this.FatPercent;
        //    //    dailyPaddockPacket.MetabolisableEnergyContent = this.MetabolisableEnergyContent;
        //    //    dailyPaddockPacket.GrossEnergyContent = this.GrossEnergyContent;
        //    //    dailyPaddockPacket.RumenDegradableProteinPercent = this.RumenDegradableProteinPercent;
        //    //    dailyPaddockPacket.AcidDetergentInsolubleProtein = this.AcidDetergentInsolubleProtein;
        //    //    dailyPaddockPacket.GutFill = this.GutFill;
        //    //    //todo: calculate gutfill somehow. We don't know the range from min to max dmd here!
        //    //}

        //    timeStepPaddockPacket = dailyPaddockPacket.Clone((request.AdditionalDetails as RuminantActivityGrazePastureHerd).DailyPastureRequired);
        //}

        ///// <summary>
        ///// Compute biomass-weighted paddock quality (DMD and CP) from the forage inputs and update the link's quality
        ///// properties so they reflect current paddock state.
        ///// </summary>
        //private void UpdatePaddockQuality()
        //{
        //    if (paddockInfo == null || paddockInfo.Forages == null)
        //        return;

        //    int classMax = GrazType.DigClassNo;
        //    int maxSpecies = GrazType.MaxPlantSpp;
        //    int ripeMax = GrazType.RIPE;

        //    // Build grazingInputs snapshot (kg/ha)
        //    Array.Resize(ref grazingInputs, paddockInfo.Forages.Count());
        //    for (int jdx = 0; jdx <= paddockInfo.Forages.Count() - 1; jdx++)
        //    {
        //        grazingInputs[jdx] = paddockInfo.Forages.ByIndex(jdx).AvailForage();
        //    }

        //    double area = paddockInfo.Area; // hectares => per-ha values multiplied by area give kg

        //    double totalBiomass = 0.0;          // kg
        //    double sumDMDTimesKg = 0.0;         // (fraction * kg)
        //    double sumCPTimesKg = 0.0;          // (% * kg)

        //    // accumulate herbage classes
        //    for (int p = 0; p < grazingInputs.Length; p++)
        //    {
        //        var g = grazingInputs[p];
        //        if (g == null) continue;

        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double availPerHa = g.Herbage[c].Biomass; // kg/ha
        //            double availKg = availPerHa * area;       // kg
        //            if (availKg <= 0.0) continue;

        //            double dmd = g.Herbage[c].Digestibility;     // 0..1
        //            double cp = g.Herbage[c].CrudeProtein;       // percent (as used elsewhere)
        //            totalBiomass += availKg;
        //            sumDMDTimesKg += dmd * availKg;
        //            sumCPTimesKg += cp * availKg;
        //        }

        //        // include seeds if present
        //        for (int sp = 1; sp <= maxSpecies; sp++)
        //        {
        //            for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            {
        //                double availPerHa = g.Seeds[sp, rp].Biomass;
        //                double availKg = availPerHa * area;
        //                if (availKg <= 0.0) continue;

        //                double dmd = g.Seeds[sp, rp].Digestibility;    // 0..1
        //                double cp = g.Seeds[sp, rp].CrudeProtein;      // percent
        //                totalBiomass += availKg;
        //                sumDMDTimesKg += dmd * availKg;
        //                sumCPTimesKg += cp * availKg;
        //            }
        //        }
        //    }

        //    if (totalBiomass > 0.0)
        //    {
        //        double avgDMD = sumDMDTimesKg / totalBiomass;   // fraction 0..1
        //        double avgCP = sumCPTimesKg / totalBiomass;     // percent

        //        DryMatterDigestibility = avgDMD * 100.0;        // store as percent
        //        CrudeProteinPercent = avgCP;
        //        NitrogenPercent = CrudeProteinPercent / FoodResourcePacket.FeedProteinToNitrogenFactor;

        //        // Recompute ADF-insoluble protein from current RDP percent setting (unchanged)
        //        AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(RumenDegradableProteinPercent, TypeOfFeed);
        //    }
        //    else
        //    {
        //        // no biomass -> leave values as-is or set defaults (keep previous values)
        //    }
        //}


        ///// <summary>
        ///// Apply the prepared forage removals for the daily intake request
        ///// </summary>
        //private void ApplyForageRemovals()
        //{
        //    // nothing prepared
        //    if (dailyRemovalByClass == null || dailyRemovalByClass.Length == 0)
        //        return;

        //    if (paddockInfo == null || grazingInputs == null)
        //        return;

        //    int classMax = GrazType.DigClassNo;
        //    int maxSpecies = GrazType.MaxPlantSpp;
        //    int ripeMax = GrazType.RIPE;
        //    double area = paddockInfo.Area; // ha
        //    double area_m2 = Math.Max(0.0, area) * 10000.0;

        //    // recompute totals across providers (kg) so we can proportionally distribute per-class and per-seed removals
        //    double[] totalAvailableByClass = new double[classMax + 1];
        //    double[,] totalSeedAvailable = new double[maxSpecies + 1, ripeMax + 1];

        //    for (int p = 0; p < grazingInputs.Length; p++)
        //    {
        //        var g = grazingInputs[p];
        //        if (g == null) continue;

        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double availPerHa = g.Herbage[c].Biomass;
        //            double availKg = availPerHa * area;
        //            totalAvailableByClass[c] += availKg;
        //        }

        //        for (int sp = 1; sp <= maxSpecies; sp++)
        //            for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            {
        //                double availPerHa = g.Seeds[sp, rp].Biomass;
        //                double availKg = availPerHa * area;
        //                totalSeedAvailable[sp, rp] += availKg;
        //            }
        //    }

        //    // For each provider compute its share of each class and seed removal and apply RemoveBiomass
        //    for (int p = 0; p < grazingInputs.Length; p++)
        //    {
        //        var g = grazingInputs[p];
        //        if (g == null) continue;

        //        double providerRemovedKg = 0.0;

        //        // sum classes
        //        for (int c = 1; c <= classMax; c++)
        //        {
        //            double providerAvailKg = g.Herbage[c].Biomass * area;
        //            if (providerAvailKg <= 0.0 || totalAvailableByClass[c] <= 0.0) continue;
        //            double share = providerAvailKg / totalAvailableByClass[c];
        //            double removedKg = dailyRemovalByClass[c] * share;
        //            removedKg = Math.Min(removedKg, providerAvailKg);
        //            providerRemovedKg += removedKg;
        //        }

        //        // sum seeds
        //        for (int sp = 1; sp <= maxSpecies; sp++)
        //            for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
        //            {
        //                double providerAvailKg = g.Seeds[sp, rp].Biomass * area;
        //                if (providerAvailKg <= 0.0 || totalSeedAvailable[sp, rp] <= 0.0) continue;
        //                double share = providerAvailKg / totalSeedAvailable[sp, rp];
        //                double removedKg = dailyRemovalSeed[sp, rp] * share;
        //                removedKg = Math.Min(removedKg, providerAvailKg);
        //                providerRemovedKg += removedKg;
        //            }

        //        if (providerRemovedKg <= 0.0)
        //            continue;

        //        try
        //        {
        //            var provider = forageProviders.ForageProvider(p);
        //            if (provider == null || provider.ForageObj == null)
        //                continue;

        //            // compute available consumable (g/m2) on this provider
        //            double total_consumable_gm2 = provider.ForageObj.Material.Sum(m => m.Consumable?.Wt ?? 0.0);
        //            if (total_consumable_gm2 <= 0.0)
        //                continue;

        //            // desired removal expressed as g/m2 across the paddock area
        //            double desired_removed_gm2 = providerRemovedKg * 1000.0 / area_m2;

        //            // fraction of available consumable to remove (clamped 0..1)
        //            double fraction = Math.Max(0.0, Math.Min(1.0, desired_removed_gm2 / total_consumable_gm2));

        //            // call forage model RemoveBiomass - returns actual g/m2 removed
        //            double actuallyRemoved_gm2 = provider.ForageObj.RemoveBiomass(liveToRemove: fraction, deadToRemove: fraction, liveToResidue: 0.0, deadToResidue: 0.0);

        //            // convert to kg removed over paddock area
        //            double actualRemovedKg = actuallyRemoved_gm2 * area_m2 / 1000.0;

        //            // update running consumed total
        //            biomassConsumed += actualRemovedKg;
        //        }
        //        catch (Exception ex)
        //        {
        //            Summary.WriteMessage(this, $"Error applying forage removal for provider index {p}: {ex.Message}", MessageType.Error);
        //        }
        //    }
        //}

        ///// <inheritdoc/>
        //public void ApplyDailyIntakeReduction(double fractionReduced)
        //{
        //    //// Adjust intake amounts due to a reduction identified elsewhere
        //    //double fractionRemaining = 1.0 - fractionReduced;

        //    //if (fractionRemaining >= 1.0)
        //    //    return;

        //    //if (fractionRemaining < 0.0)
        //    //    throw new ArgumentOutOfRangeException(nameof(fractionRemaining), "Fraction must be between 0 and 1.");

        //    //if (CurrentGrazingRequest != null)
        //    //    CurrentGrazingRequest.Provided *= fractionRemaining;

        //    //if (timeStepPaddockPacket != null)
        //    //    timeStepPaddockPacket.SetAmount(timeStepPaddockPacket.Amount * fractionRemaining);

        //    //if (dailyPaddockPacket != null)
        //    //    dailyPaddockPacket.SetAmount(dailyPaddockPacket.Amount * fractionRemaining);

        //    //// Scale per-class removal array (kg)
        //    //if (dailyRemovalByClass != null && dailyRemovalByClass.Length > 0)
        //    //{
        //    //    for (int i = 0; i < dailyRemovalByClass.Length; i++)
        //    //        dailyRemovalByClass[i] *= fractionRemaining;
        //    //}

        //    //// Scale per-seed removal 2D array (kg)
        //    //if (dailyRemovalSeed != null && dailyRemovalSeed.Length > 0)
        //    //{
        //    //    int dim0 = dailyRemovalSeed.GetLength(0);
        //    //    int dim1 = dailyRemovalSeed.GetLength(1);
        //    //    for (int i = 0; i < dim0; i++)
        //    //        for (int j = 0; j < dim1; j++)
        //    //            dailyRemovalSeed[i, j] *= fractionRemaining;
        //    //}
        //}

        #region transactions

        /// <summary>
        /// Remove a specified amount from the resource.
        /// </summary>
        /// <param name="amountToRemove">Amount to remove from resource store</param>
        /// <param name="pendingRequest">
        /// Provides a the request if this is a pending transaction that has not yet been completed. This will not
        /// reduce the amount total until available until the transaction is completed.
        /// </param>
        /// <returns>Amount removed</returns>
        protected double Remove(double amountToRemove, ResourceRequest pendingRequest)
        {
            amountToRemove = base.RemoveFromResource(amountToRemove, pendingRequest);

            // add pending amount to each pool
            if (pendingRequest.AdditionalDetails is IEnumerable<FoodResourceStore> foodStores)
            {
                foreach (var foodStore in foodStores)
                {
                    for (int i = 0; i < foodStore.Pools.Count; i++)
                    {
                        foodStore.Pools[i].AmountPending += foodStore.Details.Amount * foodStore.PoolProportions[i];
                    }
                }
            }
            return amountToRemove;
        }

        /// <inheritdoc/>
        public new void DecreasePending(ResourceRequest request, double amount)
        {
            // receives the entire amount to remove from the resource holder that needs to be proportioned to pools
            if (request.AdditionalDetails is FoodResourceStore foodStore)
            {
                for (int i = 0; i < foodStore.Pools.Count; i++)
                {
                    foodStore.Pools[i].ReducePending(amount * foodStore.PoolProportions[i]);
                }
            }
            // do removal from pending
            base.DecreasePending(request, amount);
        }

        /// <inheritdoc/>
        public new void DecreasePendingByProportion(ResourceRequest request, double proportion)
        {
            double totalAmount = 0;
            if (request.AdditionalDetails is FoodResourceStore foodStore)
            {
                for (int i = 0; i < foodStore.Pools.Count; i++)
                {
                    double amountToRemove = foodStore.Pools[i].AmountPending * proportion;
                    foodStore.Pools[i].ReducePending(amountToRemove);
                    totalAmount += amountToRemove;
                }
            }
            // do removal from pending
            base.DecreasePending(request, totalAmount);
        }

        /// <inheritdoc/>
        public new void RemoveFromResource(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            if (request.AdditionalDetails is null)
            {
                throw new Exception("A ResourceRequest to remove from GrazeFoodStoreType must contain a value in the AdditionalDetails property");
            }

            switch (request.AdditionalDetails)
            {
                case IEnumerable<FoodResourceStore> foodStores:
                    // A food store will be provided for grazing activities representing the pool group consumed. 
                    // nothing is needed here. 
                    // the base remove below will set the pending requests in the resource type which will then be filled in the selective feeding process and adjusted in Ruminant.Intake
                    intakeStoreRequests.AddRange(foodStores);
                    Remove(request.Required, request);
                    break;
                case PastureActivityCutAndCarry:
                case PastureActivityBurn:
                    //RemoveFromPools(request);
                    // use generic removal to handle pending and reporting transaction if needed 
                    base.RemoveFromResource(request);
                    break;
                case CropActivityManageProduct:
                    // this occurs when the pasture is being replaced by the provided biomass and clears the stores
                    if (request.Category == "StoreCleared")
                    {
                        //double amountCleared = Pools.Sum(a => a.AmountAvailable);
                        //if (amountCleared == 0)
                       // {
                        //    return;
                        //}
                        //Pools.Clear();
                        //request.Provided = amountCleared;
                        // use generic removal to handle pending and reporting transaction if needed 
                        base.RemoveFromResource(request);
                        //ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
                    }

                    break;
                default:
                    // Need to add new section here to allow non grazing activity to remove resources from pasture.
                    throw new Exception("Removing resources from GrazeFoodStore can only be performed by a grazing, burning and cut and carry activities at this stage");
            }
        }

        /// <summary>
        /// Performs a transaction by specified amount.
        /// </summary>
        /// <param name="request">The amount of the transaction.</param>
        /// <param name="handlePendingTransaction">
        /// This transaction should handle any pending amount rather than the amount provided.
        /// </param>
        public override void PerformTransaction(ResourceRequest request, bool handlePendingTransaction = false)
        {
            double provided = 0;
            // remove all pending and take from pools 
            // set provided to peding pool amounts
            if (request.AdditionalDetails is IEnumerable<FoodResourceStore> foodStores)
            {
                foreach (var foodStore in foodStores)
                {
                    for (int i = 0; i < foodStore.Pools.Count; i++)
                    {
                        provided += foodStore.Pools[i].AmountPending;
                        biomassConsumed += foodStore.Pools[i].AmountPending;
                        foodStore.Pools[i].ConsumePending();
                    }
                }
            }
            request.Provided = provided;

            base.PerformTransaction(request, handlePendingTransaction);
        }

        /// <inheritdoc/>
        public new void AddToResource(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            throw new NotImplementedException("Biomass cannot be added to a linked APSIM paddock");
        }

        /// <inheritdoc/>
        public double Remove(double removeAmount, string activityName, string reason)
        {
            throw new NotImplementedException("Biomass cannot be removed from a linked APSIM paddock");
        }

        #endregion

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (forages is null)
            {
                yield return new ValidationResult($"Could not find a Forages component in scope.", ["APSIM Forages component not found"]);
            }
            if (paddock is null)
            {
                yield return new ValidationResult($"Could not find a Paddock (Zone) component named [{PaddockName}] in scope.", ["APSIM Paddock component not found"]);
            }
        }
    }
}