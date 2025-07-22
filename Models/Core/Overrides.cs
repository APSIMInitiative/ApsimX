﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;

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
        /// Applies a property or model replacement to a model.
        /// </summary>
        /// <param name="model">The model to apply the changes to.</param>
        /// <param name="path">The path of the property/model to override.</param>
        /// <param name="value">The new value of the property/model.</param>
        /// <param name="matchType">Type of matching to use.</param>
        /// <remarks>
        /// Name:
        ///     Whenever a bracketed path is specified, all models that have a name or type name that match the bracketed value
        ///     will be considered for replacement .e.g
        ///         [Report].VariableNames   changes all 'VariableNames' properties in all models of name or type 'Report'
        ///
        ///     If path starts with 'Name=' then only the specified name will be used to match models to replace. This is used
        ///     by the replacements node. e.g. Name=Wheat will replace all models named 'Wheat'.
        /// Value:
        ///     Value can be a string (that will be converted into an object instance)
        ///     Value can be a csv string (that will be converted into an array of object instances)
        ///     Value can be a filename (that will be opened and searched for the first matching model of the same name and type)
        ///     Value can be filename;[path] (that will be opened and searched for the first matching model that matches 'path')
        /// </remarks>
        /// <returns>
        /// An enumeration of Override instances that, when applied, will undo the Apply.
        /// </returns>
        public static IEnumerable<Override> Apply(IModel model, string path, object value, Override.MatchTypeEnum matchType)
        {
            List<Override> undos = new List<Override>();
            IEnumerable<VariableComposite> variables = null;
            if (matchType == Override.MatchTypeEnum.Name)
            {
                // Replacements uses this.
                variables = model.FindAllInScope(path)
                                 .Where(m => m.Parent != null)
                                 .Select(m =>
                                 {
                                     var composite = new VariableComposite("");
                                     composite.AddInstance(m);
                                     return composite;
                                 });
            }
            else
                variables = FindAllByPath(model, path);

            foreach (var variable in variables)
            {
                object replacementValue = value;

                if (replacementValue is string valueAsString)
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
                        replacementValue = GetModelFromFile(variable.Value.GetType(), valueAsString, modelPath);
                    else
                        replacementValue = ReflectionUtilities.StringToObject(variable.DataType, valueAsString);
                }

                // Apply the override and return an undo override.
                string undoPath = CalculateFullPath(variable, model);
                object oldValue;
                if (variable.Value is IModel modelToReplace && replacementValue is IModel modelReplacement)
                {
                    Structure.Replace(modelToReplace, modelReplacement);
                    oldValue = modelToReplace;
                }
                else
                {
                    // Convert the replacementValue to a full sized array variable if necessary.
                    // e.g. if path = Data[3:4] then the replacementValue needs to be the full
                    // array and not just the values that are going to be used
                    // This gets around a design decision in VariableProperty.Value.set.
                    oldValue = ChangeVariableValue(variable, replacementValue);
                }

                undos.Add(new Override(undoPath, oldValue, Override.MatchTypeEnum.NameAndType));
            }

            // Updates the parameters from the manager model.
            IModel pathObject = model.FindDescendant<Manager>(StringUtilities.CleanStringOfSymbols(path.Split('.').First()));
            if (pathObject is Manager manager)
                manager.GetParametersFromScriptModel();

            // Reverse the order of the undos so that get applied in the correct order.
            undos.Reverse();
            return undos;
        }

        /// <summary>
        /// Applies a collection of property sets to a model.
        /// </summary>
        /// <param name="model">The model to apply the changes to.</param>
        /// <param name="overrides">The collection of overrides to apply.</param>
        /// <returns>
        /// An enumeration of Override instances that, when applied, will undo the Apply.
        /// </returns>
        public static IEnumerable<Override> Apply(IModel model, IEnumerable<Override> overrides)
        {
            List<Override> undos = new List<Override>();
            foreach (var replacement in overrides)
                undos.InsertRange(0, Apply(model, replacement.Path, replacement.Value, replacement.MatchType));
            return undos;
        }

        /// <summary>
        /// Parse a collection of lines into a list of overrides.
        /// </summary>
        /// <remarks>
        /// Each line must be of the form:
        ///
        /// path = value
        ///
        /// e.g.
        ///
        /// [Clock].StartDate = 1/1/2019
        /// .Simulations.Simulation.Weather.FileName = asdf.met
        /// </remarks>
        /// <param name="lines">Lines to parse.</param>
        /// <returns></returns>
        public static IEnumerable<Override> ParseStrings(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                StringUtilities.SplitOffAfterDelimiter(ref lines[i], "//");
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] values = lines[i].Split('=');
                string path = values[0].Trim();
                string value;

                if (values.Length == 2)
                {
                    // Handles single-line overrides.
                    value = values[1].Trim();
                }
                else if (values.Length > 2)
                {
                    // Handles factor specifications.
                    values = lines[i].Split("=", 2);
                    value = values[1];
                }
                else
                {
                    throw new Exception($"Wrong number of values specified on line {lines[i]}");
                }

                yield return new Override(path, value, Override.MatchTypeEnum.NameAndType);
            }
        }

        /// <summary>
        /// Change the value of the property.
        /// </summary>
        /// <param name="variable">The IVariable containing the property to change.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <returns>The old value before the change was made.</returns>
        private static object ChangeVariableValue(VariableComposite variable, object newValue)
        {
            object oldValue = variable.Value;
            variable.Value = newValue;
            if (variable is VariableComposite composite)
            {
                IModel model = composite.FirstModel as IModel;
                if (model != null)
                {
                    if (model.Parent is Manager manager && variable.Name == ".Script.Code")
                        manager.RebuildScriptModel();
                }
            }
            return oldValue;
        }

        /// <summary>
        /// Replace a model with a model from another file.
        /// </summary>
        /// <param name="typeToFind">The type of the model to find for replacement.</param>
        /// <param name="replacementFile">Path of the .apsimx file containing the model which will be inserted.</param>
        /// <param name="replacementPath">Path to the model in replacementFile which will be used to replace a model in topLevel.</param>
        private static IModel GetModelFromFile(Type typeToFind, string replacementFile, string replacementPath)
        {
            IModel extFile = FileFormat.ReadFromFile<IModel>(replacementFile).Model as IModel;

            IModel replacement;
            if (string.IsNullOrEmpty(replacementPath))
            {
                replacement = extFile.FindAllDescendants().Where(d => typeToFind.IsAssignableFrom(d.GetType())).FirstOrDefault();
                if (replacement == null)
                    throw new Exception($"Unable to find replacement model of type {typeToFind.Name} in file {replacementFile}");
            }
            else
            {
                replacement = extFile.Node.Get(replacementPath) as IModel;
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

        /// <summary>Calculate a full path for an IVariable.</summary>
        /// <param name="variableOrModel">The variable or model instance.</param>
        /// <param name="relativeTo">The calculated path should be relative to this model.</param>
        /// <returns>Full path or throws if cannot calculate path.</returns>
        private static string CalculateFullPath(object variableOrModel, IModel relativeTo)
        {
            string st = null;
            if (variableOrModel is VariableComposite composite)
                st = composite.FullPath();
            else if (variableOrModel is IModel model)
                st = model.FullPath;

            // Convert path from absolute to relative.
            string relativePath = st.Replace(relativeTo.FullPath, "").TrimStart('.');

            return relativePath;
        }

        /// <summary>
        /// Find and return multiple matches (e.g. a soil in multiple zones) for a given path.
        /// Note that this can be a variable/property or a model. NOTE: Can't use locator
        /// because it returns a single match - not multiple.
        /// </summary>
        /// <param name="model">Relative to</param>
        /// <param name="path">The path of the variable/model.</param>
        /// <returns>A collection of VariableComposite instances. Of null if no matches.</returns>
        private static IEnumerable<VariableComposite> FindAllByPath(IModel model, string path)
        {
            IEnumerable<IModel> matches = null;

            // Remove a square bracketed model name and change our relativeTo model to
            // the referenced model.
            if (path.StartsWith("["))
            {
                int posCloseBracket = path.IndexOf(']');
                if (posCloseBracket != -1)
                {
                    string modelName = path.Substring(1, posCloseBracket - 1);
                    path = path.Remove(0, posCloseBracket + 1).TrimStart('.');
                    matches = model.FindAllInScope(modelName);
                    if (!matches.Any())
                    {
                        // Didn't find a model with a name matching the square bracketed string so
                        // now try and look for a model with a type matching the square bracketed string.
                        Type[] modelTypes = ReflectionUtilities.GetTypeWithoutNameSpace(modelName, Assembly.GetExecutingAssembly());
                        if (modelTypes.Length == 1)
                            matches = model.FindAllInScope().Where(m => modelTypes[0].IsAssignableFrom(m.GetType()));
                    }
                }
            }
            else
                matches = new IModel[] { model };

            foreach (Model match in matches)
            {
                if (string.IsNullOrEmpty(path))
                {
                    var composite = new VariableComposite(path);
                    composite.AddInstance(match);
                    yield return composite;
                }
                else
                {
                    var variable = match.Node.GetObject(path, LocatorFlags.PropertiesOnly | LocatorFlags.CaseSensitive | LocatorFlags.IncludeDisabled);
                    if (variable != null)
                        yield return variable;
                }
            }
        }

        /// <summary>Encapsulates a keyword=value pair.</summary>
        [Serializable]
        public class Override
        {
            /// <summary>
            /// Parameterless constructor for serialization
            /// </summary>
            public Override() { }
            /// <summary>Constructor.</summary>
            /// <param name="path">The path of the property/model to override.</param>
            /// <param name="value">The new value of the property/model.</param>
            /// <param name="matchType">Type of matching to use.</param>
            public Override(string path, object value, MatchTypeEnum matchType)
            {
                Path = path;
                Value = value;
                MatchType = matchType;
            }

            /// <summary>Supported match types when finding something to override.</summary>
            public enum MatchTypeEnum
            {
                /// <summary>Match on name only.</summary>
                Name,

                /// <summary>Match on name and type.</summary>
                NameAndType
            }

            /// <summary>The path of the property/model to override.</summary>
            public string Path { get; set; }

            /// <summary>The new value of the property/model.</summary>
            public object Value { get; set; }

            /// <summary>Type of matching to use.</summary>
            public MatchTypeEnum MatchType { get; set; }

            /// <summary>
            /// Equality method.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                if (obj is Override ov)
                {
                    if (Path.Equals(ov.Path, StringComparison.InvariantCultureIgnoreCase) &&
                        MatchType == ov.MatchType)
                    {
                        if (Value is string value1AsString && ov.Value is string value2AsString)
                            return value1AsString.Equals(value2AsString, StringComparison.InvariantCultureIgnoreCase);
                        else
                            return Value.Equals(ov.Value);
                    }
                }
                return false;
            }

            /// <summary>
            /// Return a hash code for this instance.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return (Path, Value, MatchType).GetHashCode();
            }
        }
    }
}
