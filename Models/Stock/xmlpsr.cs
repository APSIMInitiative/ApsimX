namespace CMPServices
{
    using System.Xml;

    /// <summary>
    /// The XMLParser class is a wrapper around a DOM parser. It is used as the
    /// parent class for other specialised parser types. This class makes it easy to 
    /// access features of the DOM parser.
    /// </summary>
    public class XMLParser
    {
        /// <summary>
        /// The main DOM
        /// </summary>
        private XmlDocument doc;

        /// <summary>
        /// Always points to the root node.
        /// </summary>
        protected XmlNode topElement;

        /// <summary>
        /// Always points to current node after using firstChild() or nextSibling().
        /// </summary>
        protected XmlNode currNode;

        /// <summary>
        /// Tests if this an element node in the parser tree with a specific name.
        /// </summary>
        /// <param name="domNode">The node to test.</param>
        /// <param name="elementName">The name of the element node.</param>
        /// <returns>True if this is and element node with this name.</returns>
        protected virtual bool IsElement(XmlNode domNode, string elementName)
        {
            if ((domNode.NodeType == XmlNodeType.Element) && (domNode.Name == elementName))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Construct this parser with an XML string.
        /// </summary>
        /// <param name="xml">XML document.</param>
        public XMLParser(string xml)
        {
            this.doc = new XmlDocument();

            // Don't let the MSXML parser collapse white space
            this.doc.PreserveWhitespace = true;
            this.doc.LoadXml(xml);
            this.topElement = this.doc.DocumentElement;
            this.currNode = null;
        }

        /// <summary>
        /// Construct this parser using the node from another document parser.
        /// </summary>
        /// <param name="domNode">The node.</param>
        public XMLParser(XmlNode domNode)
        {
            this.doc = domNode.OwnerDocument;
            this.topElement = domNode;
            this.currNode = null;
        }

        /// <summary>
        /// Get the value of a DOM node. The result depends on what type of node it is. <see cref="XmlNode">See XmlNode Class</see>
        /// </summary>
        /// <param name="domNode">The DOM node to query.</param>
        /// <returns>The Node value. "" if not found.
        /// <para>This value depends on the value of the nodeType property.</para>
        /// <para><b>NODE_ATTRIBUTE:</b> Contains a string representing the value of the attribute.
        /// For attributes with subnodes, this is the concatenated text of all subnodes
        /// with entities expanded. Setting this value deletes all children of the node
        /// and replaces them with a single text node containing the value written.</para>
        /// <para><b>NODE_CDATA_SECTION:</b> Contains a string representing the text stored in the CDATA section.</para>
        /// <para><b>NODE_COMMENT:</b> Contains the content of the comment, exclusive of the comment's start and end sequence.</para>
        /// <para><b>NODE_DOCUMENT, NODE_DOCUMENT_TYPE, NODE_DOCUMENT_FRAGMENT,
        /// NODE_ELEMENT, NODE_ENTITY, NODE_ENTITY_REFERENCE, NODE_NOTATION:</b>
        /// Contains Null. Note that attempting to set the value of nodes of these types generates an error.</para>
        /// <para><b>NODE_PROCESSING_INSTRUCTION:</b> Contains the content of the processing instruction,
        /// excluding the target. (The target appears in the nodeName property.)</para>
        /// <para><b>NODE_TEXT:</b> Contains a string representing the text stored in the text node.</para>
        /// ...(source MS doco)
        /// </returns>
        /// <seealso cref="XmlNode">XmlNode Class</seealso>
        /// N.Herrmann Sep 2003
        public string GetNodeValue(XmlNode domNode)
        {
            string buf = string.Empty;
            if (domNode != null)
                buf = domNode.Value;
            return buf;
        }

        /// <summary>
        /// Get the inner XML for the node
        /// </summary>
        /// <param name="domNode">The XML node</param>
        /// <returns>The inner XML string</returns>
        public string InnerXml(XmlNode domNode)
        {
            return domNode.InnerXml;
        }

        /// <summary>
        /// Determine the type of node.
        /// </summary>
        /// <param name="domNode">The node to query.</param>
        /// <returns>The type of the node.</returns>
        public XmlNodeType GetNodeType(XmlNode domNode)
        {
            XmlNodeType nodeType = XmlNodeType.None;
            if (domNode != null)
                nodeType = domNode.NodeType;
            return nodeType;
        }

        /// <summary>
        /// Get the text of an Element node.
        /// </summary>
        /// <param name="domNode">The node to query.</param>
        /// <returns>The value of the element node.</returns>
        public string GetText(XmlNode domNode)
        {
            string text = string.Empty;

            if ((domNode != null) && domNode.HasChildNodes)
            {
                if (domNode.NodeType == XmlNodeType.Element)
                {
                    text = domNode.InnerText;
                }
            }
            return text;
        }

        /// <summary>
        /// Returns the node's tag and all XML within the node.
        /// </summary>
        /// <param name="domNode">The node to query.</param>
        /// <returns>The section of the XML document including the node's tag.</returns>
        public string DocToString(XmlNode domNode)
        {
            return domNode.OuterXml;
        }

        /// <summary>
        /// Get the text from the value of the attribute.
        /// </summary>
        /// <param name="domNode">The element node containing the attribute node.</param>
        /// <param name="attr">The name of the attribute.</param>
        /// <returns>The value of the attribute node.</returns>
        public string GetAttrValue(XmlNode domNode, string attr)
        {
            string attrValue = string.Empty;

            if ((domNode != null) /*&& (domNode.HasChildNodes)*/ )
            {
                if (domNode.NodeType == XmlNodeType.Element)
                {
                    XmlNamedNodeMap map = domNode.Attributes;
                    XmlNode attrnode;
                    attrnode = map.GetNamedItem(attr);
                    if (attrnode != null)
                    {
                        attrValue = attrnode.FirstChild.Value;
                    }
                }
            }
            return attrValue;
        }

        /// <summary>
        /// Find the first child node of the specified node. Sets currNode.
        /// </summary>
        /// <param name="domNode">The node containing the child.</param>
        /// <returns>The child node.</returns>
        public XmlNode FirstChild(XmlNode domNode)
        {
            if (domNode != null)
                this.currNode = domNode.FirstChild;
            else
                this.currNode = null;

            return this.currNode;
        }

        /// <summary>
        /// Find the next sibling to the currNode. Sets currNode.
        /// </summary>
        /// <param name="domNode">The node containing the child.</param>
        /// <returns>The child node after currNode.</returns>
        public XmlNode NextSibling(XmlNode domNode)
        {
            if ((domNode != null) && (domNode != this.topElement))
                this.currNode = domNode.NextSibling;
            else
                this.currNode = null;

            return this.currNode;
        }

        /// <summary>
        /// Find the first child node that is an element node of specified name.
        /// Sets currNode.
        /// </summary>
        /// <param name="rootNode">The node.</param>
        /// <param name="elementName">The name of the element node.</param>
        /// <returns>The XML node</returns>
        public XmlNode FirstElementChild(XmlNode rootNode, string elementName)
        {
            XmlNode childNode;

            childNode = this.FirstChild(rootNode);
            while ((childNode != null) && (!this.IsElement(childNode, elementName)))
            {
                childNode = this.NextSibling(childNode);
            }

            return this.currNode;
        }

        /// <summary>
        /// Find the next sibling element node to currNode. Sets currNode.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="elementName">The name of the element node.</param>
        /// <returns>The sibling XML node</returns>
        public XmlNode NextElementSibling(XmlNode startNode, string elementName)
        {
            XmlNode childNode;

            childNode = this.NextSibling(startNode);
            while ((childNode != null) && (!this.IsElement(childNode, elementName)))
            {
                childNode = this.NextSibling(childNode);
            }
            return this.currNode;
        }

        /// <summary>
        /// Get the node referenced by currNode.
        /// </summary>
        /// <returns>Ref to currNode.</returns>
        public XmlNode CurrentNode()
        {
            return this.currNode;
        }

        /// <summary>
        /// Get the topmost node in the document.
        /// </summary>
        /// <returns>Ref to topElement field.</returns>
        public XmlNode RootNode()
        {
            return this.topElement;
        }

        /// <summary>
        /// Set the topmost node in the document.
        /// </summary>
        /// <param name="anode">The node to be referenced by topElement field.</param>
        public void SetTopNode(XmlNode anode)
        {
            this.topElement = anode;
        }
    }
}
