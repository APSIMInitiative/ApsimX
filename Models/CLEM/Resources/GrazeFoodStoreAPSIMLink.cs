using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.ForageDigestibility;
using Models.GrazPlan;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a GrazeFoodType that links directly to an APSIM paddock containing forages for grazing
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
        [Link]
        private CLEMEvents events = null;
        private double biomassAddedThisYear;
        private double biomassConsumed;
        private Forages forages;
        private ForageProviders forageProviders = new();
        private PaddockInfo paddockInfo;
        private GrazType.GrazingInputs[] grazingInputs = new GrazType.GrazingInputs[0];
        private Zone paddock;
        private ResourceRequest dailyIntakeRequest = null;
        private double[] dailyRemovalByClass = new double[0];           // kg per DMD class (1..DigClassNo)
        private double[,] dailyRemovalSeed = new double[0, 0];          // kg per seed [species, ripe]
        private FoodResourcePacket dailyPaddockPacket = null;
        //private FoodResourcePacket timeStepPaddockPacket = null;

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
        public double MetabolisableEnergyContent { get; set; } = 8.0;

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

        /// <inheritdoc/>
        [JsonIgnore]
        public double GutFill
        {
            get
            {
                if (DryMatterDigestibility <= LowestDMD)
                {
                    return GutFillLowQuality;
                }
                if (DryMatterDigestibility >= HighestDMD)
                {
                    return GutFillHighQuality;
                }
                return GutFillLowQuality + ((DryMatterDigestibility - LowestDMD)/(HighestDMD - LowestDMD)) * (GutFillHighQuality - GutFillLowQuality);
            }
            set
            {
            }
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public double OverallPastureBiomass { get; set; }

        /// <summary>
        /// Coefficient to adjust intake for tropical herbage quality
        /// </summary>
        [Category("Advanced", "Intake")]
        [Description("Coefficient to adjust intake for tropical herbage quality")]
        [Required]
        public double IntakeTropicalQualityCoefficient { get; set; } = 0.16;

        /// <summary>
        /// Coefficient to adjust intake for herbage quality
        /// </summary>
        [Category("Advanced", "Intake")]
        [Description("Coefficient to adjust intake for herbage quality")]
        [Required]
        public double IntakeQualityCoefficient { get; set; } = 1.7;

        /// <summary>
        /// Initial pasture biomass
        /// </summary>
        public double StartingAmount { get; set; } = 0;

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
            }
        }

        private double amount = 0;

        /// <summary>
        /// Amount (kg total)
        /// </summary>
        [JsonIgnore]
        public double Amount
        {
            get
            {
                return amount;  // Set in update paddock in PastureReady event
            }
        }

        /// <summary>
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa
        {
            get
            {
                if (paddockInfo is null)
                {
                    return 0;
                }
                return Amount / paddockInfo.Area;
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
                if (paddockInfo is null)
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
            biomassAddedThisYear = paddockInfo.SummedGreenMass;
        }

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
            //AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(RumenDegradableProteinPercent, TypeOfFeed);

            forages = Node.Find<Forages>();
            paddock = Node.Find<Zone>(name: PaddockName);
            if (forages is null || paddock is null)
                return;

            // set up the ForageProviders for this paddock
            paddockInfo = new PaddockInfo(zone: paddock, structure: Structure) { zone = paddock };

            // find all the child crop, pasture components that have removable biomass
            foreach (var forage in forages.ModelsWithDigestibleBiomass.Where(m => m.Zone == paddock))
                forageProviders.AddProvider(paddockInfo, paddock.Name, paddock.Name + "." + forage.Name, 0, 0, forage);
        }

        /// <summary>Store amount of pasture available for everyone at the start of the step (kg per hectare)</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPastureReady")]
        private void OnCLEMPastureReady(object sender, EventArgs e)
        {
            paddockInfo.ClearSupplement();
            paddockInfo.ZeroRemoval();

            // request available to ruminants (modified from Stock.RequestAvailableToAnimal())
            amount = 0;
            for (int i = 0; i < forageProviders.Count(); i++)
            {
                var provider = forageProviders.ForageProvider(i);
                if (provider.ForageObj != null)
                {
                    provider.PastureGreenDM = provider.ForageObj.Material.Where(m => m.IsLive)
                                                                .Sum(m => m.Consumable.Wt); // g/m^2
                    provider.UpdateForages(provider.ForageObj);
                    amount += provider.PastureGreenDM;
                }
            }

            // convert g/m^2 to kg/ha total
            amount *= 10 * paddockInfo.Area;

            // do not return zero as there is always something there and zero affects calculations.
            TonnesPerHectareStartOfTimeStep = Math.Max(TonnesPerHectare, 0.01);

            // Update the link's quality properties from forage model values so reports can read current paddock quality.
            UpdatePaddockQuality();
        }
        
        /// <summary>Event to remove the current daily intake from forage when CLEM is determining daily consumption for time step</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDailyASPIMForageTake")]
        private void OnCLEMTakeAPSIMForage(object sender, EventArgs e)
        {
            // If nothing prepared, nothing to do
            if (dailyIntakeRequest == null)
                return;

            // Apply one day's worth of prepared removals
            ApplyForageRemovals(dailyIntakeRequest);
        }

        #region transactions

        /// <summary>
        /// Method to set the daily forage take by grazing animals
        /// </summary>
        /// <param name="request">The resource request passed from the grazing activity</param>
        public void SetDailyForageTakeFromGrazing(ResourceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // duplicate request and divide by number of days in time step
            dailyIntakeRequest = request.Clone();
            dailyIntakeRequest.Provided /= events.Interval;
            dailyIntakeRequest.Required /= events.Interval;

            // build snapshot of grazing inputs (kg/ha units)
            Array.Resize(ref grazingInputs, paddockInfo.Forages.Count());
            for (int jdx = 0; jdx <= paddockInfo.Forages.Count() - 1; jdx++)
            {
                if (grazingInputs[jdx] == null)
                    grazingInputs[jdx] = new GrazType.GrazingInputs();
                grazingInputs[jdx] = paddockInfo.Forages.ByIndex(jdx).AvailForage();
            }

            // quick exit if nothing required
            double required = dailyIntakeRequest.Required;
            if (required <= 0.0)
            {
                request.Provided = 0.0;
                // clear stored daily removals
                dailyRemovalByClass = new double[0];
                dailyRemovalSeed = new double[0, 0];
                dailyPaddockPacket = null;
                return;
            }

            int classMax = GrazType.DigClassNo;
            int maxSpecies = GrazType.MaxPlantSpp;
            int ripeMax = GrazType.RIPE;
            double area = paddockInfo.Area; // hectares

            // Total availability (kg) aggregated across providers
            double[] totalAvailableByClass = new double[classMax + 1];
            double[,] totalSeedAvailable = new double[maxSpecies + 1, ripeMax + 1];

            for (int p = 0; p < grazingInputs.Length; p++)
            {
                var g = grazingInputs[p];
                if (g == null) continue;

                for (int c = 1; c <= classMax; c++)
                {
                    double availPerHa = g.Herbage[c].Biomass; // kg/ha
                    double availKg = availPerHa * area;
                    totalAvailableByClass[c] += availKg;
                }

                for (int sp = 1; sp <= maxSpecies; sp++)
                    for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    {
                        double availPerHa = g.Seeds[sp, rp].Biomass;
                        double availKg = availPerHa * area;
                        totalSeedAvailable[sp, rp] += availKg;
                    }
            }

            // compute simple selection scores per class (availability weighted by avg DMD & height in previous implementation)
            // Reuse previous weighting concept to allocate required across classes & seeds

            // compute avg heightRatio & digest for each class
            double[] heightWeightedByClass = new double[classMax + 1];
            double[] digestWeightedByClass = new double[classMax + 1];
            double[] avgHeightRatio = new double[classMax + 1];
            double[] avgDigest = new double[classMax + 1];

            for (int p = 0; p < grazingInputs.Length; p++)
            {
                var g = grazingInputs[p];
                if (g == null) continue;
                for (int c = 1; c <= classMax; c++)
                {
                    double availPerHa = g.Herbage[c].Biomass;
                    double availKg = availPerHa * area;
                    heightWeightedByClass[c] += availKg * g.Herbage[c].HeightRatio;
                    digestWeightedByClass[c] += availKg * g.Herbage[c].Digestibility;
                }
            }
            for (int c = 1; c <= classMax; c++)
            {
                if (totalAvailableByClass[c] > 0.0)
                {
                    avgHeightRatio[c] = heightWeightedByClass[c] / totalAvailableByClass[c];
                    avgDigest[c] = digestWeightedByClass[c] / totalAvailableByClass[c];
                }
            }

            // class scores
            double qualityCoeff = this.IntakeQualityCoefficient;
            double[] classScore = new double[classMax + 1];
            double totalClassScore = 0.0;
            for (int c = 1; c <= classMax; c++)
            {
                double hr = Math.Max(0.000001, avgHeightRatio[c]);
                double score = totalAvailableByClass[c] * hr * (1.0 + qualityCoeff * avgDigest[c]);
                classScore[c] = score;
                totalClassScore += score;
            }

            // seed scores
            double[,] seedScore = new double[maxSpecies + 1, ripeMax + 1];
            double totalSeedScore = 0.0;
            for (int sp = 1; sp <= maxSpecies; sp++)
                for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                {
                    double score = totalSeedAvailable[sp, rp] * (1.0 + qualityCoeff * 0.0); // no seed digest weighting available here
                    seedScore[sp, rp] = score;
                    totalSeedScore += score;
                }

            // allocate required DM across classes first, then seeds if remainder exists
            double[] toRemoveByClass = new double[classMax + 1];
            if (totalClassScore > 0.0)
            {
                for (int c = 1; c <= classMax; c++)
                {
                    double alloc = required * (classScore[c] / totalClassScore);
                    toRemoveByClass[c] = Math.Min(alloc, totalAvailableByClass[c]);
                }
            }

            double removedSoFar = toRemoveByClass.Sum();
            double remainingReq = Math.Max(0.0, required - removedSoFar);

            double[,] toRemoveSeed = new double[maxSpecies + 1, ripeMax + 1];
            if (remainingReq > 0.0 && totalSeedScore > 0.0)
            {
                for (int sp = 1; sp <= maxSpecies; sp++)
                    for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    {
                        double alloc = remainingReq * (seedScore[sp, rp] / totalSeedScore);
                        toRemoveSeed[sp, rp] = Math.Min(alloc, totalSeedAvailable[sp, rp]);
                    }
            }

            // store the daily removal arrays for later application (kg)
            dailyRemovalByClass = new double[classMax + 1];
            Array.Copy(toRemoveByClass, dailyRemovalByClass, classMax + 1);
            dailyRemovalSeed = new double[maxSpecies + 1, ripeMax + 1];
            for (int sp = 1; sp <= maxSpecies; sp++)
                for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    dailyRemovalSeed[sp, rp] = toRemoveSeed[sp, rp];

            // Build combined paddock-level packet from weighted averages of the removed material
            double totalRemoved = 0.0;
            double sumDMDTimesAmt = 0.0; // DMD fraction * kg
            double sumCPTimesAmt = 0.0;  // CP percent * kg

            // accumulate herbage classes contribution to weighted averages
            for (int p = 0; p < grazingInputs.Length; p++)
            {
                var g = grazingInputs[p];
                if (g == null) continue;

                for (int c = 1; c <= classMax; c++)
                {
                    double providerAvailKg = g.Herbage[c].Biomass * area;
                    if (providerAvailKg <= 0.0 || totalAvailableByClass[c] <= 0.0) continue;
                    double share = providerAvailKg / totalAvailableByClass[c];
                    double removedKg = toRemoveByClass[c] * share;
                    removedKg = Math.Min(removedKg, providerAvailKg);
                    if (removedKg <= 0.0) continue;

                    double dmd = g.Herbage[c].Digestibility; // 0..1
                    double cp = g.Herbage[c].CrudeProtein;   // percent
                    totalRemoved += removedKg;
                    sumDMDTimesAmt += dmd * removedKg;
                    sumCPTimesAmt += cp * removedKg;
                }

                // seeds
                for (int sp = 1; sp <= maxSpecies; sp++)
                    for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    {
                        double providerAvailKg = g.Seeds[sp, rp].Biomass * area;
                        if (providerAvailKg <= 0.0 || totalSeedAvailable[sp, rp] <= 0.0) continue;
                        double share = providerAvailKg / totalSeedAvailable[sp, rp];
                        double removedKg = toRemoveSeed[sp, rp] * share;
                        removedKg = Math.Min(removedKg, providerAvailKg);
                        if (removedKg <= 0.0) continue;

                        double dmd = g.Seeds[sp, rp].Digestibility;
                        double cp = g.Seeds[sp, rp].CrudeProtein;
                        totalRemoved += removedKg;
                        sumDMDTimesAmt += dmd * removedKg;
                        sumCPTimesAmt += cp * removedKg;
                    }
            }

            // create the single paddock packet (if anything removed)
            var packets = new List<FoodResourcePacket>();
            if (totalRemoved > 0.0)
            {
                var packet = new FoodResourcePacket();
                packet.TypeOfFeed = this.TypeOfFeed;
                packet.Amount = totalRemoved;
                double avgDMD = sumDMDTimesAmt / totalRemoved; // fraction
                double avgCP = sumCPTimesAmt / totalRemoved;   // percent
                packet.DryMatterDigestibility = avgDMD * 100.0;
                packet.CrudeProteinPercent = avgCP;
                packet.NitrogenPercent = packet.CrudeProteinPercent / FoodResourcePacket.FeedProteinToNitrogenFactor;
                packet.FatPercent = this.FatPercent;
                packet.MetabolisableEnergyContent = this.MetabolisableEnergyContent;
                packet.GrossEnergyContent = this.GrossEnergyContent;
                packet.RumenDegradableProteinPercent = this.RumenDegradableProteinPercent;
                packet.AcidDetergentInsolubleProtein = this.AcidDetergentInsolubleProtein;
                packet.GutFill = this.GutFill;

                dailyPaddockPacket = packet;
                packets.Add(packet);
            }
            else
            {
                dailyPaddockPacket = null;
            }

            // finalize request
            //double provided = packets.Sum(p => p.Amount);
            //request.Provided = provided;
            // do not modify biomassConsumed here; it is updated when ApplyForageRemovals runs
        }

        /// <inheritdoc/>
        public new void Remove(ResourceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.AdditionalDetails is null && request.Required > 0)
                throw new Exception("Removing biomass from APSIM.Paddock requires AdditionalDetails property provided in resource request");

            switch (request.ActivityModel)
            {
                case RuminantActivityGrazePastureHerd:
                    // request expected
                    // request required has all animal reductions
                    // but this has already been through pasture reductions. in competition limiter.
                    // comp limiter should be proportion on combined animal intake by each breed, not reflecting pasture available
                    
                    request.Provided = Math.Min(request.Required, Amount);
                    request.Available = Amount;

                    ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);

                    // Build the daily biomass remove packages for ongoing daily removal until next time step call this method again.
                    SetDailyForageTakeFromGrazing(request);
                    break;
                case PastureActivityCutAndCarry:
                    // call OnCLEMTakeAPSIMForage to apply the removal immediately
                    break;
                case PastureActivityBurn:
                    // call OnCLEMTakeAPSIMForage to apply the removal immediately
                    break;
                default:
                    throw new NotImplementedException($"The Activity [a={request.AdditionalDetails.GetType()}] is not currently implemented in [GrazeFoodStoreTypeAPSIMLink].Remove()] ");
            }
        }

        #endregion

        /// <summary>
        /// Compute biomass-weighted paddock quality (DMD and CP) from the forage inputs
        /// and update the link's quality properties so they reflect current paddock state.
        /// </summary>
        private void UpdatePaddockQuality()
        {
            if (paddockInfo == null || paddockInfo.Forages == null)
                return;

            int classMax = GrazType.DigClassNo;
            int maxSpecies = GrazType.MaxPlantSpp;
            int ripeMax = GrazType.RIPE;

            // Build grazingInputs snapshot (kg/ha)
            Array.Resize(ref grazingInputs, paddockInfo.Forages.Count());
            for (int jdx = 0; jdx <= paddockInfo.Forages.Count() - 1; jdx++)
            {
                grazingInputs[jdx] = paddockInfo.Forages.ByIndex(jdx).AvailForage();
            }

            double area = paddockInfo.Area; // hectares => per-ha values multiplied by area give kg

            double totalBiomass = 0.0;          // kg
            double sumDMDTimesKg = 0.0;         // (fraction * kg)
            double sumCPTimesKg = 0.0;          // (% * kg)

            // accumulate herbage classes
            for (int p = 0; p < grazingInputs.Length; p++)
            {
                var g = grazingInputs[p];
                if (g == null) continue;

                for (int c = 1; c <= classMax; c++)
                {
                    double availPerHa = g.Herbage[c].Biomass; // kg/ha
                    double availKg = availPerHa * area;       // kg
                    if (availKg <= 0.0) continue;

                    double dmd = g.Herbage[c].Digestibility;     // 0..1
                    double cp = g.Herbage[c].CrudeProtein;       // percent (as used elsewhere)
                    totalBiomass += availKg;
                    sumDMDTimesKg += dmd * availKg;
                    sumCPTimesKg += cp * availKg;
                }

                // include seeds if present
                for (int sp = 1; sp <= maxSpecies; sp++)
                {
                    for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    {
                        double availPerHa = g.Seeds[sp, rp].Biomass;
                        double availKg = availPerHa * area;
                        if (availKg <= 0.0) continue;

                        double dmd = g.Seeds[sp, rp].Digestibility;    // 0..1
                        double cp = g.Seeds[sp, rp].CrudeProtein;      // percent
                        totalBiomass += availKg;
                        sumDMDTimesKg += dmd * availKg;
                        sumCPTimesKg += cp * availKg;
                    }
                }
            }

            if (totalBiomass > 0.0)
            {
                double avgDMD = sumDMDTimesKg / totalBiomass;   // fraction 0..1
                double avgCP = sumCPTimesKg / totalBiomass;     // percent

                DryMatterDigestibility = avgDMD * 100.0;        // store as percent
                CrudeProteinPercent = avgCP;
                NitrogenPercent = CrudeProteinPercent / FoodResourcePacket.FeedProteinToNitrogenFactor;

                // Recompute ADF-insoluble protein from current RDP percent setting (unchanged)
                AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(RumenDegradableProteinPercent, TypeOfFeed);
            }
            else
            {
                // no biomass -> leave values as-is or set defaults (keep previous values)
            }
        }

        // Aggregation options for how CLEM will present forage to ruminants
        /// <summary>
        /// The style of aggregating forage take from the APSIM paddock before providing to ruminants. Options are:
        /// </summary>
        public enum FeedAggregationMode
        {
            /// <summary>
            /// Feed ruminants by combined biomass for each forage type present
            /// </summary>
            CombineByForageType,    // combine removals per forage provider into a single packet per provider
            /// <summary>
            /// Feed ruminants by combined biomass for the paddock
            /// </summary>
            CombinePaddock          // combine all removals into a single paddock-level packet
        }

        /// <summary>
        /// How to aggregate the daily forage take before handing packets to ruminants.
        /// </summary>
        [Category("Advanced", "Intake")]
        [Description("Method of combining consumption for reporting")]
        public FeedAggregationMode AggregationMode { get; set; } = FeedAggregationMode.CombinePaddock;

        /// <summary>
        /// Apply the prepared forage removals for the daily intake request
        /// </summary>
        /// <param name="req">Optional request to use for metadata</param>
        private void ApplyForageRemovals(ResourceRequest req = null)
        {
            // nothing prepared
            if (dailyRemovalByClass == null || dailyRemovalByClass.Length == 0)
                return;

            var request = req ?? dailyIntakeRequest;

            if (paddockInfo == null || grazingInputs == null)
                return;

            int classMax = GrazType.DigClassNo;
            int maxSpecies = GrazType.MaxPlantSpp;
            int ripeMax = GrazType.RIPE;
            double area = paddockInfo.Area; // ha
            double area_m2 = Math.Max(0.0, area) * 10000.0;

            // recompute totals across providers (kg) so we can proportionally distribute per-class and per-seed removals
            double[] totalAvailableByClass = new double[classMax + 1];
            double[,] totalSeedAvailable = new double[maxSpecies + 1, ripeMax + 1];

            for (int p = 0; p < grazingInputs.Length; p++)
            {
                var g = grazingInputs[p];
                if (g == null) continue;

                for (int c = 1; c <= classMax; c++)
                {
                    double availPerHa = g.Herbage[c].Biomass;
                    double availKg = availPerHa * area;
                    totalAvailableByClass[c] += availKg;
                }

                for (int sp = 1; sp <= maxSpecies; sp++)
                    for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    {
                        double availPerHa = g.Seeds[sp, rp].Biomass;
                        double availKg = availPerHa * area;
                        totalSeedAvailable[sp, rp] += availKg;
                    }
            }

            // For each provider compute its share of each class and seed removal and apply RemoveBiomass
            for (int p = 0; p < grazingInputs.Length; p++)
            {
                var g = grazingInputs[p];
                if (g == null) continue;

                double providerRemovedKg = 0.0;

                // sum classes
                for (int c = 1; c <= classMax; c++)
                {
                    double providerAvailKg = g.Herbage[c].Biomass * area;
                    if (providerAvailKg <= 0.0 || totalAvailableByClass[c] <= 0.0) continue;
                    double share = providerAvailKg / totalAvailableByClass[c];
                    double removedKg = dailyRemovalByClass[c] * share;
                    removedKg = Math.Min(removedKg, providerAvailKg);
                    providerRemovedKg += removedKg;
                }

                // sum seeds
                for (int sp = 1; sp <= maxSpecies; sp++)
                    for (int rp = GrazType.UNRIPE; rp <= ripeMax; rp++)
                    {
                        double providerAvailKg = g.Seeds[sp, rp].Biomass * area;
                        if (providerAvailKg <= 0.0 || totalSeedAvailable[sp, rp] <= 0.0) continue;
                        double share = providerAvailKg / totalSeedAvailable[sp, rp];
                        double removedKg = dailyRemovalSeed[sp, rp] * share;
                        removedKg = Math.Min(removedKg, providerAvailKg);
                        providerRemovedKg += removedKg;
                    }

                if (providerRemovedKg <= 0.0)
                    continue;

                try
                {
                    var provider = forageProviders.ForageProvider(p);
                    if (provider == null || provider.ForageObj == null)
                        continue;

                    // compute available consumable (g/m2) on this provider
                    double total_consumable_gm2 = provider.ForageObj.Material.Sum(m => m.Consumable?.Wt ?? 0.0);
                    if (total_consumable_gm2 <= 0.0)
                        continue;

                    // desired removal expressed as g/m2 across the paddock area
                    double desired_removed_gm2 = providerRemovedKg * 1000.0 / area_m2;

                    // fraction of available consumable to remove (clamped 0..1)
                    double fraction = Math.Max(0.0, Math.Min(1.0, desired_removed_gm2 / total_consumable_gm2));

                    // call forage model RemoveBiomass - returns actual g/m2 removed
                    double actuallyRemoved_gm2 = provider.ForageObj.RemoveBiomass(liveToRemove: fraction, deadToRemove: fraction, liveToResidue: 0.0, deadToResidue: 0.0);

                    // convert to kg removed over paddock area
                    double actualRemovedKg = actuallyRemoved_gm2 * area_m2 / 1000.0;

                    // update running consumed total
                    biomassConsumed += actualRemovedKg;

                    // report per-forage transaction
                    ReportTransaction(TransactionType.Loss,
                                      actualRemovedKg,
                                      request?.ActivityModel,
                                      request?.RelatesToResource,
                                      request?.Category,
                                      this);
                }
                catch (Exception ex)
                {
                    Summary.WriteMessage(this, $"Error applying forage removal for provider index {p}: {ex.Message}", MessageType.Error);
                }
            }
        }

        /// <inheritdoc/>>
        public void ApplyDailyIntakeReduction(double fractionReduced)
        {
            // This scales:
            // - the prepared provided amount on the stored daily request,
            // - the single paddock packet amount (if present),
            // - any FoodResourcePacket.Amount entries attached to the daily request,
            // - the per class and per seed daily removal arrays.
            // Call this from the grazing activity after calling Remove(...) and before the daily
            // __CLEMDailyASPIMForageTake__ event fires when you need to apply quality/RDP reductions.

            double fractionRemaining = 1.0 - fractionReduced;

            if (fractionRemaining <= 0.0 && fractionRemaining < 1.0)
                throw new ArgumentOutOfRangeException(nameof(fractionRemaining), "Fraction must be between 0 and 1.");

            if (dailyIntakeRequest == null)
                return;

            // Scale provided amount on stored daily request (what will be handed to animals)
            dailyIntakeRequest.Provided *= fractionRemaining;

            // Scale the combined paddock packet if present
            if (dailyPaddockPacket != null)
                dailyPaddockPacket.Amount *= fractionRemaining;

            // Scale any packets attached to the request (defensive: some callers expect AdditionalData list)
            if (dailyIntakeRequest.AdditionalDetails is List<FoodResourcePacket> packets)
            {
                foreach (var p in packets)
                    p.Amount *= fractionRemaining;
            }

            // Scale per-class removal array (kg)
            if (dailyRemovalByClass != null && dailyRemovalByClass.Length > 0)
            {
                for (int i = 0; i < dailyRemovalByClass.Length; i++)
                    dailyRemovalByClass[i] *= fractionRemaining;
            }

            // Scale per-seed removal 2D array (kg)
            if (dailyRemovalSeed != null && dailyRemovalSeed.Length > 0)
            {
                int dim0 = dailyRemovalSeed.GetLength(0);
                int dim1 = dailyRemovalSeed.GetLength(1);
                for (int i = 0; i < dim0; i++)
                    for (int j = 0; j < dim1; j++)
                        dailyRemovalSeed[i, j] *= fractionRemaining;
            }
        }

        /// <inheritdoc/>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            throw new NotImplementedException("Biomass cannot be added to a linked APSIM paddock");
        }

        /// <inheritdoc/>
        public double Remove(double removeAmount, string activityName, string reason)
        {
            throw new NotImplementedException("Biomass cannot be removed from a linked APSIM paddock");
        }

        /// <inheritdoc/>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException("Cannot modify state of linked APSIM paddock");
        }

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (forages is null)
            {
                yield return new ValidationResult($"Could not find a Forages component in scope.", new string[] { "APSIM Forages component not found" });
            }
            if (paddock is null)
            {
                yield return new ValidationResult($"Could not find a Paddock (Zone) component named [{PaddockName}] in scope.", new string[] { "APSIM Paddock component not found" });
            }
        }
    }
}