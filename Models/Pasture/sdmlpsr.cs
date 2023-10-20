using System;
using System.Xml;


namespace CMPServices
{
    //===========================================================================
	/// <summary>
	/// Specialised parser for the SDML found in an XML document.
	/// </summary>
	/// <seealso cref="TXMLParser">TXMLParser Class</seealso>
    //===========================================================================
   public class TSDMLParser : TXMLParser
   {
      private const String fieldElement = "field";   //String used as the tag in the xml description. Normally "field"
      private const String arrayElement = "element"; //String used as the tag in the xml description. Normally "element"
      /// <summary>
      /// Name of the SDML value.
      /// </summary>
      protected String FName;
      /// <summary>
      /// Unit for the scalar.
      /// </summary>
      protected String FUnit;
      /// <summary>
      /// Kind of the SDML value.
      /// </summary>
      protected String FKind;
      /// <summary>
      /// True is this is a scalar.
      /// </summary>
      protected Boolean FIsScalar;
      /// <summary>
      /// True if this is an array.
      /// </summary>
      protected Boolean FIsArray;
      /// <summary>
      /// True if this is a record (structure).
      /// </summary>
      protected Boolean FIsRecord;

      //======================================================================
      /// <summary>
      /// Create a parser object using the SDML XML string.
      /// </summary>
      /// <param name="xml">XML string in SDML form.</param>
      //======================================================================
      public TSDMLParser(String xml) : base(xml)
      {
         getDescription();
      }
      //======================================================================
      /// <summary>
      /// Create a parser object using the DOM node from another document.
      /// </summary>
      /// <param name="domNode">The DOM node to use for creating the parser object.</param>
      //======================================================================
      public TSDMLParser(XmlNode domNode) : base(domNode)
      {
         getDescription();
      }
      //======================================================================
      /// <summary>
      /// Read the descriptive elements for the SDMl type.
      /// </summary>
      //======================================================================
      public void getDescription()
      {
         String sBuf = "";

         FIsScalar = false;   //init because this may not be a new parser object
         FIsRecord = false;

         FName = getAttrValue(topElement, "name");
         FUnit = getAttrValue(topElement, "unit");
         FKind = getAttrValue(topElement, "kind");

         sBuf = getAttrValue(topElement, "array");
         if (sBuf.Length > 0 && (sBuf.ToLower()[0] == 't')) 
         {
            FIsArray = true;
            FIsScalar = false;
            FIsRecord = false;
         }
         else 
         {
            FIsArray = false;
            if (FKind.Length > 0)
            {
                if (FKind == "defined")
                {
                    FIsRecord = true;
                }
                else
                {
                    FIsScalar = true;
                }
            }
            if (firstElementChild(topElement, "field") != null) //if a child element is a field
                FIsRecord = true;
         }
      }

      //======================================================================
      /// <summary>
      /// The name of the SDML value.
      /// </summary>
      /// <returns>The value of FName.</returns>
      //======================================================================
      public String Name
      {
          get { return FName; }
      }

      //======================================================================
      /// <summary>
      /// Units for the scalar value.
      /// </summary>
      /// <returns>The value of FUnit.</returns>
      //======================================================================
      public String Units
      {
          get { return FUnit; }
      }

      //======================================================================
      /// <summary>
      /// The kind of the SDML value.
      /// </summary>
      /// <returns>The value of FKind.</returns>
      //======================================================================
      public String Kind
      {
          get { return FKind; }
      }

      //======================================================================
      /// <summary>
      /// Determine if this is a scalar value.
      /// </summary>
      /// <returns>True if this is a scalar value.</returns>
      //======================================================================
      public Boolean IsScalar
      {
          get { return FIsScalar; }
      }

      //======================================================================
      /// <summary>
      /// Determine if this is a record structure.
      /// </summary>
      /// <returns>True if this is a record.</returns>
      //======================================================================
      public Boolean IsRecord
      {
          get { return FIsRecord; }
      }

      //======================================================================
      /// <summary>
      /// Determine if this is an array.
      /// </summary>
      /// <returns>True if this is an array.</returns>
      //======================================================================
      public Boolean IsArray
      {
          get { return FIsArray; }
      }

      //======================================================================
      /// <summary>
      /// Find the first element node of the document. Sets currNode.
      /// </summary>
      /// <returns>The XML document for the first element. Empty string if not found.</returns>
      //======================================================================
      public String firstMember()
      {
         XmlNode anode;
         String sSDML = "";

         anode = firstMember(topElement);
         if (anode != null)
            sSDML = docToString(anode);

         return sSDML;
      }

      //======================================================================
      /// <summary>
      /// Find the first element node in the document using rootNode as the 
      /// document root. Sets the currNode.
      /// </summary>
      /// <param name="rootNode">The base node.</param>
      /// <returns>currNode value.</returns>
      //======================================================================
      public XmlNode firstMember(XmlNode rootNode)
      {
         Boolean found;

         //step through the child elements of this component
         firstChild(rootNode);   //set the currNode
         found = false;
         while ( (currNode != null) && (!found) ) 
         {
            if (isElement(currNode, arrayElement) || isElement(currNode, fieldElement) ) 
            {
               found = true;
            }
            if (!found)           //if this node is found to be ok then leave currNode pointing to it
               nextSibling(currNode);
         }
         return currNode;
      }

      //======================================================================
      /// <summary>
      /// Find the next element node after currNode. Sets the currNode.
      /// </summary>
      /// <returns>The XML document of the node found. Empty string if not found</returns>
      //======================================================================
      public String nextMember()
      {
         XmlNode anode;
         String sSDML = "";

         anode = nextMember(currNode);
         if (anode != null)
            sSDML = docToString(anode);

         return sSDML;
      }

      //======================================================================
      /// <summary>
      /// Finds the next DOM element node that is a sibling of startNode. 
      /// </summary>
      /// <param name="startNode">DOM Node to start the search from.</param>
      /// <returns>The DOM Node found. NULL if not found.</returns>
      // N.Herrmann Apr 2002
      //======================================================================
      public XmlNode nextMember(XmlNode startNode)
      {
         Boolean found;

         //step through the child elements of this component
         nextSibling(startNode);   //set the currNode
         found = false;
         while ( (currNode != null) && (!found) ) 
         {
            if (isElement(currNode, arrayElement) || isElement(currNode, fieldElement) ) 
            {
               found = true;
            }
            if (!found)           //if this node is found to be ok then leave currNode pointing to it
               nextSibling(currNode);
         }
         return currNode;
      }

   }
}
