// -----------------------------------------------------------------------
// <copyright file="ConvertSoilNode.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.OldAPSIM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ConvertSoilNode
    {
        /// <summary>Upgrades the specified node from old soil format to new.</summary>
        /// <param name="node">The node.</param>
        public static XmlNode Upgrade(XmlNode node)
        {
            ToVersion20(node);
            ToVersion21(node);
            ToVersion22(node);
            ToVersion24(node);
            ToVersion25(node);
            ToVersion27(node);
            ToVersion32(node);
            node = ToVersion33(node);
            ToVersion35(node);
            return node;
        }

        /// <summary>Convert from old soil XML format to new soil XML format</summary>
        /// <param name="Node">The node to convert</param>
        private static void ToVersion20(XmlNode Node)
        {
            if (Node.Name.ToLower() == "soil")
            {
                // If there is a <Phosphorus> node then get rid of it. It is old and not used.
                XmlNode OldPhosphorusNode = XmlUtilities.FindByType(Node, "Phosphorus");
                if (OldPhosphorusNode != null)
                    Node.RemoveChild(OldPhosphorusNode);

                XmlNode ProfileNode = XmlUtilities.Find(Node, "profile");
                if (ProfileNode != null)
                {
                    XmlNode WaterNode = Node.AppendChild(Node.OwnerDocument.CreateElement("Water"));
                    XmlNode SoilWatNode = Node.AppendChild(Node.OwnerDocument.CreateElement("SoilWat"));
                    XmlNode SOMNode = Node.AppendChild(Node.OwnerDocument.CreateElement("SoilOrganicMatter"));
                    XmlNode LABNode = Node.AppendChild(Node.OwnerDocument.CreateElement("Lab"));

                    XmlNode CountryNode = AnnotateNode(Node, "Country", "", "");
                    Node.InsertBefore(CountryNode, Node.FirstChild);

                    AnnotateNode(Node, "State", "", "");
                    AnnotateNode(Node, "Region", "", "");
                    AnnotateNode(Node, "NearestTown", "", "Nearest town");
                    AnnotateNode(Node, "Site", "", "");
                    AnnotateNode(Node, "ApsoilNumber", "", "Apsoil number");
                    AnnotateNode(Node, "SoilType", "", "Classification");
                    AnnotateNode(Node, "Latitude", "", "Latitude (WGS84)");
                    AnnotateNode(Node, "Langitude", "", "Longitude (WGS84)");
                    AnnotateNode(Node, "LocationAccuracy", "", "Location accuracy");
                    AnnotateNode(Node, "NaturalVegetation", "", "Natural vegetation");
                    AnnotateNode(Node, "DataSource", "multiedit", "Data source");
                    AnnotateNode(Node, "Comment", "multiedit", "Comments");

                    XmlNode ConaNode = XmlUtilities.Find(Node, "CONA");
                    if (ConaNode != null)
                    {
                        XmlUtilities.SetValue(SoilWatNode, "SummerCona", ConaNode.InnerText);
                        XmlUtilities.SetValue(SoilWatNode, "WinterCona", ConaNode.InnerText);
                        Node.RemoveChild(ConaNode);
                        XmlNode UNode = XmlUtilities.Find(Node, "U");
                        if (UNode != null)
                        {
                            XmlUtilities.SetValue(SoilWatNode, "SummerU", UNode.InnerText);
                            XmlUtilities.SetValue(SoilWatNode, "WinterU", UNode.InnerText);
                            Node.RemoveChild(UNode);
                        }
                    }
                    else
                    {
                        MoveSoilNode(Node, "SummerCona", SoilWatNode);
                        MoveSoilNode(Node, "SummerU", SoilWatNode);
                        MoveSoilNode(Node, "SummerDate", SoilWatNode);
                        MoveSoilNode(Node, "WinterCona", SoilWatNode);
                        MoveSoilNode(Node, "WinterU", SoilWatNode);
                        MoveSoilNode(Node, "WinterDate", SoilWatNode);
                    }
                    if (XmlUtilities.Value(SoilWatNode, "SummerDate") == "")
                        XmlUtilities.SetValue(SoilWatNode, "SummerDate", "1-Nov");
                    if (XmlUtilities.Value(SoilWatNode, "WinterDate") == "")
                        XmlUtilities.SetValue(SoilWatNode, "WinterDate", "1-Apr");

                    MoveSoilNode(Node, "DiffusConst", SoilWatNode);
                    MoveSoilNode(Node, "DiffusSlope", SoilWatNode);
                    MoveSoilNode(Node, "SALB", SoilWatNode);
                    MoveSoilNode(Node, "CN2Bare", SoilWatNode);
                    MoveSoilNode(Node, "CNRed", SoilWatNode);
                    MoveSoilNode(Node, "CNCov", SoilWatNode);
                    MoveSoilNode(Node, "CNCanopyFact", SoilWatNode);
                    MoveSoilNode(Node, "DiffusConst", SoilWatNode);
                    MoveSoilNode(Node, "RootCN", SoilWatNode);
                    MoveSoilNode(Node, "RootWT", SoilWatNode);
                    MoveSoilNode(Node, "SoilCN", SoilWatNode);
                    MoveSoilNode(Node, "EnrACoeff", SoilWatNode);
                    MoveSoilNode(Node, "EnrBCoeff", SoilWatNode);
                    foreach (XmlNode LayerNode in XmlUtilities.ChildNodes(ProfileNode, "Layer"))
                    {
                        XmlNode WaterLayerNode = WaterNode.AppendChild(Node.OwnerDocument.CreateElement("Layer"));
                        XmlNode SoilWatLayerNode = SoilWatNode.AppendChild(Node.OwnerDocument.CreateElement("Layer"));
                        XmlNode SOMLayerNode = SOMNode.AppendChild(Node.OwnerDocument.CreateElement("Layer"));
                        XmlNode LABLayerNode = LABNode.AppendChild(Node.OwnerDocument.CreateElement("Layer"));

                        SetValue(WaterLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");
                        SetValue(SoilWatLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");
                        SetValue(SOMLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");
                        SetValue(LABLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");

                        SetValue(WaterLayerNode, "BD", XmlUtilities.Value(LayerNode, "BD"), "g/cc");
                        SetValue(WaterLayerNode, "AirDry", XmlUtilities.Value(LayerNode, "AirDry"), "mm/mm");
                        SetValue(WaterLayerNode, "LL15", XmlUtilities.Value(LayerNode, "LL15"), "mm/mm");
                        SetValue(WaterLayerNode, "DUL", XmlUtilities.Value(LayerNode, "DUL"), "mm/mm");
                        SetValue(WaterLayerNode, "SAT", XmlUtilities.Value(LayerNode, "SAT"), "mm/mm");
                        SetValue(WaterLayerNode, "KS", XmlUtilities.Value(LayerNode, "KS"), "mm/day");
                        foreach (XmlNode LLNode in XmlUtilities.ChildNodes(LayerNode, "ll"))
                        {
                            string CropName = XmlUtilities.NameAttr(LLNode);
                            XmlNode CropNode = XmlUtilities.Find(WaterNode, CropName);
                            if (CropNode == null)
                            {
                                CropNode = WaterNode.AppendChild(Node.OwnerDocument.CreateElement("SoilCrop"));
                                XmlUtilities.SetNameAttr(CropNode, CropName);
                            }
                            XmlNode CropLayerNode = CropNode.AppendChild(Node.OwnerDocument.CreateElement("Layer"));
                            SetValue(CropLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");
                            SetValue(CropLayerNode, "LL", LLNode.InnerText, "mm/mm");

                            if (XmlUtilities.ChildByNameAndType(LayerNode, CropName, "KL") != null)
                                SetValue(CropLayerNode, "KL", XmlUtilities.ChildByNameAndType(LayerNode, CropName, "KL").InnerText, "/day");
                            if (XmlUtilities.ChildByNameAndType(LayerNode, CropName, "XF") != null)
                                SetValue(CropLayerNode, "XF", XmlUtilities.ChildByNameAndType(LayerNode, CropName, "XF").InnerText, "0-1");
                        }

                        SetValue(SoilWatLayerNode, "SWCON", XmlUtilities.Value(LayerNode, "SWCON"), "0-1");
                        SetValue(SoilWatLayerNode, "MWCON", XmlUtilities.Value(LayerNode, "MWCON"), "0-1");

                        SetValue(SOMLayerNode, "OC", XmlUtilities.Value(LayerNode, "OC"), "Total %");
                        SetValue(SOMLayerNode, "FBiom", XmlUtilities.Value(LayerNode, "FBiom"), "0-1");
                        SetValue(SOMLayerNode, "FInert", XmlUtilities.Value(LayerNode, "FInert"), "0-1");

                        SetValue(LABLayerNode, "Rocks", XmlUtilities.Value(LayerNode, "Rocks"), "%");
                        SetValue(LABLayerNode, "Texture", XmlUtilities.Value(LayerNode, "Texture"), "");
                        SetValue(LABLayerNode, "EC", XmlUtilities.Value(LayerNode, "EC"), "1:5 dS/m");
                        SetValue(LABLayerNode, "PH", XmlUtilities.Value(LayerNode, "PH"), "1:5 water");
                        SetValue(LABLayerNode, "CL", XmlUtilities.Value(LayerNode, "CL"), "mg/kg");
                        SetValue(LABLayerNode, "Boron", XmlUtilities.Value(LayerNode, "Boron"), "mg/kg");
                        SetValue(LABLayerNode, "CEC", XmlUtilities.Value(LayerNode, "CEC"), "cmol+/kg");
                        SetValue(LABLayerNode, "Ca", XmlUtilities.Value(LayerNode, "Ca"), "cmol+/kg");
                        SetValue(LABLayerNode, "Mg", XmlUtilities.Value(LayerNode, "Mg"), "cmol+/kg");
                        SetValue(LABLayerNode, "Na", XmlUtilities.Value(LayerNode, "Na"), "cmol+/kg");
                        SetValue(LABLayerNode, "K", XmlUtilities.Value(LayerNode, "K"), "cmol+/kg");
                        SetValue(LABLayerNode, "ESP", XmlUtilities.Value(LayerNode, "ESP"), "%");
                        SetValue(LABLayerNode, "Mn", XmlUtilities.Value(LayerNode, "Mn"), "mg/kg");
                        SetValue(LABLayerNode, "Al", XmlUtilities.Value(LayerNode, "Al"), "meq/100g");
                        SetValue(LABLayerNode, "ParticleSizeSand", XmlUtilities.Value(LayerNode, "ParticleSizeSand"), "%");
                        SetValue(LABLayerNode, "ParticleSizeSilt", XmlUtilities.Value(LayerNode, "ParticleSizeSilt"), "%");
                        SetValue(LABLayerNode, "ParticleSizeClay", XmlUtilities.Value(LayerNode, "ParticleSizeClay"), "%");
                    }

                    // Move phosphorus stuff if necessary.
                    if (XmlUtilities.Value(Node, "RootCP") != "" && ProfileNode != null)
                    {
                        XmlNode PhosphorusNode = Node.AppendChild(Node.OwnerDocument.CreateElement("Phosphorus"));
                        MoveSoilNode(Node, "RootCP", PhosphorusNode);
                        MoveSoilNode(Node, "RateDissolRock", PhosphorusNode);
                        MoveSoilNode(Node, "RateLossAvail", PhosphorusNode);
                        foreach (XmlNode LayerNode in ProfileNode)
                        {
                            XmlNode PhosphorusLayerNode = PhosphorusNode.AppendChild(Node.OwnerDocument.CreateElement("Layer"));
                            SetValue(PhosphorusLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");
                            SetValue(PhosphorusLayerNode, "LabileP", XmlUtilities.Value(LayerNode, "LabileP"), "mg/kg");
                            SetValue(PhosphorusLayerNode, "BandedP", XmlUtilities.Value(LayerNode, "BandedP"), "kg/ha");
                            SetValue(PhosphorusLayerNode, "RockP", XmlUtilities.Value(LayerNode, "RockP"), "kg/ha");
                            SetValue(PhosphorusLayerNode, "Sorption", XmlUtilities.Value(LayerNode, "Sorption"), "-");
                        }
                    }

                    Node.RemoveChild(ProfileNode);
                }

                // Turn the <InitNitrogen> element into a sample node.  
                XmlNode InitNitrogenNode = XmlUtilities.Find(Node, "InitNitrogen");
                if (InitNitrogenNode != null)
                    ConvertSampleNode(Node, InitNitrogenNode, "Initial nitrogen");

                // Turn the <InitWater> element into a sample node IF it has layered values.
                XmlNode InitWaterNode = XmlUtilities.Find(Node, "InitWater");
                if (InitWaterNode != null)
                    ConvertSampleNode(Node, InitWaterNode, "Initial water");

                // Change all <SoilSample> nodes to <Sample>
                foreach (XmlNode Child in XmlUtilities.ChildNodes(Node, "SoilSample"))
                    ConvertSampleNode(Node, Child, XmlUtilities.NameAttr(Child));

                // Change any soil p nodes.
                foreach (XmlNode child in XmlUtilities.ChildNodes(Node, "soilp"))
                    Node.ParentNode.RemoveChild(child);
            }
        }

        private static void ToVersion21(XmlNode Node)
        {
            // ----------------------------------------------------------------
            // Rework the soil nodes that have shortcuts.
            // ---------------------------------------------------------------

            if (Node.Name.ToLower() == "soil" && XmlUtilities.Attribute(Node, "shortcut") != "")
            {
                string ShortCutPath = XmlUtilities.Attribute(Node, "shortcut");
                XmlNode ShortcutSourceNode = XmlUtilities.Find(Node.OwnerDocument.DocumentElement, ShortCutPath);
                if (ShortcutSourceNode != null)
                    ToVersion21(ShortcutSourceNode);  // recursion
                CreateChildWithShortcut(Node, ShortCutPath, "Water", "Water");
                CreateChildWithShortcut(Node, ShortCutPath, "SoilWat", "SoilWat");
                CreateChildWithShortcut(Node, ShortCutPath, "SoilOrganicMatter", "SoilOrganicMatter");
                CreateChildWithShortcut(Node, ShortCutPath, "Lab", "Lab");
                CreateChildWithShortcut(Node, ShortCutPath, "Initial Water", "InitWater");
                CreateChildWithShortcut(Node, ShortCutPath, "Initial Water", "Sample");
                CreateChildWithShortcut(Node, ShortCutPath, "Initial Nitrogen", "Sample");
                CreateChildWithShortcut(Node, ShortCutPath, "Phosphorus", "Phosphorus");

                // Now shortcut all crop nodes.
                XmlNode ShortcutSourceWaterNode = XmlUtilities.Find(Node.OwnerDocument.DocumentElement, ShortCutPath + "/Water");
                XmlNode WaterNode = XmlUtilities.Find(Node, "Water");
                if (ShortcutSourceWaterNode != null && WaterNode != null)
                {
                    foreach (string CropName in XmlUtilities.ChildNames(ShortcutSourceWaterNode, "SoilCrop"))
                        CreateChildWithShortcut(WaterNode, ShortCutPath + "/Water", CropName, "SoilCrop");
                }
            }
        }

        private static void ToVersion22(XmlNode Node)
        {
            // ----------------------------------------------------------------
            // Rework the soil nodes that have shortcuts.
            // ---------------------------------------------------------------

            if (Node.Name.ToLower() == "soil" && XmlUtilities.Attribute(Node, "shortcut") == "")
            {
                XmlNode SoilWatNode = XmlUtilities.Find(Node, "SoilWat");
                if (SoilWatNode != null)
                {
                    XmlUtilities.SetValue(SoilWatNode, "Slope", "");
                    XmlUtilities.SetValue(SoilWatNode, "DischargeWidth", "");
                    XmlUtilities.SetValue(SoilWatNode, "CatchmentArea", "");
                    XmlUtilities.SetValue(SoilWatNode, "MaxPond", "");
                }
                XmlNode SoilOrganicMatterNode = XmlUtilities.Find(Node, "SoilOrganicMatter");
                if (SoilOrganicMatterNode != null)
                {
                    MoveSoilNode(Node, "SoilWat/RootCN", SoilOrganicMatterNode);
                    MoveSoilNode(Node, "SoilWat/RootWT", SoilOrganicMatterNode);
                    MoveSoilNode(Node, "SoilWat/SoilCN", SoilOrganicMatterNode);
                    MoveSoilNode(Node, "SoilWat/EnrACoeff", SoilOrganicMatterNode);
                    MoveSoilNode(Node, "SoilWat/EnrBCoeff", SoilOrganicMatterNode);
                }
            }
        }

        private static void ToVersion24(XmlNode Node)
        {
            // ----------------------------------------------------------------
            // Add SorptionCoeff property to Phosphorus nodes.
            // ---------------------------------------------------------------

            if (Node.Name.ToLower() == "soil" && XmlUtilities.Attribute(Node, "shortcut") == "")
            {
                XmlNode PhosphorusNode = XmlUtilities.Find(Node, "Phosphorus");
                if (PhosphorusNode != null && XmlUtilities.Attribute(PhosphorusNode, "shortcut") == "")
                {
                    XmlUtilities.SetValue(PhosphorusNode, "SorptionCoeff", "0.7");
                }
            }
        }

        private static void ToVersion25(XmlNode Node)
        {
            // ----------------------------------------------------------------
            // Make sure the soil nodes are complete and in the right order.
            // ----------------------------------------------------------------
            if (Node.Name.ToLower() == "soil")
            {
                if (XmlUtilities.Attribute(Node, "shortcut") != "")
                {
                    XmlNode LabChild = XmlUtilities.Find(Node, "Lab");
                    if (LabChild != null && XmlUtilities.Attribute(LabChild, "shortcut") != "")
                    {
                        string ShortCut = XmlUtilities.Attribute(LabChild, "shortcut").Replace("/Lab", "/Analysis");
                        XmlNode AnalysisChild = XmlUtilities.ChangeType(LabChild, "Analysis");
                        XmlUtilities.SetAttribute(AnalysisChild, "shortcut", ShortCut);
                    }
                }
                else
                {
                    string[] SoilProperties = {"Country", "Site", "Region", "LocalName", "SoilType",
                                   "NearestTown",
                                   "NaturalVegetation",
                                   "State",
                                   "ApsoilNumber",
                                   "Latitude",
                                   "Longitude",
                                   "LocationAccuracy",
                                   "DataSource",
                                   "Comments"};
                    SetPropertiesOrder(Node, SoilProperties, null, false);

                    // Order the nodes under <Water>
                    XmlNode WaterNode = XmlUtilities.Find(Node, "Water");
                    if (WaterNode != null)
                    {
                        string[] Variables = { "Thickness", "KS", "BD", "AirDry", "LL15", "DUL", "SAT" };
                        string[] VariableUnits = { "mm", "mm/day", "g/cc", "mm/mm", "mm/mm", "mm/mm", "mm/mm" };
                        SetLayeredOrder(WaterNode, Variables, VariableUnits);

                        // Order the variables in <SoilCrop>
                        string[] CropVariables = { "Thickness", "LL", "KL", "XF" };
                        string[] CropUnits = { "mm", "mm/mm", "/day", "0-1" };

                        foreach (XmlNode CropNode in XmlUtilities.ChildNodes(WaterNode, "SoilCrop"))
                        {
                            SetLayeredOrder(CropNode, CropVariables, CropUnits);
                        }
                    }

                    // Order the nodes under <SoilWat>
                    XmlNode SoilWatNode = XmlUtilities.Find(Node, "SoilWat");
                    if (SoilWatNode != null)
                    {
                        string[] Properties = { "SummerCona", "SummerU", "SummerDate", "WinterCona",
                                       "WinterU", "WinterDate", "DiffusConst", "DiffusSlope",
                                       "Salb", "Cn2Bare", "CnRed", "CnCov", "CnCanopyFact",
                                       "Slope", "DischargeWidth", "CatchmentArea", "MaxPond"};
                        SetPropertiesOrder(SoilWatNode, Properties, null, false);
                        string[] Variables = { "Thickness", "SWCON", "MWCON", "KLAT" };
                        string[] Units = { "mm", "0-1", "0-1", "mm/d" };
                        SetLayeredOrder(SoilWatNode, Variables, Units);
                    }

                    // Order the nodes under <SoilOrganicMatter>
                    XmlNode SOMNode = XmlUtilities.Find(Node, "SoilOrganicMatter");
                    if (SOMNode != null)
                    {
                        string[] Properties = { "RootCN", "RootWt", "SoilCn", "EnrACoeff", "EnrBCoeff" };
                        SetPropertiesOrder(SOMNode, Properties, null, false);

                        string[] Variables = { "Thickness", "OC", "FBiom", "FInert" };
                        string[] Units = { "mm", "Total %", "0-1", "0-1" };
                        SetLayeredOrder(SOMNode, Variables, Units);
                    }
                    // Order the nodes under <Lab>
                    XmlNode LabNode = XmlUtilities.Find(Node, "Lab");
                    if (LabNode != null)
                    {
                        string[] Variables = { "Thickness", "Rocks", "Texture", "MunsellColour", "EC", "PH", "CL", "Boron", "CEC",
                                      "Ca", "Mg", "Na", "K", "ESP", "Mn", "Al",
                                      "ParticleSizeSand", "ParticleSizeSilt", "ParticleSizeClay"};



                        string[] Units =     { "mm", "%", "", "", "1:5 dS/m", "1:5 water", "mg/kg", "Hot water mg/kg", "cmol+/kg",
                                      "cmol+/kg", "cmol+/kg", "cmol+/kg", "cmol+/kg", "%", "mg/kg", "cmol+/kg",
                                      "%", "%", "%"};
                        SetLayeredOrder(LabNode, Variables, Units);
                        XmlUtilities.ChangeType(LabNode, "Analysis");
                    }
                    // Order the nodes under <Sample>
                    foreach (XmlNode SampleNode in XmlUtilities.ChildNodes(Node, "Sample"))
                    {
                        string[] Variables = { "Thickness", "NO3", "NH4", "SW" };
                        string[] Units = { "mm", "ppm", "ppm", "mm/mm" };
                        SetLayeredOrder(SampleNode, Variables, Units);
                    }
                    // Order the nodes under <Phosphorus>
                    XmlNode PNode = XmlUtilities.Find(Node, "Phosphorus");
                    if (PNode != null)
                    {
                        string[] Properties = { "RootCP", "RateDissolRock", "RateLossAvail", "SorptionCoeff" };
                        SetPropertiesOrder(PNode, Properties, null, false);

                        string[] Variables = { "Thickness", "LabileP", "BandedP", "RockP", "Sorption" };
                        string[] Units = { "mm", "mg/kg", "kg/ha", "kg/ha", "" };
                        SetLayeredOrder(PNode, Variables, Units);
                    }
                }
            }
        }

        private static void ToVersion27(XmlNode Node)
        {
            // ----------------------------------------------------------------
            // 1. Change Boron units from mg/kg to Hot water mg/kg
            // 2. Remove CnCanopyFact from SoilWat
            // ---------------------------------------------------------------

            if (Node.Name.ToLower() == "soil")
            {
                XmlNode BoronNode = XmlUtilities.Find(Node, "Analysis/Layer/Boron");
                if (BoronNode != null && XmlUtilities.Attribute(BoronNode, "units") == "mg/kg")
                    XmlUtilities.SetAttribute(BoronNode, "units", "Hot water mg/kg");

                XmlNode CnCanopyFactNode = XmlUtilities.Find(Node, "SoilWat/CnCanopyFact");
                if (CnCanopyFactNode != null)
                    CnCanopyFactNode.ParentNode.RemoveChild(CnCanopyFactNode);
            }
        }

        /// <summary>
        /// Make sure soil nodes have a ASC_Order and ASC_Sub-order nodes.
        /// </summary>
        private static void ToVersion32(XmlNode Node)
        {
            if (Node.Name.ToLower() == "soil")
            {
                if (XmlUtilities.FindByType(Node, "ASC_Order") == null)
                {
                    XmlNode NewNode = XmlUtilities.EnsureNodeExists(Node, "ASC_Order");
                    XmlUtilities.SetAttribute(NewNode, "description", "Australian Soil Classification Order");
                    NewNode.ParentNode.InsertBefore(NewNode, NewNode.FirstChild);
                }
                if (XmlUtilities.FindByType(Node, "ASC_Sub-order") == null)
                {
                    XmlNode NewNode = XmlUtilities.EnsureNodeExists(Node, "ASC_Sub-order");
                    XmlUtilities.SetAttribute(NewNode, "description", "Australian Soil Classification Sub-Order");
                    Node.InsertAfter(NewNode, XmlUtilities.FindByType(Node, "ASC_Order"));
                }
                XmlNode SoilType = XmlUtilities.EnsureNodeExists(Node, "SoilType");
                XmlUtilities.SetAttribute(SoilType, "description", "Soil description");
                Node.InsertAfter(SoilType, XmlUtilities.FindByType(Node, "ASC_Sub-order"));

                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "LocalName"), XmlUtilities.FindByType(Node, "SoilType"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "Site"), XmlUtilities.FindByType(Node, "LocalName"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "NearestTown"), XmlUtilities.FindByType(Node, "Site"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "Region"), XmlUtilities.FindByType(Node, "NearestTown"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "State"), XmlUtilities.FindByType(Node, "Region"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "Country"), XmlUtilities.FindByType(Node, "State"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "NaturalVegetation"), XmlUtilities.FindByType(Node, "Country"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "ApsoilNumber"), XmlUtilities.FindByType(Node, "NaturalVegetation"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "Latitude"), XmlUtilities.FindByType(Node, "ApsoilNumber"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "Longitude"), XmlUtilities.FindByType(Node, "Latitude"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "LocationAccuracy"), XmlUtilities.FindByType(Node, "Longitude"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "DataSource"), XmlUtilities.FindByType(Node, "LocationAccuracy"));
                Node.InsertAfter(XmlUtilities.EnsureNodeExists(Node, "Comments"), XmlUtilities.FindByType(Node, "DataSource"));
            }
        }

        /// <summary>
        /// Convert Soil format.
        /// </summary>
        /// <param name="Node"></param>
        private static XmlNode ToVersion33(XmlNode Node)
        {
            if (Node.Name.ToLower() == "soil")
            {
                string SoilName = XmlUtilities.NameAttr(Node);
                XmlUtilities.DeleteValue(Node, "Langitude");
                Node = XmlUtilities.ChangeType(Node, "Soil");
                XmlUtilities.SetNameAttr(Node, SoilName);
                RemoveBlankNodes(Node);

                ChangeNodeName(Node, "Thickness", "LayerStructure");
                ChangeNodeName(Node, "ASC_Order", "ASCOrder");
                ChangeNodeName(Node, "ASC_Sub-Order", "ASCSubOrder");

                foreach (XmlNode Child in XmlUtilities.ChildNodes(Node, ""))
                    ToVersion33(Child);
            }

            else if (Node.Name.ToLower() == "water")
            {
                // If we have a mix of shortcut <SoilCrop> and non short cut nodes then
                // we have to remove the shortcuts from all <SoilCrop> nodes and the parent
                // WaterNode.
                bool WaterNodeIsShortcutted = XmlUtilities.Attribute(Node, "shortcut") != "";
                bool AllCropsShortcutted = true;
                foreach (XmlNode SoilCrop in XmlUtilities.ChildNodes(Node, "SoilCrop"))
                {
                    if (XmlUtilities.Attribute(SoilCrop, "shortcut") == "")
                        AllCropsShortcutted = false;
                }
                // If the <water> node has a shortcut and there are <SoilCrop> children that
                // are not shortcutted then we need to remove all shortcuts on <Water> and the 
                // <SoilCrop> children.
                if (WaterNodeIsShortcutted && AllCropsShortcutted)
                {
                    // Leave alone
                }
                else
                {
                    if (WaterNodeIsShortcutted)
                        UnlinkNode(Node);
                    else
                    {
                        foreach (XmlNode SoilCrop in XmlUtilities.ChildNodes(Node, "SoilCrop"))
                        {
                            if (XmlUtilities.Attribute(SoilCrop, "shortcut") != "")
                                UnlinkNode(SoilCrop);
                        }
                    }
                }

                WriteLayeredDataAsArray(Node, "Thickness", "double", "mm");
                WriteLayeredDataAsArray(Node, "KS", "double", "mm/day");
                WriteLayeredDataAsArray(Node, "BD", "double", "g/cc");
                WriteLayeredDataAsArray(Node, "AirDry", "double", "mm/mm");
                WriteLayeredDataAsArray(Node, "LL15", "double", "mm/mm");
                WriteLayeredDataAsArray(Node, "DUL", "double", "mm/mm");
                WriteLayeredDataAsArray(Node, "SAT", "double", "mm/mm");
                foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
                    Node.RemoveChild(Layer);


                // Now convert all soil crop nodes.
                foreach (XmlNode SoilCrop in XmlUtilities.ChildNodes(Node, "SoilCrop"))
                {
                    WriteLayeredDataAsArray(SoilCrop, "Thickness", "double", "mm");
                    WriteLayeredDataAsArray(SoilCrop, "LL", "double", "mm/mm");
                    WriteLayeredDataAsArray(SoilCrop, "KL", "double", "/day");
                    WriteLayeredDataAsArray(SoilCrop, "XF", "double", "0-1");
                    foreach (XmlNode Layer in XmlUtilities.ChildNodes(SoilCrop, "Layer"))
                        SoilCrop.RemoveChild(Layer);
                }

            }
            else if (Node.Name.ToLower() == "soilwat")
            {
                string Shortcut = XmlUtilities.Attribute(Node, "shortcut");
                Node = XmlUtilities.ChangeType(Node, "SoilWater");

                if (Shortcut != "")
                {
                    Shortcut = ReplaceLastOccurrenceOf(Shortcut, "/SoilWat", "/SoilWater");
                    XmlUtilities.SetAttribute(Node, "shortcut", Shortcut);
                }
                ChangeNodeName(Node, "Cn2Bare", "CN2Bare");
                ChangeNodeName(Node, "CnRed", "CNRed");
                ChangeNodeName(Node, "CnCov", "CNCov");
                WriteLayeredDataAsArray(Node, "Thickness", "double", "mm");
                WriteLayeredDataAsArray(Node, "SWCON", "double", "0-1");
                WriteLayeredDataAsArray(Node, "MWCON", "double", "0-1");
                WriteLayeredDataAsArray(Node, "KLAT", "double", "mm/d");
                foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
                    Node.RemoveChild(Layer);
                RemoveBlankNodes(Node);
            }
            else if (Node.Name == "SoilOrganicMatter")
            {
                ChangeNodeName(Node, "RootCn", "RootCN");
                ChangeNodeName(Node, "SoilCn", "SoilCN");
                ChangeNodeName(Node, "RootWT", "RootWt");

                WriteLayeredDataAsArray(Node, "Thickness", "double", "mm");
                WriteLayeredDataAsArray(Node, "OC", "double", "Total %");
                WriteLayeredDataAsArray(Node, "FBiom", "double", "0-1");
                WriteLayeredDataAsArray(Node, "FInert", "double", "0-1");
                foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
                    Node.RemoveChild(Layer);
                RemoveBlankNodes(Node);

            }
            else if (Node.Name == "Analysis")
            {
                WriteLayeredDataAsArray(Node, "Thickness", "double", "mm");
                WriteLayeredDataAsArray(Node, "Rocks", "double", "%");
                WriteLayeredDataAsArray(Node, "Texture", "string", "");
                WriteLayeredDataAsArray(Node, "MunsellColour", "string", "");
                WriteLayeredDataAsArray(Node, "EC", "double", "1:5 dS/m");
                WriteLayeredDataAsArray(Node, "PH", "double", "1:5 water");
                WriteLayeredDataAsArray(Node, "CL", "double", "mg/kg");
                WriteLayeredDataAsArray(Node, "Boron", "double", "Hot water mg/kg");
                WriteLayeredDataAsArray(Node, "CEC", "double", "cmol+/kg");
                WriteLayeredDataAsArray(Node, "Ca", "double", "cmol+/kg");
                WriteLayeredDataAsArray(Node, "Mg", "double", "cmol+/kg");
                WriteLayeredDataAsArray(Node, "Na", "double", "cmol+/kg");
                WriteLayeredDataAsArray(Node, "K", "double", "cmol+/kg");
                WriteLayeredDataAsArray(Node, "ESP", "double", "%");
                WriteLayeredDataAsArray(Node, "Mn", "double", "mg/kg");
                WriteLayeredDataAsArray(Node, "Al", "double", "cmol+/kg");
                WriteLayeredDataAsArray(Node, "ParticleSizeSand", "double", "%");
                WriteLayeredDataAsArray(Node, "ParticleSizeSilt", "double", "%");
                WriteLayeredDataAsArray(Node, "ParticleSizeClay", "double", "%");
                foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
                    Node.RemoveChild(Layer);
            }
            else if (Node.Name == "Sample")
            {
                WriteLayeredDataAsArray(Node, "Thickness", "double", "mm");
                WriteLayeredDataAsArray(Node, "NO3", "double", "ppm");
                WriteLayeredDataAsArray(Node, "NH4", "double", "ppm");
                WriteLayeredDataAsArray(Node, "SW", "double", "mm/mm");
                WriteLayeredDataAsArray(Node, "OC", "double", "Total %");
                WriteLayeredDataAsArray(Node, "EC", "double", "1:5 dS/m");
                WriteLayeredDataAsArray(Node, "PH", "double", "1:5 water");
                WriteLayeredDataAsArray(Node, "CL", "double", "mg/kg");
                WriteLayeredDataAsArray(Node, "ESP", "double", "%");
                foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
                    Node.RemoveChild(Layer);
            }
            else if (Node.Name == "InitWater")
            {
                string Shortcut = XmlUtilities.Attribute(Node, "shortcut");
                Node = XmlUtilities.ChangeType(Node, "InitialWater");
                if (Shortcut != null)
                {
                    Shortcut = ReplaceLastOccurrenceOf(Shortcut, "/InitWater", "/InitialWater");
                    XmlUtilities.SetAttribute(Node, "shortcut", Shortcut);
                }

                string Percent = XmlUtilities.Value(Node, "percentmethod/percent");
                string distributed = XmlUtilities.Value(Node, "percentmethod/distributed");
                string DepthWetSoil = XmlUtilities.Value(Node, "DepthWetSoilMethod/Depth");
                string RelativeTo = XmlUtilities.Value(Node, "RelativeTo");
                if (Percent != "")
                {
                    // remove old <percentmethod> - case was wrong.
                    Node.RemoveChild(XmlUtilities.Find(Node, "percentmethod"));

                    if (distributed.Equals("filled from top", StringComparison.CurrentCultureIgnoreCase))
                        distributed = "FilledFromTop";
                    else
                        distributed = "EvenlyDistributed";

                    XmlUtilities.SetValue(Node, "FractionFull", Percent);
                    XmlUtilities.SetValue(Node, "PercentMethod", distributed);
                }
                else if (DepthWetSoil != "")
                    XmlUtilities.SetValue(Node, "DepthWetSoil", DepthWetSoil);

                if (RelativeTo != "")
                    XmlUtilities.SetValue(Node, "RelativeTo", RelativeTo);
            }
            else if (Node.Name == "Phosphorus")
            {
                WriteLayeredDataAsArray(Node, "Thickness", "double", "mm");
                WriteLayeredDataAsArray(Node, "LabileP", "double", "mg/kg");
                WriteLayeredDataAsArray(Node, "BandedP", "double", "kg/ha");
                WriteLayeredDataAsArray(Node, "RockP", "double", "kg/ha");
                WriteLayeredDataAsArray(Node, "Sorption", "double", "-");
                foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
                    Node.RemoveChild(Layer);
                RemoveBlankNodes(Node);
            }
            else if (Node.Name == "Swim")
            {
                if (XmlUtilities.Value(Node, "VC").Equals("on", StringComparison.CurrentCultureIgnoreCase))
                    XmlUtilities.SetValue(Node, "VC", "true");
                else
                    XmlUtilities.SetValue(Node, "VC", "false");
                if (XmlUtilities.Value(Node, "diagnostics").Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                    XmlUtilities.SetValue(Node, "diagnostics", "true");
                else
                    XmlUtilities.SetValue(Node, "diagnostics", "false");

                ChangeNodeName(Node, "Cn2Bare", "CN2Bare");
                ChangeNodeName(Node, "CnRed", "CNRed");
                ChangeNodeName(Node, "CnCov", "CNCov");
                ChangeNodeName(Node, "Kdul", "KDul");
                ChangeNodeName(Node, "psidul", "PSIDul");
                ChangeNodeName(Node, "dtmin", "DTmin");
                ChangeNodeName(Node, "dtmax", "DTmax");
                ChangeNodeName(Node, "diagnostics", "Diagnostics");

                foreach (XmlNode SwimSolute in XmlUtilities.ChildNodes(Node, "SwimSoluteParameters"))
                {
                    ChangeNodeName(SwimSolute, "dis", "Dis");
                    ChangeNodeName(SwimSolute, "disp", "Disp");
                    ChangeNodeName(SwimSolute, "a", "A");
                    ChangeNodeName(SwimSolute, "dthc", "DTHC");
                    ChangeNodeName(SwimSolute, "dthp", "DTHP");

                    WriteLayeredDataAsArray(SwimSolute, "Thickness", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "NO3Exco", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "NO3FIP", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "NH4Exco", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "NH4FIP", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "UreaExco", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "UreaFIP", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "ClExco", "double", "");
                    WriteLayeredDataAsArray(SwimSolute, "ClFIP", "double", "");
                    foreach (XmlNode Layer in XmlUtilities.ChildNodes(SwimSolute, "Layer"))
                        SwimSolute.RemoveChild(Layer);
                    RemoveBlankNodes(SwimSolute);
                }
            }
            else if (Node.Name == "LayerStructure")
            {
                string Shortcut = XmlUtilities.Attribute(Node, "shortcut");
                if (Shortcut != "")
                {
                    Shortcut = ReplaceLastOccurrenceOf(Shortcut, "/Thickness", "/LayerStructure");
                    XmlUtilities.SetAttribute(Node, "shortcut", Shortcut);
                }
                else
                {
                    string[] Values = XmlUtilities.Value(Node, "Values").Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    XmlNode ThicknessNode = Node.AppendChild(Node.OwnerDocument.CreateElement("Thickness"));
                    XmlUtilities.SetValues(ThicknessNode, "double", Values);
                    Node.RemoveChild(XmlUtilities.Find(Node, "Values"));
                }
            }
            else if (Node.Name.ToLower() == "cl")
                XmlUtilities.ChangeType(Node, "Solute");

            return Node;
        }

        /// <summary>
        /// Make sure that soil Latitude / Longitude are doubles. AgMIP translator has them as "?" characters.
        /// </summary>
        private static void ToVersion35(XmlNode Node)
        {
            if (Node.Name.ToLower() == "soil")
            {
                string LongitudeSt = XmlUtilities.Value(Node, "Longitude");
                double Longitude;
                if (!double.TryParse(LongitudeSt, out Longitude))
                    XmlUtilities.DeleteValue(Node, "Longitude");

                string LatitudeSt = XmlUtilities.Value(Node, "Latitude");
                double Latitude;
                if (!double.TryParse(LatitudeSt, out Latitude))
                    XmlUtilities.DeleteValue(Node, "Latitude");
            }
        }


        private static XmlNode AnnotateNode(XmlNode Node, string NodeName, string Type, string Description)
        {
            XmlNode NodeToAnnotate = XmlUtilities.EnsureNodeExists(Node, NodeName);
            if (NodeToAnnotate.Name != NodeName)
            {
                // Must be different case - fix.
                NodeToAnnotate = XmlUtilities.ChangeType(NodeToAnnotate, NodeName);
            }

            if (Type != "")
                XmlUtilities.SetAttribute(NodeToAnnotate, "type", Type);
            if (Description != "")
                XmlUtilities.SetAttribute(NodeToAnnotate, "description", Description);

            return NodeToAnnotate;
        }

        private static void MoveSoilNode(XmlNode Node, string NodeName, XmlNode NodeToMoveTo)
        {
            XmlNode NodeToMove = XmlUtilities.Find(Node, NodeName);
            if (NodeToMove != null)
                NodeToMoveTo.AppendChild(NodeToMove);
        }

        private static void ConvertSampleNode(XmlNode SoilNode, XmlNode OldSampleNode, string NewNodeName)
        {
            if (XmlUtilities.Attribute(OldSampleNode, "shortcut") != "")
            {
                // Remove the <InitWater> node as it's a shortcut that will be readded later.
                OldSampleNode.ParentNode.RemoveChild(OldSampleNode);
            }
            else
            {
                XmlNode ProfileNode = XmlUtilities.Find(OldSampleNode, "Profile");
                if (ProfileNode == null)
                {
                    XmlUtilities.SetNameAttr(OldSampleNode, NewNodeName);
                    if (!OldSampleNode.HasChildNodes)
                        XmlUtilities.SetValue(OldSampleNode, "PercentMethod/Percent", "0");
                }
                else
                {
                    XmlNode SampleNode = SoilNode.AppendChild(SoilNode.OwnerDocument.CreateElement("Sample"));
                    XmlUtilities.SetNameAttr(SampleNode, NewNodeName);
                    AnnotateNode(SampleNode, "Date", "date", "Sample date:");

                    foreach (XmlNode LayerNode in ProfileNode.ChildNodes)
                    {
                        XmlNode NewLayerNode = SampleNode.AppendChild(SoilNode.OwnerDocument.CreateElement("Layer"));
                        SetValue(NewLayerNode, "Thickness", XmlUtilities.Value(LayerNode, "Thickness"), "mm");
                        if (XmlUtilities.Value(OldSampleNode, "WaterFormat") == "GravimetricPercent")
                            SetValue(NewLayerNode, "SW", XmlUtilities.Value(LayerNode, "sw"), "grav. mm/mm");
                        else
                            SetValue(NewLayerNode, "SW", XmlUtilities.Value(LayerNode, "sw"), "mm/mm");
                        SetValue(NewLayerNode, "NO3", XmlUtilities.Value(LayerNode, "no3"), "ppm");
                        SetValue(NewLayerNode, "NH4", XmlUtilities.Value(LayerNode, "nh4"), "ppm");
                        SetValue(NewLayerNode, "OC", XmlUtilities.Value(LayerNode, "oc"), "Total %");
                        SetValue(NewLayerNode, "EC", XmlUtilities.Value(LayerNode, "ec"), "1:5 dS/m");
                        SetValue(NewLayerNode, "PH", XmlUtilities.Value(LayerNode, "ph"), "CaCl2");
                        SetValue(NewLayerNode, "CL", XmlUtilities.Value(LayerNode, "cl"), "mg/kg");
                    }

                    // Remove old <InitWater> node.
                    SoilNode.RemoveChild(OldSampleNode);
                }
            }
        }

        private static void SetValue(XmlNode Node, string NodeName, string Value, string Units)
        {
            if (Value != "")
            {
                XmlUtilities.SetValue(Node, NodeName, Value);
                // Only put the units on the first layer.
                XmlNode FirstChild = XmlUtilities.ChildNodes(Node.ParentNode, "Layer")[0];
                if (Node == FirstChild)
                    XmlUtilities.SetAttribute(XmlUtilities.Find(Node, NodeName), "units", Units);
            }
        }

        /// <summary>
        /// Create a child node with a shortcut attribute if the child doesn't already exist
        /// and the shortcut source code does have the child.
        /// </summary>
        private static void CreateChildWithShortcut(XmlNode Node, string ShortCutPath, string ChildName, string ChildType)
        {
            XmlNode ShortcutSourceNode = XmlUtilities.Find(Node.OwnerDocument.DocumentElement, ShortCutPath);
            if (ShortcutSourceNode != null && XmlUtilities.Find(Node, ChildName) == null)
            {
                XmlNode ShortcutSourceNodeChild = XmlUtilities.Find(ShortcutSourceNode, ShortCutPath + "/" + ChildName);
                if (ShortcutSourceNodeChild != null && XmlUtilities.Type(ShortcutSourceNodeChild) == ChildType)
                {
                    XmlNode Child = Node.AppendChild(Node.OwnerDocument.CreateElement(ChildType));
                    if (ChildName != ChildType)
                        XmlUtilities.SetNameAttr(Child, ChildName);
                    XmlUtilities.SetAttribute(Child, "shortcut", ShortCutPath + "/" + ChildName);
                }
            }
        }

        /// <summary>
        /// Fix the order of the properties of the specified parent xml node to that 
        /// giveen in ChildNodeNames
        /// </summary>
        private static void SetPropertiesOrder(XmlNode ParentNode, string[] ChildNodeNames, string[] Units, bool CheckUnits)
        {
            for (int i = 0; i < ChildNodeNames.Length; i++)
            {
                XmlNode Child = XmlUtilities.Find(ParentNode, ChildNodeNames[i]);
                if (Child == null)
                {
                    Child = ParentNode.OwnerDocument.CreateElement(ChildNodeNames[i]);
                    if (Units != null && Units[i] != "")
                        XmlUtilities.SetAttribute(Child, "units", Units[i]);
                }
                Child = ParentNode.InsertBefore(Child, ParentNode.ChildNodes[i]);
                if (CheckUnits && XmlUtilities.Attribute(Child, "units") == "")
                    XmlUtilities.SetAttribute(Child, "units", Units[i]);
            }
        }
        /// <summary>
        /// Fix the order of the layered variables of the specified parent xml node to that 
        /// giveen in ChildNodeNames
        /// </summary>
        private static void SetLayeredOrder(XmlNode ProfileNode, string[] ChildNodeNames, string[] Units)
        {
            string DepthUnits = null;
            bool First = true;
            foreach (XmlNode LayerNode in XmlUtilities.ChildNodes(ProfileNode, "Layer"))
            {
                XmlNode DepthNode = XmlUtilities.Find(LayerNode, "Depth");
                if (DepthNode != null)
                {
                    if (DepthUnits == null)
                        DepthUnits = XmlUtilities.Attribute(DepthNode, "units");
                    RemoveDepthNodes(DepthNode, DepthUnits);
                }
                SetPropertiesOrder(LayerNode, ChildNodeNames, Units, First);
                if (First)
                    First = false;
            }
        }

        /// <summary>
        /// Change the depth nodes: <depth>0-10</depth>
        /// to thickness nodes : <thickness>100</thickness>
        /// </summary>
        private static void RemoveDepthNodes(XmlNode DepthNode, string DepthUnits)
        {
            string[] DepthStringBits = DepthNode.InnerText.Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int Thickness = 100;
            if (DepthStringBits.Length == 2)
            {
                int Depth1;
                int Depth2;
                if (Int32.TryParse(DepthStringBits[0], out Depth1))
                    if (Int32.TryParse(DepthStringBits[1], out Depth2))
                    {
                        Thickness = Depth2 - Depth1;
                        if (DepthUnits == "cm")
                            Thickness *= 10;
                    }
            }
            XmlNode ThicknessNode = XmlUtilities.ChangeType(DepthNode, "Thickness");
            ThicknessNode.InnerText = Thickness.ToString();
            XmlUtilities.SetAttribute(ThicknessNode, "units", "mm");
        }

        /// <summary>
        /// Remove all blank nodes e.g. <Slope></Slope>
        /// </summary>
        private static void RemoveBlankNodes(XmlNode Node)
        {
            foreach (XmlNode Child in XmlUtilities.ChildNodes(Node, ""))
                if (Child.InnerText == "" && XmlUtilities.Attribute(Child, "shortcut") == "")
                    Node.RemoveChild(Child);
        }

        private static void ChangeNodeName(XmlNode Node, string FromVariableName, string ToVariableName)
        {
            XmlNode Child = XmlUtilities.Find(Node, FromVariableName);
            if (Child != null)
                XmlUtilities.ChangeType(Child, ToVariableName);
        }

        /// <summary>
        /// Change:
        /// <Layer>
        ///    <Thickness units="mm">150</Thickness>
        ///  </Layer>
        ///  <Layer>
        ///    <Thickness>150</Thickness>
        ///  </Layer>
        ///  
        /// to:
        /// <Thickness>
        ///     <double>150</double>
        ///     <double>150</double>
        /// </Thickness>
        /// </summary>
        private static void WriteLayeredDataAsArray(XmlNode Node, string VariableName, string TypeName, string DefaultUnits)
        {
            string Units = "";
            List<string> Values = new List<string>();
            List<string> Codes = new List<string>();
            foreach (XmlNode Layer in XmlUtilities.ChildNodes(Node, "Layer"))
            {
                XmlNode ValueNode = XmlUtilities.Find(Layer, VariableName);

                string Value = "";
                string Code = "";
                if (ValueNode != null)
                {
                    Value = ValueNode.InnerText;
                    Code = XmlUtilities.Attribute(ValueNode, "code");
                }
                if (TypeName == "double" && (Value == "999999" || Value == ""))
                    Values.Add("NaN");
                else
                    Values.Add(Value);
                Codes.Add(Code);
                if (ValueNode != null &&
                    XmlUtilities.Attribute(ValueNode, "units") != "" &&
                    XmlUtilities.Attribute(ValueNode, "units") != DefaultUnits)
                    Units = XmlUtilities.Attribute(ValueNode, "units");
            }
            bool ValuesExist;
            if (TypeName == "double")
            {
                double[] DoubleValues = MathUtilities.StringsToDoubles(Values);
                ValuesExist = MathUtilities.ValuesInArray(DoubleValues);
            }
            else
                ValuesExist = MathUtilities.ValuesInArray(Values.ToArray());

            if (ValuesExist)
            {
                XmlNode NewNode = Node.AppendChild(Node.OwnerDocument.CreateElement(VariableName));
                XmlUtilities.SetValues(NewNode, TypeName, Values);
                if (Units != "")
                {
                    Units = Units.Replace("Total %", "Total");
                    Units = Units.Replace("Walkley Black %", "WalkleyBlack");
                    Units = Units.Replace("1:5 water", "Water");
                    Units = Units.Replace("Hot water mg/kg", "HotWater");
                    Units = Units.Replace("Hot CaCl2", "HotCaCl2");
                    Units = Units.Replace("kg/ha", "kgha");
                    Units = Units.Replace("grav. mm/mm", "Gravimetric");
                    Units = Units.Replace("mm/mm", "Volumetric");
                    XmlUtilities.SetValue(Node, VariableName + "Units", Units);
                }
            }
            if (MathUtilities.ValuesInArray(Codes.ToArray()))
            {
                XmlNode CodesNode = Node.AppendChild(Node.OwnerDocument.CreateElement(VariableName + "Metadata"));
                XmlUtilities.SetValues(CodesNode, "string", CodeToMetaData(Codes.ToArray()));
            }
        }

        private static string[] CodeToMetaData(string[] Codes)
        {
            string[] Metadata = new string[Codes.Length];
            for (int i = 0; i < Codes.Length; i++)
                if (Codes[i] == "FM")
                    Metadata[i] = "Field measured and checked for sensibility";
                else if (Codes[i] == "C_grav")
                    Metadata[i] = "Calculated from gravimetric moisture when profile wet but drained";
                else if (Codes[i] == "E")
                    Metadata[i] = "Estimated based on local knowledge";
                else if (Codes[i] == "U")
                    Metadata[i] = "Unknown source or quality of data";
                else if (Codes[i] == "LM")
                    Metadata[i] = "Laboratory measured";
                else if (Codes[i] == "V")
                    Metadata[i] = "Volumetric measurement";
                else if (Codes[i] == "M")
                    Metadata[i] = "Measured";
                else if (Codes[i] == "C_bd")
                    Metadata[i] = "Calculated from measured, estimated or calculated BD";
                else if (Codes[i] == "C_pt")
                    Metadata[i] = "Developed using a pedo-transfer function";
                else
                    Metadata[i] = Codes[i];
            return Metadata;
        }

        private static string ReplaceLastOccurrenceOf(string Shortcut, string SearchFor, string ReplaceWith)
        {
            int Index = Shortcut.LastIndexOf(SearchFor);
            if (Index != -1)
                return Shortcut.Substring(0, Index) + ReplaceWith + Shortcut.Substring(Index + SearchFor.Length);
            return Shortcut;
        }
        private static void UnlinkNode(XmlNode Node)
        {
            string Shortcut = XmlUtilities.Attribute(Node, "shortcut");
            if (Shortcut != "")
            {
                XmlNode ConcreteNode = XmlUtilities.Find(Node.OwnerDocument.DocumentElement, Shortcut);
                while (ConcreteNode != null && XmlUtilities.Attribute(ConcreteNode, "shortcut") != "")
                {
                    Shortcut = XmlUtilities.Attribute(ConcreteNode, "shortcut");
                    ConcreteNode = XmlUtilities.Find(Node.OwnerDocument, Shortcut);
                }

                if (ConcreteNode != null)
                {
                    foreach (XmlNode ConcreteChild in XmlUtilities.ChildNodes(ConcreteNode, ""))
                    {
                        XmlNode NodeChild = XmlUtilities.Find(Node, XmlUtilities.NameAttr(ConcreteChild));
                        bool Keep;
                        if (NodeChild == null || NodeChild.Name == "Layer")
                            Keep = true;
                        else
                        {
                            if (XmlUtilities.Attribute(NodeChild, "shortcut") == "")
                                Keep = false;
                            else
                            {
                                Keep = true;
                                Node.RemoveChild(NodeChild);
                            }
                        }
                        if (Keep)
                            Node.AppendChild(ConcreteChild.Clone());

                    }
                    XmlUtilities.DeleteAttribute(Node, "shortcut");
                }
            }
        }

    }
}
