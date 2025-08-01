using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>Labour feed to specified targets activity</summary>
    /// <summary>This activity provides food to people from the whole available human food store based on defined nutritional targets</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform human (labour) feeding based on nutritional quality and desired daily targets. This also includes sales of excess food.")]
    [Version(1, 0, 2, "This version implements the latest approach as outlines in the help system. Suitable for Ethiopian food security project")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedToTargets.htm")]
    public class LabourActivityFeedToTargets: CLEMActivityBase, IValidatableObject
    {
        private Labour people = null;
        private HumanFoodStore food = null;
        private FinanceType bankAccount;
        private ResourcesHolder resourcesHolder = null;

        [Link]
        private IClock clock = null;

        /// <summary>
        /// Feed hired labour as well as household
        /// </summary>
        [Description("Include hired labour")]
        public bool IncludeHiredLabour { get; set; }

        /// <summary>
        /// Daily intake limit
        /// </summary>
        [Description("Daily intake limit")]
        [Units("kg/AE/day")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Daily intake limit required"), GreaterThanValue(0)]
        public double DailyIntakeLimit { get; set; }

        /// <summary>
        /// Daily intake from sources other than modelled in Human Food Store
        /// </summary>
        [Description("Intake from sources not modelled")]
        [Units("kg/AE/day")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Intake from sources not modelled required"), GreaterThanEqualValue(0)]
        public double DailyIntakeOtherSources { get; set; }

        /// <summary>
        /// Undertake managed sales to reserve level
        /// </summary>
        [Description("Sell excess")]
        public bool SellExcess { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance), "Not provided" } })]
        public string AccountName { get; set; }

        /// <summary>
        /// Storage reserves to maintain before sales
        /// </summary>
        [Description("Months storage for reserves")]
        [Units("months")]
        [GreaterThanEqualValue(0)]
        public int MonthsStorage { get; set; }

        /// <summary>
        /// Name of market if present.
        /// </summary>
        [JsonIgnore]
        public Market Market { get; private set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            people = Resources.FindResourceGroup<Labour>();
            food = Resources.FindResourceGroup<HumanFoodStore>();
            bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);

            Market = food.FindAncestor<ResourcesHolder>().FoundMarket;

            resourcesHolder = base.Resources;
            // if market is present point to market to find the resource
            if (Market != null)
                resourcesHolder = Structure.FindChild<ResourcesHolder>(relativeTo: Market);

            // set the food store linked in any TargetPurchase if target proportion set > 0
            // check that all purchase resources have transmutation or recalulate the proportion
            var targetPurchases = this.FindAllChildren<LabourActivityFeedTargetPurchase>().Where(a => a.TargetProportion > 0).ToList();
            if (targetPurchases.Any())
            {
                double checkPropAvailable = 0;
                double totPropAvailable = 0;
                bool adjusted = false;
                foreach (var item in targetPurchases)
                {
                    checkPropAvailable += item.TargetProportion;
                    item.FoodStore = resourcesHolder.FindResourceType<HumanFoodStore, HumanFoodStoreType>(this, item.FoodStoreName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
                    if (item.FoodStore.TransmutationDefined)
                    {
                        totPropAvailable += item.TargetProportion;
                        item.ProportionToPurchase = item.TargetProportion;
                    }
                    else
                    {
                        string warn = $"The HumanFoodStoreType [r={item.FoodStore.FullPath}] does not have a Transmutation required to be a LabourActivityFeedTargetPurchase [a={item.FullPath}] of [a={this.FullPath}]{Environment.NewLine}This HumanFoodStore will not be allocated and the remaining purchase proportions have been adjusted";
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                        adjusted = true;
                        item.ProportionToPurchase = 0;
                    }
                }

                if(Math.Abs(1 - checkPropAvailable) < 0.0001)
                {
                    if (!adjusted)
                    {
                        string warn = $"The TargetProportions provided for [a=LabourActivityFeedTargetPurchase] provided for [a={this.FullPath}] do not sum to 1.{Environment.NewLine}These purchase proportions have been adjusted";
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                    }
                    adjusted = true;
                }

                // recalculate proportions to buy based on transmuation of resource allowed
                if (adjusted)
                    foreach (var item in targetPurchases.Where(a => a.FoodStore.TransmutationDefined))
                        item.ProportionToPurchase = item.TargetProportion / totPropAvailable;
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            if (people is null | food is null)
            {
                return null;
            }

            List<LabourType> peopleList = people.Items.Where(a => IncludeHiredLabour || a.Hired == false).ToList();
            peopleList.Select(a => { a.FeedToTargetIntake = 0; return a; }).ToList();

            // determine AEs to be fed
            double aE = peopleList.Sum(a => a.TotalAdultEquivalents);

            if(aE <= 0)
            {
                return null;
            }

            int daysInMonth = DateTime.DaysInMonth(clock.Today.Year, clock.Today.Month);

            // determine feed limits (max kg per AE per day * AEs * days)
            double intakeLimit = DailyIntakeLimit * aE * daysInMonth;

            // remove previous consumption
            double otherIntake = this.DailyIntakeOtherSources * aE * daysInMonth;
            otherIntake += peopleList.Sum(a => a.GetAmountConsumed());

            List<LabourActivityFeedTarget> labourActivityFeedTargets = this.FindAllChildren<LabourActivityFeedTarget>().ToList();

            // determine targets
            foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
            {
                // calculate target
                target.Target = target.TargetValue * aE * daysInMonth;

                // calculate target maximum
                target.TargetMaximum = target.TargetMaximumValue * aE * daysInMonth;

                // set initial level based on off store inputs
                target.CurrentAchieved = target.OtherSourcesValue * aE * daysInMonth;

                // calculate current level from previous intake this month (LabourActivityFeed)
                target.CurrentAchieved += people.GetDietaryValue(target.Metric, IncludeHiredLabour, false); // * aE; // * daysInMonth;

                // add sources outside of this activity to peoples' diets
                if (target.OtherSourcesValue > 0)
                {
                    foreach (var person in peopleList)
                    {
                        LabourDietComponent outsideEat = new LabourDietComponent();
                        // TODO: might need to add consumed here
                        outsideEat.AmountConsumed = this.DailyIntakeOtherSources * person.TotalAdultEquivalents * daysInMonth;
                        outsideEat.AddOtherSource(target.Metric, target.OtherSourcesValue * person.TotalAdultEquivalents * daysInMonth);
                        // track this consumption by people here.
                        person.AddIntake(outsideEat);
                        person.FeedToTargetIntake += outsideEat.AmountConsumed;
                    }
                }
            }

            // get max months before spoiling of all food stored (will be zero for non perishable food)
            int maxFoodAge = food.FindAllChildren<HumanFoodStoreType>().Max(a => a.Pools.Select(b => a.UseByAge - b.Age).DefaultIfEmpty(0).Max());

            // create list of all food parcels
            List<HumanFoodParcel> foodParcels = new List<HumanFoodParcel>();

            foreach (HumanFoodStoreType foodStore in food.FindAllChildren<HumanFoodStoreType>().ToList())
            {
                foreach (HumanFoodStorePool pool in foodStore.Pools.Where(a => a.Amount > 0))
                {
                    foodParcels.Add(new HumanFoodParcel()
                    {
                        FoodStore = foodStore,
                        Pool = pool,
                        Expires = ((foodStore.UseByAge == 0) ? maxFoodAge+1: foodStore.UseByAge - pool.Age)
                    });
                }
            }

            foodParcels = foodParcels.OrderBy(a => a.Expires).ToList();

            // if a market exists add the available market produce to the list below that ordered above.
            // order market food by price ascending
            // this will include market available food in the decisions.
            // will need to purchase this food before taking it if cost associated.
            // We can check if the parent of the human food store used is a market and charge accordingly.

            // for each market
            List<HumanFoodParcel> marketFoodParcels = new List<HumanFoodParcel>();
            ResourcesHolder resources = Market?.Resources;
            if (resources != null)
            {
                HumanFoodStore food = resources.FindResourceGroup<HumanFoodStore>();
                if (food != null)
                {
                    foreach (HumanFoodStoreType foodStore in food.FindAllChildren<HumanFoodStoreType>())
                    {
                        foreach (HumanFoodStorePool pool in foodStore.Pools.Where(a => a.Amount > 0))
                        {
                            marketFoodParcels.Add(new HumanFoodParcel()
                            {
                                FoodStore = foodStore,
                                Pool = pool,
                                Expires = ((foodStore.UseByAge == 0) ? maxFoodAge + 1 : foodStore.UseByAge - pool.Age)
                            });
                        }
                    }
                }
            }
            foodParcels.AddRange(marketFoodParcels.OrderBy(a => a.FoodStore.Price(PurchaseOrSalePricingStyleType.Purchase).PricePerPacket));

            double fundsAvailable = double.PositiveInfinity;
            if (bankAccount != null)
            {
                fundsAvailable = bankAccount.FundsAvailable;
            }

            int parcelIndex = 0;
            double metricneeded = 0;
            double intake = otherIntake;
            // start eating food from list from that about to expire first

            // food from household can be eaten up to target maximum
            // food from market can only be eaten up to target

            while(parcelIndex < foodParcels.Count)
            {
                foodParcels[parcelIndex].Proportion = 0;
                var isHousehold = foodParcels[parcelIndex].FoodStore.CLEMParentName == this.CLEMParentName;
                if (intake < intakeLimit & (labourActivityFeedTargets.Where(a => ((isHousehold)? !a.TargetMaximumAchieved: !a.TargetAchieved)).Count() > 0 | foodParcels[parcelIndex].Expires == 0))
                {
                    // still able to eat and target not met or food about to expire this timestep
                    // reduce by amout that can be eaten
                    double propCanBeEaten = Math.Min(1, (intakeLimit - intake) / (foodParcels[parcelIndex].FoodStore.EdibleProportion * foodParcels[parcelIndex].Pool.Amount));
                    // reduce to target limits
                    double propToTarget = 1;
                    if (foodParcels[parcelIndex].Expires != 0)
                    {
                        // if the food is not going to spoil
                        // then adjust what can be eaten up to target otherwise allow over target consumption to avoid waste

                        LabourActivityFeedTarget targetUnfilled = labourActivityFeedTargets.Where(a => ((isHousehold) ? !a.TargetMaximumAchieved : !a.TargetAchieved)).FirstOrDefault();
                        if (targetUnfilled != null)
                        {
                            // calculate reduction to metric target
                            metricneeded = Math.Max(0, (isHousehold ? targetUnfilled.TargetMaximum : targetUnfilled.Target) - targetUnfilled.CurrentAchieved);
                            double amountneeded = metricneeded / foodParcels[parcelIndex].FoodStore.ConversionFactor(targetUnfilled.Metric);

                            propToTarget = Math.Min(1, amountneeded / (foodParcels[parcelIndex].FoodStore.EdibleProportion * foodParcels[parcelIndex].Pool.Amount));
                        }
                    }

                    foodParcels[parcelIndex].Proportion = Math.Min(propCanBeEaten, propToTarget);

                    // work out if there will be a cost limitation, only if a price structure exists for the resource
                    // no charge for household consumption
                    double propToPrice = 1;
                    if (!isHousehold && foodParcels[parcelIndex].FoodStore.PricingExists(PurchaseOrSalePricingStyleType.Purchase))
                    {
                        ResourcePricing price = foodParcels[parcelIndex].FoodStore.Price(PurchaseOrSalePricingStyleType.Purchase);
                        double cost = (foodParcels[parcelIndex].Pool.Amount * foodParcels[parcelIndex].Proportion) / price.PacketSize * price.PricePerPacket;

                        // TODO: sell cattle based on selling groups till run out of cattle or meet shortfall
                        // adjust fundsAvailable with new money
                        // if cost > 0 and cost > funds available

                        if (cost > 0)
                        {
                            propToPrice = Math.Min(1, fundsAvailable / cost);
                            // remove cost from running check tally
                            fundsAvailable = Math.Max(0, fundsAvailable - (cost * propToPrice));

                            // real finance transactions will happen in the do activity as stuff is allocated
                            // there should not be shortfall as all the checks and reductions have happened here
                        }
                    }
                    foodParcels[parcelIndex].Proportion *= propToPrice;

                    // update intake
                    double newIntake = (foodParcels[parcelIndex].FoodStore.EdibleProportion * foodParcels[parcelIndex].Pool.Amount * foodParcels[parcelIndex].Proportion);
                    intake += newIntake;
                    // update metrics
                    foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
                        target.CurrentAchieved += newIntake * foodParcels[parcelIndex].FoodStore.ConversionFactor(target.Metric);
                }
                else if (intake >= intakeLimit && labourActivityFeedTargets.Where(a => ((isHousehold) ? !a.TargetMaximumAchieved : !a.TargetAchieved)).Count() > 1)
                {
                    // full but could still reach target with some substitution
                    // but can substitute to remove a previous target

                    // does the current parcel have better target values than any previous non age 0 pool of a different food type

                }
                else
                    break;
                parcelIndex++;
            }

            // fill resource requests
            List<ResourceRequest> requests = new List<ResourceRequest>();
            foreach (var item in foodParcels.GroupBy(a => a.FoodStore))
            {
                double amount = item.Sum(a => a.Pool.Amount * a.Proportion);
                if (amount > 0)
                {
                    double financeLimit = 1;
                    // if obtained from the market make financial transaction before taking
                    ResourcePricing price = item.Key.Price(PurchaseOrSalePricingStyleType.Sale);
                    bool marketIsSource = item.Key.Parent.Parent.Parent == Market;
                    if (bankAccount != null && marketIsSource && price.PricePerPacket > 0)
                    {
                        // finance transaction to buy food from market
                        ResourceRequest marketRequest = new ResourceRequest
                        {
                            ActivityModel = this,
                            Required = amount / price.PacketSize * price.PricePerPacket,
                            AllowTransmutation = false,
                            Category = $"{TransactionCategory}.PurchaseFood",
                            MarketTransactionMultiplier = 1,
                            RelatesToResource = item.Key.NameWithParent
                        };
                        bankAccount.Remove(marketRequest);
                    }

                    // is this a market

                    requests.Add(new ResourceRequest()
                    {
                        Resource = item.Key,
                        ResourceType = typeof(HumanFoodStore),
                        AllowTransmutation = false,
                        Required = amount * financeLimit,
                        ResourceTypeName = item.Key.NameWithParent,
                        ActivityModel = this,
                        Category = $"{TransactionCategory}{(marketIsSource?".FromMarket":".FromHousehold")}"
                    });
                }
            }

            // if still hungry and funds available, try buy food in excess of what stores (private or market) offered using transmutation if present.
            // This will force the market or private sources to purchase more food to meet demand if transmutation available.
            // if no market is present it will look to transmutating from its own stores if possible.
            // this means that other than a purchase from market (above) this activity doesn't need to worry about financial tranactions.
            int testType = 0;
            // test is limited to 1 for now so only to metric target NOT intake limit as we use maximum and target values now
            while (testType < 1 && intake < intakeLimit && (labourActivityFeedTargets.Where(a => !a.TargetAchieved).Any()) && fundsAvailable > 0)
            {
                // don't worry about money anymore. The over request will be handled by the transmutation.
                // move through specified purchase list
                // 1. to assign based on energy
                // 2. if still need food assign based on intake still needed

                metricneeded = 0;
                LabourActivityFeedTarget targetUnfilled = labourActivityFeedTargets.Where(a => !a.TargetAchieved).FirstOrDefault();
                if (targetUnfilled != null)
                {
                    metricneeded = Math.Max(0, (targetUnfilled.Target - targetUnfilled.CurrentAchieved));
                    double amountToFull = intakeLimit - intake;

                    foreach (LabourActivityFeedTargetPurchase purchase in this.FindAllChildren<LabourActivityFeedTargetPurchase>())
                    {
                        HumanFoodStoreType foodtype = purchase.FoodStore;
                        if (purchase.ProportionToPurchase > 0 && foodtype != null && (foodtype.TransmutationDefined & intake < intakeLimit))
                        {
                            double amountEaten = 0;
                            if (testType == 0)
                                // metric target based on purchase proportion
                                amountEaten = metricneeded / foodtype.ConversionFactor(targetUnfilled.Metric) * purchase.ProportionToPurchase;
                            else
                                // amount to satisfy limited by proportion of purchases
                                amountEaten = amountToFull * purchase.ProportionToPurchase;

                            if (intake + amountEaten > intakeLimit)
                                amountEaten = intakeLimit - intake;

                            if (amountEaten > 0)
                            {
                                targetUnfilled.CurrentAchieved += amountEaten * foodtype.ConversionFactor(targetUnfilled.Metric);
                                double amountPurchased = amountEaten / foodtype.EdibleProportion;

                                // update intake.. needed is the amount edible, not the amount purchased.
                                intake += amountEaten;

                                // add financial transactions to purchase from market
                                // if obtained from the market make financial transaction before taking
                                ResourcePricing price = foodtype.Price(PurchaseOrSalePricingStyleType.Sale);
                                if (bankAccount != null)
                                {
                                    if (price.PricePerPacket > 0)
                                    {
                                        ResourceRequest marketRequest = new ResourceRequest
                                        {
                                            ActivityModel = this,
                                            Required = amountPurchased / price.PacketSize * price.PricePerPacket,
                                            AllowTransmutation = false,
                                            Category = "Import",
                                            MarketTransactionMultiplier = 1,
                                            RelatesToResource = foodtype.NameWithParent
                                        };
                                        bankAccount.Remove(marketRequest);
                                    }
                                    else
                                    {
                                        string warn = $"No price set [{price.PricePerPacket}] for [r={foodtype.Name}] at time of transaction for [a={this.Name}]{Environment.NewLine}No financial transactions will occur.{Environment.NewLine}Ensure price is set or resource pricing file contains entries before this transaction or start of simulation.";
                                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                                    }
                                }

                                // find in requests or create a new one
                                ResourceRequest foodRequestFound = requests.Find(a => a.Resource == foodtype);
                                if (foodRequestFound is null)
                                    requests.Add(new ResourceRequest()
                                    {
                                        Resource = foodtype,
                                        ResourceType = typeof(HumanFoodStore),
                                        AllowTransmutation = true,
                                        Required = amountPurchased,
                                        ResourceTypeName = purchase.FoodStoreName,
                                        ActivityModel = this,
                                        Category = $"{TransactionCategory}.FromImports"
                                    });
                                else
                                {
                                    foodRequestFound.Required += amountPurchased;
                                    foodRequestFound.AllowTransmutation = true;
                                }
                            }
                        }
                    }
                    testType++;
                }
            }
            return requests;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            // add all provided requests to the individuals intake pools.

            List<LabourType> group = people?.Items.Where(a => IncludeHiredLabour | a.Hired != true).ToList();
            Status = ActivityStatus.NotNeeded;
            if (group != null && group.Count > 0)
            {
                var requests = ResourceRequestList.Where(a => a.ResourceType == typeof(HumanFoodStore));
                if (requests.Any())
                {
                    double aE = group.Sum(a => a.TotalAdultEquivalents);
                    foreach (ResourceRequest request in requests.Where(a => a.Provided > 0))
                        // add to individual intake
                        foreach (LabourType labour in group)
                        {
                            double amount = request.Provided * (labour.TotalAdultEquivalents / aE);
                            labour.AddIntake(new LabourDietComponent()
                            {
                                AmountConsumed = amount,
                                FoodStore = request.Resource as HumanFoodStoreType
                            });
                            labour.FeedToTargetIntake += amount;
                        }
                }
                if (this.FindAllChildren<LabourActivityFeedTarget>().Where(a => !a.TargetAchieved).Any())
                    this.Status = ActivityStatus.Partial;
                else
                    this.Status = ActivityStatus.Success;
            }
            // finished eating, so this household is now free to sell the resources
            // assumes all households above in the tree supply this level.
            // if sibling above relies on food from this household it own't work
            // selling is perfomed in the next method called in this same event

        }

        /// <summary>An event handler to allow us to sell excess resources.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMGetResourcesRequired")]
        private void OnCLEMAnimalSell(object sender, EventArgs e)
        {
            // Sell excess above store reserve level calculated from AE and daily target of first feed target
            // Performed here so all activities have access to human food stores before being sold.

            if (SellExcess && TimingOK)
            {
                // only uses the first target metric as measure
                double[] stored = new double[MonthsStorage + 1];
                double[] target = new double[MonthsStorage + 1];
                int[] daysInMonth = new int[MonthsStorage + 1];

                // determine AEs to be fed - NOTE does not account for aging in reserve calcualtion
                double aE = people.AdultEquivalents(IncludeHiredLabour);

                LabourActivityFeedTarget feedTarget = this.FindAllChildren<LabourActivityFeedTarget>().FirstOrDefault();

                // amount to store
                //double amountToStore = daysInMonth[i] * aE * (feedTarget.TargetValue - feedTarget.OtherSourcesValue);

                for (int i = 1; i <= MonthsStorage; i++)
                {
                    DateTime month = clock.Today.AddMonths(i);
                    daysInMonth[i] = DateTime.DaysInMonth(month.Year, month.Month);
                    target[i] = daysInMonth[i] * aE * (feedTarget.TargetMaximumValue - feedTarget.OtherSourcesValue);
                }

                double amountStored = 0; // reset here to make store based on all food types

                foreach (HumanFoodStoreType foodStore in food.FindAllChildren<HumanFoodStoreType>().ToList())
                {
                    // double amountStored = 0; reset here to make store based on each food type
                    double amountAvailable = foodStore.Pools.Sum(a => a.Amount);

                    if (amountAvailable > 0)
                    {
                        foreach (HumanFoodStorePool pool in foodStore.Pools.OrderBy(a => ((foodStore.UseByAge == 0) ? MonthsStorage : a.Age)))
                        {
                            if (foodStore.UseByAge != 0 && pool.Age == foodStore.UseByAge)
                                // don't sell food expiring this month as spoiled
                                amountStored += pool.Amount;
                            else
                            {
                                int currentMonth = ((foodStore.UseByAge == 0) ? MonthsStorage : foodStore.UseByAge - pool.Age + 1);
                                double poolRemaining = pool.Amount;
                                while (currentMonth > 0)
                                {
                                    if (stored[currentMonth] < target[currentMonth])
                                    {
                                        // place amount in store
                                        double amountNeeded = target[currentMonth] - stored[currentMonth];
                                        double towardTarget = pool.Amount * foodStore.EdibleProportion * foodStore.ConversionFactor(feedTarget.Metric);
                                        double amountSupplied = Math.Min(towardTarget, amountNeeded);
                                        double proportionProvided = amountSupplied / towardTarget;

                                        amountStored += pool.Amount * proportionProvided;
                                        poolRemaining -= pool.Amount * proportionProvided;
                                        stored[currentMonth] += amountSupplied;

                                        if (poolRemaining <= 0)
                                        {
                                            break;
                                        }
                                    }
                                    currentMonth--;
                                }
                            }
                        }

                        double amountSold = amountAvailable - amountStored;
                        if (amountSold > 0)
                        {
                            ResourcePricing priceToUse = new ResourcePricing()
                            {
                                PacketSize = 1
                            };
                            if(foodStore.PricingExists(PurchaseOrSalePricingStyleType.Purchase))
                                priceToUse = foodStore.Price(PurchaseOrSalePricingStyleType.Purchase);

                            double units = amountSold / priceToUse.PacketSize;
                            if (priceToUse.UseWholePackets)
                                units = Math.Truncate(units);

                            // remove resource
                            ResourceRequest purchaseRequest = new ResourceRequest
                            {
                                ActivityModel = this,
                                Required = units * priceToUse.PacketSize,
                                AllowTransmutation = false,
                                Category = $"{TransactionCategory}.SellToMarket",
                                RelatesToResource = foodStore.NameWithParent,
                                MarketTransactionMultiplier = this.FarmMultiplier
                            };
                            foodStore.Remove(purchaseRequest);

                            // transfer money earned
                            if (bankAccount != null)
                            {
                                ResourceRequest purchaseFinance = new ResourceRequest
                                {
                                    ActivityModel = this,
                                    Required = units * priceToUse.PacketSize,
                                    AllowTransmutation = false,
                                    Category = $"{TransactionCategory}.Sales",
                                    RelatesToResource = foodStore.NameWithParent,
                                    MarketTransactionMultiplier = this.FarmMultiplier
                                };
                                bankAccount.Add(purchaseFinance, this, foodStore.NameWithParent, TransactionCategory);
                            }
                        }
                    }
                }
            }
        }

        #region validation

        /// <summary>
        /// Validate component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // if finances and not account provided throw error
            if (SellExcess && Resources.FindResource<Finance>() != null)
            {
                if (bankAccount is null)
                {
                    string[] memberNames = new string[] { "AccountName" };
                    results.Add(new ValidationResult($"A valid bank account must be supplied as sales of excess food is enabled and [r=Finance] resources are available.", memberNames));
                }
            }

            if (Resources.FoundMarket != null & bankAccount is null)
            {
                string[] memberNames = new string[] { "AccountName" };
                results.Add(new ValidationResult($"A valid bank account must be supplied for purchases of food from the market used by [a=" + this.Name + "].", memberNames));
            }

            // check that at least one target has been provided.
            if (this.FindAllChildren<LabourActivityFeedTarget>().Count() == 0)
            {
                string[] memberNames = new string[] { "LabourActivityFeedToTargets" };
                results.Add(new ValidationResult(String.Format("At least one [LabourActivityFeedTarget] component is required below the feed activity [{0}]", this.Name), memberNames));
            }

            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<LabourActivityFeedTarget>(), true, "childgroupactivityborder", "The following targets are applied:", "No LabourActivityFeedTarget was provided"),
                (FindAllChildren<LabourActivityFeedTargetPurchase>(), true, "childgroupactivityborder", "The following purchases will be used to supply food:", "")
            };
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write($"Each Adult Equivalent is able to consume {CLEMModel.DisplaySummaryValueSnippet(DailyIntakeLimit, warnZero:true)} kg per day");
                if (DailyIntakeOtherSources > 0)
                {
                    htmlWriter.Write("with <span class=\"setvalue\">");
                    htmlWriter.Write(DailyIntakeOtherSources.ToString("#,##0.##"));
                    htmlWriter.Write("</span> provided from non-modelled sources");
                }
                htmlWriter.Write(".</div>");
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write("Hired labour <span class=\"setvalue\">" + ((IncludeHiredLabour) ? "is" : "is not") + "</span> included");
                htmlWriter.Write("</div>");

                // find a market place if present
                Simulation sim = FindAncestor<Simulation>();
                if (sim != null)
                {
                    Market marketPlace = Structure.FindChild<Market>(relativeTo: sim);
                    if (marketPlace != null)
                    {
                        htmlWriter.Write("<div class=\"activityentry\">");
                        htmlWriter.Write("Food with be bought and sold through the market <span class=\"setvalue\">" + marketPlace.Name + "</span>");
                        htmlWriter.Write("</div>");
                    }
                }
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
