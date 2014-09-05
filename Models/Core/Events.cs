using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Models.Core
{
    public class Events
    {
        /// <summary>
        /// A wrapper around an event subscriber MethodInfo.
        /// </summary>
        private class EventSubscriber
        {
            public Model Model;
            public MethodInfo MethodInfo;
            public string Name;

            public virtual Delegate GetDelegate(EventPublisher publisher)
            {
                return Delegate.CreateDelegate(publisher.EventHandlerType, Model, MethodInfo);
            }
        }

        /// <summary>
        /// A wrapper around an event publisher EventInfo.
        /// </summary>
        private class EventPublisher
        {
            public Model Model;
            public EventInfo EventInfo;
            public string Name { get { return EventInfo.Name; } }
            public Type EventHandlerType { get { return EventInfo.EventHandlerType; } }
            public void AddEventHandler(Model model, Delegate eventDelegate)
            {
                EventInfo.AddEventHandler(model, eventDelegate);
            }
        }

        /// <summary>
        /// A private event subscription class.
        /// </summary>
        private class DynamicEventSubscriber : EventSubscriber
        {
            public string publishedEventPath;
            public EventHandler subscriber;
            public Model parent;
            public Model matchingModel;
            string ComponentName;
            string EventName;

            public DynamicEventSubscriber(string namePath, EventHandler handler, Model parentModel)
            {
                publishedEventPath = namePath;
                subscriber = handler;
                parent = parentModel;

                ComponentName = Utility.String.ParentName(publishedEventPath, '.');
                if (ComponentName == null)
                    throw new Exception("Invalid syntax for event: " + publishedEventPath);

                EventName = Utility.String.ChildName(publishedEventPath, '.');
            }
            public void Connect(Model model)
            {
                object Component = model.Get(ComponentName);
                if (Component == null)
                    throw new Exception(model.FullPath + " can not find the component: " + ComponentName);
                EventInfo ComponentEvent = Component.GetType().GetEvent(EventName);
                if (ComponentEvent == null)
                    throw new Exception("Cannot find event: " + EventName + " in model: " + ComponentName);

                ComponentEvent.AddEventHandler(Component, subscriber);
            }
            public void Disconnect(Model model)
            {
                object Component = model.Get(ComponentName);
                if (Component != null)
                {
                    EventInfo ComponentEvent = Component.GetType().GetEvent(EventName);
                    if (ComponentEvent != null)
                        ComponentEvent.RemoveEventHandler(Component, subscriber);
                }
            }
            public override Delegate GetDelegate(EventPublisher publisher)
            {
                return subscriber;
            }
            public bool IsMatch(EventPublisher publisher)
            {
                if (matchingModel == null)
                    matchingModel = parent.Get(ComponentName) as Model;

                return publisher.Model.FullPath == matchingModel.FullPath && EventName == publisher.Name;
            }
        }

        /// <summary>
        /// The model that we are working for.
        /// </summary>
        private Model RelativeTo;

        /// <summary>
        /// A list of all dynamic event subscriptions e.g. from REPORT
        /// </summary>
        [NonSerialized]
        private List<DynamicEventSubscriber> EventSubscriptions;

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        public Events(Model relativeTo)
        {
            RelativeTo = relativeTo;
        }

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        public void Subscribe(string namePath, EventHandler handler)
        {
            if (EventSubscriptions == null)
                EventSubscriptions = new List<DynamicEventSubscriber>();
            DynamicEventSubscriber eventSubscription = new DynamicEventSubscriber(namePath, handler, RelativeTo);
            EventSubscriptions.Add(eventSubscription);
            eventSubscription.Connect(RelativeTo);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        public void Unsubscribe(string namePath)
        {
            foreach (DynamicEventSubscriber eventSubscription in EventSubscriptions)
            {
                if (eventSubscription.publishedEventPath == namePath)
                {
                    eventSubscription.Disconnect(RelativeTo);
                    EventSubscriptions.Remove(eventSubscription);
                    return;
                }
            }
        }

        /// <summary>
        /// Connect all events. Usually only called by the APSIMX infrastructure.
        /// </summary>
        public void Connect()
        {
            Simulation simulation = RelativeTo.ParentOfType(typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.IsRunning)
                {
                    // This model is being asked to connect itself AFTER events and links
                    // have already been connected.  We have to go through all event declarations
                    // event handlers, all links in this model and all links other other models
                    // that refer to this model. This will be time consuming.

                    // 1. connect all event declarations.
                    ConnectEventPublishers();

                    // 2. connect all event handlers.
                    ConnectEventSubscribers();
                }
                else
                {
                    // we can take the quicker approach and simply connect event declarations
                    // (publish) with their event handlers and assume that our event handlers will
                    // be connected by whichever model that is publishing that event.
                    ConnectEventPublishers();
                }
            }
        }

        /// <summary>
        /// Disconnect all events. Usually only called by the APSIMX infrastructure.
        /// </summary>
        public void Disconnect()
        {
            DisconnectEventPublishers();
            DisconnectEventSubscribers();
        }

        /// <summary>
        /// Connect all event publishers for this model.
        /// </summary>
        private void ConnectEventPublishers()
        {
            // Go through all events in the specified model and attach them to subscribers.
            foreach (EventPublisher publisher in FindEventPublishers(null, RelativeTo))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = subscriber.GetDelegate(publisher);
                    publisher.AddEventHandler(RelativeTo, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Connect all event subscribers for this model.
        /// </summary>
        private void ConnectEventSubscribers()
        {
            // Connect all dynamic eventsubscriptions.
            if (EventSubscriptions != null)
                foreach (DynamicEventSubscriber eventSubscription in EventSubscriptions)
                    eventSubscription.Connect(RelativeTo);

            // Go through all subscribers in the specified model and find the event publisher to connect to.
            foreach (EventSubscriber subscriber in FindEventSubscribers(null, RelativeTo))
            {
                foreach (EventPublisher publisher in FindEventPublishers(subscriber))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
                    publisher.AddEventHandler(publisher.Model, eventdelegate);
                }
            }

        }

        /// <summary>
        /// Disconnect all published events in the specified 'model'
        /// </summary>
        private void DisconnectEventPublishers()
        {
            foreach (EventPublisher publisher in FindEventPublishers(null, RelativeTo))
            {
                FieldInfo eventAsField = FindEventField(publisher);
                Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
                if (eventDelegate != null)
                {
                    foreach (Delegate del in eventDelegate.GetInvocationList())
                    {
                        //if (model == null || del.Target == model)
                        publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
                    }
                }
            }
        }

        /// <summary>
        /// Disconnect all subscribed events in the specified 'model'
        /// </summary>
        private void DisconnectEventSubscribers()
        {
            foreach (EventSubscriber subscription in FindEventSubscribers(null, RelativeTo))
            {
                foreach (EventPublisher publisher in FindEventPublishers(subscription))
                {
                    FieldInfo eventAsField = publisher.Model.GetType().GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                    Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
                    if (eventDelegate != null)
                    {
                        foreach (Delegate del in eventDelegate.GetInvocationList())
                        {
                            if (del.Target == RelativeTo)
                                publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Locate and return the event backing field for the specified event. Returns
        /// null if not found.
        /// </summary>
        private static FieldInfo FindEventField(EventPublisher publisher)
        {
            Type t = publisher.Model.GetType();
            FieldInfo eventAsField = t.GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            while (eventAsField == null && t.BaseType != typeof(Object))
            {
                t = t.BaseType;
                eventAsField = t.GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return eventAsField;
        }

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private List<EventSubscriber> FindEventSubscribers(EventPublisher publisher)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in GetModelsVisibleToEvents(publisher.Model))
            {
                subscribers.AddRange(FindEventSubscribers(publisher.Name, model));

                // Add dynamic subscriptions if they match
                if (EventSubscriptions != null)
                    foreach (DynamicEventSubscriber subscriber in EventSubscriptions)
                    {
                        if (subscriber.IsMatch(publisher))
                            subscribers.Add(subscriber);
                    }

            }
            return subscribers;

        }

        /// <summary>
        /// Return a list of models that are visible for event connecting purposes.
        /// This is different to models in scope unfortunatley. Need to rethink this.
        /// </summary>
        private static List<Model> GetModelsVisibleToEvents(Model model)
        {
            List<Model> models = new List<Model>();

            // Find our parent Simulation or Zone.
            Model obj = model;
            while (obj != null && !(obj is Zone) && !(obj is Simulation))
            {
                obj = obj.Parent as Model;
            }
            if (obj == null)
                throw new ApsimXException(model.FullPath, "Cannot find models to connect events to");
            if (obj is Simulation)
            {
                models.AddRange((obj as Simulation).Children.AllRecursively);
            }
            else
            {
                // return all models in zone and all direct children of zones parent.
                models.AddRange((obj as Zone).Children.AllRecursively);
                if (obj.Parent != null)
                    models.AddRange(obj.Parent.Models);
            }

            return models;
        }

        /// <summary>
        /// Look through the specified model and return all event subscribers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        private static List<EventSubscriber> FindEventSubscribers(string eventName, Model model)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (MethodInfo method in model.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)Utility.Reflection.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                if (subscriberAttribute != null && (eventName == null || subscriberAttribute.ToString() == eventName))
                    subscribers.Add(new EventSubscriber()
                    {
                        Name = subscriberAttribute.ToString(),
                        MethodInfo = method,
                        Model = model
                    });
            }
            return subscribers;
        }

        /// <summary>
        /// Look through and return all models in scope for event publishers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private static List<EventPublisher> FindEventPublishers(EventSubscriber subscriber)
        {
            List<EventPublisher> publishers = new List<EventPublisher>();
            foreach (Model model in subscriber.Model.FindAll())
                publishers.AddRange(FindEventPublishers(subscriber.Name, model));
            return publishers;

        }

        /// <summary>
        /// Look through the specified model and return all event publishers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        private static List<EventPublisher> FindEventPublishers(string eventName, Model model)
        {
            List<EventPublisher> publishers = new List<EventPublisher>();
            foreach (EventInfo Event in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
            {
                if (eventName == null || Event.Name == eventName)
                    publishers.Add(new EventPublisher() { EventInfo = Event, Model = model });
            }
            return publishers;
        }


    }
}
