// -----------------------------------------------------------------------
// <copyright file="APSIMImporter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Importer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using APSIM.Shared.OldAPSIM;

    /// <summary>
    /// This is a worker class for the import process that converts
    /// an old APSIM 7.5 simulation file into an APSIM(X) file or Simulations object.
    /// <para/>
    /// Some of the components are added to the imported xml by creating instances of the
    /// Model object and then populating it then importing the serialized XML.
    /// Other components that have child components are done purely using XML creation
    /// and copying. This is because Model objects generate XML for their children and
    /// a merge would be required on the child XML. Easier just to do the
    /// whole component purely in XML (I hope).
    /// </summary>
    public class APSIMImporter
    {
        private string[] cropNames = {"AgPasture", "Bambatsi", "Banana2", "Barley", "BarleySWIM",
                                      "Broccoli","ButterflyPea","Canola","CanolaSWIM","Centro",
                                      "Chickpea","Chickpea2","Chicory","cloverseed","Cotton2",
                                      "Cowpea","EGrandis","EMelliodora","EPopulnea","Fababean",
                                      "Fieldpea","Fieldpea2","FrenchBean","Grassseed","Heliotrope",
                                      "Horsegram","Itallianryegrass","Kale","Lablab","Lentil",
                                      "Lettuce","Lolium_rigidum","Lucerne","Lucerne2","LucerneSWIM",
                                      "Lupin","Maize","MaizeZ","Millet","Mucuna","Mungbean","Navybean",
                                      "Oats","Oryza","Oryza2","Ozcot","Peanut","Pigeonpea","Potato",
                                      "raphanus_raphanistrum","Root","Seedling","Slurp","Sorghum",
                                      "Soybean","Stylo","Sugar","SugarCane","Sunflower","SweetCorn",
                                      "SweetSorghum","Taro2","Triticale","vine","Weed","WF_Millet",
                                      "Wheat","Wheat2","Wheat2X"};

        /// <summary>
        /// Used as flags during importation of a paddock
        /// </summary>
        private bool surfOMExists = false;

        /// <summary>
        /// Used as flags during importation of a paddock
        /// </summary>
        private bool soilWaterExists = false;

        /// <summary>
        /// Used as flags during importation
        /// </summary>
        private bool microClimateExists = false;

        /// <summary>
        /// Used as flags during importation
        /// </summary>
        private DateTime startDate;

        /// <summary>
        /// fertiliser type conversion lookups
        /// </summary>
        private Dictionary<string, string> fertilisers;

        /// <summary>
        /// Original path that is substituted for %apsim%
        /// </summary>
        public string ApsimPath = string.Empty;

        /// <summary>
        /// The default constructor
        /// </summary>
        public APSIMImporter()
        {
            // fertiliser type strings that are mapped to Fertiliser.Types
            this.fertilisers = new Dictionary<string, string>();
            this.fertilisers.Add("calcite_ca", "CalciteCA");
            this.fertilisers.Add("calcite_fine", "CalciteFine");
            this.fertilisers.Add("dolomite", "Dolomite");
            this.fertilisers.Add("NO3_N", "NO3N");
            this.fertilisers.Add("NH4_N", "NH4N");
            this.fertilisers.Add("NH4NO3", "NH4NO3N");
            this.fertilisers.Add("DAP", "DAP");
            this.fertilisers.Add("MAP", "MAP");
            this.fertilisers.Add("urea_N", "UreaN");
            this.fertilisers.Add("urea_no3", "UreaNO3");
            this.fertilisers.Add("urea", "Urea");
            this.fertilisers.Add("nh4so4_n", "NH4SO4N");
            this.fertilisers.Add("rock_p", "RockP");
            this.fertilisers.Add("banded_p", "BandedP");
            this.fertilisers.Add("broadcast_p", "BroadcastP");
        }

        /// <summary>
        /// Processes a file and writes the Simulation(s) to the .apsimx file
        /// </summary>
        /// <param name="filename">The name of the input file</param>
        public void ProcessFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    Simulations sims = this.CreateSimulations(filename);
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
        /// <param name="dir">The directory to process</param>
        public void ProcessDir(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);

            foreach (FileInfo file in dirInfo.GetFiles("*.apsim"))
            {
                this.ProcessFile(file.FullName);
            }
        }

        /// <summary>
        /// Interrogate the .apsim file XML and attempt to construct a 
        /// useful APSIMX Simulation object(s). Uses a temporary file
        /// location.
        /// </summary>
        /// <param name="filename">Source file (.apsim)</param>
        /// <returns>An APSIMX Simulations object</returns>
        public Simulations CreateSimulations(string filename)
        {
            string xfile = Path.GetTempFileName(); 
            Simulations newSimulations = null;

            try
            {
                // open the .apsim file
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);

                Shortcuts.Remove(doc.DocumentElement);

                // create new apsimx document
                XmlNode newNode;
                XmlDocument xdoc = new XmlDocument();
                XmlNode xdocNode = xdoc.CreateElement("Simulations");
                xdoc.AppendChild(xdocNode);
                newNode = xdocNode.AppendChild(xdoc.CreateElement("Name"));
                newNode.InnerText = "Simulations";

                XmlNode rootNode = doc.DocumentElement;     // get first folder
                this.AddFoldersAndSimulations(rootNode, xdocNode);
                this.AddDataStore(xdocNode);                     // each file must contain a DataStore

                // write to temporary xfile
                StreamWriter xmlWriter = new StreamWriter(xfile);
                xmlWriter.Write(XmlUtilities.FormattedXML(xdoc.OuterXml));
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
        /// under 'shared' folders e.g. wheat validation.
        /// </summary>
        /// <param name="systemNode">The root system node</param>
        /// <param name="destParent">The new destination node</param>
        private void AddFoldersAndSimulations(XmlNode systemNode, XmlNode destParent)
        {
            XmlNode child = systemNode.FirstChild;
            while (child != null)
            {
                if (child.Name == "simulation")
                    this.AddComponent(child, ref destParent);
                else if (child.Name == "folder")
                {
                    XmlNode newFolder = this.AddCompNode(destParent, "Folder", XmlUtilities.NameAttr(child));
                    this.AddFoldersAndSimulations(child, newFolder);
                }
                child = child.NextSibling;
            }
        }

        /// <summary>
        /// Iterate through the child nodes
        /// </summary>
        /// <param name="systemNode">The root system node</param>
        /// <param name="destParent">The new destination node</param>
        private void AddChildComponents(XmlNode systemNode, XmlNode destParent)
        {
            XmlNode child = systemNode.FirstChild;
            while (child != null)
            {
                this.AddComponent(child, ref destParent);
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
                    XmlNode newSim = this.AddCompNode(destParent, "Simulation", XmlUtilities.NameAttr(compNode));
                    this.AddChildComponents(compNode, newSim);
                }
                else if (compNode.Name == "folder")
                {
                    XmlNode newFolder = this.AddCompNode(destParent, "Folder", XmlUtilities.NameAttr(compNode));
                    this.AddChildComponents(compNode, newFolder);
                }
                else if (compNode.Name == "clock")
                {
                    newNode = this.ImportClock(compNode, destParent, newNode);
                }
                else if (compNode.Name == "metfile")
                {
                    newNode = this.ImportMetFile(compNode, destParent, newNode);
                }
                else if (compNode.Name == "micromet")
                {
                    newNode = this.ImportMicromet(compNode, destParent, newNode);
                }
                else if (compNode.Name == "manager")
                {
                    newNode = this.ImportManager(compNode, destParent, newNode);
                }
                else if (compNode.Name == "manager2")
                {
                    newNode = this.ImportManager2(compNode, destParent, newNode);
                }
                else if (compNode.Name == "outputfile")
                {
                    newNode = this.ImportOutputFile(compNode, destParent, newNode);
                }
                else if (compNode.Name == "operations")
                {
                    newNode = this.ImportOperations(compNode, destParent, newNode);
                }
                else if (compNode.Name == "summaryfile")
                {
                    newNode = this.AddCompNode(destParent, "Summary", XmlUtilities.NameAttr(compNode));
                    XmlNode childNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("html"));
                    childNode.InnerText = "true";
                }
                else if (compNode.Name.ToLower() == "soil")
                {
                    newNode = this.ImportSoil(compNode, destParent, newNode);
                }
                else if (compNode.Name.ToLower() == "sample")
                {
                    newNode = this.ImportSample(compNode, destParent, newNode);
                }
                else if (compNode.Name.ToLower() == "water")
                {
                    newNode = this.ImportWater(compNode, destParent, newNode);
                }
                else if (compNode.Name.ToLower() == "soilwater")
                {
                    newNode = this.ImportSoilWater(compNode, destParent, newNode);
                    this.soilWaterExists = newNode != null;
                    this.AddCompNode(destParent, "SoilNitrogen", "SoilNitrogen");

                    // may need to copy more details for SoilNitrogen
                }
                else if (compNode.Name == "InitialWater")
                {
                    newNode = this.ImportInitialWater(compNode, destParent, newNode);
                }
                else if (compNode.Name == "SoilOrganicMatter")
                {
                    newNode = this.ImportSOM(compNode, destParent, newNode);
                }
                else if (compNode.Name == "Analysis")
                {
                    newNode = this.ImportAnalysis(compNode, destParent, newNode);
                }
                else if (compNode.Name == "SoilCrop")
                {
                    newNode = this.ImportSoilCrop(compNode, destParent, newNode);
                }
                else if (compNode.Name == "area")
                {
                    newNode = this.AddCompNode(destParent, "Zone", XmlUtilities.NameAttr(compNode));
                    XmlNode newPaddockNode = newNode;

                    string area = this.GetInnerText(compNode, "paddock_area");
                    if (area == string.Empty)
                    {
                        XmlUtilities.SetValue(compNode, "paddock_area", "1.0");
                    }
                    this.CopyNodeAndValue(compNode, newPaddockNode, "paddock_area", "Area", true);
                    this.surfOMExists = false;
                    this.soilWaterExists = false;
                    this.microClimateExists = false;

                    // copy all the children in this paddock
                    this.AddChildComponents(compNode, newPaddockNode);

                    // if it contains a soilwater then
                    if (this.soilWaterExists && !this.surfOMExists)   
                    {
                        Console.WriteLine("Added SurfaceOM to " + XmlUtilities.FullPathUsingName(newPaddockNode));
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
                    if (!this.microClimateExists)
                    {
                        newNode = this.ImportMicromet(null, newPaddockNode, newNode);    // create a new node from no source
                    }
                }
                else if (compNode.Name == "surfaceom")
                {
                    newNode = this.ImportSurfaceOM(compNode, destParent, newNode);
                    this.surfOMExists = newNode != null;
                }
                else if (compNode.Name == "memo")
                {
                    newNode = this.AddCompNode(destParent, "Memo", XmlUtilities.NameAttr(compNode));
                    XmlNode memoTextNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("MemoText"));
                    memoTextNode.InnerText = compNode.InnerText;
                }
                else if (compNode.Name == "Graph")
                {
                    newNode = this.ImportGraph(compNode, destParent, newNode);
                }
                else if (StringUtilities.IndexOfCaseInsensitive(cropNames, compNode.Name) != -1)
                {
                    this.ImportPlant(compNode, destParent, newNode);
                }
                else
                {
                    // Do nothing.
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
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="plantNode">The new created node</param>
        /// <returns>The newly created node</returns>
        private XmlNode ImportPlant(XmlNode compNode, XmlNode destParent, XmlNode plantNode)
        {
            string name = XmlUtilities.NameAttr(compNode);

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

            XmlUtilities.SetValue(plantNode, "ResourceName", StringUtilities.CamelCase(name));

            XmlNode cropTypeNode = plantNode.AppendChild(plantNode.OwnerDocument.CreateElement("CropType"));
            cropTypeNode.InnerText = name;

            return plantNode;
        }

        /// <summary>
        /// Unused function
        /// </summary>
        /// <param name="plantNode">The new plant node</param>
        /// <param name="model">The model object</param>
        private void AddLinkedObjects(XmlNode plantNode, object model)
        {
            // Go looking for [Link]s
            foreach (FieldInfo field in ReflectionUtilities.GetAllFields(
                                                                        model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = ReflectionUtilities.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null)
                {
                    if (!field.FieldType.IsAbstract && !link.IsOptional)
                    {
                        // get the type here and add a tag?
                        XmlNode newNode = this.AddCompNode(plantNode, field.FieldType.Name, field.Name);

                        object obj = Activator.CreateInstance(field.FieldType);
                        this.AddLinkedObjects(newNode, obj);
                    }
                } 
            } 
        }

        /// <summary>
        /// Import a micromet component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new micromet node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportMicromet(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.MicroClimate mymicro = new Models.MicroClimate();
            string newName = "MicroClimate";

            if (compNode != null)
            {
                mymicro.soil_albedo = this.GetChildDouble(compNode, "soilalbedo", 0);
                mymicro.a_interception = this.GetChildDouble(compNode, "a_interception", 0);
                mymicro.b_interception = this.GetChildDouble(compNode, "b_interception", 0);
                mymicro.c_interception = this.GetChildDouble(compNode, "c_interception", 0);
                mymicro.d_interception = this.GetChildDouble(compNode, "d_interception", 0);
                newName = XmlUtilities.NameAttr(compNode);
            }
            newNode = ImportObject(destParent, newNode, mymicro, newName);

            this.microClimateExists = true; // has been added

            return newNode;
        }     

        /// <summary>
        /// Import the soil crop object
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The node SoilCrop node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportSoilCrop(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Soils.SoilCrop mycrop = new Models.Soils.SoilCrop();

            mycrop.LL = this.GetChildDoubles(compNode, "LL", 0);
            mycrop.KL = this.GetChildDoubles(compNode, "KL", 0);
            mycrop.XF = this.GetChildDoubles(compNode, "XF", 0);

            string name = XmlUtilities.NameAttr(compNode);
            if (name == "SoilCrop")
                name = XmlUtilities.Value(compNode, "Name");
            
            newNode = ImportObject(destParent, newNode, mycrop, name);

            return newNode;
        }

        /// <summary>
        /// Import surfaceom
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new surfaceom node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportSurfaceOM(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.SurfaceOM.SurfaceOrganicMatter mysom = new Models.SurfaceOM.SurfaceOrganicMatter();

            mysom.PoolName = this.GetInnerText(compNode, "PoolName");
            mysom.type = this.GetInnerText(compNode, "type");
            mysom.mass = this.GetInnerText(compNode, "mass");
            mysom.cnr = this.GetInnerText(compNode, "cnr");
            mysom.cpr = this.GetInnerText(compNode, "cpr");
            mysom.standing_fraction = this.GetInnerText(compNode, "standing_fraction");

            newNode = ImportObject(destParent, newNode, mysom, XmlUtilities.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// Import a graph object 
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new graph node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportGraph(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Graph.Graph mygraph = new Models.Graph.Graph();

            // set any values here
            newNode = ImportObject(destParent, newNode, mygraph, XmlUtilities.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// Import the initial water component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new initial water node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportInitialWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Soils.InitialWater mywater = new Models.Soils.InitialWater();

            mywater.FractionFull = this.GetChildDouble(compNode, "FractionFull", 1.0);
            string method = this.GetInnerText(compNode, "PercentMethod");
            if (method.Length < 1)
                mywater.PercentMethod = Models.Soils.InitialWater.PercentMethodEnum.FilledFromTop;
            else
            {
                Models.Soils.InitialWater.PercentMethodEnum methodValue = (Models.Soils.InitialWater.PercentMethodEnum)Enum.Parse(typeof(Models.Soils.InitialWater.PercentMethodEnum), method);
                if (Enum.IsDefined(typeof(Models.Soils.InitialWater.PercentMethodEnum), methodValue))
                    mywater.PercentMethod = methodValue;
            }

            string name = XmlUtilities.NameAttr(compNode);
            if (name == "InitialWater" && XmlUtilities.Value(compNode, "Name") != "")
                name = XmlUtilities.Value(compNode, "Name");

            newNode = ImportObject(destParent, newNode, mywater, name);

            return newNode;
        }

        /// <summary>
        /// Import an Analysis component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new Analysis node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportAnalysis(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = this.AddCompNode(destParent, "Analysis", XmlUtilities.NameAttr(compNode));

            XmlNode childNode;

            // thickness array
            childNode = XmlUtilities.Find(compNode, "Thickness");
            this.CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            childNode = XmlUtilities.Find(compNode, "PH");
            this.CopyNodeAndValueArray(childNode, newNode, "PH", "PH");

            return newNode;
        }

        /// <summary>
        /// Import the surface organic matter component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new surfaceom node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportSOM(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = this.AddCompNode(destParent, "SoilOrganicMatter", XmlUtilities.NameAttr(compNode));

            XmlNode childNode;
            this.CopyNodeAndValue(compNode, newNode, "RootCN", "RootCN", false);
            this.CopyNodeAndValue(compNode, newNode, "RootWt", "RootWt", false);
            this.CopyNodeAndValue(compNode, newNode, "SoilCN", "SoilCN", false);
            this.CopyNodeAndValue(compNode, newNode, "EnrACoeff", "EnrACoeff", false);
            this.CopyNodeAndValue(compNode, newNode, "EnrBCoeff", "EnrBCoeff", false);

            // thickness array
            childNode = XmlUtilities.Find(compNode, "Thickness");
            this.CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");
            childNode = XmlUtilities.Find(compNode, "OC");
            this.CopyNodeAndValueArray(childNode, newNode, "OC", "OC");
            childNode = XmlUtilities.Find(compNode, "FBiom");
            this.CopyNodeAndValueArray(childNode, newNode, "FBiom", "FBiom");
            childNode = XmlUtilities.Find(compNode, "FInert");
            this.CopyNodeAndValueArray(childNode, newNode, "FInert", "FInert");
            
            return newNode;
        }

        /// <summary>
        /// Import the soil water component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">New soil water node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportSoilWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Soils.SoilWater mysoilwater = new Models.Soils.SoilWater();

            // store some of the values found in the imported document
            mysoilwater.SummerCona = this.GetChildDouble(compNode, "SummerCona", 0.0);
            mysoilwater.SummerU = this.GetChildDouble(compNode, "SummerU", 0.0);
            mysoilwater.WinterCona = this.GetChildDouble(compNode, "WinterCona", 0.0);
            mysoilwater.WinterU = this.GetChildDouble(compNode, "WinterU", 0.0);
            mysoilwater.DiffusConst = this.GetChildDouble(compNode, "DiffusConst", 0.0);
            mysoilwater.DiffusSlope = this.GetChildDouble(compNode, "DiffusSlope", 0.0);
            mysoilwater.Salb = this.GetChildDouble(compNode, "Salb", 0.0);
            mysoilwater.CN2Bare = this.GetChildDouble(compNode, "CN2Bare", 0.0);
            mysoilwater.CNRed = this.GetChildDouble(compNode, "CNRed", 0.0);
            mysoilwater.CNCov = this.GetChildDouble(compNode, "CNCov", 0.0);
            mysoilwater.SummerDate = this.GetInnerText(compNode, "SummerDate");
            mysoilwater.WinterDate = this.GetInnerText(compNode, "WinterDate");
            mysoilwater.Thickness = this.GetChildDoubles(compNode, "Thickness", 0);
            mysoilwater.SWCON = this.GetChildDoubles(compNode, "SWCON", 0);

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, mysoilwater, XmlUtilities.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// Generate xml for the new object and import the serialised xml into the new document
        /// </summary>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new node</param>
        /// <param name="newObject">The object to import into the document</param>
        /// <param name="objName">Name to set the new object</param>
        /// <returns>The new created node</returns>
        private static XmlNode ImportObject(XmlNode destParent, XmlNode newNode, Model newObject, string objName)
        {
            newObject.Name = objName;
            string newObjxml = XmlUtilities.Serialise(newObject, false);
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(newObjxml);
            newNode = destParent.OwnerDocument.ImportNode(xdoc.DocumentElement, true);
            newNode = destParent.AppendChild(newNode);

            return newNode;
        }

        /// <summary>
        /// Import the water component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new water node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportWater(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = this.AddCompNode(destParent, "Water", XmlUtilities.NameAttr(compNode));

            XmlNode childNode;

            // thickness array
            childNode = XmlUtilities.Find(compNode, "Thickness");
            this.CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");

            // NO3 array
            childNode = XmlUtilities.Find(compNode, "BD");
            this.CopyNodeAndValueArray(childNode, newNode, "BD", "BD");

            // NH4 array
            childNode = XmlUtilities.Find(compNode, "AirDry");
            this.CopyNodeAndValueArray(childNode, newNode, "AirDry", "AirDry");

            // SW array
            childNode = XmlUtilities.Find(compNode, "LL15");
            this.CopyNodeAndValueArray(childNode, newNode, "LL15", "LL15");
            childNode = XmlUtilities.Find(compNode, "DUL");
            this.CopyNodeAndValueArray(childNode, newNode, "DUL", "DUL");
            childNode = XmlUtilities.Find(compNode, "SAT");
            this.CopyNodeAndValueArray(childNode, newNode, "SAT", "SAT");

            this.AddChildComponents(compNode, newNode);

            return newNode;
        }

        /// <summary>
        /// Import the Sample component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new Sample node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportSample(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            string name = XmlUtilities.NameAttr(compNode);
            if (XmlUtilities.Value(compNode, "Name") != "")
                name = XmlUtilities.Value(compNode, "Name");

            newNode = this.AddCompNode(destParent, "Sample", XmlUtilities.NameAttr(compNode));

            string date = this.GetInnerText(compNode, "Date");
            XmlNode dateNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Date"));
            dateNode.InnerText = DateUtilities.DMYtoISO(date);
            XmlNode childNode;

            // thickness array
            childNode = XmlUtilities.Find(compNode, "Thickness");
            this.CopyNodeAndValueArray(childNode, newNode, "Thickness", "Thickness");

            // NO3 array
            childNode = XmlUtilities.Find(compNode, "NO3");
            this.CopyNodeAndValueArray(childNode, newNode, "NO3", "NO3");

            // NH4 array
            childNode = XmlUtilities.Find(compNode, "NH4");
            this.CopyNodeAndValueArray(childNode, newNode, "NH4", "NH4");

            // SW array
            childNode = XmlUtilities.Find(compNode, "SW");
            this.CopyNodeAndValueArray(childNode, newNode, "SW", "SW");

            this.CopyNodeAndValue(compNode, newNode, "NO3Units", "NO3Units", false);
            this.CopyNodeAndValue(compNode, newNode, "NH4Units", "NH4Units", false);

            return newNode;
        }

        /// <summary>
        /// Import the soil component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new soil node</param>
        /// <returns>The new node</returns>
        public XmlNode ImportSoil(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            //// Models.Soils.Soil mysoil = new Models.Soils.Soil();

            //// mysoil.SoilType = GetInnerText(compNode, "SoilType");

            // import this object into the new xml document
            //// newNode = ImportObject(destParent, newNode, mysoil, XmlUtilities.Name(compNode));

            string name = XmlUtilities.Attribute(compNode, "name");
            if (name == "")
                name = XmlUtilities.Value(compNode, "Name");
            if (name == "")
                name = compNode.Name;
            newNode = this.AddCompNode(destParent, "Soil", name);

            this.CopyNodeAndValue(compNode, newNode, "NO3Units", "NO3Units", false);
            this.CopyNodeAndValue(compNode, newNode, "NH4Units", "NH4Units", false);
            this.CopyNodeAndValue(compNode, newNode, "SWUnits", "SWUnits", false);
            this.CopyNodeAndValue(compNode, newNode, "OCUnits", "OCUnits", false);
            this.CopyNodeAndValue(compNode, newNode, "PHUnits", "PHUnits", false);

            this.CopyNodeAndValue(compNode, newNode, "RecordNumber", "RecordNumber", false);
            this.CopyNodeAndValue(compNode, newNode, "ASCOrder", "ASCOrder", false);
            this.CopyNodeAndValue(compNode, newNode, "ASCSubOrder", "ASCSubOrder", false);
            this.CopyNodeAndValue(compNode, newNode, "SoilType", "SoilType", false);
            this.CopyNodeAndValue(compNode, newNode, "Site", "Site", false);
            this.CopyNodeAndValue(compNode, newNode, "NearestTown", "NearestTown", false);
            this.CopyNodeAndValue(compNode, newNode, "Region", "Region", false);
            this.CopyNodeAndValue(compNode, newNode, "State", "State", false);
            this.CopyNodeAndValue(compNode, newNode, "Country", "Country", false);
            this.CopyNodeAndValue(compNode, newNode, "NaturalVegetation", "NaturalVegetation", false);
            this.CopyNodeAndValue(compNode, newNode, "ApsoilNumber", "ApsoilNumber", false);
            this.CopyNodeAndValue(compNode, newNode, "Latitude", "Latitude", false);
            this.CopyNodeAndValue(compNode, newNode, "Longitude", "Longitude", false);
            this.CopyNodeAndValue(compNode, newNode, "LocationAccuracy", "LocationAccuracy", false);
            this.CopyNodeAndValue(compNode, newNode, "DataSource", "DataSource", false);
            this.CopyNodeAndValue(compNode, newNode, "Comments", "Comments", false);

            this.AddChildComponents(compNode, newNode);

            return newNode;
        }

        /// <summary>
        /// Import the output component
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportOutputFile(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Report.Report myreport = new Models.Report.Report();

            // compNode/variables array
            List<XmlNode> nodes = new List<XmlNode>();
            XmlUtilities.FindAllRecursively(compNode, "variable", ref nodes);
            myreport.VariableNames = new string[nodes.Count];
            int i = 0;
            foreach (XmlNode var in nodes)
            {
                if (var.InnerText.Contains("yyyy")) 
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
            XmlUtilities.FindAllRecursively(compNode, "event", ref nodes);
            myreport.EventNames = new string[nodes.Count];
            i = 0;
            foreach (XmlNode theEvent in nodes)
            {
                if (string.Compare(theEvent.InnerText, "end_day", true) == 0)
                {
                    myreport.EventNames[i] = "[Clock].DoReport";
                }
                else if (string.Compare(theEvent.InnerText, "daily", true) == 0)
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
            newNode = ImportObject(destParent, newNode, myreport, XmlUtilities.NameAttr(compNode));

            return newNode;
        }

        /// <summary>
        /// Import a Manager(1) component.
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new component node</param>
        /// <returns>The new node</returns>
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
            XmlUtilities.FindAllRecursivelyByType(compNode, "script", ref nodes);
            foreach (XmlNode script in nodes)
            {
                // find the event
                XmlNode eventNode = XmlUtilities.Find(script, "event");

                // find the text
                XmlNode textNode = XmlUtilities.Find(script, "text");
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
            newNode = ImportObject(destParent, newNode, mymanager, XmlUtilities.NameAttr(compNode));
            
            // some Manager components have Memo children. For ApsimX the import
            // will just put them as the next sibling of the Manager rather
            // than as a child of the Manager.
            destParent = this.ImportManagerMemos(compNode, destParent);
            
            return newNode;
        }

        /// <summary>
        /// Import any memo children.
        /// </summary>
        /// <param name="compNode">The Manager component node</param>
        /// <param name="destParent">The parent (folder) node of the Manager.</param>
        /// <returns>The new node</returns>
        private XmlNode ImportManagerMemos(XmlNode compNode, XmlNode destParent)
        {
            XmlNode child = compNode.FirstChild;
            while (child != null)
            {
                if (child.Name == "memo")
                {
                    this.AddComponent(child, ref destParent);
                }
                child = child.NextSibling;
            }
            return destParent;
        }

        /// <summary>
        /// Import a Manager(2) component.
        /// </summary>
        /// <param name="compNode">The Manager component node being imported</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new Manager node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportManager2(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Manager mymanager = new Models.Manager();

            // copy code here

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, mymanager, XmlUtilities.NameAttr(compNode));

            // some Manager components have Memo children. For ApsimX the import
            // will just put them as the next sibling of the Manager rather
            // than as a child of the Manager.
            destParent = this.ImportManagerMemos(compNode, destParent);

            return newNode;
        }

        /// <summary>
        /// Import the weather object
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new weather component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportMetFile(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = this.AddCompNode(destParent, "Weather", "Weather");

            // compNode/filename value
            XmlNode anode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("FileName"));
            string metfilepath = PathUtilities.OSFilePath(this.GetInnerText(compNode, "filename"));

            anode.InnerText = metfilepath.Replace("%apsim%", this.ApsimPath);

            return newNode;
        }

        /// <summary>
        /// Import Clock
        /// </summary>
        /// <param name="compNode">The node being imported from the apsim file xml</param>
        /// <param name="destParent">The new parent xml node</param>
        /// <param name="newNode">The new clock component node</param>
        /// <returns>The new component node</returns>
        private XmlNode ImportClock(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            Models.Clock myclock = new Models.Clock();

            string startDate = this.GetInnerText(compNode, "start_date");
            string endDate = this.GetInnerText(compNode, "end_date");
            myclock.StartDate = DateUtilities.DMYtoDate(startDate);
            this.startDate = myclock.StartDate;
            myclock.EndDate   = DateUtilities.DMYtoDate(endDate);

            // import this object into the new xml document
            newNode = ImportObject(destParent, newNode, myclock, XmlUtilities.NameAttr(compNode));
 
            return newNode;
        }

        /// <summary>
        /// Import operations
        /// </summary>
        /// <param name="compNode">The Operations node being imported</param>
        /// <param name="destParent">Destination parent node that the new child is added to</param>
        /// <param name="newNode">The new clock node</param>
        /// <returns>The new node</returns>
        private XmlNode ImportOperations(XmlNode compNode, XmlNode destParent, XmlNode newNode)
        {
            newNode = this.AddCompNode(destParent, "Operations", XmlUtilities.NameAttr(compNode));

            XmlNode childNode;
            
            List<XmlNode> nodes = new List<XmlNode>();
            XmlUtilities.FindAllRecursively(compNode, "operation", ref nodes);
            foreach (XmlNode oper in nodes)
            {
                XmlNode operationNode = newNode.AppendChild(destParent.OwnerDocument.CreateElement("Operation"));
                XmlNode dateNode = operationNode.AppendChild(destParent.OwnerDocument.CreateElement("Date"));

                string childText = string.Empty;
                childNode = XmlUtilities.Find(oper, "date");
                DateTime when;
                if (childNode != null && DateTime.TryParse(childNode.InnerText, out when))
                    childText = when.ToString("yyyy-MM-dd");
                else if (childNode != null && childNode.InnerText != string.Empty)
                {
                    childText = DateUtilities.DMYtoISO(childNode.InnerText);
                    if (childText == "0001-01-01")
                    {
                        childText = DateUtilities.GetDate(childNode.InnerText, this.startDate).ToString("yyyy-MM-dd");
                    }
                }
                else
                    childText = "0001-01-01";
                dateNode.InnerText = childText;
                
                XmlNode actionNode = operationNode.AppendChild(destParent.OwnerDocument.CreateElement("Action"));

                childText = " ";
                childNode = XmlUtilities.Find(oper, "action");
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
                        if ((string.Compare(operationOn, "fertiliser", true) == 0) && ((childText.IndexOf(" apply") > 0) || (childText.IndexOf(" Apply") > 0)))
                        {
                            string amount = "0", type = string.Empty, depth = "0";

                            this.FindTokenValue("amount", childText, ref amount);
                            this.FindTokenValue("type", childText, ref type);

                            string newtype;
                            this.fertilisers.TryGetValue(type, out newtype);
                            type = "Fertiliser.Types." + newtype;
                            this.FindTokenValue("depth", childText, ref depth);

                            childText = string.Format("Fertiliser.Apply({0}, {1}, {2});", amount, type, depth);
                        }
                        else
                        {
                            if ((string.Compare(operationOn, "irrigation", true) == 0) && ((childText.IndexOf(" apply") > 0) || (childText.IndexOf(" Apply") > 0)))
                            {
                                string amount = "0";

                                this.FindTokenValue("amount", childText, ref amount);

                                childText = string.Format("Irrigation.Apply({0});", amount);
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
                value = string.Empty;
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
        /// <param name="srcParentNode">The parent node of the array nodes</param>
        /// <param name="destParentNode">Destination parent node that the new child is added to</param>
        /// <param name="srcName">Name of the source array</param>
        /// <param name="destName">Name of the destination array</param>
        private void CopyNodeAndValueArray(XmlNode srcParentNode, XmlNode destParentNode, string srcName, string destName)
        {
            if (srcParentNode != null)
            {
                XmlNode childNode = destParentNode.AppendChild(destParentNode.OwnerDocument.CreateElement(destName));

                XmlNode child = srcParentNode.FirstChild;
                while (child != null)
                {
                    this.CopyNodeAndValue(child, childNode, child.Name);
                    child = child.NextSibling;
                }
            }
        }

        /// <summary>
        /// Copy the <code>srcNode</code> using <code>destName</code>.
        /// </summary>
        /// <param name="srcNode">The source node</param>
        /// <param name="destParentNode">Destination parent node that the new child is added to</param>
        /// <param name="destName">Name of the destination node that will be appended</param>
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
        /// <param name="srcParentNode">Parent node of the source node</param>
        /// <param name="destParentNode">Destination parent node that the new child is added to</param>
        /// <param name="srcName">Name of the source node</param>
        /// <param name="destName">Name of the destination node created</param>
        /// <param name="forceCreate">Always create the node even if the source is not found</param>
        private void CopyNodeAndValue(XmlNode srcParentNode, XmlNode destParentNode, string srcName, string destName, bool forceCreate)
        {
            if (srcParentNode != null)
            {
                XmlNode srcChildNode = XmlUtilities.Find(srcParentNode, srcName);
                if (forceCreate || (srcChildNode != null))
                {
                    XmlNode childNode = destParentNode.AppendChild(destParentNode.OwnerDocument.CreateElement(destName));
                    if (srcChildNode != null)
                    {
                        childNode.InnerText = srcChildNode.InnerText;
                    }
                    else
                    {
                        childNode.InnerText = string.Empty;
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
        private XmlNode AddCompNode(XmlNode parentNode, string elementName, string name)
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
            this.AddCompNode(newSim, "DataStore", "DataStore");
        }

        /// <summary>
        /// Get the array of doubles for nodePath element
        /// </summary>
        /// <param name="parentNode">Parent node to search</param>
        /// <param name="nodePath">The child node being sought</param>
        /// <param name="defValue">Use this as the default if an invalid value is found</param>
        /// <returns>The array of doubles</returns>
        private double[] GetChildDoubles(XmlNode parentNode, string nodePath, double defValue)
        {
            double[] values = null;

            XmlNode srcNode = XmlUtilities.Find(parentNode, nodePath);
            if (srcNode != null)
            {
                List<XmlNode> nodes = XmlUtilities.ChildNodesByName(srcNode, "double");
                values = new double[nodes.Count];

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode child = nodes[i];
                    if (child.InnerText.Length > 0)
                        values[i] = Convert.ToDouble(child.InnerText, CultureInfo.InvariantCulture);
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
            XmlNode srcNode = XmlUtilities.Find(parentNode, nodePath);
            if ((srcNode != null) && (srcNode.InnerText.Length > 0))
                return Convert.ToDouble(srcNode.InnerText, CultureInfo.InvariantCulture);
            else
                return defValue;
        }

        /// <summary>
        /// Get the inner text for the node
        /// </summary>
        /// <param name="parentNode">Parent node to search</param>
        /// <param name="nodePath">The child node being sought</param>
        /// <returns>The inner text for the node</returns>
        private string GetInnerText(XmlNode parentNode, string nodePath)
        {
            XmlNode srcNode = XmlUtilities.Find(parentNode, nodePath);
            if (srcNode != null)
                return srcNode.InnerText;
            else
                return string.Empty;
        }

        /// <summary>
        /// Get the text for the attribute
        /// </summary>
        /// <param name="node">Xml node</param>
        /// <param name="attr">Attribute name</param>
        /// <returns>The value from the attribute</returns>
        private string AttributeText(XmlNode node, string attr)
        {
            if (node.Attributes[attr] != null)
                return node.Attributes[attr].Value;
            else
                return string.Empty;
        }
    }
}
