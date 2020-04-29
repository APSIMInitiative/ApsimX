// Classes for I/O of the generic parameter set class, ParameterSet.           

namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using CMPServices;

    /// <summary>
    /// Class that wraps the XML param reader
    /// </summary>
    public static class GlobalParameterFactory
    {
        private static ParameterXMLFactory _GParamFactory = null;

        /// <summary>
        /// Returns a ptr to the _GParamFactory. ParamFactory is loaded on demand only.
        /// </summary>
        /// <returns>The parameter XML factory object</returns>
        public static ParameterXMLFactory ParamXMLFactory()
        {
            if (_GParamFactory == null)
                _GParamFactory = new ParameterXMLFactory();
            return _GParamFactory;
        }
    }

    /// <summary>
    /// The DTD for the XML parameter format is:                                     
    /// <![CDATA[
    /// <!ELEMENT parameters (par? set?) >                                           
    /// <!ATTLIST parameters name    CDATA "" >                                    
    /// <!ATTLIST parameters version CDATA "" >                                    
    /// <!ELEMENT set (par? set? translate?) >                                       
    /// <!ATTLIST set name CDATA #REQUIRED >                                       
    /// <!ATTLIST set locales CDATA "" >                                           
    /// <!ELEMENT translate #PCDATA >                                                
    /// <!ATTLIST translate lang CDATA #REQUIRED >                                 
    /// <!ELEMENT par #PCDATA >                                                      
    /// <!ATTLIST par name CDATA #REQUIRED >                                       
    /// ]]>                                                                           
    /// * Parameter value data are contained within the &lt;par&gt; elements.              
    /// * The name attribute of a &lt;par&gt; element is parsed to determine which         
    /// parameter value(s) it holds.                                               
    /// * Lists of values within an element are separated by commas (this means that 
    /// the comma is not permitted in text parameter values).                      
    /// * A blank parameter value (including in a list) denotes "leave at the value  
    /// in the parent parameter set", which may be undefined.                      
    /// </summary>
    public class ParameterXMLFactory
    {
        /// <summary>
        /// Type real-single
        /// </summary>
        protected static readonly Type TYPEREAL = typeof(float);

        /// <summary>
        /// Type integer
        /// </summary>
        protected static readonly Type TYPEINT = typeof(int);

        /// <summary>
        /// Type Boolean
        /// </summary>
        protected static readonly Type TYPEBOOL = typeof(bool);

        /// <summary>
        /// Type string
        /// </summary>
        protected static readonly Type TYPETEXT = typeof(string);

        /// <summary>
        /// Parses a &lt;parameters&gt; or &lt;set&gt; element in an XML parameter document          
        /// </summary>
        /// <param name="parser">The XML parser</param>
        /// <param name="xmlNode">The XML node</param>
        /// <param name="parameters">The parameter set</param>
        /// <param name="modify">Do modify</param>
        private void ReadParamNode(XMLParser parser, XmlNode xmlNode, ref ParameterSet parameters, bool modify)
        {
            XmlNode childNode;
            ParameterSet newParams;
            string tag,
            values,
            childName,
            lang,
            dummy;

            try
            {
                parameters.Name = parser.GetAttrValue(xmlNode, "name");                     // Name and version information. The version is passed to child sets during creation
                parameters.EnglishName = parameters.Name;
                if (parameters.NodeIsRoot())
                    parameters.Version = parser.GetAttrValue(xmlNode, "version");                                  

                childNode = parser.FirstElementChild(xmlNode, "translate");                 // See if tbere's a translation of the name matching our current language setting 
                while (childNode != null)
                {
                    lang = parser.GetAttrValue(childNode, "lang");
                    dummy = parser.GetText(childNode);
                    parameters.AddTranslation(lang, dummy);
                    childNode = parser.NextElementSibling(childNode, "translate");
                }

                if (!modify)
                {
                    // If we are not modifying an existing parameter set, then clear any old child parameter sets    
                    while (parameters.ChildCount() > 0)                                           
                        parameters.DeleteChild(parameters.ChildCount() - 1);                                             
                }

                values = parser.GetAttrValue(xmlNode, "locales").Trim();                   // Populate the locale list              
                parameters.SetLocaleText(values);

                childNode = parser.FirstElementChild(xmlNode, "par");                       // Parse the <par> elements              
                while (childNode != null)
                {
                    tag = parser.GetAttrValue(childNode, "name");
                    values = parser.GetText(childNode);
                    this.ReadParamValues(ref parameters, tag, values, modify);
                    childNode = parser.NextElementSibling(childNode, "par");
                }
                parameters.DeriveParams();

                childNode = parser.FirstElementChild(xmlNode, "set");                       // Create child parameter sets from the <set> elements  
                while (childNode != null)                                                                        
                {
                    if (!modify)
                        newParams = parameters.AddChild();
                    else
                    {
                        // If we are modifying an existing parameter set, then locate the child set that we are about to parse    
                        childName = parser.GetAttrValue(childNode, "name");                   
                        newParams = parameters.GetChild(childName);                                    
                        if (newParams == null)
                            newParams = parameters.AddChild();
                    }
                    this.ReadParamNode(parser, childNode, ref newParams, modify);
                    childNode = parser.NextElementSibling(childNode, "set");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Parses the contents of a &lt;par&gt; element in an XML parameter document.         
        /// </summary>
        /// <param name="parameters">The parameter set</param>
        /// <param name="tagName">The tag name</param>
        /// <param name="values">The value string</param>
        /// <param name="propagate">Do propagate</param>
        private void ReadParamValues(ref ParameterSet parameters, string tagName, string values, bool propagate)
        {
            ParameterDefinition paramDefinition;
            string value;
            
            values = values.Trim();

            if ((tagName != string.Empty) && (values != string.Empty))
            {
                paramDefinition = parameters.GetDefinition(tagName);
                if ((paramDefinition == null) || (paramDefinition.Dimension() > 1))
                    throw new Exception("Invalid tag when reading parameters: " + tagName);

                if (paramDefinition.IsScalar())
                {
                    // Reference to a single value           
                    this.AssignParameter(ref parameters, tagName, values, propagate);
                }
                else
                {
                    // Reference to a list of values         
                    for (int idx = 0; idx <= paramDefinition.Count - 1; idx++)
                    {
                        value = this.StripValue(ref values);
                        if (value != string.Empty)
                        {
                            // Null string denotes "leave value at default"    
                            this.AssignParameter(ref parameters, tagName + paramDefinition.Item(idx).PartName, value, propagate);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value of a parameter in a set, and optionally propagates the value  
        /// to descendant parameter sets                                                 
        /// </summary>
        /// <param name="parameters">The parameter set</param>
        /// <param name="tagName">The tag name</param>
        /// <param name="value">The string value</param>
        /// <param name="propagate">Do propagate</param>
        private void AssignParameter(ref ParameterSet parameters, string tagName, string value, bool propagate)
        {
            parameters.SetParam(tagName, value);
            if (propagate)
            {
                for (int idx = 0; idx <= parameters.ChildCount() - 1; idx++)
                {
                    ParameterSet child = parameters.GetChild(idx);
                    this.AssignParameter(ref child, tagName, value, true);
                }
            }
        }

        /// <summary>
        /// Reads a string from a comma-separated list                                   
        /// </summary>
        /// <param name="valueStrs">String of values</param>
        /// <returns>The first item from the list</returns>
        private string StripValue(ref string valueStrs)
        {
            int posIdx;
            string result = string.Empty;

            posIdx = valueStrs.IndexOf(',');
            if (posIdx < 0)
            {
                result = valueStrs.Trim();
                valueStrs = string.Empty;
            }
            else
            {
                result = valueStrs.Substring(0, posIdx).Trim();
                valueStrs = valueStrs.Remove(0, posIdx + 1);
            }
            return result;
        }

        /// <summary>
        /// Read the parameter set from an XML string
        /// </summary>
        /// <param name="text">The XML text containing the parameters</param>
        /// <param name="parameters">The parameter set</param>
        /// <param name="modify">Do modify</param>
        public void ReadFromXML(string text, ref ParameterSet parameters, bool modify)
        {
            XMLParser parser;

            parser = new XMLParser(text);
            this.ReadParamNode(parser, parser.RootNode(), ref parameters, modify);
        }

        /// <summary>
        /// Check for a difference
        /// </summary>
        /// <param name="subSet">The parameter subset</param>
        /// <param name="paramDefinition">The parameter definition</param>
        /// <returns>True if there is a difference</returns>
        private bool Differs(ParameterSet subSet, ParameterDefinition paramDefinition)
        {
            bool result;

            if (!paramDefinition.ValueIsDefined())
                result = false;
            else if ((subSet.ParentParameterSet == null) || (!subSet.ParentParameterSet.IsDefined(paramDefinition.FullName)))
                result = true;
            else
            {
                if (paramDefinition.ParamType == TYPEREAL)
                    result = subSet.ParamReal(paramDefinition.FullName) != subSet.ParentParameterSet.ParamReal(paramDefinition.FullName);
                else if (paramDefinition.ParamType == TYPEINT)
                    result = subSet.ParamInt(paramDefinition.FullName) != subSet.ParentParameterSet.ParamInt(paramDefinition.FullName);
                else if (paramDefinition.ParamType == TYPEBOOL)
                    result = subSet.ParamBool(paramDefinition.FullName) != subSet.ParentParameterSet.ParamBool(paramDefinition.FullName);
                else if (paramDefinition.ParamType == TYPETEXT)
                    result = subSet.ParamStr(paramDefinition.FullName) != subSet.ParentParameterSet.ParamStr(paramDefinition.FullName);
                else
                    result = false;
            }
            return result;
        }

        /// <summary>
        /// Write parameters out
        /// </summary>
        /// <param name="subSet">Parameter subset</param>
        /// <param name="paramDefinition">The parameter definition</param>
        /// <param name="strings">List of string values</param>
        /// <param name="indent">The indent to use</param>
        private void WriteParameters(ParameterSet subSet, ParameterDefinition paramDefinition, List<string> strings, int indent)
        {
            int diffCount;
            string lineStr;
            int idx;

            if (paramDefinition.Dimension() > 1)
            {
                // Multi-dimensional array of parameters - recurse           
                for (idx = 0; idx <= paramDefinition.Count - 1; idx++)                                                  
                    this.WriteParameters(subSet, paramDefinition.Item(idx), strings, indent);
            }
            else if (paramDefinition.IsScalar() && this.Differs(subSet, paramDefinition))
            {
                // Single parameter value
                lineStr = new string(' ', indent)
                       + "<par name=\"" + paramDefinition.FullName + "\">"
                       + subSet.ParamStr(paramDefinition.FullName)
                       + "</par>";
                strings.Add(lineStr);
            }
            else
            {
                // List of parameter values (one-dimensional)                        
                diffCount = 0;
                for (idx = 0; idx <= paramDefinition.Count - 1; idx++)
                    if (this.Differs(subSet, paramDefinition.Item(idx)))
                        diffCount++;

                if (diffCount > 1)
                {
                    // More than one difference - write the differing values in a list      
                    lineStr = new string(' ', indent)
                             + "<par name=\"" + paramDefinition.FullName + "\">";
                    for (idx = 0; idx <= paramDefinition.Count - 1; idx++)
                    {
                        if (idx > 0)
                            lineStr += ',';
                        if (this.Differs(subSet, paramDefinition.Item(idx)))
                            lineStr += subSet.ParamStr(paramDefinition.Item(idx).FullName);
                    }
                    lineStr += "</par>";
                    strings.Add(lineStr);
                }
                else if (diffCount == 1)
                {
                    // Only one parameter is different - write it as a scalar    
                    for (idx = 0; idx <= paramDefinition.Count - 1; idx++)
                        if (this.Differs(subSet, paramDefinition.Item(idx)))
                            this.WriteParameters(subSet, paramDefinition.Item(idx), strings, indent);
                }
            }
        }

        /// <summary>
        /// Write the parameter set
        /// </summary>
        /// <param name="subSet">The parameter subset</param>
        /// <param name="strings">List of strings</param>
        /// <param name="elem">XML element name</param>
        /// <param name="indent">The XML indentation</param>
        private void WriteParamSet(ParameterSet subSet, List<string> strings, string elem, int indent)
        {
            string lineStr;
            int idx;

            if (!subSet.NodeIsRoot())
                strings.Add(string.Empty);

            lineStr = new string(' ', indent) + "<" + elem + " name=\"" + subSet.EnglishName + "\"";
            if (subSet.NodeIsRoot())
                lineStr += " version=\"" + subSet.Version + "\">";
            else
            {
                if (subSet.LocaleCount() > 0)
                {
                    lineStr += " locales=\"" + subSet.GetLocale(0);
                    for (idx = 1; idx <= subSet.LocaleCount() - 1; idx++)
                        lineStr += ";" + subSet.GetLocale(idx);
                    lineStr += "\"";
                }
                lineStr += ">";
            }
            strings.Add(lineStr);

            if (subSet.TranslationCount() > 0)
                for (idx = 0; idx <= subSet.TranslationCount() - 1; idx++)
                {
                    lineStr = new string(' ', indent + 2) + "<translate lang=\"" +
                             subSet.GetTranslation(idx).Lang + "\">" +
                             EscapeText(subSet.GetTranslation(idx).Text) + "</translate>";
                    strings.Add(lineStr);
                }

            for (idx = 0; idx <= subSet.DefinitionCount() - 1; idx++)
                this.WriteParameters(subSet, subSet.GetDefinition(idx), strings, indent + 2);
            for (idx = 0; idx <= subSet.ChildCount() - 1; idx++)
                this.WriteParamSet(subSet.GetChild(idx), strings, "set", indent + 2);

            lineStr = new string(' ', indent) + "</" + elem + ">";
            if (!subSet.NodeIsRoot() && (subSet.ChildCount() > 0))
                lineStr += "<!-- " + subSet.EnglishName + " -->";

            strings.Add(lineStr);
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

            System.Text.StringBuilder sbuf = new System.Text.StringBuilder(string.Empty);
            for (index = 0; index < text.Length; index++)
            {
                switch (text[index])
                {
                    case '&':
                        sbuf.Append("&#38;");
                        break;
                    case '<':
                        sbuf.Append("&#60;");
                        break;
                    case '>':
                        sbuf.Append("&#62;");
                        break;
                    case '"':
                        sbuf.Append("&#34;");
                        break;
                    case '\'':
                        sbuf.Append("&#39;");
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
        /// The strategy for obtaining default parameters is:                            
        /// 1. Attempt to read a base parameter set from a resource called sPrmID in the 
        ///    current module.                                                           
        /// </summary>
        /// <param name="prmID">The parameter ID string</param>
        /// <param name="parameters">The parameter set</param>
        public void ReadDefaults(string prmID, ref ParameterSet parameters)
        {
            this.ReadFromResource(prmID, ref parameters, false);
            parameters.CurrLocale = GrazLocale.DefaultLocale();
        }

        /// <summary>
        /// Read from internal resource
        /// </summary>
        /// <param name="resID">The resource ID string</param>
        /// <param name="parameters">The parameter set</param>
        /// <param name="modify">Do modify</param>
        public void ReadFromResource(string resID, ref ParameterSet parameters, bool modify)
        {
            string paramStr = ReflectionUtilities.GetResourceAsString(resID);
            this.ReadFromXML(paramStr, ref parameters, modify);
        }

        /// <summary>
        /// The parameters from a stream
        /// </summary>
        /// <param name="readerStream">The input stream</param>
        /// <param name="parameters">The parameter set</param>
        /// <param name="modify">Do modify</param>
        private void ReadFromStream(StreamReader readerStream, ParameterSet parameters, bool modify)
        {
            string paramStr;

            paramStr = readerStream.ReadToEnd();
            this.ReadFromXML(paramStr, ref parameters, modify);
        }

        /// <summary>
        /// Read the parameters from a file
        /// </summary>
        /// <param name="fileName">The parameter file</param>
        /// <param name="parameters">The parameter set</param>
        /// <param name="modify">Do modify</param>
        public void ReadFromFile(string fileName, ParameterSet parameters, bool modify)
        {
            StreamReader readerStream = null;
            try
            {
                readerStream = new StreamReader(fileName);
                this.ReadFromStream(readerStream, parameters, modify);
            }
            catch (Exception e)
            {
                if (readerStream != null)
                    readerStream = null;
                throw new Exception("Cannot load parameter data from \"" + fileName + "\" \n\n" + e.Message);
            }
        }

        /// <summary>
        /// Parameter set as XML
        /// </summary>
        /// <param name="parameters">The parameter set</param>
        /// <returns>The values separated by CR</returns>
        public string ParamXML(ParameterSet parameters)
        {
            List<string> strings = new List<string>();

            this.WriteParamSet(parameters, strings, "parameters", 0);
            return string.Join("\n", strings.ToArray());
        }
    }
}