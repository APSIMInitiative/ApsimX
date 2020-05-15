using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// CLEM Zone to control simulation
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("This represents a shared market place for CLEM farms")]
    [HelpUri(@"Content/Features/Market.htm")]
    [Version(1, 0, 2, "Tested and functioning for targeted feeding including transmutations but still needs movement of goods to market.")]
    [Version(1, 0, 1, "Early implementation of market place for multi-farm simulations. This is a major addition and is not checked for full functionality.")]
    [ScopedModel]
    public class Market: Zone, IValidatableObject, ICLEMUI
    {
        /// <summary>Area of the zone.</summary>
        /// <value>The area.</value>
        [XmlIgnore]
        public new double Area { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        /// <value>The slope.</value>
        [XmlIgnore]
        public new double Slope { get; set; }

        /// <summary>
        /// not used in CLEM
        /// </summary>
        [XmlIgnore]
        public new double AspectAngle { get; set; }

        /// <summary>Local altitude (meters above sea level).</summary>
        [XmlIgnore]
        public new double Altitude { get; set; } = 50;

        /// <summary>
        /// Identifies the last selected tab for display
        /// </summary>
        [XmlIgnore]
        public string SelectedTab { get; set; }

        private ResourcesHolder resources;
        /// <summary>
        /// ResourceHolder for the market
        /// </summary>
        public ResourcesHolder Resources { get
            {
                if(resources == null)
                {
                    resources = this.Children.Where(a => a.GetType() == typeof(ResourcesHolder)).FirstOrDefault() as ResourcesHolder;
                }
                return resources; 
            }
        }

        private FinanceType bankAccount;
        /// <summary>
        /// Default (first) bank account for the market
        /// </summary>
        public FinanceType BankAccount
        {
            get
            {
                if (bankAccount == null)
                {
                    bankAccount = Resources.FinanceResource().Children.FirstOrDefault() as FinanceType;
                }
                return bankAccount;
            }
        }

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // check that one resources and on activities are present.
            int holderCount = this.Children.Where(a => a.GetType() == typeof(ResourcesHolder)).Count();
            if (holderCount == 0)
            {
                string[] memberNames = new string[] { "CLEM.Resources" };
                results.Add(new ValidationResult("A market place must contain a Resources Holder to manage resources", memberNames));
            }
            if (holderCount > 1)
            {
                string[] memberNames = new string[] { "CLEM.Resources" };
                results.Add(new ValidationResult("A market place must contain only one (1) Resources Holder to manage resources", memberNames));
            }
            holderCount = this.Children.Where(a => a.GetType() == typeof(ActivitiesHolder)).Count();
            if (holderCount > 1)
            {
                string[] memberNames = new string[] { "CLEM.Activities" };
                results.Add(new ValidationResult("A market place must contain only one (1) Activities Holder to manage activities", memberNames));
            }
            // only one market
            holderCount = Apsim.Children(Apsim.Parent(this, typeof(Zone)), typeof(Market)).Count();
            if (holderCount > 1)
            {
                string[] memberNames = new string[] { "CLEM.Markets" };
                results.Add(new ValidationResult("Only one [m=Market] place is allowed in a CLEM simulation", memberNames));
            }

            return results;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="useFullDescription">Use full verbose description</param>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        public string GetFullSummary(object model, bool useFullDescription, string htmlString)
        {
            string html = "";
            html += "\n<div class=\"holdermain\" style=\"opacity: " + ((!this.Enabled) ? "0.4" : "1") + "\">";

            foreach (CLEMModel cm in Apsim.Children(this, typeof(CLEMModel)).Cast<CLEMModel>())
            {
                html += cm.GetFullSummary(cm, true, "");
            }

            html += "</div>";
            return html;
        }


    }
}
