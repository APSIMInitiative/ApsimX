using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Object for an individual male Ruminant.
	/// </summary>
	public class RuminantMale: Ruminant
	{
		/// <summary>
		/// Indicates if individual is breeding sire
		/// </summary>
		public bool BreedingSire { get; set; }

		/// <summary>
		/// Indicates if individual is draught animal
		/// </summary>
		public bool Draught { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public RuminantMale()
		{
			BreedingSire = false;
			Draught = false;
		}

	}
}
