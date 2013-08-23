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

    [ViewName("ReportView")]
    public class Report
    {
        // Links.
        [Link] private DataStore DataStore = null;
        [Link] private IZone Paddock = null;
        [Link] private Simulation Simulation = null;

        // privates
        bool HaveCreatedTable = false;

        // Properties read in.
        public string Name { get; set; }
        public string[] Variables {get; set;}
        public string[] Events { get; set; }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        public void OnInitialised()
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
            List<Type> Types = new List<Type>();
            foreach (string VariableName in Variables)
            {
                object Value = Simulation.Get(VariableName);
                Values.Add(Value);
                if (Value == null)
                    Types.Add(typeof(int));
                else
                    Types.Add(Value.GetType());
            }

            if (!HaveCreatedTable)
            {
                DataStore.CreateTable(Name, Variables, Types.ToArray());
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