using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Reflection;
namespace Utility
{


    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ModelFunctions
    {
        public class EventSubscriber
        {
            public Model model;
            public MethodInfo handler;

            public string Name
            {
                get
                {
                    EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(handler, typeof(EventSubscribe), false);
                    return subscriberAttribute.Name;
                }
            }
        }
        public class EventPublisher
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
        /// Connect all events up in this simulation
        /// </summary>
        public static void ConnectEventsInAllModels(Model model)
        {
            Model[] modelsInScope = model.FindAll();

            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher.Name, modelsInScope))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.model, subscriber.handler);
                    publisher.AddEventHandler(model, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Connect all events up in the specified model.
        /// </summary>
        public static void ConnectEvent(Model model)
        {
            Model[] modelsInScope = model.FindAll();

            // Go through all events in the specified model and attach them to subscribers.
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher.Name, modelsInScope))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.model, subscriber.handler);
                    publisher.AddEventHandler(model, eventdelegate);
                }
            }

            // Go through all subscribers in the specified model and find the event publisher to connect to.
            foreach (EventSubscriber subscriber in FindEventSubscribers(null, model))
            {
                foreach (EventPublisher publisher in FindEventPublishers(subscriber.Name, modelsInScope))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.model, subscriber.handler);
                    publisher.AddEventHandler(publisher.Model, eventdelegate);
                }
            }

        }

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        public static List<EventSubscriber> FindEventSubscribers(string eventName, Model[] modelsInScope)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in modelsInScope)
                subscribers.AddRange(FindEventSubscribers(eventName, model));
            return subscribers;

        }

        /// <summary>
        /// Look through the specified model and return all event subscribers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        public static List<EventSubscriber> FindEventSubscribers(string eventName, Model model)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (MethodInfo method in model.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(method, typeof(EventSubscribe), false);
                if (subscriberAttribute != null && (eventName == null || subscriberAttribute.Name == eventName))
                    subscribers.Add(new EventSubscriber() { handler = method, model = model });
            }
            return subscribers;
        }

        /// <summary>
        /// Look through and return all models in scope for event publishers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        public static List<EventPublisher> FindEventPublishers(string eventName, Model[] modelsInScope)
        {
            List<EventPublisher> publishers = new List<EventPublisher>();
            foreach (Model model in modelsInScope)
                publishers.AddRange(FindEventPublishers(eventName, model));
            return publishers;

        }

        /// <summary>
        /// Look through the specified model and return all event publishers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        public static List<EventPublisher> FindEventPublishers(string eventName, Model model)
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
