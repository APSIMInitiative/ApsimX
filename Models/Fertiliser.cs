using System;
using Models.Core;
using System.Xml;
using Models.Soils;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    [Serializable]
    public class FertiliserType
    {
        public string Name { get; set; }
        public double FractionNO3 { get; set;}
        public double FractionNH4 { get; set;}
        public double FractionUrea { get; set;}
    }
        [Serializable]

    public class Fertiliser : Model
    {
        // Links
        [Link] private Soil Soil = null;
        [Link] private ISummary Summary = null;

        // Parameters
        public List<FertiliserType> Definitions { get; set; }

        // Events we're going to send.
        public event NitrogenChangedDelegate NitrogenChanged;

        [XmlIgnore]
        [Units("kg/ha")]
        public double NitrogenApplied { get ; private set; }

        public enum Types { Urea, DAP, MAP, UreaN, NO3N, NH4N, NH4NO3N, NH4SO4N };

        /// <summary>
        /// Apply fertiliser.
        /// </summary>
        public void Apply(double Amount, Types Type, double Depth = 0.0)
        {
            if (Amount > 0 && NitrogenChanged != null)
            {
                // find the layer that the fertilizer is to be added to.
                int layer = GetLayerDepth(Depth, Soil.Thickness);

                FertiliserType fertiliserType = Definitions.FirstOrDefault(f => f.Name == Type.ToString());
                if (fertiliserType == null)
                    throw new ApsimXException(FullPath, "Cannot find fertiliser type '" + Type + "'");

                NitrogenChangedType NitrogenChanges = new NitrogenChangedType();
                NitrogenChanges.Sender = FullPath;

                if ((fertiliserType.FractionNO3 + fertiliserType.FractionNH4 + fertiliserType.FractionUrea) != 1.0)
                    throw new ApsimXException(FullPath, "The NO3, NH4 and Urea fractions of " + Type + "must sum to a value of 1.0 ");
                
                if (fertiliserType.FractionNO3 != 0)
                {
                    NitrogenChanges.DeltaNO3 = new double[Soil.Thickness.Length];
                    NitrogenChanges.DeltaNO3[layer] = Amount * fertiliserType.FractionNO3;
                    NitrogenApplied += Amount * fertiliserType.FractionNO3;
                }
                if (fertiliserType.FractionNH4 != 0)
                {
                    NitrogenChanges.DeltaNH4 = new double[Soil.Thickness.Length];
                    NitrogenChanges.DeltaNH4[layer] = Amount * fertiliserType.FractionNH4;
                    NitrogenApplied += Amount * fertiliserType.FractionNH4;
                }
                if (fertiliserType.FractionUrea != 0)
                {
                    NitrogenChanges.DeltaUrea = new double[Soil.Thickness.Length];
                    NitrogenChanges.DeltaUrea[layer] = Amount * fertiliserType.FractionUrea;
                    NitrogenApplied += Amount * fertiliserType.FractionUrea;
                }

                NitrogenChanged.Invoke(NitrogenChanges);
                Summary.WriteMessage(FullPath, string.Format("{0} kg/ha of {1} added at depth {2} layer {3}", Amount, Type, Depth, layer + 1));
            }
        }

        /// <summary>
        /// prepare event handler from Clock.
        /// </summary>
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            NitrogenApplied = 0;
        }

        public override void OnCommencing()
        {
            NitrogenApplied = 0;
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