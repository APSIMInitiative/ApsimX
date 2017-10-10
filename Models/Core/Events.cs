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

            List<Events.Publisher> publishers = Events.Publisher.FindAll(allModels);
            List<Events.Subscriber> subscribers = Events.Subscriber.FindAll(allModels);

            // Connect publishers to subscribers.
            Dictionary<IModel, List<Subscriber>> cache = new Dictionary<IModel, List<Subscriber>>();
            foreach (Events.Publisher publisher in publishers)
                ConnectPublisherToScriber(publisher, FilterSubscribersInScope(publisher, cache, scope, subscribers));
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
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(Apsim.ChildrenRecursively(relativeTo));
            List<Events.Subscriber> subscribers = Events.Subscriber.FindAll(allModels);

            List<Subscriber> matches = subscribers.FindAll(subscriber => subscriber.Name == eventName &&
                                                                         allModels.Contains(subscriber.Model as IModel));

            foreach (Subscriber subscriber in matches)
                subscriber.Invoke(args);
        }

        /// <summary>Connect the specified publisher to all subscribers in scope</summary>
        /// <param name="publisher">Publisher to connect.</param>
        /// <param name="subscribers">All subscribers</param>
        private static void ConnectPublisherToScriber(Events.Publisher publisher, List<Events.Subscriber> subscribers)
        {
            // Find all publishers with the same name.
            List<Events.Subscriber> matchingSubscribers = subscribers.FindAll(subscriber => subscriber.Name == publisher.Name);

            // Connect subscriber to all matching publishers.
            matchingSubscribers.ForEach(subscriber => publisher.ConnectSubscriber(subscriber));
        }

        /// <summary>
        /// Return a list of subscribers that are in scope.
        /// </summary>
        /// <param name="relativeTo">Model to base scoping rules on.</param>
        /// <param name="cache">The model/scriber cache</param>
        /// <param name="scope">An instance of scoping rules</param>
        /// <param name="subscribers">A collection of all subscribers</param>
        private static List<Subscriber> FilterSubscribersInScope(Publisher relativeTo, 
                                                                 Dictionary<IModel, List<Subscriber>> cache, 
                                                                 ScopingRules scope,
                                                                 List<Events.Subscriber> subscribers)
        {
            // Try cache
            List<Subscriber> subscribersInScope;
            if (cache.TryGetValue(relativeTo.Model as IModel, out subscribersInScope))
                return subscribersInScope;

            List<IModel> modelsInScope = new List<IModel>(scope.FindAll(relativeTo.Model as IModel));
            subscribersInScope = new List<Subscriber>();
            subscribersInScope = subscribers.FindAll(subscriber => modelsInScope.Contains(subscriber.Model as IModel));
            cache.Add(relativeTo.Model as IModel, subscribersInScope);
            return subscribersInScope;
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
            /// <param name="models">The models to scan for event handlers.</param>
            /// <returns>The list of event subscribers</returns>
            internal static List<Subscriber> FindAll(List<IModel> models)
            {
                List<Subscriber> subscribers = new List<Subscriber>();
                foreach (IModel modelNode in models)
                {
                    foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                    {
                        EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                        if (subscriberAttribute != null)
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
                try
                {
                    methodInfo.Invoke(Model, args);
                }
                catch (Exception err)
                {
                    // The exception will be a "Exception thrown by the target of an invocation".
                    // Throw the inner exception as this will be the real exception.
                    throw err.InnerException;
                }
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
