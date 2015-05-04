using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.Interfaces;
using Models.Soils.Arbitrator;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// A simple agroforestry model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ForestryView")]
    [PresenterName("UserInterface.Presenters.ForestryPresenter")]
    public class Forestry : Model,IUptake
    {
        /// <summary>Gets or sets the table data.</summary>
        /// <value>The table.</value>
        [Summary]
        public List<List<string>> Table { get; set; }

        /// <summary>
        /// A list containing forestry information for each zone.
        /// </summary>
        [XmlIgnore]
        public List<ZoneInfo> ZoneInfoList;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ZoneInfoList = new List<ZoneInfo>();
            for (int i = 2; i < Table.Count; i++)
            {
                ZoneInfo newZone = new ZoneInfo();
                newZone.Name = Table[0][i - 1];
                newZone.Wind = Convert.ToDouble(Table[i][0]);
                newZone.Shade = Convert.ToDouble(Table[i][1]);
                newZone.RLD = new double[Table[1].Count - 3];
                for (int j = 3; j < Table[1].Count; j++)
                    newZone.RLD[j - 3] = Convert.ToDouble(Table[i][j]);
                ZoneInfoList.Add(newZone);
            }
        }
        /// <summary>
        /// Returns soil water uptake from each zone by the static tree model
        /// </summary>
        /// <param name="soilstate"></param>
        /// <returns></returns>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetSWUptakes(Soils.Arbitrator.SoilState soilstate)
        {
            List<ZoneWaterAndN> Uptakes = new List<ZoneWaterAndN>();
            
            foreach (ZoneWaterAndN Z in soilstate.Zones)
            {
                foreach (ZoneInfo ZI in ZoneInfoList)
                {
                    if (Z.Name == ZI.Name)
                    {
                        ZoneWaterAndN Uptake = new ZoneWaterAndN();
                        //Find the soil for this zone
                        Zone ThisZone = new Zone();
                        Soils.Soil ThisSoil = new Soils.Soil();

                        foreach (Zone SearchZ in Apsim.ChildrenRecursively(Parent, typeof(Zone)))
                            if (SearchZ.Name == Z.Name)
                                ThisSoil = Apsim.Find(SearchZ, typeof(Soils.Soil)) as Soils.Soil;

                        Uptake.Name = Z.Name;
                        double[] SW = Z.Water;
                        Uptake.NO3N = new double[SW.Length];
                        Uptake.NH4N = new double[SW.Length];
                        Uptake.Water = new double[SW.Length];
                        for (int i = 0; i <= SW.Length - 1; i++)
                        {
                            double[] LL15mm = MathUtilities.Multiply(ThisSoil.LL15,ThisSoil.Thickness);
                            Uptake.Water[i] = (SW[i] - LL15mm[i]) * ZI.RLD[i];
                        }
                        Uptakes.Add(Uptake);
                    }
                }
            }
            return Uptakes;


        }
        /// <summary>
        /// Returns soil Nitrogen uptake from each zone by the static tree model
        /// </summary>
        /// <param name="soilstate"></param>
        /// <returns></returns>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetNUptakes(Soils.Arbitrator.SoilState soilstate)
        {
            List<ZoneWaterAndN> Uptakes = new List<ZoneWaterAndN>();

            foreach (ZoneWaterAndN Z in soilstate.Zones)
            {
                foreach (ZoneInfo ZI in ZoneInfoList)
                {
                    if (Z.Name == ZI.Name)
                    {
                        ZoneWaterAndN Uptake = new ZoneWaterAndN();
                        //Find the soil for this zone
                        Zone ThisZone = new Zone();
                        Soils.Soil ThisSoil = new Soils.Soil();

                        foreach (Zone SearchZ in Apsim.ChildrenRecursively(Parent, typeof(Zone)))
                            if (SearchZ.Name == Z.Name)
                                ThisSoil = Apsim.Find(SearchZ, typeof(Soils.Soil)) as Soils.Soil;

                        Uptake.Name = Z.Name;
                        double[] SW = Z.Water;
                        Uptake.NO3N = new double[SW.Length];
                        Uptake.NH4N = new double[SW.Length];
                        Uptake.Water = new double[SW.Length];
                        //for (int i = 0; i <= SW.Length-1; i++)
                        //    Uptake.NO3N[i] = Z.NO3N[i] * ZI.RLD[i];

                        Uptakes.Add(Uptake);
                    }
                }
            }
            return Uptakes;
        }

        /// <summary>
        ///  Accepts the actual soil water uptake from the soil arbitrator.
        /// </summary>
        /// <param name="info"></param>
        public void SetSWUptake(List<Soils.Arbitrator.ZoneWaterAndN> info)
        {
            foreach (ZoneWaterAndN ZI in info)
            {
                foreach (Zone SearchZ in Apsim.ChildrenRecursively(Parent, typeof(Zone)))
                {
                    Soils.Soil ThisSoil = new Soils.Soil();
                    if (SearchZ.Name == ZI.Name)
                    {
                        ThisSoil = Apsim.Find(SearchZ, typeof(Soils.Soil)) as Soils.Soil;
                        ThisSoil.SoilWater.dlt_sw_dep = MathUtilities.Multiply_Value(ZI.Water, -1); ;
                    }
                }
            }
        }

        /// <summary>
        /// Accepts the actual soil Nitrogen uptake from the soil arbitrator.
        /// </summary>
        /// <param name="info"></param>
        public void SetNUptake(List<Soils.Arbitrator.ZoneWaterAndN> info)
        {
            // Do nothing with uptakes
            // throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A structure holding forestry information for a single zone.
    /// </summary>
    public struct ZoneInfo
    {
        /// <summary>
        /// The name of the zone.
        /// </summary>
        public string Name;

        /// <summary>
        /// Wind value.
        /// </summary>
        public double Wind;

        /// <summary>
        /// Shade value.
        /// </summary>
        public double Shade;

        /// <summary>
        /// Root Length Density information for each soil layer in a zone.
        /// </summary>
        public double[] RLD;
    }
}
