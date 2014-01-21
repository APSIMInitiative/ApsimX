
namespace Importer
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Xml;
    using System.Text.RegularExpressions;
    using Models.Core;


    /// <summary>
    /// This is a worker class for the import process that converts
    /// an old APSIM 7.5 simulation file into an APSIM(X) file or Simulations object.
    /// </summary>
    public class APSIMImporter
    {
        /// <summary>
        /// Original path that is substituted for %apsim%
        /// </summary>
        public string ApsimPath;

        public APSIMImporter()
        {

        }

        /// <summary>
        /// Processes a file and writes the Simulation(s) to the .apsimx file
        /// </summary>
        /// <param name="filename"></param>
        public void ProcessFile(String filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    Simulations sims = CreateSimulations(filename);
                    sims.Write(Path.ChangeExtension(filename, ".apsimx"));
                    Console.WriteLine(filename + " --> " + Path.ChangeExtension(filename, ".apsimx"));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("Input file: " + filename + " cannot be found!");
            }
        }

        /// <summary>
        /// Iterate through the directory and attempt to convert any .apsim files.
        /// </summary>
        /// <param name="dir"></param>
        public void ProcessDir(String dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);

            foreach (FileInfo file in dirInfo.GetFiles("*.apsim"))
            {
                ProcessFile(file.FullName);
            }
        }

        /// <summary>
        /// Interogate the .apsim file XML and attempt to construct a 
        /// useful APSIMX Simulation object(s). Uses a temporary file
        /// location.
        /// </summary>
        /// <param name="filename">Source file (.apsim)</param>
        /// <returns>An APSIMX Simulations object</returns>
        public Simulations CreateSimulations(String filename)
        {
            String xfile = Path.GetTempFileName(); 
            Simulations newSimulations = null;

            try
            {
                //open the .apsim file
                StreamReader xmlReader = new StreamReader(filename);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlReader.ReadToEnd());
                xmlReader.Close();

                //create new apsimx document
                XmlNode newNode;
                XmlDocument xdoc = new XmlDocument();
                XmlNode xdocNode = xdoc.CreateElement("Simulations");
                xdoc.AppendChild(xdocNode);
                newNode = xdocNode.AppendChild(xdoc.CreateElement("Name"));
                newNode.InnerText = "Simulations";

                XmlNode rootNode = doc.DocumentElement;     // get first folder
                AddChildComponents(rootNode, xdocNode);
                AddDataStore(xdocNode);                     // each file must contain a DataStore

                //write to temporary xfile
                StreamWriter xmlWriter = new StreamWriter(xfile);
                xmlWriter.Write(Utility.Xml.FormattedXML(xdoc.OuterXml));
                xmlWriter.Close();

                try
                {
                    newSimulations = Simulations.Read(xfile);   // construct a Simulations object
                }
                catch (Exception exp)
                {
                    throw new Exception("Cannot create the Simulations object from the input : Error - " + exp.Message);
                }
                File.Delete(xfile);
            }
            catch (Exception exp)
            {
                throw new Exception("Cannot create a simulation from " + filename + " : Error - " + exp.Message);
            }
            return newSimulations;
        }

        /// <summary>
        /// Iterate through the child nodes
        /// </summary>
        /// <param name="systemNode"></param>
        /// <param name="destParent"></param>
        private void AddChildComponents(XmlNode systemNode, XmlNode destParent)
        {
            XmlNode child = systemNode.FirstChild;
            while (child != null)
            {
                AddComponent(child, ref destParent);
                child = child.NextSibling;
            }
        }

        // *********** shortcut components need to follow paths to their owners to get the correct information *************
        /// <summary>
        /// Add a component to the xml tree. Customised for each component type
        /// found in the source.
        /// </summary>
        /// <param name="compNode">The source component node in the .apsim file</param>
        /// <param name="destParent">The parent of the new component in the .apsimx file</param>
        /// <returns>The new component xml node</returns>
        private XmlNode AddComponent(XmlNode compNode, ref XmlNode destParent)
        {
            XmlNode newNode = null;
            try
            {
                if (compNode.Name == "simulation")
                {
                    XmlNode newSim = AddCompNode(destParent, "Simulation", Utility.Xml.Name(compNode));
                    AddChildComponents(compNode, newSim);
                }
                if (compNode.Name == "folder")
                {
                    XmlNode newFolder = AddCompNode(destParent, "Folder", Utility.Xml.Name(compNode));
                    AddChildComponents(compNode, newFolder);
                }
                if (compNode.Name == "clock")
                {
                    newNode = ImportClock(compNode, destParent, newNode);
                }
                else if (compNode.Name == "metfile")
                {
                    newNode = ImportMetFile(compNode, destParent, newNode);
                }
                else if (compNode.Name == "manager")
                {
                    newNode = ImportManager(compNode, destParent, newNode);
                }
                else if (compNode.Name == "outputfile")
                {
                    newNode = ImportOutputFile(compNode, destParent, newNode);
                }
                else if (compNode.Name == "operations")
                {
                    newNode = ImportOperations(compNode, destParent, newNode);
                }
                else if (compNode.Name == "summaryfile")
                {
                    newNode = AddCompNode(destParent, "Summary", Utility.Xml.Name(compNode));
                    XmlNode childNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("html"));
                    childNode.InnerText = "true";
                }
                else if (compNode.Name.ToLower() == "soil")
                {
                    newNode = ImportSoil(compNode, destParent, newNode);
                }
                else if (compNode.Name.ToLower() == "sample")
                {
                    newNode = ImportSample(compNode, destParent, newNode);
                }
                else if (compNode.Name.ToLower() == "water")
                {
                    newNode = ImportWater(compNode, destParent, newNode);
                }
                else if (compNode.Name.ToLower() == "soilwater")
                {
                    newNode = ImportSoilWater(compNode, destParent, newNode);
                }
                else if (compNode.Name == "InitialWater")
                {
                    newNode = ImportInitialWater(compNode, destParent, newNode);
                }
                else if (compNode.Name == "SoilOrganicMatter")
                {
                    newNode = ImportSOM(compNode, destParent, newNode);
                }
                else if (compNode.Name == "Analysis")
                {
                    newNode = ImportAnalysis(compNode, destParent, newNode);
                }
                else if (compNode.Name == "area")
                {
                    newNode = AddCompNode(destParent, "Zone", Utility.Xml.Name(compNode));
                    AddChildComponents(compNode, newNode);
                }
                else if (compNode.Name == "surfaceom")
                {
                    newNode = AddCompNode(destParent, "SurfaceOrganicMatter", Utility.Xml.Name(compNode));
                }
                else if (compNode.Name == "memo")
                {
                    newNode = AddCompNode(destParent, "Memo", Utility.Xml.Name(compNode));
                    XmlNode memoTextNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("MemoText"));
                    memoTextNode.InnerText = compNode.InnerText;
                }
                else if (compNode.Name == "Graph")
                {
                    newNode = ImportGraph(compNode, destParent, newNode);
                }
                else
                {
                    //String newElement = compNode.Name[0].ToString().ToUpper() + compNode.Name.Substring(1, compNode.Name.Length - 1); // first char to uppercase
                    //newNode = AddCompNode(destParent, newElement, Utility.Xml.Name(compNode));
                }
            }
            catch (Exception exp)
            {
                throw new Exception("Cannot import " + compNode.Name + " :Error - " + exp.Message);
            }
            return newNode; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportGraph(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Graph", Utility.Xml.Name(compNode));

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportInitialWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "InitialWater", Utility.Xml.Name(compNode));

            CopyNodeAndValue(compNode, newNode, "FractionFull", "FractionFull");
            CopyNodeAndValue(compNode, newNode, "PercentMethod", "PercentMethod");

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportAnalysis(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Analysis", Utility.Xml.Name(compNode));

            XmlNode childNode;
            // thickness array
            childNode = Utility.Xml.Find(compNode, "Thickness");
            CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            childNode = Utility.Xml.Find(compNode, "PH");
            CopyNodeAndValueArray(childNode, newNode, "PH", "PH");

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSOM(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "SoilOrganicMatter", Utility.Xml.Name(compNode));

            XmlNode childNode;
            // thickness array
            childNode = Utility.Xml.Find(compNode, "Thickness");
            CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            childNode = Utility.Xml.Find(compNode, "OC");
            CopyNodeAndValueArray(childNode, newNode, "OC", "OC");
            childNode = Utility.Xml.Find(compNode, "FBiom");
            CopyNodeAndValueArray(childNode, newNode, "FBiom", "FBiom");
            childNode = Utility.Xml.Find(compNode, "FInert");
            CopyNodeAndValueArray(childNode, newNode, "FInert", "FInert");
            
            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSoilWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "SoilWater", Utility.Xml.Name(compNode));

            XmlNode childNode;
            // thickness array
            childNode = Utility.Xml.Find(compNode, "Thickness");
            CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            childNode = Utility.Xml.Find(compNode, "SWCON");
            CopyNodeAndValueArray(childNode, newNode, "SWCON", "SWCON");

            /*CopyNodeAndValue(compNode, newNode, "SummerCona", "SummerCona");
            CopyNodeAndValue(compNode, newNode, "SummerU", "SummerU");
            CopyNodeAndValue(compNode, newNode, "SummerDate", "SummerDate");
            CopyNodeAndValue(compNode, newNode, "WinterCona", "WinterCona");
            CopyNodeAndValue(compNode, newNode, "WinterU", "WinterU");
            CopyNodeAndValue(compNode, newNode, "WinterDate", "WinterDate");
            CopyNodeAndValue(compNode, newNode, "DiffusConst", "DiffusConst");
            CopyNodeAndValue(compNode, newNode, "DiffusSlope", "DiffusSlope");
            CopyNodeAndValue(compNode, newNode, "Salb", "Salb");
            CopyNodeAndValue(compNode, newNode, "CN2Bare", "CN2Bare");
            CopyNodeAndValue(compNode, newNode, "CNRed", "CNRed");
            CopyNodeAndValue(compNode, newNode, "CNCov", "CNCov"); */

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Water", Utility.Xml.Name(compNode));

            XmlNode childNode;
            // thickness array
            childNode = Utility.Xml.Find(compNode, "Thickness");
            CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            // NO3 array
            childNode = Utility.Xml.Find(compNode, "BD");
            CopyNodeAndValueArray(childNode, newNode, "BD", "BD");
            // NH4 array
            childNode = Utility.Xml.Find(compNode, "AirDry");
            CopyNodeAndValueArray(childNode, newNode, "AirDry", "AirDry");
            // SW array
            childNode = Utility.Xml.Find(compNode, "LL15");
            CopyNodeAndValueArray(childNode, newNode, "LL15", "LL15");
            childNode = Utility.Xml.Find(compNode, "DUL");
            CopyNodeAndValueArray(childNode, newNode, "DUL", "DUL");
            childNode = Utility.Xml.Find(compNode, "SAT");
            CopyNodeAndValueArray(childNode, newNode, "SAT", "SAT");

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSample(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Sample", Utility.Xml.Name(compNode));
            
            string date = GetInnerText(compNode, "Date");
            XmlNode dateNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Date"));
            dateNode.InnerText = Utility.Date.DMYtoISO(date);
            XmlNode childNode;
            // thickness array
            childNode = Utility.Xml.Find(compNode, "Thickness");
            CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            // NO3 array
            childNode = Utility.Xml.Find(compNode, "NO3");
            CopyNodeAndValueArray(childNode, newNode, "NO3", "NO3");
            // NH4 array
            childNode = Utility.Xml.Find(compNode, "NH4");
            CopyNodeAndValueArray(childNode, newNode, "NH4", "NH4");
            // SW array
            childNode = Utility.Xml.Find(compNode, "SW");
            CopyNodeAndValueArray(childNode, newNode, "SW", "SW");

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSoil(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Soil", Utility.Xml.Name(compNode));

            CopyNodeAndValue(compNode, newNode, "SoilType", "SoilType");
            CopyNodeAndValue(compNode, newNode, "LocalName", "LocalName");
            CopyNodeAndValue(compNode, newNode, "Site", "Site");
            CopyNodeAndValue(compNode, newNode, "NearestTown", "NearestTown");
            CopyNodeAndValue(compNode, newNode, "Region", "Region");
            CopyNodeAndValue(compNode, newNode, "NaturalVegetation", "NaturalVegetation");

            AddChildComponents(compNode, newNode);

            return newNode;
        }

        /// <summary>
        /// Copy an array of scalars
        /// </summary>
        /// <param name="srcParentNode"></param>
        /// <param name="destParentNode"></param>
        /// <param name="srcName"></param>
        /// <param name="destName"></param>
        private void CopyNodeAndValueArray(XmlNode srcParentNode, XmlNode destParentNode, string srcName, string destName)
        {
            if (srcParentNode != null)
            {
                XmlNode childNode = destParentNode.AppendChild(destParentNode.OwnerDocument.CreateElement(destName));

                XmlNode child = srcParentNode.FirstChild;
                while (child != null)
                {
                    CopyNodeAndValue(child, childNode, child.Name);
                    child = child.NextSibling;
                }
            }
        }

        /// <summary>
        /// Copy the srcNode using destName.
        /// </summary>
        /// <param name="srcNode"></param>
        /// <param name="destParentNode"></param>
        /// <param name="destName"></param>
        private void CopyNodeAndValue(XmlNode srcNode, XmlNode destParentNode, string destName)
        {
            if (srcNode != null)
            {
                XmlNode valNode = destParentNode.AppendChild(destParentNode.OwnerDocument.CreateElement(destName));
                valNode.InnerText = srcNode.InnerText;
            }
        }

        /// <summary>
        /// When copying a single unique node and value
        /// </summary>
        /// <param name="srcParentNode"></param>
        /// <param name="destParentNode"></param>
        /// <param name="srcName"></param>
        /// <param name="destName"></param>
        private void CopyNodeAndValue(XmlNode srcParentNode, XmlNode destParentNode, string srcName, string destName)
        {
            if (srcParentNode != null)
            {
                XmlNode childNode = destParentNode.AppendChild(destParentNode.OwnerDocument.CreateElement(destName));
                childNode.InnerText = GetInnerText(srcParentNode, srcName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportOutputFile(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Report", Utility.Xml.Name(compNode));
            // compNode/variables array
            XmlNode varsNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Variables"));
            List<XmlNode> nodes = new List<XmlNode>();
            Utility.Xml.FindAllRecursively(compNode, "variable", ref nodes);
            foreach (XmlNode var in nodes)
            {
                XmlNode varNode = varsNode.AppendChild(destParent.OwnerDocument.CreateElement("string"));
                if (var.InnerText.Contains("dd/mm/yyyy"))
                {
                    varNode.InnerText = "[Clock].Today";
                }
                else
                {
                    varNode.InnerText = var.InnerText;
                }
            }
            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportManager(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Manager", Utility.Xml.Name(compNode));
            XmlNode codeNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Code"));
            XmlNode cdataNode = codeNode.AppendChild(destParent.OwnerDocument.CreateNode("cdatasection", "", ""));
            XmlNode srcCodeNode = Utility.Xml.FindByType(compNode, "script/text");
            String code;
            if (srcCodeNode != null)
            {
                code = srcCodeNode.InnerText;
            }
            else
                code = "using System;\nusing Models.Core;\nusing Models.PMF;\nnamespace Models\n{\n}";
            cdataNode.InnerText = "/*\n" + code + "\n*/";   // just add the old script in a commented section
            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportMetFile(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "WeatherFile", Utility.Xml.Name(compNode));
            // compNode/filename value
            XmlNode anode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("FileName"));
            string metfilepath = GetInnerText(compNode, "filename");

            //resolve shortcut ?
            XmlNode filenameNode = Utility.Xml.Find(compNode, "filename");
            if (filenameNode != null)
            {
                string attrValue = AttributeText(filenameNode, "shortcut");
                if (attrValue != "")
                {
                    //resolve shortcut
                }
            }
                
            anode.InnerText = metfilepath.Replace("%apsim%", ApsimPath);

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportClock(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Clock", Utility.Xml.Name(compNode));
            string startDate = GetInnerText(compNode, "start_date");
            string endDate = GetInnerText(compNode, "end_date");

            XmlNode dateNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("StartDate"));
            dateNode.InnerText = Utility.Date.DMYtoISO(startDate);
            dateNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("EndDate"));
            dateNode.InnerText = Utility.Date.DMYtoISO(endDate);
 
            return newNode;
        }

        private XmlNode ImportOperations(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Operations", Utility.Xml.Name(compNode));

            XmlNode childNode;
            
            List<XmlNode> nodes = new List<XmlNode>();
            Utility.Xml.FindAllRecursively(compNode, "operation", ref nodes);
            foreach (XmlNode oper in nodes)
            {
                XmlNode operationNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Operation"));
                XmlNode dateNode = operationNode.AppendChild(destParent.OwnerDocument.CreateElement("Date"));

                string childText = "";
                childNode = Utility.Xml.Find(oper, "date");
                if (childNode != null)
                    childText = Utility.Date.DMYtoISO(childNode.InnerText);
                dateNode.InnerText = childText;
                
                XmlNode actionNode = operationNode.AppendChild(destParent.OwnerDocument.CreateElement("Action"));

                childText = "";
                childNode = Utility.Xml.Find(oper, "action");
                if (childNode != null)
                    childText = "// " + childNode.InnerText;    //comment out the operations code for now
                actionNode.InnerText = childText;
            } // next operation

            return newNode;
        }

        /// <summary>
        /// Do the mechanical adding of the xml node in the document
        /// </summary>
        /// <param name="parentNode">The parent of the new node</param>
        /// <param name="elementName">The element tag</param>
        /// <param name="name">The value stored in the child <code> <Name> ... </Name> </code></param>
        /// <returns>The new xml element node item</returns>
        private XmlNode AddCompNode(XmlNode parentNode, String elementName, String name)
        {
            XmlNode newNode = parentNode.AppendChild(parentNode.OwnerDocument.CreateElement(elementName));
            XmlNode newNameNode = newNode.AppendChild(parentNode.OwnerDocument.CreateElement("Name"));
            newNameNode.InnerText = name;

            return newNode;
        }

        /// <summary>
        /// Adds a new DataStore component.
        /// </summary>
        /// <param name="newSim">The new simulation xml node.</param>
        private void AddDataStore(XmlNode newSim)
        {
            AddCompNode(newSim, "DataStore", "DataStore");
        }

        private string GetInnerText(XmlNode parentNode, string nodePath)
        {
            XmlNode srcNode = Utility.Xml.Find(parentNode, nodePath);
            if (srcNode != null)
                return srcNode.InnerText;
            else
                return "";
        }

        private String AttributeText(XmlNode node, String attr)
        {
            if (node.Attributes[attr] != null)
                return node.Attributes[attr].Value;
            else
                return "";
        }
    }
}
