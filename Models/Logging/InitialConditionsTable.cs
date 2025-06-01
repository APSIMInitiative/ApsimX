using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Logging
{

    /// <summary>
    /// A class encapsulating an initial conditions table.
    /// </summary>
    public class InitialConditionsTable
    {
        /// <summary>
        /// The model
        /// </summary>
        public IModel Model { get; set; }

        private string relativePath;

        /// <summary>
        ///
        /// </summary>
        public IEnumerable<InitialCondition> Conditions { get; private set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="conditions"></param>
        /// <param name="senderPath"></param>
        public InitialConditionsTable(IModel provider, IEnumerable<InitialCondition> conditions, string senderPath)
        {
            Model = provider;
            relativePath = senderPath;
            Conditions = conditions;
        }

        /// <summary>
        /// Export the initial conditions table to markdown format.
        /// </summary>
        /// <returns></returns>
        public string ToMarkdown()
        {
            DataTable scalarsTable = new DataTable();
            DataTable vectorsTable = new DataTable();
            scalarsTable.Columns.Add("Name");
            scalarsTable.Columns.Add("Value");
            foreach (InitialCondition condition in Conditions)
            {
                if (condition.TypeName.Contains("[]"))
                {
                    string columnName = condition.Name;
                    if (!string.IsNullOrEmpty(condition.Units))
                        columnName += $" ({condition.Units})";

                    string[] value = condition.Value.Split(',');
                    if (condition.TypeName == "Double[]")
                    {
                        if (string.IsNullOrEmpty(condition.DisplayFormat))
                            DataTableUtilities.AddColumn(vectorsTable, columnName, MathUtilities.StringsToDoubles(value));
                        else
                        {
                            value = value.Select(x => double.Parse(x).ToString(condition.DisplayFormat)).ToArray();
                            DataTableUtilities.AddColumn(vectorsTable, columnName, value);
                        }
                    }
                    else
                        DataTableUtilities.AddColumn(vectorsTable, columnName, value);
                }
                else
                {
                    string value = condition.Value;
                    if (!string.IsNullOrEmpty(condition.Units))
                        value += $" ({condition.Units})";
                    scalarsTable.Rows.Add(condition.Description, value);
                }
            }

            StringBuilder markdown = new StringBuilder();
            markdown.AppendLine($"### {relativePath}");
            markdown.AppendLine();
            if (scalarsTable.Rows.Count > 0)
                markdown.AppendLine(DataTableUtilities.ToMarkdown(scalarsTable, true));
            if (vectorsTable.Rows.Count > 0)
                markdown.AppendLine(DataTableUtilities.ToMarkdown(vectorsTable, true));
            return markdown.ToString();
        }
    }
}