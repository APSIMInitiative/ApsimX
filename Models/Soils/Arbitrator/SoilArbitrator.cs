namespace Models.Soils.Arbitrator
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Interfaces;
    using System.Linq;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// The APSIM farming systems model has a long history of use for simulating mixed or intercropped systems.  Doing this requires methods for simulating the competition of above and below ground resources.  Above ground competition for light has been calculated within APSIM assuming a mixed turbid medium using the Beer-Lambert analogue as described by [Keating1993Intercropping].  The MicroClimate [Snow2004Micromet] model now used within APSIM builds upon this by also calculating the impact of mutual shading on canopy conductance and partitions aerodynamic conductance to individual species in applying the Penman-Monteith model for calculating potential crop water use.  The arbitration of below ground resources of water and nitrogen is calculated by this model.
    /// 
    /// Traditionally, below ground competition has been arbitrated using two approaches.  Firstly, the early approaches [Adiku1995Intercrop; Carberry1996Ley] used an alternating order of uptake calculation each day to ensure that different crops within a simulation did not benefit from precedence in daily orders of calculations.  Soil water simulations using the SWIM3 model [Huth2012SWIM3] arbitrate individual crop uptakes as part of the simulataneous solutions of various soil water fluxes as part of its solution of the Richards' equation [richards1931capillary].
    /// 
    /// The soil arbitrator operates via a simple integration of daily fluxes into crop root systems via a [Runge-Kutta](https://en.wikipedia.org/wiki/Runge%E2%80%93Kutta_methods) calculation. 
    /// 
    /// If Y is any soil resource, such as water or N, and U is the uptake of that resource by one or more plant root systems,  
    /// then
    /// 
    /// Y~t+1~ = Y~t~ - U
    /// 
    /// Because U will change through the time period in complex manners depending on the number and nature of demands for that resource, we use Runge-Kutta to integrate through that time period using
    /// 
    /// Y~t+1~= Y~t~ + 1/6 x (U~1~+ 2xU~2~ + 2xU~3~ + U~4~) 
    /// 
    /// Where U~1~,U~2~,U~3~ and U~4~ are 4 estimates of the Uptake rates calculated by the crop models given a range of soil resource conditions, as follows:
    /// 
    /// U~1~ = f(Y~t~),
    /// 
    /// U~2~ = f(Y~t~ - 0.5xU~1~),
    /// 
    /// U~3~ = f(Y~t~ - 0.5xU~2~),
    /// 
    /// U~4~ = f(Y~t~ - U~3~).
    /// 
    /// So U~1~ is the estimate based on the uptake rates at the beginning of the time interval, similar to a simple Euler method.
    /// U~2~ and U~3~ are estimates based on the rates somewhere near the midpoint of the time interval.  U~4~ is the estimate based on the rates toward the end of the time interval.
    /// 
    /// The iterative procedure allows crops to influence the uptake of other crops via various feedback mechanisms.  For example,  crops rapidly extracting water from near the surface will dry the soil in those layers, which will force deeper rooted crops to potentially extract water from lower layers. Uptakes can notionally be of either sign, and so trees providing hydraulic lift of water from water tables could potentially make this water available for uptake by mutplie understory species within the timestep.  Crops are responsible for meeting resource demand by whatever means they prefer.  And so, leguminous crops may start by taking up mineral N at the start of the day but rely on fixation later in a time period if N becomes limiting.  This will reduce competition from others and change the balance dynamically throughout the integration period. 
    /// 
    /// The design has been chosen to provide the following benefits:
    /// 
    /// 1) The approach is numerically simple and pure.
    /// 
    /// 2) The approach does not require the use of any particular uptake equation. The uptake equation is embodied within the crop model as designed by the crop model developer and tester.
    /// 
    /// 3) The approach will allow any number of plant species to interact.
    /// 
    /// 4) The approach will allow for arbitration between species in any zone, but also competition between species that may demand resources from multiple zones within the simulation.
    /// 
    /// 5) The approach will automatically arbitrate supply of N between zones, layers, and types (nitrate vs ammonium) with the preferences of all derived by the plant model code.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    public class SoilArbitrator : Model
    {
        private IEnumerable<IUptake> uptakeModels = null;
        private IEnumerable<Zone> zones = null;
        private SoilState InitialSoilState;


        /// <summary>Called at the start of the simulation.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Dummy event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            uptakeModels = Parent.FindAllDescendants<IUptake>().ToList();
            zones = Parent.FindAllDescendants<Zone>().ToList();
            InitialSoilState = new SoilState(zones);
            if (!(this.Parent is Simulation))
                throw new Exception(this.Name + " must be placed directly under the simulation node as it won't work properly anywhere else");
        }

        /// <summary>Called by clock to do water arbitration</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Dummy event data.</param>
        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            DoArbitration(Estimate.CalcType.Water);
        }

        /// <summary>Called by clock to do nutrient arbitration</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Dummy event data.</param>
        [EventSubscribe("DoNutrientArbitration")]
        private void DoNutrientArbitration(object sender, EventArgs e)
        {
            DoArbitration(Estimate.CalcType.Nitrogen);
        }

        /// <summary>
        /// General soil arbitration method (water or nutrients) based upon Runge-Kutta method
        /// </summary>
        /// <param name="arbitrationType">Water or Nitrogen</param>
        private void DoArbitration(Estimate.CalcType arbitrationType)
        {
            InitialSoilState.Initialise();

            Estimate UptakeEstimate1 = new Estimate(this.Parent, arbitrationType, InitialSoilState, uptakeModels);
            Estimate UptakeEstimate2 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate1 * 0.5, uptakeModels);
            Estimate UptakeEstimate3 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate2 * 0.5, uptakeModels);
            Estimate UptakeEstimate4 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate3, uptakeModels);

            List<ZoneWaterAndN> listOfZoneUptakes = new List<ZoneWaterAndN>();
            List <CropUptakes> ActualUptakes = new List<CropUptakes>();
            foreach (CropUptakes U in UptakeEstimate1.Values)
            {
                CropUptakes CU = new CropUptakes();
                CU.Crop = U.Crop;
                foreach (ZoneWaterAndN ZU in U.Zones)
                {
                    ZoneWaterAndN NewZone = UptakeEstimate1.UptakeZone(CU.Crop, ZU.Zone.Name) * (1.0 / 6.0)
                                        + UptakeEstimate2.UptakeZone(CU.Crop, ZU.Zone.Name) * (1.0 / 3.0)
                                        + UptakeEstimate3.UptakeZone(CU.Crop, ZU.Zone.Name) * (1.0 / 3.0)
                                        + UptakeEstimate4.UptakeZone(CU.Crop, ZU.Zone.Name) * (1.0 / 6.0);
                    CU.Zones.Add(NewZone);
                    listOfZoneUptakes.Add(NewZone);
                }

                ActualUptakes.Add(CU);
            }

            ScaleWaterAndNIfNecessary(InitialSoilState.Zones, listOfZoneUptakes);

            foreach (CropUptakes Uptake in ActualUptakes)
            {
                if (arbitrationType == Estimate.CalcType.Water)
                    Uptake.Crop.SetActualWaterUptake(Uptake.Zones);
                else
                    Uptake.Crop.SetActualNitrogenUptakes(Uptake.Zones);
            }
        }

        /// <summary>
        /// Scale the water and n values if the total uptake exceeds the amounts available.
        /// </summary>
        /// <param name="zones">List of zones to check.</param>
        /// <param name="uptakes">List of all potential uptakes</param>
        private static void ScaleWaterAndNIfNecessary(List<ZoneWaterAndN> zones, List<ZoneWaterAndN> uptakes)
        {
            foreach (ZoneWaterAndN uniqueZone in zones)
            {
                double[] totalWaterUptake = new double[uniqueZone.Water.Length];
                double[] totalNO3Uptake = new double[uniqueZone.Water.Length];
                double[] totalNH4Uptake = new double[uniqueZone.Water.Length];
                foreach (ZoneWaterAndN zone in uptakes)
                {
                    if (zone.Zone == uniqueZone.Zone)
                    {
                        totalWaterUptake = MathUtilities.Add(totalWaterUptake, zone.Water);
                        totalNO3Uptake = MathUtilities.Add(totalNO3Uptake, zone.NO3N);
                        totalNH4Uptake = MathUtilities.Add(totalNH4Uptake, zone.NH4N);
                    }
                }

                for (int i = 0; i < uniqueZone.Water.Length; i++)
                {
                    if (uniqueZone.Water[i] < totalWaterUptake[i] && totalWaterUptake[i] > 0)
                    {
                        // Scale water
                        double scale = uniqueZone.Water[i] / totalWaterUptake[i];
                        foreach (ZoneWaterAndN zone in uptakes)
                        {
                            if (zone.Zone == uniqueZone.Zone)
                                zone.Water[i] *= scale;
                        }
                    }

                    if (uniqueZone.NO3N[i] < totalNO3Uptake[i] && totalNO3Uptake[i] > 0)
                    {
                        // Scale NO3
                        double scale = uniqueZone.NO3N[i] / totalNO3Uptake[i];
                        foreach (ZoneWaterAndN zone in uptakes)
                        {
                            if (zone.Zone == uniqueZone.Zone)
                                zone.NO3N[i] *= scale;
                        }
                    }

                    if (uniqueZone.NH4N[i] < totalNH4Uptake[i] && totalNH4Uptake[i] > 0)
                    {
                        // Scale NH4
                        double scale = uniqueZone.NH4N[i] / totalNH4Uptake[i];
                        foreach (ZoneWaterAndN zone in uptakes)
                        {
                            if (zone.Zone == uniqueZone.Zone)
                                zone.NH4N[i] *= scale;
                        }
                    }
                }
            }
        }
    }
}