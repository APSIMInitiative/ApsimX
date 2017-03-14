using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Groupings
{
	///<summary>
	/// Individual filter term for ruminant group of filters to identify individul ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(OtherAnimalsFilterGroup))]
	public class OtherAnimalsFilter: WFModel
	{
		/// <summary>
		/// Name of parameter to filter by
		/// </summary>
		[Description("Name of parameter to filter by")]
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
