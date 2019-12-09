// -----------------------------------------------------------------------
// <copyright file="Shortcuts.cs" company="APSIM Initiative">
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
    /// A class to remove the old style APSIM shortcuts.
    /// </summary>
    public class Shortcuts
    {
        /// <summary>
        /// Removes all shortcuts from the specified node and all child nodes.
        /// </summary>
        /// <param name="node">The node to remove shortcuts from.</param>
        /// <returns>The XML node with all shortcuts resolved.</returns>
        public static void Remove(XmlNode node)
        {
            string shortcut = XmlUtilities.Attribute(node, "shortcut");
            if (shortcut != string.Empty)
                ResolveShortcut(node);

            foreach (XmlNode child in node.ChildNodes)
                Remove(child);   // recursion
        }

        /// <summary>
        /// Resolves the shortcut on the specified node. i.e. makes concrete
        /// </summary>
        /// <param name="node">The node to remove the shortcut from.</param>
        /// <exception cref="System.Exception">Cannot find shortcut:  + shortcut</exception>
        private static void ResolveShortcut(XmlNode node)
        {
            string shortcut = XmlUtilities.Attribute(node, "shortcut");
            XmlNode concreteNode = XmlUtilities.Find(node.OwnerDocument.DocumentElement, shortcut);

            if (concreteNode == null)
                throw new Exception("Cannot find shortcut: " + shortcut);

            if (!string.IsNullOrWhiteSpace(XmlUtilities.Attribute(concreteNode, "shortcut")))
            {
                // If this happens, the node we are linked to is a shortcut
                // itself and is further down in the simulations tree than the
                // current node. In this scenario, we want to resolve the link
                // and remove the shortcut attribute so we don't resolve the
                // shortcut a second time in the Remove() method.
                ResolveShortcut(concreteNode);
                XmlUtilities.DeleteAttribute(concreteNode, "shortcut");
            }

            foreach (XmlNode child in concreteNode.ChildNodes)
            {
                // Get the 'name' of the concrete child
                string childName = XmlUtilities.NameAttr(child);
                if (childName == string.Empty)
                    childName = child.Name;

                // See if we have a node under shortcutted node with same name as concrete node.
                XmlNode nodeToReplace = XmlUtilities.Find(node, childName);
                if (nodeToReplace == null)
                    node.AppendChild(child.Clone());
                else
                {
                    // Only replace non shortcutted child nodes.
                    if (XmlUtilities.Attribute(nodeToReplace, "shortcut") == string.Empty)
                        nodeToReplace.ParentNode.ReplaceChild(child.Clone(), nodeToReplace);
                }
            }
        }

    }
}
