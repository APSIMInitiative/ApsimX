using Models.Core;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;
using Models.CLEM.Groupings;
using Newtonsoft.Json;

namespace Models.CLEM.Activities
{
    /// <summary>Tracking settings for Ruminant purchases and sales</summary>
    /// <summary>If this model is provided within RuminantActivityBuySell, trucking costs and loading rules will occur</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
    [Description("Provides trucking settings for the purchase and sale of individuals with costs and emissions included")]
    [Version(1, 1, 1, "Release of trucking")]
    [HelpUri(@"Content/Features/Activities/Ruminant/Trucking.htm")]
    public class RuminantTrucking : CLEMRuminantActivityBase, IHandlesActivityCompanionModels, IActivityCompanionModel
    {
        private int numberToDo;
        private int parentNumberToDo;
        private IEnumerable<RuminantGroup> filterGroups;
        private List<Ruminant> individualsToBeTrucked;
        private RuminantActivityBuySell parentBuySellActivity;
        private (double trucks, double loadUnits, double vehicleMass, double payload, int individualsTransported) truckDetails;

        /// <inheritdoc/>
        [Category("General", "")]
        [Description("Purchase or sales identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
        public string Identifier { get; set; }

        /// <summary>
        /// Distance to market
        /// </summary>
        [Category("General", "")]
        [Description("Travel distance (km)")]
        [Required, GreaterThanValue(0)]
        public double DistanceToMarket { get; set; }

        /// <summary>
        /// Number animals per truck load
        /// </summary>
        [Category("Load rules", "")]
        [Description("Number of animals per load unit (deck/pod)")]
        [Required, GreaterThanValue(0)]
        public double NumberPerLoadUnit { get; set; }

        /// <summary>
        /// Minimum load units per truck
        /// </summary>
        [Category("Load rules", "")]
        [Description("Minimum load units (deck/pod) per truck (0 any)")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumLoadUnitsPerTruck { get; set; }

        /// <summary>
        /// Maximum load units per truck
        /// </summary>
        [Category("Load rules", "")]
        [Description("Maximum load units (deck/pod) per truck")]
        [Required, GreaterThanValue(0)]
        public double MaximumLoadUnitsPerTruck { get; set; }

        /// <summary>
        /// Load units per trailer 
        /// </summary>
        [Category("Load rules", "")]
        [Description("Load units (deck/pod) per trailer")]
        [Required, GreaterThanValue(0)]
        public double[] LoadUnitsPerTrailer { get; set; }

        /// <summary>
        /// Trailer mass + towball load units group size
        /// </summary>
        [Category("Vehicle mass", "")]
        [Description("Aggregate Trailer Mass (ATM)")]
        [Required, GreaterThanEqualValue(0)]
        public double[] AggregateTrailerMass { get; set; }

        /// <summary>
        /// Truck tare mass
        /// </summary>
        [Category("Vehicle mass", "")]
        [Description("Truck Tare Mass (avg fuel)")]
        [Required, GreaterThanEqualValue(0)]
        public double TruckTareMass { get; set; }

        /// <summary>
        /// Minimum number of load units before transporting (0 continuous)
        /// </summary>
        [Category("Load rules", "")]
        [Description("Minimum load units before transporting (0 continuous)")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumLoadUnitsBeforeTransporting { get; set; }

        /// <summary>
        /// Minimum number of load units before adding a trailer if available (0 continuous)
        /// </summary>
        [Category("Load rules", "")]
        [Description("Minimum load units before adding trailer (0 continuous)")]
        [Required, GreaterThanEqualValue(0)]
        public double[] MinimumLoadUnitsBeforeAddTrailer { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public string Measure { get; set; }

        // load unit by weight limit, floor area.
        // relationship to calculate proportion of load per individual
        // loading unit load limit (individuals, weight, floor area)
        [JsonIgnore]
        private Relationship weightToNumberPerLoadUnit { get; set; }

        /// <summary>
        /// The list of individuals remaining to be trucked in the current timestep and task (buy or sell)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Ruminant> IndividualsToBeTrucked { get { return individualsToBeTrucked; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantTrucking()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "Relationship":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>()
                        {
                            "Live weight to load unit size"
                        },
                        measures: new List<string>()
                        );
                case "RuminantGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>()
                        );
                case "ActivityFee":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "per truck",
                            "per km trucked",
                            "per load (deck/pod)",
                            "per tonne km",
                        }
                        );
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per truck",
                            "per km trucked",
                            "per load (deck/pod)",
                        }
                        );
                case "GreenhouseGasActivityEmission":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per truck",
                            "per km trucked",
                            "per tonne km",
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);
            filterGroups = GetCompanionModelsByIdentifier<RuminantGroup>(false, true);

            weightToNumberPerLoadUnit = GetCompanionModelsByIdentifier<Relationship>(false, false, "Live weight to load unit size")?.FirstOrDefault()??null;

            parentBuySellActivity = Parent as RuminantActivityBuySell;
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            ResourceRequestList.Clear();

            // walk through all trucking and reduce parent unique individuals where necessary
            numberToDo = 0;

            // number provided by the parent for trucking
            parentNumberToDo = parentBuySellActivity.IndividualsToBeTrucked?.Count() ?? 0;

            individualsToBeTrucked = GetUniqueIndividuals<Ruminant>(filterGroups.OfType<RuminantGroup>(), parentBuySellActivity.IndividualsToBeTrucked).ToList();
            numberToDo = individualsToBeTrucked?.Count() ?? 0;

            // work out how many can be trucked and return to parent of untrucked for next trucking settings if available.

            truckDetails = EstimateTrucking();

            foreach (var iChild in FindAllChildren<IActivityCompanionModel>().OfType<CLEMActivityBase>())
                iChild.Status = (Status == ActivityStatus.Skipped)? ActivityStatus.NotNeeded: ((parentNumberToDo > 0) ? ActivityStatus.NotNeeded : ActivityStatus.NoTask);
            //iChild.Status = (Status == ActivityStatus.Skipped) ? ActivityStatus.Skipped : ((parentNumberToDo > 0) ? ActivityStatus.NotNeeded : ActivityStatus.NoTask);

            parentBuySellActivity.IndividualsToBeTrucked = parentBuySellActivity.IndividualsToBeTrucked.Except(individualsToBeTrucked.Take(truckDetails.individualsTransported));

            foreach (var valueToSupply in valuesForCompanionModels.ToList())
            {
                int number = numberToDo;

                switch (valueToSupply.Key.type)
                {
                    case "RuminantFeedGroup":
                        valuesForCompanionModels[valueToSupply.Key] = 0;
                        break;
                    case "LabourRequirement":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForCompanionModels[valueToSupply.Key] = 1;
                                break;
                            case "per truck":
                                valuesForCompanionModels[valueToSupply.Key] = truckDetails.trucks;
                                break;
                            case "per km trucked":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket * truckDetails.trucks;
                                break;
                            case "per load (deck/pod)":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket * (truckDetails.payload + truckDetails.vehicleMass);
                                break;
                            default:
                                throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                    case "ActivityFee":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForCompanionModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                valuesForCompanionModels[valueToSupply.Key] = truckDetails.individualsTransported;
                                break;
                            case "per truck":
                                valuesForCompanionModels[valueToSupply.Key] = truckDetails.trucks;
                                break;
                            case "per km":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket;
                                break;
                            case "per km trucked":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket * truckDetails.trucks;
                                break;
                            case "per load (deck/pod)":
                                valuesForCompanionModels[valueToSupply.Key] = truckDetails.loadUnits;
                                break;
                            case "per tonne km":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket * (truckDetails.payload + truckDetails.vehicleMass);
                                break;
                            default:
                                throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                    case "GreenhouseGasActivityEmission":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForCompanionModels[valueToSupply.Key] = 1;
                                break;
                            case "per truck":
                                valuesForCompanionModels[valueToSupply.Key] = truckDetails.trucks;
                                break;
                            case "per km trucked":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket * truckDetails.trucks;
                                break;
                            case "per tonne km":
                                valuesForCompanionModels[valueToSupply.Key] = DistanceToMarket * (truckDetails.payload + truckDetails.vehicleMass);
                                break;
                            default:
                                throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                    default:
                        throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                }
            }
            return ResourceRequestList;
        }

        private (double trucks, double loadUnits, double vehicleMass, double payload, int individualsTransported) EstimateTrucking()
        {
            // start filling trucks with individuals until any limits met.

            bool loadingTruck = false;
            bool loadingTrailer = false;
            bool loadingUnit = false;

            double vehicleMass = 0;
            double totalUnits = 0;
            double unitLoad = 0;
            double individualContribution = 1 / NumberPerLoadUnit;
            double payload = 0;

            int trucks = 0;
            int indCnt = 0;
            int loadCnt = 0;
            int truckLoadCnt = 0;
            int trailerCnt = 0;
            int trailerId = 0;

            if(!individualsToBeTrucked.Any())
                return (0, 0, 0, 0, 0);

            double loadsRemaining = individualsToBeTrucked.Count / NumberPerLoadUnit;
            if (weightToNumberPerLoadUnit != null)
                loadsRemaining = individualsToBeTrucked.Sum(a => 1 / weightToNumberPerLoadUnit.SolveY(a.Weight));

            if (MathUtilities.IsGreaterThanOrEqual(loadsRemaining, MinimumLoadUnitsBeforeTransporting))
            {
                while (indCnt < individualsToBeTrucked.Count)
                {
                    if (!loadingTruck)
                    {
                        if (MathUtilities.IsGreaterThan(loadsRemaining, MinimumLoadUnitsPerTruck) && MathUtilities.IsGreaterThan(loadsRemaining, MinimumLoadUnitsBeforeAddTrailer[0]))
                        {
                            trucks++;
                            loadingTruck = true;
                            loadingTrailer = false;
                            loadCnt = 0;
                            truckLoadCnt = 0;
                            trailerCnt = 0;
                            trailerId = 0;
                            vehicleMass += TruckTareMass;
                        }
                        else
                            break;
                    }
                    else
                    {
                        if (!loadingTrailer)
                        {
                            if (MathUtilities.IsGreaterThan(loadsRemaining, MinimumLoadUnitsBeforeAddTrailer[trailerId]))
                            {
                                if (truckLoadCnt == MaximumLoadUnitsPerTruck)
                                {
                                    loadingTruck = false;
                                }
                                else if (truckLoadCnt < MaximumLoadUnitsPerTruck) // LoadUnitsPerTrailer[trailerId])
                                {
                                    // add trailer
                                    trailerCnt++;
                                    trailerId = Math.Min(trailerCnt, LoadUnitsPerTrailer.Length - 1);
                                    loadingTrailer = true;
                                    loadingUnit = false;
                                    vehicleMass += AggregateTrailerMass[trailerId];
                                    loadCnt = 0;
                                }
                            }
                            else
                                break;
                        }
                        else
                        {
                            if (!loadingUnit)
                            {
                                if (MathUtilities.IsLessThan(loadCnt, LoadUnitsPerTrailer[trailerId]))
                                {
                                    loadingUnit = true;
                                    loadCnt++;
                                    truckLoadCnt++;
                                    unitLoad = 0;
                                }
                                else
                                {
                                    loadingTrailer = false;
                                }
                            }
                            else
                            {
                                if (weightToNumberPerLoadUnit != null)
                                    individualContribution = 1 / weightToNumberPerLoadUnit.SolveY(individualsToBeTrucked[indCnt].Weight);
                                if (MathUtilities.IsLessThanOrEqual(unitLoad + individualContribution, 1.0))
                                {
                                    unitLoad += individualContribution;
                                    totalUnits += individualContribution;
                                    payload += individualsToBeTrucked[indCnt].Weight;

                                    indCnt++;
                                }
                                else
                                {
                                    loadingUnit = false;
                                }
                            }
                        }
                    }
                }
            }

            if (numberToDo > indCnt)
            {
                individualsToBeTrucked = individualsToBeTrucked.Take(indCnt).ToList();

                if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                    throw new ApsimXException(this, $"Unable to truck all required individuals [a={NameWithParent}]{Environment.NewLine}Adjust trucking rules or set OnPartialResourcesAvailableAction to [UseResourcesAvailable]");
                else if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.SkipActivity && (indCnt == 0 || individualsToBeTrucked.Count > indCnt))
                {
                    AddStatusMessage("No individuals loaded");
                    Status = ActivityStatus.Skipped;
                    return (0, 0, 0, 0, 0);
                }
            }
            Status = ActivityStatus.NotNeeded;
            return (trucks, totalUnits, vehicleMass, payload, indCnt);
        }


        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (parentNumberToDo > 0)
            {
                if (Status == ActivityStatus.Skipped)
                {
                    return;
                }

                if (numberToDo == truckDetails.individualsTransported)
                    SetStatusSuccessOrPartial();
                else
                {
                    if (truckDetails.individualsTransported == 0)
                    {
                        this.Status = ActivityStatus.Warning;
                        AddStatusMessage("No individuals loaded");
                    }
                    else
                        this.Status = ActivityStatus.Partial;
                }
            }
            else 
                Status = ActivityStatus.NoTask;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">It is {CLEMModel.DisplaySummaryValueSnippet(DistanceToMarket.ToString("0.##"))} km to market</div>");

                htmlWriter.Write($"\r\n<div class=\"activityentry\">Each load unit (pod/deck) holds {CLEMModel.DisplaySummaryValueSnippet(NumberPerLoadUnit, warnZero:true)} head (of specified individuals)");
                htmlWriter.Write("</div>");

                htmlWriter.Write($"\r\n<div class=\"activityentry\">Each truck ");
                if (MinimumLoadUnitsPerTruck > 0)
                    htmlWriter.Write($" requires a minimum of {CLEMModel.DisplaySummaryValueSnippet(MinimumLoadUnitsPerTruck, warnZero: true)} load units and ");
                htmlWriter.Write($"has a maximum of {CLEMModel.DisplaySummaryValueSnippet(MinimumLoadUnitsPerTruck, warnZero: true)} load units permitted");
                if (MinimumLoadUnitsBeforeTransporting > 0)
                    htmlWriter.Write($" and requires at least {CLEMModel.DisplaySummaryValueSnippet(MinimumLoadUnitsBeforeTransporting, warnZero: true)} load units before transporting.");
                htmlWriter.Write("</div>");

                if(LoadUnitsPerTrailer.Count() > 1)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Trailers from first to last hold ");
                else
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">The trailer holds ");
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet<double>(LoadUnitsPerTrailer, warnZero: true)} load units");

                if (MinimumLoadUnitsBeforeAddTrailer.Max() > 0)
                    htmlWriter.Write($" and requires {CLEMModel.DisplaySummaryValueSnippet(MinimumLoadUnitsBeforeAddTrailer, warnZero: true)} load units before adding each trailer");
                htmlWriter.Write(".</div>");

                htmlWriter.Write($"\r\n<div class=\"activityentry\">Each truck has a Tare Mass (with average fuel) of {CLEMModel.DisplaySummaryValueSnippet(TruckTareMass.ToString("0.##"), warnZero: true)} kg ");
                htmlWriter.Write($"with an Aggregate Trailer Mass {CLEMModel.DisplaySummaryValueSnippet<double>(AggregateTrailerMass, warnZero:true)} (kg)");
                if (AggregateTrailerMass.Count() > 1)
                    htmlWriter.Write($" from first to last trailer");
                htmlWriter.Write("</div>");

                return htmlWriter.ToString(); 
            }
        }

        #endregion

    }
}
