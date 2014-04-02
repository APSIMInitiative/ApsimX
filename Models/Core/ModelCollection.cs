using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;

namespace Models.Core
{
    [Serializable]
    public abstract class ModelCollection : Model
    {
        /// <summary>
        /// A list of child models.   
        /// </summary>
        [XmlElement(typeof(Simulation))]
        [XmlElement(typeof(Simulations))]
        [XmlElement(typeof(Zone))]
        [XmlElement(typeof(Model))]
        [XmlElement(typeof(Models.Graph.Graph))]
        [XmlElement(typeof(Models.PMF.Plant))]
        [XmlElement(typeof(Models.PMF.Slurp.Slurp))]
        [XmlElement(typeof(Models.PMF.OilPalm.OilPalm))]
        [XmlElement(typeof(Models.Soils.Soil))]
        [XmlElement(typeof(Models.SurfaceOM.SurfaceOrganicMatter))]
        [XmlElement(typeof(AgPasture))]
        [XmlElement(typeof(Clock))]
        [XmlElement(typeof(DataStore))]
        [XmlElement(typeof(Fertiliser))]
        [XmlElement(typeof(Input))]
        [XmlElement(typeof(Irrigation))]
        [XmlElement(typeof(Manager))]
        [XmlElement(typeof(MicroClimate))]
        [XmlElement(typeof(Operations))]
        [XmlElement(typeof(Report))]
        [XmlElement(typeof(Summary))]
        [XmlElement(typeof(NullSummary))]
        [XmlElement(typeof(Tests))]
        [XmlElement(typeof(WeatherFile))]
        [XmlElement(typeof(Log))]
        [XmlElement(typeof(Models.Factorial.Experiment))]
        [XmlElement(typeof(Models.Factorial.Factors))]
        [XmlElement(typeof(Models.Factorial.Factor))]
        [XmlElement(typeof(Models.Factorial.FactorValue))]

        [XmlElement(typeof(Memo))]
        [XmlElement(typeof(Folder))]

        [XmlElement(typeof(Soils.Water))]
        [XmlElement(typeof(Soils.SoilWater))]
        [XmlElement(typeof(Soils.SoilNitrogen))]
        [XmlElement(typeof(Soils.SoilOrganicMatter))]
        [XmlElement(typeof(Soils.Analysis))]
        [XmlElement(typeof(Soils.InitialWater))]
        [XmlElement(typeof(Soils.Phosphorus))]
        [XmlElement(typeof(Soils.Swim))]
        [XmlElement(typeof(Soils.LayerStructure))]
        [XmlElement(typeof(Soils.SoilTemperature))]
        [XmlElement(typeof(Soils.SoilTemperature2))]
        [XmlElement(typeof(Soils.SoilArbitrator))]
        [XmlElement(typeof(Soils.Sample))]
        [XmlElement(typeof(Models.PMF.Arbitrator))]
        [XmlElement(typeof(Models.PMF.Structure))]
        [XmlElement(typeof(Models.PMF.Summariser))]
        [XmlElement(typeof(Models.PMF.Biomass))]
        [XmlElement(typeof(Models.PMF.CompositeBiomass))]
        [XmlElement(typeof(Models.PMF.ArrayBiomass))]
        [XmlElement(typeof(Models.PMF.Organs.BelowGroundOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.GenericAboveGroundOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.GenericBelowGroundOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.GenericOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.HIReproductiveOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.Leaf))]
        [XmlElement(typeof(Models.PMF.Organs.LeafCohort))]
        [XmlElement(typeof(Models.PMF.Organs.Leaf.InitialLeafValues))]
        [XmlElement(typeof(Models.PMF.Organs.Nodule))]
        [XmlElement(typeof(Models.PMF.Organs.ReproductiveOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.ReserveOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.Root))]
        [XmlElement(typeof(Models.PMF.Organs.RootSWIM))]
        [XmlElement(typeof(Models.PMF.Organs.SimpleLeaf))]
        [XmlElement(typeof(Models.PMF.Organs.SimpleRoot))]
        [XmlElement(typeof(Models.PMF.Phen.Phenology))]
        [XmlElement(typeof(Models.PMF.Phen.EmergingPhase))]
        [XmlElement(typeof(Models.PMF.Phen.EmergingPhase15))]
        [XmlElement(typeof(Models.PMF.Phen.EndPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GenericPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GerminatingPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GotoPhase))]
        [XmlElement(typeof(Models.PMF.Phen.LeafAppearancePhase))]
        [XmlElement(typeof(Models.PMF.Phen.LeafDeathPhase))]
        [XmlElement(typeof(Models.PMF.Phen.Vernalisation))]
        [XmlElement(typeof(Models.PMF.Phen.VernalisationCW))]
        [XmlElement(typeof(Models.PMF.Functions.AccumulateFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AddFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AgeCalculatorFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AirTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.BellCurveFunction))]
        [XmlElement(typeof(Models.PMF.Functions.Constant))]
        [XmlElement(typeof(Models.PMF.Functions.DivideFunction))]
        [XmlElement(typeof(Models.PMF.Functions.ExponentialFunction))]
        [XmlElement(typeof(Models.PMF.Functions.ExpressionFunction))]
        [XmlElement(typeof(Models.PMF.Functions.ExternalVariable))]
        [XmlElement(typeof(Models.PMF.Functions.InPhaseTtFunction))]
        [XmlElement(typeof(Models.PMF.Functions.LessThanFunction))]
        [XmlElement(typeof(Models.PMF.Functions.LinearInterpolationFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MaximumFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MinimumFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MultiplyFunction))]
        [XmlElement(typeof(Models.PMF.Functions.OnEventFunction))]
        [XmlElement(typeof(Models.PMF.Functions.PhaseBasedSwitch))]
        [XmlElement(typeof(Models.PMF.Functions.PhaseLookup))]
        [XmlElement(typeof(Models.PMF.Functions.PhaseLookupValue))]
        [XmlElement(typeof(Models.PMF.Functions.PhotoperiodDeltaFunction))]
        [XmlElement(typeof(Models.PMF.Functions.PhotoperiodFunction))]
        [XmlElement(typeof(Models.PMF.Functions.PowerFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SigmoidFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SigmoidFunction2))]
        [XmlElement(typeof(Models.PMF.Functions.SoilTemperatureDepthFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SoilTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SoilTemperatureWeightedFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SplineInterpolationFunction))]
        [XmlElement(typeof(Models.PMF.Functions.StageBasedInterpolation))]
        [XmlElement(typeof(Models.PMF.Functions.SubtractFunction))]
        [XmlElement(typeof(Models.PMF.Functions.VariableReference))]
        [XmlElement(typeof(Models.PMF.Functions.WeightedTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.Zadok))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.AllometricDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.InternodeDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.PartitionFractionDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.PopulationBasedDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.PotentialSizeDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.RelativeGrowthRateDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.StructureFunctions.HeightFunction))]
        [XmlElement(typeof(Models.PMF.Functions.StructureFunctions.InPhaseTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.StructureFunctions.MainStemFinalNodeNumberFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SupplyFunctions.RUECO2Function))]
        [XmlElement(typeof(Models.PMF.Functions.SupplyFunctions.RUEModel))]
        [XmlElement(typeof(Models.PMF.OldPlant.Plant15))]
        [XmlElement(typeof(Models.PMF.OldPlant.Environment))]
        [XmlElement(typeof(Models.PMF.OldPlant.GenericArbitratorXY))]
        [XmlElement(typeof(Models.PMF.OldPlant.Grain))]
        [XmlElement(typeof(Models.PMF.OldPlant.Leaf1))]
        [XmlElement(typeof(Models.PMF.OldPlant.LeafNumberPotential3))]
        [XmlElement(typeof(Models.PMF.OldPlant.NStress))]
        [XmlElement(typeof(Models.PMF.OldPlant.NUptake3))]
        [XmlElement(typeof(Models.PMF.OldPlant.PlantSpatial1))]
        [XmlElement(typeof(Models.PMF.OldPlant.Pod))]
        [XmlElement(typeof(Models.PMF.OldPlant.Population1))]
        [XmlElement(typeof(Models.PMF.OldPlant.PStress))]
        [XmlElement(typeof(Models.PMF.OldPlant.RadiationPartitioning))]
        [XmlElement(typeof(Models.PMF.OldPlant.Root1))]
        [XmlElement(typeof(Models.PMF.OldPlant.RUEModel1))]
        [XmlElement(typeof(Models.PMF.OldPlant.Stem1))]
        [XmlElement(typeof(Models.PMF.OldPlant.SWStress))]
        [XmlElement(typeof(Models.PMF.SimpleTree))]
        public List<Model> Models { get; set; }

        /// <summary>
        /// Return a list containing 'this' model and all child models recursively. 
        /// Never returns null. Can return empty list.
        /// </summary>
        [XmlIgnore]
        public List<Model> AllModels 
        { 
            get 
            {
                List<Model> allModels = new List<Model>();
                allModels.Add(this);
                allModels.AddRange(AllModelsMatching(null));
                return allModels;
            } 
        }

        /// <summary>
        /// Return a list containing all child models recursively. 
        /// Never returns null. Can return empty list.
        /// If 'modelType' is specified, only models of that type will be returned.
        /// </summary>
        public List<Model> AllModelsMatching(Type modelType)
        {
            List<Model> allModels = new List<Model>();

            // Get a list of children (recursively) of this zone.
            foreach (Model child in Models)
            {
                if (modelType == null || modelType.IsAssignableFrom(child.GetType()))
                    allModels.Add(child);
                if (child is ModelCollection)
                    allModels.AddRange((child as ModelCollection).AllModelsMatching(modelType));
            }
            return allModels;
        }

        /// <summary>
        /// Return a child model that matches the specified 'modelType'. Returns null if not found.
        /// </summary>
        public Model ModelMatching(Type modelType)
        {
            foreach (Model child in Models)
                if (modelType.IsAssignableFrom(child.GetType()))
                    return child;
            return null;
        }

        /// <summary>
        /// Return a child model that matches the specified 'name'. Returns null if not found.
        /// </summary>
        public Model ModelMatching(string name)
        {
            foreach (Model child in Models)
                if (child.Name == name)
                    return child;
            return null;
        }

        /// <summary>
        /// Return child models that match the specified 'modelType'. Returns empty list if none found.
        /// </summary>
        public List<T> ModelsMatching<T>() where T : Model
        {
            List<T> modelsMatching = new List<T>();
            foreach (Model child in Models)
                if (typeof(T).IsAssignableFrom(child.GetType()))
                    modelsMatching.Add((T) child);
            return modelsMatching;
        }

        /// <summary>
        /// Add a model to the Models collection. Will throw if model cannot be added.
        /// </summary>
        public void AddModel(Model model)
        {
            EnsureNameIsUnique(model);
            Models.Add(model);
            model.Parent = this;
            ParentAllModels(this);
            Scope.ClearCache(this);
            Variables.ClearCache(this);

            // Call the model's OnLoaded method.
            CallOnLoaded(model);

            // We need to resolve all links in all models as 
            // the new model may be a better fit for an existing link.
            Simulation simulationToReLink = null;
            if (model is Simulation)
                simulationToReLink = model as Simulation;
            else
                simulationToReLink = Simulation;

            if (simulationToReLink != null)
            {
                simulationToReLink.AllModels.ForEach(DisconnectEvents);
                simulationToReLink.AllModels.ForEach(UnresolveLinks);
                simulationToReLink.AllModels.ForEach(ResolveLinks);
                simulationToReLink.AllModels.ForEach(ConnectEventPublishers);
            }
        }

        /// <summary>
        /// Replace the specified child model with the specified 'newModel'. Return
        /// true if successful.
        /// </summary>
        public bool ReplaceModel(Model modelToReplace, Model newModel)
        {
            // Find the model.
            int index = Models.IndexOf(modelToReplace);
            if (index != -1)
            {
                Model oldModel = Models[index];
                // unresolve the links in the old model and then tell the Simulation to
                // resolve all links in all models.
                UnresolveLinks(oldModel);
                DisconnectEvents(oldModel);

                // remove the existing model.
                Models.RemoveAt(index);

                // Name and parent the model we're adding.
                newModel.Name = modelToReplace.Name;
                newModel.Parent = modelToReplace.Parent;
                EnsureNameIsUnique(newModel);

                // insert the new model.
                Models.Insert(index, newModel);

                // clear caches.
                Scope.ClearCache(this);
                Variables.ClearCache(this);

                // Call the model's OnLoaded method.
                CallOnLoaded(newModel);

                oldModel.Parent = null;

                // Completely wipe out all link and event connections as the new model may
                // offer different links and events.
                Simulation.AllModels.ForEach(DisconnectEvents);
                Simulation.AllModels.ForEach(ConnectEventPublishers);
                Simulation.AllModels.ForEach(UnresolveLinks);
                Simulation.AllModels.ForEach(ResolveLinks);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a model from the Models collection. Returns true if model was removed.
        /// </summary>
        public bool RemoveModel(Model modelToRemove)
        {
            // Find the model.
            int index = Models.IndexOf(modelToRemove);
            if (index != -1)
            {
                Model oldModel = Models[index];

                // remove the existing model.
                Models.RemoveAt(index);

                // clear caches.
                Scope.ClearCache(this);
                Variables.ClearCache(this);

                // unresolve the links in the old model
                UnresolveLinks(oldModel);
                // Completely wipe out all link and event connections as the deleted model may
                // affect other model's links and events.
                if (Simulation != null)
                {
                    Simulation.AllModels.ForEach(DisconnectEvents);
                    Simulation.AllModels.ForEach(ConnectEventPublishers);
                    Simulation.AllModels.ForEach(UnresolveLinks);
                    Simulation.AllModels.ForEach(ResolveLinks);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Give the specified model a unique name
        /// </summary>
        private string EnsureNameIsUnique(Model Model)
        {
            string originalName = Model.Name;
            string NewName = originalName;
            int Counter = 0;
            Model child = Models.FirstOrDefault(m => m.Name == NewName);
            while (child != null && child != Model && Counter < 10000)
            {
                Counter++;
                NewName = originalName + Counter.ToString();
                child = Models.FirstOrDefault(m => m.Name == NewName);
            }
            if (Counter == 1000)
                throw new Exception("Cannot create a unique name for model: " + originalName);
            Utility.Reflection.SetName(Model, NewName);
            return NewName;
        }


        /// <summary>
        /// Locate the parent with the specified type. Returns null if not found.
        /// </summary>
        private Simulation Simulation
        {
            get
            {
                Model m = this;
                while (m != null && m.Parent != null && !(m is Simulation))
                    m = m.Parent;

                return m as Simulation;
            }
        }


        /// <summary>
        /// Recursively go through all child models and correctly set their parent field.
        /// </summary>
        protected static void ParentAllModels(ModelCollection parent)
        {
            foreach (Model child in parent.Models)
            {
                child.Parent = parent;
                if (child is ModelCollection)
                    ParentAllModels(child as ModelCollection);
            }
        }

    }
}
