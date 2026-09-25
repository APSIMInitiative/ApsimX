using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters and sorts to identify individual ruminants
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [Description("Set monthly feeding values for specified individual ruminants")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantFeedGroupMonthly.htm")]
    public class RuminantFeedGroupMonthly : RuminantFeedGroup, IValidatableObject
    {
        [Link]
        private IClock clock = null;

        /// <summary>
        /// Daily value to supply for each month
        /// </summary>
        [Description("Daily value to supply for each month")]
        [Required, ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <inheritdoc/>
        public override double CurrentValue
        {
            get { return MonthlyValues[clock.Today.Month - 1]; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFeedGroupMonthly()
        {
            MonthlyValues = new double[12];
        }

        #region validation

        /// <inheritdoc/>>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MonthlyValues.Length > 0)
            {
                if (MonthlyValues.Max() == 0)
                {
                    Summary.WriteMessage(this, $"No feed values were defined for any month in [{this.Name}]. No feeding will be performed for [a={this.Parent.Name}]", MessageType.Warning);
                }
            }
            return null;
        }

        #endregion
    }
}
