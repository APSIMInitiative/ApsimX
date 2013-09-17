using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Model.Core;
using System.Xml.Schema;

namespace Model.Components
{

    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    public class Manager : Model.Core.Model, IXmlSerializable
    {
        // Privates
        private Assembly CompiledAssembly;

        // Links
        [Link]
        private Zone Zone = null;

        // Publics
        public object Model { get; set; }
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
            CompiledAssembly = CompileTextToAssembly();

            // Go look for our class name.
            Type ScriptType = CompiledAssembly.GetType("Model.Components.Script");
            if (ScriptType == null)
                throw new Exception("Cannot find a public class called Script");

            // Deserialise to a model.
            XmlSerializer serial = new XmlSerializer(ScriptType);
            Model = serial.Deserialize(reader);

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


        private Assembly CompileTextToAssembly()
        {
            bool VB = Code.IndexOf("Imports System") != -1;
            string Language;
            if (VB)
                Language = CodeDomProvider.GetLanguageFromExtension(".vb");
            else
                Language = CodeDomProvider.GetLanguageFromExtension(".cs");

            if (Language != null && CodeDomProvider.IsDefinedLanguage(Language))
            {
                CodeDomProvider Provider = CodeDomProvider.CreateProvider(Language);
                if (Provider != null)
                {
                    CompilerParameters Params = new CompilerParameters();
                    Params.GenerateInMemory = true;      //Assembly is created in memory
                    Params.TempFiles = new TempFileCollection(Path.GetTempPath(), false);
                    Params.TreatWarningsAsErrors = false;
                    Params.WarningLevel = 2;
                    Params.ReferencedAssemblies.Add("System.dll");
                    Params.ReferencedAssemblies.Add("System.Xml.dll");
                    Params.ReferencedAssemblies.Add(Path.Combine(Assembly.GetExecutingAssembly().Location));

                    Params.TempFiles = new TempFileCollection(".");
                    Params.TempFiles.KeepFiles = false;
                    string[] source = new string[1];
                    source[0] = Code;
                    CompilerResults results = Provider.CompileAssemblyFromSource(Params, source);
                    string Errors = "";
                    foreach (CompilerError err in results.Errors)
                    {
                        if (Errors != "")
                            Errors += "\r\n";

                        Errors += err.ErrorText + ". Line number: " + err.Line.ToString();
                    }
                    if (Errors != "")
                        throw new Exception(Errors);

                    return results.CompiledAssembly;
                }
            }
            throw new Exception("Cannot compile manager script to an assembly");
        }
    }


}