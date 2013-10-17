using System.IO;
using System.Xml;
using Models.Core;
using System.Xml.Serialization;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates a collection of simulations. It is responsible for creating this collection,
    /// changing the structure of the components within the simulations, renaming components, adding
    /// new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    public class Simulations : Zone
    {
        class EventSubscriber
        {
            public Model model;
            public MethodInfo handler;
        }

        private string _FileName;

        /// <summary>
        /// Invoked when all simulations are about to commence.
        /// </summary>
        public event EventHandler AllCommencing;

        /// <summary>
        /// When all simulations have finished, this event will be invoked
        /// </summary>
        public event EventHandler AllCompleted;

        /// <summary>
        /// The name of the file containing the simulations.
        /// </summary>
        [XmlIgnore]
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {
                _FileName = value;
            }
        }

        /// <summary>
        /// Write the specified simulation set to the specified filename
        /// </summary>
        public void Write(string FileName)
        {
            StreamWriter Out = new StreamWriter(FileName);
            Out.Write(Utility.Xml.Serialise(this, true));
            Out.Close();
            this.FileName = FileName;
        }

        /// <summary>
        /// Read XML from specified reader. Called during Deserialisation.
        /// </summary>
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            Name = "Simulations";
            ResolveLinks();
        }

        /// <summary>
        /// Run all simulations. Return true if all ran ok.
        /// </summary>
        public bool Run()
        {
            // Connect all events for the simulations we're about to run.
            foreach (object Model in Models)
                if (Model is Simulation)
                    ConnectEvents(Model as Simulation);

            // Invoke the AllCommencing event.
            if (AllCommencing != null)
                AllCommencing(this, new EventArgs());

            // Run all simulations
            bool ok = true;
            foreach (object Model in Models)
                if (Model is Simulation)
                    ok = (Model as Simulation).Run() && ok;

            // Invoke the AllCompleted event.
            if (AllCompleted != null)
                AllCompleted(this, new EventArgs());

            // Disconnect all events for the simulations we just ran.
            foreach (object Model in Models)
                if (Model is Simulation)
                    ConnectEvents(Model as Simulation);
            return ok;
        }

        /// <summary>
        /// Run the specified simulation. Return true if it ran ok.
        /// </summary>
        public bool Run(Simulation Sim)
        {
            ConnectEvents(Sim);

            if (AllCommencing != null)
                AllCommencing(this, new EventArgs());

            Simulation Simulation = Sim as Simulation;
            bool ok = Simulation.Run();

            if (AllCompleted != null)
                AllCompleted(this, new EventArgs());

            DisconnectEvents(Sim);

            return ok;
        }

        /// <summary>
        /// Constructor, private to stop developers using it. Use 'Utility.Xml.Deserialise' instead.
        /// </summary>
        private Simulations() { }
        
        /// <summary>
        /// Connect all events up in this simulation
        /// </summary>
        private static void ConnectEvents(Simulation simulation)
        {
            Model[] modelsInScope = simulation.FindAll();

            // Loop through all events in all models: for each one locate all event handlers 9subscribers) and 
            // attach them to the event.
            foreach (Model model in modelsInScope)
            {
                foreach (EventInfo Event in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                {
                    foreach (EventSubscriber subscriber in FindEventSubscribers(Event.Name, modelsInScope))
                    {
                        // connect subscriber to the event.
                        Delegate eventdelegate = Delegate.CreateDelegate(Event.EventHandlerType, subscriber.model, subscriber.handler);
                        Event.AddEventHandler(model, eventdelegate);
                    }
                }


            }
        }

        /// <summary>
        /// Disconnect all events in this simulation
        /// </summary>
        private static void DisconnectEvents(Simulation simulation)
        {
            Model[] modelsInScope = simulation.FindAll();

            // Loop through all events in all models: for each one locate all event handlers 9subscribers) and 
            // attach them to the event.
            foreach (Model model in modelsInScope)
            {
                foreach (EventInfo Event in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                {
                    //foreach (EventSubscriber subscriber in FindEventSubscribers(Event.Name, modelsInScope))
                    {
                        // disconnect all subscribers from the event.
                        FieldInfo eventAsField = model.GetType().GetField(Event.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                        Delegate eventDelegate = eventAsField.GetValue(model) as Delegate;
                        if (eventDelegate != null)
                        {
                            foreach (Delegate del in eventDelegate.GetInvocationList())
                                Event.RemoveEventHandler(model, del);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// </summary>
        private static List<EventSubscriber> FindEventSubscribers(string eventName, Model[] modelsInScope)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in modelsInScope)
            {
                foreach (MethodInfo method in model.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(method, typeof(EventSubscribe), false);
                    if (subscriberAttribute != null && subscriberAttribute.Name == eventName)
                        subscribers.Add(new EventSubscriber() { handler = method, model = model });
                }
            }
            return subscribers;

        }


    }
}
