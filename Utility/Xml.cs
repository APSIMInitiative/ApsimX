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
    /// <summary>
    /// XML utility routines
    /// </summary>
    public class Xml
    {
        /// <summary>The delimiter</summary>
        public const char Delimiter = '/';

        /// <summary>Creates the node.</summary>
        /// <param name="Document">The document.</param>
        /// <param name="Type">The type.</param>
        /// <param name="Name">The name.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Return the the value from the <code> <Name></Name> </code> element child of aNode.
        /// </summary>
        /// <param name="aNode">The base node</param>
        /// <returns>The value from the child called "Name"</returns>
        public static string NameElement(XmlNode aNode)
        {
            XmlNode nameNode = Utility.Xml.Find(aNode, "Name");
            if ( (nameNode != null) && (nameNode.InnerText.Length > 0) )
                return nameNode.InnerText;
            else
                return Type(aNode);
        }

        /// <summary>Names the attribute.</summary>
        /// <param name="Node">The node.</param>
        /// <returns></returns>
        public static string NameAttr(XmlNode Node)
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
        /// <summary>Sets the name attribute.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="Name">The name.</param>
        public static void SetNameAttr(XmlNode Node, string Name)
        {
            if (Name != Utility.Xml.NameAttr(Node))
                SetAttribute(Node, "name", Name);
        }
        /// <summary>Types the specified node.</summary>
        /// <param name="Node">The node.</param>
        /// <returns></returns>
        public static string Type(XmlNode Node)
        {
            // ---------------------------------------
            // Return the 'type' of the specified node
            // ---------------------------------------
            return Node.Name;
        }
        /// <summary>Changes the type.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NewType">The new type.</param>
        /// <returns></returns>
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

        /// <summary>Parents the specified node.</summary>
        /// <param name="Node">The node.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Return the full path of the node using the <code> <Name></Name> </code> element values.
        /// </summary>
        /// <param name="Node">Child node</param>
        /// <returns>The path name /RootNode/ParentNode/ChildNode</returns>
        public static string FullPathUsingName(XmlNode Node)
        {
            StringBuilder path = new StringBuilder();
            XmlNode LocalData = Node;
            do
            {
                path.Insert(0, Delimiter);
                path.Insert(0, NameElement(LocalData));
            } while ((LocalData = Parent(LocalData)) != null);

            return path.ToString();
        }

        /// <summary>Fulls the path.</summary>
        /// <param name="Node">The node.</param>
        /// <returns></returns>
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
                FullPath.Insert(0, NameAttr(LocalData));
            } while ((LocalData = Parent(LocalData)) != null);
            return FullPath.ToString();
        }
        /// <summary>Parents the path.</summary>
        /// <param name="NodePath">The node path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Cannot get the parent of the specified node:  + NodePath
        /// or
        /// Cannot get the parent of the root node
        /// </exception>
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
        /// <summary>Finds the specified node.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NamePath">The name path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot call FindByName with a blank path</exception>
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
                if (RootName.ToLower() != NameAttr(Node).ToLower())
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
                    if (NameAttr(Child).ToLower() == ChildName.ToLower())
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
        /// <summary>Finds the type of the by.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypePath">The type path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot call FindByType with a blank path</exception>
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
        /// <summary>Finds the recursively.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="Name">The name.</param>
        /// <returns></returns>
        public static XmlNode FindRecursively(XmlNode Node, string Name)
        {
            if (Utility.Xml.NameAttr(Node).ToLower() == Name.ToLower())
                return Node;
            foreach (XmlNode Child in Node.ChildNodes)
            {
                XmlNode Result = FindRecursively(Child, Name);
                if (Result != null)
                    return Result;
            }
            return null;
        }
        /// <summary>Finds all recursively.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="Name">The name.</param>
        /// <param name="Nodes">The nodes.</param>
        public static void FindAllRecursively(XmlNode Node, string Name, ref List<XmlNode> Nodes)
        {
            if (Utility.Xml.NameAttr(Node).ToLower() == Name.ToLower())
                Nodes.Add(Node);
            foreach (XmlNode Child in Node.ChildNodes)
                FindAllRecursively(Child, Name, ref Nodes);
        }
        /// <summary>Finds the type of all recursively by.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypeName">Name of the type.</param>
        /// <param name="Nodes">The nodes.</param>
        public static void FindAllRecursivelyByType(XmlNode Node, string TypeName, ref List<XmlNode> Nodes)
        {
            if (Utility.Xml.Type(Node).ToLower() == TypeName.ToLower())
                Nodes.Add(Node);
            foreach (XmlNode Child in Node.ChildNodes)
                FindAllRecursivelyByType(Child, TypeName, ref Nodes);
        }

        /// <summary>Childs the type of the by name and.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NameFilter">The name filter.</param>
        /// <param name="TypeFilter">The type filter.</param>
        /// <returns></returns>
        public static XmlNode ChildByNameAndType(XmlNode Node, string NameFilter, string TypeFilter)
        {
            // ----------------------------------------------------
            // Find a child with the specified name and type
            // Returns null if no child found. 
            // ----------------------------------------------------
            foreach (XmlNode Child in Node.ChildNodes)
            {
                if (NameAttr(Child).ToLower() == NameFilter.ToLower() && Type(Child).ToLower() == TypeFilter.ToLower())
                    return Child;
            }
            return null;
        }
        /// <summary>Childs the by type and value.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypeFilter">The type filter.</param>
        /// <param name="ValueFilter">The value filter.</param>
        /// <returns></returns>
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
        /// <summary>Childs the nodes.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypeFilter">The type filter.</param>
        /// <returns></returns>
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
        /// <summary>Childs the name of the nodes by.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NameFilter">The name filter.</param>
        /// <returns></returns>
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
                        NameFilter == "" || NameAttr(Child).ToLower() == NameFilter.ToLower())
                        MatchingChildren.Add(Child);
                }
            }
            return MatchingChildren;
        }
        /// <summary>Childs the names.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypeFilter">The type filter.</param>
        /// <returns></returns>
        public static string[] ChildNames(XmlNode Node, string TypeFilter)
        {
            List<XmlNode> Children = ChildNodes(Node, TypeFilter);
            string[] Names = new string[Children.Count];
            for (int i = 0; i != Children.Count; i++)
                Names[i] = NameAttr(Children[i]);
            return Names;
        }
        /// <summary>Values the specified child.</summary>
        /// <param name="Child">The child.</param>
        /// <param name="NamePath">The name path.</param>
        /// <returns></returns>
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
        /// <summary>Sets the value.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NamePath">The name path.</param>
        /// <param name="Value">The value.</param>
        public static void SetValue(XmlNode Node, string NamePath, string Value)
        {
            XmlNode ValueNode = EnsureNodeExists(Node, NamePath);
            ValueNode.InnerText = Value;
        }
        /// <summary>Valueses the specified node.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypeFilter">The type filter.</param>
        /// <returns></returns>
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
        /// <summary>Valueses the recursive.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="TypeFilter">The type filter.</param>
        /// <returns></returns>
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
        /// <summary>Sets the values.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NamePath">The name path.</param>
        /// <param name="Values">The values.</param>
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
        /// <summary>Sets the values.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NamePath">The name path.</param>
        /// <param name="Values">The values.</param>
        public static void SetValues(XmlNode Node, string NamePath, string[] Values)
        {
            List<string> Vals = new List<string>();
            Vals.AddRange(Values);
            SetValues(Node, NamePath, Vals);
        }
        /// <summary>Attributes the specified node.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="AttributeName">Name of the attribute.</param>
        /// <returns></returns>
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
        /// <summary>Sets the attribute.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="AttributeName">Name of the attribute.</param>
        /// <param name="AttributeValue">The attribute value.</param>
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
        /// <summary>Deletes the attribute.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="AttributeName">Name of the attribute.</param>
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

        /// <summary>Deletes the value.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="ValueName">Name of the value.</param>
        public static void DeleteValue(XmlNode Node, string ValueName)
        {
            // ----------------------------------------
            // Delete the specified value
            // ----------------------------------------
            XmlNode ValueNode = Find(Node, ValueName);
            if (ValueNode != null)
                ValueNode.ParentNode.RemoveChild(ValueNode);
        }

        /// <summary>Formatteds the XML.</summary>
        /// <param name="Xml">The XML.</param>
        /// <returns></returns>
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
        /// <summary>Ensures the node is unique.</summary>
        /// <param name="Node">The node.</param>
        /// <exception cref="System.Exception">Cannot find a unique name for child:  + NameAttr(Node)</exception>
        public static void EnsureNodeIsUnique(XmlNode Node)
        {
            // -------------------------------------------------------------
            // Make sure the node's name is unique amongst it's siblings.
            // -------------------------------------------------------------
            string BaseName = NameAttr(Node);
            string UniqueChildName = BaseName;
            for (int i = 1; i != 10000; i++)
            {
                int Count = 0;
                foreach (XmlNode Sibling in Node.ParentNode.ChildNodes)
                {
                    if (NameAttr(Sibling).ToLower() == UniqueChildName.ToLower())
                        Count++;
                }
                if (Count == 1)
                    return;
                UniqueChildName = BaseName + i.ToString();
                SetAttribute(Node, "name", UniqueChildName);
            }
            throw new Exception("Cannot find a unique name for child: " + NameAttr(Node));
        }
        /// <summary>Ensures the number of children.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="ChildType">Type of the child.</param>
        /// <param name="ChildName">Name of the child.</param>
        /// <param name="NumChildren">The number children.</param>
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

        /// <summary>
        /// 
        /// </summary>
        private class XmlNodeComparer : System.Collections.IComparer
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero <paramref name="x" /> is less than <paramref name="y" />. Zero <paramref name="x" /> equals <paramref name="y" />. Greater than zero <paramref name="x" /> is greater than <paramref name="y" />.
            /// </returns>
            int System.Collections.IComparer.Compare(Object x, Object y)
            {
                XmlNode yNode = (XmlNode)y;
                XmlNode xNode = (XmlNode)x;
                return ((new CaseInsensitiveComparer()).Compare(NameAttr(xNode), NameAttr(yNode)));
            }

        }
        /// <summary>Sorts the specified node.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="Comparer">The comparer.</param>
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

        /// <summary>Ensures the node exists.</summary>
        /// <param name="Node">The node.</param>
        /// <param name="NodePath">The node path.</param>
        /// <returns></returns>
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
                if (NameAttr(Child).ToLower() == ChildNameToMatch.ToLower())
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

        /// <summary>Determines whether the specified node1 is equal.</summary>
        /// <param name="Node1">The node1.</param>
        /// <param name="Node2">The node2.</param>
        /// <returns></returns>
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
                        XmlNode Child2 = Utility.Xml.ChildByNameAndType(Node2, Utility.Xml.NameAttr(Child1), Child1.Name);
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

        /// <summary>
        /// Determines whether [is equal sequentially] [the specified node1].
        /// </summary>
        /// <param name="Node1">The node1.</param>
        /// <param name="Node2">The node2.</param>
        /// <param name="ChildType">Type of the child.</param>
        /// <returns></returns>
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

        /// <summary>Deserialise from the specified file (XML)</summary>
        /// <param name="FileName">Name of the file.</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        /// <exception cref="System.Exception">Cannot deserialise from file:  + FileName + . File does not exist.</exception>
        public static object Deserialise(string FileName)
        {
            if (!File.Exists(FileName))
                throw new Exception("Cannot deserialise from file: " + FileName + ". File does not exist.");

            XmlDocument Doc = new XmlDocument();
            Doc.Load(FileName);

            return Deserialise(Doc.DocumentElement);
        }

        /// <summary>Deserialise from the specified XmlNode.</summary>
        /// <param name="Node">The node.</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(XmlNode Node)
        {
            XmlReader Reader = new XmlNodeReader(Node);
            Reader.Read();
            return Deserialise(Reader);
        }

        /// <summary>Deserialise from the specified XmlReader.</summary>
        /// <param name="Reader">The reader.</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        /// <exception cref="System.Exception">
        /// Cannot deserialise because type:  + TypeName +  does not exist
        /// or
        /// Cannot deserialise because found two classes with class name:  + TypeName
        /// </exception>
        public static object Deserialise(XmlReader Reader)
        {
            try
            {
                object ReturnObj = null;
                string TypeName = Reader.Name;
                string xsiType = Reader.GetAttribute("xsi:type");
                if (xsiType != null)
                {
                    TypeName = xsiType;
                    XmlDocument doc = new XmlDocument();
                    doc.AppendChild(doc.CreateElement(TypeName));
                    doc.DocumentElement.InnerXml = Reader.ReadInnerXml();
                    Reader = new XmlNodeReader(doc);
                    //Reader.ReadStartElement();
                }
                // Try using the pre built serialization assembly first.
                string DeserializerFileName = System.IO.Path.ChangeExtension(Assembly.GetExecutingAssembly().Location,
                                                                             ".XmlSerializers.dll");

                // Under MONO it seems that if a class is not in the serialization assembly then exception will 
                // be thrown. Under windows this doesn't happen. For now, only use the prebuilt serialisation
                // dll if on windows.
                if ((Environment.OSVersion.Platform == PlatformID.Win32NT ||
                    Environment.OSVersion.Platform == PlatformID.Win32Windows) &&
                    File.Exists(DeserializerFileName))
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
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                return null;  // most likely invalid xml.
            }
        }

        /// <summary>
        /// Add a child model as specified by the ModelXml. Will call ModelAdded event if successful.
        /// </summary>
        /// <param name="Component">The component.</param>
        /// <param name="WithNamespace">if set to <c>true</c> [with namespace].</param>
        /// <returns>Returns the full path of the added model if successful. Null otherwise.</returns>
        public static string Serialise(object Component, bool WithNamespace)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            if (WithNamespace)
                ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            else
                ns.Add("", "");

            MemoryStream M = new MemoryStream();
            StreamWriter Writer = new StreamWriter(M);

            // Try using the pre built serialization assembly first.
            string DeserializerFileName = System.IO.Path.ChangeExtension(Assembly.GetExecutingAssembly().Location,
                                                                         ".XmlSerializers.dll");

            // Under MONO it seems that if a class is not in the serialization assembly then exception will 
            // be thrown. Under windows this doesn't happen. For now, only use the prebuilt serialisation
            // dll if on windows.
            if ((Environment.OSVersion.Platform == PlatformID.Win32NT ||
                Environment.OSVersion.Platform == PlatformID.Win32Windows) &&
                File.Exists(DeserializerFileName))
            {
                Assembly SerialiserAssembly = Assembly.LoadFile(DeserializerFileName);
                string SerialiserFullName = "Microsoft.Xml.Serialization.GeneratedAssembly." + Component.GetType().Name + "Serializer";
                object Serialiser = SerialiserAssembly.CreateInstance(SerialiserFullName);

                if (Serialiser != null)
                {
                    MethodInfo Serialise = Serialiser.GetType().GetMethod("Serialize", new Type[] { typeof(StreamWriter), typeof(object), typeof(XmlSerializerNamespaces) });
                    if (Serialise != null)
                        Serialise.Invoke(Serialiser, new object[] { Writer, Component, ns });
                }
            }
            else
            {
                XmlSerializer x = new XmlSerializer(Component.GetType());
                x.Serialize(Writer, Component, ns);
            }
               
            M.Seek(0, SeekOrigin.Begin);
            StreamReader R = new StreamReader(M);
            return R.ReadToEnd();
        }
    }
}

