using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using APSIM.Documentation.Models.Types;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;
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
        if (tags.Count == 0)
        {
            return tags;
        }
        
        ITag first = tags.First();
        
        if (tags.First().GetType() == typeof(Paragraph))
        {
            newTags.Add(new Section(title, first));
            tags.Remove(first);
            newTags.AddRange(tags);
            return newTags;
        }
        else if (tags.First().GetType() == typeof(Section))
        {
            (first as Section).Title = title;
            return tags;
        }
        else
        {
            return tags;
        }
    }

    /// <summary>
    /// Gets the name of the documentation based on the provided model, with rules for Simulations and models
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public static string GetDocumentationName(IModel model) {

        if (model is Plant)
            return model.Name;
        else if (model is Simulations)
        {
            return DocSimulations.GetSimulationsName(model as Simulations);
        }
        else 
        {
            string name = model.Name;
            if (model.Name != model.GetType().Name)
                name = $"{model.Name} ({model.GetType().Name})";
            return name;
        }
    }

    /// <summary>
    /// Given a type, returns a string of the file path instead of the namespace.
    /// This is only a temporary patch until namespaces can be fixed, and more fixes will need to be added as required.
    /// </summary>
    public static string GetFilepathOfNamespace(Type type)
    {
        string path = type.ToString();
        path = path.Replace("Models.PMF.Phen.", "Models.PMF.Phenology.");

        if (path.StartsWith("Models.PMF.") && path.EndsWith("Arbitrator"))
            path = path.Replace("Models.PMF.", "Models.PMF.Arbitrator.");

        if (path.StartsWith("Models.PMF.") && path.EndsWith("Phase"))
            path = path.Replace("Models.PMF.Phenology.", "Models.PMF.Phenology.Phases.");

        return path;
    }

    /// <summary>
    /// Returns a DataTable with each Phase listed for documentation
    /// </summary>
    public static DataTable GetPhaseTable(Phenology phenology)
    {
        DataTable phaseTable = new DataTable();
        phaseTable.Columns.Add("Number", typeof(int));
        phaseTable.Columns.Add("Name", typeof(string));
        phaseTable.Columns.Add("Type", typeof(string));
        phaseTable.Columns.Add("Start Stage", typeof(string));
        phaseTable.Columns.Add("End Stage", typeof(string));

        int n = 1;
        foreach (IPhase child in phenology.Node.FindChildren<IPhase>())
        {
            string phasetype = GetFilepathOfNamespace(child.GetType());
            DataRow row = phaseTable.NewRow();
            row[0] = n;
            row[1] = child.Name;
            row[2] = $"[{child.GetType()}](https://github.com/APSIMInitiative/ApsimX/blob/master/" + phasetype.Replace(".", "/") + ".cs)";
            row[3] = (child as IPhase).Start;
            row[4] = (child as IPhase).End;
            phaseTable.Rows.Add(row);
            n++;
        }
        return phaseTable;
    }
}