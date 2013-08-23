using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using System.Reflection;

namespace Utility
{
    public class Xml
    {
        public const char Delimiter = '/';

        public static XmlNode CreateNode(XmlDocument Document, string Type, string Name)
        {
            // -----------------------------------------------------------------
            // Create a node with the specified type and name.
            // -----------------------------------------------------------------
            XmlNode NewNode = Document.CreateElement(Type);
            if (Name != "")
                SetAttribute(NewNode, "name", Name);
            return NewNode;
        }
        public static string Name(XmlNode Node)
        {
            // --------------------------------------
            // Return the name attribute or if not 
            // found then the element name
            // i.e. 'Type'
            // --------------------------------------
            string value = Attribute(Node, "name");
            if (value == "")
                return Type(Node);
            else
                return value;
        }
        public static void SetName(XmlNode Node, string Name)
        {
            if (Name != Utility.Xml.Name(Node))
                SetAttribute(Node, "name", Name);
        }
        public static string Type(XmlNode Node)
        {
            // ---------------------------------------
            // Return the 'type' of the specified node
            // ---------------------------------------
            return Node.Name;
        }
        public static XmlNode ChangeType(XmlNode Node, string NewType)
        {
            if (Node.ParentNode != null)
            {
                XmlNode NewNode = Node.OwnerDocument.CreateElement(NewType, Node.NamespaceURI);

                // Move all attributes
                while (Node.Attributes.Count > 0)
                    NewNode.Attributes.Append(Node.Attributes.RemoveAt(0));

                // Move all child nodes
                while (Node.ChildNodes.Count > 0)
                    NewNode.AppendChild(Node.RemoveChild(Node.ChildNodes[0]));

                Node.ParentNode.ReplaceChild(NewNode, Node);
                return NewNode;
            }
            return null;
        }

        public static XmlNode Parent(XmlNode Node)
        {
            // ---------------------------------------
            // Return the parent of the specified node
            // ---------------------------------------
            if (Node.ParentNode == null || Node.ParentNode.Name == "#document")
                return null;
            else
                return Node.ParentNode;
        }

        public static string FullPath(XmlNode Node)
        {
            // --------------------------------------------------------
            // Return a full path for the specified node. Paths are 
            // of the form: /RootNode/ChildNode/SubChildNode
            // --------------------------------------------------------
            XmlNode LocalData = Node;
            StringBuilder FullPath = new StringBuilder();
            do
            {
                FullPath.Insert(0, Delimiter);
                FullPath.Insert(0, Name(LocalData));
            } while ((LocalData = Parent(LocalData)) != null);
            return FullPath.ToString();
        }
        public static string ParentPath(string NodePath)
        {
            int PosDelimiter = NodePath.LastIndexOf(Delimiter);
            if (PosDelimiter == -1)
                throw new Exception("Cannot get the parent of the specified node: " + NodePath);
            string ParentName = NodePath.Remove(PosDelimiter);
            if (ParentName == "")
                throw new Exception("Cannot get the parent of the root node");
            return ParentName;
        }
        public static XmlNode Find(XmlNode Node, string NamePath)
        {
            // ----------------------------------------------------
            // Find a child with the specified NamePath. NamePath
            // can be a single child name or a path delimited with
            // '/' characters e.g. ChildNode/SubChildNode or /RootNode/ChildNode
            // Returns null if no child found. 
            // ----------------------------------------------------
            if (Node == null)
                return null;
            if (NamePath == "")
                throw new Exception("Cannot call FindByName with a blank path");
            if (NamePath[0] == Delimiter)
            {
                Node = Node.OwnerDocument.DocumentElement;
                int Pos = NamePath.IndexOf(Delimiter, 1);
                string RootName;
                if (Pos == -1)
                {
                    RootName = NamePath.Substring(1);
                    NamePath = "";
                }
                else
                {
                    RootName = NamePath.Substring(1, Pos - 1);
                    NamePath = NamePath.Substring(Pos + 1);
                }
                if (RootName.ToLower() != Name(Node).ToLower())
                    return null;
                if (NamePath == "")
                    return Node;
            }

            string ChildName, Remainder;
            int PosDelimiter = NamePath.IndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                ChildName = NamePath.Substring(0, PosDelimiter);
                Remainder = NamePath.Substring(PosDelimiter + 1);
            }
            else
            {
                ChildName = NamePath;
                Remainder = "";
            }
            if (ChildName == "..")
                return Find(Node.ParentNode, Remainder);
            else if (ChildName.Length > 0 && ChildName[0] == '@')
            {
                return Node.Attributes[ChildName.Substring(1)];
            }
            else
            {
                foreach (XmlNode Child in Node.ChildNodes)
                {
                    if (Name(Child).ToLower() == ChildName.ToLower())
                    {
                        if (Remainder == "")
                            return Child;
                        else
                            return Find(Child, Remainder);
                    }
                }
            }
            return null;
        }
        public static XmlNode FindByType(XmlNode Node, string TypePath)
        {
            // ----------------------------------------------------
            // Find a child with the specified TypePath. TypePath
            // can be a single child type or a path delimited with
            // '/' characters e.g. ChildNode/SubChildNode or /RootNode/ChildNode
            // Returns null if no child found. 
            // ----------------------------------------------------
            if (Node == null)
                return null;
            if (TypePath == "")
                throw new Exception("Cannot call FindByType with a blank path");
            if (TypePath[0] == Delimiter)
            {
                Node = Node.OwnerDocument.DocumentElement;
                int Pos = TypePath.IndexOf(Delimiter, 1);
                string RootName = TypePath.Substring(1, Pos - 1);
                if (RootName.ToLower() != Type(Node).ToLower())
                    return null;
                TypePath = TypePath.Substring(Pos + 1);
            }

            string ChildType, Remainder;
            int PosDelimiter = TypePath.IndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                ChildType = TypePath.Substring(0, PosDelimiter);
                Remainder = TypePath.Substring(PosDelimiter + 1);
            }
            else
            {
                ChildType = TypePath;
                Remainder = "";
            }
            foreach (XmlNode Child in Node.ChildNodes)
            {
                if (Type(Child).ToLower() == ChildType.ToLower())
                {
                    if (Remainder == "")
                        return Child;
                    else
                        return FindByType(Child, Remainder);
                }
            }
            return null;
        }
        public static XmlNode FindRecursively(XmlNode Node, string Name)
        {
            if (Utility.Xml.Name(Node).ToLower() == Name.ToLower())
                return Node;
            foreach (XmlNode Child in Node.ChildNodes)
            {
                XmlNode Result = FindRecursively(Child, Name);
                if (Result != null)
                    return Result;
            }
            return null;
        }
        public static void FindAllRecursively(XmlNode Node, string Name, ref List<XmlNode> Nodes)
        {
            if (Utility.Xml.Name(Node).ToLower() == Name.ToLower())
                Nodes.Add(Node);
            foreach (XmlNode Child in Node.ChildNodes)
                FindAllRecursively(Child, Name, ref Nodes);
        }
        public static void FindAllRecursivelyByType(XmlNode Node, string TypeName, ref List<XmlNode> Nodes)
        {
            if (Utility.Xml.Type(Node).ToLower() == TypeName.ToLower())
                Nodes.Add(Node);
            foreach (XmlNode Child in Node.ChildNodes)
                FindAllRecursivelyByType(Child, TypeName, ref Nodes);
        }

        public static XmlNode ChildByNameAndType(XmlNode Node, string NameFilter, string TypeFilter)
        {
            // ----------------------------------------------------
            // Find a child with the specified name and type
            // Returns null if no child found. 
            // ----------------------------------------------------
            foreach (XmlNode Child in Node.ChildNodes)
            {
                if (Name(Child).ToLower() == NameFilter.ToLower() && Type(Child).ToLower() == TypeFilter.ToLower())
                    return Child;
            }
            return null;
        }
        public static XmlNode ChildByTypeAndValue(XmlNode Node, string TypeFilter, string ValueFilter)
        {
            // ----------------------------------------------------
            // Find a child with the specified Type and value. 
            // Returns null if no child found. 
            // ----------------------------------------------------
            foreach (XmlNode Child in Node.ChildNodes)
            {
                if (Type(Child).ToLower() == TypeFilter.ToLower() && Child.InnerText == ValueFilter)
                    return Child;
            }
            return null;
        }
        public static List<XmlNode> ChildNodes(XmlNode Node, string TypeFilter)
        {
            // ----------------------------------------------------
            // Return an array of children that match the specified
            // filter. The filter can be an empty string to match
            // all child XmlNodes
            // ----------------------------------------------------
            List<XmlNode> MatchingChildren = new List<XmlNode>();
            if (Node != null)
            {
                foreach (XmlNode Child in Node.ChildNodes)
                {
                    if (Child.Name != "#text" && Child.Name != "#comment" && Child.Name != "#cdata-section" &&
                        TypeFilter == "" || Type(Child).ToLower() == TypeFilter.ToLower())
                        MatchingChildren.Add(Child);
                }
            }
            return MatchingChildren;
        }
        public static List<XmlNode> ChildNodesByName(XmlNode Node, string NameFilter)
        {
            // ----------------------------------------------------
            // Return an array of children that match the specified
            // filter. The filter can be an empty string to match
            // all child XmlNodes
            // ----------------------------------------------------
            List<XmlNode> MatchingChildren = new List<XmlNode>();
            if (Node != null)
            {
                foreach (XmlNode Child in Node.ChildNodes)
                {
                    if (Child.Name != "#text" && Child.Name != "#comment" && Child.Name != "#cdata-section" &&
                        NameFilter == "" || Name(Child).ToLower() == NameFilter.ToLower())
                        MatchingChildren.Add(Child);
                }
            }
            return MatchingChildren;
        }
        public static string[] ChildNames(XmlNode Node, string TypeFilter)
        {
            List<XmlNode> Children = ChildNodes(Node, TypeFilter);
            string[] Names = new string[Children.Count];
            for (int i = 0; i != Children.Count; i++)
                Names[i] = Name(Children[i]);
            return Names;
        }
        public static string Value(XmlNode Child, string NamePath)
        {
            XmlNode FoundNode;
            if (NamePath == "")
                FoundNode = Child;
            else
                FoundNode = Find(Child, NamePath);
            if (FoundNode != null)
                return FoundNode.InnerText;
            else
                return "";
        }
        public static void SetValue(XmlNode Node, string NamePath, string Value)
        {
            XmlNode ValueNode = EnsureNodeExists(Node, NamePath);
            ValueNode.InnerText = Value;
        }
        public static List<string> Values(XmlNode Node, string TypeFilter)
        {
            int PosDelimiter = TypeFilter.LastIndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                Node = Find(Node, TypeFilter.Substring(0, PosDelimiter));
                TypeFilter = TypeFilter.Substring(PosDelimiter + 1);
            }

            List<string> ReturnValues = new List<string>();
            foreach (XmlNode Child in ChildNodes(Node, TypeFilter))
                ReturnValues.Add(Child.InnerText);
            return ReturnValues;
        }
        public static List<string> ValuesRecursive(XmlNode Node, string TypeFilter)
        {
            int PosDelimiter = TypeFilter.LastIndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                Node = Find(Node, TypeFilter.Substring(0, PosDelimiter));
                TypeFilter = TypeFilter.Substring(PosDelimiter + 1);
            }

            List<string> ReturnValues = new List<string>();
            foreach (XmlNode Child in ChildNodes(Node, ""))
            {
                if (Child.Name == TypeFilter)
                    ReturnValues.Add(Child.InnerText);
                ReturnValues.AddRange(ValuesRecursive(Child, TypeFilter)); // recursion
            }
            return ReturnValues;
        }
        public static void SetValues(XmlNode Node, string NamePath, List<string> Values)
        {
            int PosDelimiter = NamePath.LastIndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                Node = Find(Node, NamePath.Substring(0, PosDelimiter));
                NamePath = NamePath.Substring(PosDelimiter + 1);
            }

            EnsureNumberOfChildren(Node, NamePath, "", Values.Count);

            int i = 0;
            foreach (XmlNode Child in ChildNodes(Node, NamePath))
            {
                SetValue(Child, "", Values[i]);
                i++;
            }
        }
        public static void SetValues(XmlNode Node, string NamePath, string[] Values)
        {
            List<string> Vals = new List<string>();
            Vals.AddRange(Values);
            SetValues(Node, NamePath, Vals);
        }
        public static string Attribute(XmlNode Node, string AttributeName)
        {
            // -----------------------------------------------------------------
            // Return the specified attribute or "" if not found
            // -----------------------------------------------------------------
            if (Node.Attributes != null)
            {
                foreach (XmlAttribute A in Node.Attributes)
                {
                    if (string.Equals(A.Name, AttributeName, StringComparison.CurrentCultureIgnoreCase))
                        return A.Value;
                }
            }
            return "";
        }
        public static void SetAttribute(XmlNode Node, string AttributeName, string AttributeValue)
        {
            // ----------------------------------------
            // Set the value of the specified attribute
            // ----------------------------------------
            if (Attribute(Node, AttributeName) != AttributeValue)
            {
                XmlNode attr = Node.OwnerDocument.CreateNode(XmlNodeType.Attribute, AttributeName, "");
                attr.Value = AttributeValue;
                Node.Attributes.SetNamedItem(attr);
            }
        }
        public static void DeleteAttribute(XmlNode Node, string AttributeName)
        {
            // ----------------------------------------
            // Delete the specified attribute
            // ----------------------------------------
            XmlAttribute A = (XmlAttribute)Node.Attributes.GetNamedItem(AttributeName);
            if (A != null)
            {
                Node.Attributes.Remove(A);
            }
        }

        public static void DeleteValue(XmlNode Node, string ValueName)
        {
            // ----------------------------------------
            // Delete the specified value
            // ----------------------------------------
            XmlNode ValueNode = Find(Node, ValueName);
            if (ValueNode != null)
                ValueNode.ParentNode.RemoveChild(ValueNode);
        }

        public static string FormattedXML(string Xml)
        {
            // -------------------------------------------------
            // Format the specified XML using indentation etc.
            // -------------------------------------------------
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml("<dummy>" + Xml + "</dummy>");
            StringWriter TextWriter = new StringWriter();
            XmlTextWriter Out = new XmlTextWriter(TextWriter);
            Out.Formatting = Formatting.Indented;
            Doc.DocumentElement.WriteContentTo(Out);
            return TextWriter.ToString();
        }
        public static void EnsureNodeIsUnique(XmlNode Node)
        {
            // -------------------------------------------------------------
            // Make sure the node's name is unique amongst it's siblings.
            // -------------------------------------------------------------
            string BaseName = Name(Node);
            string UniqueChildName = BaseName;
            for (int i = 1; i != 10000; i++)
            {
                int Count = 0;
                foreach (XmlNode Sibling in Node.ParentNode.ChildNodes)
                {
                    if (Name(Sibling).ToLower() == UniqueChildName.ToLower())
                        Count++;
                }
                if (Count == 1)
                    return;
                UniqueChildName = BaseName + i.ToString();
                SetAttribute(Node, "name", UniqueChildName);
            }
            throw new Exception("Cannot find a unique name for child: " + Name(Node));
        }
        public static void EnsureNumberOfChildren(XmlNode Node, string ChildType, string ChildName, int NumChildren)
        {
            // -------------------------------------------------------------------------
            // Ensure there are the specified number of children with the speciifed type
            // -------------------------------------------------------------------------
            string[] ChildrenNames = ChildNames(Node, ChildType);
            int NumChildrenToAdd = NumChildren - ChildrenNames.Length;
            int NumChildrenToDelete = ChildrenNames.Length - NumChildren;
            for (int i = 1; i <= NumChildrenToAdd; i++)
                if (Node!= null)
                    Node.AppendChild(CreateNode(Node.OwnerDocument, ChildType, ChildName));

            if (NumChildrenToDelete > 0)
            {
                List<XmlNode> ChildsToDelete = ChildNodes(Node, ChildType);
                ChildsToDelete.RemoveRange(0, ChildsToDelete.Count - NumChildrenToDelete);
                foreach (XmlNode ChildToDelete in ChildsToDelete)
                    Node.RemoveChild(ChildToDelete);
            }
        }

        private class XmlNodeComparer : System.Collections.IComparer
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int System.Collections.IComparer.Compare(Object x, Object y)
            {
                XmlNode yNode = (XmlNode)y;
                XmlNode xNode = (XmlNode)x;
                return ((new CaseInsensitiveComparer()).Compare(Name(xNode), Name(yNode)));
            }

        }
        public static void Sort(XmlNode Node, IComparer Comparer)
        {
            XmlNode[] SortedNodes = new XmlNode[Node.ChildNodes.Count];
            for (int i = 0; i != Node.ChildNodes.Count; i++)
            {
                SortedNodes[i] = Node.ChildNodes[i];
            }
            if (Comparer == null)
                Array.Sort(SortedNodes, new XmlNodeComparer());
            else
                Array.Sort(SortedNodes, Comparer);
            foreach (XmlNode Child in ChildNodes(Node, ""))
                Child.ParentNode.RemoveChild(Child);
            foreach (XmlNode Child in SortedNodes)
                Node.AppendChild(Child);
        }

        public static XmlNode EnsureNodeExists(XmlNode Node, string NodePath)
        {
            // --------------------------------------------------------
            // Ensure a node exists by creating nodes as necessary
            // for the specified node path.
            // --------------------------------------------------------

            if (NodePath.Length == 0)
                return Node;

            int PosDelimiter = NodePath.IndexOf(Utility.Xml.Delimiter);
            string ChildNameToMatch = NodePath;
            if (PosDelimiter != -1)
                ChildNameToMatch = NodePath.Substring(0, PosDelimiter);

            foreach (XmlNode Child in Node.ChildNodes)
            {
                if (Name(Child).ToLower() == ChildNameToMatch.ToLower())
                {
                    if (PosDelimiter == -1)
                        return Child;
                    else
                        return EnsureNodeExists(Child, NodePath.Substring(PosDelimiter + 1));
                }
            }

            // Didn't find the child node so add one and continue.
            XmlNode NewChild = Node.AppendChild(Node.OwnerDocument.CreateElement(ChildNameToMatch, Node.NamespaceURI));
            if (PosDelimiter == -1)
                return NewChild;
            else
                return EnsureNodeExists(NewChild, NodePath.Substring(PosDelimiter + 1));
        }

        public static bool IsEqual(XmlNode Node1, XmlNode Node2)
        {
            // Go through each attribute and each child node and make sure everything is the same.
            // By doing this, attributes and child nodes can be in different orders but this method
            // will still return true.
            if (Utility.Xml.ChildNodes(Node1, "").Count > 0)
            {
                if (Node1.Attributes.Count != Node2.Attributes.Count)
                    return false;
                foreach (XmlAttribute Attribute1 in Node1.Attributes)
                {
                    string Attribute2Value = Utility.Xml.Attribute(Node2, Attribute1.Name);
                    if (Attribute1.InnerText != Attribute2Value)
                        return false;
                }

                // Check child nodes.
                List<XmlNode> Node1Children = Utility.Xml.ChildNodes(Node1, "");
                if (Node1Children.Count != Utility.Xml.ChildNodes(Node2, "").Count)
                    return false;

                // Some child nodes need to be checked sequentially because they don't have
                // a "name" attribute e.g. <Layer> and <Script>
                string[] SequentialNodeTypes = new string[] { "Layer", "script", "operation" };

                // Perform lookup comparison for all non sequential nodes.
                foreach (XmlNode Child1 in Utility.Xml.ChildNodes(Node1, ""))
                {
                    if (Array.IndexOf(SequentialNodeTypes, Child1.Name) == -1)
                    {
                        XmlNode Child2 = Utility.Xml.ChildByNameAndType(Node2, Utility.Xml.Name(Child1), Child1.Name);
                        if (Child2 == null)
                            return false;
                        if (!Utility.Xml.IsEqual(Child1, Child2))
                            return false;
                    }
                }

                // Now go and compare all sequential node types.
                foreach (string SequentialType in SequentialNodeTypes)
                {
                    if (!Utility.Xml.IsEqualSequentially(Node1, Node2, SequentialType))
                        return false;
                }

                return true;
            }
            else
            {
                double Value1, Value2;
                if (double.TryParse(Node1.InnerText, out Value1) &&
                    double.TryParse(Node2.InnerText, out Value2))
                    return Math.FloatsAreEqual(Value1, Value2);
                else
                    return Node1.InnerText == Node2.InnerText;
            }
        }

        private static bool IsEqualSequentially(XmlNode Node1, XmlNode Node2, string ChildType)
        {
            List<XmlNode> Children1 = Utility.Xml.ChildNodes(Node1, ChildType);
            List<XmlNode> Children2 = Utility.Xml.ChildNodes(Node2, ChildType);
            if (Children1.Count != Children2.Count)
                return false;
            for (int i = 0; i < Children1.Count; i++)
            {
                if (!Utility.Xml.IsEqual(Children1[i], Children2[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Deserialise from the specified file (XML)
        /// </summary>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(string FileName)
        {
            if (!File.Exists(FileName))
                throw new Exception("Cannot deserialise from file: " + FileName + ". File does not exist.");

            XmlDocument Doc = new XmlDocument();
            Doc.Load(FileName);

            return Deserialise(Doc.DocumentElement);
        }

        /// <summary>
        /// Deserialise from the specified XmlNode.
        /// </summary>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(XmlNode Node)
        {
            XmlReader Reader = new XmlNodeReader(Node);
            Reader.Read();
            return Deserialise(Reader);
        }

        /// <summary>
        /// Deserialise from the specified XmlReader.
        /// </summary>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(XmlReader Reader)
        {
            try
            {
                object ReturnObj = null;
                string TypeName = Reader.Name;

                // Try using the pre built serialization assembly first.
                string DeserializerFileName = System.IO.Path.ChangeExtension(Assembly.GetExecutingAssembly().Location,
                                                                             ".XmlSerializers.dll");
                if (File.Exists(DeserializerFileName))
                {
                    Assembly SerialiserAssembly = Assembly.LoadFile(DeserializerFileName);
                    string SerialiserFullName = "Microsoft.Xml.Serialization.GeneratedAssembly." + TypeName + "Serializer";
                    object Serialiser = SerialiserAssembly.CreateInstance(SerialiserFullName);

                    if (Serialiser != null)
                    {
                        MethodInfo Deserialise = Serialiser.GetType().GetMethod("Deserialize", new Type[] { typeof(XmlReader) });
                        if (Deserialise != null)
                            ReturnObj = Deserialise.Invoke(Serialiser, new object[] { Reader });
                    }
                }

                // if no pre built assembly found then deserialise manually.
                if (ReturnObj == null)
                {
                    Type[] type = Utility.Reflection.GetTypeWithoutNameSpace(TypeName);
                    if (type.Length == 0)
                        throw new Exception("Cannot deserialise because type: " + TypeName + " does not exist");
                    if (type.Length > 1)
                        throw new Exception("Cannot deserialise because found two classes with class name: " + TypeName);

                    XmlSerializer serial = new XmlSerializer(type[0]);
                    ReturnObj = serial.Deserialize(Reader);
                }

                //MethodInfo OnSerialised = ReturnObj.GetType().GetMethod("OnSerialised");
                //if (OnSerialised != null)
                //    OnSerialised.Invoke(ReturnObj, null);
                return ReturnObj;
            }
            catch (Exception)
            {
                return null;  // most likely invalid xml.
            }
        }

        /// <summary>
        /// Add a child model as specified by the ModelXml. Will call ModelAdded event if successful. 
        /// </summary>
        /// <returns>Returns the full path of the added model if successful. Null otherwise.</returns>
        //public string Add(string ModelXml)
        //{
        //    try
        //    {
        //        XmlReaderSettings settings = new XmlReaderSettings();
        //        settings.ConformanceLevel = ConformanceLevel.Fragment;

        //        XmlReader Reader = XmlReader.Create(new StringReader(ModelXml), settings);
        //        while (!Reader.IsStartElement() && !Reader.EOF)
        //            Reader.Read();

        //        object NewModel = AddChild(Reader);

        //        // Make sure model has a unique name.
        //        EnsureNameIsUnique(NewModel);

        //        string ChildFullPath = FullPath + "." + Utility.Reflection.Name(NewModel);
        //        if (ModelAdded != null)
        //            ModelAdded.Invoke(ChildFullPath);
        //        return ChildFullPath;
        //    }
        //    catch (Exception)
        //    {
        //        // Most likely invalid xml.
        //    }
        //    return null;
        //}




        /// <summary>
        /// Serialises the specified component to a string. If WithNamespace is true
        /// then a full xml namespace will be written.
        /// </summary>
        public static string Serialise(object Component, bool WithNamespace)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            MemoryStream M = new MemoryStream();
            StreamWriter Writer = new StreamWriter(M);
            XmlSerializer x = new XmlSerializer(Component.GetType());
            if (WithNamespace)
                 x.Serialize(Writer, Component, ns);
            else
                x.Serialize(Writer, Component);
               
            M.Seek(0, SeekOrigin.Begin);
            StreamReader R = new StreamReader(M);
            return R.ReadToEnd();
        }
    }
}

