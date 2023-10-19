﻿using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Clock component to handle all CLEM specific timing events
    /// </summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Clock))]
    [Description("Provides required Clock events for CLEM")]
    [HelpUri(@"Content/Features/ClockCLEM.htm")]
    public class CLEMEvents : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Access to the APSIM Clock (parent)
        /// </summary>
        [Link] public Clock Clock { get; set; }

        /// <summary>CLEM initialise Resources occurs once at start of simulation</summary>
        public event EventHandler CLEMInitialiseResource;
        /// <summary>CLEM initialise Activity occurs once at start of simulation</summary>
        public event EventHandler CLEMInitialiseActivity;
        /// <summary>CLEM validate all data entry</summary>
        public event EventHandler CLEMValidate;
        /// <summary>CLEM start of timestep event</summary>
        public event EventHandler CLEMStartOfTimeStep;
        /// <summary>CLEM set labour availability after start of timestep and financial considerations.</summary>
        public event EventHandler CLEMUpdateLabourAvailability;
        /// <summary>CLEM update pasture</summary>
        public event EventHandler CLEMUpdatePasture;
        /// <summary>CLEM detach pasture</summary>
        public event EventHandler CLEMDetachPasture;
        /// <summary>CLEM pasture has been added and is ready for use</summary>
        public event EventHandler CLEMPastureReady;
        /// <summary>CLEM cut and carry</summary>
        public event EventHandler CLEMDoCutAndCarry;
        /// <summary>CLEM Do Animal (Ruminant and Other) Breeding and milk calculations</summary>
        public event EventHandler CLEMAnimalBreeding;
        /// <summary>Get potential intake. This includes suckling milk consumption</summary>
        public event EventHandler CLEMPotentialIntake;
        /// <summary>Request and allocate resources to all Activities based on UI Tree order of priority. Some activities will obtain resources here and perform actions later</summary>
        public event EventHandler CLEMCalculateManure;
        /// <summary>Request and allocate resources to all Activities based on UI Tree order of priority. Some activities will obtain resources here and perform actions later</summary>
        public event EventHandler CLEMCollectManure;
        /// <summary>Request and perform the collection of maure after resources are allocated and manure produced in time-step</summary>
        public event EventHandler CLEMGetResourcesRequired;
        /// <summary>CLEM Calculate Animals (Ruminant and Other) milk production</summary>
        public event EventHandler CLEMAnimalMilkProduction;
        /// <summary>CLEM Calculate Animals(Ruminant and Other) weight gain</summary>
        public event EventHandler CLEMAnimalWeightGain;
        /// <summary>CLEM Do Animal (Ruminant and Other) death</summary>
        public event EventHandler CLEMAnimalDeath;
        /// <summary>CLEM Do Animal (Ruminant and Other) milking</summary>
        public event EventHandler CLEMAnimalMilking;
        /// <summary>CLEM Calculate ecological state after all deaths and before management</summary>
        public event EventHandler CLEMCalculateEcologicalState;
        /// <summary>CLEM Do animal marking so complete before undertaking management decisions</summary>
        public event EventHandler CLEMAnimalMark;
        /// <summary>CLEM Do Animal (Ruminant and Other) Herd Management (adjust breeders and sires etc.)</summary>
        public event EventHandler CLEMAnimalManage;
        /// <summary>CLEM stock animals to pasture availability or other metrics</summary>
        public event EventHandler CLEMAnimalStock;
        /// <summary>CLEM sell animals to market including transporting and labour</summary>
        public event EventHandler CLEMAnimalSell;
        /// <summary>CLEM buy animals including transporting and labour</summary>
        public event EventHandler CLEMAnimalBuy;
        /// <summary>CLEM Age your resources (eg. Decomose Fodder, Age your labour, Age your Animals)</summary>
        public event EventHandler CLEMAgeResources;
        /// <summary>CLEM event to calculate monthly herd summary</summary>
        public event EventHandler CLEMHerdSummary;
        /// <summary>CLEM finalize time-step before end</summary>
        public event EventHandler CLEMFinalizeTimeStep;
        /// <summary>CLEM end of timestep event</summary>
        public event EventHandler CLEMEndOfTimeStep;

        private DateTime nextDate;

        /// <summary>
        /// CLEM time-step
        /// </summary>
        [Description("Time-step")]
        [System.ComponentModel.DefaultValue("Monthly")]
        public TimeStepTypes TimeStep { get; set; } = TimeStepTypes.Monthly;

        /// <summary>
        /// Custom time-step (days)
        /// </summary>
        [Description("Custom time-step (in days)")]
        [Core.Display(VisibleCallback = "IsCustomIntervalPropertyVisible")]
        [Required, GreaterThanValue(0)]
        public int CustomTimeStep { get; set; }

        /// <summary>
        /// Ecological indicators calculation interval (in months, 1 monthly, 12 annual)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(12)]
        [Description("Ecological indicators interval (months)")]
        [Required, GreaterThanValue(0)]
        public int EcologicalIndicatorsCalculationInterval { get; set; }

        /// <summary>
        /// End of month to calculate ecological indicators
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(7)]
        [Description("First month for ecological indicators")]
        [Required, Month]
        public MonthsOfYear EcologicalIndicatorsCalculationMonth { get; set; }

        /// <summary>
        /// Custom interval (days)
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Month this cecological indicators calculation is next due.
        /// </summary>
        [JsonIgnore]
        public DateTime EcologicalIndicatorsNextDueDate { get; set; }


        /// <summary>An event handler to perform any start of simulation tasks</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        protected virtual void OnStartOfSimulation(object sender, EventArgs e)
        {
            switch (TimeStep)
            {
                case TimeStepTypes.Monthly:
                    break;
                case TimeStepTypes.Fortnightly:
                    Interval = 14;
                    break;
                case TimeStepTypes.Weekly:
                    Interval = 7;
                    break;
                case TimeStepTypes.Daily:
                    Interval = 1;
                    break;
                case TimeStepTypes.Custom:
                    Interval = CustomTimeStep;
                    break;
                default:
                    throw new NotImplementedException($"Unknown time-step [{TimeStep}] not supported in [CLEMClock]");
            }
            SetNextDate(Clock.StartDate.AddDays(-1));

            CLEMInitialiseResource?.Invoke(this, e);
            CLEMInitialiseActivity?.Invoke(this, e);
            CLEMValidate?.Invoke(this, e);
        }

        private void SetNextDate(DateTime fromDate)
        {
            if (TimeStep == TimeStepTypes.Monthly)
            {
                nextDate = fromDate.AddMonths(1);
                Interval = (nextDate - fromDate).Days + 1;
            }
            else
            {
                nextDate = fromDate.AddDays(Interval);
            }
        }

        /// <summary>
        /// Determines whether the custom interval property is available based on TimeStepTypes set by user.
        /// </summary>
        /// <returns>Boolean indicating whether to display custom interval property</returns>
        public bool IsCustomIntervalPropertyVisible()
        {
            return TimeStep == TimeStepTypes.Custom;
        }

        /// <summary>Fire all CLEM events in order at the EndOfDay of the specificed date</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfDay")]
        protected virtual void OnEndOfDay(object sender, EventArgs args)
        {
            if (Clock.Today == nextDate)
            {
                // CLEM events performed at the EndOfDay of specificed date
                CLEMStartOfTimeStep?.Invoke(this, args);
                CLEMUpdateLabourAvailability?.Invoke(this, args);
                CLEMUpdatePasture?.Invoke(this, args);
                CLEMPastureReady?.Invoke(this, args);
                CLEMDoCutAndCarry?.Invoke(this, args);
                CLEMAnimalBreeding?.Invoke(this, args);
                CLEMAnimalMilkProduction?.Invoke(this, args);
                CLEMPotentialIntake?.Invoke(this, args);
                CLEMGetResourcesRequired?.Invoke(this, args);
                CLEMAnimalWeightGain?.Invoke(this, args);
                CLEMCalculateManure?.Invoke(this, args);
                CLEMCollectManure?.Invoke(this, args);
                CLEMAnimalDeath?.Invoke(this, args);
                CLEMAnimalMilking?.Invoke(this, args);
                CLEMCalculateEcologicalState?.Invoke(this, args);
                CLEMAnimalMark?.Invoke(this, args);
                CLEMAnimalManage?.Invoke(this, args);
                CLEMAnimalStock?.Invoke(this, args);
                CLEMAnimalSell?.Invoke(this, args);
                CLEMDetachPasture?.Invoke(this, args);
                CLEMHerdSummary?.Invoke(this, args);
                CLEMAgeResources?.Invoke(this, args);
                CLEMAnimalBuy?.Invoke(this, args);
                CLEMFinalizeTimeStep?.Invoke(this, args);
                CLEMEndOfTimeStep?.Invoke(this, args);

                SetNextDate(Clock.Today);
            }
        }

        /// <summary>
        /// Method to determine if this is the month to calculate ecological indicators
        /// </summary>
        /// <returns></returns>
        public bool IsEcologicalIndicatorsCalculationMonth()
        {
            return EcologicalIndicatorsNextDueDate.Year == Clock.Today.Year && EcologicalIndicatorsNextDueDate.Month == Clock.Today.Month;
        }

        /// <summary>Data stores to clear at start of month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            if (IsEcologicalIndicatorsCalculationMonth())
                EcologicalIndicatorsNextDueDate = EcologicalIndicatorsNextDueDate.AddMonths(EcologicalIndicatorsCalculationInterval);

            // ToDo: Ensure next date is the last day of the relvent month
            // the IsDue will return true if the current timestep contains the due date
        }


        #region validation

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Clock.StartDate.ToShortDateString() == "1/01/0001")
            {
                string[] memberNames = new string[] { "Clock.StartDate" };
                results.Add(new ValidationResult(String.Format("Invalid start date {0}", Clock.StartDate.ToShortDateString()), memberNames));
            }
            if (Clock.EndDate.ToShortDateString() == "1/01/0001")
            {
                string[] memberNames = new string[] { "Clock.EndDate" };
                results.Add(new ValidationResult(String.Format("Invalid end date {0}", Clock.EndDate.ToShortDateString()), memberNames));
            }
            if (Clock.StartDate.Day != 1)
            {
                string[] memberNames = new string[] { "Clock.StartDate" };
                results.Add(new ValidationResult(String.Format("CLEM must commence on the first day of a month. Invalid start date {0}", Clock.StartDate.ToShortDateString()), memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to validate properties and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMValidate")]
        private void OnCLEMValidate(object sender, EventArgs e)
        {
            // validation is performed here
            // this event fires after Activity and Resource initialisation so that resources are available to check in the validation.
            // Commencing event is too early as Summary has not been created for reporting.
            // Some values assigned in commencing will not be checked before processing, but will be caught here
            // Each ZoneCLEM and Market will call this validation for all children
            // CLEM components above ZoneCLEM (e.g. RandomNumberGenerator) needs to validate itself

            // not all errors will be reported in validation so perform in two steps
            //Validate(this, "", this, summary);
            ZoneCLEM.ReportInvalidParameters(this);

            if (Clock.StartDate.Year > 1) // avoid checking if clock not set.
            {
                if ((int)EcologicalIndicatorsCalculationMonth >= Clock.StartDate.Month)
                {
                    DateTime trackDate = new DateTime(Clock.StartDate.Year, (int)EcologicalIndicatorsCalculationMonth, Clock.StartDate.Day);
                    while (trackDate.AddMonths(-EcologicalIndicatorsCalculationInterval) >= Clock.Today)
                        trackDate = trackDate.AddMonths(-EcologicalIndicatorsCalculationInterval);
                    EcologicalIndicatorsNextDueDate = trackDate;
                }
                else
                {
                    EcologicalIndicatorsNextDueDate = new DateTime(Clock.StartDate.Year, (int)EcologicalIndicatorsCalculationMonth, Clock.StartDate.Day);
                    while (Clock.StartDate > EcologicalIndicatorsNextDueDate)
                        EcologicalIndicatorsNextDueDate = EcologicalIndicatorsNextDueDate.AddMonths(EcologicalIndicatorsCalculationInterval);
                }
            }
        }

        #endregion


        #region Descriptive summary

        /////<inheritdoc/>
        //public string GetFullSummary(IModel model, List<string> parentControls, string htmlString, Func<string, string> markdown2Html = null)
        //{
        //    using (StringWriter htmlWriter = new StringWriter())
        //    {
        //        htmlWriter.Write("\r\n<div class=\"holdermain\" style=\"opacity: " + ((!this.Enabled) ? "0.4" : "1") + "\">");

        //        CurrentAncestorList = parentControls.ToList();
        //        CurrentAncestorList.Add(model.GetType().Name);

        //        // get clock
        //        IModel parentSim = FindAncestor<Simulation>();

        //        htmlWriter.Write(CLEMModel.AddMemosToSummary(parentSim, markdown2Html));

        //        // create the summary box with properties of this component
        //        if (this is ICLEMDescriptiveSummary)
        //        {
        //            htmlWriter.Write(this.ModelSummaryOpeningTags());
        //            htmlWriter.Write(this.ModelSummaryInnerOpeningTagsBeforeSummary());
        //            htmlWriter.Write(this.ModelSummary());
        //            // TODO: May need to implement Adding Memos for some Models with reduced display
        //            htmlWriter.Write(this.ModelSummaryInnerOpeningTags());
        //            htmlWriter.Write(this.ModelSummaryInnerClosingTags());
        //            htmlWriter.Write(this.ModelSummaryClosingTags());
        //        }

        //        // find random number generator
        //        RandomNumberGenerator rnd = parentSim.FindDescendant<RandomNumberGenerator>();
        //        if (rnd != null)
        //        {
        //            htmlWriter.Write("\r\n<div class=\"clearfix defaultbanner\">");
        //            htmlWriter.Write("<div class=\"namediv\">" + rnd.Name + "</div><br />");
        //            htmlWriter.Write("<div class=\"typediv\">RandomNumberGenerator</div>");
        //            htmlWriter.Write("</div>");
        //            htmlWriter.Write("\r\n<div class=\"defaultcontent\">");
        //            htmlWriter.Write("\r\n<div class=\"activityentry\">Random numbers are provided for this simultion with ");
        //            if (rnd.Seed == 0)
        //                htmlWriter.Write("every run using a different sequence.");
        //            else
        //                htmlWriter.Write("each run identical by using the seed <span class=\"setvalue\">" + rnd.Seed.ToString() + "</span>");
        //            htmlWriter.Write("\r\n</div>");

        //            htmlWriter.Write(CLEMModel.AddMemosToSummary(rnd, markdown2Html));

        //            htmlWriter.Write("\r\n</div>");
        //        }

        //        Clock clk = parentSim.FindChild<Clock>();
        //        if (clk != null)
        //        {
        //            htmlWriter.Write("\r\n<div class=\"clearfix defaultbanner\">");
        //            htmlWriter.Write("<div class=\"namediv\">" + clk.Name + "</div><br />");
        //            htmlWriter.Write("<div class=\"typediv\">Clock</div>");
        //            htmlWriter.Write("</div>");
        //            htmlWriter.Write("\r\n<div class=\"defaultcontent\">");
        //            htmlWriter.Write("\r\n<div class=\"activityentry\">This simulation runs from ");
        //            if (clk.Start == null)
        //                htmlWriter.Write("<span class=\"errorlink\">[START DATE NOT SET]</span>");
        //            else
        //                htmlWriter.Write("<span class=\"setvalue\">" + clk.StartDate.ToShortDateString() + "</span>");
        //            htmlWriter.Write(" to ");
        //            if (clk.End == null)
        //                htmlWriter.Write("<span class=\"errorlink\">[END DATE NOT SET]</span>");
        //            else
        //                htmlWriter.Write("<span class=\"setvalue\">" + clk.EndDate.ToShortDateString() + "</span>");
        //            htmlWriter.Write("\r\n</div>");

        //            htmlWriter.Write(CLEMModel.AddMemosToSummary(clk, markdown2Html));

        //            htmlWriter.Write("\r\n</div>");
        //            htmlWriter.Write("\r\n</div>");
        //        }

        //        foreach (CLEMModel cm in this.FindAllChildren<CLEMModel>())
        //            htmlWriter.Write(cm.GetFullSummary(cm, CurrentAncestorList, "", markdown2Html));

        //        CurrentAncestorList = null;

        //        return htmlWriter.ToString();
        //    }
        //}

        /////<inheritdoc/>
        //public override string ModelSummary()
        //{
        //    using (StringWriter htmlWriter = new StringWriter())
        //    {
        //        htmlWriter.Write("\r\n<div class=\"activityentry\">");
        //        htmlWriter.Write("This farm is identified as region ");
        //        htmlWriter.Write($"<span class=\"setvalue\">{ClimateRegion}</span></div>");

        //        ResourcesHolder resources = this.FindChild<ResourcesHolder>();
        //        if (resources != null)
        //        {
        //            if (resources.FoundMarket != null)
        //            {
        //                htmlWriter.Write("\r\n<div class=\"activityentry\">");
        //                htmlWriter.Write("This farm represents ");
        //                htmlWriter.Write($"<span class=\"setvalue\">{FarmMultiplier}</span></div> farm(s) when trading with the Market</div>");
        //            }
        //        }

        //        if ((this.FindDescendant<RuminantActivityGrazeAll>() != null) || (this.FindDescendant<RuminantActivityGrazePasture>() != null) || (this.FindDescendant<RuminantActivityGrazePastureHerd>() != null))
        //        {
        //            htmlWriter.Write("\r\n<div class=\"activityentry\">");
        //            htmlWriter.Write("Ecological indicators will be calculated every ");
        //            if (EcologicalIndicatorsCalculationInterval <= 0)
        //                htmlWriter.Write("<span class=\"errorlink\">NOT SET</span> months");
        //            else
        //                htmlWriter.Write($"<span class=\"setvalue\">{EcologicalIndicatorsCalculationInterval}</span> month{((EcologicalIndicatorsCalculationInterval == 1) ? "" : "s")}");
        //            htmlWriter.Write($" starting at the end of {EcologicalIndicatorsCalculationMonth}</div>");
        //        }

        //        if (AutoCreateDescriptiveSummary)
        //        {
        //            htmlWriter.Write("\r\n<div class=\"activityentry\">");
        //            htmlWriter.Write($"This component will be included in the overall simulation summary decription html file</div>");
        //        }
        //        return htmlWriter.ToString();
        //    }
        //}

        /////<inheritdoc/>
        //public string ModelSummaryClosingTags()
        //{
        //    return "\r\n</div>\r\n</div>";
        //}

        /////<inheritdoc/>
        //public string ModelSummaryOpeningTags()
        //{
        //    string overall = "default";
        //    string extra = "";

        //    using (StringWriter htmlWriter = new StringWriter())
        //    {
        //        htmlWriter.Write("\r\n<div class=\"holder" + ((extra == "") ? "main" : "sub") + " " + overall + "\" style=\"opacity: " + ((!this.Enabled) ? 0.4 : 1.0).ToString() + ";\">");
        //        htmlWriter.Write("\r\n<div class=\"clearfix " + overall + "banner" + extra + "\">" + this.ModelSummaryNameTypeHeader() + "</div>");
        //        htmlWriter.Write("\r\n<div class=\"" + overall + "content" + ((extra != "") ? extra : "") + "\">");

        //        return htmlWriter.ToString();
        //    }
        //}

        /////<inheritdoc/>
        //public string ModelSummaryInnerClosingTags()
        //{
        //    return "";
        //}

        /////<inheritdoc/>
        //public string ModelSummaryInnerOpeningTags()
        //{
        //    return "";
        //}

        /////<inheritdoc/>
        //public string ModelSummaryInnerOpeningTagsBeforeSummary()
        //{
        //    return "";
        //}

        /////<inheritdoc/>
        //public string ModelSummaryNameTypeHeader()
        //{
        //    using (StringWriter htmlWriter = new StringWriter())
        //    {
        //        htmlWriter.Write("<div class=\"namediv\">" + this.Name + ((!this.Enabled) ? " - DISABLED!" : "") + "</div>");
        //        if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
        //        {
        //            htmlWriter.Write("<div class=\"partialdiv\"");
        //            htmlWriter.Write(">");
        //            htmlWriter.Write("</div>");
        //        }
        //        htmlWriter.Write("<br /><div class=\"typediv\">" + this.GetType().Name + "</div>");
        //        return htmlWriter.ToString();
        //    }
        //}

        /////<inheritdoc/>
        //public string ModelSummaryNameTypeHeaderText()
        //{
        //    return this.Name;
        //}
        #endregion

    }
}
