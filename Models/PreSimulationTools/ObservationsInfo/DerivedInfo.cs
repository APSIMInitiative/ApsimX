using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// Stores information about derived values from the input
    /// </summary>
    public class DerivedInfo
    {
        /// <summary></summary>
        public string Name;

        /// <summary></summary>
        public string Function;

        /// <summary></summary>
        public string DataType;

        /// <summary></summary>
        public int Added;

        /// <summary></summary>
        public int Existing;

        /// <summary>
        /// Converts a list of DerivedInfo into a DataTable
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable CreateDataTable(IEnumerable data)
        {
            DataTable newTable = new DataTable();
            newTable.Columns.Add("Name");
            newTable.Columns.Add("Function");
            newTable.Columns.Add("DataType");
            newTable.Columns.Add("Added");
            newTable.Columns.Add("Existing");

            if (data == null)
                return newTable;

            foreach (DerivedInfo info in data)
            {
                DataRow row = newTable.NewRow();
                row["Name"] = info.Name;
                row["Function"] = info.Function;
                row["DataType"] = info.DataType;
                row["Added"] = info.Added;
                row["Existing"] = info.Existing;
                newTable.Rows.Add(row);
            }

            for (int i = 0; i < newTable.Columns.Count; i++)
                newTable.Columns[i].ReadOnly = true;

            DataView dv = newTable.DefaultView;
            dv.Sort = "Name asc";

            return dv.ToTable();
        }
        
         /// <summary></summary>
        public static List<DerivedInfo> AddDerivedColumns(DataTable dataTable)
        {
            List<DerivedInfo> infos = new List<DerivedInfo>();

            bool noMoreFound = false;
            while (!noMoreFound)
            {
                int count = infos.Count;

                //Our current list of derived variables
                infos.AddRange(DeriveColumn(dataTable, ".NConc", ".N", "/", ".Wt"));
                infos.AddRange(DeriveColumn(dataTable, ".N", ".NConc", "*", ".Wt"));
                infos.AddRange(DeriveColumn(dataTable, ".Wt", ".N", "/", ".NConc"));

                infos.AddRange(DeriveColumn(dataTable, ".", ".Live.", "+", ".Dead."));
                infos.AddRange(DeriveColumn(dataTable, ".Live.", ".", "-", ".Dead."));
                infos.AddRange(DeriveColumn(dataTable, ".Dead.", ".", "-", ".Live."));

                infos.AddRange(DeriveColumn(dataTable, "Leaf.SpecificAreaCanopy", "Leaf.LAI", "/", "Leaf.Live.Wt"));
                infos.AddRange(DeriveColumn(dataTable, "Leaf.LAI", "Leaf.SpecificAreaCanopy", "*", "Leaf.Live.Wt"));
                infos.AddRange(DeriveColumn(dataTable, "Leaf.Live.Wt", "Leaf.LAI", "/", "Leaf.SpecificAreaCanopy"));

                if (infos.Count - count == 0)
                    noMoreFound = true;
            }

            return infos;
        }
        
        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="derived"></param>
        /// <param name="variable1"></param>
        /// <param name="operation"></param>
        /// <param name="variable2"></param>
        /// <returns>True if a value was derived, false if not</returns>
        private static List<DerivedInfo> DeriveColumn(DataTable data, string derived, string variable1, string operation, string variable2)
        {
            return DeriveColumn(data, derived, operation, new List<string>() { variable1, variable2 });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="derived"></param>
        /// <param name="operation"></param>
        /// <param name="variables"></param>
        /// <returns>True if a value was derived, false if not</returns>
        private static List<DerivedInfo> DeriveColumn(DataTable data, string derived, string operation, List<string> variables)
        {
            List<DerivedInfo> infos = new List<DerivedInfo>();

            if (variables.Count == 0)
                return infos;

            List<string> allColumnNames = data.GetColumnNames().ToList();

            for (int j = 0; j < data.Columns.Count; j++)
            {
                string columnName = data.Columns[j].ColumnName;
                string variable1 = variables[0];

                //exclude error columns
                if (!columnName.EndsWith("Error") && columnName.LastIndexOf(variable1) > -1)
                {
                    //work out the prefix and suffix of the variables to be used
                    string prefix = columnName.Substring(0, columnName.LastIndexOf(variable1));
                    string postfix = columnName.Substring(columnName.LastIndexOf(variable1) + variable1.Length);

                    //check all the variables exist
                    bool foundAllVariables = true;
                    for (int k = 1; k < variables.Count && foundAllVariables; k++)
                        if (!allColumnNames.Contains(prefix + variables[k] + postfix))
                            foundAllVariables = false;

                    if (foundAllVariables)
                    {
                        string nameDerived = prefix + derived + postfix;
                        //create the column if it doesn't exist
                        if (!data.Columns.Contains(nameDerived))
                            data.Columns.Add(nameDerived);

                        //for each row in the datastore, see if we can compute the derived value
                        int added = 0;
                        int existing = 0;
                        for (int k = 0; k < data.Rows.Count; k++)
                        {
                            DataRow row = data.Rows[k];

                            //if it already exists, we do nothing
                            if (!string.IsNullOrEmpty(row[nameDerived].ToString()))
                            {
                                existing += 1;
                            }
                            else
                            {
                                double value = 0;

                                //Check that all our variables have values on this row
                                bool allVariablesHaveValues = true;
                                for (int m = 0; m < variables.Count && allVariablesHaveValues; m++)
                                {
                                    string nameVariable = prefix + variables[m] + postfix;
                                    if (string.IsNullOrEmpty(row[nameVariable].ToString()))
                                        allVariablesHaveValues = false;
                                    else if (m == 0)
                                        value = Convert.ToDouble(row[nameVariable]);
                                }

                                if (allVariablesHaveValues)
                                {
                                    string nameVariable = prefix + variables[0] + postfix;
                                    double? result = Convert.ToDouble(row[nameVariable]);

                                    //start at 1 here since our running value has the first value in it
                                    for (int m = 1; m < variables.Count; m++)
                                    {
                                        if (result != null && !double.IsNaN((double)result))
                                        {
                                            nameVariable = prefix + variables[m] + postfix;
                                            double valueVar = Convert.ToDouble(row[nameVariable]);

                                            if (operation == "+" || operation == "sum")
                                                result = value + valueVar;
                                            else if (operation == "-")
                                                result = value - valueVar;
                                            else if (operation == "*" || operation == "product")
                                                result = value * valueVar;
                                            else if (operation == "/" && valueVar != 0)
                                                result = value / valueVar;
                                            else
                                                result = null;
                                        }
                                    }
                                    if (result != null && !double.IsNaN((double)result))
                                    {
                                        row[nameDerived] = result;
                                        added += 1;
                                    }
                                }
                            }
                        }
                        //if we added some derived variables, list the stats for the user
                        if (added > 0)
                        {
                            string functionString = "";
                            for (int k = 0; k < variables.Count; k++)
                            {
                                if (k != 0)
                                    functionString += " " + operation + " ";
                                functionString += prefix + variables[k] + postfix;
                            }

                            DerivedInfo info = new DerivedInfo();
                            info.Name = nameDerived;
                            info.Function = functionString;
                            info.DataType = "Double";
                            info.Added = added;
                            info.Existing = existing;
                            infos.Add(info);
                        }
                    }
                }
            }

            return infos;
        }
    }
}
