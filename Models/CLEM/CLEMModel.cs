using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Models.CLEM
{
    ///<summary>
    /// CLEM base model
    ///</summary>
    [Serializable]
    [Description("This is the Base CLEM model and should not be used directly.")]
    public abstract class CLEMModel : Model, ICLEMUI, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        [JsonIgnore]
        public IStructure Structure { get; set; }
    
        /// <summary>
        /// Link to summary
        /// </summary>
        [Link]
        [NonSerialized]
        public ISummary Summary = null;

        [NonSerialized]
        private IEnumerable<IActivityTimer> activityTimers = null;

        /// <summary>
        /// Model settings notes
        /// </summary>
        [Description("Notes")]
        [Category("Simulation", "Details")]
        [Core.Display(Order = 9999)]
        public string Notes { get; set; }

        /// <summary>
        /// Identifies the last selected tab for display
        /// </summary>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Warning log for this CLEM model
        /// </summary>
        [JsonIgnore]
        public WarningLog Warnings = WarningLog.GetInstance(50);

        /// <summary>
        /// Model identifier
        /// </summary>
        [JsonIgnore]
        public Guid UniqueID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Model identifier as string for reporting as UniqueID.ToString() throws ambiguous property error
        /// </summary>
        [JsonIgnore]
        public string UniqueIDString { get { return UniqueID.ToString(); } }

        /// <summary>
        /// Parent CLEM Zone
        /// Stored here so rapidly retrieved
        /// </summary>
        [JsonIgnore]
        public string CLEMParentName { get; set; }

        /// <summary>
        /// return combo name of ParentName.ModelName
        /// </summary>
        [JsonIgnore]
        public string NameWithParent => $"{this.Parent.Name}.{this.Name}";

        /// <inheritdoc/>
        [JsonIgnore]
        public DescriptiveSummaryMemoReportingType ReportMemosType { get; set; } = DescriptiveSummaryMemoReportingType.InPlace;

        /// <inheritdoc/>
        [JsonIgnore]
        public TimeStepTypes MinimumTimeStepInterval { get; set; } = TimeStepTypes.Monthly;

        /// <summary>An event handler to allow us to do preliminary checks for model relationships and availability.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected virtual void OnCLEMInitialise(object sender, EventArgs e)
        {
            CheckModelAssociations(this);
        }

        /// <summary>
        /// Method to check model associations based on attribute values.
        /// </summary>
        /// <param name="model">The requesting model</param>
        public static void CheckModelAssociations(Model model)
        {
            IStructure structure;
            if (model is CLEMModel clemModel)
            {
                structure = clemModel.Structure;
            }
            else if (model is ZoneCLEM zoneModel)
            {
                structure = zoneModel.Structure;
            }
            else
            {
                return;
            }

            ModelAssociationsAttribute requiredAttribte = model.GetType().GetCustomAttribute<ModelAssociationsAttribute>();
            if (requiredAttribte is not null)
            {
                List<string> errors = new();
                for (int i = 0; i < (requiredAttribte.AssociatedModels?.Length ?? 0); i++)
                {
                    switch (requiredAttribte.AssociationStyles[i])
                    {
                        case ModelAssociationStyle.InScope:
                            if (structure.FindAll<Model>(relativeTo: model).Where(a => a.GetType() == requiredAttribte.AssociatedModels[i]).Any() == false)
                            {
                                errors.Add($"Cannot find required component [x={requiredAttribte.AssociatedModels[i].Name}] in scope for [x={model.FullPath}]");
                            }

                            break;
                        case ModelAssociationStyle.Descendent:
                            if (structure.FindChildren<CLEMModel>(relativeTo: model, recurse: true).Where(a => a.GetType() == requiredAttribte.AssociatedModels[i]).Any() == false)
                            {
                                errors.Add($"Cannot find required component [x={requiredAttribte.AssociatedModels[i].Name}] as descendent of [x={model.FullPath}]");
                            }

                            break;
                        case ModelAssociationStyle.Child:
                            if (structure.FindChildren<CLEMModel>(relativeTo: model).Where(a => a.GetType() == requiredAttribte.AssociatedModels[i]).Any() == false)
                            {
                                errors.Add($"Cannot find required component [x={requiredAttribte.AssociatedModels[i].Name}] as child of [x={model.FullPath}]");
                            }

                            break;
                        case ModelAssociationStyle.DescendentOfRuminantType:
                            // find Ruminant Types
                            var zone = structure.FindParent<Zone>(relativeTo: model, recurse: true);
                            var rumTypes = structure.FindChildren<RuminantType>(relativeTo: zone, recurse: true);
                            foreach (var rumType in rumTypes)
                            {
                                if (structure.FindChildren<CLEMModel>(relativeTo: rumType, recurse: true).Where(a => a.GetType() == requiredAttribte.AssociatedModels[i]).Any() == false)
                                {
                                    errors.Add($"Cannot find required component [x={requiredAttribte.AssociatedModels[i].Name}] as descendent of [r={rumType.Name}] as required by [x={model.FullPath}]");
                                }
                            }
                            break;
                    }
                }

                if (requiredAttribte.SingleInstance)
                {
                    var allFound = structure.FindAll<Model>(relativeTo: model).Where(a => a.GetType() == model.GetType());
                    if (allFound.Count() > 1 && allFound.FirstOrDefault() == model)
                    {
                        errors.Add($"Only one instance of [x={model.GetType().Name}] is allowed in scope of [CLEM].");
                    }
                }

                if (errors.Any())
                {
                    var sim = structure.FindParent<Simulation>(relativeTo: model, recurse: true);
                    Summary summary = structure.FindChild<Summary>(relativeTo: sim, recurse: true);
                    var zone = structure.FindParent<Zone>(relativeTo: model, recurse: true);
                    foreach (var error in errors)
                        summary.WriteMessage(zone, error, MessageType.Error);
                }
            }
            IEnumerable<ValidParentAttribute> validParents = model.GetType().GetCustomAttributes<ValidParentAttribute>();
            if (validParents.Any())
            {

                if(!validParents.Where(a => a.ParentType == (model as IModel).Parent.GetType() | (a.ParentType.IsInterface && model.Parent.GetType().GetInterfaces().Contains(a.ParentType)) | (model.Parent.GetType().IsSubclassOf(a.ParentType))).Any())
                {
                    var sim = structure.FindParent<Simulation>(relativeTo: model, recurse: true);
                    Summary summary = structure.FindChild<Summary>(relativeTo: sim, recurse: true);
                    var zone = structure.FindParent<Zone>(relativeTo: model, recurse: true);
                    if (validParents.Count() > 1)
                    {
                        summary.WriteMessage(zone, $"Only a component of type {string.Join(',', validParents.Select(a => $"[{a.ParentType.Name}]"))} is permitted as a parent of [x={model.FullPath}]", MessageType.Error);
                    }
                    else
                    {
                        summary.WriteMessage(zone, $"Only components of types {string.Join("", validParents.Select(a => $"[{a.ParentType.Name}]"))} are permitted as a parent of [x={model.FullPath}]", MessageType.Error);
                    }
                }
            }   
        }

        /// <summary>
        /// A list of activity timers for this activity
        /// </summary>
        [JsonIgnore]
        public IEnumerable<IActivityTimer> ActivityTimers
        {
            get
            {
                if (activityTimers is null)
                {
                    activityTimers = Structure.FindChildren<IActivityTimer>();
                }

                return activityTimers;
            }
        }

        /// <summary>
        /// Is timing ok for the current model
        /// </summary>
        public bool TimingOK
        {
            get
            {
                var result = Structure.FindChildren<IActivityTimer>().Sum(a => a.ActivityDue ? 0 : 1);
                return (result == 0);
            }
        }

        /// <summary>
        /// return a list of components available given the specified types
        /// </summary>
        /// <param name="typesToFind">the list of types to locate</param>
        /// <returns>A list of names of components including any string item in list provided</returns>
        public IEnumerable<string> GetResourcesAvailableByName(object[] typesToFind)
        {
            List<string> results = new();
            Zone zone = Structure.FindParent<Zone>(recurse: true);
            if (zone is not null)
            {
                ResourcesHolder resources = Structure.FindChild<ResourcesHolder>(relativeTo: zone);
                if (resources is not null)
                {
                    foreach (object type in typesToFind)
                    {
                        if (type is string)
                        {
                            results.Add(type as string);
                        }
                        else if (type is Type)
                        {
                            var res = resources.FindResource(type as Type);
                            IEnumerable<string> list = null;
                            if (res != null)
                            {
                                list = Structure.FindChildren<IResourceType>(relativeTo: res).Select(a => (a as CLEMModel).NameWithParent) ?? null;
                            }

                            if (list != null)
                            {
                                results.AddRange(Structure.FindChildren<IResourceType>(relativeTo: res)
                                       .Select(a => (a as CLEMModel).NameWithParent));
                            }
                        }
                    }
                }
            }
            return results.AsEnumerable();
        }

        /// <summary>
        /// Get a list of model names given specified types as array
        /// </summary>
        /// <param name="typesToFind">the list of types to include</param>
        /// <returns>A list of model names</returns>
        public IEnumerable<string> GetNameOfModelsByType(Type[] typesToFind)
        {
            Simulation simulation = Structure.FindParent<Simulation>(recurse: true);
            if (simulation is null)
            {
                return new List<string>().AsEnumerable();
            }
            else
            {
                List<Type> types = new();
                return Structure.FindChildren<IModel>(relativeTo: simulation, recurse: true).Where(a => typesToFind.ToList().Contains(a.GetType())).Select(a => a.Name);
            }
        }

        /// <summary>
        /// Determines if this component has a valid parent based on parent attributes
        /// </summary>
        /// <returns></returns>
        public bool IsParentValid()
        {
            var parents = ReflectionUtilities.GetAttributes(this.GetType(), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList();
            return (parents.Where(a => a.ParentType.Name == this.Parent.GetType().Name).Any());
        }

        /// <summary>
        /// A method to return the list of identifiers relevant to this parent activity
        /// </summary>
        /// <returns>A list of identifiers</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if (this is IActivityCompanionModel && Parent != null && Parent is IHandlesActivityCompanionModels)
            {
                return (Parent as IHandlesActivityCompanionModels).DefineCompanionModelLabels(GetType().Name).Identifiers;
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// A method to determine whether any identifiers have been provided by the parent
        /// Used to hide unnecessary property display in UI
        /// </summary>
        /// <returns></returns>
        public bool ParentSuppliedIdentifiersPresent()
        {
            var psi = ParentSuppliedIdentifiers();
            return (psi != null && psi.Any());
        }

        /// <summary>
        /// A method to return the list of units relevant to this parent activity
        /// </summary>
        /// <returns>A list of units</returns>
        public List<string> ParentSuppliedMeasures()
        {
            if (this is IActivityCompanionModel && Parent != null && Parent is IHandlesActivityCompanionModels)
            {
                return (Parent as IHandlesActivityCompanionModels).DefineCompanionModelLabels(GetType().Name).Measures;
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// A method to determine whether any measures have been provided by the parent
        /// Used to hide unnecessary property display in UI
        /// </summary>
        /// <returns></returns>
        public bool ParentSuppliedMeasuresPresent()
        {
            var psm = ParentSuppliedMeasures();
            return (psm != null && psm.Any());
        }

        /// <summary>
        /// Determines if this model supports a specified time-step interval
        /// </summary>
        /// <param name="events">Time step to check</param>
        public bool TimeStepOK(CLEMEvents events)
        {
            // monthly time-step is assumed if no MinimumTimeStepPermittedAttribute has been provided.
            var timestepAtt = ReflectionUtilities.GetAttributes(this.GetType(), typeof(MinimumTimeStepPermittedAttribute), false).Cast<MinimumTimeStepPermittedAttribute>().FirstOrDefault();
            if (events.TimeStep == TimeStepTypes.Custom)
            {
                return (int)(timestepAtt?.TimeStep??TimeStepTypes.Monthly) > events.Interval;
            }
            else
            {
                return (int)(timestepAtt?.TimeStep ?? TimeStepTypes.Monthly) <= (int)events.TimeStep;
            }
        }
    }
}
