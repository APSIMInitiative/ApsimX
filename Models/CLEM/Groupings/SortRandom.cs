using APSIM.Core;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Randomises the order of any unsorted parameters. Must be the last element in its group.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Description("Shuffle (randomises) individuals in the fiter group")]
    [Version(1, 0, 0, "")]
    [HelpUri(@"Content/Features/Filters/SortRandomise.htm")]
    public class SortRandom : CLEMModel, IValidatableObject, ISort
    {
        /// <inheritdoc/>
        public System.ComponentModel.ListSortDirection SortDirection { get; set; } = System.ComponentModel.ListSortDirection.Ascending;

        /// <inheritdoc/>
        public object OrderRule<T>(T t) => RandomNumberGenerator.Generator.Next();

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return $"<div class=\"filter\" style=\"opacity: {((this.Enabled) ? "1" : "0.4")}\">Randomise order</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        #endregion

        #region validation

        /// <summary>
        /// Validate this component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Structure.FindChildren<CLEMModel>(relativeTo: Parent as INodeModel).Last() != this)
            {
                string[] memberNames = new string[] { "RandomSort" };
                results.Add(new ValidationResult($"The sort item [f={Name}] must be the last component in its group", memberNames));
            }
            return results;
        }
        #endregion
    }

}