using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// Stores information about a column in an observed table
    /// </summary>
    public class ColumnInfo
    {
        /// <summary></summary>
        public string Name;

        /// <summary></summary>
        public string IsApsimVariable;

        /// <summary></summary>
        public string DataType;

        /// <summary></summary>
        public bool HasErrorColumn;

        /// <summary></summary>
        public string File;

        /// <summary>
        /// Converts a list of ColumnInfo into a DataTable
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable CreateDataTable(IEnumerable data)
        {
            DataTable newTable = new DataTable();
            newTable.Columns.Add("Name");
            newTable.Columns.Add("APSIM");
            newTable.Columns.Add("Type");
            newTable.Columns.Add("Error Bars");
            newTable.Columns.Add("File");

            if (data == null)
                return newTable;

            foreach (ColumnInfo columnInfo in data)
            {

                DataRow row = newTable.NewRow();
                row["Name"] = columnInfo.Name;
                row["APSIM"] = columnInfo.IsApsimVariable;
                row["Type"] = columnInfo.DataType;
                row["Error Bars"] = columnInfo.HasErrorColumn;
                row["File"] = columnInfo.File;

                newTable.Rows.Add(row);
            }

            for (int i = 0; i < newTable.Columns.Count; i++)
                newTable.Columns[i].ReadOnly = true;

            DataView dv = newTable.DefaultView;
            dv.Sort = "APSIM desc, Name asc";

            return dv.ToTable();
        }

        /// <summary>From the list of columns read in, get a list of columns that match apsim variables.</summary>
        public static List<ColumnInfo> GetAPSIMColumnsFromObserved(DataTable dataTable, Simulations simulations, List<string> columnNames)
        {
            List<ColumnInfo> infos = new List<ColumnInfo>();

            List<string> allColumnNames = dataTable.GetColumnNames().ToList();

            for (int j = 0; j < dataTable.Columns.Count; j++)
            {
                string columnName = dataTable.Columns[j].ColumnName;
                string columnNameOriginal = columnName;
                //remove Error from name
                if (columnName.EndsWith("Error"))
                    columnName = columnName.Remove(columnName.IndexOf("Error"), 5);

                //check if it has maths
                bool hasMaths = false;
                if (columnName.IndexOfAny(new char[] { '+', '-', '*', '/', '=' }) > -1 || columnName.StartsWith("sum"))
                    hasMaths = true;

                //remove ( ) from name
                if (!hasMaths && columnName.IndexOf('(') > -1 && columnName.EndsWith(')'))
                {
                    int start = columnName.IndexOf('(');
                    int end = columnName.LastIndexOf(')');
                    columnName = columnName.Remove(start, end - start + 1);
                }

                if (!columnNames.Contains(columnName))
                {
                    columnNames.Add(columnName);

                    bool nameInAPSIMFormat = NameIsAPSIMFormat(columnName);
                    VariableComposite variable = null;
                    bool nameIsAPSIMModel = false;
                    if (nameInAPSIMFormat)
                    {
                        variable = NameMatchesAPSIMModel(columnName, simulations);
                        if (variable != null)
                        {
                            nameIsAPSIMModel = true;
                        }
                    }

                    //Get a filename for this property
                    string filename = "";
                    for (int k = 0; k < dataTable.Rows.Count && string.IsNullOrEmpty(filename); k++)
                    {
                        DataRow row = dataTable.Rows[k];
                        if (!string.IsNullOrEmpty(row[columnNameOriginal].ToString()))
                        {
                            filename = row["_Filename"].ToString();
                        }
                    }

                    ColumnInfo colInfo = new ColumnInfo();
                    colInfo.File = filename;
                    colInfo.Name = columnName;

                    colInfo.IsApsimVariable = "No";
                    colInfo.DataType = "";
                    if (nameInAPSIMFormat)
                        colInfo.IsApsimVariable = "Not Found";
                    if (hasMaths)
                        colInfo.IsApsimVariable = "Maths";
                    if (nameIsAPSIMModel && variable != null)
                    {
                        colInfo.IsApsimVariable = "Yes";
                        colInfo.DataType = variable.DataType.Name;
                    }

                    colInfo.HasErrorColumn = false;
                    if (allColumnNames.Contains(columnName + "Error"))
                        colInfo.HasErrorColumn = true;

                    infos.Add(colInfo);
                }
            }

            return infos;
        }

        /// <summary></summary>
        private static bool NameIsAPSIMFormat(string columnName)
        {
            if (columnName.Contains('.'))
                return true;
            else
                return false;
        }

        /// <summary></summary>
        private static VariableComposite NameMatchesAPSIMModel(string columnName, Simulations sims)
        {
            string nameWithoutBrackets = columnName;
            //remove any characters between ( and ) as these are often layers of a model
            while (nameWithoutBrackets.Contains('(') && nameWithoutBrackets.Contains(')'))
            {
                int start = nameWithoutBrackets.IndexOf('(');
                int end = nameWithoutBrackets.IndexOf(')');
                nameWithoutBrackets = nameWithoutBrackets.Substring(0, start) + nameWithoutBrackets.Substring(end + 1);
            }

            //if name ends in Error, remove Error before checking
            if (nameWithoutBrackets.EndsWith("Error"))
                nameWithoutBrackets = nameWithoutBrackets.Substring(0, nameWithoutBrackets.IndexOf("Error"));

            if (nameWithoutBrackets.Length == 0)
                return null;

            string[] nameParts = nameWithoutBrackets.Split('.');
            IModel firstPart = sims.Node.FindChild<IModel>(nameParts[0]);
            if (firstPart == null)
                return null;

            sims.Links.Resolve(firstPart, true, true, false);
            string fullPath = firstPart.FullPath;
            for (int i = 1; i < nameParts.Length; i++)
                fullPath += "." + nameParts[i];

            try
            {
                VariableComposite variable = sims.Node.GetObject(fullPath);
                return variable;
            }
            catch
            {
                return null;
            }
        }
    }
}
