using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Reflection;
using System.Collections;
namespace Utility
{


    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ModelFunctions
    {
        #region Event functions
        private class EventSubscriber
        {
            public Model Model;
            public MethodInfo MethodInfo;
            public string Name
            {
                get
                {
                    EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(MethodInfo, typeof(EventSubscribe), false);
                    return subscriberAttribute.Name;
                }
            }
        }
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
        /// Connect all events in all models that are in scope of 'model'
        /// </summary>
        public static void ConnectEventsInAllModels(Model model)
        {
            Model[] modelsInScope = model.FindAll();

            foreach (EventPublisher publisher in FindEventPublishers(null, modelsInScope))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
                    publisher.AddEventHandler(publisher.Model, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Disconnect all events in all models that are in scope of 'model'
        /// </summary>
        public static void DisconnectEventsInAllModels(Model model)
        {
            Model[] modelsInScope = model.FindAll();

            foreach (EventPublisher publisher in FindEventPublishers(null, modelsInScope))
                DisconnectEventPublisher(publisher, null);
        }


        /// <summary>
        /// Connect all events up in the specified model.
        /// </summary>
        public static void ConnectEventsInModel(Model model)
        {
            Model[] modelsInScope = model.FindAll();

            // Go through all events in the specified model and attach them to subscribers.
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
                    publisher.AddEventHandler(model, eventdelegate);
                }
            }

            // Go through all subscribers in the specified model and find the event publisher to connect to.
            foreach (EventSubscriber subscriber in FindEventSubscribers(null, model))
            {
                foreach (EventPublisher publisher in FindEventPublishers(subscriber.Name, modelsInScope))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
                    publisher.AddEventHandler(publisher.Model, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Disconnect the specified model from all events.
        /// </summary>
        /// <param name="model"></param>
        public static void DisconnectEventsInModel(Model model)
        {
            Model[] modelsInScope = model.FindAll();

            // Go through all events in the specified model and detach them from subscribers.
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
                DisconnectEventPublisher(publisher, null);

            // Go through all subscribers in the specified model and find the event publisher to detach from.
            foreach (EventSubscriber subscriber in FindEventSubscribers(null, model))
                foreach (EventPublisher publisher in FindEventPublishers(subscriber.Name, modelsInScope))
                    DisconnectEventPublisher(publisher, model);
        }

        /// <summary>
        /// Clear all subscriptions from the specified event publisher if 'model' is null or
        /// those subscriptions where the subscriber is 'model'
        /// </summary>
        private static void DisconnectEventPublisher(EventPublisher publisher, Model model)
        {
            FieldInfo eventAsField = publisher.Model.GetType().GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
            if (eventDelegate != null)
            {
                foreach (Delegate del in eventDelegate.GetInvocationList())
                {
                    if (model == null || del.Target == model)
                        publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
                }
            }
        }

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private static List<EventSubscriber> FindEventSubscribers(EventPublisher publisher)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in publisher.Model.FindAll())
                subscribers.AddRange(FindEventSubscribers(publisher.Name, model));
            return subscribers;

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
                EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(method, typeof(EventSubscribe), false);
                if (subscriberAttribute != null && (eventName == null || subscriberAttribute.Name == eventName))
                    subscribers.Add(new EventSubscriber() { MethodInfo = method, Model = model });
            }
            return subscribers;
        }

        /// <summary>
        /// Look through and return all models in scope for event publishers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private static List<EventPublisher> FindEventPublishers(string eventName, Model[] modelsInScope)
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

        #endregion

        #region Link functions
        /// <summary>
        /// Recursively resolve all [Link] fields. A link must be private. This method will also
        /// go through any public members that are a model or a list of models. For each one found
        /// it will recursively call this method to resolve links in them. This is an internal
        /// method that won't normally be called by models.
        /// </summary>
        public static void ResolveLinks(Model model)
        {
            // Go looking for private [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                Link link = Utility.Reflection.GetAttribute(field, typeof(Link), false) as Link;
                if (link != null)
                {
                    object linkedObject;
                    if (link.NamePath != null)
                        linkedObject= model.Find(link.NamePath);
                    else
                        linkedObject = model.Find(field.FieldType);
                    if (linkedObject != null)
                        field.SetValue(model, linkedObject);
                    else
                        throw new ApsimXException(model.FullPath, "Cannot resolve [Link] '" + field.ToString() + "' in class '" + model.GetType().FullName + "'");
                }
            }

            foreach (Model child in model.Models)
            {
                // Set the childs parent property.
                child.Parent = model;

                // Tell child to resolve its links.
                ResolveLinks(child);
            }
        }
        #endregion

        #region Initialise functions

        public struct LoadError
        {
            public string FullPath;
            public string Message;
        }

        /// <summary>
        /// Call OnLoaded in the specified model and all child models.
        /// </summary>
        public static void CallOnLoaded(Model model)
        {
            model.OnLoaded();
            foreach (Model child in model.Models)
                CallOnLoaded(child);
        }

        /// <summary>
        /// Call OnCommencing in the specified model and all child models.
        /// </summary>
        public static void CallOnCommencing(Model model)
        {
            model.OnCommencing();
            foreach (Model child in model.Models)
                CallOnCommencing(child);        
        }

        /// <summary>
        /// Call OnCompleted in the specified model and all child models.
        /// </summary>
        public static void CallOnCompleted(Model model)
        {
            model.OnCompleted();
            foreach (Model child in model.Models)
                CallOnCompleted(child);
        }
        #endregion

        #region Parameter functions

        /// <summary>
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/writtable
        /// </summary>
        public static IVariable[] Parameters(object model)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                if (property.CanRead && property.CanWrite)
                {
                    Attribute XmlIgnore = Utility.Reflection.GetAttribute(property, typeof(System.Xml.Serialization.XmlIgnoreAttribute), true);

                    bool ignoreProperty = XmlIgnore != null;                                 // No [XmlIgnore]
                    ignoreProperty |= property.PropertyType.GetInterface("IList") != null;   // No List<T>
                    ignoreProperty |= property.PropertyType.IsSubclassOf(typeof(Model));     // Nothing derived from Model.
                    ignoreProperty |= property.Name == "Name";                               // No Name properties.

                    if (!ignoreProperty)
                        allProperties.Add(new VariableProperty(model, property));
                }
            }
            return allProperties.ToArray();
        }

        /// <summary>
        /// Return a complete list of state variables (public and private) for the specified model.
        /// </summary>
        public static IVariable[] States(object model)
        {
            List<IVariable> variables = new List<IVariable>();
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                if (field.DeclaringType != typeof(Model) && field.FieldType != typeof(Model) && !field.FieldType.IsSubclassOf(typeof(Model)) &&
                    !field.FieldType.Name.Contains("Delegate"))
                    variables.Add(new VariableField(model, field));
            }

            return variables.ToArray();
        }

        /// <summary>
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/writtable
        /// </summary>
        public static IVariable[] FieldsAndProperties(object model, BindingFlags flags)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().UnderlyingSystemType.GetProperties(flags))
            {
                if (property.CanRead)
                    allProperties.Add(new VariableProperty(model, property));
            }
            foreach (FieldInfo field in model.GetType().UnderlyingSystemType.GetFields(flags))
                    allProperties.Add(new VariableField(model, field));
            return allProperties.ToArray();
        }

      

        #endregion
    }
}
