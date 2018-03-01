// ----------------------------------------------------------------------
// <copyright file="BaseFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using System;

    /// <summary>
    /// # [Name]
    /// Calculate partitioning of daily growth based upon allometric relationship
    /// </summary>
    [Serializable]
    [Description("This function calculated dry matter demand using plant allometry which is described using a simple power function (y=kX^p).")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AllometricDemandFunction : BaseFunction
    {
        /// <summary>The constant</summary>
        [Description("Constant")]
        public double Const { get; set; }

        /// <summary>The power</summary>
        [Description("Power")]
        public double Power { get; set; }

        /// <summary>The x property</summary>
        [Description("XProperty")]
        public string XProperty { get; set; }

        /// <summary>The y property</summary>
        [Description("YProperty")]
        public string YProperty { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            object xValue = Apsim.Get(this, XProperty);
            if (xValue == null)
                throw new Exception("Cannot find variable: " + XProperty + " in function: " + this.Name);

            object yValue = Apsim.Get(this, YProperty);
            if (yValue == null)
                throw new Exception("Cannot find variable: " + YProperty + " in function: " + this.Name);

            if (!(xValue is double))
                throw new Exception("In function: " + Name + " the x property must be a double");
            if (!(yValue is double))
                throw new Exception("In function: " + Name + " the y property must be a double");


            double Target = Const * Math.Pow((double)xValue, Power);
            return new double[] { Math.Max(0.0, Target - (double)yValue) };
        }

    }
}
