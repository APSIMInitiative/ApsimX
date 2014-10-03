using System;
using Models.Core;
using System.Xml;
using Models.Soils;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    /// <summary>
    /// A class for holding fertiliser types
    /// </summary>
    [Serializable]
    public class FertiliserType
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>Gets or sets the fraction n o3.</summary>
        /// <value>The fraction n o3.</value>
        public double FractionNO3 { get; set; }
        /// <summary>Gets or sets the fraction n h4.</summary>
        /// <value>The fraction n h4.</value>
        public double FractionNH4 { get; set; }
        /// <summary>Gets or sets the fraction urea.</summary>
        /// <value>The fraction urea.</value>
        public double FractionUrea { get; set; }
        /// <summary>Gets or sets the fraction rock p.</summary>
        /// <value>The fraction rock p.</value>
        public double FractionRockP { get; set;}
        /// <summary>Gets or sets the fraction banded p.</summary>
        /// <value>The fraction banded p.</value>
        public double FractionBandedP{get;set;}
        /// <summary>Gets or sets the fraction labile p.</summary>
        /// <value>The fraction labile p.</value>
        public double FractionLabileP{get;set;}
        /// <summary>Gets or sets the fraction ca.</summary>
        /// <value>The fraction ca.</value>
        public double FractionCa { get; set; }
    }
    /// <summary>
    /// The fertiliser model
    /// </summary>
    [Serializable]
    public class Fertiliser : Model
    {
        // Links
        /// <summary>The soil</summary>
        [Link] private Soil Soil = null;
        /// <summary>The summary</summary>
        [Link] private ISummary Summary = null;

        // Parameters
        /// <summary>Gets or sets the definitions.</summary>
        /// <value>The definitions.</value>
        [XmlIgnore]
        public List<FertiliserType> Definitions { get; set; }

        /// <summary>Adds the definitions.</summary>
        private void AddDefinitions()
        {
            Definitions = new List<FertiliserType>();
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
        /// <summary>Occurs when [nitrogen changed].</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        /// <summary>Gets the nitrogen applied.</summary>
        /// <value>The nitrogen applied.</value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double NitrogenApplied { get ; private set; }

        /// <summary>
        /// 
        /// </summary>
        public enum Types 
        {
            /// <summary>The calcite ca</summary>
            CalciteCA,
            /// <summary>The calcite fine</summary>
            CalciteFine,
            /// <summary>The dolomite</summary>
            Dolomite,
            /// <summary>The n o3 n</summary>
            NO3N,
            /// <summary>The n h4 n</summary>
            NH4N,
            /// <summary>The n h4 n o3 n</summary>
            NH4NO3N,
            /// <summary>The dap</summary>
            DAP,
            /// <summary>The map</summary>
            MAP,
            /// <summary>The urea n</summary>
            UreaN,
            /// <summary>The urea n o3</summary>
            UreaNO3,
            /// <summary>The urea</summary>
            Urea,
            /// <summary>The n h4 s o4 n</summary>
            NH4SO4N,
            /// <summary>The rock p</summary>
            RockP,
            /// <summary>The banded p</summary>
            BandedP,
            /// <summary>The broadcast p</summary>
            BroadcastP 
        };

        /// <summary>Apply fertiliser.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="Type">The type.</param>
        /// <param name="Depth">The depth.</param>
        /// <exception cref="ApsimXException">Cannot find fertiliser type ' + Type + '</exception>
        public void Apply(double Amount, Types Type, double Depth = 0.0)
        {
            if (Amount > 0 && NitrogenChanged != null)
            {
                // find the layer that the fertilizer is to be added to.
                int layer = GetLayerDepth(Depth, Soil.Thickness);

                FertiliserType fertiliserType = Definitions.FirstOrDefault(f => f.Name == Type.ToString());
                if (fertiliserType == null)
                    throw new ApsimXException(this, "Cannot find fertiliser type '" + Type + "'");

                NitrogenChangedType NitrogenChanges = new NitrogenChangedType();
                NitrogenChanges.Sender = Apsim.FullPath(this);
               
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
                Summary.WriteMessage(this, string.Format("{0} kg/ha of {1} added at depth {2} layer {3}", Amount, Type, Depth, layer + 1));
            }
        }

        /// <summary>prepare event handler from Clock.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            NitrogenApplied = 0;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            NitrogenApplied = 0;
            AddDefinitions();
        }

        /// <summary>
        /// Utility function for determining the layer where 'depth' is located in the 'Thickness' array.
        /// </summary>
        /// <param name="depth">The depth.</param>
        /// <param name="thickness">The thickness.</param>
        /// <returns></returns>
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