using CMPServices;
using System;

namespace Models.GrazPlan
{
    /// <summary>
    /// TParameterDefinition
    /// Class used for the definition of parameter names and types, and for storing
    /// whether the value(s) of the parameter is defined. A definition may be either
    /// a single parameter, or it may be an array of parameter definitions. Array
    /// definitions are indexed by string references
    /// </summary>
    [Serializable]
    public class TParameterDefinition
    {
        private string FName;
        private string FNamePart;
        private TTypedValue.TBaseType FType;
        private TParameterDefinition[] FItems;
        private int FCount;
        private int FParamCount;
        private bool FDefined;

        /// <summary>
        /// Each element of a "definition string" array may take one of two forms:
        /// * val:val        Integer subrange (e.g. "1:8")
        /// * text[;text]*   List of text indices
        ///
        /// For example, if the original string from which sDefnStrings was constructed
        /// was "KQ-1:4-leaf;stem", then the resulting tree of definitions would be:
        /// KQ
        ///  |
        ///  +-+-------------+-------------+-------------+
        ///    |             |             |             |
        ///    1             2             3             4
        ///    +-+-----+     +-+-----+     +-+-----+     +-+-----+
        ///      |     |       |     |       |     |       |     |
        ///      leaf  stem    leaf  stem    leaf  stem    leaf  stem
        /// </summary>
        /// <param name="sDefnStrings"></param>
        /// <param name="aType"></param>
        /// <param name="iOffset"></param>
        public TParameterDefinition(string[] sDefnStrings, TTypedValue.TBaseType aType, int iOffset = 0)
        {
            FItems = new TParameterDefinition[0];
            string[] sSubDefnStrings = new string[0];
            string sIndexStr;
            int iPosn;
            int Idx;

            FName = sDefnStrings[0];
            for (Idx = 1; Idx <= iOffset; Idx++)
                FName = FName + "-" + sDefnStrings[Idx];
            if (iOffset < (sDefnStrings.Length - 1))
                FName = FName + "-";
            FName = FName.ToLower();

            FNamePart = sDefnStrings[iOffset].ToLower();
            FType = aType;
            FDefined = false;

            if (iOffset < (sDefnStrings.Length - 1))
            {
                Array.Resize(ref sSubDefnStrings, sDefnStrings.Length);
                for (Idx = 0; Idx <= (sSubDefnStrings.Length - 1); Idx++)
                    sSubDefnStrings[Idx] = sDefnStrings[Idx];
                sIndexStr = sDefnStrings[iOffset + 1];

                iPosn = sIndexStr.IndexOf(':');
                if (iPosn >= 0)                                                        // Integer subrange
                {
                    int start = Convert.ToInt32(sIndexStr.Substring(0, iPosn));
                    int endpos = Convert.ToInt32(sIndexStr.Substring(iPosn + 1, sIndexStr.Length - iPosn - 1));
                    for (Idx = start; Idx <= endpos; Idx++)
                    {
                        sSubDefnStrings[iOffset + 1] = Convert.ToString(Idx);
                        Array.Resize(ref FItems, FItems.Length + 1);
                        FItems[FItems.Length - 1] = new TParameterDefinition(sSubDefnStrings, aType, iOffset + 1);
                    }
                }
                else                                                                       // Single index or semi-colon-separated  }
                {                                                                      //   list of indices                     }
                    while (sIndexStr != "")
                    {
                        iPosn = sIndexStr.IndexOf(";");
                        if (iPosn >= 0)
                        {
                            sSubDefnStrings[iOffset + 1] = sIndexStr.Substring(0, iPosn);
                            sIndexStr = sIndexStr.Substring(iPosn + 1, sIndexStr.Length - iPosn - 1);
                        }
                        else
                        {
                            sSubDefnStrings[iOffset + 1] = sIndexStr;
                            sIndexStr = "";
                        }

                        Array.Resize(ref FItems, FItems.Length + 1);
                        FItems[FItems.Length - 1] = new TParameterDefinition(sSubDefnStrings, aType, iOffset + 1);
                    }
                }
            }

            FCount = FItems.Length;
            if (bIsScalar())
                FParamCount = 1;
            else
                FParamCount = FCount * item(0).iParamCount;
        }

        /// <summary>
        /// Initalises TParameterDefinition from another instance
        /// </summary>
        /// <param name="Source"></param>
        public TParameterDefinition(TParameterDefinition Source)
        {
            FItems = new TParameterDefinition[0];
            int Idx;

            FName = Source.sFullName;
            FNamePart = Source.sPartName;
            FType = Source.paramType;
            FCount = Source.iCount;

            if (FCount > 0)
            {
                Array.Resize(ref FItems, FCount);
                for (Idx = 0; Idx <= FCount - 1; Idx++)
                    FItems[Idx] = new TParameterDefinition(Source.item(Idx));
            }
            else
                FItems = null;

            FParamCount = Source.iParamCount;
            FDefined = Source.FDefined;
        }
        /// <summary>
        /// Full name of the parameter
        /// </summary>
        public string sFullName
        {
            get { return FName; }
        }
        /// <summary>
        /// Part name of the parameter
        /// </summary>
        public string sPartName
        {
            get { return FNamePart; }
        }
        /// <summary>
        /// Parameter type
        /// </summary>
        public TTypedValue.TBaseType paramType
        {
            get { return FType; }
        }
        /// <summary>
        /// Is a scalar
        /// </summary>
        /// <returns></returns>
        public bool bIsScalar()
        {
            return (FItems.Length == 0);
        }
        /// <summary>
        /// Get the dimension of the parameter
        /// </summary>
        /// <returns></returns>
        public int iDimension()
        {
            if (bIsScalar())
                return 0;
            else
                return 1 + this.item(0).iDimension();
        }
        /// <summary>
        /// Count
        /// </summary>
        public int iCount
        {
            get { return FCount; }
        }
        /// <summary>
        /// Get item
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TParameterDefinition item(int Idx)
        {
            return FItems[Idx];
        }
        /// <summary>
        /// Parameter count
        /// </summary>
        public int iParamCount
        {
            get { return FParamCount; }
        }
        /// <summary>
        /// Get the parameter definition at Idx
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TParameterDefinition getParam(int Idx)
        {
            int iDefn;
            int iOffset;
            TParameterDefinition result;

            if (bIsScalar() && (Idx == 0))
                result = this;
            else if ((Idx < 0) || (Idx >= iParamCount))
                result = null;
            else
            {
                iDefn = 0;
                iOffset = 0;
                while ((iDefn < iCount) && (iOffset + item(iDefn).iParamCount <= Idx))
                {
                    iOffset = iOffset + item(iDefn).iParamCount;
                    iDefn++;
                }
                result = item(iDefn).getParam(Idx - iOffset);
            }
            return result;
        }
        /// <summary>
        /// Find parameter by name
        /// </summary>
        /// <param name="sParam"></param>
        /// <param name="iOffset"></param>
        /// <returns></returns>
        public TParameterDefinition findParam(string[] sParam, int iOffset = 0)
        {
            int Idx;

            TParameterDefinition result;
            if (sPartName != sParam[iOffset])
                result = null;
            else if (iOffset == sParam.Length - 1)
                result = this;
            else
            {
                result = null;
                Idx = 0;
                while ((Idx < iCount) && (result == null))
                {
                    result = item(Idx).findParam(sParam, iOffset + 1);
                    Idx++;
                }
            }
            return result;
        }
        /// <summary>
        /// Returns true if the parameter is defined
        /// </summary>
        /// <param name="sParam"></param>
        /// <returns></returns>
        public bool bParamDefined(string[] sParam)
        {
            TParameterDefinition aParam;

            aParam = findParam(sParam, 0);
            return ((aParam != null) && (aParam.bIsScalar()));
        }
        /// <summary>
        /// Returns true if a value is defined
        /// </summary>
        /// <returns></returns>
        public bool bValueDefined()
        {
            int Idx;
            bool result;

            if (bIsScalar())
                result = FDefined;
            else
            {
                result = true;
                for (Idx = 0; Idx <= iCount - 1; Idx++)
                    result = (result && item(Idx).bValueDefined());
            }
            return result;
        }
        /// <summary>
        /// Set defined
        /// </summary>
        /// <param name="bValue"></param>
        public void setDefined(bool bValue)
        {
            if (bIsScalar())
                FDefined = bValue;
        }
        /// <summary>
        /// Set defined for the source
        /// </summary>
        /// <param name="Source"></param>
        public void setDefined(TParameterDefinition Source)
        {
            int Idx;

            if (bIsScalar())
                FDefined = Source.FDefined;
            else
            {
                for (Idx = 0; Idx <= iCount - 1; Idx++)
                    FItems[Idx].setDefined(Source.FItems[Idx]);
            }
        }
    }

    /// <summary>
    /// Encoding translation
    /// </summary>
    [Serializable]
    public struct TTranslation
    {
        /// <summary>
        /// Language
        /// </summary>
        public string sLang;
        /// <summary>
        ///
        /// </summary>
        public string sText;
    }

    //=================================================================================
    /// <summary>
    /// Parameter set class
    /// </summary>
    [Serializable]
    public class TParameterSet
    {
        /// <summary>
        /// Real-single type
        /// </summary>
        protected const TTypedValue.TBaseType ptyReal = TTypedValue.TBaseType.ITYPE_SINGLE;
        /// <summary>
        /// Integer type
        /// </summary>
        protected const TTypedValue.TBaseType ptyInt = TTypedValue.TBaseType.ITYPE_INT4;
        /// <summary>
        /// Boolean type
        /// </summary>
        protected const TTypedValue.TBaseType ptyBool = TTypedValue.TBaseType.ITYPE_BOOL;
        /// <summary>
        /// String type
        /// </summary>
        protected const TTypedValue.TBaseType ptyText = TTypedValue.TBaseType.ITYPE_STR;

        private string FVersion;
        private string FName;
        private string FEnglishName;
        private string[] FLocales = new string[0];
        private TTranslation[] FTranslations = new TTranslation[0];
        private TParameterSet FParent;
        private TParameterSet[] FChildren;

        private TParameterDefinition[] FDefinitions = new TParameterDefinition[0];

        private string FCurrLocale;
        private string FFileSource;
        private string UILang = "";

        /// <summary>
        /// Registry key used to store setting
        /// </summary>
        public const string PARAM_KEY = "\\Software\\CSIRO\\Common\\Parameters"; // Base registry key for parameter info
        /// <summary>
        /// Represents all locales
        /// </summary>
        public const string ALL_LOCALES = "#all#";

        /// <summary>
        /// Propagates the new current locale to the entire parameter set
        /// </summary>
        /// <param name="sValue"></param>
        private void setCurrLocale(string sValue)
        {
            TParameterSet Ancestor;
            int Idx;

            Ancestor = this;
            if (Ancestor != null)
            {
                while (Ancestor.Parent != null)
                    Ancestor = Ancestor.Parent;

                for (Idx = 0; Idx <= Ancestor.iNodeCount() - 1; Idx++)
                    Ancestor.getNode(Idx).FCurrLocale = sValue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagDefinition"></param>
        /// <param name="aType"></param>
        protected void defineParameters(string sTagDefinition, TTypedValue.TBaseType aType)
        {
            string[] sDefn = new string[0];
            int Kdx;

            Tokenise(sTagDefinition, ref sDefn, "-");
            Kdx = FDefinitions.Length;
            Array.Resize(ref FDefinitions, Kdx + 1);
            FDefinitions[Kdx] = new TParameterDefinition(sDefn, aType);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="StrA"></param>
        /// <param name="Idx"></param>
        /// <returns></returns>
        protected int iIndex(string[] StrA, int Idx)
        {
            return Convert.ToInt32(StrA[Idx]);
        }

        /// <summary>
        /// Parse tokens from the string
        /// </summary>
        /// <param name="sList">Input string</param>
        /// <param name="sTags">Parsed tokens</param>
        /// <param name="sDelim">Delimiter to use</param>
        private void Tokenise(string sList, ref string[] sTags, string sDelim)
        {
            int iLength;
            int iPosn;

            sList = sList.ToLower();
            iLength = 0;
            Array.Resize(ref sTags, iLength);
            while (sList != "")
            {
                iLength++;
                Array.Resize(ref sTags, iLength);

                iPosn = sList.IndexOf(sDelim);
                if (iPosn >= 0)
                {
                    sTags[iLength - 1] = sList.Substring(0, iPosn);
                    sList = sList.Remove(0, iPosn + 1);
                }
                else
                {
                    sTags[iLength - 1] = sList;
                    sList = "";
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="srcSet"></param>
        /// <param name="Defn"></param>
        protected void copyDefinition(TParameterSet srcSet, TParameterDefinition Defn)
        {
            int Idx;

            if ((Defn.bIsScalar()) && (srcSet != null) && (srcSet.bIsDefined(Defn.sFullName)))
            {
                switch (Defn.paramType)
                {
                    case ptyReal: setParam(Defn.sFullName, srcSet.fParam(Defn.sFullName));
                        break;
                    case ptyInt: setParam(Defn.sFullName, srcSet.iParam(Defn.sFullName));
                        break;
                    case ptyBool: setParam(Defn.sFullName, srcSet.bParam(Defn.sFullName));
                        break;
                    case ptyText: setParam(Defn.sFullName, srcSet.sParam(Defn.sFullName));
                        break;
                }
            }
            else if (Defn.bIsScalar())
                setUndefined(Defn.sFullName);
            else
                for (Idx = 0; Idx <= Defn.iCount - 1; Idx++)
                    copyDefinition(srcSet, Defn.item(Idx));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="srcSet"></param>
        /// <param name="bCopyData"></param>
        virtual protected void copyParams(TParameterSet srcSet, bool bCopyData)
        {
            int Kdx;
            try
            {
                if (srcSet != null)
                {
                    FVersion = srcSet.FVersion;
                    FName = srcSet.FName;
                    FEnglishName = srcSet.FEnglishName;

                    Array.Resize(ref FLocales, srcSet.FLocales.Length);
                    for (Kdx = 0; Kdx <= FLocales.Length - 1; Kdx++)
                    {
                        FLocales[Kdx] = srcSet.FLocales[Kdx];
                    }
                    FCurrLocale = srcSet.sCurrLocale;
                    Array.Resize(ref FTranslations, srcSet.FTranslations.Length);
                    for (Kdx = 0; Kdx <= FTranslations.Length - 1; Kdx++)
                    {
                        FTranslations[Kdx] = srcSet.FTranslations[Kdx];
                    }
                }

                if (bCopyData)
                {
                    for (Kdx = 0; Kdx <= (iDefinitionCount() - 1); Kdx++)
                    {
                        copyDefinition(srcSet, getDefinition(Kdx));
                    }
                    deriveParams();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception in copyParams(): " + e.Message);
            }

        }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        virtual protected TParameterSet makeChild()
        {
            return new TParameterSet(this);
        }
        /// <summary>
        /// This is over-ridden in descendant classes and then called within the
        /// </summary>
        virtual protected void defineEntries()
        {
        }
        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        virtual protected double getRealParam(string[] sTagList)
        {
            return 0;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        virtual protected int getIntParam(string[] sTagList)
        {
            return 0;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        virtual protected bool getBoolParam(string[] sTagList)
        {
            return false;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        virtual protected string getTextParam(string[] sTagList)
        {
            return "";
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="fValue"></param>
        virtual protected void setRealParam(string[] sTagList, double fValue)
        {
            //empty
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="iValue"></param>
        virtual protected void setIntParam(string[] sTagList, int iValue)
        {
            //empty
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="bValue"></param>
        virtual protected void setBoolParam(string[] sTagList, bool bValue)
        {
            //empty
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="sValue"></param>
        virtual protected void setTextParam(string[] sTagList, string sValue)
        {
            //empty
        }
        /// <summary>
        /// Constructor for the root set
        /// </summary>
        public TParameterSet()
        {
            init(null, null);
            FParent = null;
            FChildren = null;
        }
        /// <summary>
        /// Constructor for creating a child set
        /// </summary>
        /// <param name="aParent"></param>
        public TParameterSet(TParameterSet aParent)
        {
            init(aParent, aParent);
        }
        //// <summary>
        //// Copy constructor (with parent)
        //// </summary>
        //// <param name="aParent"></param>
        /*public TParameterSet(TParameterSet aParent)
        {
            FParent = aParent;
        }*/
        /// <summary>
        /// Copy constructor (with parent)
        /// </summary>
        /// <param name="aParent"></param>
        /// <param name="srcSet"></param>
        public TParameterSet(TParameterSet aParent, TParameterSet srcSet)
        {
            init(aParent, srcSet);
        }

        private void init(TParameterSet aParent, TParameterSet srcSet)
        {
            FParent = aParent;
            FChildren = null;

            defineEntries();
            copyParams(srcSet, true);

            if (FParent != null)
                sVersion = FParent.sVersion;
            else if (srcSet != null)
                sVersion = srcSet.sVersion;
        }

        /// <summary>
        /// After calling the constructor, this must be called to
        /// configure the definitions.
        /// </summary>
        /// <param name="srcSet"></param>
        public void ConstructCopy(TParameterSet srcSet)
        {
            defineEntries();
            copyParams(srcSet, true);

            if (FParent != null)
                sVersion = FParent.sVersion;
            else if (srcSet != null)
                sVersion = srcSet.sVersion;
        }

        /// <summary>
        /// This is over-ridden in descendant classes and then called within the
        /// </summary>
        virtual public void deriveParams()
        {
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="srcSet"></param>
        public void CopyAll(TParameterSet srcSet)
        {
            int Idx;

            copyParams(srcSet, true);

            while (iChildCount() > srcSet.iChildCount())
            {
                deleteChild(iChildCount() - 1);
            }
            while (iChildCount() < srcSet.iChildCount())
            {
                addChild();
            }
            for (Idx = 0; Idx <= iChildCount() - 1; Idx++)
            {
                getChild(Idx).CopyAll(srcSet.getChild(Idx));
            }
        }
        /// <summary>
        /// Version
        /// </summary>
        public string sVersion
        {
            get { return FVersion; }
            set { FVersion = value; }
        }
        /// <summary>
        /// Name
        /// </summary>
        public string sName
        {
            get { return FName; }
            set { FName = value; }
        }
        /// <summary>
        /// English name
        /// </summary>
        public string sEnglishName
        {
            get { return FEnglishName; }
            set { FEnglishName = value; }
        }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public int iTranslationCount()
        {
            if (FLocales != null)
                return FTranslations.Length;
            else
                return 0;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="text"></param>
        public void addTranslation(string lang, string text)
        {
            bool found;
            int Idx;

            found = false;
            for (Idx = 0; Idx <= FTranslations.Length - 1; Idx++)
            {
                if (String.Compare(FTranslations[Idx].sLang, lang, true) == 0)
                {
                    FTranslations[Idx].sText = text;
                    found = true;
                }
            }
            if (!found)
            {
                Idx = FTranslations.Length;
                Array.Resize(ref FTranslations, Idx + 1);
                FTranslations[Idx].sLang = lang;
                FTranslations[Idx].sText = text;
            }
            if ((String.Compare("en", lang, true) == 0) || (String.Compare(sName, "") == 0))
                sName = text;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TTranslation getTranslation(int Idx)
        {
            return FTranslations[Idx];
        }
        /// <summary>
        /// Delete the translation at index
        /// </summary>
        /// <param name="Idx"></param>
        public void deleteTranslation(int Idx)
        {
            if ((Idx >= 0) && (Idx < FTranslations.Length))
            {
                for (int i = (Idx + 1); i <= FTranslations.Length - 1; i++)
                {
                    FTranslations[i - 1] = FTranslations[i];
                }
                Array.Resize(ref FTranslations, FTranslations.Length - 1);
            }
        }
        /// <summary>
        /// Count of locales
        /// </summary>
        /// <returns></returns>
        public int iLocaleCount()
        {
            if (FLocales != null)
                return FLocales.Length;
            else
                return 0;
        }
        /// <summary>
        /// Get local at index
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public string getLocale(int Idx)
        {
            return FLocales[Idx];
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sLocale"></param>
        public void addLocale(string sLocale)
        {
            int Idx = FLocales.Length;
            Array.Resize(ref FLocales, Idx + 1);
            FLocales[Idx] = sLocale;
        }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string getLocaleText()
        {
            string result = "";
            for (int Idx = 0; Idx <= iLocaleCount() - 1; Idx++)
            {
                if (Idx == 0)
                {
                    result = result + getLocale(Idx);
                }
                else
                {
                    result = result + ";" + getLocale(Idx);
                }
            }
            return result;
        }
        /// <summary>
        /// TODO: Test this
        /// </summary>
        /// <param name="sText"></param>
        public void setLocaleText(string sText)
        {
            int iPosn;

            Array.Resize(ref FLocales, 0);

            sText = sText.Trim();
            while (sText != "")
            {
                iPosn = sText.IndexOf(";");
                if (iPosn < 0)
                {
                    addLocale(sText);
                    sText = String.Empty;
                }
                else
                {
                    addLocale((sText.Substring(0, iPosn).Trim()));
                    sText = sText.Remove(0, iPosn + 1);
                }
            }
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sLocale"></param>
        /// <returns></returns>
        public bool bInLocale(string sLocale)
        {
            int Idx;

            bool result = false;

            if (sLocale == ALL_LOCALES)
                result = true;
            else
            {
                result = false;
                sLocale = sLocale.ToLower();
                Idx = 0;
                while (!result && (Idx < FLocales.Length))
                {
                    result = (sLocale == (FLocales[Idx]).ToLower());
                    Idx++;
                }
            }

            return result;
        }
        /// <summary>
        /// Current locale
        /// </summary>
        public string sCurrLocale
        {
            get { return FCurrLocale; }
            set { setCurrLocale(value); }
        }
        /// <summary>
        /// Parent parameter set
        /// </summary>
        public TParameterSet Parent
        {
            get { return FParent; }
        }
        /// <summary>
        /// Count of children
        /// </summary>
        /// <returns></returns>
        public int iChildCount()
        {
            if (FChildren != null)
                return FChildren.Length;
            else
                return 0;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TParameterSet getChild(int Idx)
        {
            return FChildren[Idx];
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sChild"></param>
        /// <returns></returns>
        public TParameterSet getChild(string sChild)
        {
            int Idx;

            sChild = sChild.ToLower().Trim();
            Idx = 0;
            while ((Idx < iChildCount()) && (sChild != getChild(Idx).sName.ToLower()))
                Idx++;

            if (Idx < iChildCount())
                return getChild(Idx);
            else
                return null;
        }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public TParameterSet addChild()
        {
            TParameterSet result = makeChild();
            if (FChildren == null)
                FChildren = new TParameterSet[0];
            Array.Resize(ref FChildren, FChildren.Length + 1);
            FChildren[FChildren.Length - 1] = result;
            return FChildren[FChildren.Length - 1];
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="Idx"></param>
        public void deleteChild(int Idx)
        {
            int Jdx;

            for (Jdx = Idx + 1; Jdx <= iChildCount() - 1; Jdx++)
            {
                FChildren[Jdx - 1] = FChildren[Jdx];
            }
            Array.Resize(ref FChildren, FChildren.Length - 1);
        }
        /// <summary>
        /// Returns TRUE i.f.f. this is the root (ultimate parent) node
        /// </summary>
        /// <returns></returns>
        public bool bRootNode()
        {
            return (Parent == null);
        }
        /// <summary>
        /// Returns TRUE i.f.f. this node has no child nodes
        /// </summary>
        /// <returns></returns>
        public bool bLeafNode()
        {
            return (Parent != null) && ((FChildren == null) || (FChildren.Length == 0));
        }
        /// <summary>
        /// Total number of nodes in the tree of parameter sets, including the current node
        /// </summary>
        /// <returns></returns>
        public int iNodeCount()
        {
            int Idx;

            int result = 1;                                                                 // Include Self in the count of nodes
            for (Idx = 0; Idx <= iChildCount() - 1; Idx++)
                result = result + getChild(Idx).iNodeCount();
            return result;
        }

        /// <summary>
        /// Locate a node in the tree by ordinal value. Node 0 is the current node;
        /// the search then proceeds depth-first (i.e. the first child of node N is node N+1)
        /// </summary>
        /// <param name="iNode"></param>
        /// <returns></returns>
        public TParameterSet getNode(int iNode)
        {
            int iOffset, Idx;
            TParameterSet result;

            if (iNode == 0)
                result = this;
            else
            {
                iOffset = 1;
                result = null;
                Idx = 0;
                while ((Idx < iChildCount()) && (result == null))
                {
                    result = getChild(Idx).getNode(iNode - iOffset);
                    if (result == null)
                    {
                        iOffset = iOffset + getChild(Idx).iNodeCount();
                        Idx++;
                    }
                }
            }
            return result;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sNode"></param>
        /// <returns></returns>
        public TParameterSet getNode(string sNode)
        {
            int Idx;
            TParameterSet result;

            sNode = sNode.ToLower().Trim();
            if (sNode == this.sName.ToLower())
                result = this;
            else
            {
                result = getChild(sNode);
                Idx = 0;
                while ((Idx < iChildCount()) && (result == null))
                {
                    result = getChild(Idx).getNode(sNode);
                    Idx++;
                }
            }
            return result;
        }
        /// <summary>
        /// Leaf count
        /// </summary>
        /// <param name="bUseLocale"></param>
        /// <returns></returns>
        public int iLeafCount(bool bUseLocale = true)
        {
            int Idx;

            int result = 0;
            if (!bLeafNode())
                for (Idx = 0; Idx <= iChildCount() - 1; Idx++)
                {
                    result = result + getChild(Idx).iLeafCount(bUseLocale);
                }
            else if (!bUseLocale || bInLocale(sCurrLocale))
                result = 1;

            return result;
        }

        /// <summary>
        /// Locate leaf nodes that match a locale
        /// </summary>
        /// <param name="iLeaf"></param>
        /// <param name="bUseLocale"></param>
        /// <returns></returns>
        public TParameterSet getLeaf(int iLeaf, bool bUseLocale = true)
        {
            TParameterSet Node;
            int Idx, Jdx;
            TParameterSet result;

            result = null;
            int iCount = iNodeCount();
            Idx = 0;
            Jdx = 0;
            while ((Idx < iCount) && (result == null))
            {
                Node = getNode(Idx);
                if (Node.bLeafNode() && (!bUseLocale || Node.bInLocale(sCurrLocale)))
                {
                    if (Jdx == iLeaf)
                        result = Node;
                    Jdx++;
                }
                Idx++;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public int iDefinitionCount()
        {
            if (FDefinitions != null)
                return FDefinitions.Length;
            else
                return 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TParameterDefinition getDefinition(int Idx)
        {
            return FDefinitions[Idx];
        }
        /// <summary>
        /// Get the parameter definition for sTag
        /// </summary>
        /// <param name="sTag"></param>
        /// <returns></returns>
        public TParameterDefinition getDefinition(string sTag)
        {
            TParameterDefinition result = null;
            string[] sTagList = new string[0];
            bool bPartName;

            if (sTag == "")
                result = null;
            else
            {
                bPartName = (sTag[sTag.Length - 1] == '-');
                if (bPartName)
                    sTag = sTag.Remove(sTag.Length - 1, 1);
                Tokenise(sTag, ref sTagList, "-");
                result = getDefinition(sTagList);
                if ((result != null) && (bPartName == result.bIsScalar()))
                    result = null;
            }
            return result;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sTags"></param>
        /// <returns></returns>
        public TParameterDefinition getDefinition(string[] sTags)
        {
            int Idx;
            int defCount;

            TParameterDefinition result = null;
            Idx = 0;
            defCount = iDefinitionCount();
            while ((Idx < defCount) && (result == null))
            {
                result = FDefinitions[Idx].findParam(sTags);
                Idx++;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public int iParamCount()
        {
            int Idx;
            int defCount;

            int result = 0;
            defCount = iDefinitionCount();
            for (Idx = 0; Idx <= defCount - 1; Idx++)
                result = result + getDefinition(Idx).iParamCount;

            return result;
        }

        /// <summary>
        /// Get parameter at index
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TParameterDefinition getParam(int Idx)
        {
            int iDefn = 0;
            int iOffset = 0;
            int defCount = iDefinitionCount();
            while ((iDefn < defCount) && (iOffset + getDefinition(iDefn).iParamCount <= Idx))
            {
                iOffset = iOffset + getDefinition(iDefn).iParamCount;
                iDefn++;
            }

            if (iDefn < defCount)
                return getDefinition(iDefn).getParam(Idx - iOffset);
            else
                return null;
        }
        /// <summary>
        /// Returns a floating-point parameter
        /// </summary>
        /// <param name="sTag"></param>
        /// <returns>Returns a floating-point parameter</returns>
        public double fParam(string sTag)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;
            double result = 0;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else if (!Definition.bValueDefined())
                throw new Exception("Parameter value undefined: " + sTag);
            else
                result = getRealParam(sTagList);

            return result;
        }

        /// <summary>
        /// Returns an integer parameter.
        /// </summary>
        /// <param name="sTag"></param>
        /// <returns></returns>
        public int iParam(string sTag)
        {

            string[] sTagList = new string[0];
            TParameterDefinition Definition;
            int result = 0;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else if (!(Definition.bValueDefined()))
                throw new Exception("Parameter value undefined: " + sTag);
            else
                result = getIntParam(sTagList);

            return result;
        }

        /// <summary>
        /// Returns a Boolean parameter.
        /// </summary>
        /// <param name="sTag"></param>
        /// <returns></returns>
        public bool bParam(string sTag)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;
            bool result = false;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else if (!(Definition.bValueDefined()))
                throw new Exception("Parameter value undefined: " + sTag);
            else
                result = getBoolParam(sTagList);

            return result;
        }

        /// <summary>
        /// Returns a string parameter.
        /// </summary>
        /// <param name="sTag"></param>
        /// <returns></returns>
        public string sParam(string sTag)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;
            string result = "";

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else if (!(Definition.bValueDefined()))
                throw new Exception("Parameter value undefined: " + sTag);
            else
            {
                switch (Definition.paramType)
                {
                    case ptyText: result = getTextParam(sTagList);
                        break;
                    case ptyReal: result = String.Format("%f", getRealParam(sTagList));
                        break;
                    case ptyInt: result = String.Format("%d", getIntParam(sTagList));
                        break;
                    case ptyBool: if (getBoolParam(sTagList))
                            result = "true";
                        else
                            result = "false";
                        break;
                }
                return result;
            }
        }

        /// <summary>
        /// Sets the value of a floating-point parameter, noting that its value is now defined
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="fValue"></param>
        public void setParam(string sTag, double fValue)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else
            {
                setRealParam(sTagList, fValue);
                Definition.setDefined(true);
            }
        }
        /// <summary>
        /// Sets the value of an integer parameter, noting that its value is now defined
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="iValue"></param>
        public void setParam(string sTag, int iValue)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else
            {
                setIntParam(sTagList, iValue);
                Definition.setDefined(true);
            }

        }
        /// <summary>
        /// Sets the value of a Boolean parameter, noting that its value is now defined
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="bValue"></param>
        public void setParam(string sTag, bool bValue)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !Definition.bIsScalar())
                throw new Exception("Invalid parameter name " + sTag);
            else
            {
                setBoolParam(sTagList, bValue);
                Definition.setDefined(true);
            }
        }
        /// <summary>
        /// 1. Sets the value of a string parameter, noting that its value is now defined.
        /// 2. Parses and sets the value of a floating-point, integer or Boolean parameter.
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="sValue"></param>
        public void setParam(string sTag, string sValue)
        {
            string[] sTagList = new string[0];
            TParameterDefinition Definition;
            double fValue;
            int iValue;

            Tokenise(sTag, ref sTagList, "-");
            Definition = getDefinition(sTagList);
            if ((Definition == null) || !Definition.bIsScalar())
                throw new Exception("Invalid parameter name " + sTag);
            else
            {
                switch (Definition.paramType)
                {
                    case ptyText: setTextParam(sTagList, sValue);
                        break;
                    case ptyReal:
                        {
                            try
                            {
                                fValue = System.Convert.ToDouble(sValue);
                                setRealParam(sTagList, fValue);
                            }
                            catch
                            {
                                throw new Exception("Error parsing parameter " + sTag + " = " + sValue);
                            }
                        }
                        break;
                    case ptyInt:
                        {
                            try
                            {
                                iValue = System.Convert.ToInt32(sValue);
                                setIntParam(sTagList, iValue);
                            }
                            catch
                            {
                                throw new Exception("Error parsing parameter " + sTag + " = " + sValue);
                            }
                        }
                        break;
                    case ptyBool:
                        {
                            if (sValue.ToLower() == "true")
                                setBoolParam(sTagList, true);
                            else if (sValue.ToLower() == "false")
                                setBoolParam(sTagList, false);
                            else
                                throw new Exception("Error parsing parameter " + sTag + " = " + sValue);
                        }
                        break;
                }
                Definition.setDefined(true);
            }
        }

        /// <summary>
        /// Un-defines a parameter value
        /// </summary>
        /// <param name="sTag"></param>
        public void setUndefined(string sTag)
        {
            TParameterDefinition Definition;

            Definition = getDefinition(sTag);
            if ((Definition == null) || !(Definition.bIsScalar()))
                throw new Exception("Invalid parameter name " + sTag);
            else
                Definition.setDefined(false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTag"></param>
        /// <returns>Returns TRUE i.f.f. the nominated parameter has a defined value</returns>
        public bool bIsDefined(string sTag)
        {
            TParameterDefinition Definition;

            Definition = getDefinition(sTag);
            return ((Definition != null) && (Definition.bValueDefined()));
        }
        /// <summary>
        /// Returns TRUE i.f.f. all parameters in the set have defined values
        /// </summary>
        /// <returns></returns>
        public bool bIsComplete()
        {
            int Idx;
            int defCount;

            bool result = true;
            defCount = iDefinitionCount();
            for (Idx = 0; Idx <= defCount - 1; Idx++)
            {
                result = (result && getDefinition(Idx).bValueDefined());
            }

            return result;
        }

        /// <summary>
        /// Convert the name
        /// </summary>
        public void localiseNames()
        {
            TParameterSet Ancestor, child;
            int Idx, itrans;
            TTranslation translation;

            Ancestor = this;
            if (Ancestor != null)
            {
                while (Ancestor.Parent != null)
                {
                    Ancestor = Ancestor.Parent;
                }

                for (Idx = 0; Idx <= Ancestor.iNodeCount() - 1; Idx++)
                {
                    child = Ancestor.getNode(Idx);
                    child.sName = child.sEnglishName; // Use English name if target language not found
                    for (itrans = 0; itrans <= child.iTranslationCount() - 1; itrans++)
                    {
                        translation = child.getTranslation(itrans);
                        if (String.Compare(translation.sLang, getUILang(), true) == 0)
                            child.sName = translation.sText;
                    }
                }
            }
        }
        /// <summary>
        /// Source file name
        /// </summary>
        public string FileSource
        {
            get { return FFileSource; }
            set { FFileSource = value; }
        }

        /// <summary>
        /// Returns the 2-letter ISO 639 language code, or 'en' if that fails
        /// TODO: Test this
        /// </summary>
        /// <returns></returns>
        public string getUILang()
        {
            string result;

            if (UILang != "")
                result = UILang;
            else
            {
                result = System.Environment.GetEnvironmentVariable("LANG");  // Allow environment variable to override system setting
                if (result.Length > 1)  // Assume a 2-letter code
                    result = result.Substring(0, 2);
                else
                    result = "en";
            }

            return result;
        }
        /// <summary>
        /// Set the IU language
        /// </summary>
        /// <param name="sLang"></param>
        public void setUILang(string sLang)
        {
            UILang = sLang;
        }
    }

    /*

   {==============================================================================}
   { RealToText                                                                   }
   { Real-to-string conversion, taking missing values into account.  Formats      }
   { a number with SIGDIGITS significant digits and then trims trailing zeroes    }
   { Parameters:                                                                  }
   {   X   Value to be converted                                                  }
   {==============================================================================}
   const
     SIGDIGITS = 7;

   function RealToText( X : Double ) : string;
   var
     AbsX  : Double;
     DecPl : Integer;
   begin
     AbsX  := Abs(X);
     if (AbsX = 0.0) then
       Result := '0.0'
     else if (AbsX < 1.0E-6) then
       Result := FloatToStrF( X, ffExponent, 15, SIGDIGITS )
     else
     begin
       DecPl := SIGDIGITS-1;
       while (DecPl > 0) and (AbsX >= 10.0) do
       begin
         Dec( DecPl );
         AbsX := 0.10 * AbsX;
       end;
       while (AbsX < 1.0) do
       begin
         Inc( DecPl );
         AbsX := 10.0 * AbsX;
       end;
       Result := FloatToStrF( X, ffFixed, 15, DecPl );
       if (DecPl > 0) then
         while (Copy(Result,Length(Result),1) = '0') and (Copy(Result,Length(Result)-1,2) <> '.0') do
           Delete( Result, Length(Result), 1 );
     end;
   end; {_ RealToText _}

*/
    /// <summary>
    /// Common locale for use across models and programs
    /// * The locale is a two-character country code that is stored in the registry.
    /// * If there is no entry in the registry, 'au' is returned.
    /// </summary>
    public static class GrazLocale
    {
        /// <summary>
        /// Common locale for use across models and programs
        /// * The locale is a two-character country code that is stored in the registry.
        /// * If there is no entry in the registry, 'au' is returned.
        /// </summary>
        /// <returns></returns>
        public static string sDefaultLocale()
        {
            string Result;
            string sRegString = "";
            /*       TRegistry Registry;
               //  CountryName: array[0..3] of char;

                 sRegString = "";
                 Registry   = new TRegistry();
                 if (Registry.OpenKey( PARAM_KEY, false ) )
                 try
                 {
                   sRegString = Registry.ReadString( "locale" );
                 }
                   catch()
                 {
                   } */
            if (sRegString == "")
            {
                //    // If locale (=ISO 3166 country code) is not specified in the registry, then query the OS
                //    if GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SISO3166CTRYNAME, CountryName, SizeOf(CountryName)) = 3 then
                //      Result := LowerCase(CountryName)
                //    else
                Result = "au"; // Australia is the default locale
            }
            else
                Result = sRegString.ToLower();
            return Result;
        }
        /*
           {==============================================================================}
           { bInLocale                                                                    }
           { * sLocaleList is a semicolon-delimited list of locale names                  }
           {==============================================================================}

           function bInLocale( sLocale, sLocaleList : string ): Boolean;
           begin
             if (sLocale = ALL_LOCALES) then
               Result := TRUE
             else
               Result := Pos( ';'+sLocale+';', ';'+Trim(sLocaleList)+';' ) <> 0;
           end;


           end.
           */
    }
}