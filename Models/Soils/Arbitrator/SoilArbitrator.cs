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
    /// A soil arbitrator model
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