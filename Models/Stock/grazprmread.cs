using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using APSIM.Shared.Utilities;
using CMPServices;

//Classes for I/O of the generic parameter set class, ParameterSet.           

namespace Models.GrazPlan
{
    /// <summary>
    /// Class that wraps the XML param reader
    /// </summary>
    static public class GlobalParameterFactory
    {
        static private ParameterXMLFactory _GParamFactory = null;

        /// <summary>
        /// Returns a ptr to the _GParamFactory. ParamFactory is loaded on demand only.
        /// </summary>
        static public ParameterXMLFactory ParamXMLFactory()
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
        protected const TTypedValue.TBaseType ptyReal = TTypedValue.TBaseType.ITYPE_SINGLE;
        /// <summary>
        /// Type integer
        /// </summary>
        protected const TTypedValue.TBaseType ptyInt = TTypedValue.TBaseType.ITYPE_INT4;
        /// <summary>
        /// Type Boolean
        /// </summary>
        protected const TTypedValue.TBaseType ptyBool = TTypedValue.TBaseType.ITYPE_BOOL;
        /// <summary>
        /// Type string
        /// </summary>
        protected const TTypedValue.TBaseType ptyText = TTypedValue.TBaseType.ITYPE_STR;

        /// <summary>
        /// Parses a &lt;parameters&gt; or &lt;set&gt; element in an XML parameter document          
        /// </summary>
        /// <param name="Parser"></param>
        /// <param name="aNode"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        private void readParamNode(XMLParser Parser, XmlNode aNode, ref ParameterSet Params, bool bModify)
        {
            XmlNode childNode;
            ParameterSet newParams;
            string sTag,
            sValues,
            sChildName,
            sLang,
            sDummy;

            try
            {
                Params.Name = Parser.getAttrValue(aNode, "name");                        // Name and version information. The     
                Params.EnglishName = Params.Name;
                if (Params.NodeIsRoot())                                                     //   version is passed to child sets   
                    Params.Version = Parser.getAttrValue(aNode, "version");               //   during creation                   

                childNode = Parser.firstElementChild(aNode, "translate");                 // See if tbere's a translation of the name matching our current language setting 
                while (childNode != null)
                {
                    sLang = Parser.getAttrValue(childNode, "lang");
                    sDummy = Parser.getText(childNode);
                    Params.AddTranslation(sLang, sDummy);
                    childNode = Parser.nextElementSibling(childNode, "translate");
                }

                if (!bModify)                                                           // If we are not modifying an existing   
                    while (Params.ChildCount() > 0)                                           //   parameter set, then clear any old   
                        Params.DeleteChild(Params.ChildCount() - 1);                              //   child parameter sets               

                sValues = Parser.getAttrValue(aNode, "locales").Trim();                     // Populate the locale list              
                Params.SetLocaleText(sValues);

                childNode = Parser.firstElementChild(aNode, "par");                       // Parse the <par> elements              
                while (childNode != null)
                {
                    sTag = Parser.getAttrValue(childNode, "name");
                    sValues = Parser.getText(childNode);
                    readParamValues(ref Params, sTag, sValues, bModify);
                    childNode = Parser.nextElementSibling(childNode, "par");
                }
                Params.DeriveParams();

                childNode = Parser.firstElementChild(aNode, "set");                       // Create child parameter sets from the  
                while (childNode != null)                                                   //   <set> elements                     
                {
                    if (!bModify)
                        newParams = Params.AddChild();
                    else
                    {                                                                      // If we are modifying an existing      
                        sChildName = Parser.getAttrValue(childNode, "name");                  //  parameter set, then locate the child 
                        newParams = Params.GetChild(sChildName);                              //   set that we are about to parse      
                        if (newParams == null)
                            newParams = Params.AddChild();
                    }
                    readParamNode(Parser, childNode, ref newParams, bModify);
                    childNode = Parser.nextElementSibling(childNode, "set");
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
        /// <param name="Params"></param>
        /// <param name="sTag"></param>
        /// <param name="sValues"></param>
        /// <param name="bPropagate"></param>
        private void readParamValues(ref ParameterSet Params, string sTag, string sValues, bool bPropagate)
        {
            ParameterDefinition Definition;
            string sValue;
            int Idx;

            sValues = sValues.Trim();

            if ((sTag != "") && (sValues != ""))
            {
                Definition = Params.GetDefinition(sTag);
                if ((Definition == null) || (Definition.Dimension() > 1))
                    throw new Exception("Invalid tag when reading parameters: " + sTag);

                if (Definition.IsScalar())                                                // Reference to a single value           
                    assignParameter(ref Params, sTag, sValues, bPropagate);
                else
                {                                                                      // Reference to a list of values         
                    for (Idx = 0; Idx <= Definition.Count - 1; Idx++)
                    {
                        sValue = stripValue(ref sValues);
                        if (sValue != "")                                                  // Null string denotes "leave value at   
                            assignParameter(ref Params, sTag + Definition.Item(Idx).PartName,             //   default"                    
                                             sValue, bPropagate);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value of a parameter in a set, and optionally propagates the value  
        /// to descendant parameter sets                                                 
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="sTag"></param>
        /// <param name="sValue"></param>
        /// <param name="bPropagate"></param>
        private void assignParameter(ref ParameterSet Params, string sTag, string sValue, bool bPropagate)
        {
            int Idx;

            Params.SetParam(sTag, sValue);
            if (bPropagate)
            {
                for (Idx = 0; Idx <= Params.ChildCount() - 1; Idx++)
                {
                    ParameterSet child = Params.GetChild(Idx);
                    assignParameter(ref child, sTag, sValue, true);
                }
            }
        }

        /// <summary>
        /// Reads a string from a comma-separated list                                   
        /// </summary>
        /// <param name="sValues"></param>
        /// <returns></returns>
        private string stripValue(ref string sValues)
        {
            int iPosn;
            string result = "";

            iPosn = sValues.IndexOf(',');
            if (iPosn < 0)
            {
                result = sValues.Trim();
                sValues = "";
            }
            else
            {
                result = sValues.Substring(0, iPosn).Trim();
                sValues = sValues.Remove(0, iPosn + 1);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sText"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        public void readFromXML(string sText, ref ParameterSet Params, bool bModify)
        {
            XMLParser Parser;

            Parser = new XMLParser(sText);
            readParamNode(Parser, Parser.rootNode(), ref Params, bModify);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subSet"></param>
        /// <param name="Definition"></param>
        /// <returns></returns>
        private bool bDiffers(ParameterSet subSet, ParameterDefinition Definition)
        {
            bool result;

            if (!Definition.ValueIsDefined())
                result = false;
            else if ((subSet.Parent == null) || (!subSet.Parent.IsDefined(Definition.FullName)))
                result = true;
            else
            {
                switch (Definition.ParamType)
                {
                    case ptyReal: result = (subSet.dParam(Definition.FullName) != subSet.Parent.dParam(Definition.FullName));
                        break;
                    case ptyInt: result = (subSet.iParam(Definition.FullName) != subSet.Parent.iParam(Definition.FullName));
                        break;
                    case ptyBool: result = (subSet.bParam(Definition.FullName) != subSet.Parent.bParam(Definition.FullName));
                        break;
                    case ptyText: result = (subSet.sParam(Definition.FullName) != subSet.Parent.sParam(Definition.FullName));
                        break;
                    default: result = false;
                        break;
                }
            }
            return result;
        }

        private void writeParameters(ParameterSet subSet, ParameterDefinition Definition, List<string> Strings, int iIndent)
        {
            int iDiffCount;
            string sLine;
            int Idx;

            if (Definition.Dimension() > 1)                                         // Multi-dimensional array of            
                for (Idx = 0; Idx <= Definition.Count - 1; Idx++)                                  //   parameters - recurse                
                    writeParameters(subSet, Definition.Item(Idx), Strings, iIndent);

            else if (Definition.IsScalar() && bDiffers(subSet, Definition))      // Single parameter value                
            {
                sLine = new string(' ', iIndent)
                       + "<par name=\"" + Definition.FullName + "\">"
                       + subSet.sParam(Definition.FullName)
                       + "</par>";
                Strings.Add(sLine);
            }
            else                                                                     // List of parameter values (one-        
            {                                                                    //   dimensional)                        
                iDiffCount = 0;
                for (Idx = 0; Idx <= Definition.Count - 1; Idx++)
                    if (bDiffers(subSet, Definition.Item(Idx)))
                        iDiffCount++;

                if (iDiffCount > 1)                                                // More than one difference - write      
                {                                                                  //   the differing values in a list      
                    sLine = new string(' ', iIndent)
                             + "<par name=\"" + Definition.FullName + "\">";
                    for (Idx = 0; Idx <= Definition.Count - 1; Idx++)
                    {
                        if (Idx > 0)
                            sLine += ',';
                        if (bDiffers(subSet, Definition.Item(Idx)))
                            sLine += subSet.sParam(Definition.Item(Idx).FullName);
                    }
                    sLine += "</par>";
                    Strings.Add(sLine);
                }
                else if (iDiffCount == 1)                                           // Only one parameter is different -     
                    for (Idx = 0; Idx <= Definition.Count - 1; Idx++)                               //  write it as a scalar                
                        if (bDiffers(subSet, Definition.Item(Idx)))
                            writeParameters(subSet, Definition.Item(Idx), Strings, iIndent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subSet"></param>
        /// <param name="Strings"></param>
        /// <param name="sElem"></param>
        /// <param name="iIndent"></param>
        private void writeParamSet(ParameterSet subSet,
                                   List<string> Strings,
                                   string sElem,
                                   int iIndent)
        {
            string sLine;
            int Idx;

            if (!subSet.NodeIsRoot())
                Strings.Add("");

            sLine = new string(' ', iIndent) + "<" + sElem + " name=\"" + subSet.EnglishName + "\"";
            if (subSet.NodeIsRoot())
                sLine += " version=\"" + subSet.Version + "\">";
            else
            {
                if (subSet.LocaleCount() > 0)
                {
                    sLine += " locales=\"" + subSet.GetLocale(0);
                    for (Idx = 1; Idx <= subSet.LocaleCount() - 1; Idx++)
                        sLine += ";" + subSet.GetLocale(Idx);
                    sLine += "\"";
                }
                sLine += ">";
            }
            Strings.Add(sLine);

            if (subSet.TranslationCount() > 0)
                for (Idx = 0; Idx <= subSet.TranslationCount() - 1; Idx++)
                {
                    sLine = new string(' ', iIndent + 2) + "<translate lang=\"" +
                             subSet.GetTranslation(Idx).sLang + "\">" +
                             TTypedValue.escapeText(subSet.GetTranslation(Idx).sText) + "</translate>";
                    Strings.Add(sLine);
                }

            for (Idx = 0; Idx <= subSet.DefinitionCount() - 1; Idx++)
                writeParameters(subSet, subSet.GetDefinition(Idx), Strings, iIndent + 2);
            for (Idx = 0; Idx <= subSet.ChildCount() - 1; Idx++)
                writeParamSet(subSet.GetChild(Idx), Strings, "set", iIndent + 2);

            sLine = new string(' ', iIndent) + "</" + sElem + ">";
            if (!subSet.NodeIsRoot() && (subSet.ChildCount() > 0))
                sLine += "<!-- " + subSet.EnglishName + " -->";

            Strings.Add(sLine);
        }

        /// <summary>
        /// The strategy for obtaining default parameters is:                            
        /// 1. Attempt to read a base parameter set from a resource called sPrmID in the 
        ///    current module.                                                           
        /// </summary>
        /// <param name="sPrmID"></param>
        /// <param name="Params"></param>
        public void readDefaults(string sPrmID, ref ParameterSet Params)
        {
            readFromResource(sPrmID, ref Params, false);
            Params.CurrLocale = GrazLocale.DefaultLocale();
        }

        /// <summary>
        /// Read from internal resource
        /// </summary>
        /// <param name="sResID"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        public void readFromResource(string sResID, ref ParameterSet Params, bool bModify)
        {
            string paramStr = ReflectionUtilities.GetResourceAsString(sResID);
            readFromXML(paramStr, ref Params, bModify);
        }

        private void readFromStream(StreamReader Stream, ParameterSet Params, bool bModify)
        {
            string sParamStr;

            sParamStr = Stream.ReadToEnd();
            readFromXML(sParamStr, ref Params, bModify);
        }

        /// <summary>
        /// Read the parameters from a file
        /// </summary>
        /// <param name="sFileName"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        public void readFromFile(string sFileName, ParameterSet Params, bool bModify)
        {
            StreamReader fStream = null;
            try
            {
                fStream = new StreamReader(sFileName);
                readFromStream(fStream, Params, bModify);
            }
            catch (Exception e)
            {
                if (fStream != null)
                    fStream = null;
                throw new Exception("Cannot load parameter data from \"" + sFileName + "\" \n\n" + e.Message);
            }
        }
        /// <summary>
        /// Parameter set as XML
        /// </summary>
        /// <param name="Params"></param>
        /// <returns></returns>
        public string sParamXML(ParameterSet Params)
        {
            List<string> Strings = new List<string>();

            writeParamSet(Params, Strings, "parameters", 0);
            return string.Join("\n", Strings.ToArray());
        }
    }
}