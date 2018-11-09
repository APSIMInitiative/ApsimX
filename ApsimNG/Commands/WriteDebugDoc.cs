

namespace UserInterface.Commands
{
    using Models.Core;
    using System.IO;
    using System;
    using APSIM.Shared.Utilities;
    using System.Reflection;
    using System.Data;
    using System.Xml;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Presenters;
    using System.Xml.Xsl;
    using System.Diagnostics;

    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    public class WriteDebugDoc : ICommand
    {
        /// <summary>The main form.</summary>
        ExplorerPresenter explorerPresenter;

        /// <summary>Simulation to document.</summary>
        Simulation simulation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportNodeCommand"/> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter</param>
        /// <param name="simulation">The simulation to document</param>
        public WriteDebugDoc(ExplorerPresenter explorerPresenter, Simulation simulation)
        {
            this.explorerPresenter = explorerPresenter;
            this.simulation = simulation;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            Simulation clonedSimulation = null;
            IEvent events = null;
            try
            {
                List<Simulation> sims = new List<Models.Core.Simulation>();
                clonedSimulation = Apsim.Clone(simulation) as Simulation;
                sims.Add(clonedSimulation);
                explorerPresenter.ApsimXFile.MakeSubsAndLoad(clonedSimulation);

                events = explorerPresenter.ApsimXFile.GetEventService(clonedSimulation);
                events.ConnectEvents();
                explorerPresenter.ApsimXFile.Links.Resolve(clonedSimulation);

                List<ModelDoc> models = new List<ModelDoc>();
                foreach (IModel model in Apsim.ChildrenRecursively(clonedSimulation))
                {
                    ModelDoc newModelDoc = DocumentModel(model);
                    newModelDoc.Name = Apsim.FullPath(model);
                    models.Add(newModelDoc);
                }

                StringWriter rawXML = new StringWriter();
                XmlSerializer serialiser = new XmlSerializer(typeof(List<ModelDoc>));
                serialiser.Serialize(rawXML, models);
                rawXML.Close();

                // Load the XSL transform from the resource
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.DebugDoc.xsl");
                var transform = new XslCompiledTransform();
                using (XmlReader reader = XmlReader.Create(s))
                {
                    transform.Load(reader);
                }

                // Apply the transform to the reader and write it to a temporary file.
                string tempFileName = Path.GetTempFileName();
                File.Delete(tempFileName);
                string htmlFileName = Path.ChangeExtension(tempFileName, ".html");
                using (XmlReader reader = XmlReader.Create(new StringReader(rawXML.ToString())))
                    using (XmlWriter htmlWriter = XmlWriter.Create(htmlFileName))
                    {
                        transform.Transform(reader, htmlWriter);
                    }
                Process.Start(htmlFileName);
            }
            finally
            {
                if (clonedSimulation != null)
                {
                    events.DisconnectEvents();
                    explorerPresenter.ApsimXFile.Links.Unresolve(clonedSimulation, allLinks:true);
                }
            }
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="model">The model to document.</param>
        private ModelDoc DocumentModel(IModel model)
        {
            ModelDoc doc = new ModelDoc();

            foreach (FieldInfo field in model.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                                                  System.Reflection.BindingFlags.NonPublic |
                                                                  System.Reflection.BindingFlags.Instance |
                                                                  System.Reflection.BindingFlags.FlattenHierarchy))
            {
                if (field.GetCustomAttribute(typeof(LinkAttribute)) != null)
                    doc.Links.Add(DocumentLink(field, model));
                else if (field.IsPublic)
                    doc.Outputs.Add(DocumentOutput(field, field.FieldType, true));
            }
            
            foreach (PropertyInfo property in model.GetType().GetProperties(System.Reflection.BindingFlags.Public |
                                                                            System.Reflection.BindingFlags.Instance |
                                                                            System.Reflection.BindingFlags.FlattenHierarchy))
                doc.Outputs.Add(DocumentOutput(property, property.PropertyType, property.CanWrite));

            foreach (EventInfo eventMember in model.GetType().GetEvents(System.Reflection.BindingFlags.Public |
                                                                        System.Reflection.BindingFlags.Instance |
                                                                        System.Reflection.BindingFlags.FlattenHierarchy))
                doc.Events.Add(DocumentEvent(eventMember, model));

            return doc;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="member">The member</param>
        /// <param name="memberType">The name of the type.</param>
        /// <param name="writable">Is writable?</param>
        private static ModelDoc.Output DocumentOutput(MemberInfo member, Type memberType, bool writable)
        {
            ModelDoc.Output output = new ModelDoc.Output();
            output.Name = member.Name;
            if (memberType.IsGenericType && memberType.GetInterface("IList") != null)
                output.TypeName = "List<" + memberType.GenericTypeArguments[0].Name + ">";
            else
                output.TypeName = memberType.Name;
            UnitsAttribute units = member.GetCustomAttribute<UnitsAttribute>();
            if (units != null)
                output.Units = units.ToString();
            DescriptionAttribute description = member.GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
                output.Description = description.ToString();
            output.IsWritable = writable;
            output.IsField = member is FieldInfo;
            return output;
        }

        /// <summary>
        /// Create and return a new Link object for member
        /// </summary>
        /// <param name="field">The member</param>
        /// <param name="model">Model with the link</param>
        private static ModelDoc.Link DocumentLink(FieldInfo field, IModel model)
        {
            ModelDoc.Link link = new ModelDoc.Link();
            link.Name = field.Name;
            if (field.FieldType.IsGenericType && field.FieldType.GetInterface("IList") != null)
                link.TypeName = "List<" + field.FieldType.GenericTypeArguments[0].Name + ">";
            else
                link.TypeName = field.FieldType.Name;
            UnitsAttribute units = field.GetCustomAttribute<UnitsAttribute>();
            if (units != null)
                link.Units = units.ToString();
            DescriptionAttribute description = field.GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
                link.Description = description.ToString();

            LinkAttribute linkAtt = field.GetCustomAttribute<LinkAttribute>();
            link.IsOptional = linkAtt.IsOptional;

            object linkedObject = field.GetValue(model);
            if (linkedObject != null)
            {
                if (linkedObject is IModel)
                    link.LinkedModelName = Apsim.FullPath(linkedObject as IModel);
                else
                {
                }
            }

            return link;
        }

        /// <summary>
        /// Create and return a new Event object for member
        /// </summary>
        /// <param name="eventMember">The member</param>
        /// <param name="model">Model with the link</param>
        private static ModelDoc.Event DocumentEvent(EventInfo eventMember, IModel model)
        {
            ModelDoc.Event newEvent = new ModelDoc.Event();
            newEvent.Name = eventMember.Name;
            newEvent.TypeName = eventMember.EventHandlerType.Name;

            FieldInfo eventAsField = FindEventField(eventMember.Name, model);
            Delegate eventDelegate = eventAsField.GetValue(model) as Delegate;
            if (eventDelegate != null)
            {
                foreach (Delegate del in eventDelegate.GetInvocationList())
                {
                    IModel subscriberModel = del.Target as IModel;
                    if (subscriberModel != null)
                    newEvent.SubscriberNames.Add(Apsim.FullPath(subscriberModel));
                }
            }

            return newEvent;
        }


        /// <summary>
        /// Locate and return the event backing field for the specified event. Returns
        /// null if not found.
        /// </summary>
        /// <param name="eventName">The event publisher to find an event declaration for</param>
        /// <param name="publisherModel">The model containing the event.</param>
        /// <returns>The event field declaration</returns>
        private static FieldInfo FindEventField(string eventName, IModel publisherModel)
        {
            Type t = publisherModel.GetType();
            FieldInfo eventAsField = t.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            while (eventAsField == null && t.BaseType != typeof(object))
            {
                t = t.BaseType;
                eventAsField = t.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return eventAsField;
        }


        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {

        }
        

        public class ModelDoc
        {
            public string Name;
            public List<Output> Outputs = new List<Output>();
            public List<Link> Links = new List<Link>();
            public List<Event> Events = new List<Event>();


            public class Output
            {
                public string Name;
                public string TypeName;
                public string Units;
                public string Description;
                public bool IsWritable;
                public bool IsField;
            }

            public class Link
            {
                public string Name;
                public string TypeName;
                public string Units;
                public string Description;
                public string LinkedModelName;
                public bool IsOptional;
            }

            public class Event
            {
                public string Name;
                public string TypeName;
                [XmlElement("SubscriberName")]
                public List<string> SubscriberNames = new List<string>();
            }

        }

    }
}

