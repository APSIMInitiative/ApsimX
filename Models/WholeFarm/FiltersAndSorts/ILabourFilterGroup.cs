using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>
	/// Interface of a labour activity group.
	/// </summary>
	public interface ILabourFilterGroup: IModel
	{
		/// <summary>
		/// Labour priority (1 high, 10 low)
		/// </summary>
		[Description("Labour priority")]
		int Priority { get; set; }

		/// <summary>
		/// Amount provided from resource or arbitrator
		/// </summary>
		[XmlIgnore]
		double AmountProvided { get; set; }
	}
}
