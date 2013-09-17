using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Xml.Serialization;
using Model.Core;
using System.Reflection;

namespace Model.Components
{

    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    public class Report : Model.Core.Model
    {
        // Links.
        [Link] private DataStore DataStore = null;
        [Link] private Zone Paddock = null;
        [Link] private Simulation Simulation = null;

        // privates
        bool HaveCreatedTable = false;

        // Properties read in.
        public string[] Variables {get; set;}
        public string[] Events { get; set; }

        // The user interface would like to know our paddock
        public Zone ParentZone { get { return Paddock; } }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        public override void OnInitialised()
        {
            Simulation.Completed += OnCompleted;
            HaveCreatedTable = false;
            foreach (string Event in Events)
            {
                string ComponentName = Utility.String.ParentName(Event, '.');
                string EventName = Utility.String.ChildName(Event, '.');

                if (ComponentName == null)
                    throw new Exception("Invalid syntax for reporting event: " + Event);
                object Component = Paddock.Find(ComponentName);
                if (Component == null)
                    throw new Exception(Name + " can not find the component: " + ComponentName);
                EventInfo ComponentEvent = Component.GetType().GetEvent(EventName);
                if (ComponentEvent == null)
                    throw new Exception("Cannot find event: " + EventName + " in model: " + ComponentName);

                ComponentEvent.AddEventHandler(Component, new NullTypeDelegate(OnReport));
            }
        }

        /// <summary>
        /// Event handler for the report event.
        /// </summary>
        public void OnReport()
        {
            // Get all variable values.
            List<object> Values = new List<object>();
            List<string> Names = new List<string>();
            List<Type> Types = new List<Type>();
            foreach (string VariableName in Variables)
            {
                object Value = Paddock.Get(VariableName);
                // If the value is an array then put each array value into Values individually.
                if (Value is Array)
                {
                    Array Arr = Value as Array;
                    for (int i = 0; i < Arr.Length; i++)
                    {
                        Names.Add(VariableName + "(" + (i+1).ToString() + ")");
                        Values.Add(Arr.GetValue(i));
                        Types.Add(Arr.GetValue(i).GetType());
                    }
                }
                else
                {
                    // Scalar
                    Values.Add(Value);
                    Names.Add(VariableName);
                    if (Value == null)
                        Types.Add(typeof(int));
                    else
                        Types.Add(Value.GetType());
                }
            }

            if (!HaveCreatedTable)
            {
                DataStore.CreateTable(Name, Names.ToArray(), Types.ToArray());
                HaveCreatedTable = true;
            }

            DataStore.WriteToTable(Simulation.Name, Name, Values.ToArray());
        }

        private void OnCompleted()
        {
            Simulation.Completed -= OnCompleted;

            foreach (string Event in Events)
            {
                string ComponentName = Utility.String.ParentName(Event, '.');
                string EventName = Utility.String.ChildName(Event, '.');

                object Component = Paddock.Find(ComponentName);
                EventInfo ComponentEvent = Component.GetType().GetEvent(EventName);

                ComponentEvent.RemoveEventHandler(Component, new NullTypeDelegate(OnReport));
            }

        }

    }
}