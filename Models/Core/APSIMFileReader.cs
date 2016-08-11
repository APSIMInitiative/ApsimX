using APSIM.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Models.Core
{
    /// <summary>
    /// Converts a .apsim file format into something that the XmlSerializer can read
    /// into a tree of ModelWrapper objects.
    /// </summary>
    class APSIMFileReader : XmlReaderCustom
    {
        /// <summary>The possible states is state automaton</summary>
        private enum States { Initial, ModelNode, NewModel, Model, Child}

        /// <summary>The current state.</summary>
        private States currentState;

        /// <summary>The model name</summary>
        private string modelName;

    //    /// <summary>The version number of the file.</summary>
    //    private string version;

        /// <summary>The reader we're to read from.</summary>
        XmlNodeReader reader = null;

        /// <summary>Constructor.</summary>
        /// <param name="node"></param>
        public APSIMFileReader(XmlNode node)
        { 
            reader = new XmlNodeReader(node);
            currentState = States.Initial;
        }

        /// <summary>Gets the next element.</summary>
        /// <returns>The element or null if an end element.</returns>
        protected override CustomElement GetNextElement()
        {
            // Need to go from this:
            //    
            //    <Simulations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            //      <ExplorerWidth>10</ExplorerWidth>
            //      <Simulation>
            //        <Clock>
            //          <StartDate>0001-01-01T00:00:00</StartDate>
            //          <EndDate>0001-01-01T00:00:00</EndDate>
            //        </Clock>
            //      </Simulation>
            //    </Simulations>
            // to this:
            //    <ModelWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            //       <Model xsi:type=Simulations>
            //           <ExplorerWidth>10</ExplorerWidth>
            //       </Model>
            //       <Child xsi:type="ModelWrapper">
            //           <Model xsi:type="Simulation">
            //           </Model>
            //           <Child xsi:type="ModelWrapper">
            //               <Model xsi:type="Clock">
            //                   <StartDate>0001-01-01T00:00:00</StartDate>
            //                   <EndDate>0001-01-01T00:00:00</EndDate>
            //               </Model>
            //           </Child>
            //       </Child>
            //    </ModelWrapper>

            bool ok = false;
            CustomElement element = null;


            while (!ok)
            {
                ok = true;
                switch (currentState)
                {
                    case States.Initial:
                        ok = reader.Read();
                        if (!ok) return null;
                        currentState = States.ModelNode;
                        element = new CustomElement() { Name = "ModelWrapper" };
                        modelName = reader.Name;

                        while (reader.MoveToNextAttribute())
                            element.attributes.Add(new KeyValuePair<string, string>(reader.Name, reader.Value));
                        reader.MoveToElement();
                        break;

                    case States.ModelNode:
                        ok = true;
                        ok = reader.Read();
                        if (!ok) return null;
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            // This will be the endelement of name. Use it to switch states.
                            currentState = States.NewModel;
                        }
                        else
                            element = new CustomElement() { Name = reader.Name, Value = reader.Value };
                        break;

                    case States.NewModel:
                        element = new CustomElement() { Name = "Model" };
                        element.attributes.Add(new KeyValuePair<string, string>("xsi:type", modelName));
                        currentState = States.Model;
                        break;

                    case States.Model:
                        ok = reader.Read();
                        if (!ok) return null;
                        if (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
                        {
                            if (reader.Name == "Simulations" || reader.Name == "Simulation" || reader.Name == "Clock" || reader.Name == "Zone")
                                currentState = States.Child;
                            else
                                element = new CustomElement() { Name = reader.Name, Value = reader.Value };
                        }
                        break;

                    case States.Child:
                        currentState = States.ModelNode;
                        element = new CustomElement() { Name = "Child" };
                        element.attributes.Add(new KeyValuePair<string, string>("xsi:type", "ModelWrapper"));
                        currentState = States.ModelNode;
                        modelName = reader.Name;
                        break;
                }
            }

            return element;
        }

        /// <summary>Resolves a namespace prefix in the current element's scope.</summary>
        /// <param name="prefix">The Prefix</param>
        /// <returns></returns>
        public override string LookupNamespace(string prefix)
        {
            return reader.LookupNamespace(prefix);
        }

        /// <summary>Gets the namespace URI</summary>
        public override string NamespaceURI
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>Returns the name table.</summary>
        public override XmlNameTable NameTable
        {
            get
            {
                return reader.NameTable;
            }
        }
    }
}
