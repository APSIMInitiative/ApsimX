using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// A plant height function
    /// </summary>
    [Serializable]
    [Description("Calculates the potential height increment and then multiplies it by the smallest of any childern functions (Child functions represent stress)")]
    public class HeightFunction : Function
    {
        /// <summary>The potential height</summary>
        [Link]
        Function PotentialHeight = null;
        /// <summary>The potential height yesterday</summary>
        double PotentialHeightYesterday = 0;
        /// <summary>The height</summary>
        double Height = 0;
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>Gets or sets the height of the delta.</summary>
        /// <value>The height of the delta.</value>
        [XmlIgnore]
        public double DeltaHeight { get; set; }
        /// <summary>Updates the variables.</summary>
        /// <param name="initial">The initial.</param>
        public override void UpdateVariables(string initial)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(Function));

            double PotentialHeightIncrement = PotentialHeight.Value - PotentialHeightYesterday;
            double StressValue = 1.0;
            //This function is counting potential height as a stress.
            foreach (Function F in ChildFunctions)
            {
                StressValue = Math.Min(StressValue, F.Value);
            }
            DeltaHeight = PotentialHeightIncrement * StressValue;
            PotentialHeightYesterday = PotentialHeight.Value;
            Height += DeltaHeight;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function));

                return Height;
            }
        }
    }
}
