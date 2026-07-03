using APSIM.Numerics;
using Docker.DotNet.Models;
using Microsoft.IdentityModel.Protocols;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant grazing activity</summary>
    /// <summary>
    /// This activity determines how a specified ruminant herd will graze a specified pasture
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs grazing of a specified herd and pasture (paddock)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGraze.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrazing) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantActivityGrazePastureHerd : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        private CLEMEvents events = null;
        private ResourceRequest pastureRequest = null;
        private double shortfallReportingCutoff = 0.01;
        private bool isStandAloneModel = true;
        private readonly string shortHerdName = "";
        private List<Ruminant> herdToFeed = null;
        private double[,] indRelativeDailyIntake;
        private double[] indDailyIntakeRemaining;
        private double[] indDailyGreenIntakeRemaining;
        private double currentHerdDemand = 0;
        private int currentHerdSize = 0;
        private GrazeFoodStoreAPSIMLink apsimLink = null;

        /// <summary>
        /// Number of hours grazed Based on 8 hour grazing days Could be modified to account for rain/heat walking to
        /// water etc.
        /// </summary>
        [Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; }

        /// <summary>
        /// Paddock or pasture to graze
        /// </summary>
        [Description("GrazeFoodStore/pasture to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze Food Store/pasture required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// Paddock (GrazeFoodStoreType) model used
        /// </summary>
        [JsonIgnore]
        public IGrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>
        /// Ruminant Type to graze
        /// </summary>
        [Description("Ruminant type to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Ruminant Type required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(RuminantHerd) } })]
        public string RuminantTypeName { get; set; }

        /// <summary>
        /// RuminantType model used
        /// </summary>
        [JsonIgnore]
        public RuminantType RuminantTypeModel { get; set; }

        /// <summary>
        /// The proportion of required graze that is available determined from parent activity arbitration
        /// </summary>
        [JsonIgnore]
        public double GrazingCompetitionLimiter { get; set; } = 1.0;

        /// <summary>
        /// The biomass of pasture per hectare at start of allocation
        /// </summary>
        [JsonIgnore]
        public double BiomassPerHectare { get; set; }

        /// <summary>
        /// Potential intake limiter based on the biomass of available pasture
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakePastureQualityLimiter { get; set; } = 1.0;

        /// <summary>
        /// Potential intake limiter based on the biomass of available pasture
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakePastureBiomassLimiter { get; set; } = 1.0;

        /// <summary>
        /// Potential intake limiter based on the proportion of 8 hours grazing allowed
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakeGrazingTimeLimiter { get; set; } = 1.0;

        /// <summary>
        /// Potential intake limiter based on the proportion of 8 hours grazing allowed
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakeShortfallLimiter { get; set; } = 1.0;

        /// <summary>
        /// Potential intake limiter based on the proportion of 8 hours grazing allowed
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakeProportionGreenLimit { get; set; } = 1.0;

        /// <summary>
        /// Potential intake limiter based on the proportion of 8 hours grazing allowed
        /// </summary>
        [JsonIgnore]
        public double CombinedLimiter { get { return PotentialIntakePastureBiomassLimiter * PotentialIntakeGrazingTimeLimiter * PotentialIntakeShortfallLimiter * PotentialIntakePastureQualityLimiter; } }

        /// <summary>
        /// The daily biomass of pasture desired (Potential intake) by the herd (kg)
        /// </summary>
        [JsonIgnore]
        public double DailyPastureDesired { get; set; }

        /// <summary>
        /// The daily biomass of pasture required (Accounting for pasture biomass limiter) by the herd (kg)
        /// </summary>
        [JsonIgnore]
        public double DailyPastureRequired { get; set; }

        /// <summary>
        /// The daily biomass of pasture required by the herd (kg) including shortfall and proportion green limiter
        /// </summary>
        [JsonIgnore]
        public double DailyPastureTaken { get; set; }

        /// <summary>
        /// Pools available grouped into classes form which individals feed in order
        /// </summary>
        [JsonIgnore]
        public List<FoodResourceStore> DigestiblePasturePoolGroups { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RuminantActivityGrazePastureHerd()
        {
        }

        /// <summary>
        /// Constructor using details from a GrazePasture activity
        /// </summary>
        public RuminantActivityGrazePastureHerd(RuminantActivityGrazePasture grazePasture, RuminantType herdType, string transactionCategory, Guid parentBasedUid)
        {
            shortHerdName = herdType.Name;
            RuminantTypeName = herdType.NameWithParent;
            GrazeFoodStoreTypeName = grazePasture.GrazeFoodStoreTypeName;
            HoursGrazed = grazePasture.HoursGrazed;
            Parent = grazePasture;
            Name = $"Graze_{grazePasture.GrazeFoodStoreModel.Name}_{herdType.Name}";
            OnPartialResourcesAvailableAction = grazePasture.OnPartialResourcesAvailableAction;
            TransactionCategory = transactionCategory;
            Status = ActivityStatus.NoTask;
            UniqueID = parentBasedUid;
            isStandAloneModel = false;
            Resources = grazePasture.Resources;
        }

        /// <summary>
        /// Dynamically create the filters needed to select ruminants for this pasture-herd combination created
        /// </summary>
        public void AddHerdLocationFilter()
        {
            // add ruminant activity filter group to ensure correct individuals are selected
            string location = GrazeFoodStoreModel.Name;
            if (location.Contains('.'))
            {
                location = location.Split('.')[1];
            }

            RuminantActivityGroup herdGroup = new()
            {
                Name = $"Filter_{location}_{shortHerdName}"
            };
            Structure.AddChild(herdGroup);

            herdGroup.Structure.AddChild(new FilterByProperty()
            {
                PropertyOfIndividual = "Location",
                Operator = System.Linq.Expressions.ExpressionType.Equal,
                Value = location,
                Parent = herdGroup,
                Name = "GrazeLocation"
            });
            herdGroup.Structure.AddChild(new FilterByProperty()
            {
                PropertyOfIndividual = "HerdName",
                Operator = System.Linq.Expressions.ExpressionType.Equal,
                Value = shortHerdName,
                Parent = herdGroup,
                Name = "GrazeHerd"
            });

            Links links = new();
            links.Resolve(herdGroup as IModel, true, recurse: false);
            // commencing event needed to wire up filter group
            var events = new Events(herdGroup);
            events.PublishToModelAndChildren("Commencing", new object[] { herdGroup, new EventArgs() });
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GrazeFoodStoreModel = Resources.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            apsimLink = GrazeFoodStoreModel as GrazeFoodStoreAPSIMLink;

            RuminantTypeModel = Resources.FindResourceType<RuminantHerd, RuminantType>(this, RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            shortfallReportingCutoff = Structure.Find<ReportResourceShortfalls>()?.PropPastureShortfallOfDesiredIntake ?? 0.02;

            HerdResource = Structure.Find<RuminantHerd>();

            AddHerdLocationFilter();

            InitialiseHerd(true, false);

            isStandAloneModel = Structure.FindParent<RuminantActivityGrazePasture>() is null;
        }

        /// <summary>An event handler to allow us to clear requests at start of month.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnStartOfTimeStep(object sender, EventArgs e)
        {
            ResourceRequestList.Clear();

            //TODO: add local hoursGrazed that is reset to user level each time step but can be reduced by other activities such as RuminantActivityMove, or ManageRuminants
            // this actually needs to be a property of each individual as we don't know the how management of an individual affects grazing time.
            // grazing competition should probably favour individuals with the lowest grazing time. maybe. 

            PotentialIntakeGrazingTimeLimiter = HoursGrazed / 8;
            PotentialIntakePastureBiomassLimiter = 1.0;
            PotentialIntakeShortfallLimiter = 1.0;
            PotentialIntakePastureQualityLimiter = 1.0;
            DailyPastureRequired = 0;
            DailyPastureDesired = 0;
            Status = ActivityStatus.NotNeeded;
        }

        /// <summary>
        /// Method to allow another activity to request the activity determines its resources
        /// </summary>
        public List<ResourceRequest> RequestDetermineResources()
        {
            return RequestResourcesForTimestep();
        }

        /// <summary>
        /// Caclulate total herd daily pasture required and initialise intake tracking arrays.
        /// </summary>
        /// <param name="greenAge"></param>
        public double CalculateDailyFeedRequirement(int greenAge)
        {
            herdToFeed = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm).ToList();
            currentHerdSize = herdToFeed.Count;
            if (herdToFeed.Count == 0)
            {
                return 0;
            }

            // CLEM concept not included in APSIM-AusFarm - reduce intake when pasture biomass becomes low to account for greater search time
            PotentialIntakePastureBiomassLimiter = 1 - Math.Round(Math.Exp(-RuminantTypeModel.Parameters.Grazing.IntakeCoefficientBiomass * GrazeFoodStoreModel.TonnesPerHectareStartOfTimeStep * 1000), 5);

            // calculate green limit for the breed
            double green = DigestiblePasturePoolGroups.Where(a => a.ProportionGreen == 1).Sum(a => a.Pools.Sum(p => p.AmountAvailable));
            double proportionGreen = green / DigestiblePasturePoolGroups.Sum(a => a.Pools.Sum(p => p.AmountAvailable));

            PotentialIntakeProportionGreenLimit = 1;
            if (proportionGreen < 0.9)
            {
                PotentialIntakeProportionGreenLimit = Math.Max(0.0, (RuminantTypeModel.Parameters.Grazing.GreenDietMax * 100) * (1 - Math.Exp(-RuminantTypeModel.Parameters.Grazing.GreenDietCoefficient * ((proportionGreen * 100) - (RuminantTypeModel.Parameters.Grazing.GreenDietZero * 100))))) / 100.0;
            }

            indRelativeDailyIntake = new double[currentHerdSize, DigestiblePasturePoolGroups.Count()];
            indDailyIntakeRemaining = new double[currentHerdSize];
            indDailyGreenIntakeRemaining = new double[currentHerdSize];
            for (int i = 0; i < currentHerdSize; i++)
            {
                // required is the smallest of time and low biomass-limited potential intake (expected) and the remaining intake needed for the individual
                // it is not the remaining intake that is adjusted by the PastureBiomassLimiter but the potential, otherwise we 
                indDailyIntakeRemaining[i] = Math.Min(herdToFeed[i].Intake.SolidsDaily.Expected * PotentialIntakeGrazingTimeLimiter * PotentialIntakePastureBiomassLimiter, herdToFeed[i].Intake.SolidsDaily.Required) ;
                DailyPastureDesired += herdToFeed[i].Intake.SolidsDaily.Expected * PotentialIntakeGrazingTimeLimiter * PotentialIntakePastureBiomassLimiter;
                DailyPastureRequired += indDailyIntakeRemaining[i];
                indDailyGreenIntakeRemaining[i] = indDailyIntakeRemaining[i] * PotentialIntakeProportionGreenLimit;
            }
            currentHerdDemand = DailyPastureRequired;
            return DailyPastureRequired;
        }

        /// <summary>
        /// Calculate relative intake as proportion of the individuals demand and current herd demand including green
        /// pasture limitations.
        /// </summary>
        /// <param name="group">Pasture pool group to consume</param>
        /// <param name="groupIndex">0 based index of the PoolGroup in list</param>
        /// <param name="imposeGreenLimit">Switch to determine if green limits are imposed</param>
        public double PrepareTakeFromGrazingPoolGroup(FoodResourceStore group, int groupIndex, bool imposeGreenLimit = true)
        {
            double sumRelIntake = 0;
            for (int i = 0; i < currentHerdSize; i++)
            {
                double amountToEat = indDailyIntakeRemaining[i];
                if (imposeGreenLimit && group.ProportionGreen > 0.9 && PotentialIntakeProportionGreenLimit < 1.0) // must be considered green pool and a green limiter calculated
                {
                    amountToEat = Math.Min(indDailyGreenIntakeRemaining[i], amountToEat);
                }
                indRelativeDailyIntake[i, groupIndex] = amountToEat/currentHerdDemand;
                sumRelIntake += indRelativeDailyIntake[i, groupIndex];
            }
            return sumRelIntake;
        }

        /// <summary>
        /// Method to take the relative intake and consider any pastureshortfall and competition modifiers
        /// </summary>
        /// <param name="group">Pasture pool group to consume</param>
        /// <param name="groupIndex">0 based index of pasture pool in list</param>
        /// <param name="shortfallMultiplier">Intake multiplier based resulting from any pasture shortfall</param>
        /// <param name="imposeGreenLimit">
        /// Switch to determine if green limits are imposed. Must be set if greenlimit was imposed in previous
        /// PrepareTakeFromGrazingPool
        /// </param>
        public void TakeFromGrazingPoolGroup(FoodResourceStore group, int groupIndex, double shortfallMultiplier, bool imposeGreenLimit = true)
        {
            DailyPastureTaken = 0;
            for (int i = 0; i < currentHerdSize; i++)
            {
                double amountToEat = currentHerdDemand * indRelativeDailyIntake[i, groupIndex] * shortfallMultiplier; // daily take
                if (imposeGreenLimit && group.ProportionGreen > 0.9 && PotentialIntakeProportionGreenLimit < 1.0)
                {
                    indDailyGreenIntakeRemaining[i] -= amountToEat;
                }

                // TODO: Any individual grazing time calculation needs to be added here rather than the assumed proportion of 8 hours for the entire herd
                var excess = herdToFeed[i].Intake.AddFeed(group, groupID: $"GrazePool_DMD{(int)group.Details.DryMatterDigestibility}", specifyAmount: amountToEat);

                amountToEat -= excess;
                indDailyIntakeRemaining[i] -= amountToEat; 
                if (excess > 0)
                {
                    throw new Exception($"Core development error: excess feed found in grazing [a={this.NameWithParent}] where this should not be possible");
                }
                DailyPastureTaken += amountToEat;
            }
            // add daily amount to details.amount and specifiy the total time step amount as pending in pools.
            group.Add(DailyPastureTaken);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            // this code performs all actions handled by the GrazePasture activity and is only used when this GrazePastureHerd activity has been provided alone 

            if (!isStandAloneModel)
                return null;

            ResourceRequestList.Clear();

            int greenAge = (events.Clock.Today.Month <= 3) ? 2 : 1;
            double totalNeededDaily = CalculateDailyFeedRequirement(greenAge);

            if (DigestiblePasturePoolGroups is null)
            {
                DigestiblePasturePoolGroups = GrazeFoodStoreModel.GenerateIntakeGroups(events.Interval, greenAge);
            }

            // TODO: check when SetCurrentBiomass needs to be called
            //GrazeFoodStoreModel.SetCurrentBiomass();

            // fill all animals by calculating relative fill for each pool group 
            for (int i = 0; i < DigestiblePasturePoolGroups.Count; i++)
            {
                double totalRequestedTimestep = PrepareTakeFromGrazingPoolGroup(DigestiblePasturePoolGroups[i], i);

                totalRequestedTimestep *= totalNeededDaily * events.Interval;

                // shortfall limiter
                double shortfallMultiplier = RuminantActivityGrazePasture.CalculateShortfallMultiplier(DigestiblePasturePoolGroups[i].Pools.Sum(a => a.AmountAvailable), [totalRequestedTimestep]);

                TakeFromGrazingPoolGroup(DigestiblePasturePoolGroups[i], i, shortfallMultiplier);
            }
            CreateResourceRequest();
            PotentialIntakeShortfallLimiter = CalculatePotentialShortfallLimiter();

            return ResourceRequestList;
        }

        /// <summary>
        /// Method to calculate the shortfall limiter based on the pasture required vs desired. This handles all the
        /// individual FoodResourceStore biomass limits applied
        /// </summary>
        /// <returns></returns>
        public double CalculatePotentialShortfallLimiter()
        {
            if (DailyPastureRequired == 0)
                return 1;
            return (DailyPastureRequired >= DailyPastureDesired) ? 1 - ((DailyPastureRequired - DailyPastureDesired) / DailyPastureRequired) : 1;
        }

        /// <summary>
        /// Method to create the resource request for pasture required for current herd.
        /// </summary>
        public void CreateResourceRequest()
        {
            double eaten = DigestiblePasturePoolGroups.Sum(a => a.Details.Amount);
            if (eaten <= 0)
                return;
            pastureRequest = new ResourceRequest()
            {
                AllowTransmutation = false,
                Required = eaten,
                Resource = GrazeFoodStoreModel,
                ResourceType = typeof(GrazeFoodStore),
                ResourceTypeName = GrazeFoodStoreModel.Name,
                ActivityModel = this,
                AdditionalDetails = DigestiblePasturePoolGroups,
                Category = "Grazed",
                RelatesToResource = PredictedHerdNameToDisplay,
                TransactionPending = true
            };
            ResourceRequestList.Add(pastureRequest);
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            // all pools have been eaten in RequestResourcesForTimestep for CLEM graze forage.
            // nothing to do. We will also send apsim forage DMD pools to the IEnumerable<GrazeFoodStorePoolGroup> 

            // ToDo: work out where and how to update grazePaddock.TimeStepForageConsumed.Amount
        }

        /// <summary>
        /// An event handler to allow final adjustment of intake based on diet quality and ensure this is accounted for
        /// in forage take and fertilisation by urine and dung.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPostRuminantConsumption")]
        private void OnAfterDietQualityDetermined(object sender, EventArgs e)
        {
            if(pastureRequest is null)
            {
                apsimLink?.SimpleGrazingModel.ProvideExternalLivestockNoGrazing();
                return;
            }

            // provided has been tracked by the pending amounts in each pasture pool group
            // the details.Amount from each resource group can't be used as they seem to have been reset.
            pastureRequest.Provided = Math.Min(pastureRequest.Required, DigestiblePasturePoolGroups.SelectMany(a => a.Pools).Sum(a => a.AmountPending));

            PotentialIntakePastureQualityLimiter = pastureRequest.Provided / pastureRequest.Required;
            Status = ActivityStatus.Success;

            if (apsimLink is not null) 
            {
                // set the amount to take from pasture if APSIMLink
                // allow these to set values even if pasture.provided = 0 which allows animals to be on pasture with 0 consumption (no pasture) but still urinate etc.
                apsimLink.SimpleGrazingModel?.ProvideExternalLivestockConsumption(apsimLink.ConvertToPerHaPerDay(pastureRequest.Provided, events.Interval));

                // set the urine returned to pasture
                double urineN = apsimLink.ConvertToPerHaPerDay(herdToFeed.Sum(a => a.Output.NitrogenUrine), events.Interval);
                double dung = apsimLink.ConvertToPerHaPerDay(herdToFeed.Sum(a => a.Output.Manure), events.Interval);
                double dungN = apsimLink.ConvertToPerHaPerDay(herdToFeed.Sum(a => a.Output.NitrogenFaecal), events.Interval);
                int numberOfUrinations = (int)Math.Ceiling(apsimLink.ConvertToPerHaPerDay(herdToFeed.Count * 5.0 * events.Interval, events.Interval)); // how do we estimate this. this assumes 5 per individual per day

                apsimLink.SimpleGrazingModel?.ProvideExternalLivestockInputs(urineN, dungN, dung, numberOfUrinations);
            }

            // report shortfalls based on multipliers.

            if (MathUtilities.IsGreaterThan(MathUtilities.PositiveDifference(DailyPastureDesired * events.Interval, pastureRequest.Provided), DailyPastureDesired * events.Interval * shortfallReportingCutoff))
            {
                ResourceRequest shortfallRequest = pastureRequest;
                shortfallRequest.Required = DailyPastureRequired * events.Interval;
                if (shortfallRequest is null)
                {
                    shortfallRequest = new ResourceRequest()
                    {
                        Available = pastureRequest.Provided, // display all that was given
                        Required = DailyPastureRequired * events.Interval,
                        ResourceType = typeof(GrazeFoodStore),
                        ResourceTypeName = GrazeFoodStoreModel.Name
                    };
                }
                if (MathUtilities.IsLessThan(PotentialIntakeShortfallLimiter, 1.0))
                {
                    Status = (pastureRequest.Provided == 0) ? ActivityStatus.Warning : ActivityStatus.Partial;
                    if (Status == ActivityStatus.Warning)
                    {
                        AddStatusMessage("No pasture");
                    }
                    // report desired to ignore the very low pasture biomass limiter that has been invoked with zero pasture.
                    shortfallRequest.Required = DailyPastureRequired * events.Interval;
                    shortfallRequest.ShortfallStatus = "BelowRequired";
                }
                else
                {
                    if (pastureRequest.Provided == 0)
                    {
                        AddStatusMessage("No pasture");
                        Status = ActivityStatus.Warning;
                    }
                    shortfallRequest.Required = DailyPastureDesired * events.Interval;
                    shortfallRequest.ShortfallStatus = "BelowDesired";
                }

                ActivitiesHolder.ReportActivityShortfall(new ResourceRequestEventArgs() { Request = shortfallRequest });

                // only allow the stop error if this is a shortfall in required not desired.
                if (MathUtilities.IsLessThan(Math.Round(GrazingCompetitionLimiter, 4), 1) && OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    throw new ApsimXException(this, $"Insufficient pasture available for grazing in paddock ({GrazeFoodStoreModel.Name}) in time step index: {events.IntervalIndex} ({events.Clock.Today:dd\\MM\\yyyy})");
                }
            }

            // todo: set the total consumed for this herd so easily reported.
            // the GrazeFoodStoreType will automatically report pending transactions at end of time step
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (GrazeFoodStoreTypeName.Contains("."))
            {
                ResourcesHolder resHolder = Structure.Find<ResourcesHolder>();
                if (resHolder is null || resHolder.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName) is null)
                {
                    yield return new ValidationResult($"The location defined for grazing [r={GrazeFoodStoreTypeName}] in [a={Name}] is not found.{Environment.NewLine}Ensure [r=GrazeFoodStore] is present and the [GrazeFoodStoreType] is present", new string[] { "Location is not valid" });
                }
            }
        }
        #endregion
    }
}
