using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer for breeding to maintain best milk production
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [Description("This timer controls breeding and female selection for best continuous milk production")]
    [HelpUri(@"Content/Features/Timers/ActivityTimerBreedForMilking.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerBreedForMilking : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private ResourcesHolder resources = null;

        private int shortenLactationMonths;
        private double milkingsPerConceptionsCycle;
        private int minConceiveInterval;
        private int startBreedCycleGestationOffsett;
        private int pregnancyDuration;
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
        public IEnumerable<RuminantFemale> IndividualsToBreed { get; set; } = null;

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerBreedForMilking()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            base.SetDefaults();
        }

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
            breedParent = controlledMatingParent.Parent as RuminantActivityBreed;
            breedParams = resources.FindResourceType<RuminantHerd, RuminantType>(this, breedParent.PredictedHerdBreed, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            int monthsOfMilking = Convert.ToInt32(Math.Ceiling(breedParams.MilkingDays / 30.4), CultureInfo.InvariantCulture);
            shortenLactationMonths = Math.Max(0, monthsOfMilking - ShortenLactationMonths);

            pregnancyDuration = Convert.ToInt32(breedParams.GestationLength, CultureInfo.InvariantCulture);

            // determine min time between conceptions with full milk production minus cut short and resting
            minConceiveInterval = Math.Max(0, pregnancyDuration + shortenLactationMonths + RestMonths);

            startBreedCycleGestationOffsett = shortenLactationMonths - pregnancyDuration;
            if (startBreedCycleGestationOffsett < pregnancyDuration * -1)
            {
                throw new Exception("Cannot handle condition where milking cycle starts before pregnancy");
            }

            // get the milking period
            milkingsPerConceptionsCycle = Math.Ceiling((minConceiveInterval * 1.0)/ monthsOfMilking);
        }

        /// <summary>An event handler to determine the breeders to breed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDoCutAndCarry")]
        private void OnCLEMDoCutAndCarry(object sender, EventArgs e)
        {
            // cut and carry event to ensure this is determined before breeding event

            // calculate whether activity is needed this time step (IndividualsToBreed contains breeders)
            int numberNeeded = 0;
            IndividualsToBreed = null;

            int breedingSpreadMonths = 2;

            // get all breeders currently in the population
            // TODO: remove oftype when sex determination fixed
            var females = controlledMatingParent.CurrentHerd(true).OfType<RuminantFemale>();

            var breedersList = females.Where(r => r.IsBreeder);

            double tooOldToMate = double.PositiveInfinity;
            if (controlledMatingParent != null)
                tooOldToMate = controlledMatingParent.MaximumAgeMating;

            var breedersNotTooOldToMate = breedersList.Where(a => a.Age <= tooOldToMate);
            if (!breedersNotTooOldToMate.Any())
            {
                return;
            }

            // this needs to be calculated here at this time with current herd size
            // this count excludes those too old to be mated from breed param settings
            int maxBreedersPerCycle = Math.Max(1, Convert.ToInt32(Math.Ceiling((double)breedersNotTooOldToMate.Count() / milkingsPerConceptionsCycle)));

            // get females currently lactating            
            var lactatingList = females.Where(f => f.IsLactating);
            if (lactatingList.Any() && lactatingList.Max(a => a.Age - a.AgeAtLastBirth) < startBreedCycleGestationOffsett)
            {
                // the max lactation period of lactating females is less than the time to start breeding for future cycle
                // return with no individuals in the IndividualsToBreed list
                return;
            }

            // get breeders currently pregnant
            var pregnantList = females.Where(f => f.IsPregnant);

            // get individuals in first lactation cycle of gestation
            double lactationCyclesInGestation = Math.Round((double)ShortenLactationMonths / pregnancyDuration, 2);

            var firstCycleList = pregnantList.Where(a => a.Age - a.AgeAtLastConception <= lactationCyclesInGestation).ToList();
            if (firstCycleList.Count > 0 && (firstCycleList.Count < maxBreedersPerCycle & firstCycleList.Max(a => a.Age - a.AgeAtLastConception) <= breedingSpreadMonths))
            {
                // if where less than the spread months from the max pregnancy found
                numberNeeded = maxBreedersPerCycle - firstCycleList.Count;
            }

            if(numberNeeded > 0)
            {
                // return the number needed of breeders able to mate in this timestep
                IndividualsToBreed = breedersNotTooOldToMate.Where(a => a.IsAbleToBreed).Take(numberNeeded);

                // report activity performed details.
                ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                {
                    Name = this.Name,
                    Status = ActivityStatus.Timer,
                    Id = this.UniqueID.ToString(),
                };
                this.OnActivityPerformed(activitye);
            }
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                if(IndividualsToBreed != null && IndividualsToBreed.Any())
                {
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
        public override string ModelSummary()
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
                            htmlWriter.Write(" and ");
                    }
                    if (ShortenLactationMonths > 0)
                        htmlWriter.Write($" breeding {ShortenLactationMonths}</span> month{((ShortenLactationMonths > 1) ? "s" : "")} before end of lactation");
                }
                htmlWriter.Write("\r\n</div>");
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
                {
                    htmlWriter.Write(this.Name);
                }
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
