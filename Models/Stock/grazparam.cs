namespace Models.GrazPlan
{
    using Models.Core;
    using System;
    using System.Globalization;

    /// <summary>
    /// ParameterDefinition                                                         
    /// Class used for the definition of parameter names and types, and for storing  
    /// whether the value(s) of the parameter is defined. A definition may be either 
    /// a single parameter, or it may be an array of parameter definitions. Array    
    /// definitions are indexed by string references                                 
    /// </summary>
    [Serializable]
    public class ParameterDefinition
    {
        /// <summary></summary>
        public string FName;
        /// <summary></summary>
        public string FNamePart;
        /// <summary></summary>
        public Type FType;
        /// <summary></summary>
        public ParameterDefinition[] FItems;
        /// <summary></summary>
        public int FCount;
        /// <summary></summary>
        public int FParamCount;
        /// <summary></summary>
        public bool FDefined;

        /// <summary>
        /// Constructor
        /// </summary>
        public ParameterDefinition()
        {

        }

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
        /// <param name="defnStrings">Definition strings</param>
        /// <param name="baseType">Base type</param>
        /// <param name="offset">Offset index</param>
        public ParameterDefinition(string[] defnStrings, Type baseType, int offset = 0)
        {
            this.FItems = new ParameterDefinition[0];
            string[] subDefnStrings = new string[0];
            string indexStr;
            int posIdx;
            int idx;

            this.FName = defnStrings[0];
            for (idx = 1; idx <= offset; idx++)
                this.FName = this.FName + "-" + defnStrings[idx];
            if (offset < (defnStrings.Length - 1))
                this.FName = this.FName + "-";
            this.FName = this.FName.ToLower();

            this.FNamePart = defnStrings[offset].ToLower();
            this.FType = baseType;
            this.FDefined = false;

            if (offset < (defnStrings.Length - 1))
            {
                Array.Resize(ref subDefnStrings, defnStrings.Length);
                for (idx = 0; idx <= (subDefnStrings.Length - 1); idx++)
                    subDefnStrings[idx] = defnStrings[idx];
                indexStr = defnStrings[offset + 1];

                posIdx = indexStr.IndexOf(':');
                if (posIdx >= 0)                                                                              
                {
                    // Integer subrange
                    int start = Convert.ToInt32(indexStr.Substring(0, posIdx), CultureInfo.InvariantCulture);
                    int endpos = Convert.ToInt32(indexStr.Substring(posIdx + 1, indexStr.Length - posIdx - 1), CultureInfo.InvariantCulture);
                    for (idx = start; idx <= endpos; idx++)
                    {
                        subDefnStrings[offset + 1] = Convert.ToString(idx, CultureInfo.InvariantCulture);
                        Array.Resize(ref this.FItems, this.FItems.Length + 1);
                        this.FItems[this.FItems.Length - 1] = new ParameterDefinition(subDefnStrings, baseType, offset + 1);
                    }
                }
                else                                                                          
                {
                    // Single index or semi-colon-separated list of indices
                    while (indexStr != string.Empty)
                    {
                        posIdx = indexStr.IndexOf(";");
                        if (posIdx >= 0)
                        {
                            subDefnStrings[offset + 1] = indexStr.Substring(0, posIdx);
                            indexStr = indexStr.Substring(posIdx + 1, indexStr.Length - posIdx - 1);
                        }
                        else
                        {
                            subDefnStrings[offset + 1] = indexStr;
                            indexStr = string.Empty;
                        }

                        Array.Resize(ref this.FItems, this.FItems.Length + 1);
                        this.FItems[this.FItems.Length - 1] = new ParameterDefinition(subDefnStrings, baseType, offset + 1);
                    }
                }
            }

            this.FCount = this.FItems.Length;
            if (this.IsScalar())
                this.FParamCount = 1;
            else
                this.FParamCount = this.FCount * this.Item(0).ParamCount;
        }

        /// <summary>
        /// Initalises ParameterDefinition from another instance
        /// </summary>
        /// <param name="sourceParam">The source parameter</param>
        public ParameterDefinition(ParameterDefinition sourceParam)
        {
            this.FItems = new ParameterDefinition[0];
            int idx;

            this.FName = sourceParam.FullName;
            this.FNamePart = sourceParam.PartName;
            this.FType = sourceParam.ParamType;
            this.FCount = sourceParam.Count;

            if (this.FCount > 0)
            {
                Array.Resize(ref this.FItems, this.FCount);
                for (idx = 0; idx <= this.FCount - 1; idx++)
                    this.FItems[idx] = new ParameterDefinition(sourceParam.Item(idx));
            }
            else
                this.FItems = null;

            this.FParamCount = sourceParam.ParamCount;
            this.FDefined = sourceParam.FDefined;
        }

        /// <summary>
        /// Gets the full name of the parameter
        /// </summary>
        public string FullName
        {
            get { return this.FName; }
        }

        /// <summary>
        /// Gets the part name of the parameter
        /// </summary>
        public string PartName
        {
            get { return this.FNamePart; }
        }

        /// <summary>
        /// Gets the parameter type
        /// </summary>
        public Type ParamType
        {
            get { return this.FType; }
        }

        /// <summary>
        /// Is a scalar
        /// </summary>
        /// <returns>True if this is scalar</returns>
        public bool IsScalar()
        {
            return this.FItems.Length == 0;
        }

        /// <summary>
        /// Get the dimension of the parameter
        /// </summary>
        /// <returns>The dimension value</returns>
        public int Dimension()
        {
            if (this.IsScalar())
                return 0;
            else
                return 1 + this.Item(0).Dimension();
        }

        /// <summary>
        /// Gets the count of items
        /// </summary>
        public int Count
        {
            get { return this.FCount; }
        }

        /// <summary>
        /// Gets the item at the index
        /// </summary>
        /// <param name="idx">Item index 0-n</param>
        /// <returns>The item</returns>
        public ParameterDefinition Item(int idx)
        {
            return this.FItems[idx];
        }

        /// <summary>
        /// Parameter count
        /// </summary>
        public int ParamCount
        {
            get { return this.FParamCount; }
        }

        /// <summary>
        /// Get the parameter definition at Idx
        /// </summary>
        /// <param name="idx">The index</param>
        /// <returns>The parameter definition</returns>
        public ParameterDefinition getParam(int idx)
        {
            int defnIdx;
            int offset;
            ParameterDefinition result;

            if (this.IsScalar() && (idx == 0))
                result = this;
            else if ((idx < 0) || (idx >= this.ParamCount))
                result = null;
            else
            {
                defnIdx = 0;
                offset = 0;
                while ((defnIdx < this.Count) && (offset + this.Item(defnIdx).ParamCount <= idx))
                {
                    offset = offset + this.Item(defnIdx).ParamCount;
                    defnIdx++;
                }
                result = this.Item(defnIdx).getParam(idx - offset);
            }
            return result;
        }

        /// <summary>
        /// Find parameter by name
        /// </summary>
        /// <param name="param">Parameter array</param>
        /// <param name="offset">Offset value</param>
        /// <returns>The parameter definition</returns>
        public ParameterDefinition FindParam(string[] param, int offset = 0)
        {
            int idx;

            ParameterDefinition result;
            if (this.PartName != param[offset])
                result = null;
            else if (offset == param.Length - 1)
                result = this;
            else
            {
                result = null;
                idx = 0;
                while ((idx < this.Count) && (result == null))
                {
                    result = this.Item(idx).FindParam(param, offset + 1);
                    idx++;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if the parameter is defined
        /// </summary>
        /// <param name="param">Parameter array</param>
        /// <returns>True if found</returns>
        public bool ParamIsDefined(string[] param)
        {
            ParameterDefinition foundParam;

            foundParam = this.FindParam(param, 0);
            return (foundParam != null) && (foundParam.IsScalar());
        }

        /// <summary>
        /// Returns true if a value is defined
        /// </summary>
        /// <returns>True if defined</returns>
        public bool ValueIsDefined()
        {
            int idx;
            bool result;

            if (this.IsScalar())
                result = this.FDefined;
            else
            {
                result = true;
                for (idx = 0; idx <= this.Count - 1; idx++)
                    result = (result && this.Item(idx).ValueIsDefined());
            }
            return result;
        }

        /// <summary>
        /// Set defined
        /// </summary>
        /// <param name="isDefined">Whether it is defined</param>
        public void SetDefined(bool isDefined)
        {
            if (this.IsScalar())
                this.FDefined = isDefined;
        }

        /// <summary>
        /// Set defined for the source
        /// </summary>
        /// <param name="source"></param>
        public void SetDefined(ParameterDefinition source)
        {
            int idx;

            if (this.IsScalar())
                this.FDefined = source.FDefined;
            else
            {
                for (idx = 0; idx <= this.Count - 1; idx++)
                    this.FItems[idx].SetDefined(source.FItems[idx]);
            }
        }
    }

    /// <summary>
    /// Encoding translation
    /// </summary>
    [Serializable]
    public struct Translation
    {
        /// <summary>
        /// Language
        /// </summary>
        public string Lang;

        /// <summary>
        /// 
        /// </summary>
        public string Text;
    }

    // =================================================================================

    /// <summary>
    /// Parameter set class
    /// </summary>
    [Serializable]
    public class ParameterSet : Model
    {
        /// <summary>
        /// Real-single type
        /// </summary>
        protected static readonly Type TYPEREAL = typeof(float);

        /// <summary>
        /// Integer type
        /// </summary>
        protected static readonly Type TYPEINT = typeof(int);

        /// <summary>
        /// Boolean type
        /// </summary>
        protected static readonly Type TYPEBOOL = typeof(bool);

        /// <summary>
        /// String type
        /// </summary>
        protected static readonly Type TYPETEXT = typeof(string);

        private string FVersion;
        private string FEnglishName;
        private string[] FLocales = new string[0];
        private Translation[] FTranslations = new Translation[0];
        private ParameterSet FParent;
        /// <summary>
        /// 
        /// </summary>
        public ParameterSet[] FChildren { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ParameterDefinition[] FDefinitions { get; set; } = new ParameterDefinition[0];

        private string FCurrLocale;
        private string FFileSource;
        private string UILang = string.Empty;

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
        /// <param name="localeName"></param>
        private void SetCurrLocale(string localeName)
        {
            ParameterSet ancestor;
            int idx;

            ancestor = this;
            if (ancestor != null)
            {
                while (ancestor.ParentParameterSet != null)
                    ancestor = ancestor.ParentParameterSet;

                for (idx = 0; idx <= ancestor.NodeCount() - 1; idx++)
                    ancestor.GetNode(idx).FCurrLocale = localeName;
            }
        }

        /// <summary>
        /// Define parameter types
        /// </summary>
        /// <param name="tagDefinition">Tag definition</param>
        /// <param name="baseType">The definition type</param>
        protected void DefineParameters(string tagDefinition, Type baseType)
        {
            string[] defn = new string[0];
            int kdx;

            this.Tokenise(tagDefinition, ref defn, "-");
            kdx = this.FDefinitions.Length;
            var defs = FDefinitions;
            Array.Resize(ref defs, kdx + 1);
            FDefinitions = defs;
            this.FDefinitions[kdx] = new ParameterDefinition(defn, baseType);
        }

        /// <summary>
        /// Parse tokens from the string
        /// </summary>
        /// <param name="strList">Input string</param>
        /// <param name="tags">Parsed tokens</param>
        /// <param name="delim">Delimiter to use</param>
        private void Tokenise(string strList, ref string[] tags, string delim)
        {
            int length;
            int posnIdx;

            strList = strList.ToLower();
            length = 0;
            Array.Resize(ref tags, length);
            while (strList != string.Empty)
            {
                length++;
                Array.Resize(ref tags, length);

                posnIdx = strList.IndexOf(delim);
                if (posnIdx >= 0)
                {
                    tags[length - 1] = strList.Substring(0, posnIdx);
                    strList = strList.Remove(0, posnIdx + 1);
                }
                else
                {
                    tags[length - 1] = strList;
                    strList = string.Empty;
                }
            }
        }

        /// <summary>
        /// Copy the parameter definitions
        /// </summary>
        /// <param name="srcSet">Source parameter set</param>
        /// <param name="defn">Parameter definitions</param>
        protected void CopyDefinition(ParameterSet srcSet, ParameterDefinition defn)
        {
            int Idx;

            if (defn.IsScalar() && (srcSet != null) && (srcSet.IsDefined(defn.FullName)))
            {
                if (defn.ParamType == TYPEREAL)
                    this.SetParam(defn.FullName, srcSet.ParamReal(defn.FullName));
                else if (defn.ParamType == TYPEINT)
                    this.SetParam(defn.FullName, srcSet.ParamInt(defn.FullName));
                else if (defn.ParamType == TYPEBOOL)
                    this.SetParam(defn.FullName, srcSet.ParamBool(defn.FullName));
                else if (defn.ParamType == TYPETEXT)
                    this.SetParam(defn.FullName, srcSet.ParamStr(defn.FullName));
            }
            else if (defn.IsScalar())
                this.SetUndefined(defn.FullName);
            else
                for (Idx = 0; Idx <= defn.Count - 1; Idx++)
                    this.CopyDefinition(srcSet, defn.Item(Idx));
        }

        /// <summary>
        /// Copy the parameter set
        /// </summary>
        /// <param name="srcSet">Source parameter set</param>
        /// <param name="copyData">Copy the data</param>
        protected virtual void CopyParams(ParameterSet srcSet, bool copyData)
        {
            int kdx;

            if (srcSet != null)
            {
                this.FVersion = srcSet.FVersion;
                this.Name = srcSet.Name;
                this.FEnglishName = srcSet.FEnglishName;

                Array.Resize(ref this.FLocales, srcSet.FLocales.Length);
                for (kdx = 0; kdx <= this.FLocales.Length - 1; kdx++)
                {
                    this.FLocales[kdx] = srcSet.FLocales[kdx];
                }
                this.FCurrLocale = srcSet.CurrLocale;
                Array.Resize(ref this.FTranslations, srcSet.FTranslations.Length);
                for (kdx = 0; kdx <= this.FTranslations.Length - 1; kdx++)
                {
                    this.FTranslations[kdx] = srcSet.FTranslations[kdx];
                }
            }

            if (copyData)
            {
                for (kdx = 0; kdx <= (this.DefinitionCount() - 1); kdx++)
                {
                    this.CopyDefinition(srcSet, this.GetDefinition(kdx));
                }
                this.DeriveParams();
            }
        }

        /// <summary>
        /// Make a parameter set child
        /// </summary>
        /// <returns>A new parameter set</returns>
        protected virtual ParameterSet MakeChild()
        {
            return new ParameterSet(this);
        }

        /// <summary>
        /// This is over-ridden in descendant classes and then called within the         
        /// </summary>
        protected virtual void DefineEntries()
        {
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <returns>The float value</returns>
        protected virtual double GetRealParam(string[] tagList)
        {
            return 0;
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <returns>The integer value</returns>
        protected virtual int GetIntParam(string[] tagList)
        {
            return 0;
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <returns>The Boolean value</returns>
        protected virtual bool GetBoolParam(string[] tagList)
        {
            return false;
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <returns>The text value</returns>
        protected virtual string GetTextParam(string[] tagList)
        {
            return string.Empty;
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <param name="value">Double value</param>
        protected virtual void SetRealParam(string[] tagList, double value)
        {
            // empty
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <param name="value">Integer value</param>
        protected virtual void SetIntParam(string[] tagList, int value)
        {
            // empty
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <param name="value">Boolean value</param>
        protected virtual void SetBoolParam(string[] tagList, bool value)
        {
            // empty
        }

        /// <summary>
        /// These routines are over-ridden in descendant classes
        /// </summary>
        /// <param name="tagList">Array of tags</param>
        /// <param name="value">Text value</param>
        protected virtual void SetTextParam(string[] tagList, string value)
        {
            // empty
        }

        /// <summary>
        /// Constructor for the root set
        /// </summary>
        public ParameterSet()
        {
            // new ParameterSet(null, null);
            this.FParent = null;
        }

        // Constructor for creating a child set
        /*public ParameterSet(ParameterSet aParent)
        {
            new ParameterSet(aParent, aParent);
        }*/

        /// <summary>
        /// Copy constructor (with parent)
        /// </summary>
        /// <param name="parent">Parent parameter set</param>
        public ParameterSet(ParameterSet parent)
        {
            this.FParent = parent;
        }

        /// <summary>
        /// After calling the constructor, this must be called to 
        /// configure the definitions.
        /// </summary>
        /// <param name="srcSet">The source parameter set</param>
        public void ConstructCopy(ParameterSet srcSet)
        {
            this.DefineEntries();
            this.CopyParams(srcSet, true);

            if (this.FParent != null)
                this.Version = this.FParent.Version;
            else if (srcSet != null)
                this.Version = srcSet.Version;
        }

        /// <summary>
        /// This is over-ridden in descendant classes and then called within the         
        /// </summary>
        public virtual void DeriveParams()
        {
        }

        /// <summary>
        /// Copy the paramter set
        /// </summary>
        /// <param name="srcSet">The source parameter set</param>
        public void CopyAll(ParameterSet srcSet)
        {
            int idx;

            this.CopyParams(srcSet, true);

            while (this.ChildCount() > srcSet.ChildCount())
            {
                this.DeleteChild(this.ChildCount() - 1);
            }
            while (this.ChildCount() < srcSet.ChildCount())
            {
                this.AddChild();
            }
            for (idx = 0; idx <= this.ChildCount() - 1; idx++)
            {
                this.GetChild(idx).CopyAll(srcSet.GetChild(idx));
            }
        }

        /// <summary>
        /// Gets or sets the version string
        /// </summary>
        public string Version
        {
            get { return this.FVersion; }
            set { this.FVersion = value; }
        }

        /// <summary>
        /// Gets or sets the parameter set english name
        /// </summary>
        public string EnglishName
        {
            get { return this.FEnglishName; }
            set { this.FEnglishName = value; }
        }

        /// <summary>
        /// Get the count of translations
        /// </summary>
        /// <returns>The count</returns>
        public int TranslationCount()
        {
            if (this.FLocales != null)
                return this.FTranslations.Length;
            else
                return 0;
        }

        /// <summary>
        /// Add a new translation
        /// </summary>
        /// <param name="lang">Language name</param>
        /// <param name="text">Translation text</param>
        public void AddTranslation(string lang, string text)
        {
            bool found;
            int idx;

            found = false;
            for (idx = 0; idx <= this.FTranslations.Length - 1; idx++)
            {
                if (string.Compare(this.FTranslations[idx].Lang, lang, true) == 0)
                {
                    this.FTranslations[idx].Text = text;
                    found = true;
                }
            }
            if (!found)
            {
                idx = this.FTranslations.Length;
                Array.Resize(ref this.FTranslations, idx + 1);
                this.FTranslations[idx].Lang = lang;
                this.FTranslations[idx].Text = text;
            }
            if ((string.Compare("en", lang, true) == 0) || (string.Compare(this.Name, string.Empty) == 0))
                this.Name = text;
        }

        /// <summary>
        /// Get a translation at an index
        /// </summary>
        /// <param name="idx">Index value</param>
        /// <returns>The translation</returns>
        public Translation GetTranslation(int idx)
        {
            return this.FTranslations[idx];
        }

        /// <summary>
        /// Delete the translation at index
        /// </summary>
        /// <param name="idx">Index value</param>
        public void DeleteTranslation(int idx)
        {
            if ((idx >= 0) && (idx < this.FTranslations.Length))
            {
                for (int i = idx + 1; i <= this.FTranslations.Length - 1; i++)
                {
                    this.FTranslations[i - 1] = this.FTranslations[i];
                }
                Array.Resize(ref this.FTranslations, this.FTranslations.Length - 1);
            }
        }

        /// <summary>
        /// Count of locales
        /// </summary>
        /// <returns>The count of locales</returns>
        public int LocaleCount()
        {
            if (this.FLocales != null)
                return this.FLocales.Length;
            else
                return 0;
        }

        /// <summary>
        /// Get local at index
        /// </summary>
        /// <param name="idx">Index value</param>
        /// <returns>The locale string</returns>
        public string GetLocale(int idx)
        {
            return this.FLocales[idx];
        }

        /// <summary>
        /// Add a locale
        /// </summary>
        /// <param name="locale">Locale name</param>
        public void AddLocale(string locale)
        {
            int idx = this.FLocales.Length;
            Array.Resize(ref this.FLocales, idx + 1);
            this.FLocales[idx] = locale;
        }

        /// <summary>
        /// Get the text for a locale
        /// </summary>
        /// <returns>Locale text</returns>
        public string GetLocaleText()
        {
            string result = string.Empty;
            for (int idx = 0; idx <= this.LocaleCount() - 1; idx++)
            {
                if (idx == 0)
                {
                    result = result + this.GetLocale(idx);
                }
                else
                {
                    result = result + ";" + this.GetLocale(idx);
                }
            }
            return result;
        }

        /// <summary>
        /// TODO: Test this
        /// </summary>
        /// <param name="text">Locale text</param>
        public void SetLocaleText(string text)
        {
            int posIdx;

            Array.Resize(ref this.FLocales, 0);

            text = text.Trim();
            while (text != string.Empty)
            {
                posIdx = text.IndexOf(";");
                if (posIdx < 0)
                {
                    this.AddLocale(text);
                    text = string.Empty;
                }
                else
                {
                    this.AddLocale(text.Substring(0, posIdx).Trim());
                    text = text.Remove(0, posIdx + 1);
                }
            }
        }

        /// <summary>
        /// Check if this locale is in the list of locales
        /// </summary>
        /// <param name="locale">Locale name</param>
        /// <returns>True if found</returns>
        public bool InLocale(string locale)
        {
            int idx;

            bool result = false;

            if (locale == ALL_LOCALES)
                result = true;
            else
            {
                result = false;
                locale = locale.ToLower();
                idx = 0;
                while (!result && (idx < this.FLocales.Length))
                {
                    result = locale == this.FLocales[idx].ToLower();
                    idx++;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the current locale
        /// </summary>
        public string CurrLocale
        {
            get { return this.FCurrLocale; }
            set { this.SetCurrLocale(value); }
        }

        /// <summary>
        /// Gets the parent parameter set
        /// </summary>
        public ParameterSet ParentParameterSet
        {
            get { return this.FParent; }
        }

        /// <summary>
        /// Count of children
        /// </summary>
        /// <returns>The count of children</returns>
        public int ChildCount()
        {
            if (this.FChildren != null)
                return this.FChildren.Length;
            else
                return 0;
        }

        /// <summary>
        /// Get a child item
        /// </summary>
        /// <param name="idx">Child index</param>
        /// <returns>A child paramater set</returns>
        public ParameterSet GetChild(int idx)
        {
            return this.FChildren[idx];
        }

        /// <summary>
        /// Get child by name
        /// </summary>
        /// <param name="child">Child name</param>
        /// <returns>The child parameter set</returns>
        public ParameterSet GetChild(string child)
        {
            int idx;

            child = child.ToLower().Trim();
            idx = 0;
            while ((idx < this.ChildCount()) && (child != this.GetChild(idx).Name.ToLower()))
                idx++;

            if (idx < this.ChildCount())
                return this.GetChild(idx);
            else
                return null;
        }

        /// <summary>
        /// Add a new parameter set child and return it
        /// </summary>
        /// <returns>The new child</returns>
        public ParameterSet AddChild()
        {
            ParameterSet result = this.MakeChild();
            if (this.FChildren == null)
                this.FChildren = new ParameterSet[0];
            var children = FChildren;
            Array.Resize(ref children, this.FChildren.Length + 1);
            FChildren = children;
            this.FChildren[this.FChildren.Length - 1] = result;
            return this.FChildren[this.FChildren.Length - 1];
        }

        /// <summary>
        /// Delete child parameter set by index
        /// </summary>
        /// <param name="idx">Child index</param>
        public void DeleteChild(int idx)
        {
            int jdx;

            for (jdx = idx + 1; jdx <= this.ChildCount() - 1; jdx++)
            {
                this.FChildren[jdx - 1] = this.FChildren[jdx];
            }
            var children = FChildren;
            Array.Resize(ref children, this.FChildren.Length - 1);
            FChildren = children;
        }

        /// <summary>
        /// Returns TRUE i.f.f. this is the root (ultimate parent) node                  
        /// </summary>
        /// <returns>True if this is the root</returns>
        public bool NodeIsRoot()
        {
            return this.ParentParameterSet == null;
        }

        /// <summary>
        /// Returns TRUE i.f.f. this node has no child nodes                             
        /// </summary>
        /// <returns>True if no child nodes</returns>
        public bool NodeIsLeaf()
        {
            return (this.ParentParameterSet != null) && ((this.FChildren == null) || (this.FChildren.Length == 0));
        }

        /// <summary>
        /// Total number of nodes in the tree of parameter sets, including the current node
        /// </summary>
        /// <returns>Number of nodes</returns>
        public int NodeCount()
        {
            int idx;

            int result = 1;                                                                 // Include Self in the count of nodes    
            for (idx = 0; idx <= this.ChildCount() - 1; idx++)
                result = result + this.GetChild(idx).NodeCount();
            return result;
        }

        /// <summary>
        /// Locate a node in the tree by ordinal value. Node 0 is the current node;      
        /// the search then proceeds depth-first (i.e. the first child of node N is node N+1)                                                                         
        /// </summary>
        /// <param name="nodeIdx">Node index</param>
        /// <returns>The parameter set node</returns>
        public ParameterSet GetNode(int nodeIdx)
        {
            int offset, idx;
            ParameterSet result;

            if (nodeIdx == 0)
                result = this;
            else
            {
                offset = 1;
                result = null;
                idx = 0;
                while ((idx < this.ChildCount()) && (result == null))
                {
                    result = this.GetChild(idx).GetNode(nodeIdx - offset);
                    if (result == null)
                    {
                        offset = offset + this.GetChild(idx).NodeCount();
                        idx++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Get the parameter set by node name
        /// </summary>
        /// <param name="nodeName">Node name</param>
        /// <returns>The parameter set</returns>
        public ParameterSet GetNode(string nodeName)
        {
            int idx;
            ParameterSet result;

            nodeName = nodeName.ToLower().Trim();
            if (nodeName == this.Name.ToLower())
                result = this;
            else
            {
                result = this.GetChild(nodeName);
                idx = 0;
                while ((idx < this.ChildCount()) && (result == null))
                {
                    result = this.GetChild(idx).GetNode(nodeName);
                    idx++;
                }
            }
            return result;
        }

        /// <summary>
        /// Leaf count
        /// </summary>
        /// <param name="useLocale">Filter by current locale</param>
        /// <returns>The count</returns>
        public int LeafCount(bool useLocale = true)
        {
            int idx;

            int result = 0;
            if (!this.NodeIsLeaf())
                for (idx = 0; idx <= this.ChildCount() - 1; idx++)
                {
                    result = result + this.GetChild(idx).LeafCount(useLocale);
                }
            else if (!useLocale || this.InLocale(this.CurrLocale))
                result = 1;

            return result;
        }

        /// <summary>
        /// Locate leaf nodes that match a locale
        /// </summary>
        /// <param name="leafIdx">Index of the leaf</param>
        /// <param name="useLocale">Match to current locale</param>
        /// <returns>The parameter set node</returns>
        public ParameterSet GetLeaf(int leafIdx, bool useLocale = true)
        {
            ParameterSet node;
            int idx, jdx;
            ParameterSet result;

            result = null;
            int nodeCount = this.NodeCount();
            idx = 0;
            jdx = 0;
            while ((idx < nodeCount) && (result == null))
            {
                node = this.GetNode(idx);
                if (node.NodeIsLeaf() && (!useLocale || node.InLocale(this.CurrLocale)))
                {
                    if (jdx == leafIdx)
                        result = node;
                    jdx++;
                }
                idx++;
            }

            return result;
        }

        /// <summary>
        /// Count of definitions
        /// </summary>
        /// <returns>The count</returns>
        public int DefinitionCount()
        {
            if (this.FDefinitions != null)
                return this.FDefinitions.Length;
            else
                return 0;
        }

        /// <summary>
        /// Get a parameter definition
        /// </summary>
        /// <param name="idx">Index of definition</param>
        /// <returns>The definition</returns>
        public ParameterDefinition GetDefinition(int idx)
        {
            return this.FDefinitions[idx];
        }

        /// <summary>
        /// Get the parameter definition for sTag
        /// </summary>
        /// <param name="tag">Tag name</param>
        /// <returns>The definition</returns>
        public ParameterDefinition GetDefinition(string tag)
        {
            ParameterDefinition result = null;
            string[] tagList = new string[0];
            bool isPartName;

            if (tag == string.Empty)
                result = null;
            else
            {
                isPartName = (tag[tag.Length - 1] == '-');
                if (isPartName)
                    tag = tag.Remove(tag.Length - 1, 1);
                this.Tokenise(tag, ref tagList, "-");
                result = this.GetDefinition(tagList);
                if ((result != null) && (isPartName == result.IsScalar()))
                    result = null;
            }
            return result;
        }

        /// <summary>
        /// Get the parameter definition
        /// </summary>
        /// <param name="tags">Array of tags</param>
        /// <returns>The parameter definition</returns>
        public ParameterDefinition GetDefinition(string[] tags)
        {
            int idx;
            int defCount;

            ParameterDefinition result = null;
            idx = 0;
            defCount = this.DefinitionCount();
            while ((idx < defCount) && (result == null))
            {
                result = this.FDefinitions[idx].FindParam(tags);
                idx++;
            }

            return result;
        }

        /// <summary>
        /// Get the parameter count
        /// </summary>
        /// <returns>The count</returns>
        public int ParamCount()
        {
            int idx;
            int defCount;

            int result = 0;
            defCount = this.DefinitionCount();
            for (idx = 0; idx <= defCount - 1; idx++)
                result = result + this.GetDefinition(idx).ParamCount;

            return result;
        }

        /// <summary>
        /// Get parameter at index
        /// </summary>
        /// <param name="idx">The index</param>
        /// <returns>The parameter definition</returns>
        public ParameterDefinition GetParam(int idx)
        {
            int defnIdx = 0;
            int offset = 0;
            int defCount = this.DefinitionCount();
            while ((defnIdx < defCount) && (offset + this.GetDefinition(defnIdx).ParamCount <= idx))
            {
                offset = offset + this.GetDefinition(defnIdx).ParamCount;
                defnIdx++;
            }

            if (defnIdx < defCount)
                return this.GetDefinition(defnIdx).getParam(idx - offset);
            else
                return null;
        }

        /// <summary>
        /// Returns a floating-point parameter
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <returns>Returns the parameter value</returns>
        public double ParamReal(string tag)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;
            double result = 0;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else if (!definition.ValueIsDefined())
                throw new Exception("Parameter value undefined: " + tag);
            else
                result = this.GetRealParam(tagList);

            return result;
        }

        /// <summary>
        /// Returns an integer parameter.
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <returns>The integer value</returns>
        public int ParamInt(string tag)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;
            int result = 0;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else if (!definition.ValueIsDefined())
                throw new Exception("Parameter value undefined: " + tag);
            else
                result = this.GetIntParam(tagList);

            return result;
        }

        /// <summary>
        /// Returns a Boolean parameter.
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <returns>The boolean value</returns>
        public bool ParamBool(string tag)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;
            bool result = false;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else if (!definition.ValueIsDefined())
                throw new Exception("Parameter value undefined: " + tag);
            else
                result = this.GetBoolParam(tagList);

            return result;
        }

        /// <summary>
        /// Returns a string parameter.
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <returns>The parameter value</returns>
        public string ParamStr(string tag)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;
            string result = string.Empty;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else if (!definition.ValueIsDefined())
                throw new Exception("Parameter value undefined: " + tag);
            else
            {
                if (definition.ParamType == TYPETEXT)
                    result = this.GetTextParam(tagList);
                else if (definition.ParamType == TYPEREAL)
                    result = string.Format("{0:G}", this.GetRealParam(tagList));
                else if (definition.ParamType == TYPEINT)
                    result = string.Format("{0:D}", this.GetIntParam(tagList));
                else if (definition.ParamType == TYPEBOOL)
                {
                    if (this.GetBoolParam(tagList))
                        result = "true";
                    else
                        result = "false";
                }
                return result;
            }
        }

        /// <summary>
        /// Sets the value of a floating-point parameter, noting that its value is now defined
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <param name="newValue">The new value</param>
        public void SetParam(string tag, double newValue)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else
            {
                this.SetRealParam(tagList, newValue);
                definition.SetDefined(true);
            }
        }

        /// <summary>
        /// Sets the value of an integer parameter, noting that its value is now defined 
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <param name="newValue">New value</param>
        public void SetParam(string tag, int newValue)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else
            {
                this.SetIntParam(tagList, newValue);
                definition.SetDefined(true);
            }
        }

        /// <summary>
        /// Sets the value of a Boolean parameter, noting that its value is now defined
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="newValue"></param>
        public void SetParam(string tag, bool newValue)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else
            {
                this.SetBoolParam(tagList, newValue);
                definition.SetDefined(true);
            }
        }

        /// <summary>
        /// 1. Sets the value of a string parameter, noting that its value is now defined.                                                                  
        /// 2. Parses and sets the value of a floating-point, integer or Boolean parameter.                                                                
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="newValue">New value</param>
        public void SetParam(string tag, string newValue)
        {
            string[] tagList = new string[0];
            ParameterDefinition definition;
            double dblValue;
            int intValue;

            this.Tokenise(tag, ref tagList, "-");
            definition = this.GetDefinition(tagList);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else
            {
                if (definition.ParamType == TYPETEXT)
                    this.SetTextParam(tagList, newValue);
                else if (definition.ParamType == TYPEREAL)
                {
                    try
                    {
                        dblValue = System.Convert.ToDouble(newValue, System.Globalization.CultureInfo.InvariantCulture);
                        this.SetRealParam(tagList, dblValue);
                    }
                    catch
                    {
                        throw new Exception("Error parsing parameter " + tag + " = " + newValue);
                    }
                }
                else if (definition.ParamType == TYPEINT)
                {
                    try
                    {
                        intValue = System.Convert.ToInt32(newValue, CultureInfo.InvariantCulture);
                        this.SetIntParam(tagList, intValue);
                    }
                    catch
                    {
                        throw new Exception("Error parsing parameter " + tag + " = " + newValue);
                    }
                }
                else if (definition.ParamType == TYPEBOOL)
                {
                    if (newValue.ToLower() == "true")
                        this.SetBoolParam(tagList, true);
                    else if (newValue.ToLower() == "false")
                        this.SetBoolParam(tagList, false);
                    else
                        throw new Exception("Error parsing parameter " + tag + " = " + newValue);
                }
                definition.SetDefined(true);
            }
        }

        /// <summary>
        /// Un-defines a parameter value 
        /// </summary>
        /// <param name="tag">Tag name</param>
        public void SetUndefined(string tag)
        {
            ParameterDefinition definition;

            definition = this.GetDefinition(tag);
            if ((definition == null) || !definition.IsScalar())
                throw new Exception("Invalid parameter name " + tag);
            else
                definition.SetDefined(false);
        }

        /// <summary>
        /// Is the parameter defined
        /// </summary>
        /// <param name="tag">Tag name</param>
        /// <returns>Returns TRUE i.f.f. the nominated parameter has a defined value</returns>
        public bool IsDefined(string tag)
        {
            ParameterDefinition definition;

            definition = this.GetDefinition(tag);
            return (definition != null) && definition.ValueIsDefined();
        }

        /// <summary>
        /// Returns TRUE i.f.f. all parameters in the set have defined values
        /// </summary>
        /// <returns>True if all the parameters have values</returns>
        public bool IsComplete()
        {
            int idx;
            int defCount;

            bool result = true;
            defCount = this.DefinitionCount();
            for (idx = 0; idx <= defCount - 1; idx++)
            {
                result = result && this.GetDefinition(idx).ValueIsDefined();
            }

            return result;
        }

        /// <summary>
        /// Convert the name
        /// </summary>
        public void LocaliseNames()
        {
            ParameterSet ancestor, child;
            int idx, itrans;
            Translation translation;

            ancestor = this;
            if (ancestor != null)
            {
                while (ancestor.ParentParameterSet != null)
                {
                    ancestor = ancestor.ParentParameterSet;
                }

                for (idx = 0; idx <= ancestor.NodeCount() - 1; idx++)
                {
                    child = ancestor.GetNode(idx);
                    child.Name = child.EnglishName; // Use English name if target language not found
                    for (itrans = 0; itrans <= child.TranslationCount() - 1; itrans++)
                    {
                        translation = child.GetTranslation(itrans);
                        if (string.Compare(translation.Lang, this.GetUILang(), true) == 0)
                            child.Name = translation.Text;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the source file name
        /// </summary>
        public string FileSource
        {
            get { return this.FFileSource; }
            set { this.FFileSource = value; }
        }

        /// <summary>
        /// Returns the 2-letter ISO 639 language code, or 'en' if that fails            
        /// TODO: Test this
        /// </summary>
        /// <returns>The language code</returns>
        public string GetUILang()
        {
            string result;

            if (this.UILang != string.Empty)
                result = this.UILang;
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
        /// <param name="language">The language name</param>
        public void SetUILang(string language)
        {
            this.UILang = language;
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
        /// <returns>The current locale being used</returns>
        public static string DefaultLocale()
        {
            string result;
            string regString = string.Empty;
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
            if (regString == string.Empty)
            {
                //// If locale (=ISO 3166 country code) is not specified in the registry, then query the OS
                // if GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SISO3166CTRYNAME, CountryName, SizeOf(CountryName)) = 3 then
                //   Result := LowerCase(CountryName)
                // else
                result = "au"; // Australia is the default locale
            }
            else
                result = regString.ToLower();
            return result;
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