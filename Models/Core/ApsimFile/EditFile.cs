using APSIM.Shared.Utilities;
using Models.Factorial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Models.Core.ApsimFile
{
    /// <summary>
    /// Encapsulates the /Edit command-line switch which allows users
    /// to make batch edits of .apsimx files from the command line.
    /// 
    /// Usage:
    /// 
    /// Models.exe /path/to/file.apsimx /Edit /path/to/config/file.txt
    /// </summary>
    public static class EditFile
    {
        /// <summary>
        /// Edit the given .apsimx file by applying changes
        /// specified in the given config file.
        /// </summary>
        /// <param name="apsimxFilePath">Absolute path to the .apsimx file.</param>
        /// <param name="configFilePath">Absolute path to the config file.</param>
        public static Simulations Do(string apsimxFilePath, string configFilePath)
        {
            return ApplyChanges(apsimxFilePath, GetFactors(configFilePath));
        }

        /// <summary>
        /// Gets a list of factors from a config file.
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
        private static List<CompositeFactor> GetFactors(string configFileName)
        {
            List<CompositeFactor> factors = new List<CompositeFactor>();
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
                factors.Add(new CompositeFactor("factor", path, value));
            }

            return factors;
        }

        /// <summary>
        /// Edits a single apsimx file according to the changes specified in the config file.
        /// </summary>
        /// <param name="apsimxFileName">Path to an .apsimx file.</param>
        /// <param name="factors">Factors to apply to the file.</param>
        private static Simulations ApplyChanges(string apsimxFileName, List<CompositeFactor> factors)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(apsimxFileName, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw new Exception($"Error reading file ${apsimxFileName}: {errors[0].ToString()}");

            foreach (CompositeFactor factor in factors)
            {
                IVariable variable = file.FindByPath(factor.Paths[0]);
                if (variable == null)
                    throw new Exception($"Invalid path: {factor.Paths[0]}");

                string value = factor.Values[0].ToString();
                string absolutePath =  null;
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
                        ReplaceModelFromFile(file, factor.Paths[0], fileName, modelPath);
                    else if (File.Exists(absoluteFileName))
                        ReplaceModelFromFile(file, factor.Paths[0], absoluteFileName, modelPath);
                    else
                        ChangeVariableValue(variable, value);
                }
                else if (File.Exists(value) && variable.Value is IModel)
                    ReplaceModelFromFile(file, factor.Paths[0], value, null);
                else if (File.Exists(absolutePath) && variable.Value is IModel)
                    ReplaceModelFromFile(file, factor.Paths[0], absolutePath, null);
                else
                    ChangeVariableValue(variable, value);
            }
            return file;
        }

        private static void ChangeVariableValue(IVariable variable, string value)
        {
            variable.Value = ReflectionUtilities.StringToObject(variable.DataType, value);
            if (variable is VariableComposite composite)
            {
                IModel model = composite.Variables.FirstOrDefault(v => v is VariableObject obj && obj.Value is IModel)?.Value as IModel;
                if (model != null)
                {
                    ModelCollectionFromResource resourceModel = model.FindAncestor<ModelCollectionFromResource>();
                    if (resourceModel != null)
                        resourceModel.ResourceName = null;

                    if (model.Parent is Manager manager)
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
        private static void ReplaceModelFromFile(Simulations topLevel, string modelToReplace, string replacementFile, string replacementPath)
        {
            IModel toBeReplaced = topLevel.FindByPath(modelToReplace)?.Value as IModel;
            if (toBeReplaced == null)
                throw new Exception($"Unable to find model which is to be replaced ({modelToReplace}) in file {topLevel.FileName}");

            IModel extFile = FileFormat.ReadFromFile<IModel>(replacementFile, out List<Exception> errors);
            if (errors?.Count > 0)
                throw new Exception($"Error reading replacement file {replacementFile}", errors[0]);

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

            IModel parent = toBeReplaced.Parent;
            int index = parent.Children.IndexOf((Model)toBeReplaced);
            parent.Children.Remove((Model)toBeReplaced);

            // Need to call Structure.Add to add the model to the parent.
            Structure.Add(replacement, parent);

            // Move the new model to the index in the list at which the
            // old model previously resided.
            parent.Children.Remove((Model)replacement);
            parent.Children.Insert(index, (Model)replacement);
        }
    }
}
