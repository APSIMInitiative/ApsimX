using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Globalization;
using MathNet.Numerics.LinearAlgebra.Double;
using Models.Core;

namespace Models.Soils
{

    /// <summary>
    /// 
    /// </summary>
    public class UptakeInfo
    {
        /// <summary>The zone</summary>
        public string ZoneName;
        /// <summary>The amount</summary>
        public double[] Amount;

    }
    public class CropUptakeInfo
    {
        /// <summary>Crop</summary>
        public ICrop Crop;
        /// <summary>List of uptakes</summary>
        public List<UptakeInfo> Uptakes;
    }

    /// <summary>
    /// A soil arbitrator model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        /// <summary>The simulation</summary>
        [Link]
        Simulation Simulation = null;
        /// <summary>The summary file</summary>
        [Link]
        Summary SummaryFile = null;

        /// <summary>
        /// The following event handler will be called once at the beginning of the simulation
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            //collect all zones in simulation
            foreach (Model m in Apsim.FindAll(Simulation, typeof(Zone)))
            {
            }
            foreach (ICrop plant in Apsim.FindAll(Simulation, typeof(ICrop)))
            {
            }
        }

        /// <summary>Called when [do soil arbitration].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Calculating Euler integration. Number of UptakeSums different to expected value of iterations.</exception>
        [EventSubscribe("DoWaterArbitration")]
        private void OnDoSoilArbitration(object sender, EventArgs e)
        {
            List<UptakeInfo> SWSupplies = new List<UptakeInfo>();
            foreach (Zone zone in Apsim.Children(Simulation, typeof(Zone)))
            {
                UptakeInfo info = new UptakeInfo();
                info.ZoneName = zone.Name;
                Soil soil = Apsim.Child(zone, typeof(Soil)) as Soil;
                info.Amount = soil.Water;
                SWSupplies.Add(info);
            }

            List<CropUptakeInfo> Uptakes = new List<CropUptakeInfo>();
            foreach (ICrop crop in Apsim.ChildrenRecursively(Simulation, typeof(ICrop)))
            {
                if (crop.IsAlive)
                {
                    CropUptakeInfo CropUptakes = new CropUptakeInfo();
                    CropUptakes.Crop = crop;
                    CropUptakes.Uptakes = crop.GetSWUptake(SWSupplies);
                    Uptakes.Add(CropUptakes);
                }
            }

            foreach (CropUptakeInfo Info in Uptakes)
            {
                Info.Crop.SetSWUptake(Info.Uptakes);
                //CropUptakeInfo CropUptake = Uptakes.Find(u => u.Crop == crop);
            }


        }

    }
}