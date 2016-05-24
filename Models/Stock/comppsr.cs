using System;
using System.Xml;

namespace CMPServices
{
    //=======================================================================
    /// <summary>
    /// TCompParser reads a component's SDML definition. It could be a system or a 
    /// non system component.
    /// Contains a <see cref="TInitParser">TInitParser</see> for dealing with the 'initsection'
    /// <para>Component SDML follows this structure:<br/>
    /// &lt;![CDATA[&lt;component&gt;<br/>
    /// &lt;/component&gt;<br/>
    /// &lt;system&gt;<br/>
    ///    &lt;component&gt;<br/>
    ///    &lt;/component&gt;<br/>
    /// &lt;/system&gt;]]&gt;
    /// 
    /// </para>
    /// <seealso cref="TInitParser">TInitParser Class</seealso>
    /// </summary>
    //=======================================================================
    public class TCompParser : TXMLParser
    {
        private TInitParser initsParser;            //parser for the init xml

        /// <summary>
        /// Name of the component
        /// </summary>
        protected String FName;
        /// <summary>
        /// Version number of this component
        /// </summary>
        protected String FVersion;
        /// <summary>
        /// True if this is a system
        /// </summary>
        private Boolean FIsSystem;
        /// <summary>
        /// True if this has active="T"
        /// </summary>
        private Boolean FIsActive;
        /// <summary>
        /// Executable module of the component
        /// </summary>
        private String FExecutable;
        /// <summary>
        /// Machine on which the system is to be executed
        /// </summary>
        private String FLocation;
        /// <summary>
        /// XML string of the 
        /// &lt;initdata&gt;
        /// &lt;/initdata&gt; section.
        /// </summary>
        private String FInitSDML;
        /// <summary>
        /// Flag to signify whether this appears to be an APSRU component
        /// </summary>
        private Boolean FIsAPSRU;
        /// <summary>
        /// Class of the component.
        /// </summary>
        private String FClass;

        /// <summary>
        /// Name of component element in xml code
        /// </summary>
        protected const String componentElement = "component";
        /// <summary>
        /// Name of system element in xml code
        /// </summary>
        protected const String systemElement = "system";
        /// <summary>
        /// Name of simulation element in xml code
        /// </summary>
        protected const String simulationElement = "simulation";
        //=======================================================================
        /// <summary>
        /// Reads the description elements of this component and then calls readInitSection().
        /// </summary>
        //=======================================================================
        private void getDescription()
        {
            XmlNode anode;
            FVersion = "";
            FName = "";
            FExecutable = "";
            FLocation = "";
            FClass = "";
            FInitSDML = "";
            FIsSystem = false;
            FIsActive = false;
            FIsAPSRU = false;
            FVersion = "";

            anode = topElement;  //the top element is expected to be a component
            if (anode != null)
            {
                FIsSystem = isElement(anode, systemElement) || isElement(anode, simulationElement);
                if (isElement(anode, systemElement) || isElement(anode, componentElement) || isElement(anode, simulationElement))
                {
                    FName = getAttrValue(anode, "name");
                    FClass = getAttrValue(anode, "class");
                    FIsActive = (getAttrValue(anode, "active") == "T");
                }

                anode = firstElementChild(topElement, "executable");
                if (anode != null)
                {
                    FExecutable = getAttrValue(anode, "name");
                    FVersion = getAttrValue(anode, "version");
                }

                anode = firstElementChild(topElement, "location");
                if (anode != null)
                {
                    FLocation = getText(anode);
                }
            }

            readInitSection();  //creates an inits parser
        }
        //=======================================================================
        /// <summary>
        /// Reads the init section and parses the XML. Uses the &lt;initsection&gt; block 
        /// retrieved in the initData() function.
        /// </summary>
        //=======================================================================
        private void readInitSection()
        {
            if (initsParser == null)
            {
                string initSDML = initData();
                if (initSDML.Length > 0)
                {
                    initsParser = new TInitParser(initSDML);
                }
            }
        }
        //=======================================================================
        /// <summary>
        /// Create a component parser from an XML string.
        /// </summary>
        /// <param name="sXml">Component as described in an SDML document.</param>
        //=======================================================================
        public TCompParser(String sXml)
            : base(sXml)
        {
            initsParser = null;
            getDescription();
        }
        //=======================================================================
        /// <summary>
        /// Create a component parser from a DOM node in an existing DOM document.
        /// </summary>
        /// <param name="domNode">DOM node to use.</param>
        //=======================================================================
        public TCompParser(XmlNode domNode)
            : base(domNode)
        {
            initsParser = null;
            getDescription();
        }
        //=======================================================================
        /// <summary>
        /// Returns the whole &lt;initdata&gt; section cdata text. This will be the<br/>
        /// &lt;initsection&gt;<br/>
        /// &lt;/initsection&gt;<br/>
        /// </summary>
        /// <returns>The XML string of the initdata section text.</returns>
        //=======================================================================
        public String initData()
        {
            XmlNode anode;   //start node
            XmlNode cnode;   //child node
            Boolean found;

            String buf = "<initsection></initsection>";     //when it is not found
            found = false;

            //find data in a CDATA section
            anode = firstElementChild(topElement, "initdata");
            if (anode != null)
            {
                cnode = firstChild(anode);
                while (!found && cnode != null)
                {
                    if (getNodeType(cnode) == XmlNodeType.CDATA)
                    {
                        found = true;
                        buf = getNodeValue(cnode);
                        if (buf.Contains("<initsection>"))  //rough way to see if this is an <initsection> block
                            FInitSDML = buf;                //this is always only an XML string
                    }
                    cnode = nextSibling(cnode);
                }
            }

            // kludge for APSRU plant component (no CDATA section)
            if (found == false)
            {
                anode = firstElementChild(topElement, "initdata");
                if (anode != null)
                {
                    buf = docToString(anode);
                    FInitSDML = buf;
                    FIsAPSRU = true;
                }
            }

            return buf;
        }
        //=======================================================================
        /// <summary>
        /// Get the SDML text for an init in the init section.
        /// </summary>
        /// <param name="initIndex">The index of an init in the init section. 1 -> x</param>
        /// <returns>The SDML text for an init variable.<br/>
        /// &lt;init&gt;<br/>
        /// ...<br/>
        /// &lt;/init&gt;
        /// </returns>
        //=======================================================================
        public String initText(uint initIndex)
        {
            if (initsParser != null)
                return initsParser.initText(initIndex);
            else
                return "";
        }
        //=======================================================================
        /// <summary>
        /// Get the XML node of the init in the init section.
        /// </summary>
        /// <param name="initIndex">The index of an init in the init section. 1 -> x</param>
        /// <returns>The DOM node of the init.</returns>
        //=======================================================================
        public XmlNode initNode(uint initIndex)
        {
            return initsParser.initNode(initIndex);
        }
        //============================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">1-x</param>
        /// <returns>Returns the name value of the TSDMLValue.</returns>
        //============================================================================
        public string initName(uint idx)
        {
            string name = "";

            XmlNode anode = initsParser.initNode(idx);
            if (anode != null)
                name = getAttrValue(anode, "name");
            return name;
        }
        //============================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="initName">Name of the init property.</param>
        /// <returns>The string containing the init</returns>
        //============================================================================
        public string initTextByName(string initName)
        {
            return initsParser.initTextByName(initName);
        }
        //=======================================================================
        /// <summary>
        /// Executable module (dll) name of the component.
        /// </summary>
        /// <returns>The value of FExecutable field.</returns>
        //=======================================================================
        public String Executable
        {
            get { return FExecutable; }
        }
        //=======================================================================
        /// <summary>
        /// Machine on which the system is to be executed
        /// </summary>
        /// <returns>The value of FLocation field.</returns>
        //=======================================================================
        public String Location
        {
            get { return FLocation; }
        }
        //=======================================================================
        /// <summary>
        /// The class of this component
        /// </summary>
        /// <returns>The value of FClass field.</returns>
        //=======================================================================
        public String CompClass
        {
            get { return FClass; }
        }
        //=======================================================================
        /// <summary>
        /// Number of inits this component has.
        /// </summary>
        /// <returns>The count of inits in this component SDML.</returns>
        //=======================================================================
        public uint initCount()
        {
            uint count = 0;
            if (initsParser != null)
                count = initsParser.initCount();
            return count;
        }
        //=======================================================================
        /// <summary>
        /// Get the XML SDML string of the initdata section. If the initdata section
        /// doesn't contain XML then this should return an empty string.
        /// Normally<br/>
        /// &lt;initsection&gt;<br/>
        /// ...<br/>
        /// &lt;/initsection&gt;<br/>
        /// </summary>
        /// <returns>The value of SDML in the initdata section.</returns>
        //=======================================================================
        public String InitDataText
        {
            get { return FInitSDML; }
        }
        //=======================================================================
        /// <summary>
        /// True if this is a system component.
        /// </summary>
        //=======================================================================
        public Boolean IsSystem
        {
            get { return FIsSystem; }
            //set { FIsSystem = value; }
        }
        //=======================================================================
        /// <summary>
        /// True if this has active="T"
        /// </summary>
        //=======================================================================
        public Boolean IsActive
        {
            get { return FIsActive; }
            //set { FIsActive = value; }
        }
        //=======================================================================
        /// <summary>
        /// Name of the component.
        /// </summary>
        //=======================================================================
        public String InstanceName
        {
            get { return FName; }
            //set { FName = value; }
        }
        //=======================================================================
        /// <summary>
        /// Version of the component.
        /// </summary>
        //=======================================================================
        public String Version
        {
            get { return FVersion; }
            //set { FVersion = value; }
        }
        //=======================================================================
        /// <summary>
        /// 
        /// </summary>
        //=======================================================================
        public String PubEventArrayName
        {
            get { return initsParser.PubEventArrayName; }
        }
        //=======================================================================
        /// <summary>
        /// 
        /// </summary>
        //=======================================================================
        public String DriverArrayName
        {
            get { return initsParser.DriverArrayName; }
        }
        //=======================================================================
        /// <summary>
        /// 
        /// </summary>
        //=======================================================================
        public String SubEventArrayName
        {
            get { return initsParser.SubEventArrayName; }
        }
        //=======================================================================
        /// <summary>
        /// Finds the first child component or system from the rootNode.
        /// </summary>
        /// <returns>String containing the XML for the component. Returns empty string
        /// when nothing is found.</returns>
        //=======================================================================
        public String firstChildComp()
        {
            XmlNode anode;

            String buf = "";

            anode = firstChild(topElement);
            while ((anode != null) && (!isElement(anode, systemElement))
               && (!isElement(anode, componentElement)))
            {
                anode = nextSibling(anode);
            }

            if (anode != null)
                buf = docToString(anode);

            return buf;
        }
        //=======================================================================
        /// <summary>
        /// Finds the sibling component to the currNode.
        /// </summary>
        /// <returns>String containing the XML for the component. Returns empty string
        /// when nothing is found.</returns>
        //=======================================================================
        public String nextSiblingComp()
        {
            XmlNode anode;

            String buf = "";

            anode = nextSibling(currNode);
            while ((anode != null) && (!isElement(anode, systemElement))
                && (!isElement(anode, componentElement)))
            {
                anode = nextSibling(anode);
            }

            if (anode != null)
                buf = docToString(anode);

            return buf;
        }
        //=======================================================================
        /// <summary>
        /// Finds the first child component or system from the rootNode
        /// </summary>
        /// <returns>The DOM node of the component element.</returns>
        //=======================================================================
        public XmlNode firstChildCompNode()
        {
            XmlNode anode;

            anode = firstChild(topElement);
            while ((anode != null) && (!isElement(anode, systemElement))
                                   && (!isElement(anode, componentElement)))
            {
                anode = nextSibling(anode);
            }
            return anode;
        }
        //=======================================================================
        /// <summary>
        /// Finds the first child component or system from the rootNode
        /// </summary>
        /// <param name="domNode">The starting node.</param>
        /// <returns>The DOM node of the component element.</returns>
        //=======================================================================
        public XmlNode firstChildCompNode(XmlNode domNode)
        {
            XmlNode anode;

            anode = firstChild(domNode);
            while ((anode != null) && (!isElement(anode, systemElement))
                                   && (!isElement(anode, componentElement)))
            {
                anode = nextSibling(anode);
            }
            return anode;
        }
        //=======================================================================
        /// <summary>
        /// Finds the next child which is the sibling of currNode.
        /// </summary>
        /// <returns>The DOM node of the next sibling node.</returns>
        //=======================================================================
        public XmlNode nextSiblingCompNode()
        {
            XmlNode anode;

            anode = nextSibling(currNode);
            while ((anode != null) && (!isElement(anode, systemElement))
                                   && (!isElement(anode, componentElement)))
            {
                anode = nextSibling(anode);
            }
            return anode;
        }
        //=======================================================================
        /// <summary>
        /// Finds the next child which is the sibling of currNode.
        /// </summary>
        /// <param name="domNode">The starting node.</param>
        /// <returns>The DOM node of the next sibling node.</returns>
        //=======================================================================
        public XmlNode nextSiblingCompNode(XmlNode domNode)
        {
            XmlNode anode;

            anode = nextSibling(domNode);
            while ((anode != null) && (!isElement(anode, systemElement))
                                   && (!isElement(anode, componentElement)))
            {
                anode = nextSibling(anode);
            }
            return anode;
        }
        //=======================================================================
        /// <summary>
        /// Get the XML of the root document node including the root node tag.
        /// </summary>
        /// <returns>The XML representation of the root node.</returns>
        //=======================================================================
        public String getXML()
        {
            return docToString(rootNode());
        }

        //=======================================================================
        /// <summary>
        /// Get the flag indicating whether this was an APSRU component.
        /// </summary>
        /// <returns>True if an APSRU component, false otherwise.</returns>
        //=======================================================================
        public Boolean IsAPSRU()
        {
            return FIsAPSRU;
        }
    }
}
