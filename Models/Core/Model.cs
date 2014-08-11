using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Schema;
using System.Xml;


namespace Models.Core
{
    /// <summary>
    /// Base class for all models in ApsimX.
    /// </summary>
    [Serializable]
    public class Model
    {
        private string _Name = null;
        private Model _Parent = null;
        [NonSerialized] private Variables _Variables;
        [NonSerialized] private Scope _Scope;
        [NonSerialized] private Events _Events;



        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor.
        /// </summary>
        public Model()
        {
            Children = new ModelCollection(this);
            IsHidden = false;
        }

        /// <summary>
        /// Get or set the name of the model
        /// </summary>
        public string Name
        {
            get
            {
                if (_Name == null)
                    return this.GetType().Name;
                else
                    return _Name;
            }
            set
            {
                _Name = value;
                CalcFullPath();
            }
        }

        /// <summary>
        /// A list of child models.   
        /// </summary>
        [XmlElement(typeof(Simulation))]
        [XmlElement(typeof(Simulations))]
        [XmlElement(typeof(Zone))]
        [XmlElement(typeof(Model))]
        [XmlElement(typeof(ModelCollectionFromResource))]
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
        [XmlElement(typeof(Models.PostSimulationTools.Input))]
        [XmlElement(typeof(Models.PostSimulationTools.PredictedObserved))]
        [XmlElement(typeof(Models.PostSimulationTools.TimeSeriesStats))]
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
        [XmlElement(typeof(Models.PMF.Organs.TreeCanopy))]
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
        [XmlElement(typeof(Models.PMF.Cultivar))]
        public List<Model> Models { get; set; }

        #region Methods than can be overridden
        /// <summary>
        /// Called immediately after the model is XML deserialised.
        /// </summary>
        public virtual void OnLoaded() { }

        /// <summary>
        /// Called just before the simulation commences.
        /// </summary>
        public virtual void OnSimulationCommencing() { }

        /// <summary>
        /// Called just after the simulation has completed.
        /// </summary>
        public virtual void OnSimulationCompleted() { }

        /// <summary>
        /// Invoked after all simulations finish running. This allows
        /// for post simulation analysis models to run.
        /// </summary>
        public virtual void OnAllSimulationsCompleted() {}

        /// <summary>
        /// Called immediately before deserialising.
        /// </summary>
        public virtual void OnDeserialising(bool xmlSerialisation) { }

        /// <summary>
        /// Called immediately after deserialisation.
        /// </summary>
        public virtual void OnDeserialised(bool xmlSerialisation) { }

        /// <summary>
        /// Called immediately before serialising.
        /// </summary>
        public virtual void OnSerialising(bool xmlSerialisation) { }

        /// <summary>
        /// Called immediately after serialisation.
        /// </summary>
        public virtual void OnSerialised(bool xmlSerialisation) { }

        #endregion

        /// <summary>
        /// Get or set the parent of the model.
        /// </summary>
        [XmlIgnore]
        public Model Parent
        {
            get
            {
                return _Parent;
            }
            set
            {
                _Parent = value;
                _Variables = new Core.Variables(this);
                _Scope = new Core.Scope(this);
                _Events = new Core.Events(this);
                CalcFullPath();
            }
        }

        /// <summary>
        /// Return a parent node of the specified type 't'. Will throw if not found.
        /// </summary>
        public Model ParentOfType(Type t)
        {
            Model obj = this;
            while (obj.Parent != null && obj.GetType() != t)
                obj = obj.Parent;
            if (obj == null)
                throw new ApsimXException(FullPath, "Cannot find a parent of type: " + t.Name);
            return obj;
        }

        /// <summary>
        /// Return the full path of the model.
        /// Format: Simulations.SimName.PaddockName.ChildName
        /// </summary>
        [XmlIgnore]
        public string FullPath { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        [XmlIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Provides access to all child models.
        /// </summary>
        [XmlIgnore]
        public ModelCollection Children { get; private set;}

        /// <summary>
        /// Provides access to all simulation variables.
        /// </summary>
        public Variables Variables { get { return _Variables; } }

        /// <summary>
        /// Provides access to all simulation models that are in scope.
        /// </summary>
        public Scope Scope { get { return _Scope; } }

        /// <summary>
        /// Providees access to all simulation events that are in scope.
        /// </summary>
        public Events Events { get { return _Events; } }

        /// <summary>
        /// Connect this model to the others in the simulation.
        /// </summary>
        public void ResolveLinks()
        {
            Simulation simulation = ParentOfType(typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.IsRunning)
                {
                    // Resolve links in this model.
                    ResolveLinksInternal(this);

                    // Resolve links in other models that point to this model.
                    ResolveExternalLinks(this);
                }
                else
                    ResolveLinksInternal(this);
            }
        }

        /// <summary>
        /// Connect this model to the others in the simulation.
        /// </summary>
        public void UnResolveLinks()
        {
            UnresolveLinks(this);
        }

        /// <summary>
        /// Perform a deep Copy of the this model.
        /// </summary>
        public Model Clone()
        {
            // Get a list of all child models that we need to notify about the (de)serialisation.
            List<Model> modelsToNotify = Children.AllRecursively;

            // Get rid of our parent temporarily as we don't want to serialise that.
            Models.Core.Model parent = Parent;
            Parent = null;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                foreach (Model model in modelsToNotify)
                    model.OnSerialising(xmlSerialisation: false);

                formatter.Serialize(stream, this);

                foreach (Model model in modelsToNotify)
                    model.OnSerialised(xmlSerialisation: false);

                stream.Seek(0, SeekOrigin.Begin);

                foreach (Model model in modelsToNotify)
                    model.OnDeserialising(xmlSerialisation: false);
                Model returnObject = (Model)formatter.Deserialize(stream);
                foreach (Model model in modelsToNotify)
                    model.OnDeserialised(xmlSerialisation: false);

                // Reinstate parent
                Parent = parent;

                return returnObject;
            }
        }

        /// <summary>
        /// Serialise the model to a string and return the string.
        /// </summary>
        public string Serialise()
        {
            // Get a list of all child models that we need to notify about the serialisation.
            List<Model> modelsToNotify = Children.AllRecursively;
            modelsToNotify.Insert(0, this);

            // Let all models know that we're about to serialise.
            foreach (Model model in modelsToNotify)
                model.OnSerialising(xmlSerialisation: true);

            // Do the serialisation
            StringWriter writer = new StringWriter();
            writer.Write(Utility.Xml.Serialise(this, true));

            // Let all models know that we have completed serialisation.
            foreach (Model model in modelsToNotify)
                model.OnSerialised(xmlSerialisation: false);

            // Set the clipboard text.
            return writer.ToString();
        }
        #region Internals

        /// <summary>
        /// Calculate the model's full path. 
        /// Format: Simulations.SimName.PaddockName.ChildName
        /// </summary>
        private void CalcFullPath()
        {
            FullPath = "." + Name;
            Model parent = Parent;
            while (parent != null)
            {
                FullPath = FullPath.Insert(0, "." + parent.Name);
                parent = parent.Parent;
            }

            if (Models != null)
                foreach (Model child in Models)
                    child.CalcFullPath();

        }

        /// <summary>
        /// Resolve all [Link] fields in this model.
        /// </summary>
        private static void ResolveLinksInternal(Model model, Type linkTypeToMatch = null)
        {
            string errorMsg = "";

            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = Utility.Reflection.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null && 
                    (linkTypeToMatch == null || field.FieldType == linkTypeToMatch))
                {
                    object linkedObject = null;
                    
                    Model[] allMatches;
                    allMatches = model.Scope.FindAll(field.FieldType);
                    if (allMatches.Length == 1)
                        linkedObject = allMatches[0];
                    else
                    {
                        // more that one match so use name to match.
                        foreach (Model matchingModel in allMatches)
                            if (matchingModel.Name == field.Name)
                            {
                                linkedObject = matchingModel;
                                break;
                            }
                        if ((linkedObject == null) && (!link.IsOptional))
                        {
                            errorMsg = string.Format(": Found {0} matches for {1} {2} !", allMatches.Length, field.FieldType.FullName, field.Name);
                        }
                    }

                    if (linkedObject != null)
                    {
                        field.SetValue(model, linkedObject);
                    }
                    else if (!link.IsOptional)
                        throw new ApsimXException(model.FullPath, "Cannot resolve [Link] '" + field.ToString() +
                                                            "' in class '" + model.FullPath + "'" + errorMsg);
                }
            }
        }

        /// <summary>
        /// Unresolve (set to null) all [Link] fields.
        /// </summary>
        private static void UnresolveLinks(Model model)
        {
            // Go looking for private [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = Utility.Reflection.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null)
                    field.SetValue(model, null);
            }
        }

        /// <summary>
        /// Go through all other models looking for a [Linl] to the specified 'model'.
        /// Connect any links found.
        /// </summary>
        private static void ResolveExternalLinks(Model model)
        {
            foreach (Model externalModel in model.Scope.FindAll())
                ResolveLinksInternal(externalModel, typeof(Model));
        }

        #endregion

    }
}
