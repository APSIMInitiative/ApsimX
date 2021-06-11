using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter term for ruminant group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimalsFilterGroup))]
    [Description("This other animal filter filter rule is used to define specific individuals from the other animals. Multiple filters are additive.")]
    [Version(1, 0, 1, "")]
    public class OtherAnimalsFilter: CLEMModel, IFilter
    {
        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Name of parameter to filter by")]
        [Required]
        public OtherAnimalsFilterParameters Parameter
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
        private OtherAnimalsFilterParameters parameter;


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

        /// <inheritdoc/>
        public string ParameterName => Parameter.ToString();

        private void UpdateName()
        {
            this.Name = String.Format("Filter[{0}{1}{2}]", Parameter.ToString(), Operator.ToSymbol(), Value);
        }
    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum OtherAnimalsFilterParameters
    {
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender,
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age
    }
}
