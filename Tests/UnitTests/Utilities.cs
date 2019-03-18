
namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;

    public class Utilities
    {
        /// <summary>
        /// Event handler for a job runner's <see cref="IJobRunner.AllJobsCompleted"/> event.
        /// Asserts that the job ran successfully.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        public static void EnsureJobRanGreen(object sender, JobCompleteArgs args)
        {
            if (args.exceptionThrowByJob != null)
                throw new Exception(string.Format("Exception was thrown when running via {0}, when we expected no error to be thrown.", sender.GetType().Name), args.exceptionThrowByJob);
        }

        /// <summary>Call an event in a model</summary>
        public static void CallEvent(object model, string eventName, object[] arguments = null)
        {
            MethodInfo eventToInvoke = model.GetType().GetMethod("On" + eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (eventToInvoke != null)
            {
                if (arguments == null)
                    arguments = new object[] { model, new EventArgs() };
                eventToInvoke.Invoke(model, arguments);
            }
        }

        /// <summary>Inject a link into a model</summary>
        public static void InjectLink(object model, string linkFieldName, object linkFieldValue)
        {
            ReflectionUtilities.SetValueOfFieldOrProperty(linkFieldName, model, linkFieldValue);
        }


        /// <summary>Convert a SQLite table to a string.</summary>
        public static string TableToString(string fileName, string tableName, IEnumerable<string> fieldNames = null)
        {
            SQLite database = new SQLite();
            database.OpenDatabase(fileName, true);
            var st = TableToString(database, tableName, fieldNames);
            database.CloseDatabase();
            return st;
        }

        /// <summary>Convert a SQLite table to a string.</summary>
        public static string TableToString(IDatabaseConnection database, string tableName, IEnumerable<string> fieldNames = null)
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
            var orderByFieldNames = new List<string>();
            if (database.GetColumnNames(tableName).Contains("CheckpointID"))
                orderByFieldNames.Add("[CheckpointID]");
            if (database.GetColumnNames(tableName).Contains("SimulationID"))
                orderByFieldNames.Add("[SimulationID]");
            if (database.GetColumnNames(tableName).Contains("Clock.Today"))
                orderByFieldNames.Add("[Clock.Today]");
            if (orderByFieldNames.Count > 0)
                sql += " ORDER BY " + StringUtilities.BuildString(orderByFieldNames.ToArray(), ",");
            DataTable data = database.ExecuteQuery(sql);
            return TableToString(data);
        }

        /// <summary>Convert a SQLite query to a string.</summary>
        public static string TableToStringUsingSQL(IDatabaseConnection database, string sql)
        {
            var data = database.ExecuteQuery(sql);
            return TableToString(data);
        }

        /// <summary>Convert a DataTable to a string.</summary>
        public static string TableToString(DataTable data)
        {
            StringWriter writer = new StringWriter();
            DataTableUtilities.DataTableToText(data, 0, ",", true, writer);
            return writer.ToString();
        }


    }
}
