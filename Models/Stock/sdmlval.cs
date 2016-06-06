using System;
using System.Text;
using System.Xml;
using System.IO;

namespace CMPServices
{
   //============================================================================
   /// <summary>
   /// Defines an SDML structured value used throughout the CMP.
   /// </summary>
   /// <seealso cref="TTypedValue">TTypedValue Class</seealso>
	//============================================================================
   public class TSDMLValue : TTypedValue
	{
      //============================================================================
      /// <summary>
      /// Reads the fields/elements from the SDML value.
      /// </summary>
      //============================================================================
      protected override void getFldElemList()
      {
         TSDMLValue newMember;
         XmlNode memberNode;
         XmlNode valNode;
         String strVal;

         //builds the child list using the parent's parser and just shifts the
         //parser's topElement domnode respectively.
         memberNode = parser.firstMember(parser.rootNode()); //looks for <element> or <field> children

         //while more <element> or <field> children
         while (memberNode != null) {
            newMember = new TSDMLValue(parser, memberNode, "");
            FMembers.Add(newMember);    //add to the list of children

            memberNode = parser.nextMember(memberNode);
         }

         //----------------------------------------------------------------
         //Scalar arrays may have values as a list of <val> elements
         if (FIsArray && (FBaseType != TTypedValue.TBaseType.ITYPE_DEF) && (count() == 0))
         {
            valNode = parser.firstElementChild(parser.rootNode(), "val");
            while (valNode != null) { //for each <val> node
               strVal = parser.getText(valNode);   //get the string from the <val></val>
               //create a child of the parent's type with this value
               newMember = (TSDMLValue)addScalar("", FBaseType);
               newMember.setValue(strVal);
               valNode = parser.nextElementSibling(valNode, "val");
            }  //next <val>
         }

      }
      //============================================================================
      /// <summary>
      /// Writes the SDML value as XML.
      /// </summary>
      /// <param name="attrInfo">The typed value to use.</param>
      /// <param name="indent">Indent spaces to use. -1 = no indent.</param>
      /// <param name="tab">Number of spaces in each tab</param>
      /// <returns>The XML for the SDML value.</returns>
      //============================================================================
       protected override String writeFieldInfo(TTypedValue attrInfo, int indent, int tab)
       {
           uint i;
           int oneIndent;
           int nextIndent;
           int startIndent;
           String CR = "";

           //determine how much to indent this description
           oneIndent = 0;
           startIndent = 0;
           if (indent > -1)
           {
               oneIndent = tab;
               startIndent = indent;
               CR = "\r\n";
           }
           String sIndent = new String(' ', startIndent);   //begin at this level
           nextIndent = indent + oneIndent;

           StringBuilder xml = new StringBuilder("");
           if (attrInfo.baseType() != TTypedValue.TBaseType.ITYPE_DEF)
               xml.Append(" kind=\"" + attrInfo.typeName() + "\"");
           if (attrInfo.isArray())
               xml.Append(" array=\"T\"");
           if ((attrInfo.units().Length > 0) && (attrInfo.units()[0] != '-'))
               xml.Append(" unit=\"" + attrInfo.units() + "\"");

           xml.Append(">" + CR);

           if (attrInfo.isScalar()) // Scalars - use a <val> element
           {
               xml.Append(sIndent + "<val>" + scalarString(attrInfo) + "</val>" + CR);
           }
           else
           {
               //now nest into the fields/elements
               for (i = 1; i <= attrInfo.count(); i++)
               {
                   if (attrInfo.isArray() && (attrInfo.baseType() != TTypedValue.TBaseType.ITYPE_DEF))
                   {
                       xml.Append(new String(' ', oneIndent) + "<val>" + scalarString(attrInfo.item(i)) + "</val>" + CR); // Scalar array, indented format
                   }
                   else if (attrInfo.isArray())                                          // All other arrays
                       xml.Append(sIndent + "<element"
                                   + writeFieldInfo(attrInfo.item(i), nextIndent, oneIndent)
                                   + sIndent + "</element>" + CR);
                   else if (attrInfo.isRecord())                                         // Records
                       xml.Append(sIndent + "<field name=\"" + attrInfo.item(i).Name + "\""
                                   + writeFieldInfo(attrInfo.item(i), nextIndent, oneIndent)
                                   + sIndent + "</field>" + CR);
               }
           }

           return xml.ToString();
       }
       //============================================================================
       /// <summary>
       /// Return the string for the scalar ttypedvalue.
       /// Numeric types are returned without any rounding or escaping.
       /// </summary>
       /// <param name="attrInfo">The scalar TTypedValue</param>
       /// <returns>String represenation of the scalar</returns>
       //============================================================================
       protected String scalarString(TTypedValue attrInfo)
       {
           String strVal;

           if ((attrInfo.baseType() >= TTypedValue.TBaseType.ITYPE_SINGLE) && (attrInfo.baseType() <= TTypedValue.TBaseType.ITYPE_DOUBLE))
           {
               strVal = attrInfo.asDouble().ToString(); //full precision
           }
           else
           {
               if ((attrInfo.baseType() >= TTypedValue.TBaseType.ITYPE_INT1) && (attrInfo.baseType() <= TTypedValue.TBaseType.ITYPE_INT8))
               {
                   strVal = attrInfo.asInt().ToString(); //no need to escape this   
               }
               else
               {
                   strVal = attrInfo.asEscapedString();
               }
           }
           return strVal;
       }
      //============================================================================
      /// <summary>
      /// Construct this SDML Value from the SDML in an XML file.
      /// </summary>
      /// <param name="fileName">Full path name of the file to open.</param>
      //============================================================================
      public TSDMLValue(String fileName) : base ("", "")
      {
         StreamReader sr = new StreamReader(fileName);
         string sXML = sr.ReadToEnd();
         sr.Close();

         //required in this derived class
         buildType(sXML);         //calls suitable virtual functions
      }
      //============================================================================
      /// <summary>
      /// Constructs a typed value using an xml description.
      /// </summary>
      /// <param name="sXML">XML text description.</param>
      /// <param name="sBaseType">Set the base type of this object.</param>
      //============================================================================
      public TSDMLValue(String sXML, String sBaseType) : base (sXML, sBaseType)
      {
         //required in this derived class
         buildType(sXML);         //calls suitable virtual functions
      }

      //============================================================================
      /// <summary>
      /// Creates a scalar of this aBaseType with szName.
      /// </summary>
      /// <param name="sName">Name of the scalar.</param>
      /// <param name="aBaseType">Base type of this scalar.</param>
      /// <seealso cref="TTypedValue">TTypedValue Class</seealso>
      //============================================================================
      public TSDMLValue(String sName, TBaseType aBaseType) : base (sName, aBaseType)
      {
         //required in this derived class
         //create a scalar type of TTypedValue
         constructScalar(sName, aBaseType); //calls suitable virtual functions
      }
      //============================================================================
      /// <summary>
      /// Creates a one dimensional array of scalar items.
      /// </summary>
      /// <param name="sArrayName">Name of this array.</param>
      /// <param name="aBaseType">Set the base type of this array.</param>
      /// <param name="iNoElements">Create it with this number of elements.</param>
      //============================================================================
      public TSDMLValue(String sArrayName, TBaseType aBaseType, int iNoElements)
               : base (sArrayName, aBaseType, iNoElements)
      {
         //required in this derived class
         //add array elements which are scalars
         addScalar("", aBaseType);     //calls suitable virtual function
         setElementCount((uint)iNoElements);
      }
      //============================================================================
      /// <summary>
      /// Creates a one dimensional array of arbitrary items.
      /// </summary>
      /// <param name="sArrayName">Name of this array.</param>
      /// <param name="baseValue">Use as the base type of the array elements.</param>
      /// <param name="iNoElements">Create it with this number of elements.</param>
      //============================================================================
      public TSDMLValue(String sArrayName, TTypedValue baseValue, int iNoElements)
               : base (sArrayName, baseValue, iNoElements)
      {
         newMember( baseValue );
         setElementCount((uint)iNoElements);
      }

      //============================================================================
      /// <summary>
      /// Construct this object using the parser already created in the parent. Also
      /// use the dom node, baseNode to be the root node of the document for this
      /// new typed value. Can also specify the base type using szBaseType.
      /// </summary>
      /// <param name="parentParser">Pointer to the parents parser.</param>
      /// <param name="baseNode">DOM node to use as the root node.</param>
      /// <param name="sBaseType">Used to set the base type.</param>
      //============================================================================
      public TSDMLValue(TSDMLParser parentParser, XmlNode baseNode, String sBaseType)
               : base (parentParser, baseNode, sBaseType)
      {
         //required in this derived class
         buildType(parentParser, baseNode);  //calls suitable virtual functions
      }
      //============================================================================
      /// <summary>
      /// Copy constructor. This constructor makes a copy of the source's structure.
      /// For specialised child classes, this constructor should be overriden.
      /// </summary>
      /// <param name="typedValue">Use this as the blue print type.</param>
      //============================================================================
      public TSDMLValue(TTypedValue typedValue)
               : base (typedValue)
      {
         //required in this derived class
         initTypeCopy(typedValue);  //calls suitable virtual functions
      }
      //============================================================================
      /// <summary>
      /// Uses the copy constructor to make a clone of a typedvalue's structure.
      /// It is then added as a member to an array or record.
      /// this virtual function is expected to be overriden so that new members are
      /// of the child classes' type.
      /// </summary>
      /// <param name="bluePrintValue">Use this typed value as the blue print.</param>
      //============================================================================
      public override void newMember(TTypedValue bluePrintValue)
      {
         TSDMLValue newElement;

         newElement = new TSDMLValue(bluePrintValue); //calls copy constructor
         addMember(newElement);  //add the copy
      }
      //============================================================================
      /// <summary>
      /// Used to add a scalar to a record or array
      /// </summary>
      /// <param name="sName">Name of the scalar value.</param>
      /// <param name="aType">Use this type.</param>
      /// <returns>The scalar value added.</returns>
      //============================================================================
      public override TTypedValue addScalar(String sName, TBaseType aType)
      {
         TSDMLValue newScalar;
         TTypedValue result = null;

         if (FIsArray || FIsRecord) {
               newScalar = new TSDMLValue(sName, aType);
               addMember(newScalar);
               result = newScalar;
         }
         return result;
      }
      //============================================================================
      /// <summary>
      /// Create a scalar value child.
      /// </summary>
      //============================================================================
      protected override void createScalar()
      {
         String sbuf;
         XmlNode valNode;

         base.createScalar();   //allocates memory for the type

         if (parser != null) {   //if this is being constructed from a parsed script
            //if this is a scalar then I will need to get the <val> tags for this type
            valNode = parser.rootNode();
            if (valNode != null) {
               sbuf = parser.getText(parser.firstElementChild(valNode, "val"));

               //if the value '' is returned for a numeric I should trap it here
               //but I would need a value to represent missing

               try {
                  setValue(sbuf);
               }
               catch {
                  throw (new ApplicationException("Cannot set this TSDMLValue scalar with a value."));
               }
            }
         }

      }
      //============================================================================
      /// <summary>
      /// Gets the typed value as an XML description. &lt;init&gt; &lt;/init&gt;
      /// </summary>
      /// <param name="value">The typed value to describe as XML.</param>
      /// <param name="startIndent">Formatting indentation start.</param>
      /// <param name="tab">Number of spaces in each tab</param>
      /// <returns>The string buffer with the result.</returns>
      //============================================================================
      public override String getText(TTypedValue value, int startIndent, int tab)
      {
         int nextIndent;

         if (startIndent > -1)
            nextIndent = startIndent + tab;
         else
            nextIndent = -1;

         if (startIndent < 0)
            startIndent = 0;

         String sIndent= new String(' ', startIndent);

         StringBuilder sbuf = new StringBuilder(sIndent);
         sbuf.Append("<init name=\"");
         sbuf.Append(value.Name);
         sbuf.Append("\"");
         sbuf.Append(writeFieldInfo(value, nextIndent, tab));
         sbuf.Append(sIndent);
         sbuf.Append("</init>");

         return sbuf.ToString();
      }
      //============================================================================
      /// <summary>
      /// Store the full SDML description to a UTF-8 XML file.
      /// </summary>
      /// <param name="fileName">File name to use.</param>
      /// <returns>True if saved.</returns>
      //============================================================================
      public bool saveToFile(string fileName) 
      {
         try 
         {
            StreamWriter writer;
            if (File.Exists(fileName)) 
            {
               writer = new StreamWriter(fileName);
            }
            else 
            {
               writer = File.CreateText(fileName);
            }
            writer.Write(getText(this, 0, 2));
            writer.Flush();
            writer.Close();
            return true;
         }
         catch (Exception) 
         {
            return false;
         }
      }
	}
}
