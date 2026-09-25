using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// A component to determine how many of the filtered group to use
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines the number of individuals to take")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Version(1, 0, 0, "")]
    [HelpUri(@"Content/Features/Filters/TakeFromFiltered.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class TakeFromFiltered : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Take style
        /// </summary>
        [Description("Style")]
        [Required]
        public TakeFromFilterStyle TakeStyle { get; set; }

        /// <summary>
        /// Take position
        /// </summary>
        [Description("From")]
        [Required]
        public TakeFromFilteredPositionStyle TakePositionStyle { get; set; } = TakeFromFilteredPositionStyle.Start;

        /// <summary>
        /// Value to take
        /// </summary>
        [Description("Value")]
        public float Value { get; set; } = 1.0f;

        /// <summary>
        /// Method to calculate the number required based on style and population size
        /// </summary>
        /// <param name="groupSize">The number of individuals in the group</param>
        /// <returns>Number to take</returns>
        public int NumberToTake(int groupSize)
        {
            int numberToTake;
            if (TakeStyle == TakeFromFilterStyle.TakeIndividuals || TakeStyle == TakeFromFilterStyle.SkipIndividuals)
                numberToTake = Convert.ToInt32(Value);
            else
                numberToTake = Convert.ToInt32(Math.Ceiling(Value * groupSize));
            return Math.Min(numberToTake, groupSize);
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool isProportion = (TakeStyle == TakeFromFilterStyle.TakeProportion || TakeStyle == TakeFromFilterStyle.SkipProportion);
            if (Value == 0)
            {
                yield return new ValidationResult($"Provide a {((isProportion) ? "proportion" : "number of individuals")} greater than 0 for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}]", new string[] { "Invalid value to take from filter" });
            }

            if (isProportion)
            {
                if (Value > 1)
                {
                    bool isTake = (TakeStyle.ToString().Contains("Take"));

                    yield return new ValidationResult($"The proportion to {(isTake ? "take" : "skip")} from [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] must be less than or equal to 1", new string[] { $"Invalid proportion to {(isTake ? "take" : "skip")} from filter" });
                }
            }
        }
        #endregion
    }
}
