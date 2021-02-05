using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;
using Models.Soils.Nutrients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    /// <summary>This model is responsible for applying fertiliser.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Fertiliser :  ModelCollectionFromResource
    {
        /// <summary>The soil</summary>
        [Link] private IPhysical soilPhysical = null;
        
        /// <summary>The summary</summary>
        [Link] private ISummary Summary = null;

        /// <summary>NO3 solute</summary>
        [Link(ByName = true)] private ISolute NO3 = null;

        /// <summary>NO3 solute</summary>
        [Link(ByName = true)] private ISolute NH4 = null;

        /// <summary>NO3 solute</summary>
        [Link(ByName = true)] private ISolute Urea = null;

        /// <summary>Gets or sets the definitions.</summary>
        public List<FertiliserType> Definitions { get; set; }

        /// <summary>Invoked whenever fertiliser is applied.</summary>
        public event EventHandler<FertiliserApplicationType> Fertilised;

        /// <summary>The amount of nitrogen applied.</summary>
        [Units("kg/ha")]
        public double NitrogenApplied { get; private set; } = 0;

        /// <summary>Types of fertiliser.</summary>
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
        public void Apply(double Amount, Types Type, double Depth = 0.0, bool doOutput = true)
        {
            if (Amount > 0)
            {
                // find the layer that the fertilizer is to be added to.
                int layer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, Depth);

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

        /// <summary>Invoked by clock at start of each daily timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            NitrogenApplied = 0;
        }
    }
}