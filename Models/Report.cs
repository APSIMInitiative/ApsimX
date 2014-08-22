using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Xml.Serialization;
using Models.Core;
using System.Reflection;

namespace Models
{

    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    public class Report : Model
    {
        /// <summary>
        /// A variable class for looking after multiple values for a single variable. 
        /// The variable might be a scalar, an array or a class or structure. This class 
        /// has members for returning names, types and values ready for DataStore. It 
        /// can handle array sizes changing through a simulation. It "flattens" arrays and structures
        /// e.g. if the variable is sw_dep and has 3 elements then
        ///      Names -> sw_dep(1), sw_dep(2), sw_dep(3)
        ///      Types ->    double,    double,    double
        ///      
        /// e.g. if the variable is a struct {double A; double B; double C;}
        ///      Names -> struct.A, struct.B, struct.C
        /// </summary>
        private class VariableMember
        {
            private Report Report;
            private string FullName;
            private List<string> _Names = new List<string>();
            private List<Type> _Types = new List<Type>();
            private List<object> _Values = new List<object>();
            private int MaxNumArrayElements;
            private Type TypeOfThisMember;
            private string Alias;

            /// <summary>
            /// Constructor
            /// </summary>
            public VariableMember(string FullName, Report report)
            {
                this.FullName = FullName;
                int posAlias = this.FullName.IndexOf(" as ");
                if (posAlias != -1)
                {
                    Alias = this.FullName.Substring(posAlias + 4);
                    this.FullName = this.FullName.Substring(0, posAlias);
                }
                this.Report = report;
            }

            /// <summary>
            /// Analyse the variable to "flatten" arrays and classes to a list of names and types.
            /// </summary>
            public void Analyse()
            {
                // Work out the type of data we're dealing with.
                TypeOfThisMember = DetermineType();

                if (TypeOfThisMember != null && _Values.Count > 0 && TypeOfThisMember.IsArray)
                    {
                        // Array - calculate the maximum number of array elements and analyse each array element
                        // on row 0 for a name and type.
                        MaxNumArrayElements = CalcMaxNumArrayElements();

                        Array Arr = _Values[0] as Array;
                        for (int Col = 0; Col < MaxNumArrayElements; Col++)
                        {
                            string Heading = FullName;
                            if (Alias != null)
                                Heading = Alias;
                            Heading += "(" + (Col + 1).ToString() + ")";
                            if (Col < Arr.Length)
                                AnalyseValue(Heading, Arr.GetValue(Col));
                        }
                    }
                else if (_Values.Count > 0)
                {
                    if (Alias != null)
                        AnalyseValue(Alias, _Values[0]);
                    else                        
                        AnalyseValue(FullName, _Values[0]);
                }
            }

            /// <summary>
            /// Go through values and determine the type of data.
            /// </summary>
            /// <returns></returns>
            private Type DetermineType()
            {
                foreach (object value in _Values)
                    if (value != null)
                        return value.GetType();
                return null;
            }

            /// <summary>
            /// Return a list of names for this variable after flattening out arrays and 
            /// structures.
            /// </summary>
            public string[] Names { get { return _Names.ToArray(); } }

            /// <summary>
            /// Return a list of types for this variable.
            /// </summary>
            public Type[] Types { get { return _Types.ToArray(); } }

            /// <summary>
            /// Return an array of values for the specified row.
            /// </summary>
            public object[] Values(int Row)
            {
                List<object> AllValues = new List<object>();

                if (TypeOfThisMember != null && TypeOfThisMember.IsArray)
                    {
                        // Add required columns
                        Array Arr = _Values[Row] as Array;
                        for (int Col = 0; Col < MaxNumArrayElements; Col++)
                        {
                            string Heading = FullName + "(" + (Col + 1).ToString() + ")";
                            if (Col < Arr.Length)
                                AddValueToList(AllValues, Row, Heading, Arr.GetValue(Col));
                        }
                    }
                else if (_Values.Count > 0)
                    AddValueToList(AllValues, Row, FullName, _Values[Row]);
                return AllValues.ToArray();
            }

            /// <summary>
            /// Return the number of values stored in this variable.
            /// </summary>
            public int NumValues { get { return _Values.Count; } }

            /// <summary>
            /// Store the current value in our array of values.
            /// </summary>
            public void StoreValue()
            {
                object Value = Report.Get(FullName);
                if (Value != null)
                {
                    if (Value.GetType().IsArray)
                    {
                        Array A = Value as Array;
                        Value = A.Clone();
                    }
                    else if (Value.GetType().IsClass)
                    {
                        Value = Utility.Reflection.Clone(Value);
                    }

                    _Values.Add(Value);
                }
            }

            /// <summary>
            /// Analyse the value passed in and store it's name and type in the
            /// Names and Types List.
            /// </summary>
            private void AnalyseValue(string Name, object Value)
            {
                string cleanName = Name.Replace("[", "").Replace("]", "");

                if (Value == null)
                {
                    _Names.Add(cleanName);
                    _Types.Add(null);
                }
                else
                {
                    Type T = Value.GetType();

                    // Scalar
                    if (T == typeof(DateTime) || T == typeof(string) || !T.IsClass)
                    {
                        // Built in type.
                        _Names.Add(cleanName);
                        _Types.Add(T);
                    }
                    else
                    {
                        // class or struct.
                        foreach (PropertyInfo Property in Utility.Reflection.GetPropertiesSorted(T, BindingFlags.Instance | BindingFlags.Public))
                        {
                            _Names.Add(cleanName + "." + Property.Name);
                            _Types.Add(Property.PropertyType);
                        }
                    }
                }
            }

            /// <summary>
            /// Add the specified Value to the AllValues list. If Value is a class then it will add 
            /// class fields and property values as well.
            /// </summary>
            private void AddValueToList(List<object> AllValues, int Row, string Name, object Value)
            {
                if (Value == null)
                    AllValues.Add(Value);
                else
                {
                    Type T = Value.GetType();

                    // Scalar
                    if (T == typeof(DateTime) || T == typeof(string) || !T.IsClass)
                    {
                        // Built in type.
                        AllValues.Add(Value);
                    }
                    else
                    {
                        // class or struct.
                        foreach (PropertyInfo Property in Utility.Reflection.GetPropertiesSorted(T, BindingFlags.Instance | BindingFlags.Public))
                        {
                            AllValues.Add(Property.GetValue(Value, null));
                        }
                        
                    }
                }
            }

            /// <summary>
            /// Calculate the maximum number of array elements.
            /// </summary>
            private int CalcMaxNumArrayElements()
            {
                int MaxNumValues = 0;
                foreach (object Value in _Values)
                {
                    if (Value != null)
                        MaxNumValues = Math.Max(MaxNumValues, (Value as Array).Length);
                }
                return MaxNumValues;
            }
        }

        // privates
        private List<VariableMember> Members = null;

        [Link]
        public Simulation Simulation = null;

        // Properties read in.
        [Summary]
        [Description("Output variables")]
        public string[] VariableNames {get; set;}

        [Summary]
        [Description("Output frequency")]
        public string[] EventNames { get; set; }

        public object Get(string name)
        {
            return base.Get(name);
        }
        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        public override void OnSimulationCommencing()
        {
            List<string> eventNames = new List<string>();
            for (int i = 0; i < EventNames.Length; i++ )
            {
                if (EventNames[i] != "")
                {
                    eventNames.Add(EventNames[i].Trim());
                    base.Events.Subscribe(EventNames[i].Trim(), OnReport);
                }
            }
            EventNames = eventNames.ToArray();

            // sanitise the variable names and remove duplicates
            List<string> variableNames = new List<string>();
            for (int i = 0; i < VariableNames.Length; i++)
            {
                bool isDuplicate = Utility.String.IndexOfCaseInsensitive(variableNames, VariableNames[i].Trim()) != -1;
                if (!isDuplicate && VariableNames[i] != "")
                    variableNames.Add(VariableNames[i].Trim());
            }
            VariableNames = variableNames.ToArray();
        }

        /// <summary>
        /// Event handler for the report event.
        /// </summary>
        public void OnReport(object sender, EventArgs e)
        {
            if (Members == null)
                FindVariableMembers();
            foreach (VariableMember Variable in Members)
                Variable.StoreValue();
        }

        /// <summary>
        /// Public method to allow reporting from scripts.
        /// </summary>
        public void DoReport()
        {
            if (Members == null)
                FindVariableMembers();
            foreach (VariableMember Variable in Members)
                Variable.StoreValue();
        }

        /// <summary>
        /// Fill the Members list with VariableMember objects for each variable.
        /// </summary>
        private void FindVariableMembers()
        {
            Members = new List<VariableMember>();

            List<string> Names = new List<string>();
            List<Type> Types = new List<Type>();
            foreach (string FullVariableName in VariableNames)
            {
                if (FullVariableName != "")
                    Members.Add(new VariableMember(FullVariableName, this));
            }
        }

        /// <summary>
        /// Simulation has completed - write the report table.
        /// </summary>
        public override void OnSimulationCompleted()
        {
            if (Simulation != null)
            {
                // Get rid of old data in .db
                DataStore DataStore = new DataStore(this);
                DataStore.DeleteOldContentInTable(Simulation.Name, Name);

                // Write and store a table in the DataStore
                if (Members != null && Members.Count > 0)
                {
                    DataTable table = new DataTable();

                    foreach (VariableMember Variable in Members)
                    {
                        Variable.Analyse();
                        for (int i = 0; i < Variable.Names.Length; i++)
                        {
                            if (Variable.Types[i] == null)
                                table.Columns.Add(Variable.Names[i], typeof(int));
                            else
                                table.Columns.Add(Variable.Names[i], Variable.Types[i]);
                        }
                    }

                    for (int Row = 0; Row < Members[0].NumValues; Row++)
                    {
                        DataRow newRow = table.NewRow();
                        int colIndex = 0;
                        foreach (VariableMember Variable in Members)
                        {
                            foreach (object value in Variable.Values(Row))
                            {
                                if (value != null)
                                    newRow[colIndex] = value;
                                colIndex++;
                            }
                        }
                        table.Rows.Add(newRow);
                    }

                    DataStore.WriteTable(Simulation.Name, Name, table);

                    Members.Clear();
                    Members = null;
                }

                UnsubscribeAllEventHandlers();
                DataStore.Disconnect();
                DataStore = null;
            }
        }

        private void UnsubscribeAllEventHandlers()
        {
            // Unsubscribe to all events.
            foreach (string Event in EventNames)
                if ( (Event != null) && (Event != "") )
                    base.Events.Unsubscribe(Event);
        }

    }
}