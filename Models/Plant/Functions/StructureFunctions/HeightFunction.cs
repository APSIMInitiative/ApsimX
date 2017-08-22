using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// Height is used by the MicroClimate model to calculate the aerodynamic resistance used for calculation of potential transpiration.
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
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));

            double PotentialHeightIncrement = PotentialHeight.Value(arrayIndex) - PotentialHeightYesterday;
            double StressValue = 1.0;
            //This function is counting potential height as a stress.
            foreach (IFunction F in ChildFunctions)
            {
                StressValue = Math.Min(StressValue, F.Value(arrayIndex));
            }
            DeltaHeight = PotentialHeightIncrement * StressValue;
            PotentialHeightYesterday = PotentialHeight.Value(arrayIndex);
            Height += DeltaHeight;
            return Height;
        }
    }
}
