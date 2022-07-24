using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates all property/model overrides in APSIM. An override is defined as a 
    /// collection of (name, value) pairs. Overrides are applied to a model (usually an
    /// instance of a Simulation or Simulations class).
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public static class Overrides
    {
        /// <summary>
        /// Applies a (keyword, value) tuple to a model.
        /// </summary>
        /// <param name="model">The model to apply the changes to.</param>
        /// <param name="name">The name of the property/model to override.</param>
        /// <param name="value">The new value of the property/model.</param>
        /// <returns>
        /// Name:
        ///     Whenever a bracketed name is specified, all models that have a name or type name that match the bracketed value
        ///     will be considered for replacement .e.g
        ///         [Report].VariableNames   changes all 'VariableNames' properties in all models of name or type 'Report'
        ///         
        ///     If name starts with 'Name=' then only the specified name will be used to match models to replace. This is used
        ///     by the replacements node. e.g. Name=Wheat will replace all models named 'Wheat'.
        /// Value:
        ///     Value can be a string (that will be converted into an object instance)
        ///     Value can be a csv string (that will be converted into an array of object instances)
        ///     Value can be a filename (that will be opened and searched for the first matching model of the same name and type)
        ///     Value can be filename;[path] (that will be opened and searched for the first matching model that matches 'path')
        /// </returns>
        public static void Apply(IModel model, string name, object value)
        {
            Apply(model, new (string name, object value)[] { (name, value) });
        }

        /// <summary>
        /// Applies a collection of property sets to a model.
        /// </summary>
        /// <param name="model">The model to apply the changes to.</param>
        /// <param name="factors">The property sets (keyword, value).</param>
        /// <returns></returns>
        public static void Apply(IModel model, IEnumerable<(string name, object value)> factors)
        {
            foreach (var factor in factors)
            {
                IEnumerable<IVariable> variables = null;
                if (factor.name.StartsWith("Name="))
                {
                    // Replacements uses this.
                    string name = factor.Item1.Replace("Name=", "");
                    variables = model.FindAllInScope(name).Select(m => new VariableObject(m));
                }
                else
                {
                    variables = model.FindAllByPath(factor.name);
                    if (!variables.Any())
                        throw new Exception($"Invalid path: {factor.name}");
                }

                foreach (IVariable variable in variables)
                {
                    object replacement = factor.value;

                    if (factor.value is string valueAsString)
                    {
                        // See if value is a filename that exists. If not then try convert it to an absolute path.
                        valueAsString = TryConvertToAbsolutePath(valueAsString);

                        // See if value has a semicolon in it i.e. denotes a filename and a model path.
                        string modelPath = null;
                        string[] parts = valueAsString.Split(';');
                        if (parts != null && parts.Length == 2)
                        {
                            valueAsString = TryConvertToAbsolutePath(parts[0]);
                            modelPath = parts[1];
                        }

                        if (File.Exists(valueAsString) && variable.Value is IModel)
                            replacement = GetModelFromFile(variable.Value.GetType(), valueAsString, modelPath);
                        else
                            replacement = ReflectionUtilities.StringToObject(variable.DataType, valueAsString);
                    }

                    if (variable.Value is IModel modelToReplace && replacement is IModel modelReplacement)
                        Structure.Replace(modelToReplace, modelReplacement);
                    else
                        ChangeVariableValue(variable, replacement);
                }
            }
        }

        /// <summary>
        /// Parse a configuration file for a list of factors..
        /// </summary>
        /// <remarks>
        /// Each line in the file must be of the form:
        /// 
        /// path = value
        /// 
        /// e.g.
        /// 
        /// [Clock].StartDate = 1/1/2019
        /// .Simulations.Simulation.Weather.FileName = asdf.met
        /// </remarks>
        /// <param name="configFileName">Path to the config file.</param>
        public static IEnumerable<(string, object)> ParseConfigFile(string configFileName)
        {
            List<(string, object)> factors = new List<(string, object)>();
            string[] lines = File.ReadAllLines(configFileName);
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] values = lines[i].Split('=');
                if (values.Length != 2)
                    throw new Exception($"Wrong number of values specified on line {i} of config file '{configFileName}'.");

                string path = values[0].Trim();
                string value = values[1].Trim();
                factors.Add((path, value));
            }

            return factors;
        }

        /// <summary>
        /// Change the value of the property.
        /// </summary>
        /// <param name="variable">The IVariable containing the property to change.</param>
        /// <param name="newValue">The new value of the property.</param>
        private static void ChangeVariableValue(IVariable variable, object newValue)
        {
            variable.Value = newValue;
            if (variable is VariableComposite composite)
            {
                IModel model = composite.Variables.FirstOrDefault(v => v is VariableObject obj && obj.Value is IModel)?.Value as IModel;
                if (model != null)
                {
                    var resourceModel = model.FindAllAncestors().FirstOrDefault(a => !string.IsNullOrEmpty(a.ResourceName));
                    if (resourceModel != null)
                        resourceModel.ResourceName = null;

                    if (model.Parent is Manager manager && variable.Name == ".Script.Code")
                        manager.RebuildScriptModel();
                }
            }
        }

        /// <summary>
        /// Replace a model with a model from another file.
        /// </summary>
        /// <param name="typeToFind">The type of the model to find for replacement.</param>
        /// <param name="replacementFile">Path of the .apsimx file containing the model which will be inserted.</param>
        /// <param name="replacementPath">Path to the model in replacementFile which will be used to replace a model in topLevel.</param>
        private static IModel GetModelFromFile(Type typeToFind, string replacementFile, string replacementPath)
        {
            IModel extFile = FileFormat.ReadFromFile<IModel>(replacementFile, e => throw e, false);

            IModel replacement;
            if (string.IsNullOrEmpty(replacementPath))
            {
                replacement = extFile.FindAllDescendants().Where(d => typeToFind.IsAssignableFrom(d.GetType())).FirstOrDefault();
                if (replacement == null)
                    throw new Exception($"Unable to find replacement model of type {typeToFind.Name} in file {replacementFile}");
            }
            else
            {
                replacement = extFile.FindByPath(replacementPath)?.Value as IModel;
                if (replacement == null)
                    throw new Exception($"Unable to find model at path {replacementPath} in file {replacementFile}");
            }

            return replacement;
        }

        /// <summary>
        /// Try and convert a string to a path that can be opened.
        /// </summary>
        /// <param name="valueAsString"></param>
        /// <returns>The original string or the original string with a full path.</returns>
        private static string TryConvertToAbsolutePath(string valueAsString)
        {
            if (!File.Exists(valueAsString) && !valueAsString.Contains(":"))
            {
                string path = PathUtilities.GetAbsolutePath(valueAsString, Directory.GetCurrentDirectory());
                if (File.Exists(path))
                    valueAsString = path;
            }
            return valueAsString;
        }
    }
}
