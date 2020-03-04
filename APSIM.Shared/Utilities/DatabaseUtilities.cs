namespace APSIM.Shared.Utilities
{
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Some utilities for manipulating a data base connection.
    /// </summary>
    public class DatabaseUtilities
    {

        /// <summary>Add a parameter to a command</summary>
        /// <param name="command">The command to add a parameter to</param>
        /// <param name="name">The name of the parameter to add</param>
        /// <param name="value">The value of the parameter to add</param>
        public static void AddParameter(IDbCommand command, string name, object value)
        {
            var param1 = command.CreateParameter();
            param1.ParameterName = name;
            param1.Value = value;
            command.Parameters.Add(param1);
        }
        /// <summary>Convert a database to a string.</summary>
        public static string TableToString(IDbConnection connection, string tableName, IEnumerable<string> fieldNames = null)
        {
            string sql = "SELECT ";
            if (fieldNames == null)
                sql += "*";
            else
            {
                bool first = true;
                foreach (string fieldName in fieldNames)
                {
                    if (first)
                        first = false;
                    else
                        sql += ",";
                    sql += fieldName;
                }
            }
            sql += " FROM " + tableName;
            DataTable data = null;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                var reader = command.ExecuteReader();
                data = new DataTable();
                data.Load(reader);
            }
            System.IO.StringWriter writer = new System.IO.StringWriter();
            DataTableUtilities.DataTableToText(data, 0, ",", true, writer);
            return writer.ToString();
        }


    }
}
