using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Resources
{
	///<summary>
	/// Resource request for Resource from a ResourceType
	///</summary> 
	public class ResourceRequest
	{
		///<summary>
		/// Name of resource being requested 
		///</summary> 
		public string ResourceName { get; set; }
		///<summary>
		/// Name of resource type being requested 
		///</summary> 
		public string ResourceTypeName { get; set; }
		///<summary>
		/// Name of item requesting resource
		///</summary> 
		public string Requestor { get; set; }
		///<summary>
		/// Amount required 
		///</summary> 
		public double Required { get; set; }
		///<summary>
		/// Amount available
		///</summary> 
		public double Available { get; set; }
		///<summary>
		/// Amount provided
		///</summary> 
		public double Provided { get; set; }
		///<summary>
		/// Filtering and sorting items list
		///</summary> 
		public List<object> FilterDetails { get; set; }
		/////<summary>
		///// Requesting activity containing detials and filtering options
		/////</summary> 
		//public object RequestDetails { get; set; }
		///<summary>
		/// Allow transmutation
		///</summary> 
		public bool AllowTransmutation { get; set; }
		///<summary>
		/// Allow transmutation
		///</summary> 
		public bool TransmutationPossible { get; set; }
		///<summary>
		/// ResourceRequest constructor
		///</summary> 
		public ResourceRequest()
		{
			// default values
			TransmutationPossible = false;
			AllowTransmutation = false;
		}
	}

	///<summary>
	/// Additional information for animal food requests
	///</summary> 
	public class AnimalFoodResourceRequest : ResourceRequest
	{
		///<summary>
		/// DMD of food supplied
		///</summary> 
		public double DMD { get; set; }
		///<summary>
		/// Percent N of food supplied
		///</summary> 
		public double PercentN { get; set; }
	}
}
