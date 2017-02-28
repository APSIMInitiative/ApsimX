using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Models.WholeFarm
{
	///<summary>
	/// Individual filter term for ruminant group of filters to identify individul ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
//	[ValidParent(ParentType = (typeof(RuminantFilterGroup) | typeof(FodderLimitsFilterGroup)))]
	public class LabourFilter: Model
	{
		/// <summary>
		/// Name of parameter to filter by
		/// </summary>
		[Description("Name of parameter to filter by")]
		public LabourFilterParameters Parameter
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
		private LabourFilterParameters parameter;

		/// <summary>
		/// Name of parameter to filter by
		/// </summary>
		[Description("Operator to use for filtering")]
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
			this.Name = String.Format("Filter[{0}{1}{2}]", Parameter.ToString(), Operator.ToSymbol(), Value );
		}
	}

	/// <summary>
	/// Ruminant filter parameters
	/// </summary>
	public enum LabourFilterParameters
	{
		/// <summary>
		/// Name of individual
		/// </summary>
		Name,
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
