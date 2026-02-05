using DeepCloner.Core;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using Models.ForageDigestibility;
using Models.GrazPlan;
using Models.Interfaces;
using Models.PMF.Struct;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a GrazeFoodType that links directly to an APSIM crop or pasture model for grazing
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [Description("This resource represents a link to an APSIM crop or pasture model")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreAPSIMLink.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrazing) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class GrazeFoodStoreAPSIMLink : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFeed, IGrazeFoodStoreType
    {
        private double biomassAddedThisYear;
        private double biomassConsumed;
        private Forages forages;
        private ForageProviders forageProviders = new();
        private PaddockInfo paddockInfo;
        private Zone paddock;

        /// <inheritdoc/>
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        [Required, GreaterThanValue(0)]
        [Description("Metabolisable energy content")]
        [Category("Farm", "Quality")]
        [Units("MJ/kg DM")]
        public double MetabolisableEnergyContent { get; set; } = 8.0;

        /// <inheritdoc/>
        public double NitrogenPercent { get; set; }

        private double rumenDegradableProteinPercent = 58;

        /// <inheritdoc/>
        [Required, Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Rumen degradable protein percent (%, g/g CP * 100)")]
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
        /// Style of providing the dry matter digestibility of pasture
        /// </summary>
        public DryMatterDigestibilityStyle DMDStyle { get; set; } = DryMatterDigestibilityStyle.EstimateFromNitrogenContent;

        /// <inheritdoc/>
        public double DryMatterDigestibility { get; set; }

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

        /// <inheritdoc/>
        public double AcidDetergentInsolubleProtein { get; set; }

        /// <inheritdoc/>
        public double CrudeProteinPercent { get; set; }

        /// <inheritdoc/>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Fat percent (ether extract) (%)")]
        public double FatPercent { get; set; } = 1.9;

        /// <summary>
        /// Value of gut fill for highest quality green pasture
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill high quality (Green DMD)")]
        public double GutFillHighQuality { get; set; } = 0.08;

        /// <summary>
        /// Value of gut fill for lowest quality cured pasture at min DMD
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill low quality (min DMD)")]
        public double GutFillLowQuality { get; set; } = 0.2;

        /// <inheritdoc/>
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

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
        public double Amount
        {
            get
            {
                return 0;  // ToDo: total standing biomass from model
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
                return Amount / paddockInfo.Area / 10_000.0;
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
        /// Get the new growth from the pasture model
        /// </summary>
        public void GetNewGrowth()
        {
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
            AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(RumenDegradableProteinPercent, TypeOfFeed);

            forages = Node.Find<Forages>();
            if (forages is null)
                Summary.WriteMessage(this, $"Could not find a Forages component in scope.", MessageType.Error);

            paddock = Node.Find<Zone>(name: PaddockName);
            if (paddock is null)
            {
                Summary.WriteMessage(this, $"Could not find a Paddock (Zone) component named [{PaddockName}] in scope.", MessageType.Error);
                return;
            }

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
        private void ONCLEMPastureReady(object sender, EventArgs e)
        {
            paddockInfo.ClearSupplement();
            paddockInfo.ZeroRemoval();

            // request available to ruminants (modified from Stock.RequestAvailableToAnimal())
            for (int i=0; i < forageProviders.Count(); i++)
            {
                var provider = forageProviders.ForageProvider(i);
                if (provider.ForageObj != null)
                {
                    provider.PastureGreenDM = provider.ForageObj.Material.Where(m => m.IsLive)
                                                                    .Sum(m => m.Consumable.Wt); // g/m^2
                    provider.UpdateForages(provider.ForageObj);
                }
            }

            // do not return zero as there is always something there and zero affects calculations.
            TonnesPerHectareStartOfTimeStep = Math.Max(TonnesPerHectare, 0.01);
        }

        #region transactions

        private void TakeResourceByGrazing(ref ResourceRequest request)
        {
            // take from pools as specified for the breed
            double amountRequired = request.Required;

            // determine the plant components to take from
            // know an amount needed
            // find APSIM plant component preferences

            // do need the current biomass again as previous breeds may have taken some.
            // this will only add CPU demand when multiple breeds present as appropriate

            // we can feed the animals each component with it's DMD, CP etc
            // or we can shandy it into a feedPacket and feed all at once
            // pasture could identify legume component


            request.Provided = request.Required - amountRequired;
            biomassConsumed += request.Provided;
        }

        /// <inheritdoc/>
        public new void Remove(ResourceRequest request)
        {
            // handles grazing by breed from the APSIM paddock with forage components

            if (request.AdditionalDetails is null || request.Required == 0)
                throw new Exception("Removing biomass from APSIM.Paddock requires AdditionalDetails property provided in resource request");

            switch (request.AdditionalDetails)
            {
                case RuminantActivityGrazePastureHerd grazingActivity2:
                    TakeResourceByGrazing(ref request);
                    break;
                case PastureActivityCutAndCarry:
                    break;
                case PastureActivityBurn:
                    break;
                default:
                    break;
            }
            ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
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

        #endregion

    }
}