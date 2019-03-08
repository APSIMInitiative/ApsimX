using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;
using Models.Core.Attributes;
using Models.CLEM.Reporting;

namespace Models.CLEM.Resources
{

    ///<summary>
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyTreeView")]
    [PresenterName("UserInterface.Presenters.PropertyTreeTablePresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all rumiant types (herds or breeds) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/resources/ruminant/ruminantherd.htm")]
    public class RuminantHerd: ResourceBaseWithTransactions
    {
        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [XmlIgnore]
        public List<Ruminant> Herd;

        /// <summary>
        /// List of requested purchases.
        /// </summary>
        [XmlIgnore]
        public List<Ruminant> PurchaseIndividuals;

        /// <summary>
        /// The last individual to be added or removed (for reporting)
        /// </summary>
        [XmlIgnore]
        public object LastIndividualChanged { get; set; }

        /// <summary>
        /// The details of an individual for reporting
        /// </summary>
        [XmlIgnore]
        public RuminantReportItemEventArgs ReportIndividual { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            id = 1;
            Herd = new List<Ruminant>();
            PurchaseIndividuals = new List<Ruminant>();
            LastIndividualChanged = new Ruminant();

            // for each Ruminant type 
            foreach (RuminantType rType in Apsim.Children(this, typeof(RuminantType)))
            {
                foreach (RuminantInitialCohorts ruminantCohorts in Apsim.Children(rType, typeof(RuminantInitialCohorts)))
                {
                    foreach (var ind in ruminantCohorts.CreateIndividuals())
                    {
                        ind.SaleFlag = HerdChangeReason.InitialHerd;
                        AddRuminant(ind, this);
                    }
                }
            }

            // Assign mothers to suckling calves
            foreach (string herdName in Herd.Select(a => a.HerdName).Distinct())
            {
                List<Ruminant> herd = Herd.Where(a => a.HerdName == herdName).ToList();

                // get list of females of breeding age and condition
                List<RuminantFemale> breedFemales = herd.Where(a => a.Gender == Sex.Female && a.Age >= a.BreedParams.MinimumAge1stMating + a.BreedParams.GestationLength && a.Weight >= (a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight) && a.Weight >= (a.BreedParams.CriticalCowWeight * a.StandardReferenceWeight)).OrderByDescending(a => a.Age).ToList().Cast<RuminantFemale>().ToList();

                // get list of all sucking individuals
                List<Ruminant> sucklingList = herd.Where(a => a.Weaned == false).ToList();

                if (breedFemales.Count() == 0)
                {
                    if (sucklingList.Count > 0)
                    {
                        Summary.WriteWarning(this, String.Format("Insufficient breeding females to assign [{0}] sucklings for herd [r={1}].\nUnassigned calves will need to graze or be fed and may have reduced growth until weaned.\nBreeding females must be at least minimum breeding age + gestation length at the start of the simulation to provide a calf.", sucklingList.Count, herdName));
                    }
                }
                else
                {
                    // gestation interval at smallest size generalised curve
                    double minAnimalWeight = breedFemales[0].StandardReferenceWeight - ((1 - breedFemales[0].BreedParams.SRWBirth) * breedFemales[0].StandardReferenceWeight) * Math.Exp(-(breedFemales[0].BreedParams.AgeGrowthRateCoefficient * (breedFemales[0].BreedParams.MinimumAge1stMating * 30.4)) / (Math.Pow(breedFemales[0].StandardReferenceWeight, breedFemales[0].BreedParams.SRWGrowthScalar)));
                    double minsizeIPI = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (minAnimalWeight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient);
                    // restrict minimum period between births
                    minsizeIPI = Math.Max(minsizeIPI, breedFemales[0].BreedParams.GestationLength + 2);

                    // assign calves to cows
                    int sucklingCount = 0;
                    int numberThisPregnancy = breedFemales[0].CalulateNumberOfOffspringThisPregnancy();
                    foreach (var suckling in sucklingList)
                    {
                        sucklingCount++;
                        if (breedFemales.Count > 0)
                        {
                            breedFemales[0].DryBreeder = false;

                            //Initialise female milk production in at birth so ready for sucklings to consume
                            double milkTime = (suckling.Age*30.4) + 15; // +15 equivalent to mid month production

                            // need to calculate normalised animal weight here for milk production
                            double milkProduction = breedFemales[0].BreedParams.MilkPeakYield * breedFemales[0].Weight / breedFemales[0].NormalisedAnimalWeight * (Math.Pow(((milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay), breedFemales[0].BreedParams.MilkCurveSuckling)) * Math.Exp(breedFemales[0].BreedParams.MilkCurveSuckling * (1 - (milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay));
                            breedFemales[0].MilkProduction = Math.Max(milkProduction, 0.0);
                            breedFemales[0].MilkCurrentlyAvailable = milkProduction * 30.4;

                            // generalised curve
                            // previously * 30.64
                            double currentIPI = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (breedFemales[0].Weight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient);
                            // restrict minimum period between births
                            currentIPI = Math.Max(currentIPI, breedFemales[0].BreedParams.GestationLength + 2);

                            // calculate previous births
                            breedFemales[0].NumberOfBirths = Convert.ToInt32((breedFemales[0].Age - suckling.Age - breedFemales[0].BreedParams.GestationLength - breedFemales[0].BreedParams.MinimumAge1stMating) / ((currentIPI + minsizeIPI) / 2));
                            // add this birth
                            if (suckling.Age == 0)
                            {
                                // do not add if this is not a new born suckling at initialisation as was assumed to be previousl added
                                breedFemales[0].NumberOfBirths++;
                                breedFemales[0].NumberOfBirthsThisTimestep++;
                            }

                            //breedFemales[0].Parity = breedFemales[0].Age - suckling.Age - 9;
                            // AL removed the -9 as this would make it conception month not birth month
                            breedFemales[0].AgeAtLastBirth = breedFemales[0].Age - suckling.Age;
                            breedFemales[0].AgeAtLastConception = breedFemales[0].AgeAtLastBirth - breedFemales[0].BreedParams.GestationLength;
                            breedFemales[0].SuccessfulPregnancy = true;

                            // suckling mother set
                            suckling.Mother = breedFemales[0];
                            // add suckling to suckling offspring of mother.
                            suckling.Mother.SucklingOffspringList.Add(suckling);

                            // check if a twin and if so apply next individual to same mother.
                            // otherwise remove this mother from the list
                            if (numberThisPregnancy == 1)
                            {
                                breedFemales.RemoveAt(0);
                                numberThisPregnancy = breedFemales[0].CalulateNumberOfOffspringThisPregnancy();
                            }
                            else
                            {
                                numberThisPregnancy--;
                            }
                        }
                        else
                        {
                            Summary.WriteWarning(this, String.Format("Insufficient breeding females to assign [{0}] sucklings for herd [r={1}].\nUnassigned calves will need to graze or be fed and may have reduced growth until weaned.\nBreeding females must be at least minimum breeding age + gestation length at the start of the simulation to provide a calf.", sucklingList.Count - sucklingCount, herdName));
                        }
                    }

                    // assigning values for the remaining females who haven't just bred.
                    // i.e meet breeding rules and not pregnant or lactating (just assigned calf), but calculate for underweight individuals not previously provided calves.
                    double ageFirstBirth = herd[0].BreedParams.MinimumAge1stMating + herd[0].BreedParams.GestationLength;
                    foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female && a.Age > a.BreedParams.MinimumAge1stMating + a.BreedParams.GestationLength && a.Weight >= (a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight)).Cast<RuminantFemale>().Where(a => !a.IsLactating && !a.IsPregnant))
                    {
                        female.DryBreeder = true;
                        // generalised curve
                        double currentIPI = Math.Pow(herd[0].BreedParams.InterParturitionIntervalIntercept * (female.Weight / female.StandardReferenceWeight), herd[0].BreedParams.InterParturitionIntervalCoefficient);
                        // restrict minimum period between births (previously +61)
                        currentIPI = Math.Max(currentIPI, breedFemales[0].BreedParams.GestationLength + 2);

                        // calculate number of births assuming conception at min age first mating
                        // therefore first birth min age + gestation length

                        female.NumberOfBirths = Convert.ToInt32((female.Age - ageFirstBirth) / ((currentIPI + minsizeIPI) / 2)) - 1;
                        female.AgeAtLastBirth = ageFirstBirth + (currentIPI* female.NumberOfBirths);
                        female.AgeAtLastConception = female.AgeAtLastBirth - breedFemales[0].BreedParams.GestationLength;
                        female.SuccessfulPregnancy = true;
                    }
                }
            }
        }

        /// <summary>
        /// Add individual/cohort to the the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to add</param>
        /// <param name="model">Model adding individual</param>
        public void AddRuminant(Ruminant ind, IModel model)
        {
            if (ind.ID == 0)
            {
                ind.ID = this.NextUniqueID;
            }
            Herd.Add(ind);
            LastIndividualChanged = ind;

            ResourceTransaction details = new ResourceTransaction();
            details.Gain = 1;
            details.Activity = model.Name; // "Unknown";
            details.ActivityType = model.GetType().Name; // "Unknown";
            details.Reason = ind.SaleFlag.ToString(); // "Unknown";
            details.ResourceType = this.Name;
            details.ExtraInformation = ind;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Remove individual/cohort from the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to remove</param>
        /// <param name="model">Model removing individual</param>
        public void RemoveRuminant(Ruminant ind, IModel model)
        {
            // Remove mother ID from any suckling offspring
            if (ind.Gender == Sex.Female)
            {
                foreach (var offspring in (ind as RuminantFemale).SucklingOffspringList)
                {
                    offspring.Mother = null;
                }
            }
            Herd.Remove(ind);
            LastIndividualChanged = ind;

            ResourceTransaction details = new ResourceTransaction();
            details.Loss = 1;
            details.Activity = model.Name; // "Unknown";
            details.ActivityType = model.GetType().Name; // "Unknown";
            details.Reason = ind.SaleFlag.ToString(); // "Unknown";
            details.ResourceType = this.Name;
            details.ExtraInformation = ind;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Herd != null)
            {
                Herd.Clear();
            }
            Herd = null;
            if (PurchaseIndividuals != null)
            {
                PurchaseIndividuals.Clear();
            }
            PurchaseIndividuals = null;
        }

        /// <summary>
        /// Remove list of Ruminants from the herd
        /// </summary>
        /// <param name="list">List of Ruminants to remove</param>
        /// <param name="model">Model removing individuals</param>
        public void RemoveRuminant(List<Ruminant> list, IModel model)
        {
            foreach (var ind in list)
            {
                // report removal
                RemoveRuminant(ind, model);
            }
        }

        /// <summary>
        /// Gte the next unique individual id number
        /// </summary>
        public int NextUniqueID { get { return id++; } }
        private int id = 1;

        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }

        #endregion

        #region weaning event

        /// <summary>
        /// Override base event
        /// </summary>
        public void OnWeanOccurred(EventArgs e)
        {
            ReportIndividual = e as RuminantReportItemEventArgs;
            WeanOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public event EventHandler WeanOccurred;

        private void Resource_WeanOccurred(object sender, EventArgs e)
        {
            OnWeanOccurred(e);
        }

        #endregion



        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

    }
}
