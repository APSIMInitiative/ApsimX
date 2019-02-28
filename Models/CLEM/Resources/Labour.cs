using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Labour Person models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all labour types (people) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/resources/labour/labour.htm")]
    public class Labour: ResourceBaseWithTransactions, IValidatableObject
    {
        /// <summary>
        /// Get the Clock.
        /// </summary>
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Labour types currently available.
        /// </summary>
        [XmlIgnore]
        public List<LabourType> Items;

        /// <summary>
        /// Allows indiviuals to age each month
        /// </summary>
        [Description("Allow individuals to age")]
        [Required]
        public bool AllowAging { get; set; }

        private LabourAvailabilityList availabilityList;

        /// <summary>
        /// Validation of this resource
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            availabilityList = Apsim.Children(this, typeof(LabourAvailabilityList)).Cast<LabourAvailabilityList>().FirstOrDefault();
            if (availabilityList == null && Apsim.Children(this, typeof(LabourType)).Count > 0)
            {
                string[] memberNames = new string[] { "Labour.AvailabilityList" };
                results.Add(new ValidationResult("A labour availability list is required under the labour resource for this simulation.", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<LabourType>();
            foreach (LabourType labourChildModel in Apsim.Children(this, typeof(LabourType)).Cast<LabourType>().ToList())
            {
                for (int i = 0; i < labourChildModel.Individuals; i++)
                {
                    // get the availability from provided list

                    LabourType labour = new LabourType()
                    {
                        Gender = labourChildModel.Gender,
                        Individuals = 1,
                        InitialAge = labourChildModel.InitialAge,
                        AgeInMonths = labourChildModel.InitialAge * 12,
                        LabourAvailability = labourChildModel.LabourAvailability,
                        Parent = this,
                        Name = labourChildModel.Name + ((labourChildModel.Individuals > 1)?"_"+(i+1).ToString():"")
                    };
                    labour.TransactionOccurred += Resource_TransactionOccurred;
                    Items.Add(labour);
                }
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (LabourType childModel in Apsim.Children(this, typeof(LabourType)))
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
            if (Items != null)
            {
                Items.Clear();
            }
            Items = null;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (Clock.Today.Day != 1)
            {
                OnStartOfMonth(this, null);
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            int currentmonth = Clock.Today.Month;
            foreach (LabourType item in Items)
            {
                CheckAssignLabourAvailability(item);

                // set available days from availabilityitem
                item.SetAvailableDays(currentmonth);
            }
        }

        private void CheckAssignLabourAvailability(LabourType labour)
        {
            List<LabourType> checkList = new List<LabourType>() { labour };
            if (labour.LabourAvailability != null)
            {
                // check labour availability still ok
                if (checkList.Filter(labour.LabourAvailability).Count == 0)
                {
                    labour.LabourAvailability = null;
                }
            }

            // if not assign new value
            if (labour.LabourAvailability == null)
            {
                foreach (Model availItem in availabilityList.Children.Where(a => typeof(LabourSpecificationItem).IsAssignableFrom(a.GetType())).ToList())
                {
                    if (checkList.Filter(availItem).Count > 0)
                    {
                        labour.LabourAvailability = availItem as LabourSpecificationItem;
                        break;
                    }
                }
                // if still null report error
                if (labour.LabourAvailability == null)
                {
                    throw new ApsimXException(this, string.Format("Unable to find labour availability suitable for labour type [f=Name:{0}] [f=Gender:{1}] [f=Age:{2}]\nAdd additional labour availability item to [r={3}] under [r={4}]", labour.Name, labour.Gender, labour.Age, availabilityList.Name, this.Name));
                }
            }
        }

        /// <summary>Age individuals</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void ONCLEMAgeResources(object sender, EventArgs e)
        {
            if(AllowAging)
            {
                foreach (LabourType item in Items)
                {
                    item.AgeInMonths++;

                    //Update labour available if needed.
                    CheckAssignLabourAvailability(item);

                }
            }
        }

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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if(AllowAging)
            {
                html += "\n<div class=\"activityentry\">";
                html += "Individuals age with time";
                html += "</div>";
            }
            html += "\n<div class=\"holderresourcesub\">";
            html += "\n<div class=\"clearfix resourcebannerlight\">Labour types</div>";
            html += "\n<div class=\"resourcecontentlight\">";
            html += "<table><tr><th>Name</th><th>Gender</th><th>Age (yrs)</th><th>Number</th></tr>";
            foreach (LabourType labourType in Apsim.Children(this, typeof(LabourType)).Cast<LabourType>().ToList())
            {
                html += "<tr>";
                html += "<td>" + labourType.Name + "</td>";
                html += "<td><span class=\"setvalue\">" + labourType.Gender.ToString() + "</span></td>";
                html += "<td><span class=\"setvalue\">" + labourType.InitialAge.ToString() + "</span></td>";
                html += "<td><span class=\"setvalue\">" + labourType.Individuals.ToString() + "</span></td>";
                html += "</tr>";
            }
            html += "</table>";
            html += "</div></div>";
            return html;
        }


    }
}
