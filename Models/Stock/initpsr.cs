using System;
using System.Xml;

namespace CMPServices
{
    //=======================================================================
    /// <summary>
    /// This class is used to parse the initsection descriptor XML definition that
    /// is in an SDML format. Requires XML:
    /// <para>
    /// &lt;somexmltag&gt;<br/> 
    ///     &lt;init&gt; ... &lt;/init&gt;<br/>
    ///     &lt;init&gt; ... &lt;/init&gt;<br/>
    /// &lt;/somexmltag>
    /// </para>
    /// <seealso cref="TCompParser">TCompParser Class</seealso>
    /// </summary>
    //=======================================================================
    public class TInitParser : TXMLParser
    {
        private uint FInitCount;
        /* names of the special SDML arrays (inits) found in the <initsection> of a component */
        /// <summary>
        /// Name of the published event array
        /// </summary>
        /// <returns></returns>
        public string PubEventArrayName = "published_events";
        /// <summary>
        /// Name of the subscribed event array
        /// </summary>
        /// <returns></returns>
        public string SubEventArrayName = "subscribed_events";
        /// <summary>
        /// Name of the driver connection array
        /// </summary>
        /// <returns></returns>
        public string DriverArrayName = "driver_connections";
        //=======================================================================
        /// <summary>
        /// Read all the inits found in the the init XML. Used by the constructor.
        /// </summary>
        //=======================================================================
        protected void getInits()
        {
            XmlNode anode;
            FInitCount = 0;

            anode = firstElementChild(topElement, "init");
            while (anode != null)
            {
                FInitCount++;
                anode = nextElementSibling(anode, "init");
            }
        }
        //=======================================================================
        /// <summary>
        /// Construct an inits parser using the SDML text.
        /// </summary>
        /// <param name="sdml">SDML formatted XML text.</param>
        //=======================================================================
        public TInitParser(String sdml)
            : base(sdml)
        {
            getInits();   //does a count of the inits for this section
        }
        //=======================================================================
        /// <summary>
        /// Gets the SDML for an init.
        /// </summary>
        /// <param name="initIndex">Indexed 1 -> x</param>
        /// <returns>The SDML text for an init variable.</returns>
        //=======================================================================
        public String initText(uint initIndex)
        {
            int count = 0;
            XmlNode anode;

            String result = "";
            if (initIndex <= FInitCount)
            {
                count = 1;
                anode = firstElementChild(topElement, "init");
                while ((initIndex > count) && (anode != null))
                {
                    anode = nextElementSibling(anode, "init");
                    count++;
                }
                if (anode != null)
                    result = docToString(anode);
            }
            return result;
        }

        //=======================================================================
        /// <summary>
        /// Get the DOM node in the inits list at the specified index.
        /// </summary>
        /// <param name="initIndex">Indexed 1 -> x</param>
        /// <returns>The DOM node for an init variable.</returns>
        // E.Zurcher  Oct 2005
        //=======================================================================
        public XmlNode initNode(uint initIndex)
        {
            int count;
            XmlNode anode;
            XmlNode result = null;

            if (initIndex <= FInitCount)
            {
                count = 1;
                anode = firstElementChild(topElement, "init");
                while ((initIndex > count) && (anode != null))
                {
                    anode = nextElementSibling(anode, "init");
                    count++;
                }
                result = anode;
            }
            return result;
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
            bool found = false;
            XmlNode anode;

            string buf = "";
            anode = firstElementChild(rootNode(), "init");
            while (!found && (anode != null))
            {
                if (getAttrValue(anode, "name") == initName)
                {
                    found = true;
                    buf = docToString(anode);
                }
                else
                    anode = nextElementSibling(anode, "init");
            }
            return buf;
        }
        //=======================================================================
        /// <summary>
        /// Get the node text for the 'events' element.
        /// </summary>
        /// <returns>SDML text</returns>
        // N.Herrmann Dec 2003
        //=======================================================================
        public String getEventArraySDML()
        {
            XmlNode anode;

            String result = "";

            anode = firstElementChild(topElement, "events"); //this init tag is <events>
            if (anode != null)
                result = docToString(anode);

            return result;
        }
        //=======================================================================
        /// <summary>
        /// Get the count if init items in this init section.
        /// </summary>
        /// <returns>The value of FInitCount.</returns>
        //=======================================================================
        public uint initCount()
        {
            return FInitCount;
        }
    }
}
