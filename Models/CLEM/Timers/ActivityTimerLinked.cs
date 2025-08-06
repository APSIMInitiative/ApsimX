using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on monthly interval
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This timer provides a link to an existing timer")]
    [HelpUri(@"Content/Features/Timers/LinkedTimer.htm")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [ValidParent(ParentType = typeof(ReportResourceBalances))]
    [ValidParent(ParentType = typeof(SummariseRuminantHerd))]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [Version(1, 0, 1, "")]
    public class ActivityTimerLinked : CLEMModel, IActivityTimer, IActivityPerformedNotifier, IValidatableObject
    {
        [NonSerialized]
        private IEnumerable<IActivityTimer> timersAvailable;
        [NonSerialized]
        private IActivityTimer linkedTimer;

        /// <summary>
        /// Linked existing timer
        /// </summary>
        [Description("Existing timer to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetAllTimerNames")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "An existing timer must be selected")]
        public string ExistingTimerName { get; set; }

        /// <summary>
        /// Notify CLEM that timer was ok
        /// </summary>
        public event EventHandler ActivityPerformed;

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerLinked()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                return linkedTimer?.ActivityDue ?? false;
            }
        }

        private void GetAllTimersAvailable()
        {
            var zone = Structure.FindParent<Zone>(recurse: true);
            timersAvailable = Structure.FindChildren<IActivityTimer>(relativeTo: zone, recurse: true).Where(a => (a as IModel).Enabled);
        }

        private List<string> GetAllTimerNames()
        {
            GetAllTimersAvailable();
            return timersAvailable.Cast<Model>().Select(a => $"{a.Parent.Name}.{a.Name}").ToList();
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            if (linkedTimer != null)
                return linkedTimer.Check(dateToCheck);
            else
                return false;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GetAllTimersAvailable();
            linkedTimer = timersAvailable.Cast<Model>().Where(a => $"{a.Parent.Name}.{a.Name}" == ExistingTimerName).FirstOrDefault() as IActivityTimer;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write($"Linked to {CLEMModel.DisplaySummaryValueSnippet(ExistingTimerName, errorString: "No timer selected")}");
                htmlWriter.Write("</span></div>");
                if (!this.Enabled & !FormatForParentControl)
                    htmlWriter.Write(" - DISABLED!");
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                    htmlWriter.Write(this.Name);

                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
                return htmlWriter.ToString();
            }
        }
        #endregion

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (linkedTimer is null)
            {
                string[] memberNames = new string[] { "Linked timer" };
                string errorMsg = string.Empty;
                if (ExistingTimerName is null)
                    errorMsg = "No existing timer has been specified";
                else
                    errorMsg = $"The timer {ExistingTimerName} could not be found.{Environment.NewLine}Ensure the name matches the name of an enabled timer in the simulation tree below the same ZoneCLEM";

                results.Add(new ValidationResult(errorMsg, memberNames));
            }
            return results;
        }
        #endregion
    }
}
