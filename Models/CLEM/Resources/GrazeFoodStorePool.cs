using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
	/// <summary>
	/// A food pool of given age
	/// </summary>
	[Serializable]
	public class GrazeFoodStorePool : IFeedType
	{
		/// <summary>
		/// Dry Matter (%)
		/// </summary>
		[Description("Dry Matter (%)")]
        [Required, Range(0, 100, ErrorMessage = "Value must be a percentage in the range 0 to 100")]
        public double DryMatter { get; set; }

		/// <summary>
		/// Dry Matter Digestibility (%)
		/// </summary>
		[Description("Dry Matter Digestibility (%)")]
        [Required, Range(0, 100, ErrorMessage = "Value must be a percentage in the range 0 to 100")]
        public double DMD { get; set; }

		/// <summary>
		/// Nitrogen (%)
		/// </summary>
		[Description("Nitrogen (%)")]
        [Required, Range(0, 100, ErrorMessage = "Value must be a percentage in the range 0 to 100")]
        public double Nitrogen { get; set; }

		/// <summary>
		/// Amount (kg)
		/// </summary>
		[XmlIgnore]
		public double Amount { get { return amount; } }
		private double amount = 0;

		/// <summary>
		/// Age of pool in months
		/// </summary>
		[XmlIgnore]
		public int Age { get; set; }

		/// <summary>
		/// Current pool grazing limit based on ruminant eating pool
		/// </summary>
		[XmlIgnore]
		public double Limit { get; set; }

		/// <summary>
		/// Amount to set at start (kg)
		/// </summary>
		public double StartingAmount { get; set; }

		/// <summary>
		/// Add to Resource method.
		/// This style is not supported in GrazeFoodStoreType
		/// </summary>
		/// <param name="ResourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
		/// <param name="ActivityName"></param>
		/// <param name="Reason"></param>
		public void Add(object ResourceAmount, string ActivityName, string Reason)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RemoveAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="Reason"></param>
		public double Remove(double RemoveAmount, string ActivityName, string Reason)
		{
			RemoveAmount = Math.Min(this.amount, RemoveAmount);
			this.amount = this.amount - RemoveAmount;

			//if (FodderChanged != null)
			//	FodderChanged.Invoke(this, new EventArgs());

			return RemoveAmount;
		}

		/// <summary>
		/// Remove from finance type store
		/// </summary>
		/// <param name="Request">Resource request class with details.</param>
		public void Remove(ResourceRequest Request)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RemoveRequest"></param>
		public void Remove(object RemoveRequest)
		{
//			RuminantFeedRequest removeRequest = RemoveRequest as RuminantFeedRequest;
			// limit by available
//			removeRequest.Amount = Math.Min(removeRequest.Amount, amount);
			// add to intake and update %N and %DMD values
//			removeRequest.Requestor.AddIntake(removeRequest);
			// Remove from resource
//			Remove(removeRequest.Amount, removeRequest.FeedActivity.Name, removeRequest.Requestor.BreedParams.Name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="NewAmount"></param>
		public void Set(double NewAmount)
		{
			this.amount = NewAmount;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Initialise()
		{
			throw new NotImplementedException();
		}
	}
}