using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>Calculate partitioning of daily growth based upon allometric relationship</summary>
    [Serializable]
    [Description("This function calculated dry matter demand using plant allometry which is described using a simple power function (y=kX^p).")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AllometricDemandFunction : Model, IFunction
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
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Cannot find variable:  + XProperty +  in function:  + this.Name
        /// or
        /// Cannot find variable:  + YProperty +  in function:  + this.Name
        /// </exception>
        public double Value
        {
            get
            {
                double returnValue = 0.0;
                object XValue = Apsim.Get(this, XProperty);
                if (XValue == null)
                    throw new Exception("Cannot find variable: " + XProperty + " in function: " + this.Name);
                object YValue = Apsim.Get(this, YProperty);
                if (YValue == null)
                    throw new Exception("Cannot find variable: " + YProperty + " in function: " + this.Name);

                double Target = Const * Math.Pow((double)XValue, Power);
                returnValue = Math.Max(0.0, Target - (double)YValue);

                return returnValue;
            }
        }

    }
}
