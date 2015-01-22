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
    public class ZoneWaterAndN
    {
        /// <summary>The zone</summary>
        public string Name;
        /// <summary>The amount</summary>
        public double[] Water;
        public double[] NO3N;
        public double[] NH4N;
        public double TotalWater
        {
            get{
                return Utility.Math.Sum(Water);
            }
        }

    }
    public class SoilState
    {
        public List<ZoneWaterAndN> Zones = new List<ZoneWaterAndN>();
        IModel Parent;
        public SoilState(IModel parent)
        {
            Parent = parent;
        }
        public void Initialise()
        {
            foreach (Zone Z in Apsim.Children(this.Parent, typeof(Zone)))
            {
                ZoneWaterAndN NewZ = new ZoneWaterAndN();
                NewZ.Name = Z.Name;
                Soil soil = Apsim.Child(Z, typeof(Soil)) as Soil;
                NewZ.Water = soil.Water;
                NewZ.NO3N = soil.NO3N;
                NewZ.NH4N = soil.NH4N;
                Zones.Add(NewZ);
            }
        }

        public ZoneWaterAndN ZoneState(string ZoneName)
        {
            foreach (ZoneWaterAndN Z in Zones)
                if (Z.Name == ZoneName)
                    return Z;
            throw new Exception("Could not find zone called " + ZoneName);
        }
        public static SoilState operator -(SoilState State, SoilArbitrator.Estimate E)
        {
            SoilState NewState = new SoilState(State.Parent);
            foreach (ZoneWaterAndN Z in State.Zones)
            {
                ZoneWaterAndN NewZ = new ZoneWaterAndN();
                NewZ.Name = Z.Name;
                NewZ.Water = Z.Water;
                NewZ.NO3N = Z.NO3N;
                NewZ.NH4N = Z.NH4N;
                NewState.Zones.Add(NewZ);
            }

            foreach (SoilArbitrator.CropUptakes C in E.Value)
                foreach (ZoneWaterAndN Z in C.Zones)
                    foreach (ZoneWaterAndN NewZ in NewState.Zones)
                        if (Z.Name == NewZ.Name)
                        {
                            NewZ.Water = Utility.Math.Subtract(NewZ.Water, Z.Water);
                            NewZ.NO3N = Utility.Math.Subtract(NewZ.NO3N, Z.NO3N);
                            NewZ.NH4N = Utility.Math.Subtract(NewZ.NH4N, Z.NH4N);
                        }
            return NewState;
        }

    }
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
            public List<ZoneWaterAndN> Zones;

            public CropUptakes()
            {
                Zones = new List<ZoneWaterAndN>();
            }
        }
        public class Estimate
        {
            IModel Parent;
            List<CropUptakes> EstimateValues = new List<CropUptakes>();
            public Estimate(IModel parent)
            {
                Parent = parent;
            }
            public List<CropUptakes> Value
            {
                get { return EstimateValues; }
            }
            public enum CalcType { Water, Nitrogen };
            public void Calculate(CalcType Type, SoilState soilstate)
            {
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
                        ZoneWaterAndN NewZ = new ZoneWaterAndN();
                        NewZ.Water = Utility.Math.Multiply_Value(Z.Water, value);
                        NewZ.NO3N = Utility.Math.Multiply_Value(Z.NO3N, value);
                        NewZ.NH4N = Utility.Math.Multiply_Value(Z.NH4N, value);
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

            
            SoilState InitialSoilState = new SoilState(this.Parent);
            InitialSoilState.Initialise();

            Estimate Estimate1 = new Estimate(this.Parent);
            Estimate1.Calculate(Estimate.CalcType.Water, InitialSoilState);

            Estimate Estimate2 = new Estimate(this.Parent);
            SoilState SoilState2 = InitialSoilState - Estimate1 * 0.5;
            Estimate2.Calculate(Estimate.CalcType.Water, SoilState2);

            Estimate Estimate3 = new Estimate(this.Parent);
            SoilState SoilState3 = InitialSoilState - Estimate2 * 0.5;
            Estimate3.Calculate(Estimate.CalcType.Water, SoilState3);

            Estimate Estimate4 = new Estimate(this.Parent);
            SoilState SoilState4 = InitialSoilState - Estimate3;
            Estimate4.Calculate(Estimate.CalcType.Water, SoilState4);


            List<CropUptakes> UptakesFinal = new List<CropUptakes>();
            foreach (CropUptakes U in Estimate1.Value)
            {
                CropUptakes CWU = new CropUptakes();
                CWU.Crop=U.Crop;
                foreach (ZoneWaterAndN ZW1 in U.Zones)
                {
                    ZoneWaterAndN NewZ = new ZoneWaterAndN();
                    NewZ.Name = ZW1.Name;                 
                    NewZ.Water = Utility.Math.Add(Utility.Math.Multiply_Value(Estimate1.UptakeZone(CWU.Crop,NewZ.Name).Water, 1.0/6.0),
                                               Utility.Math.Multiply_Value(Estimate2.UptakeZone(CWU.Crop,NewZ.Name).Water, 1.0/3.0));
                    NewZ.Water = Utility.Math.Add(NewZ.Water,Utility.Math.Multiply_Value(Estimate3.UptakeZone(CWU.Crop,NewZ.Name).Water, 1.0/3.0));
                    NewZ.Water = Utility.Math.Add(NewZ.Water, Utility.Math.Multiply_Value(Estimate4.UptakeZone(CWU.Crop, NewZ.Name).Water, 1.0/6.0));
                    
                    CWU.Zones.Add(NewZ);
                }
                
                UptakesFinal.Add(CWU);

            }

            foreach (CropUptakes Uptake in UptakesFinal)
            {
                Uptake.Crop.SetSWUptake(Uptake.Zones);
            }
        }

        /// <summary>Called when [do soil arbitration].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Calculating Euler integration. Number of UptakeSums different to expected value of iterations.</exception>
        [EventSubscribe("DoNutrientArbitration")]
        private void DoNutrientArbitration(object sender, EventArgs e)
        {

            SoilState InitialSoilState = new SoilState(this.Parent);
            InitialSoilState.Initialise();

            Estimate Estimate1 = new Estimate(this.Parent);
            Estimate1.Calculate(Estimate.CalcType.Nitrogen, InitialSoilState);

            Estimate Estimate2 = new Estimate(this.Parent);
            SoilState SoilState2 = InitialSoilState - Estimate1 * 0.5;
            Estimate2.Calculate(Estimate.CalcType.Nitrogen, SoilState2);

            Estimate Estimate3 = new Estimate(this.Parent);
            SoilState SoilState3 = InitialSoilState - Estimate2 * 0.5;
            Estimate3.Calculate(Estimate.CalcType.Nitrogen, SoilState3);

            Estimate Estimate4 = new Estimate(this.Parent);
            SoilState SoilState4 = InitialSoilState - Estimate3;
            Estimate4.Calculate(Estimate.CalcType.Nitrogen, SoilState4);

            List<CropUptakes> UptakesFinal = new List<CropUptakes>();
            foreach (CropUptakes U in Estimate1.Value)
            {
                CropUptakes CWU = new CropUptakes();
                CWU.Crop = U.Crop;
                foreach (ZoneWaterAndN ZW1 in U.Zones)
                {
                    ZoneWaterAndN NewZ = new ZoneWaterAndN();
                    NewZ.Name = ZW1.Name;
                    NewZ.NO3N = Utility.Math.Add(Utility.Math.Multiply_Value(Estimate1.UptakeZone(CWU.Crop, NewZ.Name).NO3N, 0.166666),
                                               Utility.Math.Multiply_Value(Estimate2.UptakeZone(CWU.Crop, NewZ.Name).NO3N, 0.333333));
                    NewZ.NO3N = Utility.Math.Add(NewZ.NO3N, Utility.Math.Multiply_Value(Estimate3.UptakeZone(CWU.Crop,NewZ.Name).NO3N, 0.333333));
                    NewZ.NO3N = Utility.Math.Add(NewZ.NO3N, Utility.Math.Multiply_Value(Estimate4.UptakeZone(CWU.Crop, NewZ.Name).NO3N, 0.16666));

                    NewZ.NH4N = Utility.Math.Add(Utility.Math.Multiply_Value(Estimate1.UptakeZone(CWU.Crop, NewZ.Name).NH4N, 0.166666),
                           Utility.Math.Multiply_Value(Estimate2.UptakeZone(CWU.Crop, NewZ.Name).NH4N, 0.333333));
                    NewZ.NH4N = Utility.Math.Add(NewZ.NH4N, Utility.Math.Multiply_Value(Estimate3.UptakeZone(CWU.Crop, NewZ.Name).NH4N, 0.333333));
                    NewZ.NH4N = Utility.Math.Add(NewZ.NH4N, Utility.Math.Multiply_Value(Estimate4.UptakeZone(CWU.Crop, NewZ.Name).NH4N, 0.16666));

                    CWU.Zones.Add(NewZ);
                }

                UptakesFinal.Add(CWU);

            }

            foreach (CropUptakes Uptake in UptakesFinal)
            {
                Uptake.Crop.SetNUptake(Uptake.Zones);
            }
        }
    }
}