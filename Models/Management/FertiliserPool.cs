using System;
using System.Collections.Generic;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using System.Linq;
using Models.Functions;
using Models.Soils;

namespace Models;

/// <summary>
/// Encapsulates an amount of fertiliser that has been applied. The pools will optionally
/// be released over time via the 'release' function if it is specified.
/// </summary>
public class FertiliserPool : Model
{
    private readonly ISummary summary;
    private readonly Fertiliser fertiliser;
    private IFunction releaseRate;
    private readonly IEnumerable<(ISolute solute, double fraction)> solutesToApply;
    private readonly double depthTop;
    private readonly double depthBottom;
    private readonly bool doOutput;
    private readonly double[] cumThickness;
    private double minimumAmount;

    private double initialAmount;

    /// <summary>Amount of fertiliser in pool.</summary>
    public string FertiliserTypeName { get; private set; }

    /// <summary>Amount of fertiliser in pool.</summary>
    public double Amount { get; private set; }

    /// <summary>Age of associated fertiliser pool.</summary>
    public int Age { get; private set; }

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
        this.solutesToApply = solutes;
        this.minimumAmount = (1 - fertiliserType.FractionWhenRemainderReleased) * amount;
        this.depthTop = depthTop;
        this.depthBottom = depthBottom;
        this.doOutput = doOutput;

        Name = fertiliserType.Name;
        FertiliserTypeName = fertiliserType.Name;
        initialAmount = amount;
        Amount = amount;
        cumThickness = SoilUtilities.ToCumThickness(thickness);
    }

    /// <summary>
    /// Set the release function.
    /// </summary>
    /// <param name="f">The function.</param>
    public void SetReleaseFunction(IFunction f)
    {
        releaseRate = f;
    }

    /// <summary>
    /// Perform daily release of fertiliser to solute pools.
    /// </summary>
    /// <returns>The amount of fertiliser (kg/ha) applied</returns>
    internal double PerformRelease()
    {
        // Calculate the rate of release for today.
        double rate = releaseRate.Value();

        // Determine the amount to add and remove it from our state variable.
        double amountToAdd = initialAmount * MathUtilities.Constrain(rate, 0, 1);
        amountToAdd = MathUtilities.Constrain(amountToAdd, 0, Amount);
        Amount -= amountToAdd;
        if (Amount <= minimumAmount)
        {
            amountToAdd += Amount;
            Amount = 0;
        }

        Apply(amountToAdd, depthTop, depthBottom, cumThickness, solutesToApply,
              summary: doOutput ? summary : null,
              fertiliser: fertiliser,
              fertiliserTypeName: FertiliserTypeName);

        // Increment age of pool
        Age++;

        return amountToAdd;
    }

    /// <summary>
    /// Send fertiliser to solutes.
    /// </summary>
    /// <param name="amountToAdd">Amount of fertiliser to apply (kg/ha)</param>
    /// <param name="depthTop">The upper soil depth to apply fertiliser to (mm).</param>
    /// <param name="depthBottom">The lower soil depth to apply fertiliser to (mm).</param>
    /// <param name="cumThickness">Cumulative soil layer thickness</param>
    /// <param name="solutesToApply">Collection of solute/fraction tuples</param>
    /// <param name="summary">Instance of summary model</param>
    /// <param name="fertiliser">Instance of fertiliser model</param>
    /// <param name="fertiliserTypeName">Type of fertiliser to apply</param>
    internal static void Apply(double amountToAdd, double depthTop, double depthBottom, double[] cumThickness,
                               IEnumerable<(ISolute solute, double fraction)> solutesToApply,
                               ISummary summary,
                               Fertiliser fertiliser,
                               string fertiliserTypeName)
    {
        // If only one depth specified then calculate the depth top and bottom to be a whole layer.
        if (depthBottom == depthTop || depthBottom == -1)
        {
            int layer;
            for (layer = 0; layer < cumThickness.Length; layer++)
                if (cumThickness[layer] >= depthTop) break;

            if (layer == cumThickness.Length)
                throw new Exception("Depth deeper than bottom of soil profile");
            if (layer == 0)
                depthTop = 0;
            else
                depthTop = cumThickness[layer - 1];
            depthBottom = cumThickness[layer];
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

                if (amountForLayer > 0)
                {
                    // Move all solutes
                    foreach (var soluteToApply in solutesToApply)
                        soluteToApply.solute.AddToLayer(amount: soluteToApply.fraction * amountForLayer, layerIndex);

                    amountApplied += amountForLayer;

                    // Optionally write message to summary file.
                    summary?.WriteMessage(fertiliser, $"{amountForLayer:F1} kg/ha of {fertiliserTypeName} added at depth {cumThickness[layerIndex]:F0} layer {layerIndex + 1}", MessageType.Diagnostic);
                }
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
            FertiliserType = fertiliserTypeName
        });

    }
}