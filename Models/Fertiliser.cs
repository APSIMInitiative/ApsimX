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
        public string Description { get; set; }
        public double FractionNO3 { get; set; }
        public double FractionNH4 { get; set; }
        public double FractionUrea { get; set; }
        public double FractionRockP { get; set;}
        public double FractionBandedP{get;set;}
        public double FractionLabileP{get;set;}
        public double FractionCa { get; set; }
    }
        [Serializable]

    public class Fertiliser : Model
    {
        // Links
        [Link] private Soil Soil = null;
        [Link] private ISummary Summary = null;

        // Parameters
        public List<FertiliserType> Definitions { get; set; }

        private void AddDefinitions()
        {
            Definitions.Add(new FertiliserType { Name = "CalciteCA", Description = "Ca as finely ground Agricultural Lime", FractionCa = 1.0 });
            Definitions.Add(new FertiliserType { Name = "CalciteFine", Description = "finely ground Agricultural Lime", FractionCa = 0.4 });
            Definitions.Add(new FertiliserType { Name = "Dolomite", Description = "finely ground dolomite", FractionCa = 0.22 });
            Definitions.Add(new FertiliserType { Name = "NO3N", Description = "N as nitrate", FractionNO3 = 1.0 });
            Definitions.Add(new FertiliserType { Name = "NH4N", Description = "N as ammonium", FractionNH4 = 1.0 });
            Definitions.Add(new FertiliserType { Name = "NH4NO3N", Description = "ammonium nitrate", FractionNH4 = 0.5, FractionNO3 = 0.5 });
            Definitions.Add(new FertiliserType { Name = "DAP", Description = "di-ammonium phosphate", FractionNH4 = 0.18 });
            Definitions.Add(new FertiliserType { Name = "MAP", Description = "mono-ammonium phosphate", FractionNH4 = 0.11 });
            Definitions.Add(new FertiliserType { Name = "UreaN", Description = "N as urea", FractionUrea = 1.0 });
            Definitions.Add(new FertiliserType { Name = "UreaNO3", Description = "N as urea", FractionNO3 = 0.5, FractionUrea = 0.5 });
            Definitions.Add(new FertiliserType { Name = "Urea", Description = "Urea fertiliser", FractionUrea = 0.46 });
            Definitions.Add(new FertiliserType { Name = "NH4SO4N", Description = "ammonium sulphate", FractionNH4 = 1.0 });
            Definitions.Add(new FertiliserType { Name = "RockP", Description = "Rock phosphorus", FractionRockP = 0.8, FractionLabileP = 0.2 });
            Definitions.Add(new FertiliserType { Name = "BandedP", Description = "Banded phosphorus", FractionBandedP = 1.0 });
            Definitions.Add(new FertiliserType { Name = "BroadcastP", Description = "Broadcast phosphorus", FractionLabileP = 1.0 });
        }


        // Events we're going to send.
        public event NitrogenChangedDelegate NitrogenChanged;

        [XmlIgnore]
        [Units("kg/ha")]
        public double NitrogenApplied { get ; private set; }

        public enum Types { CalciteCA, CalciteFine, Dolomite, NO3N, NH4N, NH4NO3N, 
                            DAP, MAP, UreaN, UreaNO3, Urea, NH4SO4N, RockP, BandedP, BroadcastP };

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
            AddDefinitions();
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