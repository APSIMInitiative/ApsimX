using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
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
    [ValidParent(ParentType = typeof(RuminantActivityBreed))]
    [Description("Time breeding and female selection for best continuous milk production")]
    [HelpUri(@"Content/Features/Timers/ActivityTimerBreedForMilking.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerBreedForMilking : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private ResourcesHolder Resources = null;

        private int monthsSinceConception = 0;
        private int conceptionInterval = 0;
        private int numberConceivedThisInterval = 0;
        private int numberNeededThisInterval = 0;
        private RuminantType breedParams; 
        private RuminantActivityBreed breedParent = null;

        /// <summary>
        /// Notify CLEM that timer was ok
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get details from parent breeding activity
            breedParent = this.Parent.Parent as RuminantActivityBreed;
            if (breedParent is null)
            {
                throw new ApsimXException(this, $"Invalid grandparent component of [a={this.Name}]. Expecting [a=RuminantActivityBreed].[a=RuminantActivityControlledMating].[f=ActivityTimerBreedForMilking]");
            }

            // breed params
            breedParams = Resources.GetResourceItem(this, breedParent.PredictedHerdBreed, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            // determine min time between conceptions with full milk production
            double conceiveInterval = Convert.ToInt32(breedParams.GestationLength + Math.Ceiling(breedParams.MilkingDays / 30.4), CultureInfo.InvariantCulture);
            // get the milking period
            double milkInterval = Math.Ceiling(breedParams.MilkingDays / 30.4);

            double numGroups = milkInterval / conceiveInterval;




            // determine number of breeders each timing

            // reduce interval to increase milk production

        }

        /// <summary>An event handler to perform herd breeding </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            numberConceivedThisInterval += breedParent.NumberConceived;
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                if((monthsSinceConception >= conceptionInterval) | (numberConceivedThisInterval < numberNeededThisInterval))
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
                htmlWriter.Write("\r\nTiming based on calculated conception interval and number of breeders per mating period to attmept to maintain continous milk production");
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
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

        #endregion
    }
}
