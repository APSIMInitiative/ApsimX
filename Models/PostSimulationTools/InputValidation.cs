﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Models.Core;
using Models.Core.Run;
using Models.Interfaces;
using Models.Storage;
using Models.Utilities;
using Newtonsoft.Json;
using static Models.Core.ScriptCompiler;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// Class
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.GridMultiPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    public class InputValidation : Model, IPostSimulationTool, IGridModel
    {
        /// <summary></summary>
        public struct ValidationResult 
        {
            /// <summary></summary>
            public string ColumnName;
            /// <summary></summary>
            public string InputName;
            /// <summary></summary>
            public string TableName;
            /// <summary></summary>
            public string Message;
            /// <summary></summary>
            public bool Error;
        }

        [Link]
        private DataStore dataStore = null;

        /// <summary></summary>
        public List<ValidationResult> Successes { get; set; }

        /// <summary></summary>
        public List<ValidationResult> Errors { get; set; }

        /// <summary>Gets or sets the table of values.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                List<GridTable> tables = new List<GridTable>();
                List<GridTableColumn> columns;

                columns = new List<GridTableColumn>();
                columns.Add(new GridTableColumn("InputName", new VariableProperty(this, GetType().GetProperty("Errors"))));
                columns.Add(new GridTableColumn("TableName", new VariableProperty(this, GetType().GetProperty("Errors"))));
                columns.Add(new GridTableColumn("ColumnName", new VariableProperty(this, GetType().GetProperty("Successes"))));
                columns.Add(new GridTableColumn("Message", new VariableProperty(this, GetType().GetProperty("Successes"))));
                tables.Add(new GridTable("Successes", columns, this));

                columns = new List<GridTableColumn>();
                columns.Add(new GridTableColumn("InputName", new VariableProperty(this, GetType().GetProperty("Errors"))));
                columns.Add(new GridTableColumn("TableName", new VariableProperty(this, GetType().GetProperty("Errors"))));
                columns.Add(new GridTableColumn("ColumnName", new VariableProperty(this, GetType().GetProperty("Errors"))));
                columns.Add(new GridTableColumn("Message", new VariableProperty(this, GetType().GetProperty("Errors"))));
                tables.Add(new GridTable("Errors", columns, this));

                return tables;
            }
        }

        /// <summary>
        /// Combines the live and dead forages into a single row for display and renames columns
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            return dt;
        }

        /// <summary>
        /// Breaks the lines into the live and dead parts and changes headers to match class
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            return dt;
        }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            List<string> tableNames = new List<string>();
            List<string> inputNames = new List<string>();
            Model model = dataStore as Model;
            List<ExcelInput> excelInputs = model.FindAllChildren<ExcelInput>().ToList();

            Simulations sims = model.FindAncestor<Simulations>();

            foreach (ExcelInput input in excelInputs)
            {
                foreach (string name in input.SheetNames)
                {
                    tableNames.Add(name);
                    inputNames.Add(input.Name);
                }
            }

            List<ValidationResult> newErrors = new List<ValidationResult>();
            List<ValidationResult> newGood = new List<ValidationResult>();
            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                string inputName = inputNames[i];

                List<string> columnNames = dataStore.Reader.ColumnNames(tableName);
                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    if (NameIsAPSIMFormat(columnName))
                    {
                        ValidationResult result = NameMatchesAPSIMModel(columnName, sims);
                        result.TableName = tableName;
                        result.InputName = inputName;
                        if (result.Error)
                            newErrors.Add(result);
                        else
                            newGood.Add(result);
                    }
                    else
                    {
                        ValidationResult result = new ValidationResult();
                        result.ColumnName = columnName;
                        result.TableName = tableName;
                        result.InputName = inputName;
                        result.Message = $"{columnName} is not considered an APSIM variable";
                        result.Error = false;
                        newErrors.Add(result);
                    }
                }
            }
            Errors = newErrors;
            Successes = newGood;
            return;
        }

        /// <summary></summary>
        private bool NameIsAPSIMFormat(string columnName)
        {
            if (columnName.Contains('.'))
                return true;
            else
                return false;
        }

        /// <summary></summary>
        private ValidationResult NameMatchesAPSIMModel(string columnName, Simulations sims)
        {
            ValidationResult result = new ValidationResult();
            result.ColumnName = columnName;
            result.Message = "Good";
            result.Error = false;

            string[] nameParts = columnName.Split('.');
            IModel firstPart = sims.FindDescendant(nameParts[0]);
            if (firstPart == null)
            {
                result.Error = true;
                result.Message = nameParts[0] + " of column " + columnName + " could not be found in simulation";
                return result;
            }
            string fullPath = firstPart.FullPath;
            for (int i = 1; i < nameParts.Length; i++)
            {
                fullPath += "." + nameParts[i];
            }
            try
            {
                IModel model = sims.FindByPath(fullPath) as IModel;
            }
            catch (Exception ex)
            {
                result.Error = true;
                result.Message = columnName + " could not be found in simulation. " + ex.Message;
                return result;
            }
            return result;
        }
    }
}
