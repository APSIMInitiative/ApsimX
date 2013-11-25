using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Calculates the maximum leaf size (mm2/leaf) given its node position (Elings, 2000 - Agronomy Journal 92, 436-444)")]
    public class BellCurveFunction : Function
    {
        public Function LargestLeafPosition { get; set; } // Node position where the largest leaf occurs (e.g. 10 is the 10th leaf from bottom to top)

        public Function AreaMax { get; set; }             // Area of the largest leaf of a plant (mm2)

        public Function Breadth { get; set; }

        public Function Skewness { get; set; }

        public Structure Structure { get; set; }


        
        public override double Value
        {
            get
            {
                double LeafSizePerNode = 0; // Current Size of a leaf at a given node position (mm2/leaf)

                double LeafNo = Structure.MainStemNodeNo;

                LeafSizePerNode = AreaMax.Value * Math.Exp(Breadth.Value * Math.Pow(LeafNo - LargestLeafPosition.Value, 2.0)
                                  + Skewness.Value * (Math.Pow(LeafNo - LargestLeafPosition.Value, 3.0)));

                return LeafSizePerNode;

            }
        }
    }
}
