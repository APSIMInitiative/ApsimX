using System.Collections.Generic;
using System.Linq;
using Models;
using APSIM.Shared.Documentation;

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
        bool areSectionsExcluded = true;
        while (areSectionsExcluded)
        {
            (bool sectionsExcluded, List<ITag> cleanedTags) = CreateListWithoutEmptyTags(tags);
            areSectionsExcluded = sectionsExcluded;
            tags = cleanedTags;
        }
        return tags;
    }

    /// <summary>
    /// Cleans a list of documentation tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns>Tuple with boolean that notifies if any tags where empty and a list of tags that are not empty.</returns>
    private static (bool, List<ITag>) CreateListWithoutEmptyTags(List<ITag> tags)
    {            
        List<ITag> newTags = new();
        bool excluded = false;
        foreach(ITag tag in tags)
        {
            if (tag is Section)
            {
                string title = (tag as Section).Title;
                List<ITag> children = (tag as Section).Children;
                if (children.Count > 0)
                {
                    bool sectionsExcluded;
                    (sectionsExcluded, children) = CreateListWithoutEmptyTags(children);
                    newTags.Add(new Section(title, children));
                }
                else
                {
                    excluded = true;
                }
            }
            else if (tag is Paragraph paragraph)
            {
                if (!string.IsNullOrWhiteSpace(paragraph.text))
                    newTags.Add(tag);
                else
                    excluded = true;
            }
            else
            {
                newTags.Add(tag);
            }
        }
        return (excluded, newTags);
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
        
        if (tags.First().GetType() == typeof(Section))
        {
            newTags.Add(new Header((first as Section).Title));
            newTags.AddRange((first as Section).Children);
            tags.Remove(first);
            newTags.AddRange(tags);
            return newTags;
        }
        else if (tags.First().GetType() == typeof(Paragraph))
        {
            newTags.Add(new Header(title));
            newTags.AddRange(tags);
            return newTags;
        }
        else
        {
            return tags;
        }
    }
}