using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// Stores information about a column in an observed table
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>Name of the column</summary>
        public string Name;

        /// <summary>Does the column name match an APSIM variable</summary>
        public string IsApsimVariable;

        /// <summary>The type of the APSIM variable if it matches, null if it doesn't match</summary>
        public Type VariableType;

        /// <summary>The type of the data from the observed data</summary>
        public Type DataType;

        /// <summary>Does the type of the variable match the type of the observed data. Null if the variable doesn't exist</summary>
        public bool DataTypesMatch;

        /// <summary>Does this column have an error column in the observed data</summary>
        public bool HasErrorColumn;

        /// <summary>The observed file where this column was found</summary>
        public string File;

        /// <summary>
        /// Converts a list of ColumnInfo into a DataTable
        /// </summary>
        /// <param name="data">A list of ColumnInfo</param>
        /// <returns>A DataTable of the contents of the given list. Used by the GUI for displaying this class.</returns>
        public static DataTable CreateDataTable(IEnumerable data)
        {
            DataTable newTable = new DataTable();
            newTable.Columns.Add("Name");
            newTable.Columns.Add("APSIM");
            newTable.Columns.Add("Type");
            newTable.Columns.Add("Data Type");
            newTable.Columns.Add("Error Bars");
            newTable.Columns.Add("File");

            if (data == null)
                return newTable;

            foreach (ColumnInfo columnInfo in data)
            {

                DataRow row = newTable.NewRow();
                row["Name"] = columnInfo.Name;
                row["APSIM"] = columnInfo.IsApsimVariable;
                if (columnInfo.VariableType == null)
                    row["Type"] = "";
                else
                    row["Type"] = columnInfo.VariableType.ToString();
                if (columnInfo.DataType == null)
                    row["Data Type"] = "";
                else
                    row["Data Type"] = columnInfo.DataType.ToString();
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

        /// <summary>
        /// From the list of columns read in, get a list of columns that match apsim variables.
        /// </summary>
        /// <param name="dataTable">A datatable of the observed data</param>
        /// <param name="simulations">The Simulations node</param>
        /// <param name="columnNames">A list to store column names in as they are found</param>
        /// <returns>A list of ColumnInfo objects for Observations to store</returns>
        public static List<ColumnInfo> GetAPSIMColumnsFromObserved(DataTable dataTable, Simulations simulations, List<string> columnNames)
        {
            List<ColumnInfo> infos = new List<ColumnInfo>();

            List<string> allColumnNames = dataTable.GetColumnNames().ToList();

            if (allColumnNames.Count == 0)
                return infos;

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
                        if (!string.IsNullOrEmpty(row[columnNameOriginal].ToString()) && !string.IsNullOrEmpty(row["_Filename"].ToString()))
                        {
                            filename = row["_Filename"].ToString();
                        }
                    }

                    ColumnInfo colInfo = new ColumnInfo();
                    colInfo.File = filename;
                    colInfo.Name = columnName;

                    colInfo.IsApsimVariable = "No";
                    colInfo.VariableType = null;
                    colInfo.DataType = GetTypeOfColumn(columnNameOriginal, dataTable.Rows);

                    if (colInfo.DataType != dataTable.Columns[j].DataType)
                        colInfo.DataTypesMatch = false;
                    else
                        colInfo.DataTypesMatch = true;

                    if (nameInAPSIMFormat)
                        colInfo.IsApsimVariable = "Not Found";
                    if (hasMaths)
                        colInfo.IsApsimVariable = "Maths";

                    if (nameIsAPSIMModel && variable != null)
                    {
                        colInfo.IsApsimVariable = "Yes";
                        colInfo.VariableType = variable.DataType;
                    }

                    colInfo.HasErrorColumn = false;
                    if (allColumnNames.Contains(columnName + "Error"))
                        colInfo.HasErrorColumn = true;

                    infos.Add(colInfo);
                }
            }

            return infos;
        }

        /// <summary>
        /// Determines if the given string matches the format for an APSIM variable.
        /// This is still rather simple, could be improved further if this class needed a performance boost.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <returns>True if it could be an APSIM variable, false if it cant</returns>
        private static bool NameIsAPSIMFormat(string columnName)
        {
            if (columnName.Contains('.'))
                return true;
            else
                return false;
        }

        /// <summary>
        /// This works out if the given column name actually matches an apsim variable using the locator
        /// </summary>
        /// <param name="columnName">The name to match</param>
        /// <param name="sims">Reference to Simulations</param>
        /// <returns>Returns null if the name doesn't match, or a VariableComposite reference if it was found.</returns>
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
            IModel firstPart = sims.Node.FindChild<IModel>(nameParts[0], recurse: true);
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

        /// <summary>
        /// Corrects the column types to match the type of data. Really important for making sure dates are stored as DateTimes instead of as strings.
        /// </summary>
        /// <param name="dataTable">Takes in an observed data in a datatable and corrects an mistakes to the data types on columns.</param>
        /// <returns>Returns the datatable with the columns corrected</returns>
        public static DataTable FixColumnTypes(DataTable dataTable)
        {
            List<string> columnNames = dataTable.GetColumnNames().ToList();

            //Check if any columns that only contain dates are being read in as strings (and won't graph properly because of it)
            foreach (string name in columnNames)
            {
                Type type = GetTypeOfColumn(name, dataTable.Rows);
                if (type != null)
                {
                    DataColumn column = dataTable.Columns[name];
                    int ordinal = column.Ordinal;

                    DataColumn newColumn = new DataColumn("NewColumn" + name, type);
                    dataTable.Columns.Add(newColumn);
                    newColumn.SetOrdinal(ordinal);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string value = row[name].ToString().Trim();
                        if(row[name].GetType() == typeof(DateTime))
                            value = DateUtilities.GetDateAsString(Convert.ToDateTime(row[name]));

                        if (value.Length > 0)
                        {
                            if (type == typeof(DateTime))
                            {
                                row[newColumn.ColumnName] = DateUtilities.GetDate(value);
                            }
                            else if (type == typeof(int))
                            {
                                row[newColumn.ColumnName] = int.Parse(value);
                            }
                            else if (type == typeof(double))
                            {
                                row[newColumn.ColumnName] = double.Parse(value);
                            }
                            else if (type == typeof(bool))
                            {
                                row[newColumn.ColumnName] = bool.Parse(value);
                            }
                            else if (type == typeof(string))
                            {
                                row[newColumn.ColumnName] = value;
                            }
                        }
                    }
                    dataTable.Columns.Remove(name);
                    newColumn.ColumnName = name;
                }
                else
                {
                    dataTable.Columns.Remove(name);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Determines what data type a column in a datatable should be
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="rows">Collection of rows from the DataTable</param>
        /// <returns>A Type for that matches the data in the given column</returns>
        private static Type GetTypeOfColumn(string columnName, DataRowCollection rows)
        {
            Type type = null;
            foreach (DataRow row in rows)
            {
                string value = row[columnName].ToString().Trim();
                if(row[columnName].GetType() == typeof(DateTime))
                    value = DateUtilities.GetDateAsString(Convert.ToDateTime(row[columnName]));

                if (!string.IsNullOrEmpty(value))
                {
                    Type cellType = GetTypeOfCell(value, type);
                    if (cellType == typeof(DateTime) && (type == null || type == typeof(DateTime)))
                    {
                        type = typeof(DateTime);
                    }
                    else if (cellType == typeof(int) && (type == null || type == typeof(DateTime) || type == typeof(int)))
                    {
                        type = typeof(int);
                    }
                    else if (cellType == typeof(double) && (type == null || type == typeof(DateTime) || type == typeof(int) || type == typeof(double)))
                    {
                        type = typeof(double);
                    }
                    else if (cellType == typeof(bool) && (type == null || type == typeof(DateTime) || type == typeof(int) || type == typeof(double)) || type == typeof(bool))
                    {
                        type = typeof(bool);
                    }
                    else if (cellType == typeof(string))
                    {
                        return typeof(string);
                    }
                }
            }
            return type;
        }

        /// <summary>
        /// Given a value as a string, this returns what type the value could be.
        /// In order of preference:
        /// - DateTime
        /// - Double
        /// - Int
        /// - Bool
        /// - String
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="knownType">What type this cell could be based on the previous cells in the column the value came from. This prevents a high preferenced type from being picked if a previous cell alreay ruled that out.</param>
        /// <returns>A Type for this cell</returns>
        private static Type GetTypeOfCell(string value, Type knownType = null)
        {
            if (knownType == null || knownType == typeof(DateTime))
            {
                string dateTrimmed = value;
                if (value.Contains(' '))
                    dateTrimmed = value.Split(' ')[0];

                if (DateUtilities.ValidateStringHasYear(dateTrimmed)) //try parsing to date
                {
                    string dateString = DateUtilities.ValidateDateString(dateTrimmed);
                    if (dateString != null)
                    {
                        DateTime date = DateUtilities.GetDate(value);
                        if (DateUtilities.CompareDates("1900/01/01", date) >= 0)
                            return typeof(DateTime);
                    }
                }
            }

            if (knownType == null || knownType == typeof(int) || knownType == typeof(double) || knownType == typeof(DateTime))
            {
                //try parsing to double
                bool canParse = double.TryParse(value, out double num);
                if (canParse)
                {
                    double wholeNum = num - Math.Floor(num);
                    if (MathUtilities.FloatsAreEqual(wholeNum, 0)) //try parsing to int
                        return typeof(int);
                    else
                        return typeof(double);
                }
            }

            if (knownType == null || knownType == typeof(DateTime) || knownType == typeof(int) || knownType == typeof(double) || knownType == typeof(bool))
            {
                bool canParse = bool.TryParse(value.Trim(), out bool boolean);
                if (canParse)
                    return typeof(bool);
            }

            return typeof(string);
        }
    }
}
