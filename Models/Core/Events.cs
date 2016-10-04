
namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    class Events
    {
        private List<Events.Publisher> publishers = null;
        private List<Events.Subscriber> subscribers = null;
        private Scope scope = null;
        private Dictionary<IModel, List<Publisher>> cache = new Dictionary<IModel, List<Publisher>>();

        /// <summary>Connect all events in the specified simulation.</summary>
        /// <param name="model"></param>
        internal void ConnectEvents(IModel model)
        {
            // Get a complete list of all models in simulation (including the simulation itself).
            List<IModel> allModels = new List<IModel>();
            allModels.Add(model);
            allModels.AddRange(Apsim.ChildrenRecursively(model));

            scope = new Scope(allModels);

            if (publishers == null)
                publishers = Events.Publisher.FindAll(allModels);
            if (subscribers == null)
                subscribers = Events.Subscriber.FindAll(allModels);

            // Connect publishers to subscribers.
            foreach (Events.Subscriber subscriber in subscribers)
                ConnectSubscriber(subscriber, FilterPublishersInScope(subscriber));
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        /// <param name="model"></param>
        internal void DisconnectEvents(IModel model)
        {
            foreach (Events.Publisher publisher in publishers)
                publisher.DisconnectAll();
        }

        /// <summary>
        /// Scan a model and all child model for events and add the found publishers and subscribers
        /// to the list of known events and handlers.
        /// </summary>
        /// <param name="model">The model to scan</param>
        internal void AddModelEvents(IModel model)
        {
            // Get a complete list of all models in simulation (including the simulation itself).
            List<IModel> allModels = new List<IModel>();
            allModels.Add(model);
            allModels.AddRange(Apsim.ChildrenRecursively(model));
            if (publishers == null)
            {
                publishers = new List<Core.Events.Publisher>();
                subscribers = new List<Core.Events.Subscriber>();
            }
            publishers.AddRange(Events.Publisher.FindAll(allModels));
            subscribers.AddRange(Events.Subscriber.FindAll(allModels));
        }

        /// <summary>
        /// Remove a model and all child model events and handlers from the list.
        /// </summary>
        /// <param name="model">The model to scan</param>
        internal void RemoveModelEvents(IModel model)
        {
            // Get a complete list of all models in simulation (including the simulation itself).
            List<IModel> allModels = new List<IModel>();
            allModels.Add(model);
            allModels.AddRange(Apsim.ChildrenRecursively(model));

            publishers.RemoveAll(publisher => allModels.Contains(publisher.Model as IModel));
            subscribers.RemoveAll(subscriber => allModels.Contains(subscriber.Model as IModel));
        }

        /// <summary>
        /// Call the specified event on the specified model and all child models.
        /// </summary>
        /// <param name="model">The model to call the event on</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments. Can be null</param>
        internal void CallEventHandler(IModel model, string eventName, object[] args)
        {
            List<IModel> allModels = new List<IModel>();
            allModels.Add(model);
            allModels.AddRange(Apsim.ChildrenRecursively(model));

            List<Subscriber> matches = subscribers.FindAll(subscriber => subscriber.Name == eventName &&
                                                                         allModels.Contains(subscriber.Model as IModel));

            foreach (Subscriber subscriber in matches)
                subscriber.Invoke(args);
        }

        /// <summary>Connect the specified subscriber to the closest publisher.</summary>
        /// <param name="subscriber">Subscriber to connect.</param>
        /// <param name="publishers">All publishers</param>
        private static void ConnectSubscriber(Events.Subscriber subscriber, List<Events.Publisher> publishers)
        {
            // Find all publishers with the same name.
            List<Events.Publisher> matchingPublishers = publishers.FindAll(publisher => publisher.Name == subscriber.Name);

            // Connect subscriber to all matching publishers.
            matchingPublishers.ForEach(publisher => publisher.ConnectSubscriber(subscriber));
        }

        /// <summary>
        /// Return a list of publishers that are in scope.
        /// </summary>
        /// <param name="relativeTo">Modle to base scoping rules on.</param>
        private List<Publisher> FilterPublishersInScope(Subscriber relativeTo)
        {
            // Try cache
            List<Publisher> publishersInScope;
            if (cache.TryGetValue(relativeTo.Model as IModel, out publishersInScope))
                return publishersInScope;

            List<IModel> modelsInScope = scope.InScope(relativeTo.Model as IModel);
            publishersInScope = new List<Publisher>();
            publishersInScope = publishers.FindAll(publisher => modelsInScope.Contains(publisher.Model as IModel));
            cache.Add(relativeTo.Model as IModel, publishersInScope);
            return publishersInScope;
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

        internal class Scope
        {
            private Dictionary<IModel, List<IModel>> cache = new Dictionary<IModel, List<IModel>>();
            private List<IModel> allModels = new List<IModel>();

            /// <summary>Constructor</summary>
            internal Scope(List<IModel> allModelsInSimulation)
            {
                allModels = allModelsInSimulation;
            }

            /// <summary>
            /// Return a list of models in scope to the one specified.
            /// </summary>
            /// <param name="relativeTo">The model to base scoping rules on</param>
            internal List<IModel> InScope(IModel relativeTo)
            {
                // Try the cache first.
                List<IModel> modelsInScope;
                if (cache.TryGetValue(relativeTo, out modelsInScope))
                    return modelsInScope;


                // The algorithm is to find the parent Zone of the specified model.
                // Then return all children of this zone recursively and then recursively 
                // the direct children of the parents of the zone.
                IModel parentZone = FindScopedParentModel(relativeTo);
                if (parentZone == null)
                    throw new Exception("No scoping model found relative to: " + Apsim.FullPath(relativeTo));

                // return all models in zone and all direct children of zones parent.
                modelsInScope = new List<IModel>();
                modelsInScope.Add(parentZone);
                modelsInScope.AddRange(Apsim.ChildrenRecursively(parentZone));
                while (parentZone.Parent != null)
                {
                    parentZone = parentZone.Parent;
                    foreach (IModel child in parentZone.Children)
                    {
                        if (!modelsInScope.Contains(child))
                            modelsInScope.Add(child);
                    }
                }
                modelsInScope.Add(parentZone); // top level simulation

                // add to cache for next time.
                cache.Add(relativeTo, modelsInScope);
                return modelsInScope;
            }

            /// <summary>
            /// Find a parent of 'relativeTo' that has a [ScopedModel] attribute. 
            /// Returns null if non found.
            /// </summary>
            /// <param name="relativeTo">The model to use as a base.</param>
            private static IModel FindScopedParentModel(IModel relativeTo)
            {
                while (relativeTo.Parent != null)
                {
                    relativeTo = relativeTo.Parent;
                    ScopedModelAttribute attribute = relativeTo.GetType().GetCustomAttribute(typeof(ScopedModelAttribute), true) as ScopedModelAttribute;
                    if (attribute != null)
                        return relativeTo;
                }

                return null;
            }
        }
    }
}
