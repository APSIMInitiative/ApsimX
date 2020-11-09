namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// This class is a reader for a .apsimx file format into something that the XmlSerializer can read.
    /// It uses a state machine to parse the XML on the reader passed into the constructor and presents
    /// XML elements to whatever consumes an instance of this class. In essence it translates the XML
    /// into something that the .NET serialisation engine can work with.
    /// </summary>
    /// <remarks>
    /// Converts:
    ///    
    ///    <Simulations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    ///      <ExplorerWidth>10</ExplorerWidth>
    ///      <Simulation>
    ///        <Clock>
    ///          <StartDate>0001-01-01T00:00:00</StartDate>
    ///          <EndDate>0001-01-01T00:00:00</EndDate>
    ///        </Clock>
    ///      </Simulation>
    ///    </Simulations>
    /// to this:
    ///    <ModelWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    ///       <Model xsi:type="Simulations">
    ///           <ExplorerWidth>10</ExplorerWidth>
    ///       </Model>
    ///       <Child xsi:type="ModelWrapper">
    ///           <Model xsi:type="Simulation">
    ///           </Model>
    ///           <Child xsi:type="ModelWrapper">
    ///               <Model xsi:type="Clock">
    ///                   <StartDate>0001-01-01T00:00:00</StartDate>
    ///                   <EndDate>0001-01-01T00:00:00</EndDate>
    ///               </Model>
    ///           </Child>
    ///       </Child>
    ///    </ModelWrapper>
    /// </remarks>
    public class Reader : XmlReaderCustom
    {
        /// <summary>The possible parsing states</summary>
        private enum States { Initial, ExpectingChildOrModel, ExpectingModelProperties, ParsingArrayParameter, End }

        /// <summary>The current state.</summary>
        private States currentState;

        /// <summary>An list of valid model types.</summary>
        private List<Type> validModelTypes;

        /// <summary>A stack of open xml elements.</summary>
        private Stack<string> openElements = new Stack<string>();

        /// <summary>A stack of model types that we have encourntered so far.</summary>
        private Stack<string> modelTypesFound = new Stack<string>();

        /// <summary>The reader we're to read from.</summary>
        private ReadWithLookAhead reader = null;

        private string prefix = string.Empty;
        private string namespaceURI = string.Empty;

        /// <summary>A counter of the number of open structures.</summary>
        private int structureCounter = 0;

        /// <summary>Constructor.</summary>
        /// <param name="node">Node to parse</param>
        public Reader(XmlNode node)
        { 
            reader = new ReadWithLookAhead(new XmlNodeReader(node));
            validModelTypes = ModelTypes.GetModelTypes();
            currentState = States.Initial;
        }

        /// <summary>Constructor.</summary>
        /// <param name="s">Stream to parse</param>
        public Reader(Stream s)
        {
            validModelTypes = ModelTypes.GetModelTypes();
            XmlDocument doc = new XmlDocument();
            doc.Load(s);
            reader = new ReadWithLookAhead(new XmlNodeReader(doc.DocumentElement));
            currentState = States.Initial;
        }

        /// <summary>
        /// Add elements to the specified list. If no elements are added, it is assumed that
        /// there are no more elements left and parsing is finished.
        /// </summary>
        /// <param name="elements">A list of elements to add to.</param>
        protected override void AddElements(List<CustomElement> elements)
        {
            // Iterate one or more times through elements on the reader.
            bool continueProcessing;

            do
            {
                continueProcessing = false;
                switch (currentState)
                {
                    case States.Initial:
                        {
                            // This is the very first state and will create the root <ModelWrapper> element.
                            CustomElement element = reader.Read();
                            if (element != null)
                            {
                                //prefix = "xsi";
//                                namespaceURI = "http://www.w3.org/2001/XMLSchema-instance";

                                element.Name = "ModelWrapper";
                                element.attributes = new List<KeyValuePair<string, string>>();
                                element.attributes.Add(new KeyValuePair<string, string>("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance"));
                                elements.Add(element);
                                ReadInName(elements);
                                modelTypesFound.Push("Simulations");
                                openElements.Push("ModelWrapper");
                                currentState = States.ExpectingChildOrModel;
                            }
                            break;
                        }
                    case States.ExpectingChildOrModel:
                        {
                            prefix = "";
                            //namespaceURI = "";

                            // In this state we look for:
                            //    1. the end of all reading, OR
                            //    2. an end element associated with closing a model node e.g. </Clock>, OR
                            //    3. a new model e.g. <Clock>, OR
                            //    4. the first property of a model e.g. <StartDate>
                            CustomElement element = reader.Read();
                            if (element == null)
                            {
                                // 1. the end of all reading
                                elements.Add(new CustomElement() { Name = "ModelWrapper", NodeType = XmlNodeType.EndElement });
                                openElements.Pop();
                                currentState = States.End;
                            }
                            else if (element.NodeType == XmlNodeType.EndElement)
                            {
                               // 2. an end element associated with closing a model node e.g. </ Clock >
                               modelTypesFound.Pop();
                                if (openElements.Peek() == "Model")
                                {
                                    elements.Add(new CustomElement() { Name = "Model", NodeType = XmlNodeType.EndElement });
                                    openElements.Pop();
                                }
                                if (openElements.Peek() == "Child")
                                {
                                    elements.Add(new CustomElement() { Name = "Child", NodeType = XmlNodeType.EndElement });
                                    openElements.Pop();
                                }
                            }
                            else if (ElementIsModel(element))
                            {
                                // 3. a new model e.g. <Clock>, OR
                                CustomElement childElement = new CustomElement();
                                childElement.Name = "Child";
                                childElement.NodeType = XmlNodeType.Element; 
                                childElement.attributes = new List<KeyValuePair<string, string>>();
                                childElement.attributes.Add(new KeyValuePair<string, string>("xsi:type", "ModelWrapper"));
                                elements.Add(childElement);
                                if (element.IsEmptyElement)
                                {
                                    // Create a model element.
                                    CustomElement modelElement = new CustomElement();
                                    modelElement.Name = "Model";
                                    modelElement.attributes = new List<KeyValuePair<string, string>>();
                                    modelElement.attributes.Add(new KeyValuePair<string, string>("xsi:type", element.Name));
                                    modelElement.IsEmptyElement = true;
                                    modelElement.NodeType = XmlNodeType.Element;
                                    elements.Add(modelElement);
                                    elements.Add(new CustomElement() { Name = "Child", NodeType = XmlNodeType.EndElement });
                                }
                                else
                                {
                                    modelTypesFound.Push(element.Name);
                                    openElements.Push("Child");
                                    ReadInName(elements);
                                }
                            }
                            else
                            {
                                // 4. the first property of a model e.g. <StartDate>
                                CustomElement modelElement = new CustomElement();
                                modelElement.Name = "Model";
                                modelElement.NodeType = XmlNodeType.Element;
                                modelElement.attributes = new List<KeyValuePair<string, string>>();
                                modelElement.attributes.Add(new KeyValuePair<string, string>("xsi:type", modelTypesFound.Peek()));
                                elements.Add(modelElement);
                                openElements.Push("Model");
                                currentState = States.ExpectingModelProperties;
                                reader.UndoRead();
                            }
                            break;
                        }
                    case States.ExpectingModelProperties:
                        {
                            // In this state we look for model parameters and structures.
                            //    e.g. simple parameter: <StartDate>xxxx</StartDate>
                            //    e.g. array parameter:
                            //          <LL>
                            //             <double>0.29</double>
                            //             <double>0.29</double>
                            //          </LL>
                            //    e.g. structure
                            //          <ResidueTypes>
                            //          </ResidueTypes>
                            // This state will detect the end of a model parameter and pass control back to
                            // States.ExpectingChildOrModel.

                            CustomElement element = reader.Read();
                            if (element == null)
                                throw new Exception("Unexpected end of .apsimx format reached while reading model parameters.");

                            if (element.NodeType == XmlNodeType.Element)
                            {
                                if (element.IsEmptyElement)
                                {
                                    // 1. If it is an empty element e.g. <eo_source/> then simply pass it through to caller.
                                    elements.Add(element);
                                }
                                else
                                {
                                    CustomElement nextElement = reader.LookAhead();
                                    if (nextElement.Value != string.Empty)
                                    {
                                        // 2. Simple parameter 
                                        elements.Add(element);
                                        elements.Add(reader.Read());
                                        elements.Add(reader.Read());
                                    }
                                    else if (ElementIsModel(element))
                                    {
                                        // 3. New child model found. Need to close the model element and pass control back
                                        //    to States.ExpectingChildOrModel
                                        elements.Add(new CustomElement() { Name = "Model", NodeType = XmlNodeType.EndElement });
                                        openElements.Pop();
                                        reader.UndoRead();
                                        currentState = States.ExpectingChildOrModel;
                                    }
                                    else
                                    {
                                        if (nextElement.Value == string.Empty)
                                        {
                                            //4. Structure - stay in this state and increment counter.
                                            structureCounter++;
                                        }
                                        else
                                        {
                                            // 5. Array parameter found. Need to pass control to States.ParsingArrayParameter
                                            currentState = States.ParsingArrayParameter;
                                        }

                                        elements.Add(element);
                                    }
                                }
                            }
                            else if (element.NodeType == XmlNodeType.EndElement)
                            {
                                if (structureCounter > 0)
                                {
                                    // 6. End of structure.
                                    structureCounter--;
                                    elements.Add(element); // pass element to caller.
                                }
                                else
                                {
                                    // 7. End of our model element. Undo the read of the model element so that 
                                    // States.ExpectingChildOrModel can read it in.
                                    reader.UndoRead();
                                    continueProcessing = true;
                                    currentState = States.ExpectingChildOrModel;
                                }
                            }
                            else
                                elements.Add(element);

                            break;
                        }
                    case States.ParsingArrayParameter:
                        {
                            // In this state we parse array parameters
                            //    e.g. array parameter:
                            //          <LL>
                            //             <double>0.29</double>
                            //             <double>0.29</double>
                            //          </LL>
                            // This state will detect the end of an array parameter and pass control back to
                            // States.ExpectingModelProperties.

                            CustomElement element = reader.Read();
                            if (element == null)
                                throw new Exception("Unexpected end of .apsimx format reached while reading an array.");

                            if (element.IsEmptyElement)
                                elements.Add(element);
                            else if (element.NodeType == XmlNodeType.Element)
                            {
                                elements.Add(element);
                                elements.Add(reader.Read());
                                elements.Add(reader.Read());
                            }
                            else if (element.NodeType == XmlNodeType.EndElement)
                            {
                                elements.Add(element);
                                currentState = States.ExpectingModelProperties;
                            }
                            break;
                        }
                    case States.End:
                        {
                            // All finished.
                            break;
                        }
                }
            }
            while (continueProcessing);
        }

        /// <summary>Is the specified element a known model?</summary>
        /// <param name="element">The element to inspect.</param>
        private bool ElementIsModel(CustomElement element)
        {
            return validModelTypes.Find(t => t.Name == element.Name) != null;
        }

        /// <summary>Read in a name parameter e.g. <Name>xxx</Name></summary>
        /// <param name="elements"></param>
        private void ReadInName(List<CustomElement> elements)
        {
            // Expecting a <Name> element.
            CustomElement element = reader.Read();
            if (element == null || element.Name != "Name")
                throw new Exception("Invalid .apsimx format. Cannot find name element.");
            elements.Add(element);       // name
            elements.Add(reader.Read()); // value
            elements.Add(reader.Read()); // end element
        }

        /// <summary>Gets the prefix of the current element.</summary>
        public override string Prefix
        {
            get
            {
                return prefix;
            }
        }

        /// <summary>Resolves a namespace prefix in the current element's scope.</summary>
        /// <param name="prefix">The Prefix</param>
        /// <returns></returns>
        public override string LookupNamespace(string prefix)
        {
            return reader.LookupNamespace(prefix);
        }

        /// <summary>Gets the namespace URI</summary>
        public override string NamespaceURI { get { return namespaceURI; } }

        /// <summary>Returns the name table.</summary>
        public override XmlNameTable NameTable { get { return reader.NameTable; } }


        /// <summary>
        /// Encapsulates a XmlReader that has the ability to look ahead one or more reads.
        /// </summary>
        class ReadWithLookAhead
        {
            /// <summary>A list of elements that have already been read (looked ahead).</summary>
            private List<CustomElement> lookAheadReads = new List<CustomElement>();

            /// <summary>The reader being encapsulated.</summary>
            private XmlReader reader;

            /// <summary>The next index into lookAheadReads to return.</summary>
            private int currentLookAheadIndex = 0;

            /// <summary>The last element read.</summary>
            private CustomElement lastRead = null;

            /// <summary>Constructor</summary>
            /// <param name="reader">The reader to read from</param>
            public ReadWithLookAhead(XmlReader reader)
            {
                this.reader = reader;
            }

            /// <summary>Read the next element.</summary>
            /// <returns>A CustomElement or null if nothing left to read.</returns>
            public CustomElement Read()
            {
                CustomElement element;
                currentLookAheadIndex = 0;
                if (lookAheadReads.Count > 0)
                {
                    element = lookAheadReads[0];
                    lookAheadReads.RemoveAt(0);
                }
                else
                    element = ReadFromReader();
                lastRead = element;

                return element;
            }

            /// <summary>Undo the last read.</summary>
            public void UndoRead()
            {
                if (lastRead != null)
                {
                    lookAheadReads.Insert(0, lastRead);
                    currentLookAheadIndex = 0;
                }
            }

            /// <summary>
            /// Look ahead the next element.
            /// </summary>
            /// <returns>A CustomElement or null if nothing left to read.</returns>
            public CustomElement LookAhead()
            {
                CustomElement nextElement;
                if (currentLookAheadIndex >= lookAheadReads.Count)
                {
                    nextElement = ReadFromReader();
                    lookAheadReads.Add(nextElement);
                }
                else
                    nextElement = lookAheadReads[currentLookAheadIndex];

                currentLookAheadIndex++;
                return nextElement;
            }

            /// <summary>
            /// Read from the XmlReader instance.
            /// </summary>
            /// <returns>A CustomElement or null if nothing left to read.</returns>
            private CustomElement ReadFromReader()
            {
                bool ok = reader.Read();
                if (!ok)
                    return null;
                CustomElement newElement = new CustomElement();
                newElement.Name = reader.Name;
                newElement.Value = reader.Value;
                newElement.NodeType = reader.NodeType;
                newElement.IsEmptyElement = reader.IsEmptyElement;

                while (reader.MoveToNextAttribute())
                {
                    if (newElement.attributes == null)
                        newElement.attributes = new List<KeyValuePair<string, string>>();
                    newElement.attributes.Add(new KeyValuePair<string, string>(reader.Name, reader.Value));
                }
                reader.MoveToElement();

                return newElement;
            }

            /// <summary>Resolves a namespace prefix in the current element's scope.</summary>
            /// <param name="prefix">The Prefix</param>
            /// <returns></returns>
            public string LookupNamespace(string prefix)
            {
                return reader.LookupNamespace(prefix);
            }

            /// <summary>Returns the name table.</summary>
            public XmlNameTable NameTable
            {
                get
                {
                    return reader.NameTable;
                }
            }

        }
    }
}
