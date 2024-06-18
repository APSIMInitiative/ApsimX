using System;
using System.Xml;

namespace CMPServices
{
    ///=========================================================================
    /// <summary>
    /// The TXMLParser class is a wrapper around a DOM parser. It is used as the
    /// parent class for other specialised parser types. This class makes it easy to 
    /// access features of the DOM parser.
    /// </summary>
    ///=========================================================================
    public class TXMLParser
    {
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
        protected virtual bool isElement(XmlNode domNode, String elementName)
        {
            if ((domNode.NodeType == XmlNodeType.Element) && (domNode.Name == elementName))
                return true;
            else
                return false;
        }
        //======================================================================
        /// <summary>
        /// Construct this parser with an XML string.
        /// </summary>
        /// <param name="xml">XML document.</param>
        //======================================================================
        public TXMLParser(String xml)
        {
            doc = new XmlDocument();
            // Don't let the MSXML parser collapse white space
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            topElement = doc.DocumentElement;
            currNode = null;
        }
        //======================================================================
        /// <summary>
        /// Construct this parser using the node from another document parser.
        /// </summary>
        /// <param name="domNode">The node.</param>
        //======================================================================
        public TXMLParser(XmlNode domNode)
        {
            doc = domNode.OwnerDocument;
            topElement = domNode;
            currNode = null;
        }

        //=======================================================================
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
        // N.Herrmann Sep 2003
        //=======================================================================
        public String getNodeValue(XmlNode domNode)
        {
            String buf = "";
            if (domNode != null)
                buf = domNode.Value;
            return buf;
        }
        //=======================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domNode"></param>
        /// <returns></returns>
        //=======================================================================
        public String InnerXml(XmlNode domNode)
        {
            return domNode.InnerXml;
        }
        //=======================================================================
        /// <summary>
        /// Determine the type of node.
        /// </summary>
        /// <param name="domNode">The node to query.</param>
        /// <returns>The type of the node.</returns>
        // N.Herrmann May 2003
        //=======================================================================
        public XmlNodeType getNodeType(XmlNode domNode)
        {
            XmlNodeType nodeType = XmlNodeType.None;
            if (domNode != null)
                nodeType = domNode.NodeType;
            return nodeType;
        }
        //======================================================================
        /// <summary>
        /// Get the text of an Element node.
        /// </summary>
        /// <param name="domNode">The node to query.</param>
        /// <returns>The value of the element node.</returns>
        //======================================================================
        public String getText(XmlNode domNode)
        {
            String text = "";

            if ((domNode != null) && domNode.HasChildNodes)
            {
                if (domNode.NodeType == XmlNodeType.Element)
                {
                    text = domNode.InnerText;
                }
            }
            return text;
        }
        //======================================================================
        /// <summary>
        /// Returns the node's tag and all XML within the node.
        /// </summary>
        /// <param name="domNode">The node to query.</param>
        /// <returns>The section of the XML document including the node's tag.</returns>
        //======================================================================
        public String docToString(XmlNode domNode)
        {
            return domNode.OuterXml;
        }
        //======================================================================
        /// <summary>
        /// Get the text from the value of the attribute.
        /// </summary>
        /// <param name="domNode">The element node containing the attribute node.</param>
        /// <param name="attr">The name of the attribute.</param>
        /// <returns>The value of the attribute node.</returns>
        //======================================================================
        public String getAttrValue(XmlNode domNode, String attr)
        {
            String attrValue = "";

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
        //======================================================================
        /// <summary>
        /// Find the first child node of the specified node. Sets currNode.
        /// </summary>
        /// <param name="domNode">The node containing the child.</param>
        /// <returns>The child node.</returns>
        //======================================================================
        public XmlNode firstChild(XmlNode domNode)
        {
            if (domNode != null)
                currNode = domNode.FirstChild;
            else
                currNode = null;

            return currNode;
        }
        //======================================================================
        /// <summary>
        /// Find the next sibling to the currNode. Sets currNode.
        /// </summary>
        /// <param name="domNode">The node containing the child.</param>
        /// <returns>The child node after currNode.</returns>
        //======================================================================
        public XmlNode nextSibling(XmlNode domNode)
        {
            if ((domNode != null) && (domNode != topElement))
                currNode = domNode.NextSibling;
            else
                currNode = null;

            return currNode;
        }
        //======================================================================
        /// <summary>
        /// Find the first child node that is an element node of specified name.
        /// Sets currNode.
        /// </summary>
        /// <param name="rootNode">The node.</param>
        /// <param name="elementName">The name of the element node.</param>
        /// <returns></returns>
        //======================================================================
        public XmlNode firstElementChild(XmlNode rootNode, String elementName)
        {
            XmlNode childNode;

            childNode = firstChild(rootNode);
            while ((childNode != null) && (!isElement(childNode, elementName)))
            {
                childNode = nextSibling(childNode);
            }

            return currNode;
        }
        //======================================================================
        /// <summary>
        /// Find the next sibling element node to currNode. Sets currNode.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="elementName">The name of the element node.</param>
        /// <returns></returns>

        //======================================================================
        public XmlNode nextElementSibling(XmlNode startNode, String elementName)
        {
            XmlNode childNode;

            childNode = nextSibling(startNode);
            while ((childNode != null) && (!isElement(childNode, elementName)))
            {
                childNode = nextSibling(childNode);
            }
            return currNode;
        }
        //======================================================================
        /// <summary>
        /// Get the node referenced by currNode.
        /// </summary>
        /// <returns>Ref to currNode.</returns>
        //======================================================================
        public XmlNode currentNode()
        {
            return currNode;
        }
        //======================================================================
        /// <summary>
        /// Get the topmost node in the document.
        /// </summary>
        /// <returns>Ref to topElement field.</returns>
        //======================================================================
        public XmlNode rootNode()
        {
            return topElement;
        }
        //======================================================================
        /// <summary>
        /// Set the topmost node in the document.
        /// </summary>
        /// <param name="anode">The node to be referenced by topElement field.</param>
        //======================================================================
        public void setTopNode(XmlNode anode)
        {
            topElement = anode;
        }
    }

}
