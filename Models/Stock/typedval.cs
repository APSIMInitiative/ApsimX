// -----------------------------------------------------------------------
// <copyright file="typedval.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace CMPServices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The main class that is used as the base class for structured types such as SDML and DDML values.
    /// </summary>
    public abstract class TTypedValue
    {
        /// <summary>
        /// Byte size of a four byte integer.
        /// </summary>
        public const uint INTSIZE = 4;   // size of Int32
        
        /// <summary>
        /// Byte sizes for the field types TBaseType.ITYPE_INT1 To TBaseType.ITYPE_WCHAR
        /// </summary>
        public static uint[] TypeSize = { 0, 1, 2, 4, 8, 4, 8, 1, 1, 2, 0 };

        /// <summary>
        /// Lookup table of type name strings. 
        /// </summary>
        /// <example>
        /// integer1, 
        /// integer2, 
        /// integer4, 
        /// single, 
        /// double,
        /// char,
        /// boolean,
        /// wchar,
        /// string,
        /// wstring,
        /// defined
        /// <code>
        /// string dblString = sTYPECODES[TBaseType.ITYPE_DOUBLE];
        /// </code>
        /// </example>
        public static string[] sTYPECODES = 
        {
                            string.Empty,           // Zero entry is unused
                           "integer1", // 1
                           "integer2", // 2
                           "integer4", // 3
                           "integer8", // 4
                           "single",   // 5
                           "double",   // 6
                           "char",     // 7
                           "boolean",  // 8
                           "wchar",    // 9
                           "string",   // 10
                           "wstring",  // 11
                           "defined"   // 12
        };
        
        /// <summary>
        /// The text name of a string type. "string"
        /// </summary>
        public static string STYPE_STR = sTYPECODES[(int)TBaseType.ITYPE_STR];
        
        /// <summary>
        /// The text name of a boolean type. "boolean"
        /// </summary>
        public static string STYPE_BOOL = sTYPECODES[(int)TBaseType.ITYPE_BOOL];
        
        /// <summary>
        /// The text name of a double type. "double"
        /// </summary>
        public static string STYPE_DOUBLE = sTYPECODES[(int)TBaseType.ITYPE_DOUBLE];
        
        /// <summary>
        /// The text name of an integer 4 type. "integer4"
        /// </summary>
        public static string STYPE_INT4 = sTYPECODES[(int)TBaseType.ITYPE_INT4];
        
        /// <summary>
        /// The text name of a defined type. "defined"
        /// </summary>
        public static string STYPE_DEF = sTYPECODES[(int)TBaseType.ITYPE_DEF];
        
        /// <summary>
        /// Return value from TTypedValue.isSameType()
        /// </summary>
        /// <seealso cref="TTypedValue">TTypedValue Class</seealso>
        public const int ctSAME = 0;
        
        /// <summary>
        /// Return value from TTypedValue.isSameType()
        /// </summary>
        /// <seealso cref="TTypedValue">TTypedValue Class</seealso>
        public const int ctCOMP = 1;
        
        /// <summary>
        /// Return value from TTypedValue.isSameType()
        /// </summary>
        /// <seealso cref="TTypedValue">TTypedValue Class</seealso>
        public const int ctDODGY = 2;
        
        /// <summary>
        /// Return value from TTypedValue.isSameType(). 
        /// </summary>
        /// <seealso cref="TTypedValue">TTypedValue Class</seealso>
        public const int ctBAD = -1;

        /// <summary>
        /// "cc/cc","mm/mm" are matching units.
        /// </summary>
        private static Unit match1 = new Unit("g/cm^3", "Mg/m^3");
        
        /// <summary>
        /// "m^3/m^3", "mm/mm" are matching units.
        /// </summary>
        private static Unit match2 = new Unit("m^3/m^3", "mm/mm");
        
        /// <summary>
        /// "ppm" and "mg/kg" are allowed to match, although "ppm" is invalid
        /// This is a concession to APSIM
        /// </summary>
        private static Unit match3 = new Unit("ppm", "mg/kg");

        /// <summary>
        /// "g/cc", "Mg/m^3" are matching units, although "cc" is invalid
        /// This is a concession to APSIM
        /// </summary>
        private static Unit match4 = new Unit("g/cc", "Mg/m^3");
        
        /// <summary>
        /// "0-1" and "-" match,  as both are dimensionless
        /// </summary>
        private static Unit match5 = new Unit("0-1", "-");
        
        /// <summary>
        /// "0-1" and "mm/mm" match, as both are effectively dimensionless
        /// </summary>
        private static Unit match6 = new Unit("0-1", "mm/mm");
        
        /// <summary>
        /// "cm^3/cm^3" and "mm/mm" match, as both are effectively dimensionless
        /// </summary>
        private static Unit match7 = new Unit("cm^3/cm^3", "mm/mm");
        
        /// <summary>
        /// "0-1" and "m^3/m^3" match, as both are effectively dimensionless
        /// </summary>
        private static Unit match8 = new Unit("0-1", "m^3/m^3");
        
        /// <summary>
        /// "0-1" and "m^2/m^2" match, as both are effectively dimensionless
        /// </summary>
        private static Unit match9 = new Unit("0-1", "m^2/m^2");
                
        private TTypedValue childTemplate;      // !<used to keep a pointer to the last element after setElementCount(0)
        
        /// <summary>
        /// Name of the typed value.
        /// </summary>
        protected string FName;
        
        /// <summary>
        /// Unit of the typed value.
        /// </summary>
        protected string FUnit;
        
        /// <summary>
        /// Store the base type as an integer.
        /// </summary>
        protected TBaseType FBaseType;
        
        /// <summary>
        /// True if a scalar.
        /// </summary>
        protected bool FIsScalar;
        
        /// <summary>
        /// True if an array.
        /// </summary>
        protected bool FIsArray;
        
        /// <summary>
        /// True if a record.
        /// </summary>
        protected bool FIsRecord;
        
        /// <summary>
        /// Block of bytes containing field/element values
        /// </summary>
        protected byte[] FData;
        
        /// <summary>
        /// Size in bytes of the memory block holding the value data
        /// </summary>
        protected uint FDataSize;
        
        /// <summary>
        /// List of TTypedValues that are fld or elem children
        /// </summary>
        protected List<TTypedValue> FMembers;
        
        /// <summary>
        /// Each typed value uses a parser at creation
        /// </summary>
        protected SDMLParser parser;

        /// <summary>
        /// Encoding object
        /// </summary>
        private System.Text.ASCIIEncoding ascii;

        /// <summary>
        /// Count of scalar types available in a TTypedValue.
        /// </summary>
        public const int NUMSCALARTYPES = 9;

        /// <summary>
        /// The type of the TTypedValue expressed as a simple int.
        /// See <see cref="BaseType">baseType()</see>
        /// See <see cref="sTYPECODES"/>
        /// </summary>
        public enum TBaseType
        {
            /// <summary>
            /// Not a type.
            /// </summary>
            ITYPE_EMPTY = 0,

            /// <summary>
            /// Single byte integer.
            /// </summary>
            ITYPE_INT1,

            /// <summary>
            /// Two byte integer.
            /// </summary>
            ITYPE_INT2,

            /// <summary>
            /// Four byte signed integer.
            /// </summary>
            ITYPE_INT4,

            /// <summary>
            /// Eight byte signed integer.
            /// </summary>
            ITYPE_INT8,

            /// <summary>
            /// Single precision float. 4 bytes.
            /// </summary>
            ITYPE_SINGLE,

            /// <summary>
            /// Double precision float. 8 bytes.
            /// </summary>
            ITYPE_DOUBLE,

            /// <summary>
            /// Character type.
            /// </summary>
            ITYPE_CHAR,

            /// <summary>
            /// Boolean type.
            /// </summary>
            ITYPE_BOOL,

            /// <summary>
            /// Two byte char.
            /// </summary>
            ITYPE_WCHAR,

            /// <summary>
            /// Character string.
            /// </summary>
            ITYPE_STR,

            /// <summary>
            /// Two byte char string.
            /// </summary>
            ITYPE_WSTR,

            /// <summary>
            /// Defined type such as a record.
            /// </summary>
            ITYPE_DEF
        }

        /// <summary>
        /// Array of the matching units- match1, match2,...
        /// </summary>
        private static Unit[] UNITMATCHES = { match1, match2, match3, match4, match5, match6, match7, match8, match9 };
        
        /// <summary>
        /// Get the integer value of the dimension bytes. Used for arrays
        /// and strings 
        /// </summary>
        /// <param name="data">The block of byte data.</param>
        /// <param name="startIndex">Start at this index. 0 -> x</param>
        /// <returns>Returns the dimension of the array/string</returns>
        protected static uint GetDimension(byte[] data, uint startIndex)
        {
            return (uint)(data[startIndex] + (data[startIndex + 1] << 8) + (data[startIndex + 2] << 16) + (data[startIndex + 3] << 24));
        }

        /// <summary>
        /// Finds children nodes in the xml doc.
        /// </summary>
        protected abstract void GetFldElemList();
        
        /// <summary>
        /// Add a new member.
        /// </summary>
        /// <param name="bluePrintValue">The typedvalue to clone</param>
        public abstract void NewMember(TTypedValue bluePrintValue);
        
        /// <summary>
        /// Add a scalar value.
        /// </summary>
        /// <param name="scalarName">Name of the scalar</param>
        /// <param name="scalarType">Type of the scalar</param>
        /// <returns>The new scalar</returns>
        public abstract TTypedValue AddScalar(string scalarName, TBaseType scalarType);
        
        /// <summary>
        /// Writes a field as a string
        /// </summary>
        /// <param name="attrInfo">The value</param>
        /// <param name="indent">Indentation 0-n</param>
        /// <param name="tab">Number of spaces in each tab</param>
        /// <returns>The XML for the typed value.</returns>
        protected abstract string WriteFieldInfo(TTypedValue attrInfo, int indent, int tab);
        
        /// <summary>
        /// Text representation of a TTypedValue.
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="startIndent">Indent from here.</param>
        /// <param name="tab">Number of spaces in each tab</param>
        /// <returns>The XML.</returns>
        public abstract string GetText(TTypedValue value, int startIndent, int tab);

        /// <summary>
        /// Constructs a typed value using an XML description.
        /// </summary>
        /// <param name="xmlStr">XML text description.</param>
        /// <param name="baseTypeStr">Set the base type of this object. See <see cref="sTYPECODES"/></param>
        /// N.Herrmann Apr 2002
        public TTypedValue(string xmlStr, string baseTypeStr)
        {
            this.ascii = new System.Text.ASCIIEncoding();

            this.FMembers = new List<TTypedValue>();

            // set the kind of this typed value
            this.SetBaseType(baseTypeStr);

            this.parser = null;
            this.FData = null;
            this.FDataSize = 0;
            this.childTemplate = null;
            this.FUnit = string.Empty;

            // Called in the derived classes because it calls virtual functions
            // buildType(szXML);
        }

        /// <summary>
        /// Construct this object using the parser already created in the parent. Also
        /// use the dom node, baseNode to be the root node of the document for this
        /// new typed value. Can also specify the base type using sBaseType.
        /// </summary>
        /// <param name="parentParser">Pointer to the parents parser.</param>
        /// <param name="baseNode">DOM node to use as the root node.</param>
        /// <param name="baseTypeStr">Used to set the base type.  See <see cref="sTYPECODES"/></param>
        /// N.Herrmann Apr 2002
        public TTypedValue(SDMLParser parentParser, XmlNode baseNode, string baseTypeStr)
        {
            this.ascii = new System.Text.ASCIIEncoding();

            this.FMembers = new List<TTypedValue>();

            // set the kind of this typed value
            this.SetBaseType(baseTypeStr);

            this.parser = null;
            this.FData = null;
            this.FDataSize = 0;
            this.childTemplate = null;
            this.FUnit = string.Empty;

            // Called in the derived classes because it calls virtual functions
            // buildType(parentParser, baseNode);
        }

        /// <summary>
        /// Creates a scalar of this aBaseType with sName.
        /// </summary>
        /// <param name="scalarName">Name of the scalar.</param>
        /// <param name="scalarBaseType">Base type of this scalar.</param>
        /// N.Herrmann Apr 2002
        public TTypedValue(string scalarName, TBaseType scalarBaseType)
        {
            this.ascii = new System.Text.ASCIIEncoding();

            this.FMembers = new List<TTypedValue>();

            // set the kind of this typed value
            this.FBaseType = scalarBaseType;

            // Called in the derived classes because it calls virtual functions
            // constructScalar(szName, iBaseType);  //create a scalar type of TTypedValue
            this.parser = null;
            this.childTemplate = null;
            this.FUnit = string.Empty;
        }

        /// <summary>
        /// Creates a one dimensional array of scalar items.
        /// </summary>
        /// <param name="arrayName">Name of this array.</param>
        /// <param name="arrayBaseType">Set the base type of this array.</param>
        /// <param name="numElements">Create it with this number of elements.</param>
        /// N.Herrmann Apr 2002
        public TTypedValue(string arrayName, TBaseType arrayBaseType, int numElements)
        {
            this.ascii = new System.Text.ASCIIEncoding();

            this.FMembers = new List<TTypedValue>();

            // set the kind of this typed value
            this.FBaseType = arrayBaseType;

            this.parser = null;
            this.FData = null;
            this.FDataSize = 0;
            this.childTemplate = null;

            this.Name = arrayName;
            this.FUnit = string.Empty;
            this.FIsScalar = false;
            this.FIsArray = true;
            this.FIsRecord = false;

            // Called in the derived classes because they call virtual functions
            // addScalar("", iBaseType);     //calls suitable virtual function
            // setElementCount(iNoElements);
        }
        
        /// <summary>
        /// Creates a 1-dimensional array of arbitrary type
        /// baseValue is used as a blue print only.
        /// </summary>
        /// <param name="arrayName">Name of the array.</param>
        /// <param name="baseValue">Blue print typed value.</param>
        /// <param name="numElements">Number of elements for the array.</param>
        public TTypedValue(string arrayName, TTypedValue baseValue, int numElements)
        {
            this.ascii = new System.Text.ASCIIEncoding();

            this.FMembers = new List<TTypedValue>();

            // set the kind of this typed value
            this.FBaseType = baseValue.FBaseType;

            this.parser = null;
            this.FData = null;
            this.FDataSize = 0;
            this.childTemplate = null;
            this.FUnit = string.Empty;
        }

        /// <summary>
        /// Copy constructor. This constructor makes a copy of the source's structure.
        /// For specialised child classes, this constructor should be overriden.
        /// </summary>
        /// <param name="typedValue">Use this typed value as the source.</param>
        /// N.Herrmann Apr 2002
        public TTypedValue(TTypedValue typedValue)
        {
            this.ascii = new System.Text.ASCIIEncoding();

            this.FMembers = new List<TTypedValue>();

            // set the kind of this typed value
            this.FBaseType = typedValue.FBaseType;

            this.FData = null;
            this.FDataSize = 0;
            this.parser = null; // won't be using a parser here
            this.childTemplate = null;
            this.FUnit = string.Empty;

            // Called in the derived classes because it calls virtual functions
            // initTypeCopy(typedValue)
        }
        
        /// <summary>
        /// Finds the array item or field corresponding to the given index.
        /// </summary>
        /// <param name="index">Index of the member of this typed value. 1 -> x</param>
        /// <returns>The typed value.</returns>
        /// N.Herrmann Apr 2002
        public TTypedValue member(uint index)
        {
            return this.Item(index);
        }
        
        /// <summary>
        /// Finds the record field corresponding to the given name.
        /// </summary>
        /// <param name="fieldName">Name of the field to find.</param>
        /// <returns>The typed value found.</returns>
        /// N.Herrmann Apr 2002
        public TTypedValue member(string fieldName)
        {
            TTypedValue foundMember = null;
            TTypedValue _item;

            if (!this.FIsRecord)
                throw new TypeMisMatchException("Cannot access named members for scalar or array");

            uint i = 1;
            while ((foundMember == null) && (i <= this.FMembers.Count))
            {
                _item = this.Item(i);
                if (_item.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    foundMember = _item;
                else
                    i++;
            }
            return foundMember;
        }

        /// <summary>
        /// Searches an array of records and attempts to match a specified field with
        /// a value. When a case insensitive match is found then return the array item.
        /// </summary>
        /// <param name="memberName">The field name of the record</param>
        /// <param name="value">The string value of the field</param>
        /// <returns>The record item from the array of records</returns>
        //// N.Herrmann Jan 2016
        public TTypedValue FindItemByMemberValue(string memberName, string value)
        {
            TTypedValue foundMember = null;
            TTypedValue _item;

            if (this.FIsArray)
            {
                if ((this.FMembers.Count > 0) && this.FMembers[0].HasField(memberName))
                {
                    int i = 0;
                    while ((foundMember == null) && (i <= this.FMembers.Count - 1))
                    {
                        _item = this.FMembers[i]; 
                        if (string.Compare(_item.member(memberName).AsStr(), value, StringComparison.OrdinalIgnoreCase) == 0)
                            foundMember = _item;
                        else
                            i++;
                    }
                }
            }

            return foundMember;
        }

        // Common code for the constructors
        // Some of these functions call virtual functions, so they are called
        // in the derived classes.
        
        /// <summary>
        /// Sets the FBaseType class type.
        /// </summary>
        /// <param name="baseTypeStr">The base type string. See <see cref="sTYPECODES"/></param>
        /// N.Herrmann Apr 2002
        protected void SetBaseType(string baseTypeStr)
        {
            if (baseTypeStr != null && (baseTypeStr.Length > 0))
            {
                this.FBaseType = TBaseType.ITYPE_DEF;
                while ((this.FBaseType > TBaseType.ITYPE_EMPTY) && (baseTypeStr != sTYPECODES[(int)this.FBaseType]))
                    this.FBaseType--;
            }
            else
                this.FBaseType = TBaseType.ITYPE_EMPTY;
        }

        /// <summary>
        /// Do the parsing of this type. If it is a structured type, then it will
        /// attempt to find all the children. Called during the construction process.
        /// </summary>
        /// N.Herrmann Apr 2002
        protected void ParseType()
        {
            if (this.FIsScalar)                                     // decide here if this is a scalar and whether to get fields
                this.CreateScalar();
            else
                this.GetFldElemList();                              // get the fields/elements

            if ((this.FBaseType == TBaseType.ITYPE_EMPTY) && this.FIsArray)
            {
                this.FBaseType = this.FindArrayType(this);               // retrieve base type from a child
            }
            else if ((this.FBaseType == TBaseType.ITYPE_DEF) && !this.FIsArray)
            {
                this.FIsRecord = true;
            }
        }

        /// <summary>
        /// Loads the description of this typed value from the parsed xml text.
        /// Assume that parser.getDescription() has been called.
        /// </summary>
        /// N.Herrmann Apr 2002
        protected void GetDescription()
        {
            this.FName = this.parser.Name;
            this.FUnit = this.parser.Units;
            if (this.FBaseType == TBaseType.ITYPE_EMPTY)
            {
                this.FBaseType = TBaseType.ITYPE_DEF;
                if (this.parser.Kind.Length > 0)
                {
                    while ((this.FBaseType > TBaseType.ITYPE_EMPTY) && (this.parser.Kind != sTYPECODES[(int)this.FBaseType]))
                        this.FBaseType--;
                }
            }

            this.FIsScalar = this.parser.IsScalar;
            this.FIsArray = this.parser.IsArray;
            this.FIsRecord = this.parser.IsRecord;
        }
        
        /// <summary>
        /// Contains common code used by the constructors to set the field values of this
        /// type when it is a scalar.
        /// </summary>
        /// <param name="scalarName">Name of the scalar.</param>
        /// <param name="baseType">The type for this scalar.</param>
        /// N.Herrmann Apr 2002
        protected void ConstructScalar(string scalarName, TBaseType baseType)
        {
            this.FBaseType = baseType;
            this.FData = null;
            //// FDataSize = 0;

            this.Name = scalarName;
            this.FIsScalar = true;
            this.FIsArray = false;
            this.FIsRecord = false;
            this.SetUnits(string.Empty);

            this.CreateScalar();              // allocates memory and initialises
        }
        
        /// <summary>
        /// Allocates memory for this scalar and sets it's initial value.
        /// </summary>
        /// N.Herrmann Apr 2002
        protected virtual void CreateScalar()
        {
            this.FDataSize = 0;

            // allocate memory for this type
            if ((this.FBaseType >= TBaseType.ITYPE_INT1) && (this.FBaseType <= TBaseType.ITYPE_WSTR))
            {
                if ((this.FBaseType == TBaseType.ITYPE_STR) || (this.FBaseType == TBaseType.ITYPE_WSTR))
                {
                    this.FDataSize = INTSIZE;         // strings have a header to specify the length
                    // create the header so it is always available
                    this.FData = new byte[this.FDataSize];
                    this.FData[0] = 0;  // no characters yet
                    this.FData[1] = 0;
                    this.FData[2] = 0;
                    this.FData[3] = 0;
                }
                else
                {
                    this.FDataSize = TypeSize[(int)this.FBaseType];
                    this.FData = new byte[this.FDataSize];
                    this.SetValue(0);                  // init this scalar to 0
                }
            }

            // strings will use their own memory allocation routines to add characters
        }
        
        /// <summary>
        /// The value returned by count() depends on the type of the value, as follows:
        /// <para>For a <b>record</b>, it is the number of members in the record</para>
        /// <para>For a <b>string</b>, it is the number of characters</para>
        /// <para>For a simple <b>scalar</b>, it is zero</para>
        /// </summary>
        /// <returns>The count of elements.</returns>
        /// N.Herrmann Apr 2002
        public uint Count()
        {
            uint icount;

            if (this.FIsScalar && ((this.FBaseType == TBaseType.ITYPE_STR) || (this.FBaseType == TBaseType.ITYPE_WSTR)))   // String - return the string length
                icount = GetDimension(this.FData, 0);
            else if (this.FIsRecord || this.FIsArray)               // Collection - return number of elements
                icount = (uint)this.FMembers.Count;
            else                                                    // Simple scalar
                icount = 0;

            return icount;
        }

        /// <summary>
        /// Finds the type of this array object by recursing into the lower dimesions
        /// if needed.
        /// </summary>
        /// <param name="typedValue">The typed value to interogate.</param>
        /// <returns>The base type for this variable.  </returns>
        /// N.Herrmann Apr 2002
        protected TBaseType FindArrayType(TTypedValue typedValue)
        {
            TTypedValue value;
            TBaseType baseType;

            baseType = TBaseType.ITYPE_EMPTY;                       // default

            value = typedValue.Item(1);                             // first element
            if (value != null)
            {
                baseType = value.BaseType();
                if (baseType == TBaseType.ITYPE_EMPTY)
                    baseType = this.FindArrayType(value);
            }
            return baseType;
        }
        
        /// <summary>
        /// Set the units of the array elements
        /// </summary>
        /// <param name="unitStr">The units string.</param>
        public void SetUnits(string unitStr)
        {
            if ((this.FBaseType >= TBaseType.ITYPE_INT1) && (this.FBaseType <= TBaseType.ITYPE_DOUBLE))
            {   // if number type
                if (this.FIsScalar || this.FIsArray)
                    this.FUnit = unitStr;
                uint itemCount = this.Count();
                if (this.FIsArray && (itemCount > 0))
                {            // if has array elements
                    for (uint i = 1; i < itemCount; i++)
                        this.member(i).SetUnits(unitStr);
                }
                else
                    if (this.FIsArray && (itemCount == 0) && (this.member(0) != null))         // else set the 0 element
                        this.member(0).SetUnits(unitStr);
            }
        }
        
        /// <summary>
        /// Get the base type of the typed value. See <see cref="TBaseType"/>
        /// </summary>
        /// <returns>The base type.</returns>
        public TBaseType BaseType()
        {
            return this.FBaseType;
        }

        /// <summary>
        /// Contains two unit fields. Used in the array of matching units.
        /// </summary>
        private struct Unit
        {
            /// <summary>
            /// First unit
            /// </summary>
            public string Unit1;

            /// <summary>
            /// Second unit
            /// </summary>
            public string Unit2;

            /// <summary>
            /// Construct a unit class
            /// </summary>
            /// <param name="u1">Unit one</param>
            /// <param name="u2">Unit two</param>
            public Unit(string u1, string u2)
            {
                this.Unit1 = u1;
                this.Unit2 = u2;
            }
        }
        
        /// <summary>
        /// Gets or sets the name of the typed value.
        /// </summary>
        public string Name
        {
            get { return this.FName; }
            set { this.FName = value; }
        }
        
        /// <summary>
        /// Get the units string.
        /// </summary>
        /// <returns>Unit string</returns>
        public string Units()
        {
            return this.FUnit;
        }
        
        /// <summary>
        /// True is this is a scalar.
        /// </summary>
        /// <returns>True if scalar</returns>
        public bool IsScalar()
        {
            return this.FIsScalar;
        }
        
        /// <summary>
        /// True if this is an array.
        /// </summary>
        /// <returns>True if an array</returns>
        public bool IsArray()
        {
            return this.FIsArray;
        }
        
        /// <summary>
        /// True if this is a record.
        /// </summary>
        /// <returns>True if record</returns>
        public bool IsRecord()
        {
            return this.FIsRecord;
        }
        
        /// <summary>
        /// Tests if this is a character type of scalar.
        /// </summary>
        /// <returns>True if this is a scalar of a non number type (text).</returns>
        public bool IsTextType()
        {
            bool isText = false;
            if (this.IsScalar())
            {
                if ((this.BaseType() == TBaseType.ITYPE_STR) ||        // if char types
                        (this.BaseType() == TBaseType.ITYPE_WCHAR) ||
                        (this.BaseType() == TBaseType.ITYPE_WSTR) ||
                        (this.BaseType() == TBaseType.ITYPE_CHAR))
                    isText = true;
            }
            return isText;
        }
        
        /// <summary>
        /// Set the values in the array.
        /// </summary>
        /// <param name="values">Array of scalar values.</param>
        /// <returns>True if this is successful: This is an array of scalars and each
        /// item has been set.</returns>
        public bool SetValue(double[] values)
        {
            bool result = false;
            if (this.FIsArray && (this.FBaseType != TBaseType.ITYPE_DEF) && (values != null))
            {
                result = true;
                this.SetElementCount((uint)values.Length);
                for (uint i = 0; i < values.Length; i++)
                    result = result && this.Item(i + 1).SetValue(values[i]);
            }
            return result;
        }
        
        /// <summary>
        /// Set the values in the array.
        /// </summary>
        /// <param name="values">Array of scalar values.</param>
        /// <returns>True if this is successful: This is an array of scalars and each
        /// item has been set.</returns>
        public bool SetValue(int[] values)
        {
            bool result = false;
            if (this.FIsArray && (this.FBaseType != TBaseType.ITYPE_DEF) && (values != null))
            {
                result = true;
                this.SetElementCount((uint)values.Length);
                for (uint i = 0; i < values.Length; i++)
                    result = result && this.Item(i + 1).SetValue(values[i]);
            }
            return result;
        }
        
        /// <summary>
        /// Set the values in the array.
        /// </summary>
        /// <param name="values">Array of scalar values.</param>
        /// <returns>True if this is successful: This is an array of scalars and each
        /// item has been set.</returns>
        public bool SetValue(float[] values)
        {
            bool result = false;
            if (this.FIsArray && (this.FBaseType != TBaseType.ITYPE_DEF) && (values != null))
            {
                result = true;
                this.SetElementCount((uint)values.Length);
                for (uint i = 0; i < values.Length; i++)
                    result = result && this.Item(i + 1).SetValue(values[i]);
            }
            return result;
        }
        
        /// <summary>
        /// Set the values in the array.
        /// </summary>
        /// <param name="values">Array of scalar values.</param>
        /// <returns>True if this is successful: This is an array of scalars and each
        /// item has been set.</returns>
        public bool SetValue(bool[] values)
        {
            bool result = false;
            if (this.FIsArray && (this.FBaseType != TBaseType.ITYPE_DEF) && (values != null))
            {
                result = true;
                this.SetElementCount((uint)values.Length);
                for (uint i = 0; i < values.Length; i++)
                    result = result && this.Item(i + 1).SetValue(values[i]);
            }
            return result;
        }
        
        /// <summary>
        /// Set the values in the array.
        /// </summary>
        /// <param name="values">Array of scalar values.</param>
        /// <returns>True if this is successful: This is an array of scalars and each
        /// item has been set.</returns>
        public bool SetValue(string[] values)
        {
            bool result = false;
            if (this.FIsArray && (this.FBaseType == TBaseType.ITYPE_STR) && (values != null))
            {
                result = true;
                this.SetElementCount((uint)values.Length);
                for (uint i = 0; i < values.Length; i++)
                    result = result && this.Item(i + 1).SetValue(values[i]);
            }
            return result;
        }
        
        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(double value)
        {
            bool result = false;

            if (this.FIsScalar)
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8:
                        {
                            long intValue;
                            if (value < 0)
                                intValue = (long)Math.Ceiling(value);
                            else
                                intValue = (long)Math.Floor(value);
                            this.SetValue(intValue);
                        }
                        break;
                    case TBaseType.ITYPE_SINGLE:
                        { // 4 byte
                            if (value <= float.MaxValue)
                                this.FData = BitConverter.GetBytes(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                        }
                        break;
                    case TBaseType.ITYPE_DOUBLE:
                        {
                            this.FData = BitConverter.GetBytes(value);
                        }
                        break;
                }
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(long value)
        {
            bool result = false;

            if (this.FIsScalar)
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_INT1:
                        {
                            if (value <= sbyte.MaxValue)
                                this.FData[0] = BitConverter.GetBytes(value)[0];
                            else
                                return false;
                        }
                        break;
                    case TBaseType.ITYPE_INT2:
                        {
                            if (value <= short.MaxValue)
                            {
                                this.FData[0] = BitConverter.GetBytes(value)[0];
                                this.FData[1] = BitConverter.GetBytes(value)[1];
                            }
                            else
                                return false;
                        }
                        break;
                    case TBaseType.ITYPE_INT4:
                        {
                            if (value <= int.MaxValue)
                            {
                                this.FData[0] = BitConverter.GetBytes(value)[0];
                                this.FData[1] = BitConverter.GetBytes(value)[1];
                                this.FData[2] = BitConverter.GetBytes(value)[2];
                                this.FData[3] = BitConverter.GetBytes(value)[3];
                            }
                            else
                                return false;
                        }
                        break;
                    case TBaseType.ITYPE_INT8:
                        {
                            this.FData = BitConverter.GetBytes(value);
                        }
                        break;
                    case TBaseType.ITYPE_SINGLE: this.SetValue((double)value);
                        break;
                    case TBaseType.ITYPE_DOUBLE: this.SetValue((double)value);
                        break;
                }
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(float value)
        {
            return this.SetValue((double)value);
        }

        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(int value)
        {
            return this.SetValue((long)value);
        }

        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(bool value)
        {
            bool result = false;

            if (this.FIsScalar)
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_BOOL:
                        this.FData = BitConverter.GetBytes(value);
                        break;
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8:
                    case TBaseType.ITYPE_SINGLE:
                    case TBaseType.ITYPE_DOUBLE:
                        {
                            if (value) this.SetValue(1);
                            else this.SetValue(0);
                        }
                        break;
                    case TBaseType.ITYPE_STR:
                    case TBaseType.ITYPE_WSTR:
                        {
                            if (value)
                                this.SetValue("true");
                            else
                                this.SetValue("false");
                        }
                        break;
                }
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(string value)
        {
            bool result = false;

            if (this.FIsScalar)
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_BOOL:
                        {
                            if ((value.Length > 0) && (char.ToLower(value[0]) == 't'))
                                this.SetValue(true);
                            else this.SetValue(false);
                        }
                        break;
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8: 
                        if (value.Length > 0)
                            this.SetValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                        break;
                    case TBaseType.ITYPE_SINGLE:
                    case TBaseType.ITYPE_DOUBLE:
                        {
                            if (value.Length > 0)
                                this.SetValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                            else
                                this.SetValue(0);
                        }
                        break;
                    case TBaseType.ITYPE_CHAR:
                    case TBaseType.ITYPE_WCHAR:
                        this.SetValue(value[0]);
                        break;
                    case TBaseType.ITYPE_STR:
                        {    
                            // single byte characters
                            uint byteCount = (uint)value.Length;
                            this.FDataSize = INTSIZE + byteCount;
                            this.FData = new byte[this.FDataSize];
                            this.FData[0] = (byte)((uint)value.Length);
                            this.FData[1] = (byte)(((uint)value.Length) >> 8);
                            this.FData[2] = (byte)(((uint)value.Length) >> 16);
                            this.FData[3] = (byte)(((uint)value.Length) >> 24);

                            this.ascii.GetBytes(value, 0, value.Length, this.FData, 4); // copy the unicode chars to single bytes
                        }
                        break;
                    case TBaseType.ITYPE_WSTR:
                        {    
                            // double byte unicode characters
                            System.Text.UnicodeEncoding uni = new System.Text.UnicodeEncoding();
                            uint byteCount = (uint)uni.GetByteCount(value);
                            this.FDataSize = INTSIZE + byteCount;
                            this.FData = new byte[this.FDataSize];
                            this.FData[0] = (byte)((uint)value.Length);
                            this.FData[1] = (byte)(((uint)value.Length) >> 8);
                            this.FData[2] = (byte)(((uint)value.Length) >> 16);
                            this.FData[3] = (byte)(((uint)value.Length) >> 24);

                            uni.GetBytes(value, 0, value.Length, this.FData, 4); // copy the unicode chars to double bytes
                        }
                        break;
                }
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Sets the value for this scalar.
        /// </summary>
        /// <param name="value">The value to set this scalar to.</param>
        /// <returns>True if successful.</returns>
        public bool SetValue(char value)
        {
            bool result = false;

            if (this.FIsScalar)
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_BOOL:
                        this.SetValue(value.ToString());
                        break;
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8: this.SetValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                        break;
                    case TBaseType.ITYPE_SINGLE:
                    case TBaseType.ITYPE_DOUBLE:
                        this.SetValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                        break;
                    case TBaseType.ITYPE_CHAR:
                        {
                            // single byte char
                            this.FData = this.ascii.GetBytes(value.ToString());
                        }
                        break;
                    case TBaseType.ITYPE_WCHAR:
                        {
                            // double byte unicode char
                            this.FData = BitConverter.GetBytes(value);
                        }
                        break;
                    case TBaseType.ITYPE_STR:
                    case TBaseType.ITYPE_WSTR:
                        this.SetValue(value.ToString());
                        break;
                }
                result = true;
            }
            return result;
        }
        
        /// <summary>
        /// Assignment from a TTypedValue that need not be of identical type, but must   
        /// be type-compatible.
        /// When converting from a scalar string to a numeric an exception will be thrown
        /// if the source string is not a valid numeric.
        /// </summary>
        /// <param name="srcValue">The source typed value.</param>
        /// <returns>True is the value can be set.</returns>
        public bool SetValue(TTypedValue srcValue)
        {
            bool result = false;
            bool isCompatible;

            if (srcValue != null)
            {
                isCompatible = ((this.FIsScalar == srcValue.IsScalar()) && (this.FIsArray == srcValue.IsArray()));
                if (this.FBaseType == TBaseType.ITYPE_DEF)
                {
                    isCompatible = (isCompatible && (srcValue.BaseType() == TBaseType.ITYPE_DEF));
                }      
                if (!isCompatible)
                {
                    string error = string.Format("Incompatible assignment from {0} to {1}\nCannot convert {2} to {3}", this.Name, srcValue.Name, srcValue.BaseType().ToString(), this.FBaseType.ToString());
                    throw new TypeMisMatchException(error);
                }
                if (this.FIsScalar)
                {
                    try
                    {
                        switch (this.FBaseType)
                        {
                            case TBaseType.ITYPE_INT1:
                            case TBaseType.ITYPE_INT2:
                            case TBaseType.ITYPE_INT4:
                                {
                                    result = this.SetValue(srcValue.AsInt());
                                    break;
                                }
                            case TBaseType.ITYPE_INT8:
                                result = this.SetValue(Convert.ToInt64(srcValue.AsDouble(), CultureInfo.InvariantCulture));
                                break;
                            case TBaseType.ITYPE_DOUBLE:
                                {
                                    result = this.SetValue(srcValue.AsDouble());
                                    break;
                                }
                            case TBaseType.ITYPE_SINGLE:
                                {
                                    result = this.SetValue(srcValue.AsSingle());
                                    break;
                                }
                            case TBaseType.ITYPE_BOOL:
                                {
                                    result = this.SetValue(srcValue.AsBool());
                                    break;
                                }
                            default:
                                {
                                    result = this.SetValue(srcValue.AsStr());
                                    break;
                                }
                        }
                    }
                    catch
                    {
                        throw new Exception("setValue() cannot convert " + srcValue.AsStr() + " to " + this.FBaseType.ToString()); 
                    }
                }
                else
                {
                    if (this.FIsArray)
                    {
                        this.SetElementCount(srcValue.Count());
                    }
                    uint itemCount = this.Count();
                    for (uint idx = 1; idx <= itemCount; idx++)
                    {
                        result = this.Item(idx).SetValue(srcValue.Item(idx));
                    }
                }
            }
            return result;
        }
        
        /// <summary>
        /// Uses the xml text to build the type. Called by the descendant constructor.
        /// </summary>
        /// <param name="xmlStr">XML text description.</param>
        /// N.Herrmann Apr 2002
        protected void BuildType(string xmlStr)
        {
            this.parser = new SDMLParser(xmlStr);    // create a parser, also reads description fields
            this.GetDescription();                  // set the description fields from the parser

            this.ParseType();                       // do the parsing of this type 
        }
        
        /// <summary>
        /// Uses the parents parser and the base node for this this type to build the
        /// type. Called by the descendant constructor.
        /// </summary>
        /// <param name="parentParser">Pointer to the parents parser.</param>
        /// <param name="baseNode">DOM Node to use as the root node.</param>
        /// N.Herrmann Apr 2002
        protected void BuildType(SDMLParser parentParser, XmlNode baseNode)
        {
            XmlNode parentsNode;

            this.parser = parentParser;              // use the parent's parser
            parentsNode = parentParser.RootNode(); // store

            this.parser.SetTopNode(baseNode);
            this.parser.getDescription();            // gets the values from the dom into the parser fields
            this.GetDescription();                   // set the description fields from the parser
            this.ParseType();                        // do the parsing of this type 

            this.parser = null;                      // has only been borrowed
            parentParser.SetTopNode(parentsNode);   // restore the topElement of the parser
        }
        
        /// <summary>
        /// Finds the array item or field corresponding to the given index.
        /// </summary>
        /// <param name="index">Index of the member of this typed value. 1 -> x</param>
        /// <returns>The typed value. null if not found.</returns>
        /// N.Herrmann Apr 2002
        public TTypedValue Item(uint index)
        {
            TTypedValue result;

            result = null;
            if (this.FIsScalar && (index == 1))                     // Item(1) for a scalar is the scalar itself
                result = this;
            else if ((index <= this.FMembers.Count) && (index > 0)) // records and arrays
                result = this.FMembers[(int)index - 1];             // N.B. 1-offset indexing
            else if (this.FIsArray && (index == 0))
                if (this.childTemplate != null)
                    result = this.childTemplate;
                else if (this.FMembers.Count > 0)
                    result = this.FMembers[0];

            return result;
        }
        
        /// <summary>
        /// Delete an element from an array. Assumes that 'index' is the natural order
        /// of the items in the FMembers list.
        /// </summary>
        /// <param name="index">Array index 1->x</param>
        /// N.Herrmann Feb 2003
        public void DeleteElement(int index)
        {
            if (this.FIsArray)
            {
                if ((this.FMembers.Count > 0) && (this.FMembers.Count >= index))
                {
                    // delete some elements (newsize>=0)
                    if (this.FMembers.Count == 1)
                        this.childTemplate = this.FMembers[index - 1];   // store the last element locally for cloning later
                    this.FMembers.RemoveAt(index - 1);              // delete it from the list
                }
            }
        }
        
        /// <summary>
        /// For arrays, this will adjust the size of the FMembers list.
        /// </summary>
        /// <param name="dim">New dimension of this array.</param>
        /// N.Herrmann Apr 2002
        public void SetElementCount(uint dim)
        {
            if (this.FIsArray)
            {
                if (dim > this.FMembers.Count)
                {
                    // add some more elements
                    while (dim > this.FMembers.Count)
                    {
                        // add a copy of the first element (structure)
                        if (this.FMembers.Count > 0)
                        {  // if there is an element to clone
                            this.NewMember(this.Item(1));           // Clones (copy constructor) the element structure
                        }
                        else
                        {
                            if (this.childTemplate != null)
                            {    // if previously stored an item that was removed when setElementCount(0)
                                this.AddMember(this.childTemplate);
                                this.childTemplate = null;          // now belongs to the list
                            }
                            else
                                this.AddScalar(string.Empty, this.FBaseType); // else determine what type the first element should be (must be a scalar)
                        }
                    }
                }
                else if (dim < this.FMembers.Count)
                {
                    while ((dim < this.FMembers.Count) && (this.FMembers.Count > 0))
                    {
                        // delete some elements (newsize>=0)
                        this.DeleteElement(this.FMembers.Count);    // 1 based
                    }
                }
            }
            else
                throw new TypeMisMatchException("Cannot add or remove an array member to a non-array type.");
        }
        
        /// <summary>
        /// Only allowed to add members to records and arrays.
        /// </summary>
        /// <param name="newMember">The new member to add to this structure.</param>
        /// N.Herrmann Apr 2002
        public void AddMember(TTypedValue newMember)
        {
            if ((this.FIsArray || this.FIsRecord) && (newMember != null))
            {
                if (this.FIsArray && ((this.FBaseType >= TBaseType.ITYPE_INT1) && (this.FBaseType <= TBaseType.ITYPE_DOUBLE))) // if number type
                    newMember.SetUnits(this.FUnit);
                this.FMembers.Add(newMember);
            }
        }

        /// <summary>
        /// The new member to add to this structure.
        /// </summary>
        /// <param name="typedValue">Typed value to copy.</param>
        /// N.Herrmann Apr 2002
        protected void InitTypeCopy(TTypedValue typedValue)
        {
            uint i;

            this.Name = typedValue.Name;
            this.FBaseType = typedValue.BaseType();
            this.FIsScalar = typedValue.IsScalar();
            this.FIsArray = typedValue.IsArray();
            this.FIsRecord = typedValue.IsRecord();
            this.SetUnits(typedValue.Units());

            if (this.FIsScalar)
            {
                this.CreateScalar();
                switch (this.FBaseType)
                {                                                                         // For scalars, copy the value data.
                    case TBaseType.ITYPE_INT1:                                            // Data pertaining to arrays and records
                    case TBaseType.ITYPE_INT2:                                            // is ultimately stored in their
                    case TBaseType.ITYPE_INT4:                                            // constituent scalars
                    case TBaseType.ITYPE_INT8: this.SetValue(typedValue.AsInt());
                        break;
                    case TBaseType.ITYPE_SINGLE: this.SetValue(typedValue.AsSingle());
                        break;
                    case TBaseType.ITYPE_DOUBLE: this.SetValue(typedValue.AsDouble());
                        break;
                    case TBaseType.ITYPE_BOOL: this.SetValue(typedValue.AsBool());
                        break;
                    case TBaseType.ITYPE_CHAR:
                    case TBaseType.ITYPE_WCHAR: this.SetValue(typedValue.AsChar());
                        break;
                    case TBaseType.ITYPE_STR:
                    case TBaseType.ITYPE_WSTR: this.SetValue(typedValue.AsStr());
                        break;
                }
            }
            else if (this.FIsArray || this.FIsRecord)
            {
                uint itemCount = typedValue.Count();
                if (this.FIsArray && (itemCount == 0))
                {
                    if (typedValue.Item(0) != null)
                        this.NewMember(typedValue.Item(0));
                    this.SetElementCount(0);
                }
                else
                    for (i = 1; i <= itemCount; i++)
                        this.NewMember(typedValue.Item(i)); // clones and adds this typed value
            }
        }
        
        /// <summary>
        /// Retrieves the TTypedValue as an integer.
        /// Will also read shorter types of numbers and return them as integers.
        /// On error an exception is thrown.
        /// </summary>
        /// <returns>An integet value.</returns>
        /// N.Herrmann Apr 2002
        public int AsInteger()
        {
            return this.AsInt();
        }
        
        /// <summary>
        /// Return an array of integers.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public int[] AsIntArray()
        {
            uint itemCount = this.Count();
            int[] data = new int[0];
            if (this.FIsArray && (itemCount > 0))
            {
                data = new int[itemCount];
                if (this.Item(1).IsScalar())
                {
                    for (uint i = 1; i <= itemCount; i++)
                    {
                        data[i - 1] = this.Item(i).AsInt();
                    }
                }
            }
            return data;
        }
        
        /// <summary>
        /// Retrieves the TTypedValue as an integer.
        /// Will also read shorter types of numbers and return them as integers.
        /// On error an exception is thrown.
        /// </summary>
        /// <returns>The integer value</returns>
        public int AsInt32()
        {
            return this.AsInt();
        }
        
        /// <summary>
        /// Return an array of integers.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public int[] AsInt32Array()
        {
            return this.AsIntArray();
        }
        
        /// <summary>
        /// Retrieves the TTypedValue as an integer.
        /// Will also read shorter types of numbers and return them as integers.
        /// On error an exception is thrown.
        /// </summary>
        /// <returns>An integet value.</returns>
        /// N.Herrmann Apr 2002
        public int AsInt()
        {
            string errorMsg = string.Empty;
            int value = 0;

            if (this.FIsScalar && (this.FData != null))
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_INT4: value = BitConverter.ToInt32(this.FData, 0);
                        break;
                    case TBaseType.ITYPE_INT1: value = this.FData[0];
                        break;
                    case TBaseType.ITYPE_INT2: value = BitConverter.ToInt16(this.FData, 0);
                        break;
                    ////?? case TBaseType.ITYPE_INT8: value = BitConverter.ToInt32(FData); break;
                    case TBaseType.ITYPE_BOOL:
                        {
                            if (this.AsBool())
                                value = 1;
                            else
                                value = 0;
                        }
                        break;
                    case TBaseType.ITYPE_DOUBLE:
                    case TBaseType.ITYPE_SINGLE:
                    case TBaseType.ITYPE_STR:
                    case TBaseType.ITYPE_WSTR:
                        value = Convert.ToInt32(Math.Floor(this.AsDouble() + 0.5), CultureInfo.InvariantCulture);
                        break;

                    default:
                        {
                            errorMsg = "Cannot convert " + sTYPECODES[(int)this.FBaseType] + " TTypedValue to an integer value.";
                            throw new TypeMisMatchException(errorMsg);
                        }
                }
            }
            else
            {
                errorMsg = "Cannot retrieve " + sTYPECODES[(int)this.FBaseType] + " TTypedValue as an integer value.";
                throw new TypeMisMatchException(errorMsg);
            }

            return value;
        }
        
        /// <summary>
        /// Return an array of Singles.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public float[] AsSingleArray()
        {
            uint itemCount = this.Count();
            float[] data = new float[0];
            if (this.FIsArray && (itemCount > 0))
            {
                data = new float[itemCount];
                if (this.Item(1).IsScalar())
                {
                    for (uint i = 1; i <= itemCount; i++)
                    {
                        data[i - 1] = this.Item(i).AsSingle();
                    }
                }
            }
            return data;
        }
        
        /// <summary>
        /// The value of this scalar as a float.
        /// </summary>
        /// <returns>Floating point value.</returns>
        /// N.Herrmann Apr 2002
        public float AsSingle()
        {
            string errorMsg = string.Empty;
            float value = 0.0f;

            if (this.FIsScalar && (this.FData != null))
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_DOUBLE: value = Convert.ToSingle(this.AsDouble(), CultureInfo.InvariantCulture);
                        break;
                    case TBaseType.ITYPE_SINGLE: value = BitConverter.ToSingle(this.FData, 0);
                        break;
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8: value = this.AsInt();
                        break;
                    case TBaseType.ITYPE_BOOL:
                        {
                            if (this.AsBool())
                                value = 1;
                            else
                                value = 0;
                        }
                        break;
                    case TBaseType.ITYPE_STR:
                        {
                            string buf;
                            buf = this.AsStr();
                            if (buf.Length < 1)
                                buf = "0";
                            value = Convert.ToSingle(buf, CultureInfo.InvariantCulture);
                        }
                        break;
                    default:
                        {
                            errorMsg = "Cannot convert " + sTYPECODES[(int)this.FBaseType] + " TTypedValue to a float value.";
                            throw new TypeMisMatchException(errorMsg);
                        }
                }
            }
            else
            {
                errorMsg = "Cannot retrieve " + sTYPECODES[(int)this.FBaseType] + " TTypedValue as a float value.";
                throw new TypeMisMatchException(errorMsg);
            }

            return value;
        }
        
        /// <summary>
        /// Return an array of Doubles.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public double[] AsDoubleArray()
        {
            uint itemCount = this.Count();
            double[] data = new double[0];
            if (this.FIsArray && (itemCount > 0))
            {
                data = new double[itemCount];
                if (this.Item(1).IsScalar())
                {
                    for (uint i = 1; i <= itemCount; i++)
                    {
                        data[i - 1] = this.Item(i).AsDouble();
                    }
                }
            }
            return data;
        }
        
        /// <summary>
        /// The value of this scalar as a double.
        /// </summary>
        /// <returns>Double precision value.</returns>
        /// N.Herrmann Apr 2002
        public double AsDouble()
        {
            string errorMsg = string.Empty;
            double value = 0.0;

            if (this.FIsScalar && (this.FData != null))
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_DOUBLE: value = BitConverter.ToDouble(this.FData, 0);
                        break;
                    case TBaseType.ITYPE_SINGLE: value = this.AsSingle();
                        break;
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8: value = this.AsInt();
                        break;
                    case TBaseType.ITYPE_BOOL:
                        {
                            if (this.AsBool())
                                value = 1;
                            else
                                value = 0;
                        }
                        break;
                    case TBaseType.ITYPE_STR:
                        {
                            string buf;
                            buf = this.AsStr();
                            if (buf.Length < 1)
                                buf = "0";
                            value = Convert.ToDouble(buf, System.Globalization.CultureInfo.InvariantCulture);
                        }
                        break;
                    default:
                        {
                            errorMsg = "Cannot convert " + sTYPECODES[(int)this.FBaseType] + " TTypedValue to a double.";
                            throw new TypeMisMatchException(errorMsg);
                        }
                }
            }
            else
            {
                errorMsg = "Cannot retrieve " + sTYPECODES[(int)this.FBaseType] + " TTypedValue as a double.";
                throw new TypeMisMatchException(errorMsg);
            }
            return value;
        }
        
        /// <summary>
        /// Return an array of Booleans.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public bool[] AsBoolArray()
        {
            uint itemCount = this.Count();
            bool[] data = new bool[0];
            if (this.FIsArray && (itemCount > 0))
            {
                data = new bool[itemCount];
                if (this.Item(1).IsScalar())
                {
                    for (uint i = 1; i <= itemCount; i++)
                    {
                        data[i - 1] = this.Item(i).AsBool();
                    }
                }
            }
            return data;
        }
        
        /// <summary>
        /// Return an array of Booleans.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public bool[] AsBooleanArray()
        {
            return this.AsBoolArray();
        }
        
        /// <summary>
        /// Returns false if value is 0. Returns true if anything else.
        /// Reads other interger values and interprets them.
        /// On error an exception is thrown.
        /// </summary>
        /// <returns>Value as true or false.</returns>
        public bool AsBoolean()
        {
            return this.AsBool();
        }
        
        /// <summary>
        /// Returns false if value is 0. Returns true if anything else.
        /// Reads other interger values and interprets them.
        /// On error an exception is thrown.
        /// </summary>
        /// <returns>Value as true or false.</returns>
        /// N.Herrmann Apr 2002
        public bool AsBool()
        {
            string errorMsg;
            bool value = false;

            if (this.FIsScalar && (this.FData != null))
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_BOOL: value = BitConverter.ToBoolean(this.FData, 0);
                        break;
                    case TBaseType.ITYPE_INT1:
                    case TBaseType.ITYPE_INT2:
                    case TBaseType.ITYPE_INT4:
                    case TBaseType.ITYPE_INT8:
                        {
                            if (this.AsInt() == 0)
                                value = false;
                            else
                                value = true;
                        }
                        break;
                    case TBaseType.ITYPE_CHAR:
                    case TBaseType.ITYPE_WCHAR:
                        {
                            if (this.AsStr().ToLower() == "t")
                                value = true;
                        }
                        break;
                    case TBaseType.ITYPE_WSTR: 
                    case TBaseType.ITYPE_STR:
                        {
                            if (char.ToLower(this.AsStr()[0]) == 't')
                                value = true;
                        }
                        break;
                    default:
                        {
                            errorMsg = "Cannot convert " + sTYPECODES[(int)this.FBaseType] + " TTypedValue to a boolean value.";
                            throw new TypeMisMatchException(errorMsg);
                        }
                }
            }
            else
            {
                errorMsg = "Cannot retrieve " + sTYPECODES[(int)this.FBaseType] + " TTypedValue as a boolean value.";
                throw new TypeMisMatchException(errorMsg);
            }
            return value;
        }
        
        /// <summary>
        /// Returns the character. On error an exception is thrown.
        /// <para>Conversions: Bool -> 'true'/'false', String -> asStr()[0] .</para>
        /// </summary>
        /// <returns>Character value.</returns>
        /// N.Herrmann Apr 2002
        public char AsChar()
        {
            string errorMsg = string.Empty;
            char value = '\0';

            if (this.FIsScalar && (this.FData != null))
            {
                switch (this.FBaseType)
                {
                    case TBaseType.ITYPE_BOOL:
                        {
                            if (this.AsBool())
                                value = 'T';
                            else
                                value = 'F';
                        }
                        break;
                    case TBaseType.ITYPE_CHAR:
                    case TBaseType.ITYPE_WCHAR: value = BitConverter.ToChar(this.FData, 0);
                        break;
                    case TBaseType.ITYPE_WSTR:
                    case TBaseType.ITYPE_STR: value = this.AsStr()[0];
                        break;
                    default:
                        {
                            errorMsg = "Cannot convert " + sTYPECODES[(int)this.FBaseType] + " TTypedValue to a character value.";
                            throw new TypeMisMatchException(errorMsg);
                        }
                }
            }
            else
            {
                errorMsg = "Cannot retrieve " + sTYPECODES[(int)this.FBaseType] + " TTypedValue as a boolean value.";
                throw new TypeMisMatchException(errorMsg);
            }
            return value;
        }
        
        /// <summary>
        /// Return an array of Booleans.
        /// </summary>
        /// <returns>Returns and array of zero length if this is not array of scalars
        /// with at least one element.</returns>
        public string[] AsStringArray()
        {
            uint itemCount = this.Count();
            string[] data = new string[0];
            if (this.FIsArray && (itemCount > 0))
            {
                data = new string[itemCount];
                if (this.Item(1).IsScalar())
                {
                    for (uint i = 1; i <= itemCount; i++)
                    {
                        data[i - 1] = this.Item(i).AsStr();
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Gets the text value for this scalar typed value from the data block.
        /// </summary>
        /// <returns>The value as a string.</returns>
        public string AsString()
        {
            return this.AsStr();
        }
        
        /// <summary>
        /// Gets the text value for this scalar typed value from the data block.
        /// </summary>
        /// <returns>The value as a string.</returns>
        public string AsStr()
        {
            uint varSize;    // number of characters (not bytes)
            string buf = string.Empty;

            if (this.FIsScalar && (this.FData != null))
            {      
                // char strings (str) are scalars
                if (this.FBaseType == TBaseType.ITYPE_STR)
                {
                    // should be able to get the data from the data block
                    varSize = GetDimension(this.FData, 0);
                    buf = this.ascii.GetString(this.FData, 4, (int)varSize);
                }
                if (this.FBaseType == TBaseType.ITYPE_WSTR)
                {
                    // Wide strings have x * 2 bytes
                    varSize = GetDimension(this.FData, 0);
                    System.Text.UnicodeEncoding uni = new System.Text.UnicodeEncoding();
                    buf = uni.GetString(this.FData, 4, (int)varSize * 2);
                }
                else if ((this.FBaseType == TBaseType.ITYPE_CHAR) || (this.FBaseType == TBaseType.ITYPE_WCHAR))
                    buf = this.AsChar().ToString();
                else if ((this.FBaseType == TBaseType.ITYPE_DOUBLE) || // if the field is a double I can still return a string representation
                    (this.FBaseType == TBaseType.ITYPE_SINGLE))
                {
                    buf = this.AsDouble().ToString("G8");
                }
                else if ((this.FBaseType == TBaseType.ITYPE_INT1) ||   // if the field is an int I can still return a string representation
                      (this.FBaseType == TBaseType.ITYPE_INT2) ||
                      (this.FBaseType == TBaseType.ITYPE_INT4) ||
                      (this.FBaseType == TBaseType.ITYPE_INT8))
                {
                    buf = this.AsInt().ToString();
                }
                else if (this.FBaseType == TBaseType.ITYPE_BOOL)
                {
                    if (this.AsBool())
                        buf = "true";
                    else
                        buf = "false";
                }
            }
            return buf;
        }

        /// <summary>
        /// Gets the text value for this scalar typed value from the data block.
        /// This representation is intended primarily for use in writing log files.
        /// </summary>
        /// <returns>The formatted output as a string. <para>If this is a scalar then the result will 
        /// be same as asStr().</para><para>An array will be [1,2,3,4,5].</para><para>Records will be
        /// [fieldname1: asStr(), fieldname2: asStr()]</para></returns>
        public string AsText()
        {
            if (this.FIsScalar)
            {
                return this.AsStr();
            }
            else
            {
                StringBuilder buf = new StringBuilder("[");
                uint i;
                uint itemCount = this.Count();
                for (i = 1; i <= itemCount; i++)
                {
                    if (i > 1) buf.Append(", ");
                    if (this.IsRecord())
                    {
                        buf.Append(this.member(i).Name);
                        buf.Append(": ");
                    }
                    buf.Append(this.member(i).AsText());
                }
                buf.Append("]");
                return buf.ToString();
            }
        }

        /// <summary>
        /// Returns the string value of this typed value as an escaped text string.
        /// </summary>
        /// <returns>String value escaped.</returns>
        /// N.Herrmann Apr 2002
        public string AsEscapedString()
        {
            return EscapeText(this.AsStr());
        }
        
        /// <summary>
        /// Escapes the special characters for storing as xml.
        /// </summary>
        /// <param name="text">The character string to escape.</param>
        /// <returns>The escaped string.</returns>
        /// N.Herrmann Apr 2002
        public static string EscapeText(string text)
        {
            int index;
            
            StringBuilder sbuf = new StringBuilder(string.Empty);
            for (index = 0; index < text.Length; index++)
            {
                switch (text[index])
                {
                    case '&': sbuf.Append("&#38;");
                        break;
                    case '<': sbuf.Append("&#60;");
                        break;
                    case '>': sbuf.Append("&#62;");
                        break;
                    case '"': sbuf.Append("&#34;");
                        break;
                    case '\'': sbuf.Append("&#39;");
                        break;
                    default:
                        {
                            // If it is none of the special characters, just copy it
                            sbuf.Append(text[index]);
                        }
                        break;
                }
            }

            return sbuf.ToString();
        }
        
        /// <summary>
        /// The type of this value as a character string.
        /// </summary>
        /// <returns>Type string.</returns>
        /// N.Herrmann Apr 2002
        public string TypeName()
        {
            return sTYPECODES[(int)this.FBaseType];
        }
        
        /// <summary>
        /// The size of this type in bytes. For an array it includes the
        /// 4 byte header of each dimension.
        /// </summary>
        /// <returns>Integer value of the size.</returns>
        /// N.Herrmann Apr 2002
        public uint SizeBytes()
        {
            int i;
            uint typeSize = 0;

            if (this.FIsScalar)
            {
                typeSize = this.FDataSize;
            }
            else
            {
                for (i = 0; i < this.FMembers.Count; i++)
                {
                    typeSize = typeSize + this.FMembers[i].SizeBytes();
                }
                if (this.FIsArray)
                    typeSize = typeSize + INTSIZE;
            }

            return typeSize;
        }
        
        /// <summary>
        /// Recursive routine for checking whether two types are (a) identical,
        /// (b) different but compatible, (c) incompatible.
        /// <para>Note:</para>
        /// <para>1. Type compatibility is not a transitive relationship.</para>
        /// <para>2. Unit compatibility needs further implementation.</para>
        /// </summary>
        /// <param name="srcValue">The TTypedValue to compare with.</param>
        /// <returns>Returns: 0 - exact match, 1 - compatible, -1 - cannot match</returns>
        public int CanAssignFrom(TTypedValue srcValue)
        {
            int result = ctBAD;
            uint idx;

            if (srcValue.IsScalar())
            {
                if (!this.FIsScalar)
                    result = ctBAD;
                else if (srcValue.BaseType() == this.FBaseType)
                    result = ctSAME;
                else if ((srcValue.BaseType() <= TBaseType.ITYPE_INT8) && (srcValue.BaseType() >= TBaseType.ITYPE_INT1) &&
                         (this.FBaseType <= TBaseType.ITYPE_INT8) && (this.FBaseType >= TBaseType.ITYPE_INT1))
                    result = ctCOMP;  // both integers
                else if ((this.FBaseType >= TBaseType.ITYPE_SINGLE) && (this.FBaseType <= TBaseType.ITYPE_DOUBLE) &&           // These conditions are not transitive                        
                         (srcValue.BaseType() >= TBaseType.ITYPE_INT1) && (srcValue.BaseType() <= TBaseType.ITYPE_DOUBLE))
                    result = ctCOMP;  // can match an int/single source to single/double destination
                else if ((srcValue.BaseType() == TBaseType.ITYPE_CHAR) &&
                    ((this.FBaseType == TBaseType.ITYPE_WCHAR) ||
                    (this.FBaseType == TBaseType.ITYPE_STR) ||
                    (this.FBaseType == TBaseType.ITYPE_WSTR)))
                    result = ctCOMP;
                else if ((srcValue.BaseType() == TBaseType.ITYPE_WCHAR) && (this.FBaseType == TBaseType.ITYPE_WSTR))
                    result = ctCOMP;
                else if ((srcValue.BaseType() == TBaseType.ITYPE_STR) && (this.FBaseType == TBaseType.ITYPE_WSTR))
                    result = ctCOMP;
                //// A sop to the old APSIM manager, which sends out all request-set values as strings
                else if ((srcValue.BaseType() == TBaseType.ITYPE_STR) || (this.FBaseType == TBaseType.ITYPE_WSTR))
                    result = ctDODGY;
                else
                    result = ctBAD;

                if ((this.FBaseType >= TBaseType.ITYPE_INT1) && (this.FBaseType <= TBaseType.ITYPE_DOUBLE) &&
                      (!UnitsMatch(this.Units(), srcValue.Units())))
                    result = ctBAD;
            }
            else if (srcValue.IsArray())
            {   
                // an array
                if (!this.FIsArray)
                    result = ctBAD;
                else
                {
                    if (this.Count() == 0)
                        this.SetElementCount(1);          // addElement();
                    if (srcValue.Count() == 0)
                        srcValue.SetElementCount(1);        // addElement();
                    result = this.member(1).CanAssignFrom(srcValue.member(1));
                }
            }
            else
            {   
                // a record
                if (!this.IsRecord())
                    result = ctBAD;
                else
                {
                    uint recCount = this.Count();
                    result = ctCOMP;                                                        // First, test for identity
                    if (recCount == srcValue.Count())
                    {
                        result = ctSAME;
                        for (idx = 1; idx <= recCount; idx++)
                        {
                            if ((this.member(idx).Name.ToLower() != srcValue.member(idx).Name.ToLower()) ||
                                  (this.member(idx).CanAssignFrom(srcValue.member(idx)) != ctSAME))
                                result = ctCOMP;
                        }
                    }

                    if (result == ctCOMP)
                    {                                                           // If not same, test for compatibility
                        string elemName;
                        for (idx = 1; idx <= srcValue.Count(); idx++)
                        {
                            elemName = srcValue.member(idx).Name;               // field name
                            if (!this.HasField(elemName) ||
                                  (this.member(elemName).CanAssignFrom(srcValue.member(idx)) == ctBAD))
                                result = ctBAD;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns TRUE i.f.f. the two unit strings have the same dimension and
        /// identical scale.
        /// </summary>
        /// <param name="unitA">Unit name one.</param>
        /// <param name="unitB">Unit name two.</param>
        /// <returns>True if matched.</returns>
        /// N.B. this is a temporary implementation.
        /// A.Moore
        protected static bool UnitsMatch(string unitA, string unitB)
        {
            int i;
            bool result = false;

            // APSIM has historically sometimes encased units in parentheses
            // Get rid of these before proceding
            string unit1 = StripOuterParens(ref unitA);
            string unit2 = StripOuterParens(ref unitB);

            if (unit1 == unit2)
                result = true;
            else if ((unit1.Length == 0) || (unit2.Length == 0))       // The null string matches any unit
                result = true;
            else
            {    
                // Search the lookup table of matching units
                i = 0;
                while (!result && (i < UNITMATCHES.Length))
                {
                    if ((unit1 == UNITMATCHES[i].Unit1) && (unit2 == UNITMATCHES[i].Unit2))
                        result = true;
                    else if ((unit1 == UNITMATCHES[i].Unit2) && (unit2 == UNITMATCHES[i].Unit1))
                        result = true;
                    else
                        i++;
                }
            }
            return result;
        }

        /// <summary>
        /// Strip the outer parenthisis
        /// </summary>
        /// <param name="text">Input text</param>
        /// <returns>The text with no ( )</returns>
        private static string StripOuterParens(ref string text)
        {
            if (text.Length > 2 && text[0] == '(' && text[text.Length - 1] == ')')
                return text.Substring(1, text.Length - 2);
            else
                return text;
        }

        /// <summary>
        /// Tests for identity of two TTypedValue objects.
        /// </summary>
        /// <param name="otherValue">Typed value to test against this one.</param>
        /// <returns>True if it matches in type, size, and structure.</returns>
        /// N.Herrmann Apr 2002
        public bool Equals(TTypedValue otherValue)
        {
            uint i;
            bool isEqual = false;

            if ((otherValue != null) &&
               (this.FBaseType == otherValue.BaseType()) &&
               (this.FIsArray == otherValue.IsArray()) &&
               (this.FIsRecord == otherValue.IsRecord()) &&
               (this.Count() == otherValue.Count()) &&
               (this.FDataSize == otherValue.SizeBytes()))
                isEqual = true;

            if (isEqual)
            {
                if (this.FIsScalar)
                    isEqual = isEqual && (this.AsStr() == otherValue.AsStr());    // str comparison of the scalar (needs refinement)
            }
            else
            {
                for (i = 1; i <= this.Count(); i++)
                    isEqual = isEqual && this.Item(i).Equals(otherValue.Item(i));
            }
            return isEqual;
        }
        
        /// <summary>
        /// Check if the field exists
        /// </summary>
        /// <param name="fieldName">Name of the field to find.</param>
        /// <returns>Returns TRUE if the value is a record and it has the nominated field.</returns>
        /// N.Herrmann Apr 2002
        public bool HasField(string fieldName)
        {
            bool result = false;
            try
            {
                if (this.member(fieldName) != null)
                    result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Copies the data from a data block into this scalar.
        /// </summary>
        /// <param name="newData">The data block to copy into this scalar.</param>
        /// <param name="startIndex">Start at this index in the byte array.</param>
        /// <returns>True if success</returns>
        /// N.Herrmann Apr 2002
        public bool CopyDataBlock(byte[] newData, uint startIndex)
        {
            bool copyOK = true;

            if ((this.FBaseType >= TBaseType.ITYPE_EMPTY) && (this.FBaseType <= TBaseType.ITYPE_WCHAR))
                this.FDataSize = TypeSize[(int)this.FBaseType];
            else if (this.FBaseType == TBaseType.ITYPE_STR)
            {
                uint noChars = GetDimension(newData, startIndex);
                this.FDataSize = INTSIZE + noChars;
            }
            else if (this.FBaseType == TBaseType.ITYPE_WSTR)
            {
                uint noChars = GetDimension(newData, startIndex);
                this.FDataSize = INTSIZE + (2 * noChars);          // size in bytes
            }

            if ((startIndex + this.FDataSize) <= newData.Length) 
            {
                // if there are enough bytes to copy
                this.FData = new byte[this.FDataSize];
                Array.Copy(newData, startIndex, this.FData, 0, this.FDataSize);
            }
            else
            {
                copyOK = false;
            }
            return copyOK;
        }
        
        /// <summary>
        /// Gets the datablock containing all the values from this TTypedValue.
        /// Assumes that memory has already been allocated for the data to be copied
        /// into.
        /// </summary>
        /// <param name="data">Location to copy data to.</param>
        /// N.Herrmann Apr 2002
        public void GetData(ref byte[] data)
        {
            byte[] tmpPtr;
            uint startIdx = 0;

            tmpPtr = data;                  // store the pointer position because it gets shifted
            this.CopyData(data, ref startIdx);     // do the copy
            data = tmpPtr;                  // restore the pointer to the start of the memory block
        }
        
        /// <summary>
        /// Copies the data from a block of memory owned by someone else.
        /// Overrides setData() and assumes startIndex = 0.
        /// </summary>
        /// <param name="newData">The new data to copy into this typed value.</param>
        /// <param name="newSize">The size of the source typed value.</param>
        public void SetData(byte[] newData, int newSize)
        {
            this.SetData(newData, newSize, 0);
        }
        
        /// <summary>
        /// Copies the data from a block of memory owned by someone else.
        /// </summary>
        /// <param name="newData">The new data to copy into this typed value.</param>
        /// <param name="newSize">The size of the source typed value.</param>
        /// <param name="startIndex">Start at this index in the byte array.</param>
        /// <returns>True if the function succeeds</returns>
        /// N.Herrmann Apr 2002
        public bool SetData(byte[] newData, int newSize, uint startIndex)
        {
            uint dim;
            int i;
            int bytesRemain;
            TTypedValue value;
            uint childSize;
            bool success = true;

            bytesRemain = 0;
            if ((newSize > 0) && (newData != null))
            {         
                // if the incoming block has data
                if (this.FIsScalar)
                {
                    success = this.CopyDataBlock(newData, startIndex); // copies scalars (including strings)
                }
                else
                {
                    bytesRemain = newSize;      // keep count of bytes
                    if (this.FIsArray)
                    {
                        // get the DIM=x value from the datablock
                        dim = GetDimension(newData, startIndex);
                        if (dim != this.Count())
                            this.SetElementCount(dim);   // create or delete child elements
                        bytesRemain = newSize - (int)INTSIZE;
                        startIndex += INTSIZE;
                    }

                    // now copy the children. All children exist now
                    // go through each child and set the data block
                    i = 0;
                    while (success && (i < this.FMembers.Count))
                    {             
                        // for each field
                        value = this.FMembers[i];
                        success = value.SetData(newData, bytesRemain, startIndex);      // store the datablock
                        if (success)
                        {
                            childSize = value.SizeBytes();
                            bytesRemain = bytesRemain - (int)childSize;
                            startIndex += childSize;                                    // inc ptr along this dataBlock
                        }
                        i++;
                    }

                    // store the size in bytes for this type
                    // FDataSize = (uint)(iNewSize - Math.Max(0, bytesRemain));   //bytesRemain should = 0
                }
            }
            else if (this.FIsArray)
            {
                this.SetElementCount(0);
            }
            ////   if bytesRemain <> 0 then
            ////      raise Exception.Create( 'Input data inconsistent with type of value in setData()' + Name );

            return success;
        }
        
        /// <summary>
        /// Copies the FData of this type and any children into the memory already
        /// allocated. Called recursively to fill with data from the children.
        /// </summary>
        /// <param name="dataPtr">The location to copy to.</param>
        /// <param name="startIndex">Start at this index in the byte array.</param>
        /// N.Herrmann Apr 2002
        public void CopyData(byte[] dataPtr, ref uint startIndex)
        {
            uint idx;

            if (this.FIsScalar)
            {  
                // scalars (and strings) are one block of data at this point
                Array.Copy(this.FData, 0, dataPtr, startIndex, this.FDataSize);
                startIndex += this.FDataSize;                         // move along so other scalars can follow
            }
            else
            {
                uint dim = this.Count(); // store the array dimension
                if (this.FIsArray)
                {   
                    // arrays have a dimension header and then the data blocks following
                    dataPtr[startIndex] = (byte)dim;
                    dataPtr[startIndex + 1] = (byte)(dim >> 8);
                    dataPtr[startIndex + 2] = (byte)(dim >> 16);
                    dataPtr[startIndex + 3] = (byte)(dim >> 24);
                    startIndex += INTSIZE;          // move along so the array items follow
                }
                if (this.FIsRecord || this.FIsArray)          
                {
                    // if this is a value that has children
                    for (idx = 1; idx <= dim; idx++)                            // for each member/element
                        this.Item(idx).CopyData(dataPtr, ref startIndex);       // var dataPtr. Ptr get moved along with each copy.
                }
            }
        }
        
        /// <summary>
        /// Copies data from one type to this type using the getData() setData() pair.
        /// Assumes that the source and destination are exactly compatible. Arrays will be
        /// resized as required.
        /// </summary>
        /// <param name="srcValue">The source value.</param>
        /// adapted from A. Moore 2002
        public void CopyFrom(TTypedValue srcValue)
        {
            if (srcValue != null)
            {
                uint sizeBytes = srcValue.SizeBytes();
                if (sizeBytes > 0)
                {
                    byte[] data = new byte[sizeBytes];
                    srcValue.GetData(ref data);
                    this.SetData(data, (int)sizeBytes, 0);
                }
            }
        }
        
        /// <summary>
        /// Copies the data from a data block into this scalar.
        /// Overloaded version, taking data from an IntPtr
        /// </summary>
        /// <param name="newData">The data block to copy into this scalar.</param>
        /// <param name="startIndex">Start at this index in the byte array.</param>
        /// N.Herrmann Apr 2002
        public void CopyDataBlock(IntPtr newData, uint startIndex)
        {
            if ((this.FBaseType >= TBaseType.ITYPE_EMPTY) && (this.FBaseType <= TBaseType.ITYPE_WCHAR))
                this.FDataSize = TypeSize[(int)this.FBaseType];
            else if (this.FBaseType == TBaseType.ITYPE_STR)
            {
                uint noChars = (uint)Marshal.ReadInt32(newData, (int)startIndex);
                this.FDataSize = INTSIZE + noChars;
            }
            else if (this.FBaseType == TBaseType.ITYPE_WSTR)
            {
                uint noChars = (uint)Marshal.ReadInt32(newData, (int)startIndex);
                this.FDataSize = INTSIZE + (2 * noChars); // size in bytes
            }

            this.FData = new byte[this.FDataSize];
            for (int i = 0; i < this.FDataSize; i++)
                this.FData[i] = Marshal.ReadByte(newData, (int)startIndex + i);
        }

        /// <summary>
        /// Copies the data from a block of memory owned by someone else.
        /// Overloaded version, taking data from an IntPtr
        /// </summary>
        /// <param name="newData">The new data to copy into this typed value.</param>
        /// <param name="startIndex">Start at this index in the byte array.</param>
        /// N.Herrmann Apr 2002
        public void SetData(IntPtr newData, uint startIndex)
        {
            uint dim;
            int i;
            TTypedValue value;
            uint childSize;

            if (!newData.Equals(IntPtr.Zero))
            {                   
                // if the incoming block has data
                if (this.FIsScalar)
                {
                    this.CopyDataBlock(newData, startIndex);                            // copies scalars (including strings)
                }
                else
                {
                    if (this.FIsArray)
                    {
                        dim = (uint)Marshal.ReadInt32(newData, (int)startIndex);       // get the DIM=x value from the datablock
                        if (dim != this.Count())
                            this.SetElementCount(dim);                                  // create or delete child elements
                        startIndex += INTSIZE;
                    }

                    // now copy the children. All children exist now
                    // go through each child and set the data block
                    for (i = 0; i < this.FMembers.Count; i++)
                    {      
                        // for each field
                        value = this.FMembers[i];
                        value.SetData(newData, startIndex);         // store the datablock
                        childSize = value.SizeBytes();
                        startIndex += childSize;                    // inc ptr along this dataBlock
                    }
                }
            }
            else if (this.FIsArray)
            {
                this.SetElementCount(0);
            }
        }
    }

    /// <summary>
    /// Thrown when a type mismatch occurs. 
    /// For example: attempting to access an array in the manner of a scalar.
    /// </summary>
    [Serializable] 
    public class TypeMisMatchException : ApplicationException
    {
        /// <summary>
        /// Create an exception that specifies a type mis match
        /// </summary>
        /// <param name="message">Exception message</param>
        public TypeMisMatchException(string message)
            : base(message)
        {
        }
        
        /// <summary>
        /// Constructor that will show details of the two types causing the problem.
        /// </summary>
        /// <param name="first">First TTypedValue.</param>
        /// <param name="second">Second TTypedValue.</param>
        public TypeMisMatchException(TTypedValue first, TTypedValue second)
            : base("Type mismatch exception: " + first.TypeName() + " does not match " + second.TypeName())
        {
        }
    }
    
    /// <summary>
    /// Thrown when an array item is out of range.
    /// </summary>
    [Serializable]
    public class ArrayIndexException : ApplicationException
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ArrayIndexException(string message)
            : base(message)
        {
        }
    }
}
