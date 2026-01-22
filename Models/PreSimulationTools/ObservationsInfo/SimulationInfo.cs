using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Models.Core;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// Stores information about simulation names and if they match between the apsim file and observed datas
    /// </summary>
    public class SimulationInfo
    {
        /// <summary>Simulation name</summary>
        public string Name;

        /// <summary>Does this simulation exist in the APSIM file</summary>
        public bool HasSimulation;

        /// <summary>Does this simulation have data in the observed files</summary>
        public bool HasData;

        /// <summary>How many rows of data this simulation has</summary>
        public int Rows;

        /// <summary>
        /// Converts a list of SimulationInfo into a DataTable
        /// </summary>
        /// <param name="data">A list of SimulationInfo</param>
        /// <returns>A DataTable of the contents of the given list. Used by the GUI for displaying this class.</returns>
        public static DataTable CreateDataTable(IEnumerable data)
        {
            DataTable newTable = new DataTable();

            newTable.Columns.Add("Name");
            newTable.Columns.Add("HasSimulation");
            newTable.Columns.Add("HasData");
            newTable.Columns.Add("Rows");

            if (data == null)
                return newTable;

            foreach (SimulationInfo info in data)
            {
                DataRow row = newTable.NewRow();
                row["Name"] = info.Name;
                row["HasSimulation"] = info.HasSimulation;
                row["HasData"] = info.HasData;
                row["Rows"] = info.Rows;
                newTable.Rows.Add(row);
            }

            for (int i = 0; i < newTable.Columns.Count; i++)
                newTable.Columns[i].ReadOnly = true;

            DataView dv = newTable.DefaultView;
            dv.Sort = "Name asc";

            return dv.ToTable();
        }

        /// <summary>
        /// Compare the simulation names from the apsim file against the simulation names in the given DataTable and returns SimulationInfo about if they match or not.
        /// </summary>
        /// <param name="dataTable">Observed data in a DataTable</param>
        /// <param name="simulations">Reference to the Simulations node</param>
        /// <returns>A list of SimulationInfo that says if the name match and if any are missing from the simulation or data</returns>
        public static List<SimulationInfo> GetSimulationsFromObserved(DataTable dataTable, Simulations simulations)
        {
            List<SimulationInfo> data = new List<SimulationInfo>();

            List<string> apsimSims = simulations.GetAllSimulationAndFactorialNameList();
            List<string> observedSims = dataTable.AsEnumerable().Select(s => s["SimulationName"].ToString()).Distinct().ToList<string>();

            List<string> combinedSims = new List<string>();
            combinedSims.AddRange(apsimSims);
            combinedSims.AddRange(observedSims);
            combinedSims = combinedSims.Distinct().ToList<string>();
            combinedSims.Sort();

            foreach (string name in combinedSims)
            {
                bool hasData = false;
                bool hasSim = false;
                int rows = 0;

                if (observedSims.Contains(name))
                {
                    hasData = true;
                    rows = dataTable.Select().Where(s => s["SimulationName"].ToString() == name).Count();
                }

                if (apsimSims.Contains(name))
                    hasSim = true;

                SimulationInfo info = new SimulationInfo();
                info.Name = name;
                info.HasData = hasData;
                info.HasSimulation = hasSim;
                info.Rows = rows;
                data.Add(info);
            }

            return data;
        }
    }
}
