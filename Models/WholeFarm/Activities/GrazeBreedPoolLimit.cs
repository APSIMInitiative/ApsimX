using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>
	/// The proportional intake limit for a given pool by breed
	/// </summary>
	public class GrazeBreedPoolLimit
	{
		/// <summary>
		/// Proportion of intake limit for pool
		/// </summary>
		public double Limit { get; set; }

		/// <summary>
		/// Pool that this limit applies to
		/// </summary>
		public GrazeFoodStorePool Pool { get; set; }
	}
}
