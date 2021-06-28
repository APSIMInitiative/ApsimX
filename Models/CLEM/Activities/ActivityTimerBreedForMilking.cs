using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer sequence
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [Description("Time breeding and female selection for best continuous milk production")]
    [HelpUri(@"Content/Features/Timers/ActivityTimerBreedForMilking.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerBreedForMilking : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private ResourcesHolder Resources = null;

        private int shortenLactationMonths = 0;
        private double milkingsPerConceptionsCycle = 0;
        private double minConceiveInterval = 0;
        private RuminantType breedParams; 
        private RuminantActivityBreed breedParent = null;
        private RuminantActivityControlledMating controlledMatingParent = null;

        /// <summary>
        /// Months to rest after lactation
        /// </summary>
        [Description("Months to rest after lactation")]
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int RestMonths { get; set; }

        /// <summary>
        /// Months to shorten lactation before next conception
        /// </summary>
        [Description("Months to shorten lactation")]
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int ShortenLactationMonths { get; set; }

        /// <summary>
        /// Notify CLEM that timer was ok
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// The list of individuals to breed this time step
        /// </summary>
        [JsonIgnore]
        public int NumberOfIndividualsToBreed { get; set; } = 0;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get details from parent breeding activity
            controlledMatingParent = this.Parent as RuminantActivityControlledMating;
            if (controlledMatingParent is null)
            {
                throw new ApsimXException(this, $"Invalid parent component of [a={this.Name}]. Expecting [a=RuminantActivityControlledMating].[f=ActivityTimerBreedForMilking]");
            }
            breedParent = this.Parent as RuminantActivityBreed;
            breedParams = Resources.GetResourceItem(this, breedParent.PredictedHerdBreed, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            int monthsOfMilking = Convert.ToInt32(Math.Ceiling(breedParams.MilkingDays / 30.4));
            shortenLactationMonths = Math.Min(ShortenLactationMonths, monthsOfMilking);

            // determine min time between conceptions with full milk production minus cut short and resting
            double minConceiveInterval = Convert.ToInt32(breedParams.GestationLength + Math.Ceiling(breedParams.MilkingDays / 30.4), CultureInfo.InvariantCulture) - shortenLactationMonths + RestMonths;

            // get the milking period
            milkingsPerConceptionsCycle = Math.Ceiling(minConceiveInterval/ monthsOfMilking);
        }



        /// <summary>An event handler to determine the breeders to breed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDoCutAndCarry")]
        private void OnCLEMDoCutAndCarry(object sender, EventArgs e)
        {
            // cut and carry event to ensure this is determined before breeding event
            // calculate whether activity is needed this time step

            NumberOfIndividualsToBreed = 0;

            // get all breeders
            IEnumerable<RuminantFemale> breeders = controlledMatingParent.GetBreeders().OfType<RuminantFemale>();

            if (breeders.Count() > 0)
            {
                int maxBreedersPerCycle = Math.Max(1, Convert.ToInt32(Math.Ceiling(breeders.Count() / milkingsPerConceptionsCycle)));

                // should always have max pregnant in early stage up to 
                int pregnantInMinConceiveInterval = breeders.Where(a => a.IsPregnant && a.Age - a.AgeAtLastConception < minConceiveInterval).Count();
                int readyToBreedCount = breeders.Where(a => a.IsAbleToBreed).Count();

                if(readyToBreedCount > 0 && pregnantInMinConceiveInterval < maxBreedersPerCycle)
                {
                    NumberOfIndividualsToBreed = maxBreedersPerCycle - pregnantInMinConceiveInterval;
                }
            }
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                if(NumberOfIndividualsToBreed > 0)
                {
                    // report activity performed details.
                    ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                    {
                        Activity = new BlankActivity()
                        {
                            Status = ActivityStatus.Timer,
                            Name = this.Name,
                        }
                    };
                    activitye.Activity.SetGuID(this.UniqueID);
                    this.OnActivityPerformed(activitye);

                    return true;
                }
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }


        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write("\r\nTiming of breeding and selection of breeders for continous milk production");
                if(RestMonths + ShortenLactationMonths > 0)
                {
                    htmlWriter.Write("\r\n<br />");
                    if(RestMonths > 0)
                    {
                        htmlWriter.Write($"\r\nAllowing <span class=\"setvalueextra\">{RestMonths}</span> month{((RestMonths>1)?"s":"")} rest after lactation");
                        if(ShortenLactationMonths > 0)
                        {
                            htmlWriter.Write(" and ");
                        }
                    }
                    if (ShortenLactationMonths > 0)
                    {
                        htmlWriter.Write($" breeding {ShortenLactationMonths}</span> month{((ShortenLactationMonths > 1) ? "s" : "")} before end of lactation");
                    }
                }
                htmlWriter.Write("\r\n</div>");
                if (!this.Enabled)
                {
                    htmlWriter.Write(" - DISABLED!");
                }
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                {
                    htmlWriter.Write(this.Name);
                }
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
