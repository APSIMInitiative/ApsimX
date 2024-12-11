using System.Collections.Generic;
using System.IO;
using System.Reflection;
using APSIM.Documentation.Models;
using APSIM.Shared.Documentation;
using APSIM.Shared.Documentation.Mapping;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Newtonsoft.Json.Linq;
using DocumentationGraph = APSIM.Shared.Documentation.Graph;
using DocumentationGraphPage = APSIM.Shared.Documentation.GraphPage;
using DocumentationMap = APSIM.Shared.Documentation.Map;

namespace APSIM.Documentation;

/// <summary>
/// This class contains the code for generating the files used for unit testing documentation structure.
/// </summary>
public static class TestUtilities
{

    /// <summary>
    /// List of Models and Tutorials to test
    /// </summary>
    public static readonly List<string> FILES = new(){"Potato", "OilPalm", "MicroClimate", "Nutrient", "SCRUM", "SWIM", "AgPasture", "Report", "Manager"};

    /// <summary>
    /// This function creates each of the example doc structure jsons that the test TestDocumentationStructure uses to compare against.
    /// This needs to be run again if the documentation structure code is changed in a way that causes the structure of docs to change.
    /// </summary>
    public static void GenerateComparisonJSONs()
    {
        string apsimx = PathUtilities.GetAbsolutePath("%root%", null);
        foreach (string file in FILES)
        {
            string resources = Path.Combine(apsimx, "Tests", "Validation", file) + "/";
            if (file == "Report" || file == "Manager")
                resources = Path.Combine(apsimx, "Examples", "Tutorials") + "/";

            string json = File.ReadAllText(resources+file+".apsimx");
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            sims.FileName = "/Tests/Validation/"+file+".apsimx";
            if (file == "Report" || file == "Manager")
                sims.FileName = "/Examples/Tutorials/"+file+".apsimx";
                
            List<ITag> actualTags = AutoDocumentation.Document(sims);

            //use this to recreate the json file for an apsimx doc if changes to it's structure are made.
            string newJSON = GetJSON(actualTags);
            string testFilesDirectory = Path.Combine(apsimx, "Tests", "UnitTests", "Documentation", "TestFiles") + "/";
            string fileName = testFilesDirectory + file + ".json";
            File.WriteAllText(fileName, newJSON);
        }
    }

    ///<summary>
    ///</summary>
    public static string GetJSON(List<ITag> tags) {
        return "[" + TagsToJSON(tags) + "]";
    }

    ///<summary>
    ///</summary>
    public static string TagsToJSON(List<ITag> tags) {
        string output = "";

        for(int i = 0; i < tags.Count; i++) {
            ITag tag = tags[i];
            string type = tag.GetType().ToString();
            string text = "";
            string children = "";

            if (tag.GetType() == typeof(Section)) {
                text = (tag as Section).Title;
                children = TagsToJSON((tag as Section).Children);
            } else if (tag.GetType() == typeof(Paragraph)) {
                text = (tag as Paragraph).text;
            } else if (tag.GetType() == typeof(Table)) {
                text = (tag as Table).Style;
            } else if (tag.GetType() == typeof(DocumentationGraphPage)) {
                List<ITag> childGraphs = new List<ITag>();
                foreach(IGraph iGraph in (tag as DocumentationGraphPage).Graphs)
                    childGraphs.Add(iGraph as DocumentationGraph);
                children = TagsToJSON(childGraphs);
            } else if (tag.GetType() == typeof(DocumentationGraph)) {
                text = (tag as DocumentationGraph).Title;
            }
            text = text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");

            string objectString = "";
            objectString += "{\"type\": \""+type+"\"";
            if (text.Length > 0)
                objectString += ", \"text\": \""+text+"\"";
            if (children.Length > 0)
                objectString += ", \"children\": ["+children+"]";
            objectString += "}";

            if (i < tags.Count-1)
                objectString += ",";

            output += objectString;
        }
        return output;
    }

    ///<summary>
    ///</summary>
    public static List<ITag> GetTags(string json) {
        JArray obj = JArray.Parse(json);
        return JSONToTags(obj);
    }

    ///<summary>
    ///</summary>
    public static List<ITag> JSONToTags(JArray array) {

        List<ITag> output = new List<ITag>();

        for(int i = 0; i < array.Count; i++) {
            JToken obj = array[i];
            if (obj["type"].ToString() == typeof(Section).ToString()) {
                output.Add(new Section(obj["text"].ToString(), JSONToTags(obj["children"] as JArray)));

            } else if (obj["type"].ToString() == typeof(Paragraph).ToString()) {
                output.Add(new Paragraph(obj["text"].ToString()));

            } else if (obj["type"].ToString() == typeof(Table).ToString()) {
                output.Add(new Table(new System.Data.DataTable(), style: obj["text"].ToString()));

            } else if (obj["type"].ToString() == typeof(DocumentationGraphPage).ToString()) {
                List<ITag> childGraphs = JSONToTags(obj["children"] as JArray);
                List<IGraph> childIGraphs = new List<IGraph>();
                foreach(DocumentationGraph graph in childGraphs)
                    childIGraphs.Add(graph as IGraph);
                output.Add(new DocumentationGraphPage(childIGraphs));

            } else if (obj["type"].ToString() == typeof(DocumentationGraph).ToString()) {
                output.Add(new DocumentationGraph(obj["text"].ToString(), "", null, null, null, null));

            }  else if (obj["type"].ToString() == typeof(DocumentationMap).ToString()) {
                Coordinate center = new Coordinate(0, 0);
                output.Add(new DocumentationMap(center, 0, new List<Coordinate>()));
            }
        }

        return output;
    }

}