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
using System.Text.Json.Serialization;
using Models.CLEM.Groupings;

namespace Models.CLEM.Activities
{
    /// <summary>Tracking settings for Ruminant purchases and sales</summary>
    /// <summary>If this model is provided within RuminantActivityBuySell, trucking costs and loading rules will occur</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
    [Description("Provides trucking settings for the purchase and sale of individuals with costs and emissions included")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/Trucking.htm")]
    public class TruckingSettings : CLEMRuminantActivityBase, ICanHandleIdentifiableChildModels, IIdentifiableChildModel
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
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
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
        [Description("Truck Tare Mass (half fuel)")]
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
        /// Minimum number of load units before transporting (0 continuous)
        /// </summary>
        [Category("Load rules", "")]
        [Description("Minimum load units before adding trailer (0 continuous)")]
        [Required, GreaterThanEqualValue(0)]
        public double[] MinimumLoadUnitsBeforeAddTrailer { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public string Units { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public bool ShortfallCanAffectParentActivity { get; set; }

        // load unit by weight limit, floor area.
        // relationship to calculate proportion of load per individual
        // loading unit load limit (individuals, weight, floor area)
        [JsonIgnore]
        private Relationship weightToNumberPerLoadUnit { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TruckingSettings()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            TransactionCategory = "Livestock.Trucking";
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>(),
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>(),
                        units: new List<string>() {
                            "fixed",
                            "per head",
                            "per truck",
                            "per km trucked",
                            "per loading unit",
                            "per tonne km",
                        }
                        );
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>(),
                        units: new List<string>() {
                            "fixed",
                            "per truck",
                            "per km trucked",
                            "per loading unit",
                        }
                        );
                case "GreenhouseGasActivityEmission":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>(),
                        units: new List<string>() {
                            "fixed",
                            "per truck",
                            "per km trucked",
                            "per tonne km",
                        }
                        );
                default:
                    return new LabelsForIdentifiableChildren();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);
            filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>(false, true);
            weightToNumberPerLoadUnit = this.FindAllChildren<Relationship>().FirstOrDefault();
            parentBuySellActivity = Parent as RuminantActivityBuySell;
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
            double individualContribution = 0;
            double payload = 0;

            int trucks = 0;
            int indCnt = 0;
            int loadCnt = 0;
            int trailerCnt = 0;
            int trailerId = 0;

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
                                if (loadCnt == MaximumLoadUnitsPerTruck)
                                {
                                    loadingTruck = false;
                                }
                                else if (loadCnt <= LoadUnitsPerTrailer[trailerId])
                                {
                                    // add trailer
                                    trailerCnt++;
                                    trailerId = Math.Min(trailerCnt, LoadUnitsPerTrailer.Length - 1);
                                    loadingTrailer = true;
                                    loadingUnit = false;
                                    vehicleMass += AggregateTrailerMass[trailerId];
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
                // need to fill to minimum loads on last truck if needed
                while(loadCnt < MinimumLoadUnitsPerTruck)
                {
                    loadCnt++;
                    if (loadCnt == LoadUnitsPerTrailer[trailerId])
                    {
                        // add trailer
                        trailerCnt++;
                        trailerId = Math.Min(trailerCnt, LoadUnitsPerTrailer.Length - 1);
                        vehicleMass += AggregateTrailerMass[trailerId];
                    }
                }
            }

            return (trucks, totalUnits, vehicleMass, payload, indCnt);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> DetermineResourcesForActivity(double argument = 0)
        {
            List<ResourceRequest> resourceRequests = new List<ResourceRequest>();

            // walk through all trucking and reduce parent unique individuals where necessary
            numberToDo = 0;

            // number provided by the parent for trucking
            parentNumberToDo = parentBuySellActivity.IndividualsToBeTrucked?.Count() ?? 0;

            individualsToBeTrucked = GetUniqueIndividuals<Ruminant>(filterGroups.OfType<RuminantGroup>(), parentBuySellActivity.IndividualsToBeTrucked).ToList();
            numberToDo = individualsToBeTrucked?.Count() ?? 0;

            // work out how many can be trucked and return to parent of untrucked for next trucking settings if available.

            truckDetails = EstimateTrucking();
            //parentBuySellActivity.IndividualsToBeTrucked = individualsToBeTrucked.Take(truckDetails.individualsTransported);

            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                int number = numberToDo;

                switch (valueToSupply.Key.type)
                {
                    case "RuminantFeedGroup":
                        valuesForIdentifiableModels[valueToSupply.Key] = 0;
                        break;
                    case "LabourGroup":
                    case "RuminantActivityFee":
                    case "GreenhouseGasActivityEmission":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForIdentifiableModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                valuesForIdentifiableModels[valueToSupply.Key] = truckDetails.individualsTransported;
                                break;
                            case "per truck":
                                valuesForIdentifiableModels[valueToSupply.Key] = truckDetails.trucks;
                                break;
                            case "per km":
                                valuesForIdentifiableModels[valueToSupply.Key] = DistanceToMarket;
                                break;
                            case "per km trucked":
                                valuesForIdentifiableModels[valueToSupply.Key] = DistanceToMarket * truckDetails.trucks;
                                break;
                            case "per loading unit":
                                valuesForIdentifiableModels[valueToSupply.Key] = truckDetails.loadUnits;
                                break;
                            case "per tonne km":
                                valuesForIdentifiableModels[valueToSupply.Key] = DistanceToMarket * (truckDetails.payload + truckDetails.vehicleMass);
                                break;
                            default:
                                throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                    default:
                        throw new NotImplementedException(UnknownIdentifierErrorText(this, valueToSupply.Key));
                }
            }

            // return any identifiable child results with this to be handled by parent as this too is an iidentifiable child.
            foreach (var iChild in FindAllChildren<IIdentifiableChildModel>())
            {
                resourceRequests.AddRange(iChild.DetermineResourcesForActivity(ValueForIdentifiableChild(iChild)));
            }
            return resourceRequests;
        }

        /// <inheritdoc/>
        public override void PerformTasksForActivity(double argument = 0)
        {
            if (parentNumberToDo > 0)
            {
                if (truckDetails.individualsTransported == 0)
                    Status = ActivityStatus.Warning;
                else
                {
                    if (numberToDo == truckDetails.individualsTransported)
                        SetStatusSuccessOrPartial();
                    else
                    {
                        if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                            throw new ApsimXException(this, $"Unable to truck all required individuals [a={NameWithParent}]{Environment.NewLine}Adjust trucking rules or set OnPartialResourcesAvailableAction to [UseResourcesAvailable]");
                        else
                            this.Status = ActivityStatus.Partial;
                    }
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">It is <span class=\"setvalue\">" + DistanceToMarket.ToString("0.##") + "</span> km to market");
                htmlWriter.Write("</div>");

                //htmlWriter.Write("\r\n<div class=\"activityentry\">Each truck load can carry ");
                //if (Number450kgPerTruck == 0)
                //    htmlWriter.Write("<span class=\"errorlink\">[NOT SET]</span>");
                //else
                //    htmlWriter.Write("<span class=\"setvalue\">" + Number450kgPerTruck.ToString("0.###") + "</span>");

                //htmlWriter.Write(" 450 kg individuals</div>");

                //if (MathUtilities.IsPositive(MinimumLoadBeforeSelling) || MathUtilities.IsPositive(MinimumTrucksBeforeSelling))
                //{
                //    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                //    if (MinimumTrucksBeforeSelling > 0)
                //        htmlWriter.Write("A minimum of <span class=\"setvalue\">" + MinimumTrucksBeforeSelling.ToString("###") + "</span> truck loads is required");

                //    if (MathUtilities.IsPositive(MinimumLoadBeforeSelling))
                //    {
                //        if (MathUtilities.IsPositive(MinimumTrucksBeforeSelling))
                //            htmlWriter.Write(" and each ");
                //        else
                //            htmlWriter.Write("Each ");

                //        htmlWriter.Write("truck must be at least <span class=\"setvalue\">" + MinimumLoadBeforeSelling.ToString("0.##%") + "</span> full");
                //    }
                //    htmlWriter.Write(" for sales</div>");
                //}

                //if (MathUtilities.IsPositive(MinimumLoadBeforeBuying) || MathUtilities.IsPositive(MinimumTrucksBeforeBuying))
                //{
                //    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                //    if (MathUtilities.IsPositive(MinimumTrucksBeforeBuying))
                //        htmlWriter.Write("A minimum of <span class=\"setvalue\">" + MinimumTrucksBeforeBuying.ToString("###") + "</span> truck loads is required");

                //    if (MathUtilities.IsPositive(MinimumLoadBeforeBuying))
                //    {
                //        if (MathUtilities.IsPositive(MinimumTrucksBeforeBuying))
                //            htmlWriter.Write(" and each ");
                //        else
                //            htmlWriter.Write("Each ");

                //        htmlWriter.Write("truck must be at least <span class=\"setvalue\">" + MinimumLoadBeforeBuying.ToString("0.##%") + "</span> full");
                //    }
                //    htmlWriter.Write(" for purchases</div>");
                //}

                //if (MathUtilities.IsPositive(TruckMethaneEmissions) || MathUtilities.IsPositive(TruckN2OEmissions))
                //{
                //    htmlWriter.Write("\r\n<div class=\"activityentry\">Each truck will emmit ");
                //    if (TruckMethaneEmissions > 0)
                //        htmlWriter.Write("<span class=\"setvalue\">" + TruckMethaneEmissions.ToString("0.###") + "</span> kg methane");

                //    if (MathUtilities.IsPositive(TruckCO2Emissions))
                //    {
                //        if (MathUtilities.IsPositive(TruckMethaneEmissions))
                //            htmlWriter.Write(", ");

                //        htmlWriter.Write("<span class=\"setvalue\">" + TruckCO2Emissions.ToString("0.###") + "</span> kg carbon dioxide");
                //    }
                //    if (MathUtilities.IsPositive(TruckN2OEmissions))
                //    {
                //        if (TruckMethaneEmissions + TruckCO2Emissions > 0)
                //            htmlWriter.Write(" and ");

                //        htmlWriter.Write("<span class=\"setvalue\">" + TruckN2OEmissions.ToString("0.###") + "</span> kg nitrous oxide");
                //    }
                //    htmlWriter.Write(" per km");
                //    htmlWriter.Write("</div>");

                //    if (MathUtilities.IsPositive(TruckMethaneEmissions))
                //    {
                //        htmlWriter.Write("\r\n<div class=\"activityentry\">Methane emissions will be placed in ");
                //        if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                //            htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].Methane</span> if present");
                //        else
                //            htmlWriter.Write($"<span class=\"resourcelink\">{MethaneStoreName}</span>");

                //        htmlWriter.Write("</div>");
                //    }
                //    if (MathUtilities.IsPositive(TruckCO2Emissions))
                //    {
                //        htmlWriter.Write("\r\n<div class=\"activityentry\">Carbon dioxide emissions will be placed in ");
                //        if (CarbonDioxideStoreName is null || CarbonDioxideStoreName == "Use store named CO2 if present")
                //            htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].CO2</span> if present");
                //        else
                //            htmlWriter.Write($"<span class=\"resourcelink\">{CarbonDioxideStoreName}</span>");

                //        htmlWriter.Write("</div>");
                //    }
                //    if (MathUtilities.IsPositive(TruckN2OEmissions))
                //    {
                //        htmlWriter.Write("\r\n<div class=\"activityentry\">Nitrous oxide emissions will be placed in ");
                //        if (NitrousOxideStoreName is null || NitrousOxideStoreName == "Use store named N2O if present")
                //            htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].N2O</span> if present");
                //        else
                //            htmlWriter.Write($"<span class=\"resourcelink\">{NitrousOxideStoreName}</span>");

                //        htmlWriter.Write("</div>");
                //    }
                //}
                return htmlWriter.ToString(); 
            }
        }



        #endregion

    }
}
