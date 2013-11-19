using System;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using Models.Core;
using System.Xml.Schema;

namespace Models
{

    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    public class Manager : Model, IXmlSerializable
    {
        // Privates
        private Assembly CompiledAssembly;

        // Links
        [Link]
        private Zone Zone = null;

        // Publics
        public Model Model { get; set; }
        public string Code { get; set; }
        public Zone ParentZone { get { return Zone; } }

        #region XmlSerializable methods
        /// <summary>
        /// Return our schema - needed for IXmlSerializable.
        /// </summary>
        public XmlSchema GetSchema() { return null; }

        /// <summary>
        /// Read XML from specified reader. Called during Deserialisation.
        /// </summary>
        public virtual void ReadXml(XmlReader reader)
        {
            reader.Read();
            Name = reader.ReadString();
            reader.Read();
            Code = reader.ReadString();
            reader.Read();
            CompiledAssembly = Utility.Reflection.CompileTextToAssembly(Code);

            // Go look for our class name.
            Type ScriptType = CompiledAssembly.GetType("Models.Script");
            if (ScriptType == null)
                throw new Exception("Cannot find a public class called Script");

            // Deserialise to a model.
            XmlSerializer serial = new XmlSerializer(ScriptType);
            Model = serial.Deserialize(reader) as Model;
            
            // Tell reader we're done with the Manager deserialisation.
            reader.ReadEndElement();
        }

        /// <summary>
        /// Write this point to the specified XmlWriter
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Name");
            writer.WriteString(Name);
            writer.WriteEndElement();
            writer.WriteStartElement("Code");
            writer.WriteString(Code);
            writer.WriteEndElement();

            // Serialise the model.
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer serial = new XmlSerializer(Model.GetType());
            serial.Serialize(writer, Model, ns);
        }

        #endregion
      
    }
}