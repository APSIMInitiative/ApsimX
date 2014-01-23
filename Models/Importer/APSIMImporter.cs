
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
    /// 
    /// Some of the components are added to the imported xml by creating instances of the
    /// Model object and then populating it then importing the serialised XML.
    /// Other components that have child components are done purely using XML creation
    /// and copying. This is because Model objects generate XML for their children and
    /// a merge would be required on the child XML. Easier just to do the
    /// whole component purely in XML (I hope).
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
                else if (compNode.Name == "manager2")
                {
                    newNode = ImportManager2(compNode, destParent, newNode);
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
                    newNode = ImportSurfaceOM(compNode, destParent, newNode);
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
        private XmlNode ImportSurfaceOM(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.SurfaceOM.SurfaceOrganicMatter mysom = new Models.SurfaceOM.SurfaceOrganicMatter();

            mysom.PoolName  = GetInnerText(compNode, "PoolName");
            mysom.type      = GetInnerText(compNode, "type");
            mysom.mass      = GetInnerText(compNode, "mass");
            mysom.cnr       = GetInnerText(compNode, "cnr");
            mysom.cpr       = GetInnerText(compNode, "cpr");
            mysom.standing_fraction = GetInnerText(compNode, "standing_fraction");

            newNode = ImportObject(destParent, newNode, mysom, Utility.Xml.Name(compNode));

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
            Models.Graph.Graph mygraph = new Models.Graph.Graph();

            // set any values here

            newNode = ImportObject(destParent, newNode, mygraph, Utility.Xml.Name(compNode));

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
            Models.Soils.SoilWater mysoilwater = new Models.Soils.SoilWater();

            // store some of the values found in the imported document
            mysoilwater.SummerCona  = GetChildDouble(compNode, "SummerCona", 0.0);
            mysoilwater.SummerU     = GetChildDouble(compNode, "SummerU", 0.0);
            mysoilwater.WinterCona  = GetChildDouble(compNode, "WinterCona", 0.0);
            mysoilwater.WinterU     = GetChildDouble(compNode, "WinterU", 0.0);
            mysoilwater.DiffusConst = GetChildDouble(compNode, "DiffusConst", 0.0);
            mysoilwater.DiffusSlope = GetChildDouble(compNode, "DiffusSlope", 0.0);
            mysoilwater.Salb        = GetChildDouble(compNode, "Salb", 0.0);
            mysoilwater.CN2Bare     = GetChildDouble(compNode, "CN2Bare", 0.0);
            mysoilwater.CNRed       = GetChildDouble(compNode, "CNRed", 0.0);
            mysoilwater.CNCov       = GetChildDouble(compNode, "CNCov", 0.0);
            mysoilwater.SummerDate  = GetInnerText(compNode, "SummerDate");
            mysoilwater.WinterDate  = GetInnerText(compNode, "WinterDate");
            mysoilwater.Thickness   = GetChildDoubles(compNode, "Thickness", 0);
            mysoilwater.SWCON       = GetChildDoubles(compNode, "SWCON", 0);

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, mysoilwater, Utility.Xml.Name(compNode));

            return newNode;
        }

        /// <summary>
        /// Generate xml for the new object and import the serialised xml into the new document
        /// </summary>
        /// <param name="destParent">Destination parent xml node</param>
        /// <param name="newNode">The new node</param>
        /// <param name="newObject">The object to import into the document</param>
        /// <returns>The new node</returns>
        private static XmlNode ImportObject(XmlNode destParent, XmlNode newNode, Model newObject, string objName)
        {
            newObject.Name = objName;
            string newObjxml = Utility.Xml.Serialise(newObject, true);
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(newObjxml);
            newNode = destParent.OwnerDocument.ImportNode(xdoc.DocumentElement, true);
            newNode = destParent.AppendChild(newNode);

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
            //Models.Soils.Soil mysoil = new Models.Soils.Soil();

            //mysoil.SoilType = GetInnerText(compNode, "SoilType");

            // import this object into the new xml document
            //newNode = ImportObject(destParent, newNode, mysoil, Utility.Xml.Name(compNode));

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
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportOutputFile(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Report myreport = new Models.Report();

            // compNode/variables array
            List<XmlNode> nodes = new List<XmlNode>();
            Utility.Xml.FindAllRecursively(compNode, "variable", ref nodes);
            myreport.Variables = new string[nodes.Count];
            int i = 0;
            foreach (XmlNode var in nodes)
            {
                if ( var.InnerText.Contains("yyyy") ) 
                {
                    myreport.Variables[i] = "[Clock].Today";
                }
                else
                {
                    myreport.Variables[i] = var.InnerText;
                }
                i++;
            }
            // now for the events
            nodes.Clear();
            Utility.Xml.FindAllRecursively(compNode, "event", ref nodes);
            myreport.Events = new string[nodes.Count];
            i = 0;
            foreach (XmlNode _event in nodes)
            {
                if (_event.InnerText == "end_day")
                {
                    myreport.Events[i] = "[Clock].EndOfDay";
                }
                else if (_event.InnerText == "daily")
                {
                    myreport.Events[i] = "[Clock].StartOfDay";
                }
                i++;
            }

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, myreport, Utility.Xml.Name(compNode));

            return newNode;
        }

        /// <summary>
        /// Import a Manager(1) component.
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportManager(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Manager mymanager = new Models.Manager();

            StringBuilder code = new StringBuilder();
            code.Append("using System;\nusing Models.Core;\nusing Models.PMF;\nnamespace Models\n{\n");
            code.Append("\t[Serializable]\n");
            code.Append("\t[System.Xml.Serialization.XmlInclude(typeof(Model))]\n");
            code.Append("\tpublic class Script : Model\n");
            code.Append("\t{\n");
            code.Append("\t\t[Link] Clock TimeClock\n"); 

            List<XmlNode> nodes = new List<XmlNode>();
            Utility.Xml.FindAllRecursively(compNode, "script", ref nodes);
            foreach (XmlNode script in nodes)
            {
                // find the event
                XmlNode eventNode = Utility.Xml.Find(script, "event");

                // find the text
                XmlNode textNode = Utility.Xml.Find(script, "text");
                if ((textNode != null) && (textNode.InnerText.Length > 0))
                {
                    if (eventNode.InnerText == "init")
                    {
                        code.Append("\t\t[EventSubscribe(\"Initialised\")]\n");
                        code.Append("\t\tprivate void OnInitialised(object sender, EventArgs e)\n");
                    }
                    else if (eventNode.InnerText == "start_of_day")
                    {
                        code.Append("\t\t[EventSubscribe(\"StartOfDay\")]\n");
                        code.Append("\t\tprivate void OnStartOfDay(object sender, EventArgs e)\n");
                    }
                    else if (eventNode.InnerText == "end_of_day")
                    {
                        code.Append("\t\t[EventSubscribe(\"EndOfDay\")]\n");
                        code.Append("\t\tprivate void OnEndOfDay(object sender, EventArgs e)\n");
                    }
                    code.Append("\t\t{\n");
                    code.Append("\t\t\t/*\n");
                    code.Append("\t\t\t\t" + textNode.InnerText + "\n");
                    code.Append("\t\t\t*/\n");
                    code.Append("\t\t}\n");
                }
            }
            code.Append("\t}\n}\n");
           
            mymanager.Code = code.ToString();   

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, mymanager, Utility.Xml.Name(compNode));
            
            return newNode;
        }

        /// <summary>
        /// Import a Manager(2) component.
        /// </summary>
        /// <param name="compNode"></param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportManager2(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Manager mymanager = new Models.Manager();

            // copy code here

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, mymanager, Utility.Xml.Name(compNode));

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
            Models.Clock myclock = new Models.Clock();

            string startDate = GetInnerText(compNode, "start_date");
            string endDate = GetInnerText(compNode, "end_date");
            myclock.StartDate = Utility.Date.DMYtoDate(startDate);
            myclock.EndDate   = Utility.Date.DMYtoDate(endDate);

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, myclock, Utility.Xml.Name(compNode));
 
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

        /// <summary>
        /// Get the array of doubles for nodePath element
        /// </summary>
        /// <param name="parentNode">Parent node to search</param>
        /// <param name="nodePath">The child node being sought</param>
        /// <param name="defValue">Use this as the default if an invalid value is found</param>
        /// <returns></returns>
        private double[] GetChildDoubles(XmlNode parentNode, string nodePath, double defValue)
        {
            double[] values = null;

            XmlNode srcNode = Utility.Xml.Find(parentNode, nodePath);
            if (srcNode != null)
            {
                List<XmlNode> nodes = Utility.Xml.ChildNodesByName(srcNode, "double");
                values = new double[nodes.Count];

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode child = nodes[i];
                    if (child.InnerText.Length > 0)
                        values[i] = Convert.ToDouble(child.InnerText);
                    else
                        values[i] = defValue;
                }
            }
                
            return values;
        }

        /// <summary>
        /// Get the floating point value from the InnerText for the child node of parentNode.
        /// </summary>
        /// <param name="parentNode">Parent node to search</param>
        /// <param name="nodePath">The child node being sought</param>
        /// <param name="defValue">Use this default if an invalid value found</param>
        /// <returns>The floating point value or the default value</returns>
        private double GetChildDouble(XmlNode parentNode, string nodePath, double defValue)
        {
            XmlNode srcNode = Utility.Xml.Find(parentNode, nodePath);
            if ((srcNode != null) && (srcNode.InnerText.Length > 0))
                return Convert.ToDouble(srcNode.InnerText);
            else
                return defValue;
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
