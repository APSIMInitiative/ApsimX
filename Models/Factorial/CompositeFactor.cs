using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using APSIM.Core;
using APSIM.Shared.Extensions.Collections;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;

namespace Models.Factorial
{

    /// <summary>
    /// This class represents a series of paths and the same number of object values.
    /// Its sole purpose is to apply the object values to the model represented by the paths.
    /// If Specifications are specified then they are used instead of paths and values.
    /// </summary>
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Factor))]
    [ValidParent(ParentType = typeof(Permutation))]
    [Serializable]
    [ViewName("UserInterface.Views.QuadView")]
    [PresenterName("UserInterface.Presenters.QuadPresenter")]
    public class CompositeFactor : Model, IReferenceExternalFiles, ICodeEditor
    {
        /// <summary>
        /// A list of models that have been passed into this composite factor 
        /// by the one edge case constructor that does that
        /// </summary>
        private List<IModel> _models { get; set; }

        /// <summary>
        /// Variables to hold the names and values of Descriptors during updates
        /// </summary>
        private string[] _names;
        private string[] _values;

        /// <summary>
        /// Gets or sets the specification to create overrides for a 
        /// simulation.
        /// </summary>
        public string[] Specifications { get; set; }

        /// <summary>
        /// This hold a list of additional Composite Factor variables that are 
        /// stored in the datastore like a factor column. This allows them to 
        /// used for graphing and filtering factors with complex composite 
        /// factors.
        /// </summary>
        public SimulationDescriptor[] CustomDescriptors { get; set; }

        /// <summary>
        /// Helper Property to display CustomDescriptors in a Grid interface
        /// </summary>
        [Display(DisplayName = "Name")]
        [JsonIgnore]
        public string[] DescriptorNames { 
            get
            {
                List<SimulationDescriptor> descriptors = new List<SimulationDescriptor>();
                try
                {
                    List<SimulationDescriptor> generatedDescriptors = GetExperimentDescriptors();
                    descriptors.AddRange(generatedDescriptors);
                    descriptors.AddRange(CustomDescriptors);
                }
                catch
                {
                    descriptors.AddRange(CustomDescriptors);
                }
                finally
                {
                    _names = descriptors.Select(d => d.Name).ToArray();
                }
                return _names;
            }
            set
            {
                _names = value;
                UpdateDescriptors();
            }
        }

        /// <summary>
        /// Helper Property to display CustomDescriptors in a Grid interface
        /// </summary>
        [Display(DisplayName = "Value")]
        [JsonIgnore]
        public string[] DescriptorValues { 
            get
            {
                List<SimulationDescriptor> descriptors = new List<SimulationDescriptor>();
                try
                {
                    List<SimulationDescriptor> generatedDescriptors = GetExperimentDescriptors();
                    descriptors.AddRange(generatedDescriptors);
                    descriptors.AddRange(CustomDescriptors);
                }
                catch
                {
                    descriptors.AddRange(CustomDescriptors);
                }
                finally
                {
                    _values = descriptors.Select(d => d.Value).ToArray();
                }
                return _values;
            }
            set
            {
                _values = value;
                UpdateDescriptors();
            }
        }

        /// <summary>Property for the ICodeEditor to change Specifications with</summary>
        [JsonIgnore]
        public IEnumerable<string> Code { 
            get { return Specifications; } 
            set { Specifications = value.ToArray(); } 
        }

        /// <summary>
        /// The command language lines generated from the specifications. These are the
        /// commands applied to each simulation this CompositeFactor is part of.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Commands => SpecificationsToCommandLines();

        /// <summary>Parameterless constructor needed for serialisation</summary>
        public CompositeFactor()
        {
            _models = new List<IModel>();
            CustomDescriptors = [];
            Specifications = [];
        }

        /// <summary>Constructor for full specification line</summary>
        public CompositeFactor(string name, string specification)
        {
            _models = new List<IModel>();
            Name = name;
            CustomDescriptors = [];
            Specifications = [specification];
        }

        /// <summary>Constructor for path and value</summary>
        public CompositeFactor(string name, string path, object value)
        {
            CustomDescriptors = [];
            CreateSpecifications(path, value);
            Name = name;
        }

        /// <summary>Constructor for a composite factor created from a Factor</summary>
        public CompositeFactor(Factor parentFactor, string path, object value)
        {
            Parent = parentFactor;
            CustomDescriptors = [];
            CreateSpecifications(path, value);
        }

        /// <summary>
        /// Apply this CompositeFactor to the specified simulation
        /// </summary>
        /// <param name="simulationDescription">A description of a simulation.</param>
        public void ApplyToSimulation(SimulationDescription simulationDescription)
        {
            foreach (IModelCommand command in GetCommands())
                simulationDescription.AddOverride(command);

            List<SimulationDescriptor> descriptors = new List<SimulationDescriptor>();
            if (Parent == null) //used by sobol and morris
            {
                descriptors.Add(new SimulationDescriptor(Name, Name));
            }
            else
            {
                if (!(Parent is Factors))
                    descriptors.Add(new SimulationDescriptor(Parent.Name, Name));
            }
            foreach(SimulationDescriptor descriptor in descriptors)
                simulationDescription.Descriptors.Add(descriptor);

            //add any custom descriptors on
            foreach(SimulationDescriptor descriptor in CustomDescriptors)
                simulationDescription.Descriptors.Add(descriptor);
        }

        /// <summary>Return paths to all files referenced by this model.</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            if (Node == null)
                return new List<string>();

            Simulations sims = Node.FindParent<Simulations>(recurse: true);
            List<string> values = new List<string>();
            foreach (IModelCommand command in GetCommands())
                if (command is SetPropertyCommand setCommand)
                    values.Add(setCommand.Value.ToString());
            IEnumerable<string> result = values.Where(str => File.Exists(PathUtilities.GetAbsolutePath(str, sims.FileName)));
            return result;
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert the specification lines into model commands using the command language.
        /// </summary>
        private IEnumerable<IModelCommand> GetCommands()
        {
            List<string> lines = Commands.ToList();
            if (lines.Count == 0)
                return [];
            string relativeDirectory = string.IsNullOrEmpty(Node?.FileName) ? null : Path.GetDirectoryName(Node.FileName);
            // Models held in _models are not children of this factor, so resolve them relative to the parent.
            INodeModel relativeTo = Children.Count == 0 && Parent is INodeModel parent ? parent : this;
            return CommandLanguage.StringToCommands(lines, relativeTo, relativeDirectory, multiple: true);
        }

        private void CreateSpecifications(string path, object value)
        {
            if (value is IModel model)
            {
                if (model is CompositeFactor factor)
                {
                    _models = new List<IModel>();
                    foreach(IModel child in factor.Children)
                        _models.Add(child);
                }
                else
                {
                    _models = new List<IModel>() {model};
                }
                Name = model.Name;
                Specifications = [$"{path}"];
            }
            else
            {
                _models = new List<IModel>();
                Name = value.ToString();
                Specifications = [$"{path}={value}"];
            }
        }

        /// <summary>
        /// Convert the specifications into command language lines. Specifications containing an
        /// '=' are property settings and are used as is. Specifications without an '='
        /// name a model to replace, which becomes a replace command using the matching child model.
        /// </summary>
        private List<string> SpecificationsToCommandLines()
        {
            List<string> commands = [];

            if (Specifications is null)
                return commands;

            List<string> specifications = [];
            if (Children.Count == 0)
            {
                EnsureAModelExistsForEachSpecification(_models, Specifications);
                specifications = RetrieveActiveSpecifications(_models, Specifications);
            }
            else 
            {
                EnsureAModelExistsForEachSpecification(Children, Specifications);
                specifications = RetrieveActiveSpecifications(Children, Specifications);
            }

            if (specifications.Count == 0)
                return commands;

            List<IModel> models = [];
            foreach (string specification in specifications)
            {
                if (specification.Contains('='))
                {
                    int equalsIndex = specification.IndexOf('=');
                    string value = specification.Substring(equalsIndex + 1).Trim();
                    if (value.Length == 0)
                        throw new Exception($"Error in composite factor {Name}: Unable to parse factor specification {specification}: No value was provided");
                    commands.Add($"{specification.Substring(0, equalsIndex).Trim()}={value}");
                }
                else
                {
                    string path = specification.Trim();
                    object value = null;

                    // Find the model that we are to replace.
                    Experiment experiment = Parent.Node.FindParent<Experiment>(recurse: true);
                    Simulation baseSimulation = experiment.Node.FindChild<Simulation>(relativeTo: experiment);
                    IModel modelToReplace = baseSimulation.Node.Get(path) as IModel;
                    if (modelToReplace == null)
                        throw new Exception($"Error in CompositeFactor {Name}: Unable to find a model to replace from path '{path}'");

                    IEnumerable<IModel> modelsToSearch = Node.FindChildren<IModel>();
                    if (modelsToSearch.Count() == 0)
                        modelsToSearch = _models;

                    //Work out if any of the replacing models have the same type or share a non-imodel interface
                    List<Type> interfacesOfModel = typeof(Model).GetInterfaces().ToList();
                    interfacesOfModel.Add(typeof(IStructureDependency));
                    IEnumerable<Type> interfacesToReplace = modelToReplace.GetType().GetInterfaces().Except(interfacesOfModel);

                    List<IModel> possibleMatches = new List<IModel>();
                    foreach (IModel model in modelsToSearch)
                    {
                        if (model.GetType() == modelToReplace.GetType())
                            possibleMatches.Add(model);
                        else if (modelToReplace.GetType().IsAssignableFrom(model.GetType()))
                            possibleMatches.Add(model);
                        else
                        {
                            IEnumerable<Type> interfacesOfSearch = model.GetType().GetInterfaces().Except(interfacesOfModel);
                            //if this is a manager script, also get the interfaces from the class defined in the script
                            if (model is Manager manager)
                                interfacesOfSearch = interfacesOfSearch.AppendMany(manager.Script.GetType().GetInterfaces().Except(interfacesOfModel));

                            if (interfacesToReplace.Intersect(interfacesOfSearch).Any())
                                possibleMatches.Add(model);
                        }
                    }

                    //if no matches, throw
                    if (possibleMatches.Count() == 0)
                    {
                        throw new NullReferenceException($"Error in composite factor {Name}: Unable to parse factor specification {specification}: No children are of type {modelToReplace.GetType().Name}, so model {modelToReplace.Name} cannot be overriden.");
                    }
                    //If only one match, return that
                    else if (possibleMatches.Count() == 1)
                    {
                        value = possibleMatches.First();
                    }
                    //if multiple, try and match by name as well
                    else if (possibleMatches.Count() > 1)
                    {
                        IModel match = possibleMatches.FirstOrDefault(m => m.Name.ToLower() == modelToReplace.Name.ToLower());
                        if (match == null) //if multiple matches, but none match on name, throw
                            throw new NullReferenceException($"Error in composite factor {Name}: Unable to parse factor specification {specification}: Multiple children of type {modelToReplace.GetType().Name} but none share a name with the model they replace. Ambiguous replacement has been prevented.");
                        else
                            value = match;
                    }

                    string target = path.Contains('.') ? path : $"[{modelToReplace.Name}]";
                    IModel replacement = (IModel)value;
                    string replacementPath = replacement.Parent != null ? $"[{replacement.Parent.Name}].{replacement.Name}" : $"[{replacement.Name}]";
                    commands.Add($"replace all {target} with {replacementPath}");
                    models.Add(value as IModel);
                }
            }

            // If there are any child models which aren't being used as
            // a factor value (e.g. as a model replacement), throw an exception.
            IEnumerable<IModel> extraModels = Children.Except(models.OfType<IModel>());
            foreach (var model in extraModels)
                if (model.Enabled && !(model is Memo))
                    throw new InvalidOperationException($"Error in composite factor {Name}: Unused child models found: {string.Join(", ", extraModels.Select(m => m.Name))}");

            return commands;
        }

        internal List<string> RetrieveActiveSpecifications(IEnumerable<IModel> children, string[] specifications)
        {
            // Remove specifictions if it makes the name of a disabled child model.
            HashSet<string> disabledChildNames = children
                .Where(child => child is { Enabled: false })
                .Select(child => child.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<string> activeSpecifications = specifications
                .Where(spec => spec.Length > 0)
                .Where(spec => !spec.StartsWith("//", StringComparison.Ordinal))
                 .Where(spec =>
                 {
                     int open = spec.IndexOf('[');
                     int close = spec.IndexOf(']', open + 1);
                     if (open < 0 || close < 0)
                         return true;
                     string referencedName = spec.Substring(open + 1, close - open - 1).Trim();
                     return !disabledChildNames.Contains(referencedName);
                 })
                .ToList();
            return activeSpecifications;
        }

        /// <summary>
        /// Ensure a specification exists for each valid model in the CompositeFactor.
        /// </summary>
        internal void EnsureAModelExistsForEachSpecification(IEnumerable<IModel> children, string[] specifications)
        {
            int modelOnlySpecsCount = specifications.Count(spec => !spec.Contains("=") && !string.IsNullOrEmpty(spec) && !spec.StartsWith("//"));
            if (modelOnlySpecsCount != children.Count(child => child is not IText))
            {
                if (this.FullPath is not null)
                {
                    throw new ApsimXException(this, $"There is disparity between the number of specifications and the number of" +
                        $" children for the composite model located at: {FullPath} in file: " +
                        $"{Node.FindParents<Simulations>().First().FileName}. " +
                        "Please check both your specifications and child models.");
                }
                else
                {
                    throw new ApsimXException(this, $"There is disparity between the number of specifications and the number of" +
                        $" children for the composite model in this simulation." +
                        " Please check both your specifications and child models for each of your CompositeFactor models.");                    
                }
            }
        }

        /// <summary>
        /// Since the grid interface will udpate the names and values column 
        /// seperately, this function is run on both so the CustomDescriptors 
        /// property is correctly updated when either are touched.
        /// </summary>
        private void UpdateDescriptors()
        {
            if (_names != null && _values != null && _names.Length == _values.Length)
            {
                List<SimulationDescriptor> descriptors = null;
                try
                {
                    descriptors = GetExperimentDescriptors();
                }
                catch
                {
                    descriptors = new List<SimulationDescriptor>();
                }
                finally
                {
                    IEnumerable<string> readOnlyNames = descriptors.Select(d => d.Name);
                    List<SimulationDescriptor> newCustomDescriptors = new List<SimulationDescriptor>();
                    for(int i = 0; i < _names.Length; i++)
                        if (!readOnlyNames.Contains(_names[i]) && !string.IsNullOrEmpty(_names[i].Trim()))
                            newCustomDescriptors.Add(new SimulationDescriptor(_names[i].Trim(), _values[i].Trim()));
                    
                    CustomDescriptors = newCustomDescriptors.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the list of automatically created Descriptors for this factor
        /// </summary>
        /// <returns></returns>
        private List<SimulationDescriptor> GetExperimentDescriptors()
        {
            if (Node == null)
                return null;
            
            Experiment experiment = Node.FindParent<Experiment>(recurse: true);
            if (experiment == null)
                return null;
            
            SimulationDescription description = experiment.GetDescriptors(new List<CompositeFactor>() {this});
            if (description == null)
                return null;

            List<SimulationDescriptor> descriptors = description.Descriptors.ToList();
            return descriptors.Except(CustomDescriptors).ToList();
        }
    }
}