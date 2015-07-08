using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// Calculates the potential height increment and then multiplies it by the smallest of any childern functions (Child functions represent stress).
    /// </summary>
    [Serializable]
    public class HeightFunction : Model, IFunction
    {
        /// <summary>The potential height</summary>
        [Link] IFunction PotentialHeight = null;
        /// <summary>The potential height yesterday</summary>
        double PotentialHeightYesterday = 0;
        /// <summary>The height</summary>
        double Height = 0;
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>Gets or sets the height of the delta.</summary>
        [XmlIgnore]
        public double DeltaHeight { get; set; }

        /// <summary>Gets the value.</summary>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                double PotentialHeightIncrement = PotentialHeight.Value - PotentialHeightYesterday;
                double StressValue = 1.0;
                //This function is counting potential height as a stress.
                foreach (IFunction F in ChildFunctions)
                {
                    StressValue = Math.Min(StressValue, F.Value);
                }
                DeltaHeight = PotentialHeightIncrement * StressValue;
                PotentialHeightYesterday = PotentialHeight.Value;
                Height += DeltaHeight;
                return Height;
            }
        }
    }
}
