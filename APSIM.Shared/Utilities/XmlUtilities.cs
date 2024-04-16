namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// XML utility routines
    /// </summary>
    public class XmlUtilities
    {
        /// <summary>The delimiter</summary>
        public const char Delimiter = '/';

        /// <summary>Creates the node.</summary>
        /// <param name="document">The document.</param>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static XmlNode CreateNode(XmlDocument document, string type, string name)
        {
            // -----------------------------------------------------------------
            // Create a node with the specified type and name.
            // -----------------------------------------------------------------
            XmlNode NewNode = document.CreateElement(type);
            if (name != "")
			{
				SetAttribute(NewNode, "name", name);
                XmlUtilities.SetValue(NewNode, "Name", name);
            }
			return NewNode;
        }

        /// <summary>
        /// Return the the value from the <code> <Name></Name> </code> element child of aNode.
        /// </summary>
        /// <param name="aNode">The base node</param>
        /// <returns>The value from the child called "Name"</returns>
        public static string NameElement(XmlNode aNode)
        {
            XmlNode nameNode = Find(aNode, "Name");
            if ( (nameNode != null) && (nameNode.InnerText.Length > 0) )
                return nameNode.InnerText;
            else
                return Type(aNode);
        }

        /// <summary>Names the attribute.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public static string NameAttr(XmlNode node)
        {
            // --------------------------------------
            // Return the name attribute or if not 
            // found then the element name
            // i.e. 'Type'
            // --------------------------------------
            string value = Attribute(node, "name");
            if (value == "")
                return Type(node);
            else
                return value;
        }

        /// <summary>Sets the name attribute.</summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The name.</param>
        public static void SetNameAttr(XmlNode node, string name)
        {
            if (name != NameAttr(node))
                SetAttribute(node, "name", name);
        }

        /// <summary>Types the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public static string Type(XmlNode node)
        {
            // ---------------------------------------
            // Return the 'type' of the specified node
            // ---------------------------------------
            return node.Name;
        }

        /// <summary>Changes the type.</summary>
        /// <param name="node">The node.</param>
        /// <param name="newType">The new type.</param>
        /// <returns></returns>
        public static XmlNode ChangeType(XmlNode node, string newType)
        {
            if (node.ParentNode != null)
            {
                XmlNode NewNode = node.OwnerDocument.CreateElement(newType, node.NamespaceURI);

                // Move all attributes
                while (node.Attributes.Count > 0)
                    NewNode.Attributes.Append(node.Attributes.RemoveAt(0));

                // Move all child nodes
                while (node.ChildNodes.Count > 0)
                    NewNode.AppendChild(node.RemoveChild(node.ChildNodes[0]));

                node.ParentNode.ReplaceChild(NewNode, node);
                return NewNode;
            }
            return null;
        }

        /// <summary>Renames the specified child node.</summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="childName">Name of the child.</param>
        /// <param name="newName">The new name.</param>
        public static void Rename(XmlNode parentNode, string childName, string newName)
        {
            XmlNode nodeToRename = Find(parentNode, childName);
            if (nodeToRename != null)
                ChangeType(nodeToRename, newName);
        }

        /// <summary>Parents the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public static XmlNode Parent(XmlNode node)
        {
            // ---------------------------------------
            // Return the parent of the specified node
            // ---------------------------------------
            if (node.ParentNode == null || node.ParentNode.Name == "#document")
                return null;
            else
                return node.ParentNode;
        }

        /// <summary>Parents the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeName">Type name to search for.</param>
        /// <returns>Matching parent or null if none found.</returns>
        public static XmlNode ParentOfType(XmlNode node, string typeName)
        {
            XmlNode parent = Parent(node);
            while (parent != null && parent.Name != typeName)
                parent = Parent(parent);
            return parent;
        }

        /// <summary>Find a parent to base our series on.</summary>
        public static XmlNode ParentOfType(XmlNode node, string[] typeNames)
        {
            XmlNode parent = Parent(node);
            while (parent != null && Array.IndexOf(typeNames, parent.Name) == -1)
                parent = Parent(parent);
            return parent;
        }

        /// <summary>
        /// Return the full path of the node using the <code> <Name></Name> </code> element values.
        /// </summary>
        /// <param name="node">Child node</param>
        /// <returns>The path name /RootNode/ParentNode/ChildNode</returns>
        public static string FullPathUsingName(XmlNode node)
        {
            StringBuilder path = new StringBuilder();
            XmlNode LocalData = node;
            do
            {
                path.Insert(0, Delimiter);
                path.Insert(0, NameElement(LocalData));
            } while ((LocalData = Parent(LocalData)) != null);

            return path.ToString();
        }

        /// <summary>Fulls the path.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public static string FullPath(XmlNode node)
        {
            // --------------------------------------------------------
            // Return a full path for the specified node. Paths are 
            // of the form: /RootNode/ChildNode/SubChildNode
            // --------------------------------------------------------
            XmlNode LocalData = node;
            StringBuilder FullPath = new StringBuilder();
            do
            {
                FullPath.Insert(0, Delimiter);
                FullPath.Insert(0, NameAttr(LocalData));
            } while ((LocalData = Parent(LocalData)) != null);
            return FullPath.ToString();
        }

        /// <summary>Parents the path.</summary>
        /// <param name="nodePath">The node path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Cannot get the parent of the specified node:  + NodePath
        /// or
        /// Cannot get the parent of the root node
        /// </exception>
        public static string ParentPath(string nodePath)
        {
            int PosDelimiter = nodePath.LastIndexOf(Delimiter);
            if (PosDelimiter == -1)
                throw new Exception("Cannot get the parent of the specified node: " + nodePath);
            string ParentName = nodePath.Remove(PosDelimiter);
            if (ParentName == "")
                throw new Exception("Cannot get the parent of the root node");
            return ParentName;
        }

        /// <summary>Finds the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <param name="namePath">The name path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot call FindByName with a blank path</exception>
        public static XmlNode Find(XmlNode node, string namePath)
        {
            // ----------------------------------------------------
            // Find a child with the specified NamePath. NamePath
            // can be a single child name or a path delimited with
            // '/' characters e.g. ChildNode/SubChildNode or /RootNode/ChildNode
            // Returns null if no child found. 
            // ----------------------------------------------------
            if (node == null)
                return null;
            if (namePath == "")
                throw new Exception("Cannot call FindByName with a blank path");
            if (namePath[0] == Delimiter)
            {
                node = node.OwnerDocument.DocumentElement;
                int Pos = namePath.IndexOf(Delimiter, 1);
                string RootName;
                if (Pos == -1)
                {
                    RootName = namePath.Substring(1);
                    namePath = "";
                }
                else
                {
                    RootName = namePath.Substring(1, Pos - 1);
                    namePath = namePath.Substring(Pos + 1);
                }
                if (!String.Equals(RootName, NameAttr(node), StringComparison.CurrentCultureIgnoreCase))
                    return null;
                if (namePath == "")
                    return node;
            }

            string ChildName, Remainder;
            int PosDelimiter = namePath.IndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                ChildName = namePath.Substring(0, PosDelimiter);
                Remainder = namePath.Substring(PosDelimiter + 1);
            }
            else
            {
                ChildName = namePath;
                Remainder = "";
            }
            if (ChildName == "..")
                return Find(node.ParentNode, Remainder);
            else if (ChildName.Length > 0 && ChildName[0] == '@')
            {
                return node.Attributes[ChildName.Substring(1)];
            }
            else
            {
                foreach (XmlNode Child in node.ChildNodes)
                {
                    if (String.Equals(NameAttr(Child), ChildName, StringComparison.CurrentCultureIgnoreCase))
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
        /// <param name="node">The node.</param>
        /// <param name="typePath">The type path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot call FindByType with a blank path</exception>
        public static XmlNode FindByType(XmlNode node, string typePath)
        {
            // ----------------------------------------------------
            // Find a child with the specified TypePath. TypePath
            // can be a single child type or a path delimited with
            // '/' characters e.g. ChildNode/SubChildNode or /RootNode/ChildNode
            // Returns null if no child found. 
            // ----------------------------------------------------
            if (node == null)
                return null;
            if (typePath == "")
                throw new Exception("Cannot call FindByType with a blank path");
            if (typePath[0] == Delimiter)
            {
                node = node.OwnerDocument.DocumentElement;
                int Pos = typePath.IndexOf(Delimiter, 1);
                string RootName = typePath.Substring(1, Pos - 1);
                if (!String.Equals(RootName, Type(node), StringComparison.CurrentCultureIgnoreCase))
                    return null;
                typePath = typePath.Substring(Pos + 1);
            }

            string ChildType, Remainder;
            int PosDelimiter = typePath.IndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                ChildType = typePath.Substring(0, PosDelimiter);
                Remainder = typePath.Substring(PosDelimiter + 1);
            }
            else
            {
                ChildType = typePath;
                Remainder = "";
            }
            foreach (XmlNode Child in node.ChildNodes)
            {
                if (String.Equals(Type(Child), ChildType, StringComparison.CurrentCultureIgnoreCase))
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
        /// <param name="node">The node.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static XmlNode FindRecursively(XmlNode node, string name)
        {
            if (String.Equals(NameAttr(node), name, StringComparison.CurrentCultureIgnoreCase))
                return node;
            foreach (XmlNode Child in node.ChildNodes)
            {
                XmlNode Result = FindRecursively(Child, name);
                if (Result != null)
                    return Result;
            }
            return null;
        }

        /// <summary>Finds all recursively.</summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The name.</param>
        /// <param name="nodes">The nodes.</param>
        public static void FindAllRecursively(XmlNode node, string name, ref List<XmlNode> nodes)
        {
            if (String.Equals(NameAttr(node), name, StringComparison.CurrentCultureIgnoreCase))
                nodes.Add(node);
            foreach (XmlNode Child in node.ChildNodes)
                FindAllRecursively(Child, name, ref nodes);
        }

        /// <summary>Finds all XML nodes of the specified type (recursively).</summary>
        /// <param name="node">The node. to search</param>
        /// <param name="typeName">Name of the type.</param>
        public static List<XmlNode> FindAllRecursivelyByType(XmlNode node, string typeName)
        {
            List<XmlNode> matches = new List<XmlNode>();
            FindAllRecursivelyByType(node, typeName, ref matches);
            return matches;
        }

        /// <summary>Finds all XML nodes of the specified type (recursively).</summary>
        /// <param name="node">The node. to search</param>
        /// <param name="typeNames">Types to look for</param>
        public static List<XmlNode> FindAllRecursivelyByTypes(XmlNode node, string[] typeNames)
        {
            List<XmlNode> matches = new List<XmlNode>();
            foreach (string typeName in typeNames)
                FindAllRecursivelyByType(node, typeName, ref matches);
            return matches;
        }

        /// <summary>Finds the type of all recursively by.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="nodes">The nodes.</param>
        public static void FindAllRecursivelyByType(XmlNode node, string typeName, ref List<XmlNode> nodes)
        {
            if (String.Equals(Type(node), typeName, StringComparison.CurrentCultureIgnoreCase))
                nodes.Add(node);
            foreach (XmlNode Child in node.ChildNodes)
                FindAllRecursivelyByType(Child, typeName, ref nodes);
        }

        /// <summary>Tests if an XML node is of a given type.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeName">Name of the type.</param>
        public static bool IsType(XmlNode node, string typeName)
        {
            if (String.Equals(Type(node), typeName, StringComparison.CurrentCultureIgnoreCase))
                return true;
            else
                return false;
        }

        /// <summary>Childs the type of the by name and.</summary>
        /// <param name="node">The node.</param>
        /// <param name="nameFilter">The name filter.</param>
        /// <param name="typeFilter">The type filter.</param>
        /// <returns></returns>
        public static XmlNode ChildByNameAndType(XmlNode node, string nameFilter, string typeFilter)
        {
            // ----------------------------------------------------
            // Find a child with the specified name and type
            // Returns null if no child found. 
            // ----------------------------------------------------
            foreach (XmlNode Child in node.ChildNodes)
            {
                if (String.Equals(NameAttr(Child), nameFilter, StringComparison.CurrentCultureIgnoreCase) && 
                    String.Equals(Type(Child), typeFilter, StringComparison.CurrentCultureIgnoreCase))
                    return Child;
            }
            return null;
        }

        /// <summary>Childs the by type and value.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">The type filter.</param>
        /// <param name="valueFilter">The value filter.</param>
        /// <returns></returns>
        public static XmlNode ChildByTypeAndValue(XmlNode node, string typeFilter, string valueFilter)
        {
            // ----------------------------------------------------
            // Find a child with the specified Type and value. 
            // Returns null if no child found. 
            // ----------------------------------------------------
            foreach (XmlNode Child in node.ChildNodes)
            {
                if (String.Equals(Type(Child), typeFilter, StringComparison.CurrentCultureIgnoreCase) && Child.InnerText == valueFilter)
                    return Child;
            }
            return null;
        }

        /// <summary>Finds all direct children of the specified node</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">The type filter. Can be null for all children.</param>
        /// <returns></returns>
        public static List<XmlNode> ChildNodes(XmlNode node, string typeFilter)
        {
            List<XmlNode> MatchingChildren = new List<XmlNode>();
            if (node != null)
            {
                foreach (XmlNode Child in node.ChildNodes)
                {
                    if (Child.Name != "#text" && Child.Name != "#comment" && Child.Name != "#cdata-section" &&
                        typeFilter == null || typeFilter == "" || String.Equals(Type(Child), typeFilter, StringComparison.CurrentCultureIgnoreCase))
                        MatchingChildren.Add(Child);
                }
            }
            return MatchingChildren;
        }

        /// <summary>Finds all direct and non direct children of the specified node</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">The type filter. Can be null for all children.</param>
        /// <returns></returns>
        public static List<XmlNode> ChildNodesRecursively(XmlNode node, string typeFilter)
        {
            List<XmlNode> matchingChildren = new List<XmlNode>();
            if (node != null)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name != "#text" && child.Name != "#comment" && child.Name != "#cdata-section" &&
                        (typeFilter == null || typeFilter == "" || String.Equals(Type(child), typeFilter, StringComparison.CurrentCultureIgnoreCase)))
                        matchingChildren.Add(child);

                    matchingChildren.AddRange(ChildNodesRecursively(child, typeFilter));
                }
            }
            return matchingChildren;
        }

        /// <summary>Return an array of children that match the specified filter.</summary>
        /// <param name="node">The node.</param>
        /// <param name="nameFilter">The name filter.</param>
        /// <returns></returns>
        public static List<XmlNode> ChildNodesByName(XmlNode node, string nameFilter)
        {
            // ----------------------------------------------------
            // Return an array of children that match the specified
            // filter. The filter can be an empty string to match
            // all child XmlNodes
            // ----------------------------------------------------
            List<XmlNode> MatchingChildren = new List<XmlNode>();
            if (node != null)
            {
                foreach (XmlNode Child in node.ChildNodes)
                {
                    if (Child.Name != "#text" && Child.Name != "#comment" && Child.Name != "#cdata-section" &&
                        nameFilter == "" || String.Equals(NameAttr(Child), nameFilter, StringComparison.CurrentCultureIgnoreCase))
                        MatchingChildren.Add(Child);
                }
            }
            return MatchingChildren;
        }

        /// <summary>Return an array of the names of children of the given node.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">The type filter.</param>
        /// <returns></returns>
        public static string[] ChildNames(XmlNode node, string typeFilter)
        {
            List<XmlNode> Children = ChildNodes(node, typeFilter);
            string[] Names = new string[Children.Count];
            for (int i = 0; i != Children.Count; i++)
                Names[i] = NameAttr(Children[i]);
            return Names;
        }

        /// <summary>Return the Value of the given node.</summary>
        /// <param name="child">The child.</param>
        /// <param name="namePath">The name path.</param>
        /// <returns></returns>
        public static string Value(XmlNode child, string namePath)
        {
            XmlNode FoundNode;
            if (namePath == "")
                FoundNode = child;
            else
                FoundNode = Find(child, namePath);
            if (FoundNode != null)
                return FoundNode.InnerText;
            else
                return "";
        }

        /// <summary>Sets the value.</summary>
        /// <param name="node">The node.</param>
        /// <param name="namePath">The name path.</param>
        /// <param name="value">The value.</param>
        public static void SetValue(XmlNode node, string namePath, string value)
        {
            XmlNode ValueNode = EnsureNodeExists(node, namePath);
            ValueNode.InnerText = value;
        }

        /// <summary>Return a list of Values for all children of a given node.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">The type filter.</param>
        /// <returns></returns>
        public static List<string> Values(XmlNode node, string typeFilter)
        {
            int PosDelimiter = typeFilter.LastIndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                node = Find(node, typeFilter.Substring(0, PosDelimiter));
                typeFilter = typeFilter.Substring(PosDelimiter + 1);
            }

            List<string> ReturnValues = new List<string>();
            foreach (XmlNode Child in ChildNodes(node, typeFilter))
                ReturnValues.Add(Child.InnerText);
            return ReturnValues;
        }

        /// <summary>Return a list of Values for all descendants of a given node.</summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">The type filter.</param>
        /// <returns></returns>
        public static List<string> ValuesRecursive(XmlNode node, string typeFilter)
        {
            int PosDelimiter = typeFilter.LastIndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                node = Find(node, typeFilter.Substring(0, PosDelimiter));
                typeFilter = typeFilter.Substring(PosDelimiter + 1);
            }

            List<string> ReturnValues = new List<string>();
            foreach (XmlNode Child in ChildNodes(node, ""))
            {
                if (Child.Name == typeFilter)
                    ReturnValues.Add(Child.InnerText);
                ReturnValues.AddRange(ValuesRecursive(Child, typeFilter)); // recursion
            }
            return ReturnValues;
        }

        /// <summary>Sets the values.</summary>
        /// <param name="node">The node.</param>
        /// <param name="namePath">The name path.</param>
        /// <param name="values">The values.</param>
        public static void SetValues(XmlNode node, string namePath, List<string> values)
        {
            int PosDelimiter = namePath.LastIndexOf(Delimiter);
            if (PosDelimiter != -1)
            {
                node = Find(node, namePath.Substring(0, PosDelimiter));
                namePath = namePath.Substring(PosDelimiter + 1);
            }

            EnsureNumberOfChildren(node, namePath, "", values.Count);

            int i = 0;
            foreach (XmlNode Child in ChildNodes(node, namePath))
            {
                SetValue(Child, "", values[i]);
                i++;
            }
        }

        /// <summary>Sets the values.</summary>
        /// <param name="node">The node.</param>
        /// <param name="namePath">The name path.</param>
        /// <param name="values">The values.</param>
        public static void SetValues(XmlNode node, string namePath, string[] values)
        {
            List<string> Vals = new List<string>();
            Vals.AddRange(values);
            SetValues(node, namePath, Vals);
        }

        /// <summary>Attributes the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns></returns>
        public static string Attribute(XmlNode node, string attributeName)
        {
            // -----------------------------------------------------------------
            // Return the specified attribute or "" if not found
            // -----------------------------------------------------------------
            if (node.Attributes != null)
            {
                foreach (XmlAttribute A in node.Attributes)
                {
                    if (string.Equals(A.Name, attributeName, StringComparison.CurrentCultureIgnoreCase))
                        return A.Value;
                }
            }
            return "";
        }

        /// <summary>Sets the attribute.</summary>
        /// <param name="node">The node.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public static void SetAttribute(XmlNode node, string attributeName, string attributeValue)
        {
            int posLastDelimiter = attributeName.LastIndexOf(Delimiter);
            if (posLastDelimiter != -1)
            {
                string attributePath = attributeName.Substring(0, posLastDelimiter);
                string newAttributeName = attributeName.Substring(posLastDelimiter + 1);
                XmlNode attributeNode = EnsureNodeExists(node, attributePath);
                SetAttribute(attributeNode, newAttributeName, attributeValue);
            }
            else if (Attribute(node, attributeName) != attributeValue)
            {
                XmlNode attr = node.OwnerDocument.CreateNode(XmlNodeType.Attribute, attributeName, "");
                attr.Value = attributeValue;
                node.Attributes.SetNamedItem(attr);
            }
        }

        /// <summary>Deletes the attribute.</summary>
        /// <param name="node">The node.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        public static void DeleteAttribute(XmlNode node, string attributeName)
        {
            // ----------------------------------------
            // Delete the specified attribute
            // ----------------------------------------
            XmlAttribute A = (XmlAttribute)node.Attributes.GetNamedItem(attributeName);
            if (A != null)
            {
                node.Attributes.Remove(A);
            }
        }

        /// <summary>Deletes the value.</summary>
        /// <param name="node">The node.</param>
        /// <param name="valueName">Name of the value.</param>
        public static void DeleteValue(XmlNode node, string valueName)
        {
            // ----------------------------------------
            // Delete the specified value
            // ----------------------------------------
            XmlNode ValueNode = Find(node, valueName);
            if (ValueNode != null)
                ValueNode.ParentNode.RemoveChild(ValueNode);
        }

        /// <summary>Formatteds the XML.</summary>
        /// <param name="xml">The XML.</param>
        /// <returns></returns>
        public static string FormattedXML(string xml)
        {
            // -------------------------------------------------
            // Format the specified XML using indentation etc.
            // -------------------------------------------------
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml("<dummy>" + xml + "</dummy>");
            StringWriter TextWriter = new StringWriter();
            XmlTextWriter Out = new XmlTextWriter(TextWriter);
            Out.Formatting = Formatting.Indented;
            Doc.DocumentElement.WriteContentTo(Out);
            return TextWriter.ToString();
        }

        /// <summary>Ensures the node is unique.</summary>
        /// <param name="node">The node.</param>
        /// <exception cref="System.Exception">Cannot find a unique name for child:  + NameAttr(Node)</exception>
        public static void EnsureNodeIsUnique(XmlNode node)
        {
            // -------------------------------------------------------------
            // Make sure the node's name is unique amongst it's siblings.
            // -------------------------------------------------------------
            string BaseName = NameAttr(node);
            string UniqueChildName = BaseName;
            for (int i = 1; i != 10000; i++)
            {
                int Count = 0;
                foreach (XmlNode Sibling in node.ParentNode.ChildNodes)
                {
                    if (String.Equals(NameAttr(Sibling), UniqueChildName, StringComparison.CurrentCultureIgnoreCase))
                        Count++;
                }
                if (Count == 1)
                    return;
                UniqueChildName = BaseName + i.ToString();
                SetAttribute(node, "name", UniqueChildName);
            }
            throw new Exception("Cannot find a unique name for child: " + NameAttr(node));
        }

        /// <summary>Ensures the number of children.</summary>
        /// <param name="node">The node.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="childName">Name of the child.</param>
        /// <param name="numChildren">The number children.</param>
        public static void EnsureNumberOfChildren(XmlNode node, string childType, string childName, int numChildren)
        {
            // -------------------------------------------------------------------------
            // Ensure there are the specified number of children with the speciifed type
            // -------------------------------------------------------------------------
            string[] ChildrenNames = ChildNames(node, childType);
            int NumChildrenToAdd = numChildren - ChildrenNames.Length;
            int NumChildrenToDelete = ChildrenNames.Length - numChildren;
            for (int i = 1; i <= NumChildrenToAdd; i++)
                if (node!= null)
                    node.AppendChild(CreateNode(node.OwnerDocument, childType, childName));

            if (NumChildrenToDelete > 0)
            {
                List<XmlNode> ChildsToDelete = ChildNodes(node, childType);
                ChildsToDelete.RemoveRange(0, ChildsToDelete.Count - NumChildrenToDelete);
                foreach (XmlNode ChildToDelete in ChildsToDelete)
                    node.RemoveChild(ChildToDelete);
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
        /// <param name="node">The node.</param>
        /// <param name="comparer">The comparer.</param>
        public static void Sort(XmlNode node, IComparer comparer)
        {
            XmlNode[] SortedNodes = new XmlNode[node.ChildNodes.Count];
            for (int i = 0; i != node.ChildNodes.Count; i++)
            {
                SortedNodes[i] = node.ChildNodes[i];
            }
            if (comparer == null)
                Array.Sort(SortedNodes, new XmlNodeComparer());
            else
                Array.Sort(SortedNodes, comparer);
            foreach (XmlNode Child in ChildNodes(node, ""))
                Child.ParentNode.RemoveChild(Child);
            foreach (XmlNode Child in SortedNodes)
                node.AppendChild(Child);
        }

        /// <summary>Ensures the node exists.</summary>
        /// <param name="node">The node.</param>
        /// <param name="nodePath">The node path.</param>
        /// <returns></returns>
        public static XmlNode EnsureNodeExists(XmlNode node, string nodePath)
        {
            // --------------------------------------------------------
            // Ensure a node exists by creating nodes as necessary
            // for the specified node path.
            // --------------------------------------------------------

            if (nodePath.Length == 0)
                return node;

            int PosDelimiter = nodePath.IndexOf(Delimiter);
            string ChildNameToMatch = nodePath;
            if (PosDelimiter != -1)
                ChildNameToMatch = nodePath.Substring(0, PosDelimiter);

            foreach (XmlNode Child in node.ChildNodes)
            {
                if (String.Equals(NameAttr(Child), ChildNameToMatch, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (PosDelimiter == -1)
                        return Child;
                    else
                        return EnsureNodeExists(Child, nodePath.Substring(PosDelimiter + 1));
                }
            }

            // Didn't find the child node so add one and continue.
            XmlNode NewChild = node.AppendChild(node.OwnerDocument.CreateElement(ChildNameToMatch, node.NamespaceURI));
            if (PosDelimiter == -1)
                return NewChild;
            else
                return EnsureNodeExists(NewChild, nodePath.Substring(PosDelimiter + 1));
        }

        /// <summary>Determines whether the specified node1 is equal.</summary>
        /// <param name="node1">The node1.</param>
        /// <param name="node2">The node2.</param>
        /// <returns></returns>
        public static bool IsEqual(XmlNode node1, XmlNode node2)
        {
            // Go through each attribute and each child node and make sure everything is the same.
            // By doing this, attributes and child nodes can be in different orders but this method
            // will still return true.
            if (ChildNodes(node1, "").Count > 0)
            {
                if (node1.Attributes.Count != node2.Attributes.Count)
                    return false;
                foreach (XmlAttribute Attribute1 in node1.Attributes)
                {
                    string Attribute2Value = Attribute(node2, Attribute1.Name);
                    if (Attribute1.InnerText != Attribute2Value)
                        return false;
                }

                // Check child nodes.
                List<XmlNode> Node1Children = ChildNodes(node1, "");
                if (Node1Children.Count != ChildNodes(node2, "").Count)
                    return false;

                // Some child nodes need to be checked sequentially because they don't have
                // a "name" attribute e.g. <Layer> and <Script>
                string[] SequentialNodeTypes = new string[] { "Layer", "script", "operation" };

                // Perform lookup comparison for all non sequential nodes.
                foreach (XmlNode Child1 in ChildNodes(node1, ""))
                {
                    if (Array.IndexOf(SequentialNodeTypes, Child1.Name) == -1)
                    {
                        XmlNode Child2 = ChildByNameAndType(node2, NameAttr(Child1), Child1.Name);
                        if (Child2 == null)
                            return false;
                        if (!IsEqual(Child1, Child2))
                            return false;
                    }
                }

                // Now go and compare all sequential node types.
                foreach (string SequentialType in SequentialNodeTypes)
                {
                    if (!IsEqualSequentially(node1, node2, SequentialType))
                        return false;
                }

                return true;
            }
            else
            {
                double Value1, Value2;
                if (double.TryParse(node1.InnerText, out Value1) &&
                    double.TryParse(node2.InnerText, out Value2))
                    return MathUtilities.FloatsAreEqual(Value1, Value2);
                else
                    return node1.InnerText == node2.InnerText;
            }
        }

        /// <summary>
        /// Determines whether [is equal sequentially] [the specified node1].
        /// </summary>
        /// <param name="node1">The node1.</param>
        /// <param name="node2">The node2.</param>
        /// <param name="childType">Type of the child.</param>
        /// <returns></returns>
        private static bool IsEqualSequentially(XmlNode node1, XmlNode node2, string childType)
        {
            List<XmlNode> Children1 = ChildNodes(node1, childType);
            List<XmlNode> Children2 = ChildNodes(node2, childType);
            if (Children1.Count != Children2.Count)
                return false;
            for (int i = 0; i < Children1.Count; i++)
            {
                if (!IsEqual(Children1[i], Children2[i]))
                    return false;
            }
            return true;
        }

        /// <summary>Deserialise from the specified file (XML)</summary>
        /// <param name="inStream">An input stream.</param>
        /// <param name="assembly">The assembly to search for types</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(Stream inStream, Assembly assembly)
        {
            XmlDocument Doc = new XmlDocument();
            Doc.Load(inStream);

            return Deserialise(Doc.DocumentElement, assembly);
        }

        /// <summary>Deserialise from the specified file (XML)</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="assembly">The assembly to search for types</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        /// <exception cref="System.Exception">Cannot deserialise from file:  + FileName + . File does not exist.</exception>
        public static object Deserialise(string fileName, Assembly assembly)
        {
            if (!File.Exists(fileName))
                throw new Exception("Cannot deserialise from file: " + fileName + ". File does not exist.");

            XmlDocument Doc = new XmlDocument();
            Doc.Load(fileName);

            return Deserialise(Doc.DocumentElement, assembly);
        }

        /// <summary>Deserialise from the specified XmlNode.</summary>
        /// <param name="node">The node.</param>
        /// <param name="assembly">The assembly to search for types</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(XmlNode node, Assembly assembly)
        {
            XmlReader Reader = new XmlNodeReader(node);
            Reader.Read();
            return Deserialise(Reader, assembly);
        }

        /// <summary>Deserialise from the specified XmlNode.</summary>
        /// <param name="node">The node.</param>
        /// <param name="t">The type to deserialise</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        public static object Deserialise(XmlNode node, Type t)
        {
            XmlReader Reader = new XmlNodeReader(node);
            XmlSerializer serial = new XmlSerializer(t);
            return serial.Deserialize(Reader);
        }

        /// <summary>Deserialise from the specified XmlReader.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="assembly">The assembly to search for types</param>
        /// <returns>Returns the newly created object or null if not found.</returns>
        /// <exception cref="System.Exception">
        /// Cannot deserialise because type:  + TypeName +  does not exist
        /// or
        /// Cannot deserialise because found two classes with class name:  + TypeName
        /// </exception>
        public static object Deserialise(XmlReader reader, Assembly assembly)
        {
            object ReturnObj = null;
            string TypeName = reader.Name;
            string xsiType = reader.GetAttribute("xsi:type");
            if (xsiType != null)
            {
                TypeName = xsiType;
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement(TypeName));
                doc.DocumentElement.InnerXml = reader.ReadInnerXml();
                reader = new XmlNodeReader(doc);
            }
            // Try using the pre built serialization assembly first.
            string deserializerFileName = Path.ChangeExtension(assembly.Location, 
                                                               ".XmlSerializers.dll");

            // Under MONO it seems that if a class is not in the serialization assembly then exception will 
            // be thrown. Under windows this doesn't happen. For now, only use the prebuilt serialisation
            // dll if on windows.
            if (File.Exists(deserializerFileName))
            {
                Assembly SerialiserAssembly = Assembly.LoadFile(deserializerFileName);
                string SerialiserFullName = "Microsoft.Xml.Serialization.GeneratedAssembly." + TypeName + "Serializer";
                object Serialiser = SerialiserAssembly.CreateInstance(SerialiserFullName);

                if (Serialiser != null)
                {
                    MethodInfo Deserialise = Serialiser.GetType().GetMethod("Deserialize", new Type[] { typeof(XmlReader) });
                    if (Deserialise != null)
                    {
                        try
                        {
                            ReturnObj = Deserialise.Invoke(Serialiser, new object[] { reader });
                        }
                        catch (System.Reflection.TargetInvocationException except)
                        {
                            throw except.InnerException;
                        }
                        catch
                        {
                            ReturnObj = null;
                        }
                    }
                }
            }

            // if no pre built assembly found then deserialise manually.
            if (ReturnObj == null)
            {
                Type[] type = ReflectionUtilities.GetTypeWithoutNameSpace(TypeName, assembly);
                if (type.Length == 0)
                    throw new Exception("Cannot deserialise because type: " + TypeName + " does not exist");
                if (type.Length > 1)
                    throw new Exception("Cannot deserialise because found two classes with class name: " + TypeName);

                XmlSerializer serial = new XmlSerializer(type[0]);
                ReturnObj = serial.Deserialize(reader);
            }

            return ReturnObj;
        }


        private sealed class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding encoding;

            public StringWriterWithEncoding(Encoding encoding)
            {
                this.encoding = encoding;
            }

            public override Encoding Encoding
            {
                get { return encoding; }
            }
        }

        /// <summary>
        /// Serialise component
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="withNamespace">if set to <c>true</c> [with namespace].</param>
        /// <param name="extraTypes">Optional extra types.</param>
        /// <param name="deserializerFileName">Pre-compiled deserialiser file name</param>
        /// <returns>Returns the full path of the added model if successful. Null otherwise.</returns>
        public static string Serialise(object component, bool withNamespace, string deserializerFileName = null, Type[] extraTypes = null)
        {
            StringWriterWithEncoding s = new StringWriterWithEncoding(Encoding.UTF8);
            XmlTextWriter writer = new XmlTextWriter(s);
            writer.Formatting = Formatting.Indented;

            if (deserializerFileName == null)
                deserializerFileName = System.IO.Path.ChangeExtension(Assembly.GetCallingAssembly().Location,
                                                                      ".XmlSerializers.dll");
            SerialiseWithOptions(component, withNamespace, deserializerFileName, extraTypes, writer);

            return s.ToString();
        }

        /// <summary>
        /// Serialise the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="withNamespace">if set to <c>true</c> [with namespace].</param>
        /// <param name="extraTypes">Optional extra types.</param>
        /// <param name="deserializerFileName">Pre-compiled deserialiser file name</param>
        /// <param name="writer">The writer to use.</param>
        public static void SerialiseWithOptions(object component, bool withNamespace, string deserializerFileName = null, Type[] extraTypes = null, XmlTextWriter writer = null)
        {
            // Try using the pre built serialization assembly first.
            if (deserializerFileName == null)
                deserializerFileName = System.IO.Path.ChangeExtension(Assembly.GetCallingAssembly().Location,
                                                                      ".XmlSerializers.dll");

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            if (withNamespace)
                ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            else
                ns.Add("", "");

            // Under MONO it seems that if a class is not in the serialization assembly then exception will 
            // be thrown. Under windows this doesn't happen. For now, only use the prebuilt serialisation
            // dll if on windows.
            object Serialiser = null;
            if (File.Exists(deserializerFileName))
            {
                Assembly SerialiserAssembly = Assembly.LoadFile(deserializerFileName);
                string SerialiserFullName = "Microsoft.Xml.Serialization.GeneratedAssembly." + component.GetType().Name + "Serializer";
                Serialiser = SerialiserAssembly.CreateInstance(SerialiserFullName);

                if (Serialiser != null)
                {
                    MethodInfo Serialise = Serialiser.GetType().GetMethod("Serialize", new Type[] { typeof(XmlTextWriter), typeof(object), typeof(XmlSerializerNamespaces) });
                    if (Serialise != null)
                    {
                        try
                        {
                            Serialise.Invoke(Serialiser, new object[] { writer, component, ns });
                        }
                        catch
                        {
                            Serialiser = null;
                        }
                    }
                }
            }
            if (Serialiser == null)
            {
                XmlSerializer x = new XmlSerializer(component.GetType(), extraTypes);
                x.Serialize(writer, component, ns);
            }
        }

        /// <summary>
        /// Serialise component as unicode
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="withNamespace">if set to <c>true</c> [with namespace].</param>
        /// <param name="extraTypes">Optional extra types.</param>
        /// <param name="deserializerFileName">Pre-compiled deserialiser file name</param>
        /// <returns>Returns the full path of the added model if successful. Null otherwise.</returns>
        public static string SerialiseUnicode(object component, bool withNamespace, string deserializerFileName = null, Type[] extraTypes = null)
        {
            StringWriterWithEncoding s = new StringWriterWithEncoding(Encoding.Unicode);
            XmlTextWriter writer = new XmlTextWriter(s);
            writer.Formatting = Formatting.Indented;

            if (deserializerFileName == null)
                deserializerFileName = System.IO.Path.ChangeExtension(Assembly.GetCallingAssembly().Location,
                                                                      ".XmlSerializers.dll");
            SerialiseWithOptions(component, withNamespace, deserializerFileName, extraTypes, writer);

            return s.ToString();
        }

        /// <summary>Clones the specified object.</summary>
        /// <param name="obj">The object to clone</param>
        /// <returns>The newly created object</returns>
        public static object Clone(object obj)
        {
            string xml = Serialise(obj, true);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return Deserialise(doc.DocumentElement, obj.GetType());

        }

        /// <summary>Moves the specified value from parent to a new parent node.</summary>
        /// <param name="fromParent">From parent.</param>
        /// <param name="fromPath">From path.</param>
        /// <param name="toParent">To parent.</param>
        /// <param name="toPath">To path.</param>
        public static void Move(XmlNode fromParent, string fromPath, XmlNode toParent, string toPath)
        {
            string value = XmlUtilities.Value(fromParent, fromPath);
            if (value != string.Empty)
            {
                XmlUtilities.SetValue(toParent, toPath, value);
                DeleteValue(fromParent, fromPath);
            }
        }

        /// <summary>Helper class to ignore namespaces when de-serializing</summary>
        public class NamespaceIgnorantXmlTextReader : XmlTextReader
        {
            /// <summary>Constructor</summary>
            /// <param name="reader">The text reader.</param>
            public NamespaceIgnorantXmlTextReader(TextReader reader) : base(reader) { }

            /// <summary>Override the namespace.</summary>
            public override string NamespaceURI
            {
                get { return ""; }
            }
        }
    }
}

