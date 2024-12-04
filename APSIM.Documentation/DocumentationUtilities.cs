using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using APSIM.Shared.Documentation;
using DocumentationGraphPage = APSIM.Shared.Documentation.GraphPage;

namespace APSIM.Documentation;

/// <summary>
/// Contains utility methods for Documentation.
/// </summary>
public static class DocumentationUtilities
{
    /// <summary>
    /// Gets a documentation list without empty tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static List<ITag> CleanEmptySections(List<ITag> tags)
    {
        bool blankFound = CheckForBlankTags(tags);
        while (blankFound)
        {
            List<ITag> cleanedTags = CreateListWithoutEmptyTags(tags);
            tags = cleanedTags;
            blankFound = CheckForBlankTags(tags);
        }
        return tags;
    }

    /// <summary>
    /// Cleans a list of documentation tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns>Tuple with boolean that notifies if any tags where empty and a list of tags that are not empty.</returns>
    private static List<ITag> CreateListWithoutEmptyTags(List<ITag> tags)
    {            
        List<ITag> newTags = new();
        foreach(ITag tag in tags)
        {
            if (tag is Section)
            {
                string title = (tag as Section).Title;
                List<ITag> children = (tag as Section).Children;
                if (children.Count > 0)
                {
                    children = CreateListWithoutEmptyTags(children);
                    newTags.Add(new Section(title, children));
                }
            }
            else if (tag is Paragraph paragraph)
            {
                if (!string.IsNullOrWhiteSpace(paragraph.text))
                    newTags.Add(tag);
            }
            else if (tag is DocumentationGraphPage graphPage)
            {
                if (graphPage.Graphs.Count() > 0)
                    newTags.Add(tag);
            }
            else
            {
                newTags.Add(tag);
            }
        }
        return newTags;
    }

    /// <summary>
    /// Cleans a list of documentation tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns>Tuple with boolean that notifies if any tags where empty and a list of tags that are not empty.</returns>
    private static bool CheckForBlankTags(List<ITag> tags)
    {
        bool blankFound = false;
        foreach(ITag tag in tags)
        {
            if (tag is Section)
            {
                List<ITag> children = (tag as Section).Children;
                if (children.Count > 0)
                {
                    bool result = CheckForBlankTags(children);
                    if (result == true)
                        blankFound = true;
                }
                else
                {
                    blankFound = true;
                }
            }
            else if (tag is Paragraph paragraph)
            {
                if (string.IsNullOrWhiteSpace(paragraph.text))
                    blankFound = true;
            }
            else if (tag is DocumentationGraphPage graphPage)
            {
                if (graphPage.Graphs.Count() == 0)
                    blankFound = true;
            }
        }
        return blankFound;
    }

    /// <summary>
    /// Replaces a starting section with a header, or adds a header if first tag is a paragraph
    /// </summary>
    /// <param name="title"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static List<ITag> AddHeader(string title, List<ITag> tags) {

        List<ITag> newTags = new List<ITag>();
        ITag first = tags.First();
        
        if (tags.First().GetType() == typeof(Paragraph))
        {
            newTags.Add(new Section(title, first));
            tags.Remove(first);
            newTags.AddRange(tags);
            return newTags;
        }
        else
        {
            return tags;
        }
    }

    /// <summary>
    /// Get the summary of a member (field, property)
    /// </summary>
    /// <param name="member">The member to get the summary for.</param>
    public static string GetSummary(MemberInfo member)
    {
        var fullName = member.ReflectedType + "." + member.Name;
        if (member is PropertyInfo)
            return GetSummary(fullName, 'P');
        else if (member is FieldInfo)
            return GetSummary(fullName, 'F');
        else if (member is EventInfo)
            return GetSummary(fullName, 'E');
        else if (member is MethodInfo method)
        {
            string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
            args = args.Replace("+", ".");
            return GetSummary($"{fullName}({args})", 'M');
        }
        else
            throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
    }

    /// <summary>
    /// Get the summary of a type removing CRLF.
    /// </summary>
    /// <param name="t">The type to get the summary for.</param>
    public static string GetSummary(Type t)
    {
        return GetSummary(t.FullName, 'T');
    }

    /// <summary>
    /// Get the summary of a type without removing CRLF.
    /// </summary>
    /// <param name="t">The type to get the summary for.</param>
    public static string GetSummaryRaw(Type t)
    {
        return GetSummaryRaw(t.FullName, 'T');
    }

    /// <summary>
    /// Get the remarks tag of a type (if it exists).
    /// </summary>
    /// <param name="t">The type.</param>
    public static string GetRemarks(Type t)
    {
        return GetRemarks(t.FullName, 'T');
    }

    /// <summary>
    /// Get the remarks of a member (field, property) if it exists.
    /// </summary>
    /// <param name="member">The member.</param>
    public static string GetRemarks(MemberInfo member)
    {
        var fullName = member.ReflectedType + "." + member.Name;
        if (member is PropertyInfo)
            return GetRemarks(fullName, 'P');
        else if (member is FieldInfo)
            return GetRemarks(fullName, 'F');
        else if (member is EventInfo)
            return GetRemarks(fullName, 'E');
        else if (member is MethodInfo method)
        {
            string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
            args = args.Replace("+", ".");
            return GetRemarks($"{fullName}({args})", 'M');
        }
        else
            throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
    }

    /// <summary>
    /// Get the summary of a member (class, field, property)
    /// </summary>
    /// <param name="path">The path to the member.</param>
    /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
    private static string GetSummary(string path, char typeLetter)
    {
        var rawSummary = GetSummaryRaw(path, typeLetter);
        if (rawSummary != null)
        {
            // Need to fix multiline comments - remove newlines and consecutive spaces.
            return Regex.Replace(rawSummary, @"\n[ \t]+", "\n");
        }
        return null;
    }

    /// <summary>
    /// Get the summary of a member (class, field, property)
    /// </summary>
    /// <param name="path">The path to the member.</param>
    /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
    private static string GetSummaryRaw(string path, char typeLetter)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        path = path.Replace("+", ".");

        //if (summaries.TryGetValue($"{typeLetter}:{path}", out var summary))
        //    return summary;
        return null;
    }

    /// <summary>
    /// Get the remarks of a member (class, field, property).
    /// </summary>
    /// <param name="path">The path to the member.</param>
    /// <param name="typeLetter">Type letter: 'T' for type, 'F' for field, 'P' for property.</param>
    /// <returns></returns>
    private static string GetRemarks(string path, char typeLetter)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        path = path.Replace("+", ".");

        //if (remarks.TryGetValue($"{typeLetter}:{path}", out var remark))
        //{
            // Need to fix multiline remarks - trim newlines and consecutive spaces.
        //    return Regex.Replace(remark, @"\n\s+", "\n");
        //}
        return null;
    }
}