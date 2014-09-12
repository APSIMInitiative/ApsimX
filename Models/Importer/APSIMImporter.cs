
namespace Importer
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Xml;
    using System.Text.RegularExpressions;
    using System.Reflection;
    using Models.Core;
    using ApsimFile;

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
        /// Used as flags during importation of a paddock
        /// </summary>
        private bool SurfOMExists = false;
        private bool SoilWaterExists = false;
        private bool MicroClimateExists = false;
        private DateTime StartDate;

        // fertiliser type conversion lookups
        Dictionary<string, string> Fertilisers;

        /// <summary>
        /// Original path that is substituted for %apsim%
        /// </summary>
        public string ApsimPath = "";

        public APSIMImporter()
        {
            // fertiliser type strings that are mapped to Fertiliser.Types
            Fertilisers = new Dictionary<string, string>();
            Fertilisers.Add("calcite_ca", "CalciteCA");
            Fertilisers.Add("calcite_fine", "CalciteFine");
            Fertilisers.Add("dolomite", "Dolomite");
            Fertilisers.Add("NO3_N", "NO3N");
            Fertilisers.Add("NH4_N", "NH4N");
            Fertilisers.Add("NH4NO3", "NH4NO3N");
            Fertilisers.Add("DAP", "DAP");
            Fertilisers.Add("MAP", "MAP");
            Fertilisers.Add("urea_N", "UreaN");
            Fertilisers.Add("urea_no3", "UreaNO3");
            Fertilisers.Add("urea", "Urea");
            Fertilisers.Add("nh4so4_n", "NH4SO4N");
            Fertilisers.Add("rock_p", "RockP");
            Fertilisers.Add("banded_p", "BandedP");
            Fertilisers.Add("broadcast_p", "BroadcastP");

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
                    throw new Exception(e.Message);
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
                ApsimFile infile;

                // initialise the configuration for ApsimFile               
                if (ApsimPath.Length > 0)
                {
                    Configuration.SetApsimDir(ApsimPath);
                    PlugIns.LoadAll();  // loads the plugins from apsim.xml and types from other xml files
                    // open and resolve all the links
                    infile = new ApsimFile(filename);
                }
                else
                {
                    // Get the types.xml from built-in resources
                    string xml = Properties.Resources.Types;

                    // Create a temporary file.
                    string tempFileName = Path.GetTempFileName();
                    StreamWriter f = new StreamWriter(tempFileName);
                    f.Write(xml);
                    f.Close();

                    PlugIns.Load(tempFileName);

                    infile = new ApsimFile();
                    XmlDocument filedoc = new XmlDocument();
                    filedoc.Load(filename);
                    infile.Open(filedoc.DocumentElement);
                }
                string concretexml = infile.RootComponent.FullXMLNoShortCuts();

                //open the .apsim file
                //StreamReader xmlReader = new StreamReader(filename);
                XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlReader.ReadToEnd());
                doc.LoadXml(concretexml);
                //xmlReader.Close();

                //create new apsimx document
                XmlNode newNode;
                XmlDocument xdoc = new XmlDocument();
                XmlNode xdocNode = xdoc.CreateElement("Simulations");
                xdoc.AppendChild(xdocNode);
                newNode = xdocNode.AppendChild(xdoc.CreateElement("Name"));
                newNode.InnerText = "Simulations";

                XmlNode rootNode = doc.DocumentElement;     // get first folder
                AddFoldersAndSimulations(rootNode, xdocNode);
                AddDataStore(xdocNode);                     // each file must contain a DataStore

                //write to temporary xfile
                StreamWriter xmlWriter = new StreamWriter(xfile);
                xmlWriter.Write(Utility.Xml.FormattedXML(xdoc.OuterXml));
                xmlWriter.Close();

                try
                {
                    newSimulations = Simulations.Read(xfile);
                }
                catch (Exception exp)
                {
                    throw new Exception("Cannot create the Simulations object from the input : Error - " + exp.Message + "\n");
                }
                File.Delete(xfile);
            }
            catch (Exception exp)
            {
                throw new Exception("Cannot create a simulation from " + filename + " : Error - " + exp.Message + "\n");
            }
            return newSimulations;
        }

        /// <summary>
        /// At the top level we only want to add folder and simulation nodes, not nodes
        /// under 'shared' folders eg. wheat validation.
        /// </summary>
        /// <param name="systemNode"></param>
        /// <param name="destParent"></param>
        private void AddFoldersAndSimulations(XmlNode systemNode, XmlNode destParent)
        {
            XmlNode child = systemNode.FirstChild;
            while (child != null)
            {
                if (child.Name == "simulation")
                    AddComponent(child, ref destParent);
                else if (child.Name == "folder")
                {
                    XmlNode newFolder = AddCompNode(destParent, "Folder", Utility.Xml.NameAttr(child));
                    AddFoldersAndSimulations(child, newFolder);
                }
                child = child.NextSibling;
            }
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
                    XmlNode newSim = AddCompNode(destParent, "Simulation", Utility.Xml.NameAttr(compNode));
                    AddChildComponents(compNode, newSim);
                }
                else if (compNode.Name == "folder")
                {
                    XmlNode newFolder = AddCompNode(destParent, "Folder", Utility.Xml.NameAttr(compNode));
                    AddChildComponents(compNode, newFolder);
                }
                else if (compNode.Name == "clock")
                {
                    newNode = ImportClock(compNode, destParent, newNode);
                }
                else if (compNode.Name == "metfile")
                {
                    newNode = ImportMetFile(compNode, destParent, newNode);
                }
                else if (compNode.Name == "micromet")
                {
                    newNode = ImportMicromet(compNode, destParent, newNode);
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
                    newNode = AddCompNode(destParent, "Summary", Utility.Xml.NameAttr(compNode));
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
                    SoilWaterExists = (newNode != null);
                    AddCompNode(destParent, "SoilNitrogen", "SoilNitrogen");
                    // may need to copy more details for SoilNitrogen
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
                else if (compNode.Name == "SoilCrop")
                {
                    newNode = ImportSoilCrop(compNode, destParent, newNode);
                }
                else if (compNode.Name == "area")
                {
                    newNode = AddCompNode(destParent, "Zone", Utility.Xml.NameAttr(compNode));
                    XmlNode newPaddockNode = newNode;

                    string area = GetInnerText(compNode, "paddock_area");
                    if (area == "")
                    {
                        Utility.Xml.SetValue(compNode, "paddock_area", "1.0");
                    }
                    CopyNodeAndValue(compNode, newPaddockNode, "paddock_area", "Area", true);
                    SurfOMExists = false;
                    SoilWaterExists = false;
                    MicroClimateExists = false;
                    // copy all the children in this paddock
                    AddChildComponents(compNode, newPaddockNode);
                    if (SoilWaterExists && !SurfOMExists)   // if it contains a soilwater then
                    {
                        Console.WriteLine("Added SurfaceOM to " + Utility.Xml.FullPathUsingName(newPaddockNode));
                        Models.SurfaceOM.SurfaceOrganicMatter mysom = new Models.SurfaceOM.SurfaceOrganicMatter();

                        mysom.PoolName = "wheat_stubble";
                        mysom.type = "wheat";
                        mysom.mass = "0";
                        mysom.cnr = "80";
                        mysom.cpr = "0";
                        mysom.standing_fraction = "0.0";

                        newNode = ImportObject(newPaddockNode, newNode, mysom, "SurfaceOrganicMatter");
                    }
                    // alway ensure that MicroClimate exists in this paddock
                    if (!MicroClimateExists)
                    {
                        newNode = ImportMicromet(null, newPaddockNode, newNode);    // create a new node from no source
                    }
                }
                else if (compNode.Name == "surfaceom")
                {
                    newNode = ImportSurfaceOM(compNode, destParent, newNode);
                    SurfOMExists = (newNode != null);
                }
                else if (compNode.Name == "memo")
                {
                    newNode = AddCompNode(destParent, "Memo", Utility.Xml.NameAttr(compNode));
                    XmlNode memoTextNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("MemoText"));
                    memoTextNode.InnerText = compNode.InnerText;
                }
                else if (compNode.Name == "Graph")
                {
                    newNode = ImportGraph(compNode, destParent, newNode);
                }
                else
                {
                    // all other components not listed above will be handled by this
                    // code and some test used to try to determine what type of object it is
                    string show = Types.Instance.MetaData(compNode.Name, "ShowInMainTree"); 
                    if (String.Compare(show, "yes", true) == 0)
                    {
                        // make some guesses about the type of component to add
                        string classname = compNode.Name[0].ToString().ToUpper() + compNode.Name.Substring(1, compNode.Name.Length - 1); // first char to uppercase
                        string compClass = Types.Instance.MetaData(compNode.Name, "class");
                        bool usePlantClass = (compClass.Length == 0) || ( (compClass.Length > 4) && (String.Compare(compClass.Substring(0, 5), "plant", true) == 0) );
                        if (Types.Instance.IsCrop(compNode.Name) &&  usePlantClass)
                        {
                            ImportPlant(compNode, destParent, newNode);
                        }
                        else
                        {
                            // objects like root may have a name attribute
                            string usename = Utility.Xml.NameAttr(compNode);
                            if (usename.Length < 1)
                                usename = compNode.Name;
                            newNode = AddCompNode(destParent, classname, usename);    //found a model component that should be added to the simulation
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                throw new Exception("Cannot import " + compNode.Name + " :Error - " + exp.Message + "\n");
            }
            return newNode; 
        }

        /// <summary>
        /// Import a Plant component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="plantNode"></param>
        /// <returns></returns>
        private XmlNode ImportPlant(XmlNode compNode, XmlNode destParent, XmlNode plantNode)
        {
            string name = Utility.Xml.NameAttr(compNode);

            Model plantModel;
            if (name == "wheat")
            {
                plantModel = new Models.PMF.OldPlant.Plant15();
            }
            else if (name == "OilPalm")
            {
                plantModel = new Models.PMF.OilPalm.OilPalm();
            }
            else
            {
                plantModel = new Models.PMF.Plant();
            }

            plantNode = ImportObject(destParent, plantNode, plantModel, name);

            Utility.Xml.SetValue(plantNode, "ResourceName", Utility.String.CamelCase(name));

            XmlNode cropTypeNode = plantNode.AppendChild(plantNode.OwnerDocument.CreateElement("CropType"));
            cropTypeNode.InnerText = name;

            return plantNode;
        }

        /// <summary>
        /// Unused
        /// </summary>
        /// <param name="plantNode"></param>
        /// <param name="model"></param>
        private void AddLinkedObjects(XmlNode plantNode, Object model)
        {

            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = Utility.Reflection.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null)
                {
                    if (!field.FieldType.IsAbstract && !link.IsOptional)
                    {
                        // get the type here and add a tag?
                        XmlNode newNode = AddCompNode(plantNode, field.FieldType.Name, field.Name);

                        Object obj = Activator.CreateInstance(field.FieldType);
                        AddLinkedObjects(newNode, obj);
                    }
                } 
            } 
        }

        /// <summary>
        /// Import a micromet component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportMicromet(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.MicroClimate mymicro = new Models.MicroClimate();
            string newName = "MicroClimate";

            if (compNode != null)
            {
                mymicro.soil_albedo = GetChildDouble(compNode, "soilalbedo", 0);
                mymicro.a_interception = GetChildDouble(compNode, "a_interception", 0);
                mymicro.b_interception = GetChildDouble(compNode, "b_interception", 0);
                mymicro.c_interception = GetChildDouble(compNode, "c_interception", 0);
                mymicro.d_interception = GetChildDouble(compNode, "d_interception", 0);
                newName = Utility.Xml.NameAttr(compNode);
            }
            newNode = ImportObject(destParent, newNode, mymicro, newName);

            MicroClimateExists = true; // has been added

            return newNode;
        }     

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSoilCrop(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Soils.SoilCrop mycrop = new Models.Soils.SoilCrop();

            mycrop.LL = GetChildDoubles(compNode, "LL", 0);
            mycrop.KL = GetChildDoubles(compNode, "KL", 0);
            mycrop.XF = GetChildDoubles(compNode, "XF", 0);

            newNode = ImportObject(destParent, newNode, mycrop, Utility.Xml.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
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

            newNode = ImportObject(destParent, newNode, mysom, Utility.Xml.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportGraph(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Graph.Graph mygraph = new Models.Graph.Graph();

            // set any values here

            newNode = ImportObject(destParent, newNode, mygraph, Utility.Xml.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportInitialWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Soils.InitialWater mywater = new Models.Soils.InitialWater();

            mywater.FractionFull = GetChildDouble(compNode, "FractionFull", 1.0);
            string method = GetInnerText(compNode, "PercentMethod");
            if (method.Length < 1)
                mywater.PercentMethod = Models.Soils.InitialWater.PercentMethodEnum.FilledFromTop;
            else
            {
                Models.Soils.InitialWater.PercentMethodEnum methodValue = (Models.Soils.InitialWater.PercentMethodEnum)Enum.Parse(typeof(Models.Soils.InitialWater.PercentMethodEnum), method);
                if (Enum.IsDefined(typeof(Models.Soils.InitialWater.PercentMethodEnum), methodValue))
                    mywater.PercentMethod = methodValue;
            }

            newNode = ImportObject(destParent, newNode, mywater, Utility.Xml.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportAnalysis(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Analysis", Utility.Xml.NameAttr(compNode));

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
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSOM(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "SoilOrganicMatter", Utility.Xml.NameAttr(compNode));

            XmlNode childNode;
            CopyNodeAndValue(compNode, newNode, "RootCN", "RootCN", true);
            CopyNodeAndValue(compNode, newNode, "RootWt", "RootWt", true);
            CopyNodeAndValue(compNode, newNode, "SoilCN", "SoilCN", true);
            CopyNodeAndValue(compNode, newNode, "EnrACoeff", "EnrACoeff", true);
            CopyNodeAndValue(compNode, newNode, "EnrBCoeff", "EnrBCoeff", true);

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
        /// <param name="compNode">The node being imported from the apsim file xml</param>
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
            newNode = ImportObject(destParent, newNode, mysoilwater, Utility.Xml.NameAttr(compNode));

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
            string newObjxml = Utility.Xml.Serialise(newObject, false);
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(newObjxml);
            newNode = destParent.OwnerDocument.ImportNode(xdoc.DocumentElement, true);
            newNode = destParent.AppendChild(newNode);

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Water", Utility.Xml.NameAttr(compNode));

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

            AddChildComponents(compNode, newNode);

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSample(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Sample", Utility.Xml.NameAttr(compNode));
            
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

            CopyNodeAndValue(compNode, newNode, "NO3Units", "NO3Units", false);
            CopyNodeAndValue(compNode, newNode, "NH4Units", "NH4Units", false);

            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        private XmlNode ImportSoil(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            //Models.Soils.Soil mysoil = new Models.Soils.Soil();

            //mysoil.SoilType = GetInnerText(compNode, "SoilType");

            // import this object into the new xml document
            //newNode = ImportObject(destParent, newNode, mysoil, Utility.Xml.Name(compNode));

            newNode = AddCompNode(destParent, "Soil", Utility.Xml.NameAttr(compNode));

            CopyNodeAndValue(compNode, newNode, "NO3Units", "NO3Units", false);
            CopyNodeAndValue(compNode, newNode, "NH4Units", "NH4Units", false);
            CopyNodeAndValue(compNode, newNode, "SWUnits", "SWUnits", false);
            CopyNodeAndValue(compNode, newNode, "OCUnits", "OCUnits", false);
            CopyNodeAndValue(compNode, newNode, "PHUnits", "PHUnits", false);

            CopyNodeAndValue(compNode, newNode, "SoilType", "SoilType", true);
            CopyNodeAndValue(compNode, newNode, "LocalName", "LocalName", true);
            CopyNodeAndValue(compNode, newNode, "Site", "Site", true);
            CopyNodeAndValue(compNode, newNode, "NearestTown", "NearestTown", true);
            CopyNodeAndValue(compNode, newNode, "Region", "Region", true);
            CopyNodeAndValue(compNode, newNode, "NaturalVegetation", "NaturalVegetation", true);

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
            myreport.VariableNames = new string[nodes.Count];
            int i = 0;
            foreach (XmlNode var in nodes)
            {
                if ( var.InnerText.Contains("yyyy") ) 
                {
                    myreport.VariableNames[i] = "[Clock].Today";
                }
                else
                {
                    myreport.VariableNames[i] = var.InnerText;
                }
                i++;
            }
            // now for the events
            nodes.Clear();
            Utility.Xml.FindAllRecursively(compNode, "event", ref nodes);
            myreport.EventNames = new string[nodes.Count];
            i = 0;
            foreach (XmlNode _event in nodes)
            {
                if (String.Compare(_event.InnerText, "end_day", true) == 0)
                {
                    myreport.EventNames[i] = "[Clock].DoReport";
                }
                else if (String.Compare(_event.InnerText, "daily", true) == 0)
                {
                    myreport.EventNames[i] = "[Clock].DoReport";
                }
                else
                {
                    myreport.EventNames[i] = "[Clock].DoReport";
                }
                i++;
            }

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, myreport, Utility.Xml.NameAttr(compNode));

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
            code.Append("using System;\nusing Models.Core;\nusing Models.PMF;\nusing Models.PMF.OldPlant;\nnamespace Models\n{\n");
            code.Append("\t[Serializable]\n");
            code.Append("\t[System.Xml.Serialization.XmlInclude(typeof(Model))]\n");
            code.Append("\tpublic class Script : Model\n");
            code.Append("\t{\n");
            code.Append("\t\t[Link] Clock Clock;\n");

            List<string> startofdayScripts = new List<string>();
            List<string> endofdayScripts = new List<string>();
            List<string> initScripts = new List<string>();
            List<string> unknownHandlerScripts = new List<string>();

            List<XmlNode> nodes = new List<XmlNode>();
            Utility.Xml.FindAllRecursivelyByType(compNode, "script", ref nodes);
            foreach (XmlNode script in nodes)
            {
                // find the event
                XmlNode eventNode = Utility.Xml.Find(script, "event");

                // find the text
                XmlNode textNode = Utility.Xml.Find(script, "text");
                if ((textNode != null) && (textNode.InnerText.Length > 0))
                {
                    if (eventNode.InnerText.ToLower() == "init")
                    {
                        initScripts.Add(textNode.InnerText);
                    }
                    else if (eventNode.InnerText.ToLower() == "start_of_day")
                    {
                        startofdayScripts.Add(textNode.InnerText);
                    }
                    else if (eventNode.InnerText.ToLower() == "end_of_day")
                    {
                        endofdayScripts.Add(textNode.InnerText);
                    }
                    else
                    {
                        // use the StartOfDay as a default when the event name is unknown
                        unknownHandlerScripts.Add("// ----- " + eventNode.InnerText + " ----- \n" + textNode.InnerText);
                    }
                }
            }
            // append all the scripts for each type
            if (initScripts.Count > 0)
            {
                code.Append("\t\t[EventSubscribe(\"Commencing\")]\n");
                code.Append("\t\tprivate void OnSimulationCommencing(object sender, EventArgs e)\n");
                code.Append("\t\t{\n");
                foreach (string scripttext in initScripts)
                {
                    code.Append("\t\t\t/*\n");
                    code.Append("\t\t\t\t" + scripttext + "\n");
                    code.Append("\t\t\t*/\n");
                }
                code.Append("\t\t}\n");
            }
            if (startofdayScripts.Count > 0)
            {
                code.Append("\t\t[EventSubscribe(\"DoManagement\")]\n");
                code.Append("\t\tprivate void OnDoManagement(object sender, EventArgs e)\n");
                code.Append("\t\t{\n");
                foreach (string scripttext in startofdayScripts)
                {
                    code.Append("\t\t\t/*\n");
                    code.Append("\t\t\t\t" + scripttext + "\n");
                    code.Append("\t\t\t*/\n");
                }
                code.Append("\t\t}\n");
            }
            if (endofdayScripts.Count > 0)
            {
                code.Append("\t\t[EventSubscribe(\"DoCalculations\")]\n");
                code.Append("\t\tprivate void OnDoCalculations(object sender, EventArgs e)\n");
                code.Append("\t\t{\n");
                foreach (string scripttext in endofdayScripts)
                {
                    code.Append("\t\t\t/*\n");
                    code.Append("\t\t\t\t" + scripttext + "\n");
                    code.Append("\t\t\t*/\n");
                }
                code.Append("\t\t}\n");
            }
            if (unknownHandlerScripts.Count > 0)
            {
                code.Append("\t\t//[EventSubscribe(\"unknown\")]\n");
                code.Append("\t\t//private void OnUnknown(object sender, EventArgs e)\n");
                code.Append("\t\t//{\n");
                foreach (string scripttext in unknownHandlerScripts)
                {
                    code.Append("\t\t\t/*\n");
                    code.Append("\t\t\t\t" + scripttext + "\n");
                    code.Append("\t\t\t*/\n");
                }
                code.Append("\t\t//}\n");
            }
            code.Append("\t}\n}\n");
           
            mymanager.Code = code.ToString();   

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, mymanager, Utility.Xml.NameAttr(compNode));

            
            // some Manager components have Memo children. For ApsimX the import
            // will just put them as the next sibling of the Manager rather
            // than as a child of the Manager.
            destParent = ImportManagerMemos(compNode, destParent);
            
            return newNode;
        }

        /// <summary>
        /// Import any memo children.
        /// </summary>
        /// <param name="compNode">The Manager component node</param>
        /// <param name="destParent">The parent (folder) node of the Manager.</param>
        /// <returns></returns>
        private XmlNode ImportManagerMemos(XmlNode compNode, XmlNode destParent)
        {
            XmlNode child = compNode.FirstChild;
            while (child != null)
            {
                if (child.Name == "memo")
                {
                    AddComponent(child, ref destParent);
                }
                child = child.NextSibling;
            }
            return destParent;
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
            newNode = ImportObject(destParent, newNode, mymanager, Utility.Xml.NameAttr(compNode));

            // some Manager components have Memo children. For ApsimX the import
            // will just put them as the next sibling of the Manager rather
            // than as a child of the Manager.
            destParent = ImportManagerMemos(compNode, destParent);

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
            newNode = AddCompNode(destParent, "WeatherFile", "WeatherFile");
            // compNode/filename value
            XmlNode anode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("FileName"));
            string metfilepath = Utility.PathUtils.OSFilePath(GetInnerText(compNode, "filename"));
              
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
            StartDate = myclock.StartDate;
            myclock.EndDate   = Utility.Date.DMYtoDate(endDate);

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, myclock, Utility.Xml.NameAttr(compNode));
 
            return newNode;
        }

        private XmlNode ImportOperations(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = AddCompNode(destParent, "Operations", Utility.Xml.NameAttr(compNode));

            XmlNode childNode;
            
            List<XmlNode> nodes = new List<XmlNode>();
            Utility.Xml.FindAllRecursively(compNode, "operation", ref nodes);
            foreach (XmlNode oper in nodes)
            {
                XmlNode operationNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Operation"));
                XmlNode dateNode = operationNode.AppendChild(destParent.OwnerDocument.CreateElement("Date"));

                string childText = "";
                childNode = Utility.Xml.Find(oper, "date");
                DateTime when;
                if (childNode != null && DateTime.TryParse(childNode.InnerText, out when))
                    childText = when.ToString("yyyy-MM-dd");
                else if (childNode != null && childNode.InnerText != "")
                {
                    childText = Utility.Date.DMYtoISO(childNode.InnerText);
                    if (childText == "0001-01-01")
                    {
                        childText = Utility.Date.GetDate(childNode.InnerText, StartDate).ToString("yyyy-MM-dd");
                    }
                }
                else
                    childText = "0001-01-01";
                dateNode.InnerText = childText;
                
                XmlNode actionNode = operationNode.AppendChild(destParent.OwnerDocument.CreateElement("Action"));

                childText = " ";
                childNode = Utility.Xml.Find(oper, "action");
                if (childNode != null)
                {
                    childText = childNode.InnerText;    

                    // parse the operation and determine if this can be converted to an apsimx function call
                    // ** This code makes a BIG assumption that the fertiliser component has that name. Also the irrigation component!
                    // if this name is not found then the conversion is not done as expected.
                    childText = childText.Trim();
                    int index = childText.IndexOf(" ");
                    if (index > 0)
                    {
                        string operationOn = childText.Substring(0, index);
                        if ((String.Compare(operationOn, "fertiliser", true) == 0) && ((childText.IndexOf(" apply") > 0) || (childText.IndexOf(" Apply") > 0)))
                        {
                            string amount = "0", type = "", depth = "0";

                            FindTokenValue("amount", childText, ref amount);
                            FindTokenValue("type", childText, ref type);

                            string newtype;
                            Fertilisers.TryGetValue(type, out newtype);
                            type = "Fertiliser.Types." + newtype;
                            FindTokenValue("depth", childText, ref depth);

                            childText = String.Format("Fertiliser.Apply({0}, {1}, {2});", amount, type, depth);
                        }
                        else
                        {
                            if ((String.Compare(operationOn, "irrigation", true) == 0) && ((childText.IndexOf(" apply") > 0) || (childText.IndexOf(" Apply") > 0)))
                            {
                                string amount = "0";

                                FindTokenValue("amount", childText, ref amount);

                                childText = String.Format("Irrigation.Apply({0});", amount);
                            }
                            else
                            {
                                childText = " // " + childText; // for default comment out the operations code for now
                            }
                        }
                    }
                }
                actionNode.InnerText = childText;
            } // next operation

            return newNode;
        }

        /// <summary>
        /// Rough method for parsing an old manager script function call to obtain
        /// the parameter values.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <param name="line">Input function call line text</param>
        /// <param name="value">The value found for the parameter. Contains the original value if not found.</param>
        /// <returns>Index of the parameter name found or -1 if not found</returns>
        private int FindTokenValue(string name, string line, ref string value)
        {
            int index = -1; // not found
            index = line.IndexOf(name);
            if (index > 0)
            {
                value = "";
                int i = index + name.Length;
                while ((i < line.Length) && (line[i] == ' '))
                    i++;
                // find =
                while ((i < line.Length) && (line[i] != '='))   
                    i++;
                i++;
                while ((i < line.Length) && (line[i] == ' '))
                    i++;
                // now build the value parameter
                while ((i < line.Length) && (line[i] != ' ') && (line[i] != '(') && (line[i] != ','))
                {
                    value = value + line[i];
                    i++;
                }
            }
            return index;
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
        /// <param name="forceCreate">Always create the node even if the source is not found</param>
        private void CopyNodeAndValue(XmlNode srcParentNode, XmlNode destParentNode, string srcName, string destName, bool forceCreate)
        {
            if (srcParentNode != null)
            {
                XmlNode srcChildNode = Utility.Xml.Find(srcParentNode, srcName);
                if ( forceCreate || (srcChildNode != null))
                {
                    XmlNode childNode = destParentNode.AppendChild(destParentNode.OwnerDocument.CreateElement(destName));
                    if (srcChildNode != null)
                    {
                        childNode.InnerText = srcChildNode.InnerText;
                    }
                    else
                    {
                        childNode.InnerText = "";
                    }
                }
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
