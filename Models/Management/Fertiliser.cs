using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;

namespace Models
{
    /// <summary>This model is responsible for applying fertiliser.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Fertiliser : Model
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
        [Link(Type = LinkType.Child)]
        public List<FertiliserType> Definitions { get; set; }

        /// <summary>Invoked whenever fertiliser is applied.</summary>
        public event EventHandler<FertiliserApplicationType> Fertilised;

        /// <summary>The amount of nitrogen applied.</summary>
        [Units("kg/ha")]
        public double NitrogenApplied { get; private set; } = 0;

        /// <summary>Apply fertiliser.</summary>
        /// <param name="amount">The amount.</param>
        /// <param name="type">The type.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="doOutput">If true, output will be written to the summary.</param>
        public void Apply(double amount, string type, double depth = 0.0, bool doOutput = true)
        {
            if (amount > 0)
            {
                // find the layer that the fertilizer is to be added to.
                int layer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, depth);

                ApplyToLayer(layer, amount, type, doOutput);
                Fertilised?.Invoke(this, new FertiliserApplicationType() { Amount = amount, DepthTop = depth, DepthBottom = depth, FertiliserType = type });
            }
        }

        /// <summary>Apply fertiliser.</summary>
        /// <param name="amount">The amount.</param>
        /// <param name="type">The type.</param>
        /// <param name="depthTop">The upper depth (mm) to apply the fertiliser.</param>
        /// <param name="depthBottom">The lower depth (mm) to apply the fertiliser.</param>
        /// <param name="doOutput">If true, output will be written to the summary.</param>
        public void Apply(double amount, string type, double depthTop, double depthBottom, bool doOutput = true)
        {
            if (amount > 0)
            {
                double topOfLayer = depthTop;
                var cumThickness = SoilUtilities.ToCumThickness(soilPhysical.Thickness);
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    double bottomOfLayer = Math.Min(depthBottom, cumThickness[i]);
                    double soilInLayer = Math.Max(0, bottomOfLayer - topOfLayer);
                    double amountForLayer = soilInLayer / (depthBottom - depthTop) * amount;
                    ApplyToLayer(i, amountForLayer, type, doOutput);
                    topOfLayer = cumThickness[i];
                }

                Fertilised?.Invoke(this, new FertiliserApplicationType() { Amount = amount, DepthTop = depthTop, DepthBottom = depthBottom, FertiliserType = type });
            }
        }

        private void ApplyToLayer(int layer, double amount, string type, bool doOutput)
        {
            if (amount > 0)
            {
                FertiliserType fertiliserType = Definitions.FirstOrDefault(f => f.Name == type);
                if (fertiliserType == null)
                    throw new ApsimXException(this, $"Cannot apply unknown fertiliser type: {type}");

                // We find the current amount of N in each form, add to it as needed,
                // then set the new value. An alternative approach could call AddKgHaDelta
                // rather than SetKgHa
                if (fertiliserType.FractionNO3 != 0)
                {
                    var values = NO3.kgha;
                    values[layer] += amount * fertiliserType.FractionNO3;
                    NO3.SetKgHa(SoluteSetterType.Fertiliser, values);
                    NitrogenApplied += amount * fertiliserType.FractionNO3;
                }
                if (fertiliserType.FractionNH4 != 0)
                {
                    var values = NH4.kgha;
                    values[layer] += amount * fertiliserType.FractionNH4;
                    NH4.SetKgHa(SoluteSetterType.Fertiliser, values);
                    NitrogenApplied += amount * fertiliserType.FractionNH4;
                }
                if (fertiliserType.FractionUrea != 0)
                {
                    var values = Urea.kgha;
                    values[layer] += amount * fertiliserType.FractionUrea;
                    Urea.SetKgHa(SoluteSetterType.Fertiliser, values);
                    NitrogenApplied += amount * fertiliserType.FractionUrea;
                }
                if (doOutput)
                {
                    var cumThickness = SoilUtilities.ToCumThickness(soilPhysical.Thickness);
                    Summary.WriteMessage(this, $"{amount:F1} kg/ha of {type} added at depth {cumThickness[layer]:F0} layer {layer + 1}", MessageType.Diagnostic);
                }
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
