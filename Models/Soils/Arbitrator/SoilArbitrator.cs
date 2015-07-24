// -----------------------------------------------------------------------
// <copyright file="SoilArbitrator.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Soils.Arbitrator
{
    using System;
    using System.Collections.Generic;
    using Models.Core;

    /// <summary>
    /// The APSIM farming systems model has a long history of use for simulating mixed or intercropped systems.  Doing this requires methods for simulating the competition of above and below ground resources.  Above ground competition for light has been calculated within APSIM assuming a mixed turbid medium using the Beer-Lambert analogue as described by [Keating1993Intercropping].  The MicroClimate [Snow2004Micromet] model now used within APSIM builds upon this by also calculating the impact of mutual shading on canopy conductance and partitions aerodynamic conductance to individual species in applying the Penman-Monteith model for calculating potential crop water use.  The arbitration of below ground resources of water and nitrogen is calculated by this model.
    /// 
    /// Traditionally, below ground competition has been arbitrated using two approaches.  Firstly, the early approaches [Adiku1995Intercrop; Carberry1996Ley] used an alternating order of uptake calculation each day to ensure that different crops within a simulation did not benefit from precedence in daily orders of calculations.  Soil water simulations using the SWIM3 model [Huth2012SWIM3] arbitrate individual crop uptakes as part of the simulataneous solutions of various soil water fluxes as part of its solution of the Richards' equation [richards1931capillary].
    /// 
    /// The soil arbitrator operates via a simple integration of daily fluxes into crop root systems via a <a href="https://en.wikipedia.org/wiki/Runge%E2%80%93Kutta_methods">Runge-Kutta</a> calculation. 
    /// 
    /// If Y is any soil resource, such as water or N, and U is the uptake of that resource by one or more plant root systems,  
    /// then
    /// 
    /// Y<sub>t+1</sub> = Y<sub>t</sub> - U
    /// 
    /// Because U will change through the time period in complex manners depending on the number and nature of demands for that resource, we use Runge-Kutta to integrate through that time period using
    /// 
    /// Y<sub>t+1</sub>= Y<sub>t</sub> + 1/6 x (U<sub>1</sub>+ 2xU<sub>2</sub> + 2xU<sub>3</sub> + U<sub>4</sub>) 
    /// 
    /// Where U<sub>1</sub>,U<sub>2</sub>,U<sub>3</sub> and U<sub>4</sub> are 4 estimates of the Uptake rates calculated by the crop models given a range of soil resource conditions, as follows:
    /// 
    /// U<sub>1</sub> = f(Y<sub>t</sub>),
    /// 
    /// U<sub>2</sub> = f(Y<sub>t</sub> - 0.5xU<sub>1</sub>),
    /// 
    /// U<sub>3</sub> = f(Y<sub>t</sub> - 0.5xU<sub>2</sub>),
    /// 
    /// U<sub>4</sub> = f(Y<sub>t</sub> - U<sub>3</sub>).
    /// 
    /// So U<sub>1</sub> is the estimate based on the uptake rates at the beginning of the time interval, similar to a simple Euler method.
    /// U<sub>2</sub> and U<sub>3</sub> are estimates based on the rates somewhere near the midpoint of the time interval.  U<sub>4</sub> is the estimate based on the rates toward the end of the time interval.
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
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
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
            SoilState InitialSoilState = new SoilState(this.Parent);
            InitialSoilState.Initialise();

            Estimate UptakeEstimate1 = new Estimate(this.Parent, arbitrationType, InitialSoilState);
            Estimate UptakeEstimate2 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate1 * 0.5);
            Estimate UptakeEstimate3 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate2 * 0.5);
            Estimate UptakeEstimate4 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate3);

            List<CropUptakes> UptakesFinal = new List<CropUptakes>();
            foreach (CropUptakes U in UptakeEstimate1.Values)
            {
                CropUptakes CWU = new CropUptakes();
                CWU.Crop = U.Crop;
                foreach (ZoneWaterAndN ZW1 in U.Zones)
                {
                    ZoneWaterAndN NewZ = UptakeEstimate1.UptakeZone(CWU.Crop, ZW1.Name) * (1.0 / 6.0)
                                       + UptakeEstimate2.UptakeZone(CWU.Crop, ZW1.Name) * (1.0 / 3.0)
                                       + UptakeEstimate3.UptakeZone(CWU.Crop, ZW1.Name) * (1.0 / 3.0)
                                       + UptakeEstimate4.UptakeZone(CWU.Crop, ZW1.Name) * (1.0 / 6.0);
                    CWU.Zones.Add(NewZ);
                }

                UptakesFinal.Add(CWU);
            }

            foreach (CropUptakes Uptake in UptakesFinal)
            {
                if (arbitrationType == Estimate.CalcType.Water)
                    Uptake.Crop.SetSWUptake(Uptake.Zones);
                else
                    Uptake.Crop.SetNUptake(Uptake.Zones);
            }
        }
    }
}