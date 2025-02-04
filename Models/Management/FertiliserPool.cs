using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;

namespace Models;

/// <summary>
/// Encapsulates an amount of fertiliser that has been applied. The pools will optionally
/// be released over time via the 'release' function if it is specified.
/// </summary>
public class FertiliserPool
{
    private readonly ISummary summary;
    private readonly Fertiliser fertiliser;
    private readonly FertiliserType fertiliserType;
    private readonly IEnumerable<(ISolute solute, double fraction)> solutesToApply;
    private readonly double depthTop;
    private readonly double depthBottom;
    private readonly bool doOutput;
    private readonly double[] cumThickness;
    private double[] deltaArray;
    private double minimumAmount;

    /// <summary>Amount of fertiliser in pool.</summary>
    public double Amount { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fertiliser">Instance of fertiliser model</param>
    /// <param name="summary">Instance of summary model</param>
    /// <param name="fertiliserType">Instance of fertiliser type</param>
    /// <param name="solutes">Collection of solute/fraction tuples</param>
    /// <param name="thickness">Soil layer thickness</param>
    /// <param name="amount">Amount of fertiliser to apply (kg/ha)</param>
    /// <param name="depthTop">The upper soil depth to apply fertiliser to (mm).</param>
    /// <param name="depthBottom">The lower soil depth to apply fertiliser to (mm).</param>
    /// <param name="doOutput">Write a line of output on every fertiliser application?</param>
    public FertiliserPool(Fertiliser fertiliser, ISummary summary, FertiliserType fertiliserType,
                          IEnumerable<(ISolute solute, double fraction)> solutes,
                          double[] thickness, double amount, double depthTop, double depthBottom, bool doOutput)
    {
        this.fertiliser = fertiliser;
        this.summary = summary;
        this.fertiliserType = fertiliserType;
        this.solutesToApply = solutes;
        this.minimumAmount = (1 - fertiliserType.FractionWhenRemainderReleased) * amount;
        this.depthTop = depthTop;
        this.depthBottom = depthBottom;
        this.doOutput = doOutput;
        Amount = amount;
        cumThickness = SoilUtilities.ToCumThickness(thickness);

        if (depthBottom == depthTop || depthBottom == -1)
        {
            int layer = SoilUtilities.LayerIndexOfDepth(thickness, depthTop);
            this.depthBottom = depthTop + thickness[layer];
        }
    }

    /// <summary>
    /// Perform daily release of fertiliser to solute pools.
    /// </summary>
    /// <returns>The amount of fertiliser (kg/ha) applied</returns>
    internal double PerformRelease()
    {
        // Calculate the rate of release for today.
        double rate = fertiliserType.releaseRate.Value();

        // Determine the amount to add and remove it from our state variable.
        double amountToAdd = Amount * rate;
        Amount -= amountToAdd;
        if (Amount <= minimumAmount)
        {
            amountToAdd += Amount;
            Amount = 0;
        }

        double amountApplied = 0;
        double topOfLayer = 0;
        for (int layerIndex = 0; layerIndex < cumThickness.Length; layerIndex++)
        {
            double bottomOfLayer = cumThickness[layerIndex];

            double top = MathUtilities.Bound(depthTop, topOfLayer, bottomOfLayer);
            double bottom = MathUtilities.Bound(depthBottom, topOfLayer, bottomOfLayer);
            if (bottom > top)
            {
                double amountForLayer = (bottom - top) / (depthBottom - depthTop) * amountToAdd;

                // Move all solutes
                foreach (var soluteToApply in solutesToApply)
                    MoveToSolute(soluteToApply.solute, soluteToApply.fraction * amountForLayer, layerIndex);

                amountApplied += amountForLayer;

                // Optionally write message to summary file.
                if (doOutput)
                    summary.WriteMessage(fertiliserType, $"{amountForLayer:F1} kg/ha of {fertiliserType.Name} added at depth {cumThickness[layerIndex]:F0} layer {layerIndex + 1}", MessageType.Diagnostic);
            }
            topOfLayer = cumThickness[layerIndex];
        }

        // check to make sure we applied the full amount.
        if (!MathUtilities.FloatsAreEqual(amountApplied, amountToAdd))
            throw new Exception($"Internal error: The amount of fertiliser applied ({amountApplied} does not equal the amount that should have been applied {amountToAdd})");

        fertiliser.InvokeNotification(new FertiliserApplicationType()
        {
            Amount = amountToAdd,
            DepthTop = depthTop,
            DepthBottom = depthBottom,
            FertiliserType = fertiliserType.Name
        });

        return amountToAdd;
    }

    /// <summary>
    /// Move an amount of fertiliser to solute.
    /// </summary>
    /// <param name="solute">The destination solute</param>
    /// <param name="amountToAdd">The amount to move.</param>
    /// <param name="layerIndex">The index of the soil layer to move solute to.</param>
    private void MoveToSolute(ISolute solute, double amountToAdd, int layerIndex)
    {
        deltaArray ??= new double[cumThickness.Length];

        deltaArray[layerIndex] = amountToAdd;
        solute.AddKgHaDelta(SoluteSetterType.Fertiliser, deltaArray);

        // Zero the array for next time this method is called.
        deltaArray[layerIndex] = 0;
    }
}