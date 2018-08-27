// -----------------------------------------------------------------------
// <copyright file="Events.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// An event handling class
    /// </summary>
    public class Events : IEvent
    {
        private IModel relativeTo;
        private ScopingRules scope = new ScopingRules();

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">The model this events instance is relative to</param>
        internal Events(IModel relativeTo)
        {
            this.relativeTo = relativeTo;
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        public void ConnectEvents()
        {
            // Get a complete list of all models in simulation (including the simulation itself).
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(Apsim.ChildrenRecursively(relativeTo));

            List<Publisher> publishers = Publisher.FindAll(allModels);

            // Connect publishers to subscribers.
            foreach (Publisher publisher in publishers)
            {
                var subscribers = Subscriber.FindAll(publisher.Name, publisher.Model as IModel, scope);
                subscribers.ForEach(subscriber => publisher.ConnectSubscriber(subscriber));
            }
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        public void DisconnectEvents()
        {
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(Apsim.ChildrenRecursively(relativeTo));
            List<Events.Publisher> publishers = Events.Publisher.FindAll(allModels);
            foreach (Events.Publisher publisher in publishers)
                publisher.DisconnectAll();
        }

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public void Subscribe(string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(relativeTo, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(relativeTo) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Subscribe to the event.
            componentEvent.AddEventHandler(component, handler);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public void Unsubscribe(string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(relativeTo, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(relativeTo) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Unsubscribe to the event.
            componentEvent.RemoveEventHandler(component, handler);
        }

        /// <summary>
        /// Call the specified event on the specified model and all child models.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments. Can be null</param>
        internal void Publish(string eventName, object[] args)
        {
            List<Subscriber> subscribers = Subscriber.FindAll(eventName, relativeTo, scope);

            foreach (Subscriber subscriber in subscribers)
                subscriber.Invoke(args);
        }
        
        /// <summary>A wrapper around an event subscriber MethodInfo.</summary>
        internal class Subscriber
        {
            /// <summary>The model instance containing the event hander.</summary>
            public object Model { get; set; }

            /// <summary>The method info for the event handler.</summary>
            private MethodInfo methodInfo { get; set; }

            /// <summary>Gets or sets the name of the event.</summary>
            public string Name { get; private set; }

            /// <summary>Find all event subscribers in the specified models.</summary>
            /// <param name="name">The name of the event to look for</param>
            /// <param name="relativeTo">The model to use in scoping lookup</param>
            /// <param name="scope">Scoping rules</param>
            /// <returns>The list of event subscribers</returns>
            internal static List<Subscriber> FindAll(string name, IModel relativeTo, ScopingRules scope)
            {
                List<Subscriber> subscribers = new List<Subscriber>();
                foreach (IModel modelNode in scope.FindAll(relativeTo as IModel))
                {
                    foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                    {
                        EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                        if (subscriberAttribute != null && subscriberAttribute.ToString() == name)
                            subscribers.Add(new Subscriber()
                            {
                                Name = subscriberAttribute.ToString(),
                                methodInfo = method,
                                Model = modelNode
                            });
                    }
                }

                return subscribers;
            }

            /// <summary>Creates and returns a delegate for the event handler.</summary>
            /// <param name="handlerType">The corresponding event publisher event handler type.</param>
            /// <returns>The delegate. Never returns null.</returns>
            internal virtual Delegate CreateDelegate(Type handlerType)
            {
                return Delegate.CreateDelegate(handlerType, Model, methodInfo);
            }

            /// <summary>
            /// Call the event handler.
            /// </summary>
            /// <param name="args"></param>
            internal void Invoke(object[] args)
            {
                methodInfo.Invoke(Model, args);
            }

        }

        /// <summary>
        /// A wrapper around an event publisher EventInfo.
        /// </summary>
        internal class Publisher
        {
            /// <summary>The model instance containing the event hander.</summary>
            public object Model { get; set; }

            /// <summary>The reflection event info instance.</summary>
            private EventInfo eventInfo;

            /// <summary>Return the event name.</summary>
            public string Name {  get { return eventInfo.Name; } }

            internal void ConnectSubscriber(Subscriber subscriber)
            {
                // connect subscriber to the event.
                Delegate eventDelegate = subscriber.CreateDelegate(eventInfo.EventHandlerType);
                eventInfo.AddEventHandler(Model, eventDelegate);
            }

            internal void DisconnectAll()
            {
                FieldInfo eventAsField = Model.GetType().GetField(Name, BindingFlags.Instance | BindingFlags.NonPublic);
                eventAsField.SetValue(Model, null);
            }

            /// <summary>Find all event publishers in the specified models.</summary>
            /// <param name="models">The models to scan for event publishers</param>
            /// <returns>The list of event publishers</returns>
            internal static List<Publisher> FindAll(List<IModel> models)
            {
                List<Publisher> publishers = new List<Publisher>();
                foreach (IModel modelNode in models)
                {
                    foreach (EventInfo eventInfo in modelNode.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                        publishers.Add(new Publisher() { eventInfo = eventInfo, Model = modelNode });
                }

                return publishers;
            }
        }

    }
}
