using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates the /Edit command-line switch which allows users
    /// to make batch edits of .apsimx files from the command line.
    /// 
    /// Usage:
    /// 
    /// Models.exe /path/to/file.apsimx /Edit /path/to/config/file.txt
    /// </summary>
    public static class Overrides
    {
        /// <summary>
        /// Applies a collection of property sets (contained in a file) to a model.
        /// </summary>
        /// <param name="model">The model to apply the changes to.</param>
        /// <param name="configFilePath">A configuration file containing property sets.</param>
        public static void Apply(IModel model, string configFilePath)
        {
            var factors = ParseConfigFile(configFilePath);
            Apply(model, factors);
        }

        /// <summary>
        /// Applies a collection of property sets to a model.
        /// </summary>
        /// <param name="model">The model to apply the changes to.</param>
        /// <param name="factors">The property sets (keyword, value).</param>
        /// <returns></returns>
        public static void Apply(IModel model, IEnumerable<(string, object)> factors)
        {
            foreach (var factor in factors)
            {
                IEnumerable<IVariable> variables = null;
                if (factor.Item1.StartsWith("Name="))
                {
                    // Replacements uses this.
                    string name = factor.Item1.Replace("Name=", "");
                    variables = model.FindAllInScope(name).Select(m => new VariableObject(m));
                }
                else
                {
                    variables = model.FindAllByPath(factor.Item1);
                    if (!variables.Any())
                        throw new Exception($"Invalid path: {factor.Item1}");
                }

                foreach (IVariable variable in variables)
                {
                    string value = factor.Item2.ToString();
                    string absolutePath = null;
                    try
                    {
                        if (!value.Contains(":"))
                            absolutePath = PathUtilities.GetAbsolutePath(value, Directory.GetCurrentDirectory());
                    }
                    catch
                    {
                    }

                    string[] parts = value.Split(';');
                    if (parts != null && parts.Length == 2)
                    {
                        string fileName = parts[0];
                        string absoluteFileName = PathUtilities.GetAbsolutePath(fileName, Directory.GetCurrentDirectory());
                        string modelPath = parts[1];

                        if (File.Exists(fileName))
                            ReplaceModelFromFile(model, factor.Item1, fileName, modelPath);
                        else if (File.Exists(absoluteFileName))
                            ReplaceModelFromFile(model, factor.Item1, absoluteFileName, modelPath);
                        else
                            ChangeVariableValue(variable, value);
                    }
                    else if (File.Exists(value) && variable.Value is IModel)
                        ReplaceModelFromFile(model, factor.Item1, value, null);
                    else if (File.Exists(absolutePath) && variable.Value is IModel)
                        ReplaceModelFromFile(model, factor.Item1, absolutePath, null);
                    else if (variable.Value is IModel)
                        Structure.Replace(variable.Value as IModel, factor.Item2 as IModel);
                    else
                        ChangeVariableValue(variable, value);
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
        private static IEnumerable<(string, object)> ParseConfigFile(string configFileName)
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

        private static void ChangeVariableValue(IVariable variable, string value)
        {
            variable.Value = ReflectionUtilities.StringToObject(variable.DataType, value);
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
        /// <param name="topLevel">The top-level model of the file being modified.</param>
        /// <param name="modelToReplace">Path to the model which is to be replaced.</param>
        /// <param name="replacementFile">Path of the .apsimx file containing the model which will be inserted.</param>
        /// <param name="replacementPath">Path to the model in replacementFile which will be used to replace a model in topLevel.</param>
        private static void ReplaceModelFromFile(IModel topLevel, string modelToReplace, string replacementFile, string replacementPath)
        {
            IModel toBeReplaced = topLevel.FindByPath(modelToReplace)?.Value as IModel;
            if (toBeReplaced == null)
                throw new Exception($"Unable to find model which is to be replaced ({modelToReplace})");

            IModel extFile = FileFormat.ReadFromFile<IModel>(replacementFile, e => throw e, false);

            IModel replacement;
            if (string.IsNullOrEmpty(replacementPath))
            {
                replacement = extFile.FindAllDescendants().Where(d => toBeReplaced.GetType().IsAssignableFrom(d.GetType())).FirstOrDefault();
                if (replacement == null)
                    throw new Exception($"Unable to find replacement model of type {toBeReplaced.GetType().Name} in file {replacementFile}");
            }
            else
            {
                replacement = extFile.FindByPath(replacementPath)?.Value as IModel;
                if (replacement == null)
                    throw new Exception($"Unable to find model at path {replacementPath} in file {replacementFile}");
            }

            Structure.Replace(toBeReplaced, replacement);
        }
    }
}
