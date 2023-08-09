using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// Encapsulates a carbon and nutrient flow between pools.  This flow is characterised in terms of the rate of flow (fraction of the pool per day).  
    /// Carbon loss as CO2 is expressed in terms of the efficiency of C retension within the soil.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(NutrientPool))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class CarbonFlow : Model
    {
        private INutrientPool[] destinations;
        private double[] carbonFlowToDestination;
        private double[] nitrogenFlowToDestination;
        private double[] phosphorusFlowToDestination;

        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction rate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction co2Efficiency = null;

        [Link(ByName = true)]
        private readonly ISolute no3 = null;

        [Link(ByName = true)]
        private readonly ISolute nh4 = null;

        [Link(ByName = true, IsOptional = true)]
        private readonly ISolute labileP = null;

        /// <summary>Amount of N Mineralised (kg/ha)</summary>
        public double[] MineralisedN { get; private set; }

        /// <summary>Amount of P Mineralised (kg/ha)</summary>
        public double[] MineralisedP { get; private set; }

        /// <summary>Total carbon lost to the atmosphere (kg/ha)</summary>
        public double[] Catm { get; private set; }

        /// <summary>Names of destination pools</summary>
        [Description("Names of destination pools (comma separated)")]
        public string[] DestinationNames { get; set; }

        /// <summary>Fractions for each destination pool</summary>
        [Description("Fractions of flow to each pool (comma separated)")]
        public double[] DestinationFraction { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="numberLayers">Number of layers.</param>
        public void Initialise(int numberLayers)
        {
            destinations = new INutrientPool[DestinationNames.Length];

            for (int i = 0; i < DestinationNames.Length; i++)
            {
                INutrientPool destination = FindInScope<INutrientPool>(DestinationNames[i]);
                if (destination == null)
                    throw new Exception("Cannot find destination pool with name: " + DestinationNames[i]);
                destinations[i] = destination;
            }
            MineralisedN = new double[numberLayers];
            MineralisedP = new double[numberLayers];
            Catm = new double[numberLayers];
            carbonFlowToDestination = new double[destinations.Length];
            nitrogenFlowToDestination = new double[destinations.Length];
            phosphorusFlowToDestination = new double[destinations.Length];
        }

        /// <summary>Perform daily flow calculations.</summary>
        public void DoFlow()
        {
            NutrientPool source = Parent as NutrientPool;

            int numLayers = source.C.Count;

            double[] no3 = this.no3.kgha;
            double[] nh4 = this.nh4.kgha;
            double[] labileP;
            if (this.labileP == null)
                labileP = new double[numLayers];
            else
                labileP = this.labileP.kgha;

            int numDestinations = destinations.Length;
            for (int i = 0; i < numLayers; i++)
            {

                double carbonFlowFromSource = rate.Value(i) * source.C[i];
                double nitrogenFlowFromSource = MathUtilities.Divide(carbonFlowFromSource * source.N[i], source.C[i], 0);
                double phosphorusFlowFromSource = MathUtilities.Divide(carbonFlowFromSource * source.P[i], source.C[i], 0);

                double totalNitrogenFlowToDestinations = 0;
                double totalPhosphorusFlowToDestinations = 0;

                double co2Efficiency = this.co2Efficiency.Value(i);
                for (int j = 0; j < numDestinations; j++)
                {
                    var destination = destinations[j];
                    carbonFlowToDestination[j] = carbonFlowFromSource * co2Efficiency * DestinationFraction[j];
                    nitrogenFlowToDestination[j] = MathUtilities.Divide(carbonFlowToDestination[j] * destination.N[i], destination.C[i], 0.0);
                    totalNitrogenFlowToDestinations += nitrogenFlowToDestination[j];
                    phosphorusFlowToDestination[j] = MathUtilities.Divide(carbonFlowToDestination[j] * destination.P[i], destination.C[i], 0.0);
                    totalPhosphorusFlowToDestinations += phosphorusFlowToDestination[j];

                }

                // some pools do not fully occupy a layer (e.g. residue decomposition) and so need to incorporate fraction of layer
                double mineralNSupply = (no3[i] + nh4[i]) * source.LayerFraction[i];
                double nSupply = nitrogenFlowFromSource + mineralNSupply;
                double mineralPSupply = labileP[i] * source.LayerFraction[i];
                double pSupply = phosphorusFlowFromSource + mineralPSupply;

                double SupplyFactor = 1;

                if (totalNitrogenFlowToDestinations > nSupply)
                    SupplyFactor = MathUtilities.Bound(MathUtilities.Divide(mineralNSupply, totalNitrogenFlowToDestinations - nitrogenFlowFromSource, 1.0), 0.0, 1.0);
                if (totalPhosphorusFlowToDestinations > pSupply)
                {
                    double pSupplyFactor = MathUtilities.Bound(MathUtilities.Divide(mineralPSupply, totalPhosphorusFlowToDestinations - phosphorusFlowFromSource, 1.0), 0.0, 1.0);
                    // ALERT
                    pSupplyFactor = 1; // remove P constraint until P model fully operational
                    SupplyFactor = Math.Min(SupplyFactor, pSupplyFactor);
                }

                if (SupplyFactor < 1)
                {
                    for (int j = 0; j < numDestinations; j++)
                    {
                        carbonFlowToDestination[j] *= SupplyFactor;
                        nitrogenFlowToDestination[j] *= SupplyFactor;
                        phosphorusFlowToDestination[j] *= SupplyFactor;
                    }
                    totalNitrogenFlowToDestinations *= SupplyFactor;
                    totalPhosphorusFlowToDestinations *= SupplyFactor;

                    carbonFlowFromSource *= SupplyFactor;
                    nitrogenFlowFromSource *= SupplyFactor;
                    phosphorusFlowFromSource *= SupplyFactor;

                }

                // Remove from source
                source.Add(i, -carbonFlowFromSource, -nitrogenFlowFromSource, -phosphorusFlowFromSource);

                Catm[i] = carbonFlowFromSource - carbonFlowToDestination.Sum();

                // Add to destination
                for (int j = 0; j < numDestinations; j++)
                    destinations[j].Add(i, carbonFlowToDestination[j], nitrogenFlowToDestination[j], phosphorusFlowToDestination[j]);

                if (totalNitrogenFlowToDestinations <= nitrogenFlowFromSource)
                {
                    MineralisedN[i] = nitrogenFlowFromSource - totalNitrogenFlowToDestinations;
                    nh4[i] += MineralisedN[i];
                }
                else
                {
                    double NDeficit = totalNitrogenFlowToDestinations - nitrogenFlowFromSource;
                    double NH4Immobilisation = Math.Min(nh4[i], NDeficit);
                    nh4[i] -= NH4Immobilisation;
                    NDeficit -= NH4Immobilisation;

                    double NO3Immobilisation = Math.Min(no3[i], NDeficit);
                    no3[i] -= NO3Immobilisation;
                    NDeficit -= NO3Immobilisation;

                    MineralisedN[i] = -NH4Immobilisation - NO3Immobilisation;

                    if (MathUtilities.IsGreaterThan(NDeficit, 0.0))
                        throw new Exception("Insufficient mineral N for immobilisation demand for C flow " + Name);
                }

                if (totalPhosphorusFlowToDestinations <= phosphorusFlowFromSource)
                {
                    MineralisedP[i] = phosphorusFlowFromSource - totalPhosphorusFlowToDestinations;
                    labileP[i] += MineralisedP[i];
                }
                else
                {
                    double PDeficit = totalPhosphorusFlowToDestinations - phosphorusFlowFromSource;
                    double PImmobilisation = Math.Min(labileP[i], PDeficit);
                    labileP[i] -= PImmobilisation;
                    PDeficit -= PImmobilisation;
                    MineralisedP[i] = -PImmobilisation;

                    // ALERT
                    //if (MathUtilities.IsGreaterThan(PDeficit, 0.0))
                    //    throw new Exception("Insufficient mineral P for immobilisation demand for C flow " + Name);
                }
            }
        }
    }
}