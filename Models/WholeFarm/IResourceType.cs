using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models.WholeFarm
{
    /// <summary>
    /// Interface of a Resource Type.
    /// </summary>
    public interface IResourceType
    {

		//event EventHandler ResourceChanged;

		/// <summary>
		/// Add this Amount to the existing Amount.
		/// </summary>
		/// <param name="AddAmount">Amount to add</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		void Add(double AddAmount, string ActivityName, string UserName);

		/// <summary>
		/// Remove this Amount from the existing Amount
		/// </summary>
		/// <param name="RemoveAmount">nb. This is a positive value not a negative value.</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		void Remove(double RemoveAmount, string ActivityName, string UserName);

		/// <summary>
		/// Remove this request
		/// </summary>
		/// <param name="RemoveRequest">A suitable request object with required details</param>
		void Remove(object RemoveRequest);

		/// <summary>
		/// Set the amount to this new value.
		/// </summary>
		void Set(double NewAmount);

		/// <summary>
		/// Initialise the variables that store the current state of the resource.
		/// </summary>
		void Initialise();
    }
}
