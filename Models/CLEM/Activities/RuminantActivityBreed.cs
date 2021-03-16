
using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.Globalization;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant breeding activity</summary>
    /// <summary>This activity provides all functionality for ruminant breeding up until natural weaning</summary>
    /// <summary>It will be applied to the supplied herd if males and females are located together</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the breeding of ruminants based upon the current herd filtering.")]
    [Version(1, 0, 7, "Fixed period considered in infering pre simulation conceptions and spread of uncontrolled matings.")]
    [Version(1, 0, 6, "Fixed period considered in infering pre simulation conceptions and spread of uncontrolled matings.")]
    [Version(1, 0, 5, "Fixed issue defining breeders who's weight fell below critical limit.\r\nThis change requires all simulations to be performed again.")]
    [Version(1, 0, 4, "Implemented conception status reporting.")]
    [Version(1, 0, 3, "Removed the inter-parturition calculation and influence on uncontrolled mating\r\nIt is assumed that the individual based model will track conception timing based on the individual's body condition.")]
    [Version(1, 0, 2, "Added calculation for proportion offspring male parameter")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantBreed.htm")]
    public class RuminantActivityBreed : CLEMRuminantActivityBase
    {
        [Link]
        private List<LabourRequirement> labour;
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Use artificial insemination (no sires required)
        /// </summary>
        [Description("Use controlled mating/artificial insemination (no sires required)")]
        [Required]
        public bool UseAI { get; set; }

        /// <summary>
        /// Infer pregnancy status at startup
        /// </summary>
        [Description("Infer pregnancy status at startup")]
        [Required]
        public bool InferStartupPregnancy { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            // Assignment of mothers was moved to RuminantHerd resource to ensure this is done even if no breeding activity is included
            this.InitialiseHerd(false, true);

            // get labour specifications
            labour = this.FindAllChildren<LabourRequirement>().Cast<LabourRequirement>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour.Count() == 0)
            {
                labour = new List<LabourRequirement>();
            }

            // check that timer exists for AI
            if (UseAI)
            {
                if(!this.TimingExists)
                {
                    Summary.WriteWarning(this, String.Format("Breeding with Artificial Insemination (AI) requires a Timer otherwise breeding will be undertaken every time step in activity [a={0}]", this.Name));
                }
            }
            else
            {
                if (this.TimingExists)
                {
                    Summary.WriteWarning(this, String.Format("Uncontrolled/natural breeding will occur every month and the timer associated with [a={0}] will be ignored.", this.Name));
                }
            }

            // work out pregnancy status of initial herd
            if (InferStartupPregnancy)
            {
                // set up pre start conception status of breeders
                List<Ruminant> herd = CurrentHerd(true);

                int aDay = Clock.Today.Year;

                // go back (gestation - 1) months
                // this won't include those individuals due to give birth on day 1.

                int monthsAgoStart = 0 - (Convert.ToInt32(Math.Truncate(herd.FirstOrDefault().BreedParams.GestationLength), CultureInfo.InvariantCulture) - 1);
                int monthsAgoStop = -1;

                for (int i = monthsAgoStart; i <= monthsAgoStop; i++)
                {
                    DateTime previousDate = Clock.Today.AddMonths(i);

                    // get list of all individuals of breeding age and condition
                    // grouped by location
                    var breeders = from ind in herd
                                   where
                                   (ind.Gender == Sex.Male && ind.Age + i >= ind.BreedParams.MinimumAge1stMating) ||
                                   (ind.Gender == Sex.Female &&
                                   ind.Age + i >= ind.BreedParams.MinimumAge1stMating &&
                                   !(ind as RuminantFemale).IsPregnant
                                   )
                                   group ind by ind.Location into grp
                                   select grp;

                    int breedersCount = breeders.Count();

                    // must be breeders to bother checking any further
                    // must be either uncontrolled mating or the timing of controlled mating
                    if (breedersCount > 0 & (!UseAI || this.TimingCheck(previousDate)))
                    {
                        int numberPossible = breedersCount;
                        int numberServiced = 1;
                        double limiter = 1;

                        // for each location where parts of this herd are located
                        foreach (var location in breeders)
                        {
                            // uncontrolled conception
                            if (!UseAI)
                            {
                                // check if males and females of breeding condition are together
                                if (location.GroupBy(a => a.Gender).Count() == 2)
                                {
                                    // servicing rate
                                    int maleCount = location.Where(a => a.Gender == Sex.Male).Count();
                                    int femaleCount = location.Where(a => a.Gender == Sex.Female).Count();
                                    double matingsPossible = maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30;

                                    double maleLimiter = Math.Min(1, matingsPossible / femaleCount);

                                    // only get non-pregnant females of breeding age at the time before the simulation included
                                    var availableBreeders = location.Where(b => b.Gender == Sex.Female && b.Age + i >= b.BreedParams.MinimumAge1stMating)
                                        .Cast<RuminantFemale>().Where(a => !a.IsPregnant).ToList();

                                    // only get selection of these of breeders available to spread conceptions
                                    // only 15% of breeding herd of age can conceive in any month or male limited proportion whichever is smaller
                                    int count = Convert.ToInt32(Math.Ceiling(availableBreeders.Count() * Math.Min(0.15, maleLimiter)));
                                    availableBreeders = availableBreeders.OrderBy(x => RandomNumberGenerator.Generator.NextDouble()).Take(count).ToList();

                                    foreach (RuminantFemale female in availableBreeders)
                                    {
                                        // calculate conception
                                        Reporting.ConceptionStatus status = Reporting.ConceptionStatus.NotMated;
                                        double conceptionRate = ConceptionRate(female, out status);
                                        if (RandomNumberGenerator.Generator.NextDouble() <= conceptionRate)
                                        {
                                            female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, i);
                                            // report conception status changed
                                            female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Conceived, female, Clock.Today));

                                            // check for perenatal mortality
                                            for (int j = i; j < monthsAgoStop; j++)
                                            {
                                                for (int k = 0; k < female.CarryingCount; i++)
                                                {
                                                    if (RandomNumberGenerator.Generator.NextDouble() < (female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                                                    {
                                                        female.OneOffspringDies();
                                                        if (female.NumberOfOffspring == 0)
                                                        {
                                                            // report conception status changed when last multiple birth dies.
                                                            female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Failed, female, Clock.Today));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // controlled conception
                            else
                            {
                                numberPossible = Convert.ToInt32(limiter * location.Where(a => a.Gender == Sex.Female).Count(), CultureInfo.InvariantCulture);
                                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                                {
                                    if (!female.IsPregnant && (female.Age - female.AgeAtLastBirth) * 30.4 >= female.BreedParams.MinimumDaysBirthToConception)
                                    {
                                        // calculate conception
                                        Reporting.ConceptionStatus status = Reporting.ConceptionStatus.NotMated;
                                        double conceptionRate = ConceptionRate(female, out status);
                                        if (numberServiced <= numberPossible) // labour/finance limited number
                                        {
                                            if (RandomNumberGenerator.Generator.NextDouble() <= conceptionRate)
                                            {
                                                female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, i);
                                                // report conception status changed
                                                female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Conceived, female, Clock.Today));

                                                // check for perenatal mortality
                                                for (int j = i; j < monthsAgoStop; j++)
                                                {
                                                    for (int k = 0; k < female.CarryingCount; k++)
                                                    {
                                                        if (RandomNumberGenerator.Generator.NextDouble() < (female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                                                        {
                                                            female.OneOffspringDies();
                                                            if (female.NumberOfOffspring == 0)
                                                            {
                                                                // report conception status changed when last multiple birth dies.
                                                                female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Failed, female, Clock.Today));
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                            numberServiced++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        /// <summary>An event handler to perform herd breeding </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            List<Ruminant> herd = CurrentHerd(true); 

            int aDay = Clock.Today.Year;

            // get list of all individuals of breeding age and condition
            // grouped by location
            var breeders = from ind in herd
                            where ind.IsBreedingCondition
                            group ind by ind.Location into grp
                            select grp;

            // calculate labour and finance limitations if needed when doing AI
            int breedersCount = breeders.Count();
            int numberPossible = breedersCount;
            int numberServiced = 1;
            double limiter = 1;
            if (UseAI && TimingOK)
            {
                // attempt to get required resources
                List<ResourceRequest> resourcesneeded = GetResourcesNeededForActivityLocal();
                CheckResources(resourcesneeded, Guid.NewGuid());
                bool tookRequestedResources = TakeResources(resourcesneeded, true);
                // get all shortfalls
                if (tookRequestedResources && (ResourceRequestList != null))
                {
                    //TODO: fix this to account for perHead payments and labour and not fixed expenses
                    double amountCashNeeded = resourcesneeded.Where(a => a.ResourceType == typeof(Finance)).Sum(a => a.Required);
                    double amountCashProvided = resourcesneeded.Where(a => a.ResourceType == typeof(Finance)).Sum(a => a.Provided);
                    double amountLabourNeeded = resourcesneeded.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
                    double amountLabourProvided = resourcesneeded.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
                    double cashlimit = 1;
                    if (amountCashNeeded > 0)
                    {
                        cashlimit = amountCashProvided == 0 ? 0 : amountCashNeeded / amountCashProvided;
                    }
                    double labourlimit = 1;
                    if (amountLabourNeeded > 0)
                    {
                        labourlimit = amountLabourProvided == 0 ? 0 : amountLabourNeeded / amountLabourProvided;
                    }
                    limiter = Math.Min(cashlimit, labourlimit);

                    // TODO: determine if fixed payments were not possible
                    // TODO: determine limits by insufficient labour or cash for per head payments

                }
                // report that this activity was performed as it does not use base GetResourcesRequired
                this.TriggerOnActivityPerformed();
            }
            
            if(!UseAI)
            {
                // report that this activity was performed as it does not use base GetResourcesRequired
                this.TriggerOnActivityPerformed();
                this.Status = ActivityStatus.NotNeeded;
            }

            // for each location where parts of this herd are located
            foreach (var location in breeders)
            {
                // determine all fetus and newborn mortality of all pregnant females.
                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsPregnant).ToList())
                {
                    // calculate fetus and newborn mortality 
                    // total mortality / (gestation months + 1) to get monthly mortality
                    // done here before births to account for post birth motality as well..
                    // IsPregnant status does not change until births occur in next section so will include mortality in month of birth
                    // needs to be caclulated for each offspring carried.
                    for (int i = 0; i < female.CarryingCount; i++)
                    {
                        if (RandomNumberGenerator.Generator.NextDouble() < (female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                        {
                            female.OneOffspringDies();
                            if (female.NumberOfOffspring == 0)
                            {
                                // report conception status changed when last multiple birth dies.
                                female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Failed, female, Clock.Today));
                            }
                        }
                    }
                }

                // check for births of all pregnant females.
                int month = Clock.Today.Month;
                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                {
                    if (female.BirthDue)
                    {
                        int numberOfNewborn = female.CarryingCount;
                        for (int i = 0; i < numberOfNewborn; i++)
                        {
                            // Foetal mortality is now performed each timestep at base of this method
                            object newCalf = null;
                            bool isMale = (RandomNumberGenerator.Generator.NextDouble() <= female.BreedParams.ProportionOffspringMale);
                            double weight = female.BreedParams.SRWBirth * female.StandardReferenceWeight * (1 - 0.33 * (1 - female.Weight / female.StandardReferenceWeight));
                            if (isMale)
                            {
                                newCalf = new RuminantMale(0, Sex.Male, weight, female.BreedParams);
                            }
                            else
                            {
                                newCalf = new RuminantFemale(0, Sex.Female, weight, female.BreedParams);
                            }
                            Ruminant newCalfRuminant = newCalf as Ruminant;
                            newCalfRuminant.HerdName = female.HerdName;
                            newCalfRuminant.Breed = female.BreedParams.Breed;
                            newCalfRuminant.ID = Resources.RuminantHerd().NextUniqueID;
                            newCalfRuminant.Location = female.Location;
                            newCalfRuminant.Mother = female;
                            newCalfRuminant.Number = 1;
                            newCalfRuminant.SetUnweaned();
                            // calf weight from  Freer
                            newCalfRuminant.PreviousWeight = newCalfRuminant.Weight;
                            newCalfRuminant.SaleFlag = HerdChangeReason.Born;
                            Resources.RuminantHerd().AddRuminant(newCalfRuminant, this);

                            // add to sucklings
                            female.SucklingOffspringList.Add(newCalfRuminant);
                            // this now reports for each individual born not a birth event as individual wean events are reported
                            female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Birth, female, Clock.Today));
                        }
                        female.UpdateBirthDetails();
                        this.Status = ActivityStatus.Success;
                    }
                }

                numberPossible = -1;
                if (!UseAI)
                {
                    numberPossible = 0;
                    // uncontrolled conception
                    if (location.GroupBy(a => a.Gender).Count() == 2)
                    {
                        int maleCount = location.Where(a => a.Gender == Sex.Male).Count();
                        int femaleCount = location.Where(a => a.Gender == Sex.Female).Count();
                        numberPossible = Convert.ToInt32(Math.Ceiling(maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30), CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    // controlled mating (AI)
                    if(this.TimingOK)
                    {
                        numberPossible = Convert.ToInt32(limiter * location.Where(a => a.Gender == Sex.Female).Count(), CultureInfo.InvariantCulture);
                    }
                }

                numberServiced = 1;
                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => !a.IsPregnant & a.Age <= a.BreedParams.MaximumAgeMating).ToList())
                {
                    Reporting.ConceptionStatus status = Reporting.ConceptionStatus.NotMated;
                    if (numberServiced <= numberPossible)
                    {
                        // calculate conception
                        double conceptionRate = ConceptionRate(female, out status);
                        if (conceptionRate > 0)
                        {
                            if (RandomNumberGenerator.Generator.NextDouble() <= conceptionRate)
                            {
                                female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, 0);
                                status = Reporting.ConceptionStatus.Conceived;
                            }
                        }
                        numberServiced++;
                        this.Status = ActivityStatus.Success;
                    }

                    // report change in breeding status
                    // do not report for -1 (controlled mating outside timing)
                    if (numberPossible >= 0 && status != Reporting.ConceptionStatus.NotAvailable)
                    {
                        female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(status, female, Clock.Today));
                    }
                }

                // report a natural mating locations for transparency via a message
                if (this.Status == ActivityStatus.Success && !UseAI)
                {
                    string warning = "Natural (uncontrolled) mating ocurred in [r=" + location.Key + "]";
                    if (!Warnings.Exists(warning))
                    {
                        Warnings.Add(warning);
                        Summary.WriteMessage(this, warning);
                    }
                }


            }
        }

        /// <summary>
        /// Calculate conception rate for a female
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <param name="status">Returns conception status</param>
        /// <returns></returns>
        private double ConceptionRate(RuminantFemale female, out Reporting.ConceptionStatus status)
        {
            bool isConceptionReady = false;
            status = Reporting.ConceptionStatus.NotAvailable;
            if (!female.IsPregnant)
            {
                status = Reporting.ConceptionStatus.NotReady;
                if (female.Age >= female.BreedParams.MinimumAge1stMating && female.NumberOfBirths == 0)
                {
                    isConceptionReady = true;
                }
                else
                {
                    // add one to age to ensure that conception is due this timestep
                    if ((female.Age + 1 - female.AgeAtLastBirth) * 30.4 > female.BreedParams.MinimumDaysBirthToConception)
                    {
                        // only based upon period since birth
                        isConceptionReady = true;

                        // DEVELOPMENT NOTE:
                        // The following IPI calculation and check present in NABSA has been removed for testing
                        // It is assumed that the individual based model with weight influences will handle the old IPI calculation 
                        // These parameters can now be removed form the RuminantType list
                        //double currentIPI = female.BreedParams.InterParturitionIntervalIntercept * Math.Pow(female.ProportionOfNormalisedWeight, female.BreedParams.InterParturitionIntervalCoefficient) * 30.4;
                        //double ageNextConception = female.AgeAtLastConception + (currentIPI / 30.4);
                        //isConceptionReady = (female.Age+1 >= ageNextConception);
                    }
                }
            }

            // if first mating and of age or sufficient time since last birth
            if(isConceptionReady)
            {
                status = Reporting.ConceptionStatus.Unsuccessful;

                // Get conception rate from conception model associated with the Ruminant Type parameters
                if (female.BreedParams.ConceptionModel == null)
                {
                    throw new ApsimXException(this, String.Format("No conception details were found for [r={0}]\r\nPlease add a conception component below the [r=RuminantType]", female.BreedParams.Name));
                }
                return female.BreedParams.ConceptionModel.ConceptionRate(female);
            }
            return 0;
        }

        /// <summary>
        /// Private method to determine resources required for this activity in the current month
        /// This method is local to this activity and not called with CLEMGetResourcesRequired event
        /// </summary>
        /// <returns>List of required resource requests</returns>
        private List<ResourceRequest> GetResourcesNeededForActivityLocal()
        {
            ResourceRequestList = null;

            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            // get only breeders for labour calculations
            List<Ruminant> herd = CurrentHerd(true).Where(a => a.Gender == Sex.Female &&
                            a.IsBreedingCondition).ToList();
            int head = herd.Count();
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);

            if (head == 0)
            {
                return null;
            }

            // get all fees for breeding
            foreach (RuminantActivityFee item in this.FindAllChildren<RuminantActivityFee>())
            {
                if (ResourceRequestList == null)
                {
                    ResourceRequestList = new List<ResourceRequest>();
                }

                double sumneeded = 0;
                switch (item.PaymentStyle)
                {
                    case AnimalPaymentStyleType.Fixed:
                        sumneeded = item.Amount;
                        break;
                    case AnimalPaymentStyleType.perHead:
                        sumneeded = head * item.Amount;
                        break;
                    case AnimalPaymentStyleType.perAE:
                        sumneeded = adultEquivalents * item.Amount;
                        break;
                    default:
                        throw new Exception(String.Format("PaymentStyle ({0}) is not supported for ({1}) in ({2})", item.PaymentStyle, item.Name, this.Name));
                }
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = sumneeded,
                    ResourceType = typeof(Finance),
                    ResourceTypeName = item.BankAccountName.Split('.').Last(),
                    ActivityModel = this,
                    FilterDetails = null,
                    Category = item.Name
                }
                );
            }

            // for each labour item specified
            foreach (var item in labour)
            {
                double daysNeeded = 0;
                switch (item.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = item.LabourPerUnit;
                        break;
                    case LabourUnitType.perHead:
                        daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
                        break;
                    case LabourUnitType.perAE:
                        daysNeeded = Math.Ceiling(adultEquivalents / item.UnitSize) * item.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                }
                if (daysNeeded > 0)
                {
                    if (ResourceRequestList == null)
                    {
                        ResourceRequestList = new List<ResourceRequest>();
                    }

                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = daysNeeded,
                        ResourceType = typeof(Labour),
                        ResourceTypeName = "",
                        ActivityModel = this,
                        FilterDetails = new List<object>() { item }
                    }
                    );
                }
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Property to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree
        /// </summary>
        /// <returns>T/F</returns>
        public override bool TimingOK
        {
            get
            {
                return (!UseAI) ? true : base.TimingOK;
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (UseAI)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Using Artificial insemination");
                    htmlWriter.Write("</div>");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("This simulation uses natural (uncontrolled) mating");
                    htmlWriter.Write("</div>");
                }
                if (InferStartupPregnancy)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Pregnancy status of breeders from matings prior to simulation start will be predicted");
                    htmlWriter.Write("</div>");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("No pregnancy of breeders from matings prior to simulation start is inferred");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
