using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Functions;
using Models.Soils;

namespace Models;

/// <summary>This model is responsible for applying fertiliser.</summary>
[Serializable]
[ValidParent(ParentType = typeof(Zone))]
public class Fertiliser : Model
{
    /// <summary>The soil</summary>
    [Link] private readonly IPhysical physical = null;

    /// <summary>The summary</summary>
    [Link] private readonly ISummary summary = null;

    /// <summary>Collection of solutes</summary>
    [Link] private readonly ISolute[] solutes = null;

    /// <summary>Gets or sets the definitions.</summary>
    [Link(Type = LinkType.Child)]
    private List<FertiliserType> Definitions { get; set; }

    private readonly List<FertiliserPool> pools = [];

    /// <summary>Invoked whenever fertiliser is applied.</summary>
    public event EventHandler<FertiliserApplicationType> Fertilised;

    /// <summary>The amount of nitrogen applied.</summary>
    [Units("kg/ha")]
    public double NitrogenApplied { get; private set; } = 0;

    /// <summary>Apply fertiliser.</summary>
    /// <param name="amount">The amount.</param>
    /// <param name="type">The type.</param>
    /// <param name="depth">The upper depth (mm) to apply the fertiliser.</param>
    /// <param name="depthBottom">The lower depth (mm) to apply the fertiliser.</param>
    /// <param name="doOutput">If true, output will be written to the summary.</param>
    public void Apply(double amount, string type, double depth = 0, double depthBottom = -1, bool doOutput = true)
    {
        if (amount > 0)
        {
            FertiliserType fertiliserType = Definitions.FirstOrDefault(f => f.Name == type)
                ?? throw new ApsimXException(this, $"Cannot apply unknown fertiliser type: {type}");
            List<(ISolute solute, double fraction)> solutesToApply = [];
            AddFertiliserSoluteSpecToArray(fertiliserType.Solute1Name, fertiliserType.Solute1Fraction, solutesToApply);
            AddFertiliserSoluteSpecToArray(fertiliserType.Solute2Name, fertiliserType.Solute2Fraction, solutesToApply);
            AddFertiliserSoluteSpecToArray(fertiliserType.Solute3Name, fertiliserType.Solute3Fraction, solutesToApply);
            AddFertiliserSoluteSpecToArray(fertiliserType.Solute4Name, fertiliserType.Solute4Fraction, solutesToApply);
            AddFertiliserSoluteSpecToArray(fertiliserType.Solute5Name, fertiliserType.Solute5Fraction, solutesToApply);
            AddFertiliserSoluteSpecToArray(fertiliserType.Solute6Name, fertiliserType.Solute6Fraction, solutesToApply);

            var newPool = new FertiliserPool(this, summary, fertiliserType, solutesToApply, physical.Thickness,
                                             amount, depth, depthBottom, doOutput);
            var node = Services.GetNode(this);
            var poolNode = node.AddChild(newPool);

            // find and clone fertiliser release function (child of FertiliserType) so that the release rate function
            // can hold state that is specific to this fertiliser application
            var releaseRate = fertiliserType.FindChild<IFunction>("Release");
            if (releaseRate == null)
                throw new Exception($"Cannot find a release rate function for fertiliser type: {fertiliserType.Name}");
            releaseRate = releaseRate.Clone();
            poolNode.AddChild(releaseRate as INodeModel);
            newPool.SetReleaseFunction(releaseRate);
        }
    }

    /// <summary>
    /// Add a fertiliser solute specification to an array that will be passed to a pool.
    /// </summary>
    /// <param name="name">Name of the solute</param>
    /// <param name="fraction">Fration of solute in fertiliser type</param>
    /// <param name="solutesTuple">The array to add to.</param>
    private void AddFertiliserSoluteSpecToArray(string name, double fraction, List<(ISolute solute, double fraction)> solutesTuple)
    {
        if (!string.IsNullOrEmpty(name))
            solutesTuple.Add((solutes.FirstOrDefault(sol => sol.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new Exception($"Cannot find solute: {name}"),
                              fraction));
    }

    /// <summary>Invoked by clock at start of each daily timestep.</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    [EventSubscribe("DoDailyInitialisation")]
    private void OnDoDailyInitialisation(object sender, EventArgs e)
    {
        NitrogenApplied = 0;
    }

    /// <summary>Invoked by clock at start of each daily timestep to do all fertiliser applications for the day.</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event data.</param>
    [EventSubscribe("DoFertiliserApplications")]
    private void OnDoFertiliserApplications(object sender, EventArgs e)
    {
        foreach (FertiliserPool pool in Children.Where(child => child is FertiliserPool)
                                                .ToArray())
        {
            NitrogenApplied += pool.PerformRelease();

            // Remove pools that are empty.
            if (pool.Amount == 0)
                Structure.Delete(pool);
        }
    }

    /// <summary>
    /// Called by pool to invoke a fertilised event.
    /// </summary>
    /// <param name="fertiliserApplicationType">Event data.</param>
    internal void InvokeNotification(FertiliserApplicationType fertiliserApplicationType)
    {
        Fertilised?.Invoke(this, fertiliserApplicationType);
    }
}