using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Globalization;
using MathNet.Numerics.LinearAlgebra.Double;
using Models.Core;
using Models.Soils.Arbitrator;

namespace Models.Soils
{

    /// <summary>
    /// A soil arbitrator model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilArbitrator : Model
    {
        public class CropUptakes
        {
            /// <summary>Crop</summary>
            public ICrop Crop;
            /// <summary>List of uptakes</summary>
            public List<ZoneWaterAndN> Zones = new List<ZoneWaterAndN>();
        }
        public class Estimate
        {
            IModel Parent;
            List<CropUptakes> EstimateValues = new List<CropUptakes>();
            public enum CalcType { Water, Nitrogen };

            public Estimate(IModel parent)
            {
                Parent = parent;
            }

            public Estimate(IModel parent, CalcType Type, SoilState soilstate)
            {
                Parent = parent;
                foreach (ICrop crop in Apsim.ChildrenRecursively(Parent, typeof(ICrop)))
                {
                    if (crop.IsAlive)
                    {
                        CropUptakes Uptake = new CropUptakes();
                        Uptake.Crop = crop;
                        if (Type == CalcType.Water)
                            Uptake.Zones = crop.GetSWUptakes(soilstate);
                        else
                            Uptake.Zones = crop.GetNUptakes(soilstate);
                        EstimateValues.Add(Uptake);
                    }
                }

            }
            public List<CropUptakes> Value
            {
                get { return EstimateValues; }
            }
            public ZoneWaterAndN UptakeZone(ICrop crop, string ZoneName)
            {
                foreach (CropUptakes U in EstimateValues)
                    if (U.Crop == crop)
                        foreach (ZoneWaterAndN Z in U.Zones)
                            if (Z.Name == ZoneName)
                                return Z;
                
                throw (new Exception("Cannot find uptake for" + crop.CropType + " " + ZoneName));
            }
            public static Estimate operator *(Estimate E, double value)
            {
                Estimate NewE = new Estimate(E.Parent);
                foreach (CropUptakes U in E.Value)
                {
                    CropUptakes NewU = new CropUptakes();
                    NewE.Value.Add(NewU);
                    foreach (ZoneWaterAndN Z in U.Zones)
                    {
                        ZoneWaterAndN NewZ = Z * value;
                        NewU.Zones.Add(NewZ);
                    }
                }
                
                    return NewE;
            }

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
            SoilState InitialSoilState = new SoilState(this.Parent);
            InitialSoilState.Initialise();

            Estimate UptakeEstimate1 = new Estimate(this.Parent, arbitrationType, InitialSoilState);
            Estimate UptakeEstimate2 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate1 * 0.5);
            Estimate UptakeEstimate3 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate2 * 0.5);
            Estimate UptakeEstimate4 = new Estimate(this.Parent, arbitrationType, InitialSoilState - UptakeEstimate3);

            List<CropUptakes> UptakesFinal = new List<CropUptakes>();
            foreach (CropUptakes U in UptakeEstimate1.Value)
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