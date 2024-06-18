using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace Models.Core
{
    /// <summary>
    /// An event handling class
    /// </summary>
    public class Events : IEvent
    {
        /// <summary>A cache of event handlers for model types</summary>
        private Dictionary<Type, List<(MethodInfo, string)>> cache = new();

        private IModel relativeTo;
        private ScopingRules scope = new ScopingRules();

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">The model this events instance is relative to</param>
        public Events(IModel relativeTo)
        {
            this.relativeTo = relativeTo;
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        public void ConnectEvents()
        {
            // Get a list of all models that need to have event subscriptions resolved in.
            var modelsToInspectForSubscribers = new List<IModel>();
            modelsToInspectForSubscribers.Add(relativeTo);
            modelsToInspectForSubscribers.AddRange(relativeTo.FindAllDescendants());

            // Get a list of models in scope that publish events.
            var modelsToInspectForPublishers = scope.FindAll(relativeTo).ToList();

            // Get a complete list of all models in scope
            var publishers = Publisher.FindAll(modelsToInspectForPublishers);
            var subscribers = GetAllSubscribers(modelsToInspectForSubscribers);

            foreach (Publisher publisher in publishers)
                if (subscribers.ContainsKey(publisher.Name))
                    foreach (var subscriber in subscribers[publisher.Name])
                        if (scope.InScopeOf(subscriber.Model, publisher.Model))
                            publisher.ConnectSubscriber(subscriber);
        }

        /// <inheritdoc/>
        public void ReconnectEvents(string publisherName = null, string eventName = null)
        {
            // disconnect named events
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(relativeTo.FindAllDescendants());
            List<Publisher> publishers = Publisher.FindAll(allModels).Where(a => a.Model.GetType().FullName.Contains(publisherName ?? "") && a.EventInfo.Name.Contains(eventName ?? "")).ToList();
            foreach (Events.Publisher publisher in publishers)
                publisher.DisconnectAll();

            var subscribers = GetAllSubscribers(allModels);

            foreach (Publisher publisher in publishers)
                if (subscribers.ContainsKey(publisher.Name))
                    foreach (var subscriber in subscribers[publisher.Name])
                        if (scope.InScopeOf(subscriber.Model, publisher.Model))
                            publisher.ConnectSubscriber(subscriber);
        }

        /// <summary>Disconnect all events in the specified simulation.</summary>
        public void DisconnectEvents()
        {
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(relativeTo.FindAllDescendants());
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
            object component = relativeTo.FindByPath(componentName)?.Value;
            if (component == null)
                throw new Exception(relativeTo.FullPath + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Subscribe to the event.
            Delegate target = Delegate.CreateDelegate(componentEvent.EventHandlerType, handler.Target, handler.Method);
            componentEvent.AddEventHandler(component, target);
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
            object component = relativeTo.FindByPath(componentName)?.Value;
            if (component == null)
                throw new Exception(relativeTo.FullPath + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Unsubscribe to the event.
            Delegate target = Delegate.CreateDelegate(componentEvent.EventHandlerType, handler.Target, handler.Method);
            componentEvent.RemoveEventHandler(component, target);
        }

        /// <summary>
        /// Publish the specified event to the specified model and all models in scope.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments. Can be null</param>
        public void Publish(string eventName, object[] args)
        {
            List<Subscriber> subscribers = FindAllSubscribers(eventName, relativeTo, scope);

            foreach (Subscriber subscriber in subscribers)
            {
                try
                {
                    subscriber.Invoke(args);
                }
                catch (Exception err)
                {
                    throw new Exception($"Failed to publish event {eventName}. Error from subscriber {subscriber.Name}.{subscriber.MethodName}", err);
                }
            }
        }

        /// <summary>
        /// Publish the specified event to the specified model and all child models.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments. Can be null</param>
        public void PublishToModelAndChildren(string eventName, object[] args)
        {
            var modelsToInspectForSubscribers = new List<IModel>();
            modelsToInspectForSubscribers.Add(relativeTo);
            modelsToInspectForSubscribers.AddRange(relativeTo.FindAllDescendants());

            var subscribers = GetAllSubscribers(modelsToInspectForSubscribers);

            foreach (var subscriber in subscribers.Where(sub => sub.Key == eventName))
                foreach (var subscriberMethod in subscriber.Value)
                    subscriberMethod.Invoke(args);
        }

        private Dictionary<string, List<Subscriber>> GetAllSubscribers(List<IModel> allModels)
        {
            Dictionary<string, List<Subscriber>> subscribers = new Dictionary<string, List<Subscriber>>();

            foreach (IModel modelNode in allModels)
            {
                List<(MethodInfo, string)> eventHandlers = GetEventHandlersForModel(modelNode);

                foreach (var method in eventHandlers)
                {
                    string eventName = method.Item2;
                    Subscriber subscriber = new Subscriber(eventName, modelNode, method: method.Item1);

                    if (!subscribers.ContainsKey(eventName))
                        subscribers.Add(eventName, new List<Subscriber>());
                    subscribers[eventName].Add(subscriber);
                }
            }

            return subscribers;
        }

        private List<(MethodInfo, string)> GetEventHandlersForModel(IModel modelNode)
        {
            if (!cache.TryGetValue(modelNode.GetType(), out List<(MethodInfo, string)> eventHandlers))
            {
                eventHandlers = new();
                foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    EventSubscribeAttribute attribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                    if (attribute != null)
                    {
                        string eventName = attribute.ToString();
                        eventHandlers.Add((method, eventName));
                    }
                }
                cache.Add(modelNode.GetType(), eventHandlers);
            }

            return eventHandlers;
        }

        private Dictionary<string, List<Subscriber>> GetAllSubscribers(string name, IModel relativeTo, ScopingRules scope)
        {
            IEnumerable<IModel> allModels = scope.FindAll(relativeTo);
            Dictionary<string, List<Subscriber>> subscribers = new Dictionary<string, List<Subscriber>>();

            foreach (IModel modelNode in allModels)
            {
                List<(MethodInfo, string)> eventHandlers = GetEventHandlersForModel(modelNode);
                foreach (var method in eventHandlers)
                {
                    string eventName = method.Item2;

                    if (!eventName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    Subscriber subscriber = new Subscriber(eventName, modelNode, method.Item1);

                    if (subscribers[eventName] == null)
                        subscribers[eventName] = new List<Subscriber>();
                    subscribers[eventName].Add(subscriber);
                }
            }

            return subscribers;
        }

        /// <summary>Find all event subscribers in the specified models.</summary>
        /// <param name="allModels">A list of all models in simulation.</param>
        /// <returns>The list of event subscribers</returns>
        private List<Subscriber> FindAllSubscribers(List<IModel> allModels)
        {
            List<Subscriber> subscribers = new List<Subscriber>();
            foreach (IModel modelNode in allModels)
            {
                List<(MethodInfo, string)> eventHandlers = GetEventHandlersForModel(modelNode);
                foreach (var method in eventHandlers)
                {
                    string eventName = method.Item2;
                    subscribers.Add(new Subscriber(eventName, modelNode, method.Item1));
                }
            }

            return subscribers;
        }

        /// <summary>Find all event subscribers in the specified models.</summary>
        /// <param name="name">The name of the event to look for</param>
        /// <param name="relativeTo">The model to use in scoping lookup</param>
        /// <param name="scope">Scoping rules</param>
        /// <returns>The list of event subscribers</returns>
        private List<Subscriber> FindAllSubscribers(string name, IModel relativeTo, ScopingRules scope)
        {
            List<Subscriber> subscribers = new List<Subscriber>();
            foreach (IModel modelNode in scope.FindAll(relativeTo))
            {
                List<(MethodInfo, string)> eventHandlers = GetEventHandlersForModel(modelNode);
                foreach (var method in eventHandlers)
                {
                    string eventName = method.Item2;
                    if (eventName == name)
                        subscribers.Add(new Subscriber(eventName, modelNode, method.Item1));
                }
            }

            return subscribers;
        }

        /// <summary>
        /// A wrapper around an event publisher EventInfo.
        /// </summary>
        public class Publisher
        {
            /// <summary>The model instance containing the event hander.</summary>
            public IModel Model { get; private set; }

            /// <summary>The reflection event info instance.</summary>
            public EventInfo EventInfo { get; private set; }

            /// <summary>Return the event name.</summary>
            public string Name { get { return EventInfo.Name; } }

            internal void ConnectSubscriber(Subscriber subscriber)
            {
                // connect subscriber to the event.
                try
                {
                    Delegate eventDelegate = subscriber.CreateDelegate(EventInfo.EventHandlerType);
                    EventInfo.AddEventHandler(Model, eventDelegate);
                }
                catch (Exception err)
                {
                    throw new Exception($"Unable to connect event handler function {subscriber.MethodName} in model {subscriber.Model.FullPath} to event {subscriber.Name}", err);
                }
            }

            internal void DisconnectAll()
            {
                FieldInfo eventAsField = Model.GetType().GetField(Name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (eventAsField == null)
                {
                    //GetField will not find the EventHandler on a DerivedClass as the delegate is private
                    Type searchType = Model.GetType().BaseType;
                    while (eventAsField == null)
                    {
                        eventAsField = searchType?.GetField(Name, BindingFlags.Instance | BindingFlags.NonPublic);
                        searchType = searchType.BaseType;
                        if (searchType == null)
                        {
                            //not sure it's even possible to get to here, but it will make it easier to find itf it does
                            throw new Exception("Could not find " + Name + " in " + Model.GetType().Name + " using GetField");
                        }

                    }
                }
                eventAsField.SetValue(Model, null);
            }

            /// <summary>Find all event publishers in the specified models.</summary>
            /// <param name="models">The models to scan for event publishers</param>
            /// <returns>The list of event publishers</returns>
            public static List<Publisher> FindAll(IEnumerable<IModel> models)
            {
                List<Publisher> publishers = new List<Publisher>();
                foreach (IModel modelNode in models)
                {
                    foreach (EventInfo eventInfo in modelNode.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                        publishers.Add(new Publisher() { EventInfo = eventInfo, Model = modelNode });
                }

                return publishers;
            }
        }

        /// <summary>A wrapper around an event subscriber MethodInfo.</summary>
        internal class Subscriber
            {
                /// <summary>The model instance containing the event hander.</summary>
                public IModel Model { get; set; }

                /// <summary>The method info for the event handler.</summary>
                private MethodInfo methodInfo { get; set; }

                /// <summary>Gets or sets the name of the event.</summary>
                public string Name { get; private set; }

                /// <summary>Name of the target method.</summary>
                public string MethodName { get => methodInfo.Name; }

                public Subscriber(string name, IModel model, MethodInfo method)
                {
                    Name = name;
                    Model = model;
                    methodInfo = method;
                }

                /// <summary>Creates and returns a delegate for the event handler.</summary>
                /// <param name="handlerType">The corresponding event publisher event handler type.</param>
                /// <returns>The delegate. Never returns null.</returns>
                internal virtual Delegate CreateDelegate(Type handlerType)
                {
                    if (typeof(EventHandler).IsAssignableFrom(handlerType))
                    {
                        // We can give a specific error message for EventHandler delegate types.
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length != 2)
                            throw new Exception($"{methodInfo.Name} is a not a valid event handler: should have two arguments, but has {parameters.Length} arguments");
                        if (parameters[0].ParameterType != typeof(object))
                            throw new Exception($"{methodInfo.Name} is not a valid event handler: first argument should be of type object, but is {parameters[0].ParameterType}");
                        if (!typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType))
                            throw new Exception($"{methodInfo.Name} is not a valid event handler: second argument should be of type EventArgs, but is {parameters[1].ParameterType}");
                    }

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
    }
}

