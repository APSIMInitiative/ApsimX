using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter term for ruminant group of filters to identify individul ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantFilterGroup))]
    [Description("This ruminant filter rule is used to define specific individuals from the current ruminant herd. Multiple filters are additive.")]
    public class RuminantFilter: CLEMModel
    {
        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Name of parameter to filter by")]
        [Required]
        public RuminantFilterParameters Parameter
        {
            get
            {
                return parameter;
            }
            set
            {
                parameter = value;
                UpdateName();
            }
        }
        private RuminantFilterParameters parameter;


        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        public FilterOperators Operator
        {
            get
            {
                return operatr;
            }
            set
            {
                operatr = value;
                UpdateName();
            }
        }
        private FilterOperators operatr;

        /// <summary>
        /// Value to check for filter
        /// </summary>
        [Description("Value to filter by")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value to filter by required")]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                UpdateName();
            }
        }
        private string _value;


        private void UpdateName()
        {
            this.Name = String.Format("Filter[{0}{1}{2}]", Parameter.ToString(), Operator.ToSymbol(), Value);
        }
    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum RuminantFilterParameters
    {
        /// <summary>
        /// Breed of ruminant
        /// </summary>
        Breed,
        /// <summary>
        /// Herd individuals belong to
        /// </summary>
        HerdName,
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender,
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age,
        /// <summary>
        /// ID of individuals
        /// </summary>
        ID,
        /// <summary>
        /// Weight of individuals
        /// </summary>
        Weight,
        /// <summary>
        /// Weight as proportion of High weight achieved
        /// </summary>
        ProportionOfHighWeight,
        /// <summary>
        /// Weight as proportion of Standard Reference Weight
        /// </summary>
        ProportionOfSRW,
        /// <summary>
        /// Current grazing location
        /// </summary>
        Location,
        /// <summary>
        /// Weaned status
        /// </summary>
        Weaned,
        /// <summary>
        /// Is female lactating
        /// </summary>
        IsLactating,
        /// <summary>
        /// Is female pregnant
        /// </summary>
        IsPregnant,
        /// <summary>
        /// Is male draught individual
        /// </summary>
        Draught,
        /// <summary>
        /// Is male breeding sire
        /// </summary>
        BreedingSire,
    }
}
