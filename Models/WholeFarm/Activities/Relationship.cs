using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public double Value { get; set; }

		/// <summary>
		/// Starting value
		/// </summary>
		[Description("Value at start of simulation")]
		public double StartingValue { get; set; }

		/// <summary>
		/// Minimum value possible
		/// </summary>
		[Description("Minimum value possible")]
		public double Minumum { get; set; }

		/// <summary>
		/// Maximum value possible
		/// </summary>
		[Description("Maximum value possible")]
		public double Maximum { get; set; }

		/// <summary>
		/// Extrapolate beyond given values
		/// </summary>
		[Description("Extrapolate beyond given values")]
		public bool ExtropolateEquation { get; set; }

		/// <summary>
		/// List of points to define relationship 
		/// </summary>
		public List<Point> Points { get; set; }

		/// <summary>
		/// Solve equation for y given x
		/// </summary>
		/// <param name="X">x value to solve y</param>
		/// <returns>y value for given x</returns>
		public double SolveY(double X)
		{
			return 0;
		}

		/// <summary>
		/// Modify the current value by Y calculated from x
		/// </summary>
		/// <param name="x">x value</param>
		public void Modify(double x)
		{
			Value += SolveY(x);
		}

		/// <summary>
		/// Calculate new value using Y calculated from x
		/// </summary>
		/// <param name="x">x value</param>
		public void Calculate(double x)
		{
			Value = SolveY(x);
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			Value = StartingValue;

			// Get points from children
			Points = this.Children.Where(a => a.GetType() == typeof(Point)).Cast<Point>().ToList();
			if(Points.Count == 0)
			{
				Summary.WriteWarning(this, String.Format("No data points were provided for relationship ({0})", this.Name));
			}
			if (Points.Count < 2)
			{
				Summary.WriteWarning(this, String.Format("Tow or more data points required for relationship ({0})", this.Name));
			}
		}
	}

	/// <summary>
	/// Point for relationship
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class Point: Model
	{
		/// <summary>
		/// X value
		/// </summary>
		[Description("x value")]
		public double X { get; set; }

		/// <summary>
		/// Y value
		/// </summary>
		[Description("y value")]
		public double Y { get; set; }

	}
}
