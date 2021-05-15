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
using Newtonsoft.Json;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant breeding activity</summary>
    /// <summary>This activity provides all functionality for ruminant breeding up until natural weaning</summary>
    /// <summary>It will be applied to the supplied herd if males and females are located together</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the breeding of ruminants based upon the current herd filtering.")]
    [Version(1, 0, 8, "Include passing inherited attributes from mating to newborn")]
    [Version(1, 0, 7, "Removed UseAI to a new ControlledMating add-on activity")]
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
        Clock Clock = null;

        /// <summary>
        /// Artificial insemination in use (defined by presence of add-on component)
        /// </summary>
        private bool useControlledMating { get { return (controlledMating != null); }  }

        private RuminantActivityControlledMating controlledMating = null;

        /// <summary>
        /// Records the nuber of individuals that conceived in the BreedingEvent for sub-components to work with.
        /// </summary>
        public int NumberConceived { get; set; }

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

            controlledMating = this.FindAllChildren<RuminantActivityControlledMating>().FirstOrDefault();

            // Assignment of mothers was moved to RuminantHerd resource to ensure this is done even if no breeding activity is included
            this.InitialiseHerd(false, true);

            // report what is happening with timing when uncontrolled mating
            if (!useControlledMating & this.TimingExists)
            {
                Summary.WriteWarning(this, $"Uncontrolled/natural breeding should occur every month. The timer associated with [a={this.Name}] may restrict uncontrolled mating regardless of whether males and females of breeding condition are located together.\r\nYou can also seperate genders by moving to different paddocks to manage the timing of natural mating or add a [a=RuminantActivityControlledMating] component to define controlled mating");
            }

            // work out pregnancy status of initial herd
            if (InferStartupPregnancy)
            {
                // set up pre start conception status of breeders
                List<Ruminant> herd = CurrentHerd(true);

                // only initialise if herd present
                if (herd.Count() > 0)
                {
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
                        if (breedersCount > 0 & (!useControlledMating || this.TimingCheck(previousDate)))
                        {
                            int numberPossible = breedersCount;
                            int numberServiced = 1;
                            double limiter = 1;
                            List<Ruminant> maleBreeders = new List<Ruminant>();

                            // for each location where parts of this herd are located
                            foreach (var location in breeders)
                            {
                                // uncontrolled conception
                                if (!useControlledMating)
                                {
                                    // check if males and females of breeding condition are together
                                    if (location.GroupBy(a => a.Gender).Count() == 2)
                                    {
                                        // servicing rate
                                        int maleCount = location.Where(a => a.Gender == Sex.Male).Count();
                                        // get a list of males to provide attributes when incontrolled mating.
                                        if (maleCount > 0 && location.FirstOrDefault().BreedParams.IncludedAttributeInheritanceWhenMating)
                                        {
                                            maleBreeders = location.Where(a => a.Gender == Sex.Male).ToList();
                                        }

                                        int femaleCount = location.Where(a => a.Gender == Sex.Female).Count();
                                        double matingsPossible = maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30;

                                        double maleLimiter = Math.Min(1, matingsPossible / femaleCount);

                                        // only get non-pregnant females of breeding age at the time before the simulation included
                                        var availableBreeders = location.Where(b => b.Gender == Sex.Female && b.Age + i >= b.BreedParams.MinimumAge1stMating)
                                            .Cast<RuminantFemale>().Where(a => !a.IsPregnant).ToList();

                                        // only get selection of these of breeders available to spread conceptions
                                        // only 15% of breeding herd of age can conceive in any month or male limited proportion whichever is smaller
                                        int count = Convert.ToInt32(Math.Ceiling(availableBreeders.Count() * Math.Min(0.15, maleLimiter)), CultureInfo.InvariantCulture);
                                        availableBreeders = availableBreeders.OrderBy(x => RandomNumberGenerator.Generator.NextDouble()).Take(count).ToList();

                                        foreach (RuminantFemale female in availableBreeders)
                                        {
                                            // calculate conception
                                            Reporting.ConceptionStatus status = Reporting.ConceptionStatus.NotMated;
                                            double conceptionRate = ConceptionRate(female, out status);
                                            if (RandomNumberGenerator.Generator.NextDouble() <= conceptionRate)
                                            {
                                                female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, i);

                                                // if mandatory attributes are present in the herd, save male value with female details.
                                                if (female.BreedParams.IncludedAttributeInheritanceWhenMating)
                                                {
                                                    // randomly select male as father
                                                    AddMalesAttributeDetails(female, maleBreeders[RandomNumberGenerator.Generator.Next(0, maleBreeders.Count() - 1)]);
                                                }

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

                                                    // if mandatory attributes are present in the herd, save male value with female details.
                                                    if (female.BreedParams.IncludedAttributeInheritanceWhenMating)
                                                    {
                                                        // save all male attributes
                                                        AddMalesAttributeDetails(female, controlledMating.SireAttributes);
                                                    }

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

        }

        /// <summary>An event handler to perform herd breeding </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            this.Status = ActivityStatus.NotNeeded;
            NumberConceived = 0;

            // get list of all pregnant females
            List<RuminantFemale> pregnantherd = CurrentHerd(true).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsPregnant).ToList();

            // determine all fetus and newborn mortality of all pregnant females.
            foreach (RuminantFemale female in pregnantherd)
            {
                // calculate fetus and newborn mortality 
                // total mortality / (gestation months + 1) to get monthly mortality
                // done here before births to account for post birth motality as well..
                // IsPregnant status does not change until births occur in next section so will include mortality in month of birth
                // needs to be calculated for each offspring carried.
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

                if (female.BirthDue)
                {
                    int numberOfNewborn = female.CarryingCount;
                    for (int i = 0; i < numberOfNewborn; i++)
                    {
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

                        // add attributes inherited from mother
                        foreach (var attribute in female.Attributes)
                        {
                            newCalfRuminant.AddAttribute(attribute.Key, attribute.Value.GetInheritedAttribute() as ICLEMAttribute);
                        }

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

            // Perform breeding
            IEnumerable<Ruminant> herd = null;
            if (useControlledMating && controlledMating.TimingOK)
            {
                // determined by controlled mating and subsequent timer (e.g. smart milking)
                herd = controlledMating.BreedersToMate();
                this.TriggerOnActivityPerformed();
            }
            else if (!useControlledMating && TimingOK)
            {
                // whole herd for activity
                herd = CurrentHerd(true);
                // report that this activity was performed as it does not use base GetResourcesRequired
                this.TriggerOnActivityPerformed();
            }

            if (herd != null && herd.Count() > 0)
            {
                // group by location
                var breeders = from ind in herd
                               where ind.IsAbleToBreed
                               group ind by ind.Location into grp
                               select grp;

                int breedersCount = breeders.Count();
                int numberPossible = breedersCount;
                int numberServiced = 1;
                List<Ruminant> maleBreeders = new List<Ruminant>();

                // for each location where parts of this herd are located
                foreach (var location in breeders)
                {
                    numberPossible = -1;
                    if (useControlledMating)
                    {
                        numberPossible = Convert.ToInt32(location.Where(a => a.Gender == Sex.Female).Count(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        numberPossible = 0;
                        // uncontrolled conception
                        if (location.GroupBy(a => a.Gender).Count() == 2)
                        {
                            int maleCount = location.Where(a => a.Gender == Sex.Male).Count();
                            // get a list of males to provide attributes when incontrolled mating.
                            if(maleCount > 0 && location.FirstOrDefault().BreedParams.IncludedAttributeInheritanceWhenMating)
                            {
                                maleBreeders = location.Where(a => a.Gender == Sex.Male).ToList();
                            }
                            int femaleCount = location.Where(a => a.Gender == Sex.Female).Count();
                            numberPossible = Convert.ToInt32(Math.Ceiling(maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30), CultureInfo.InvariantCulture);
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
                                    
                                    // if mandatory attributes are present in the herd, save male value with female details.
                                    if(female.BreedParams.IncludedAttributeInheritanceWhenMating)
                                    {
                                        if(useControlledMating)
                                        {
                                            // save all male attributes
                                            AddMalesAttributeDetails(female, controlledMating.SireAttributes);
                                        }
                                        else
                                        {
                                            // randomly select male
                                            AddMalesAttributeDetails(female, maleBreeders[RandomNumberGenerator.Generator.Next(0,maleBreeders.Count()-1)]);
                                        }
                                    }
                                    status = Reporting.ConceptionStatus.Conceived;
                                    NumberConceived++;
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
                    if (numberServiced > 0 & !useControlledMating)
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
        }

        /// <summary>
        /// A method to add the available male attributes to the female store at mating using attributes supplied by controlled mating 
        /// </summary>
        /// <param name="female">The female breeder successfully mated</param>
        /// <param name="maleAttributes">a list of available male attributes setters</param>
        private void AddMalesAttributeDetails(RuminantFemale female, List<SetAttributeWithValue> maleAttributes)
        {
            foreach (var attribute in female.Attributes)
            {
                var maleAttribute = maleAttributes.Where(a => a.AttributeName == attribute.Key).FirstOrDefault();
                if(maleAttribute != null)
                {
                    var calculatedAttribute = maleAttribute.GetRandomSetAttribute();
                    if(attribute.Value.InheritanceStyle != calculatedAttribute.InheritanceStyle)
                    {
                        throw new ApsimXException(this, $"The inheritance style for attribute [{attribute.Key}] differs between the breeder and attributes supplied by controlled mating in [a={this.Name}]");
                    }
                    attribute.Value.storedMateValue = calculatedAttribute.storedValue;
                }
                else
                {
                    attribute.Value.storedMateValue = null;
                    if(female.BreedParams.IsMandatoryAttribute(attribute.Key))
                    {
                        throw new ApsimXException(this, $"The sire attributes provided for [a={this.Name}] do not include the madatory attribute [{attribute.Key}]");
                    }
                }
            }
        }

        /// <summary>
        /// A method to add the male attributes to the female attribute store at mating
        /// </summary>
        /// <param name="female">The female breeder successfully mated</param>
        /// <param name="male">The mated male</param>
        private void AddMalesAttributeDetails(RuminantFemale female, Ruminant male)
        {
            if (male != null)
            {
                foreach (var attribute in female.Attributes)
                {
                    var maleAttribute = male.GetAttributeValue(attribute.Key);
                    if (maleAttribute != null)
                    {
                        if (attribute.Value.InheritanceStyle != maleAttribute.InheritanceStyle)
                        {
                            throw new ApsimXException(this, $"The inheritance style for attribute [{attribute.Key}] differs between the breeder and breeding male from the herd in [a={this.Name}]");
                        }
                        attribute.Value.storedMateValue = maleAttribute.storedValue;
                    }
                    else
                    {
                        attribute.Value.storedMateValue = null;
                        if (female.BreedParams.IsMandatoryAttribute(attribute.Key))
                        {
                            throw new ApsimXException(this, $"The attributes provided with the breeding male from the herd does not include the madatory attribute [{attribute.Key}] in [a={this.Name}]");
                        }
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
        /// Property to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree
        /// </summary>
        /// <returns>T/F</returns>
        public override bool TimingOK
        {
            get
            {
                return (useControlledMating) ? controlledMating.TimingOK:  base.TimingOK;
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

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
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
                controlledMating = this.FindAllChildren<RuminantActivityControlledMating>().FirstOrDefault();
                if (useControlledMating)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Breeding uses controlled mating as outlined in the component below");
                    htmlWriter.Write("</div>");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("This simulation uses natural (uncontrolled) mating");

                    if (this.FindAllChildren<IActivityTimer>().Count() >= 1)
                    {
                        htmlWriter.Write(". The timer associated with this activity may restrict uncontrolled mating regardless of whether males and females of breeding condition are located together.");
                    }
                    else
                    {
                        htmlWriter.Write(" that will occur every month when males and females of breeding condition are located together.");
                    }
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
