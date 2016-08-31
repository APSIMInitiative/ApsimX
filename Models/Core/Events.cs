
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

        /// <summary>Connect all events in the specified simulation.</summary>
        /// <param name="model"></param>
        internal void ConnectEvents(IModel model)
        {
            // Get a complete list of all models in simulation (including the simulation itself).
            List<IModel> allModels = new List<IModel>();
            allModels.Add(model);
            allModels.AddRange(Apsim.ChildrenRecursively(model));

            if (publishers == null)
                publishers = Events.Publisher.FindAll(allModels);
            if (subscribers == null)
                subscribers = Events.Subscriber.FindAll(allModels);

            // Connect publishers to subscribers.
            foreach (Events.Subscriber subscriber in subscribers)
                ConnectSubscriber(subscriber, publishers);
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        /// <param name="model"></param>
        internal void DisconnectEvents(IModel model)
        {
            foreach (Events.Publisher publisher in publishers)
                publisher.DisconnectAll();
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



        /// <summary>A wrapper around an event subscriber MethodInfo.</summary>
        internal class Subscriber
        {
            /// <summary>The model instance containing the event hander.</summary>
            private object model;

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
                                model = modelNode
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
                return Delegate.CreateDelegate(handlerType, model, methodInfo);
            }

        }

        /// <summary>
        /// A wrapper around an event publisher EventInfo.
        /// </summary>
        internal class Publisher
        {
            /// <summary>The model instance containing the event hander.</summary>
            private object model;

            /// <summary>The reflection event info instance.</summary>
            private EventInfo eventInfo;

            /// <summary>Return the event name.</summary>
            public string Name {  get { return eventInfo.Name; } }

            internal void ConnectSubscriber(Subscriber subscriber)
            {
                // connect subscriber to the event.
                Delegate eventDelegate = subscriber.CreateDelegate(eventInfo.EventHandlerType);
                eventInfo.AddEventHandler(model, eventDelegate);
            }

            internal void DisconnectAll()
            {
                FieldInfo eventAsField = model.GetType().GetField(Name, BindingFlags.Instance | BindingFlags.NonPublic);
                eventAsField.SetValue(model, null);
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
                        publishers.Add(new Publisher() { eventInfo = eventInfo, model = modelNode });
                }

                return publishers;
            }
        }
    }
}
