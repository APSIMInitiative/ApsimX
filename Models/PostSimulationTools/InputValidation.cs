﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Aqua;
using Models.Core;
using Models.Core.Run;
using Models.Interfaces;
using Models.Storage;
using Models.Utilities;
using Newtonsoft.Json;

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
            public string SimulationName;
            /// <summary></summary>
            public string Rule;
            /// <summary></summary>
            public string Message;
            /// <summary></summary>
            public bool Error;
        }

        [Link]
        private DataStore dataStore = null;

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
                columns.Add(new GridTableColumn("ColumnName", new VariableProperty(this, GetType().GetProperty("Errors"))));
                columns.Add(new GridTableColumn("Rule", new VariableProperty(this, GetType().GetProperty("Errors"))));
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
            
            Model model = dataStore as Model;
            Simulations sims = model.FindAncestor<Simulations>();
            List<ValidationResult> newErrors = new List<ValidationResult>();

            List<ExcelInput> excelInputs = model.FindAllChildren<ExcelInput>().ToList();
            List<string> tableNames = new List<string>();
            List<string> inputNames = new List<string>();
            foreach (ExcelInput input in excelInputs)
            {
                if (input.Enabled == true) {
                    foreach (string name in input.SheetNames)
                    {
                        tableNames.Add(name);
                        inputNames.Add(input.Name);
                    }
                }
            }
            
            List<ValidationResult> columnErrors = ValidateColumnNames(tableNames, inputNames, sims);
            foreach(ValidationResult err in columnErrors)
                newErrors.Add(err);

            List<ValidationResult> checkTypes = ValidateColumnTypes(tableNames, inputNames, sims);
            foreach (ValidationResult err in checkTypes)
                newErrors.Add(err);

            //List<ValidationResult> totalErrors = ValidateLiveDeadEqualsTotal(tableNames, inputNames, sims);
            //foreach (ValidationResult err in totalErrors)
            //    newErrors.Add(err);

            List<ValidationResult> zerosErrors = ValidateZerosAndEmpties(tableNames, inputNames, sims);
            foreach (ValidationResult err in zerosErrors)
                newErrors.Add(err);
            
            List<ValidationResult> fractionErrors = ValidateFactionals(tableNames, inputNames, sims);
            foreach (ValidationResult err in fractionErrors)
                 newErrors.Add(err);
            
            Errors = newErrors;
            return;
        }

        private List<ValidationResult> ValidateFactionals(List<string> tableNames, List<string> inputNames, Simulations sims)
        {
            List<string> totalNames = new List<string>();

            List<ValidationResult> errors = new List<ValidationResult>();
            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                string inputName = inputNames[i];
                DataTable dt = dataStore.Reader.GetData(tableName);
                string[] columnsNames = dt.GetColumnNames();

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    int count = 0;
                    string columnName = columnsNames[j];
                    if (columnName.Contains(".Cover")) {
                        for (int k = 0; k < dt.Rows.Count; k++)
                        {
                            string value = dt.Rows[k][j].ToString();
                            if (value.Length == 0)
                            {
                                double.TryParse(value, out double num);

                                if (num < 0 || num >= 1)
                                {
                                    ValidationResult result = new ValidationResult();
                                    result.TableName = tableName;
                                    result.InputName = inputName;
                                    result.SimulationName = count.ToString();
                                    result.ColumnName = columnName;
                                    result.Rule = "Number not Fractional";
                                    result.Message = $"Number was not fractional (0-1). Value was {num}. Row: {k}";
                                    errors.Add(result);
                                }
                            }
                        }
                    }
                }
            }
            return errors;

        }

        private List<ValidationResult> ValidateZerosAndEmpties(List<string> tableNames, List<string> inputNames, Simulations sims)
        {
            List<string> totalNames = new List<string>();

            List<ValidationResult> errors = new List<ValidationResult>();
            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                string inputName = inputNames[i];
                DataTable dt = dataStore.Reader.GetData(tableName);
                string[] columnsNames = dt.GetColumnNames();

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    int count = 0;
                    string columnName = columnsNames[j];

                    for (int k = 0; k < dt.Rows.Count; k++)
                    {
                        string value = dt.Rows[k][j].ToString();
                        if (value.Length > 0)
                        {
                            value = value.Trim();
                            if (value.Length == 0)
                            {
                                ValidationResult result = new ValidationResult();
                                result.TableName = tableName;
                                result.InputName = inputName;
                                result.SimulationName = count.ToString();
                                result.ColumnName = columnName;
                                result.Rule = "Whitespace No Value";
                                result.Message = $"Whitespace was found in column {columnName} row {k}";
                                errors.Add(result);
                            }
                            if (value == "0")
                            {
                                count += 1;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        ValidationResult result = new ValidationResult();
                        result.TableName = tableName;
                        result.InputName = inputName;
                        result.SimulationName = count.ToString();
                        result.ColumnName = columnName;
                        result.Rule = "0 Value";
                        result.Message = $"A value of '0' was found in column {columnName}";
                        errors.Add(result);
                    }
                }
            }
            return errors;

        }

        enum ValidDataTypes { None=0, Date=1, Int=2, Double=3, String=4 }
        private List<ValidationResult> ValidateColumnTypes(List<string> tableNames, List<string> inputNames, Simulations sims)
        {
            List<string> totalNames = new List<string>();

            List<ValidationResult> errors = new List<ValidationResult>();
            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                string inputName = inputNames[i];
                DataTable dt = dataStore.Reader.GetData(tableName);
                string[] columnsNames = dt.GetColumnNames();

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    int count = 0;
                    string columnName = columnsNames[j];

                    //0: no type, 1: date, 2: int, 3: double, 4: string
                    bool error = false;

                    int[] typesCount = new int[5] { 0, 0, 0, 0, 0 };


                    for (int k = 0; k < dt.Rows.Count && !error; k++)
                    {
                        string value = dt.Rows[k][j].ToString();
                        ValidDataTypes type = ValidDataTypes.None;

                        if (value.Length > 0)
                        {
                            //try parsing to date
                            bool dateResult = DateTime.TryParse(value, out DateTime date);
                            if (dateResult == true)
                                if (DateUtilities.CompareDates("1900/01/01", date) >= 0)
                                    type = ValidDataTypes.Date;

                            //try parsing to int
                            //try parsing to double
                            bool d = double.TryParse(value, out double num);
                            if (d == true)
                            {
                                double wholeNum = num - Math.Floor(num);
                                if (wholeNum == 0)
                                    type = ValidDataTypes.Int;
                                else
                                    type = ValidDataTypes.Double;
                            }

                            //try parsing to string
                            if (type == 0)
                                type = ValidDataTypes.String;

                            typesCount[(int)type] += 1;
                        }

                    }

                    int errorCount = 0;
                    for (int k = 0; k < typesCount.Length; k++)
                        if (typesCount[k] > 0)
                            errorCount += 1;

                    if (errorCount > 1)
                    {
                        string message = $"Column {columnName} has data of different types.";
                        foreach (ValidDataTypes type in Enum.GetValues(typeof(ValidDataTypes)))
                            if (typesCount[(int)type] > 0)
                                message += $" Type {type.ToString()} read {typesCount[(int)type]} times.";

                        error = true;
                        ValidationResult result = new ValidationResult();
                        result.TableName = tableName;
                        result.InputName = inputName;
                        result.SimulationName = count.ToString();
                        result.ColumnName = columnName;
                        result.Rule = "Column Type Error";
                        result.Message = message;
                        errors.Add(result);
                    }
                }
            }
            return errors;

        }

        private List<ValidationResult> ValidateLiveDeadEqualsTotal(List<string> tableNames, List<string> inputNames, Simulations sims)
        {
            List<string> totalNames = new List<string>();

            List<ValidationResult> errors = new List<ValidationResult>();
            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];

                List<string> columnNames = dataStore.Reader.ColumnNames(tableName);
                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    if (columnName.Contains("Total"))
                    {
                        string cleanedName = columnName.Trim();
                        int end = cleanedName.IndexOf("Total");
                        int start = cleanedName.LastIndexOf('.')+1;
                        cleanedName = cleanedName.Substring(start, end-start);
                        totalNames.Add(cleanedName);

                    }
                }
            }
            return errors;

        }

        private List<ValidationResult> ValidateColumnNames(List<string> tableNames, List<string> inputNames, Simulations sims)
        {
            List<ValidationResult> errors = new List<ValidationResult>();
            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                string inputName = inputNames[i];

                List<string> columnNamesRead = new List<string>();

                List<string> columnNames = dataStore.Reader.ColumnNames(tableName);
                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    if (columnNamesRead.Contains(columnName))
                    {
                        ValidationResult result = new ValidationResult();
                        result.TableName = tableName;
                        result.InputName = inputName;
                        result.SimulationName = "All";
                        result.Rule = "Column Exists Twice";
                        result.Message = $"{columnName} is listed more than once in {inputName}:{tableName}";
                        errors.Add(result);
                    } 
                    else
                    {
                        columnNamesRead.Add(columnName);
                    }

                    if (NameIsAPSIMFormat(columnName))
                    {
                        ValidationResult result = NameMatchesAPSIMModel(columnName, sims);
                        result.TableName = tableName;
                        result.InputName = inputName;
                        result.SimulationName = "All";
                        result.Rule = "Column != APSIM Column";
                        if (result.Error)
                            errors.Add(result);
                        else
                        {//check types

                        }
                    }
                    else
                    {
                        ValidationResult result = new ValidationResult();
                        result.ColumnName = columnName;
                        result.TableName = tableName;
                        result.InputName = inputName;
                        result.SimulationName = "All";
                        result.Rule = "No . in Column";
                        result.Message = $"{columnName} is not considered an APSIM variable";
                        result.Error = false;
                        errors.Add(result);
                    }
                }
            }
            return errors;
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
            sims.Links.Resolve(firstPart,true, true, false);
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
