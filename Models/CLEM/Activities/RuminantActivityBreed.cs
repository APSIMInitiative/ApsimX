using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.CLEM.Groupings;
using System.Xml;

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
    [Description("Manages the breeding of ruminants based on the current herd filtering")]
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
        private IClock clock = null;

        /// <summary>
        /// Artificial insemination in use (defined by presence of add-on component)
        /// </summary>
        private bool useControlledMating { get { return (controlledMating != null); }  }

        private RuminantActivityControlledMating controlledMating = null;
        private ConceptionStatusChangedEventArgs conceptionArgs = new ConceptionStatusChangedEventArgs();

        /// <summary>
        /// Records the number of individuals that conceived in the BreedingEvent for sub-components to work with.
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
                Summary.WriteMessage(this, $"Uncontrolled/natural breeding should occur every month. The timer associated with [a={this.Name}] may restrict uncontrolled mating regardless of whether males and females of breeding condition are located together.\r\nYou can also seperate genders by moving to different paddocks to manage the timing of natural mating or add a [a=RuminantActivityControlledMating] component to define controlled mating", MessageType.Warning);

            // set up pre start conception status of breeders
            IEnumerable<Ruminant> herd = CurrentHerd(true);

            // report sucklings birth and conception for startup mothers.
            foreach (RuminantFemale female in herd.OfType<RuminantFemale>().Where(a => a.SucklingOffspringList.Any()))
            {
                // report conception status changed from those identified suckling at startup
                DateTime dte = clock.StartDate.AddMonths(Convert.ToInt32(female.AgeAtLastConception - female.Age));
                dte = new DateTime(dte.Year, dte.Month, DateTime.DaysInMonth(dte.Year, dte.Month));
                conceptionArgs.Update(ConceptionStatus.Conceived, female, dte, calculateFromAge:false);
                female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                //dte = clock.StartDate.AddDays(-1).AddMonths(Convert.ToInt32(female.AgeAtLastBirth - female.Age));
                //dte = new DateTime(dte.Year, dte.Month, DateTime.DaysInMonth(dte.Year, dte.Month));
                conceptionArgs.Update(ConceptionStatus.Birth, female, dte, calculateFromAge: false);
                female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
            }

            // report previous pregnancies as conceptions
            foreach (RuminantFemale female in herd.OfType<RuminantFemale>().Where(a => a.IsPregnant))
            {
                // report conception status changed from those identified pregnant at startup
                // ToDo: spread over gestation period. Currently all in one month... doesn't really matter
                conceptionArgs.Update(ConceptionStatus.Conceived, female, clock.Today);
                female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
            }

            // work out pregnancy status of initial herd
            if (InferStartupPregnancy)
            {
                // only initialise if herd present
                if (herd.Any())
                {
                    // go back (gestation - 1) months
                    // this won't include those individuals due to give birth on day 1.

                    int monthsAgoStart = 0 - (Convert.ToInt32(Math.Truncate(herd.FirstOrDefault().BreedParams.GestationLength), CultureInfo.InvariantCulture) - 1);
                    int monthsAgoStop = -1;

                    for (int i = monthsAgoStart; i <= monthsAgoStop; i++)
                    {
                        DateTime previousDate = clock.Today.AddMonths(i);
                        DateTime conceiveDate = clock.Today.AddMonths(i);
                        conceiveDate = new DateTime(conceiveDate.Year, conceiveDate.Month, DateTime.DaysInMonth(conceiveDate.Year, conceiveDate.Month));

                        // get list of all individuals of breeding age and condition
                        // grouped by location
                        var breeders = from ind in herd
                                       where
                                       (ind.Sex == Sex.Male && ind.Age + i >= ind.BreedParams.MinimumAge1stMating) ||
                                       (ind.Sex == Sex.Female &&
                                       ind.Age + i >= ind.BreedParams.MinimumAge1stMating &&
                                       !(ind as RuminantFemale).IsPregnant
                                       )
                                       group ind by ind.Location into grp
                                       select grp;

                        // must be breeders to bother checking any further
                        // must be either uncontrolled mating or the timing of controlled mating
                        if (breeders.Count() > 0 & (!useControlledMating || controlledMating.TimingCheck(previousDate)))
                        {
                            int numberPossible = 0;
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
                                    if (location.GroupBy(a => a.Sex).Count() == 2)
                                    {
                                        // servicing rate
                                        int maleCount = location.Where(a => a.Sex == Sex.Male).Count();
                                        // get a list of males to provide attributes when incontrolled mating.
                                        if (maleCount > 0 && location.FirstOrDefault().BreedParams.IncludedAttributeInheritanceWhenMating)
                                            maleBreeders = location.Where(a => a.Sex == Sex.Male).ToList();

                                        int femaleCount = location.OfType<RuminantFemale>().Count();
                                        double matingsPossible = maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30;

                                        double maleLimiter = Math.Min(1, matingsPossible / femaleCount);

                                        // only get non-pregnant females of breeding age at the time before the simulation included
                                        var availableBreeders = location.OfType<RuminantFemale>().Where(b => !b.IsPregnant && b.Age + i >= b.BreedParams.MinimumAge1stMating);

                                        // only get selection of these of breeders available to spread conceptions
                                        // only 15% of breeding herd of age can conceive in any month or male limited proportion whichever is smaller
                                        int count = Convert.ToInt32(Math.Ceiling(availableBreeders.Count() * Math.Min(0.15, maleLimiter)), CultureInfo.InvariantCulture);
                                        availableBreeders = availableBreeders.OrderBy(x => RandomNumberGenerator.Generator.NextDouble()).Take(count);

                                        foreach (RuminantFemale female in availableBreeders)
                                        {
                                            // calculate conception
                                            Reporting.ConceptionStatus status = Reporting.ConceptionStatus.NotMated;
                                            double conceptionRate = ConceptionRate(female, out status);
                                            if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), conceptionRate))
                                            {
                                                female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, i);
                                                female.LastMatingStyle = MatingStyle.PreSimulation;

                                                // if mandatory attributes are present in the herd, save male value with female details.
                                                if (female.BreedParams.IncludedAttributeInheritanceWhenMating)
                                                    // randomly select male as father
                                                    AddMalesAttributeDetails(female, maleBreeders[RandomNumberGenerator.Generator.Next(0, maleBreeders.Count() - 1)]);

                                                // report conception status changed
                                                conceptionArgs.Status = ConceptionStatus.Conceived;
                                                conceptionArgs.Female = female;

                                                conceptionArgs.Update(ConceptionStatus.Conceived, female, conceiveDate, null, false);
                                                female.BreedParams.OnConceptionStatusChanged(conceptionArgs);

                                                // check for perinatal mortality
                                                // Todo: match functionality of controlled below that was made to work with i and j to give month
                                                for (int j = i; j < monthsAgoStop; j++)
                                                {
                                                    DateTime lossDate = clock.Today.AddMonths(i);
                                                    lossDate = new DateTime(lossDate.Year, lossDate.Month, DateTime.DaysInMonth(lossDate.Year, lossDate.Month));

                                                    for (int k = 0; k < female.CarryingCount; k++)
                                                    {
                                                        if (MathUtilities.IsLessThan(RandomNumberGenerator.Generator.NextDouble(), female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                                                        {
                                                            female.OneOffspringDies();
                                                            if (female.NumberOfOffspring == 0)
                                                            {
                                                                // report conception status changed when last multiple birth dies.
                                                                conceptionArgs.Update(ConceptionStatus.Failed, female, lossDate, null, false);
                                                                female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            //else
                                            //{
                                            //    // report conception status changed when last multiple birth dies.
                                            //    // ToDo: not sure this is working.. Do we need an unsuccessful tag in history
                                            //    conceptionArgs.Update(ConceptionStatus.Unsuccessful, female, conceiveDate, null, false);
                                            //    female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                                            //    //female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Unsuccessful, female, conceiveDate));
                                            //}
                                        }
                                    }
                                }
                                // controlled conception
                                else
                                {
                                    numberPossible = Convert.ToInt32(limiter * location.OfType<RuminantFemale>().Count(), CultureInfo.InvariantCulture);
                                    foreach (RuminantFemale female in location.OfType<RuminantFemale>())
                                    {
                                        if (!female.IsPregnant && MathUtilities.IsGreaterThanOrEqual((female.Age - female.AgeAtLastBirth) * 30.4, female.BreedParams.MinimumDaysBirthToConception))
                                        {
                                            // calculate conception
                                            ConceptionStatus status = ConceptionStatus.NotMated;
                                            double conceptionRate = ConceptionRate(female, out status);
                                            if (numberServiced <= numberPossible) // labour/finance limited number
                                            {
                                                if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), conceptionRate))
                                                {
                                                    female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, i);
                                                    female.LastMatingStyle = MatingStyle.Controlled;

                                                    // if mandatory attributes are present in the herd, save male value with female details.
                                                    if (female.BreedParams.IncludedAttributeInheritanceWhenMating)
                                                        // save all male attributes
                                                        AddMalesAttributeDetails(female, controlledMating.SireAttributes);

                                                    // report conception status changed
                                                    conceptionArgs.Update(ConceptionStatus.Conceived, female, conceiveDate, null, false);
                                                    female.BreedParams.OnConceptionStatusChanged(conceptionArgs);

                                                    // check for perenatal mortality
                                                    for (int j = i+1; j < monthsAgoStop; j++)
                                                    {
                                                        DateTime lossDate = conceiveDate.AddMonths(j-i);
                                                        lossDate = new DateTime(lossDate.Year, lossDate.Month, DateTime.DaysInMonth(lossDate.Year, lossDate.Month));

                                                        for (int k = 0; k < female.CarryingCount; k++)
                                                        {
                                                            if (MathUtilities.IsLessThan(RandomNumberGenerator.Generator.NextDouble(), female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                                                            {
                                                                female.OneOffspringDies();
                                                                if (female.NumberOfOffspring == 0)
                                                                {
                                                                    // report conception status changed when last multiple birth dies.
                                                                    conceptionArgs.Update(ConceptionStatus.Failed, female, lossDate, null, false);
                                                                    female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
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

        /// <summary>Function to determine naturally wean individuals at start of timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // Reset all activity determined conception rates
            if(useControlledMating)
                _ = GetIndividuals<RuminantFemale>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => a.IsBreeder).Select(a => a.ActivityDeterminedConceptionRate == null);
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
            List<RuminantFemale> pregnantherd = CurrentHerd(true).OfType<RuminantFemale>().Where(a => a.IsPregnant).ToList();

            // determine all fetus and newborn mortality of all pregnant females.
            bool preglost = false;
            bool birthoccurred = false;
            foreach (RuminantFemale female in pregnantherd)
            {

                // calculate fetus and newborn mortality
                // total mortality / (gestation months + 1) to get monthly mortality
                // done here before births to account for post birth motality as well..
                // IsPregnant status does not change until births occur in next section so will include mortality in month of birth
                // needs to be calculated for each offspring carried.

                for (int i = 0; i < female.CarryingCount; i++)
                {
                    var rnd = RandomNumberGenerator.Generator.NextDouble();
                    if (MathUtilities.IsLessThan(rnd, female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                    {
                        preglost = true;
                        female.OneOffspringDies();
                        if (female.NumberOfOffspring == 0)
                        {
                            // report conception status changed when last multiple birth dies.
                            conceptionArgs.Update(ConceptionStatus.Failed, female, clock.Today);
                            female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                        }
                    }
                }

                if (female.BirthDue)
                {
                    birthoccurred = true;
                    int numberOfNewborn = female.CarryingCount;
                    for (int i = 0; i < numberOfNewborn; i++)
                    {
                        bool isMale = RandomNumberGenerator.Generator.NextDouble() <= female.BreedParams.ProportionOffspringMale;
                        Sex sex = isMale ? Sex.Male : Sex.Female;
                        double weight = female.BreedParams.SRWBirth * female.StandardReferenceWeight * (1 - 0.33 * (1 - female.Weight / female.StandardReferenceWeight));

                        Ruminant newSucklingRuminant = Ruminant.Create(sex, female.BreedParams, 0, weight);
                        newSucklingRuminant.HerdName = female.HerdName;
                        newSucklingRuminant.Breed = female.BreedParams.Breed;
                        newSucklingRuminant.ID = HerdResource.NextUniqueID;
                        newSucklingRuminant.Location = female.Location;
                        newSucklingRuminant.Mother = female;
                        newSucklingRuminant.Number = 1;
                        // suckling/calf weight from Freer
                        newSucklingRuminant.PreviousWeight = newSucklingRuminant.Weight;
                        newSucklingRuminant.SaleFlag = HerdChangeReason.Born;

                        // add attributes inherited from mother
                        foreach (var attribute in female.Attributes.Items.Where(a => a.Value is not null))
                            newSucklingRuminant.AddInheritedAttribute(attribute); // .Attributes.Add(attribute.Key, attribute.Value.GetInheritedAttribute() as IIndividualAttribute);

                        HerdResource.AddRuminant(newSucklingRuminant, this);

                        // add to sucklings
                        female.SucklingOffspringList.Add(newSucklingRuminant);
                        // this now reports for each individual born not a birth event as individual wean events are reported

                        conceptionArgs.Update(ConceptionStatus.Birth, female, clock.Today);
                        female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                        //female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Birth, female, clock.Today));
                    }
                    female.UpdateBirthDetails();
                }
            }

            if (preglost)
            {
                AddStatusMessage("Lost pregnancy");
                this.Status = ActivityStatus.Success;
            }
            if (birthoccurred)
            {
                AddStatusMessage("Births occurred");
                this.Status = ActivityStatus.Success;
            }

            var filters = GetCompanionModelsByIdentifier<RuminantGroup>(false, true, "SelectBreedersAvailable");

            // Perform breeding
            IEnumerable<Ruminant> herd = null;
            if (useControlledMating && controlledMating.TimingOK)
                // determined by controlled mating and subsequent timer (e.g. smart milking)
                herd = controlledMating.BreedersToMate();
            else if (!useControlledMating && TimingOK)
                // whole herd for activity including males
                herd = CurrentHerd(true);

            if (herd != null && herd.Any())
            {
                // group by location
                var breeders = from ind in herd
                               where ind.IsAbleToBreed
                               group ind by ind.Location into grp
                               select grp;

                // identify not ready for reporting and tracking
                var notReadyBreeders = herd.Where(a => a.Sex == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsBreeder && !a.IsAbleToBreed && !a.IsPregnant);
                foreach (RuminantFemale female in notReadyBreeders)
                {
                    conceptionArgs.Update(ConceptionStatus.NotReady, female, clock.Today);
                    female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                }

                int numberPossible = breeders.Sum(a => a.Count());
                int numberServiced = 1;
                List<Ruminant> maleBreeders = new List<Ruminant>();

                // for each location where parts of this herd are located
                bool breedoccurred = false;
                foreach (var location in breeders)
                {
                    numberPossible = -1;
                    if (useControlledMating)
                        numberPossible = Convert.ToInt32(location.OfType<RuminantFemale>().Count(), CultureInfo.InvariantCulture);
                    else
                    {
                        numberPossible = 0;
                        // uncontrolled conception
                        if (location.GroupBy(a => a.Sex).Count() == 2)
                        {
                            int maleCount = location.OfType<RuminantMale>().Count();
                            // get a list of males to provide attributes when in controlled mating.
                            if(maleCount > 0 && location.FirstOrDefault().BreedParams.IncludedAttributeInheritanceWhenMating)
                                maleBreeders = location.Where(a => a.Sex == Sex.Male).ToList();

                            int femaleCount = location.Where(a => a.Sex == Sex.Female).Count();
                            numberPossible = Convert.ToInt32(Math.Ceiling(maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30), CultureInfo.InvariantCulture);
                        }
                    }

                    numberServiced = 0;
                    lastJoinIndex = -1;
                    int cnt = 0;
                    // shuffle the not pregnant females when obtained to avoid any inherant order by creation of individuals affecting which individuals are available first
                    var notPregnantFemales = location.OfType<RuminantFemale>().Where(a => !a.IsPregnant).OrderBy(a => RandomNumberGenerator.Generator.Next()).ToList();
                    int totalToBreed = notPregnantFemales.Count;
                    while(cnt < totalToBreed)
                    {
                        RuminantFemale female = notPregnantFemales.ElementAt(cnt);
                        ConceptionStatus status = ConceptionStatus.NotMated;
                        if (numberServiced < numberPossible)
                        {
                            double conceptionRate = 0;

                            if (female.ActivityDeterminedConceptionRate != null)
                                // If an activity controlled mating has previously determined conception rate and saved it (it will not be null if mated)
                                // This conception rate can be used instead of determining conception here.
                                conceptionRate = female.ActivityDeterminedConceptionRate ?? 0;
                            else
                                // calculate conception
                                conceptionRate = ConceptionRate(female, out status);

                            // if mandatory attributes are present in the herd, save male value with female details.
                            // update male for both successful and failed matings (next if statement
                            if (female.BreedParams.IncludedAttributeInheritanceWhenMating)
                            {
                                object male = null;
                                if (useControlledMating)
                                {
                                    bool newJoining = needsNewJoiningMale(controlledMating.JoiningsPerMale, numberServiced);
                                    // save all male attributes
                                    AddMalesAttributeDetails(female, controlledMating.SireAttributes, newJoining);
                                }
                                else
                                {
                                    male = maleBreeders[RandomNumberGenerator.Generator.Next(0, maleBreeders.Count() - 1)];
                                    female.LastMatingStyle = ((male as RuminantMale).IsWildBreeder ? MatingStyle.WildBreeder : MatingStyle.Natural);

                                    // randomly select male
                                    AddMalesAttributeDetails(female, male as Ruminant);
                                }
                            }

                            // conception rate will be -ve for unsuccessful matings from controlled mating. a value of 0 still represents not mated
                            if (Math.Abs(conceptionRate) > 0)
                            {
                                // if controlled mating (ActiDetConcepRate not null and rate > 0 then successful mating), otherwise compare with random and conception rate for natural mating.
                                //ActivitydeterminedConception rate > 0, otherwise rate calculated above versus the random number approach
                                if ((female.ActivityDeterminedConceptionRate != null)?conceptionRate > 0:RandomNumberGenerator.Generator.NextDouble() <= conceptionRate)
                                {
                                    female.UpdateConceptionDetails(female.CalulateNumberOfOffspringThisPregnancy(), conceptionRate, 0);
                                    conceptionArgs.Update(ConceptionStatus.Conceived, female, clock.Today);
                                    female.BreedParams.OnConceptionStatusChanged(conceptionArgs);

                                    if (useControlledMating)
                                        female.LastMatingStyle = MatingStyle.Controlled;

                                    status = ConceptionStatus.Conceived;
                                    NumberConceived++;
                                }
                                else
                                {
                                    status = ConceptionStatus.Unsuccessful;
                                }
                            }
                            numberServiced++;
                            breedoccurred = true;
                        }

                        // report change in breeding status
                        // do not report for -1 (controlled mating outside timing)
                        if (numberPossible >= 0 && status != ConceptionStatus.Conceived && status != ConceptionStatus.NotMated)
                        {
                            conceptionArgs.Update(status, female, clock.Today, null, false);
                            female.BreedParams.OnConceptionStatusChanged(conceptionArgs);
                            //female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(status, female, clock.Today));
                        }
                        cnt++;
                    }

                    // report a natural mating locations for transparency via a message
                    if (numberServiced > 0 & !useControlledMating)
                    {
                        string warning = $"Natural (uncontrolled) mating ocurred in [r={(location.Key ?? "Not specified - general yards")}]";
                        Warnings.CheckAndWrite(warning, Summary, this, MessageType.Information);
                    }
                }
                if(breedoccurred)
                {
                    this.Status = ActivityStatus.Success;
                    AddStatusMessage("Breeding occurred");
                }
            }
            // report that this activity was performed as it does not use base GetResourcesRequired
            this.TriggerOnActivityPerformed();
        }

        private int lastJoinIndex = 0;
        private bool needsNewJoiningMale(int joiningsPerMale, int numberServiced)
        {
            var index = Convert.ToInt32(Math.Floor(numberServiced/((joiningsPerMale==0)?1: joiningsPerMale) * 1.0));
            if (index == lastJoinIndex)
                return false;
            else
            {
                lastJoinIndex = index;
                return true;
            }
        }

        /// <summary>
        /// A method to add the available male attributes to the female store at mating using attributes supplied by controlled mating
        /// </summary>
        /// <param name="female">The female breeder successfully mated</param>
        /// <param name="maleAttributes">a list of available male attributes setters</param>
        /// <param name="newMale">Create new instance (T) or use last created (F)</param>
        private void AddMalesAttributeDetails(RuminantFemale female, List<ISetAttribute> maleAttributes, bool newMale = true)
        {
            foreach (var attribute in female.Attributes.Items)
            {
                var maleAttribute = maleAttributes.FirstOrDefault(a => a.AttributeName == attribute.Key);
                SetFemaleMateAttributes(female, attribute, maleAttribute?.GetAttribute(newMale));
            }
        }

        /// <summary>
        /// A method to add the male attributes to the female attribute store at mating
        /// </summary>
        /// <param name="female">The female breeder successfully mated</param>
        /// <param name="male">The mated male</param>
        private void AddMalesAttributeDetails(RuminantFemale female, Ruminant male)
        {
            if (male is null) return;

            foreach (var attribute in female.Attributes.Items)
            {
                var maleAttribute = male.Attributes.GetValue(attribute.Key);
                SetFemaleMateAttributes(female, attribute, maleAttribute);
            }
        }

        private void SetFemaleMateAttributes(RuminantFemale female, KeyValuePair<string, IIndividualAttribute> femaleAttribute, IIndividualAttribute maleAttribute)
        {
            if (maleAttribute != null)
            {
                if (femaleAttribute.Value != null && femaleAttribute.Value.InheritanceStyle != maleAttribute.InheritanceStyle)
                {
                    string errorMsg;
                    if (useControlledMating)
                        errorMsg = $"provided from [a={controlledMating.NameWithParent}]";
                    else
                        errorMsg = $"from the herd in [a={NameWithParent}]";
                    throw new ApsimXException(this, $"The inheritance style for attribute [{femaleAttribute.Key}] differs between the breeder [{femaleAttribute.Value.InheritanceStyle}] and breeding male [{maleAttribute.InheritanceStyle}] {errorMsg}");
                }

                if (femaleAttribute.Value != null)
                    femaleAttribute.Value.StoredMateValue = maleAttribute.StoredValue;
            }
            else
            {
                if (femaleAttribute.Value != null)
                    femaleAttribute.Value.StoredMateValue = null;
                if (female.BreedParams.IsMandatoryAttribute(femaleAttribute.Key))
                {
                    string errorMsg;
                    if (useControlledMating)
                        errorMsg = $"Cannot locate the madatory attribute [{femaleAttribute.Key}] in [a={controlledMating.NameWithParent}]{Environment.NewLine}Add a [SetAttribute] component below the [a=RuminantnActivityControlledMating]";
                    else
                        errorMsg = $"Cannot locate the madatory attribute [{femaleAttribute.Key}] in from the breeding male selected from the herd in [a={NameWithParent}]{Environment.NewLine}Ensure all sires in initial herd or purchased provide the appropriate [SetAttribute] component";
                    throw new ApsimXException(this, errorMsg);
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
                if (MathUtilities.IsGreaterThanOrEqual(female.Age, female.BreedParams.MinimumAge1stMating) && female.NumberOfBirths == 0)
                    isConceptionReady = true;
                else
                {
                    // add one to age to ensure that conception is due this timestep
                    if (MathUtilities.IsGreaterThan((female.Age + 1 - female.AgeAtLastBirth) * 30.4, female.BreedParams.MinimumDaysBirthToConception))
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
                    throw new ApsimXException(this, String.Format("No conception details were found for [r={0}]\r\nPlease add a conception component below the [r=RuminantType]", female.BreedParams.Name));

                return female.BreedParams.ConceptionModel.ConceptionRate(female);
            }
            return 0;
        }


        /// <inheritdoc/>
        public override bool TimingOK
        {
            get
            {
                return (useControlledMating) ? controlledMating.TimingOK:  base.TimingOK;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
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
                if (controlledMating is null)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("This simulation uses natural (uncontrolled) mating that will occur when males and females of breeding condition are located together");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString();
            }
        }
        #endregion
    }
}
