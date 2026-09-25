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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class SortRandom : CLEMModel, IValidatableObject, ISort
    {
        /// <inheritdoc/>
        public System.ComponentModel.ListSortDirection SortDirection { get; set; } = System.ComponentModel.ListSortDirection.Ascending;

        /// <inheritdoc/>
        public object OrderRule<T>(T t) => RandomNumberGenerator.Generator.Next();

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Structure.FindChildren<CLEMModel>(relativeTo: Parent as INodeModel).Last() != this)
            {
                yield return new ValidationResult($"The sort item [f={Name}] must be the last component in its group", new string[] { "RandomSort" });
            }
        }
        #endregion
    }
}