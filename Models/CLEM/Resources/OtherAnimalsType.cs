using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for bank account
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimals))]
    [Description("This resource represents an other animal type (e.g. chickens)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimalType.htm")]
    public class OtherAnimalsType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// Age when individuals become adults for feeding and breeding rates
        /// </summary>
        [Description("Age when adult")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier AgeWhenAdult { get; set; } = new int[] { 12, 0 };

        /// <summary>
        /// Age when individuals die
        /// </summary>
        [Description("Maximum age before death")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MaxAge { get; set; } = new int[] { 20, 0 };

        /// <summary>
        /// Current cohorts of this Other Animal Type.
        /// </summary>
        [JsonIgnore]
        public List<OtherAnimalsTypeCohort> Cohorts;

        /// <summary>
        /// The last group of individuals to be added or removed (for reporting)
        /// </summary>
        [JsonIgnore]
        public OtherAnimalsTypeCohort LastCohortChanged { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Initialise();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            Cohorts?.Clear();
            Cohorts = null;
        }

        /// <summary>
        /// Initialise resource type
        /// </summary>
        public void Initialise()
        {
            Cohorts = new List<OtherAnimalsTypeCohort>();
            foreach (var child in this.Children)
            {
                if (child is OtherAnimalsTypeCohort cohort)
                {
                    cohort.SaleFlag = HerdChangeReason.InitialHerd;
                    Add(child, null, null, "Initial numbers");
                }
            }
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
            }
        }

        #region Transactions

        /// <summary>
        /// Amount
        /// </summary>
        [JsonIgnore]
        public double Amount { get; set; }

        /// <summary>
        /// Add individuals to type based on cohort
        /// </summary>
        /// <param name="addIndividuals">OtherAnimalsTypeCohort Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object addIndividuals, CLEMModel activity, string relatesToResource, string category)
        {
            OtherAnimalsTypeCohort cohortToAdd = addIndividuals as OtherAnimalsTypeCohort;

            OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortToAdd.Age && a.Sex == cohortToAdd.Sex).FirstOrDefault();

            if (cohortexists == null)
                // add new
                Cohorts.Add(cohortToAdd);
            else
                cohortexists.Number += cohortToAdd.Number;

            LastCohortChanged = cohortToAdd;

            ReportTransaction(TransactionType.Gain, cohortToAdd.Number, activity, relatesToResource, category, this, cohortToAdd);
        }

        /// <summary>
        /// Remove individuals from type based on cohort
        /// </summary>
        /// <param name="removeIndividuals"></param>
        /// <param name="activity"></param>
        /// <param name="reason"></param>
        public void Remove(object removeIndividuals, CLEMModel activity, string reason)
        {
            OtherAnimalsTypeCohort cohortToRemove = removeIndividuals as OtherAnimalsTypeCohort;
            OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortToRemove.Age && a.Sex == cohortToRemove.Sex).First();

            if (cohortexists == null)
            {
                // tried to remove individuals that do not exist
                throw new Exception($"Tried to remove individuals from {Name} that do not exist");
            }
            else
            {
                cohortexists.Number -= cohortToRemove.Number;
                cohortexists.Number = Math.Max(0, cohortexists.Number);
            }

            LastCohortChanged = cohortToRemove;
            ReportTransaction(TransactionType.Loss, cohortToRemove.Number, activity, "", reason, this, cohortToRemove);
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
