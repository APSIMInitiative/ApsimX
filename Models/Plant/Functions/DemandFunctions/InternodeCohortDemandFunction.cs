// ----------------------------------------------------------------------
// <copyright file="InternodeCohortDemandFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using Models.PMF.Organs;
    using Models.PMF.Struct;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// # [Name]
    /// Calculate individual internode demand base on age and maxSize
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Calculate individual internode demand base on age and maxSize.")]
    public class InternodeCohortDemandFunction : BaseFunction
    {
        /// <summary>YinBetaFunction Constructor</summary>
        public InternodeCohortDemandFunction()
        {
            Lmax = 1.0;
            te = 1.0;
            tm = 1.0;
        }
        /// <summary>Lmax</summary>
        [Description("Lmax")]
        public double Lmax { get; set; }
        /// <summary>te</summary>
        [Description("te")]
        public double te { get; set; }
        /// <summary>The tm</summary>
        [Description("tm")]
        public double tm { get; set; }

        /// <summary>The leaf </summary>
        [Link]
        Leaf Leaf = null;
        
        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double sinkStrength = 0;
            foreach (LeafCohort L in Leaf.Leaves)
            {
                if (L.IsAppeared)
                { 
                    double maxSinkStrength = Lmax * ((2 * te - tm) / (te * (te - tm))) *
                        Math.Pow((tm / te), (tm / (te - tm)));
                    double result = maxSinkStrength * 
                        ((te - L.Age) / (te - tm)) * 
                        Math.Pow((L.Age / tm), (tm / (te - tm)));

                     
                    if (L.Age > te)
                    {
                        sinkStrength += 0;
                    }
                    else
                    {
                        sinkStrength += result;
                    }
                } 
            }

            double[] returnValue =  new double[] { Structure.TotalStemPopn * sinkStrength };
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + returnValue[0]);
            return returnValue;
        }
    }
}   
