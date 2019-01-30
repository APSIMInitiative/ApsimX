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
    [Version(1, 0, 1, "")]
    public class RuminantActivityBreed : CLEMRuminantActivityBase
    {
        [Link]
        private List<LabourRequirement> labour;
        [Link]
        Clock Clock = null;
        /// <summary>
        /// Maximum conception rate for uncontrolled matings
        /// </summary>
        [Description("Maximum conception rate for uncontrolled matings")]
        [Required]
        public double MaximumConceptionRateUncontrolled { get; set; }

        /// <summary>
        /// Use artificial insemination (no bulls required)
        /// </summary>
        [Description("Use artificial insemination (no bulls required)")]
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
            // Assignment of mothers was moved to RuminantHerd resource to ensure this is done even if no breeding activity is included
            this.InitialiseHerd(false, true);

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourRequirement)).Cast<LabourRequirement>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
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
        }

        /// <summary>An event handler to allow us to make final changes just prior to simulation.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMinitialiseActivity(object sender, EventArgs e)
        {
            if (InferStartupPregnancy)
            {
                // set up pre start conception status of breeders
                List<Ruminant> herd = CurrentHerd(true);

                int aDay = Clock.Today.Year;

                // go back (gestation - 1) months
                // this won't include those individuals due to give birth on day 1.

                int monthsAgoStart = Clock.Today.Month - (Convert.ToInt32(Math.Truncate(herd.FirstOrDefault().BreedParams.GestationLength)) - 1);
                int monthsAgoStop = -1;

                for (int i = monthsAgoStart; i <= monthsAgoStop; i++)
                {
                    DateTime previousDate = Clock.Today.AddMonths(i);

                    // get list of all individuals of breeding age and condition
                    // grouped by location
                    var breeders = from ind in herd
                                   where
                                   (ind.Gender == Sex.Male & ind.Age + i >= ind.BreedParams.MinimumAge1stMating) ||
                                   (ind.Gender == Sex.Female &
                                   ind.Age + i >= ind.BreedParams.MinimumAge1stMating //&
                                                                                      // ind.Weight >= (ind.BreedParams.MinimumSize1stMating * ind.StandardReferenceWeight)
                                   )
                                   group ind by ind.Location into grp
                                   select grp;

                    int breedersCount = breeders.Count();
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
                                double maleLimiter = Math.Min(1.0, matingsPossible / femaleCount);

                                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                                {
                                    if (!female.IsPregnant && !female.IsLactating && (female.Age - female.AgeAtLastBirth) * 30.4 >= female.BreedParams.MinimumDaysBirthToConception)
                                    {
                                        // calculate conception
                                        double conceptionRate = ConceptionRate(female) * maleLimiter;
                                        conceptionRate = Math.Min(conceptionRate, MaximumConceptionRateUncontrolled);
                                        if (ZoneCLEM.RandomGenerator.NextDouble() <= conceptionRate)
                                        {
                                            female.UpdateConceptionDetails(ZoneCLEM.RandomGenerator.NextDouble() < female.BreedParams.TwinRate, conceptionRate, i);
                                        }
                                    }
                                }
                            }
                        }
                        // controlled conception
                        else
                        {
                            if (this.TimingCheck(previousDate))
                            {
                                numberPossible = Convert.ToInt32(limiter * location.Where(a => a.Gender == Sex.Female).Count());
                                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                                {
                                    if (!female.IsPregnant && !female.IsLactating && (female.Age - female.AgeAtLastBirth) * 30.4 >= female.BreedParams.MinimumDaysBirthToConception)
                                    {
                                        // calculate conception
                                        double conceptionRate = ConceptionRate(female);
                                        if (numberServiced <= numberPossible) // labour/finance limited number
                                        {
                                            if (ZoneCLEM.RandomGenerator.NextDouble() <= conceptionRate)
                                            {
                                                female.UpdateConceptionDetails(ZoneCLEM.RandomGenerator.NextDouble() < female.BreedParams.TwinRate, conceptionRate, i);
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
            List<Ruminant> herd = CurrentHerd(true); //ruminantHerd.Herd.Where(a => a.BreedParams.Name == HerdName).ToList();

            int aDay = Clock.Today.Year;

            // get list of all individuals of breeding age and condition
            // grouped by location
            var breeders = from ind in herd
                            where
                            (ind.Gender == Sex.Male & ind.Age >= ind.BreedParams.MinimumAge1stMating) ||
                            (ind.Gender == Sex.Female &
                            ind.Age >= ind.BreedParams.MinimumAge1stMating &
                            ind.Weight >= (ind.BreedParams.MinimumSize1stMating * ind.StandardReferenceWeight)
                            )
                            group ind by ind.Location into grp
                            select grp;

            // calculate labour and finance limitations if needed when doing AI
            int breedersCount = breeders.Count();
            int numberPossible = breedersCount;
            int numberServiced = 1;
            double limiter = 1;
            if (UseAI & TimingOK)
            {
                // attempt to get required resources
                List<ResourceRequest> resourcesneeded = GetResourcesNeededForActivityLocal();
                CheckResources(resourcesneeded, Guid.NewGuid());
                bool tookRequestedResources = TakeResources(resourcesneeded, true);
                // get all shortfalls
                if (tookRequestedResources & (ResourceRequestList != null))
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
            }

            // for each location where parts of this herd are located
            foreach (var location in breeders)
            {
                // determine all foetus and newborn mortality.
                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                {
                    if (female.IsPregnant)
                    {
                        // calculate foetus and newborn mortality 
                        // total mortality / (gestation months + 1) to get monthly mortality
                        // done here before births to account for post birth motality as well..
                        if (ZoneCLEM.RandomGenerator.NextDouble() < (female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1)))
                        {
                            female.OneOffspringDies();
                        }
                    }
                }

                // check for births of all pregnant females.
                foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                {
                    if (female.BirthDue)
                    {
                        female.WeightLossDueToCalf = 0;
                        int numberOfNewborn = (female.CarryingTwins) ? 2 : 1;
                        for (int i = 0; i < numberOfNewborn; i++)
                        {
                            // Foetal mortality is now performed each timestep at base of this method
                            object newCalf = null;
                            bool isMale = (ZoneCLEM.RandomGenerator.NextDouble() >= 0.5);
                            if (isMale)
                            {
                                newCalf = new RuminantMale();
                            }
                            else
                            {
                                newCalf = new RuminantFemale();
                            }
                            Ruminant newCalfRuminant = newCalf as Ruminant;
                            newCalfRuminant.Age = 0;
                            newCalfRuminant.HerdName = female.HerdName;
                            newCalfRuminant.BreedParams = female.BreedParams;
                            newCalfRuminant.Breed = female.BreedParams.Breed;
                            newCalfRuminant.Gender = (isMale) ? Sex.Male : Sex.Female;
                            newCalfRuminant.ID = Resources.RuminantHerd().NextUniqueID;
                            newCalfRuminant.Location = female.Location;
                            newCalfRuminant.Mother = female;
                            newCalfRuminant.Number = 1;
                            newCalfRuminant.SetUnweaned();
                            // calf weight from  Freer
                            newCalfRuminant.Weight = female.BreedParams.SRWBirth * female.StandardReferenceWeight * (1 - 0.33 * (1 - female.Weight / female.StandardReferenceWeight));
                            newCalfRuminant.HighWeight = newCalfRuminant.Weight;
                            newCalfRuminant.SaleFlag = HerdChangeReason.Born;
                            Resources.RuminantHerd().AddRuminant(newCalfRuminant);

                            // add to sucklings
                            female.SucklingOffspring.Add(newCalfRuminant);
                            // remove calf weight from female
                            female.WeightLossDueToCalf += newCalfRuminant.Weight;
                        }
                        female.UpdateBirthDetails();
                    }
                }
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
                        double maleLimiter = Math.Min(1.0, matingsPossible / femaleCount);

                        foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                        {
                            if (!female.IsPregnant && !female.IsLactating && (female.Age - female.AgeAtLastBirth) * 30.4 >= female.BreedParams.MinimumDaysBirthToConception)
                            {
                                // calculate conception
                                double conceptionRate = ConceptionRate(female) * maleLimiter;
                                conceptionRate = Math.Min(conceptionRate, MaximumConceptionRateUncontrolled);
                                if (ZoneCLEM.RandomGenerator.NextDouble() <= conceptionRate)
                                {
                                    female.UpdateConceptionDetails(ZoneCLEM.RandomGenerator.NextDouble() <= female.BreedParams.TwinRate, conceptionRate, 0);
                                }
                            }
                        }
                    }
                }
                // controlled conception
                else
                {
                    if (this.TimingOK)
                    {
                        numberPossible = Convert.ToInt32(limiter * location.Where(a => a.Gender == Sex.Female).Count());
                        foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
                        {
                            if (!female.IsPregnant && !female.IsLactating && (female.Age - female.AgeAtLastBirth) * 30.4 >= female.BreedParams.MinimumDaysBirthToConception)
                            {
                                // calculate conception
                                double conceptionRate = ConceptionRate(female);
                                if (numberServiced <= numberPossible) // labour/finance limited number
                                {
                                    if (ZoneCLEM.RandomGenerator.NextDouble() <= conceptionRate)
                                    {
                                        female.UpdateConceptionDetails(ZoneCLEM.RandomGenerator.NextDouble() <= female.BreedParams.TwinRate, conceptionRate, 0);
                                    }
                                    numberServiced++;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate conception rate for a female
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns></returns>
        private double ConceptionRate(RuminantFemale female)
        {
            double rate = 0;

            bool isConceptionReady = false;
            if (female.Age >= female.BreedParams.MinimumAge1stMating && female.NumberOfBirths == 0)
            {
                isConceptionReady = true;
            }
            else
            {
                double currentIPI = female.BreedParams.InterParturitionIntervalIntercept * Math.Pow((female.Weight / female.StandardReferenceWeight), female.BreedParams.InterParturitionIntervalCoefficient) * 30.64;
                // calculate inter-parturition interval
                currentIPI = Math.Max(currentIPI, female.BreedParams.GestationLength * 30.4 + female.BreedParams.MinimumDaysBirthToConception); // 2nd param was 61
                double ageNextConception = female.AgeAtLastConception + (currentIPI / 30.4);
                isConceptionReady = (female.Age >= ageNextConception);
            }

            // if first mating and of age or suffcient time since last birth/conception
            if(isConceptionReady)
            {
                // get advanced conception rate if available otherwise use defaults.
                if(female.BreedParams.AdvancedConceptionParameters != null)
                {
                    // generalised curve
                    switch (female.NumberOfBirths)
                    {
                        case 0:
                            // first mating
                            //if (female.BreedParams.MinimumAge1stMating >= 24)
                            if (female.Age >= 24)
                            {
                                // 1st mated at 24 months or older
                                rate = female.BreedParams.AdvancedConceptionParameters.ConceptionRateAsymptote[1] / (1 + Math.Exp(female.BreedParams.AdvancedConceptionParameters.ConceptionRateCoefficent[1] * female.Weight / female.StandardReferenceWeight + female.BreedParams.AdvancedConceptionParameters.ConceptionRateIntercept[1]));
                            }
                            //else if (female.BreedParams.MinimumAge1stMating >= 12)
                            else if (female.Age >= 12)
                            {
                                // 1st mated between 12 and 24 months
                                double rate24 = female.BreedParams.AdvancedConceptionParameters.ConceptionRateAsymptote[1] / (1 + Math.Exp(female.BreedParams.AdvancedConceptionParameters.ConceptionRateCoefficent[1] * female.Weight / female.StandardReferenceWeight + female.BreedParams.AdvancedConceptionParameters.ConceptionRateIntercept[1]));
                                double rate12 = female.BreedParams.AdvancedConceptionParameters.ConceptionRateAsymptote[0] / (1 + Math.Exp(female.BreedParams.AdvancedConceptionParameters.ConceptionRateCoefficent[0] * female.Weight / female.StandardReferenceWeight + female.BreedParams.AdvancedConceptionParameters.ConceptionRateIntercept[0]));
                                // interpolate, not just average
                                double propOfYear = (female.Age - 12) / 12;
                                rate = rate12 + ((rate24-rate12)*propOfYear);
                                //Concep_rate = ((730 - Anim_concep(rumcat)) * temp1 + (Anim_concep(rumcat) - 365) * temp2) / 365 ' interpolate between 12 & 24 months
                            }
                            else
                            {
                                // first mating < 12 months old
                                rate = female.BreedParams.AdvancedConceptionParameters.ConceptionRateAsymptote[0] / (1 + Math.Exp(female.BreedParams.AdvancedConceptionParameters.ConceptionRateCoefficent[0] * female.Weight / female.StandardReferenceWeight + female.BreedParams.AdvancedConceptionParameters.ConceptionRateIntercept[0]));
                            }
                            break;
                        case 1:
                            // second offspring mother
                            rate = female.BreedParams.AdvancedConceptionParameters.ConceptionRateAsymptote[2] / (1 + Math.Exp(female.BreedParams.AdvancedConceptionParameters.ConceptionRateCoefficent[2] * female.Weight / female.StandardReferenceWeight + female.BreedParams.AdvancedConceptionParameters.ConceptionRateIntercept[2]));
                            break;
                        default:
                            // females who have had more than two births (twins should count as one birth)
                            if (female.WeightAtConception > female.BreedParams.CriticalCowWeight * female.StandardReferenceWeight)
                            {
                                rate = female.BreedParams.AdvancedConceptionParameters.ConceptionRateAsymptote[3] / (1 + Math.Exp(female.BreedParams.AdvancedConceptionParameters.ConceptionRateCoefficent[3] * female.Weight / female.StandardReferenceWeight + female.BreedParams.AdvancedConceptionParameters.ConceptionRateIntercept[3]));
                            }
                            break;
                    }
                }
                else
                {
                    // use default values 
                    rate = female.BreedParams.ConceptionRateAsymptote / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent * female.Weight / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept));
                }
            }
            return rate / 100;
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
            List<Ruminant> herd = CurrentHerd(true).Where(a => a.Gender == Sex.Female &
                            a.Age >= a.BreedParams.MinimumAge1stMating & a.Weight >= (a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight)).ToList();
            int head = herd.Count();
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);

            if (head == 0)
            {
                return null;
            }

            // get all fees for breeding
            foreach (RuminantActivityFee item in Apsim.Children(this, typeof(RuminantActivityFee)))
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
                    Reason = item.Name
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
        public override double GetDaysLabourRequired(LabourRequirement requirement)
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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (UseAI)
            {
                html += "\n<div class=\"activityentry\">";
                html += "Using Artificial insemination";
                html += "</div>";
            }
            else
            {
                html += "\n<div class=\"activityentry\">";
                html += "Maximum conception rate is <span class=\"setvalue\">" + MaximumConceptionRateUncontrolled.ToString("0.###") + "</span> using uncontrolled breeding";
                html += "</div>";
            }
            if (InferStartupPregnancy)
            {
                html += "\n<div class=\"activityentry\">";
                html += "Pregnancy status of breeders from matings prior to simulation start will be predicted";
                html += "</div>";
            }

            return html;
        }
    }
}
