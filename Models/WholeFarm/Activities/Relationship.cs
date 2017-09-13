using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>
	/// This determines a relationship
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class Relationship: Model
	{
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Current value
		/// </summary>
		[XmlIgnore]
		public double Value { get; set; }

		/// <summary>
		/// Starting value
		/// </summary>
		[Description("Value at start of simulation")]
        [Required]
        public double StartingValue { get; set; }

		/// <summary>
		/// Minimum value possible
		/// </summary>
		[Description("Minimum value possible")]
        [Required]
        public double Minumum { get; set; }

		/// <summary>
		/// Maximum value possible
		/// </summary>
		[Description("Maximum value possible")]
        [Required]
        public double Maximum { get; set; }

		/// <summary>
		/// X values of relationship
		/// </summary>
		[Description("X values of relationship")]
        [Required]
        public double[] XValues { get; set; }

		/// <summary>
		/// Y values of relationship
		/// </summary>
		[Description("Y values of relationship")]
        [Required]
        public double[] YValues { get; set; }

		///// <summary>
		///// List of points to define relationship 
		///// </summary>
		//[XmlIgnore]
		//public List<Point> Points { get; set; }

		/// <summary>
		/// Solve equation for y given x
		/// </summary>
		/// <param name="X">x value to solve y</param>
		/// <param name="LinearInterpolation">Use linear interpolation between the nearest point before and after x</param>
		/// <returns>y value for given x</returns>
		public double SolveY(double X, bool LinearInterpolation)
		{
			if (X <= XValues[0])
				return YValues[0];
			if (X >= XValues[XValues.Length-1])
				return YValues[YValues.Length - 1];

			int k = 0;
			for (int i = 0; i < XValues.Length; i++)
			{
				if (X <= XValues[i + 1])
				{
					k = i;
					break;
				}
			}

			if(LinearInterpolation)
			{
				return YValues[k] + (YValues[k + 1] - YValues[k]) / (XValues[k + 1] - XValues[k]) * (X - YValues[k]);
			}
			else
			{
				return YValues[k + 1];
			}
		}

		/// <summary>
		/// Modify the current value by Y calculated from x
		/// </summary>
		/// <param name="x">x value</param>
		public void Modify(double x)
		{
			Value += SolveY(x, true);
			Value = Math.Min(Value, Maximum);
			Value = Math.Max(Value, Minumum);
		}

		/// <summary>
		/// Calculate new value using Y calculated from x
		/// </summary>
		/// <param name="x">x value</param>
		public void Calculate(double x)
		{
			Value = SolveY(x, true);
			Value = Math.Min(Value, Maximum);
			Value = Math.Max(Value, Minumum);
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			Value = StartingValue;
			if(XValues == null)
			{
				Summary.WriteWarning(this, String.Format("X values are required for relationship ({0})", this.Name));
				throw new Exception(String.Format("X values are required for relationship ({0})", this.Name));
			}
			if (YValues == null)
			{
				Summary.WriteWarning(this, String.Format("Y values are required for relationship ({0})", this.Name));
				throw new Exception(String.Format("Y values are required for relationship ({0})", this.Name));
			}
			if (XValues.Length != YValues.Length)
			{
				Summary.WriteWarning(this, String.Format("The same number of X and Y values are required for relationship ({0})", this.Name));
				throw new Exception(String.Format("The same number of X and Y values are required for relationship ({0})", this.Name));
			}
			if (XValues.Length == 0)
			{
				Summary.WriteWarning(this, String.Format("No data points were provided for relationship ({0})", this.Name));
				throw new Exception(String.Format("No data points were provided for relationship ({0})", this.Name));
			}
			if (XValues.Length < 2)
			{
				Summary.WriteWarning(this, String.Format("At least two data points are required for relationship ({0})", this.Name));
				throw new Exception(String.Format("At least two data points are required for relationship ({0})", this.Name));
			}
		}
	}

	///// <summary>
	///// Point for relationship
	///// </summary>
	//[Serializable]
	//[ViewName("UserInterface.Views.GridView")]
	//[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	//public class Point: Model
	//{
	//	/// <summary>
	//	/// X value
	//	/// </summary>
	//	[Description("x value")]
	//	public double X { get; set; }

	//	/// <summary>
	//	/// Y value
	//	/// </summary>
	//	[Description("y value")]
	//	public double Y { get; set; }

	//}
}
