using System;
using Models.Core;
using System.Xml;
using Models.Soils;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using Models.Interfaces;

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
    /// Stores information about a fertiliser application.
    /// </summary>
    public class FertiliserApplicationType : EventArgs
    {
        /// <summary>
        /// Amount of fertiliser applied.
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Depth to which fertiliser was applied.
        /// </summary>
        public double Depth { get; set; }

        /// <summary>
        /// Type of fertiliser applied.
        /// </summary>
        public Fertiliser.Types FertiliserType { get; set; }
    }

    /// <summary>
    /// The fertiliser model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Fertiliser : Model
    {
        /// <summary>The soil</summary>
        [Link] private ISoil Soil = null;
        
        /// <summary>The summary</summary>
        [Link] private ISummary Summary = null;

        /// <summary>NO3 solute</summary>
        [Link(ByName = true)] private ISolute NO3 = null;

        /// <summary>NO3 solute</summary>
        [Link(ByName = true)] private ISolute NH4 = null;

        /// <summary>NO3 solute</summary>
        [Link(ByName = true)] private ISolute Urea = null;

        // Parameters
        /// <summary>Gets or sets the definitions.</summary>
        /// <value>The definitions.</value>
        [XmlIgnore]
        public List<FertiliserType> Definitions { get; set; }

        /// <summary>
        /// Invoked whenever fertiliser is applied.
        /// </summary>
        public event EventHandler<FertiliserApplicationType> Fertilised;

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
        /// <param name="doOutput">If true, output will be written to the summary.</param>
        /// <exception cref="ApsimXException">Cannot find fertiliser type ' + Type + '</exception>
        public void Apply(double Amount, Types Type, double Depth = 0.0, bool doOutput = true)
        {
            if (Amount > 0)
            {
                // find the layer that the fertilizer is to be added to.
                int layer = GetLayerDepth(Depth, Soil.Thickness);

                FertiliserType fertiliserType = Definitions.FirstOrDefault(f => f.Name == Type.ToString());
                if (fertiliserType == null)
                    throw new ApsimXException(this, "Cannot find fertiliser type '" + Type + "'");

                // We find the current amount of N in each form, add to it as needed, 
                // then set the new value. An alternative approach could call AddKgHaDelta
                // rather than SetKgHa
                if (fertiliserType.FractionNO3 != 0)
                {
                    var values = NO3.kgha;
                    values[layer] += Amount * fertiliserType.FractionNO3;
                    NO3.SetKgHa(SoluteSetterType.Fertiliser, values);
                    NitrogenApplied += Amount * fertiliserType.FractionNO3;
                }
                if (fertiliserType.FractionNH4 != 0)
                {
                    var values = NH4.kgha;
                    values[layer] += Amount * fertiliserType.FractionNH4;
                    NH4.SetKgHa(SoluteSetterType.Fertiliser, values);
                    NitrogenApplied += Amount * fertiliserType.FractionNH4;
                }
                if (fertiliserType.FractionUrea != 0)
                {
                    var values = Urea.kgha;
                    values[layer] += Amount * fertiliserType.FractionUrea;
                    Urea.SetKgHa(SoluteSetterType.Fertiliser, values);
                    NitrogenApplied += Amount * fertiliserType.FractionUrea;
                }
                if (doOutput)
                    Summary.WriteMessage(this, string.Format("{0} kg/ha of {1} added at depth {2} layer {3}", Amount, Type, Depth, layer + 1));

                Fertilised?.Invoke(this, new FertiliserApplicationType() { Amount = Amount, Depth = Depth, FertiliserType = Type });
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