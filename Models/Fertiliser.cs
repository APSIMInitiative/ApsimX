using System;
using Models.Core;
using System.Xml;
using Models.Soils;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Models
{
    public class FertiliserType : Model
    {
        public double FractionNO3 { get; set;}
        public double FractionNH4 { get; set;}
        public double FractionUrea { get; set;}
    }

    public class Fertiliser : Model
    {
        // Links
        [Link] private Soil Soil = null;
        [Link] private ISummary Summary = null;

        // Parameters
        public List<FertiliserType> Types { get; set; }

        // Events we're going to send.
        public event NitrogenChangedDelegate NitrogenChanged;

        [XmlIgnore]
        [Units("kg/ha")]
        public double FertiliserApplied { get; private set; }

        /// <summary>
        /// Apply fertiliser.
        /// </summary>
        public void Apply(double amount, double depth, string type)
        {
            if (amount > 0 && NitrogenChanged != null)
            {
                // find the layer that the fertilizer is to be added to.
                int layer = GetLayerDepth(depth, Soil.Thickness);

                FertiliserType fertiliserType = this.Get(type) as FertiliserType;
                if (fertiliserType == null)
                    throw new ApsimXException(FullPath, "Cannot find fertiliser type '" + type + "'");

                NitrogenChangedType NitrogenChanges = new NitrogenChangedType();
                NitrogenChanges.Sender = FullPath;

                if ((fertiliserType.FractionNO3 + fertiliserType.FractionNH4 + fertiliserType.FractionUrea) != 1.0)
                    throw new ApsimXException(FullPath, "The NO3, NH4 and Urea fractions of " + type + "must sum to a value of 1.0 ");
                
                if (fertiliserType.FractionNO3 != 0)
                {
                    NitrogenChanges.DeltaNO3 = new double[Soil.Thickness.Length];
                    NitrogenChanges.DeltaNO3[layer] = amount * fertiliserType.FractionNO3;
                }
                if (fertiliserType.FractionNH4 != 0)
                {
                    NitrogenChanges.DeltaNH4 = new double[Soil.Thickness.Length];
                    NitrogenChanges.DeltaNH4[layer] = amount * fertiliserType.FractionNH4;
                }
                if (fertiliserType.FractionUrea != 0)
                {
                    NitrogenChanges.DeltaUrea = new double[Soil.Thickness.Length];
                    NitrogenChanges.DeltaUrea[layer] = amount * fertiliserType.FractionUrea;
                }

                FertiliserApplied = amount;
                NitrogenChanged.Invoke(NitrogenChanges);
                Summary.WriteMessage(string.Format("{0} kg/ha of {1} added at depth {2} layer {3}", amount, type, depth, layer + 1));
            }
        }

        /// <summary>
        /// Tick event handler from Clock.
        /// </summary>
        [EventSubscribe("Tick")]
        private void OnTick(object sender, EventArgs e)
        {
            FertiliserApplied = 0;
        }

        /// <summary>
        /// Utility function for determining the layer where 'depth' is located in the 'Thickness' array.
        /// </summary>
        private int GetLayerDepth(double depth, double[] thickness)
        {
            double cum = 0.0;
            for (int i = 0; i < thickness.Length; i++)
            {
                cum += thickness[i];
                if (cum >= depth)
                    return i;
            }
            return thickness.Length - 1;
        }
    }
}