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
        [XmlElement(typeof(Models.PMF.Cultivars))]
        public List<Model> Models { get; set; }

        /// <summary>
        /// Returns true if this model has all events and links connected.
        /// </summary>
        [XmlIgnore]
        public bool IsConnected { get; set; }

        #region Methods than can be overridden
        /// <summary>
        /// Called immediately after the model is XML deserialised.
        /// </summary>
        public virtual void OnLoaded() { }

        /// <summary>
        /// Called just before a simulation commences.
        /// </summary>
        public virtual void OnSimulationCommencing() { }

        /// <summary>
        /// Called just after a simulation has completed.
        /// </summary>
        public virtual void OnSimulationCompleted() { }

        /// <summary>
        /// Invoked immediately before all simulations begin running.
        /// </summary>
        public virtual void OnAllCommencing() {}

        /// <summary>
        /// Invoked after all simulations finish running.
        /// </summary>
        public virtual void OnAllCompleted() {}

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
        /// Parent all child models.
        /// </summary>
        public void ParentAllChildren()
        {
            CalcFullPath();

            if (Models != null)
                foreach (Model child in Models)
                {
                    child.Parent = this;
                    child.ParentAllChildren();
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
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/writtable
        /// </summary>
        public static IVariable[] FieldsAndProperties(object model, BindingFlags flags)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().UnderlyingSystemType.GetProperties(flags))
            {
                if (property.CanRead)
                    allProperties.Add(new VariableProperty(model, property));
            }
            foreach (FieldInfo field in model.GetType().UnderlyingSystemType.GetFields(flags))
                allProperties.Add(new VariableField(model, field));
            return allProperties.ToArray();
        }

        /// <summary>
        /// Write the specified simulation set to the specified 'stream'
        /// </summary>
        public void Write(TextWriter stream)
        {
            foreach (Model model in Children.AllRecursively())
                model.OnSerialising(xmlSerialisation: true);

            try
            {
                stream.Write(Utility.Xml.Serialise(this, true));
            }
            finally
            {
                foreach (Model model in Children.AllRecursively())
                    model.OnSerialised(xmlSerialisation: true);
            }
        }


        /// <summary>
        /// Is this model hidden in the GUI?
        /// </summary>
        [XmlIgnore]
        public bool HiddenModel { get; set; }

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
        /// Connect this model to the others in the simulation.
        /// </summary>
        public void Connect()
        {
            if (IsConnected)
            {
                // This model is being asked to connect itself AFTER events and links
                // have already been connected.  We have to go through all event declarations
                // event handlers, all links in this model and all links other other models
                // that refer to this model. This will be time consuming.

                // 1. connect all event declarations.
                Events.ConnectEventPublishers();

                // 2. connect all event handlers.
                Events.ConnectEventSubscribers();

                // 3. resolve links in this model.
                ResolveLinks(this);

                // 4. resolve links in other models that point to this model.
                ResolveExternalLinks(this);
            }
            else
            {
                // we can take the quicker approach and simply connect event declarations
                // (publish) with their event handlers and assume that our event handlers will
                // be connected by whichever model that is publishing that event.
                Events.ConnectEventPublishers();

                // Resolve all links.
                ResolveLinks(this);
            }
            this.IsConnected = true;
        }
        
        /// <summary>
        /// Connect this model to the others in the simulation.
        /// </summary>
        public void Disconnect()
        {
            Events.DisconnectEventPublishers();
            Events.DisconnectEventSubscribers();
            UnresolveLinks(this);
            this.IsConnected = false;
        }

        /// <summary>
        /// Perform a deep Copy of the 'source' model.
        /// </summary>
        public static Model Clone(Model source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
                throw new ApsimXException("", "Trying to clone a null model");

            // Get a list of all child models that we need to notify about the (de)serialisation.
            List<Model> modelsToNotify = source.Children.AllRecursively();

            // Get rid of source's parent as we don't want to serialise that.
            Models.Core.Model parent = source.Parent;
            source.Parent = null;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                foreach (Model model in modelsToNotify)
                    model.OnSerialising(xmlSerialisation:false);

                formatter.Serialize(stream, source);

                foreach (Model model in modelsToNotify)
                    model.OnSerialised(xmlSerialisation: false);
                
                stream.Seek(0, SeekOrigin.Begin);

                foreach (Model model in modelsToNotify)
                    model.OnDeserialising(xmlSerialisation: false);
                Model returnObject = (Model)formatter.Deserialize(stream);
                foreach (Model model in modelsToNotify)
                    model.OnDeserialised(xmlSerialisation: false);

                source.Parent = parent;

                returnObject.IsConnected = false;
                return returnObject;
            }
        }

        /// <summary>
        /// Resolve all [Link] fields in this model.
        /// </summary>
        private static void ResolveLinks(Model model, Type linkTypeToMatch = null)
        {
            string errorMsg = "";
            //Console.WriteLine(model.FullPath + ":");

            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                Link link = Utility.Reflection.GetAttribute(field, typeof(Link), false) as Link;
                if (link != null && 
                    (linkTypeToMatch == null || field.FieldType == linkTypeToMatch))
                {
                    object linkedObject = null;
                    
                    // NEW SECTION
                    Model[] allMatches;
                    if (link.MustBeChild)
                        allMatches = model.Children.AllRecursively(field.FieldType).ToArray();
                    else
                        allMatches = model.Scope.FindAll(field.FieldType);
                    if (!link.MustBeChild && allMatches.Length == 1)
                        linkedObject = allMatches[0];
                    else if (allMatches.Length > 1 && model.Parent is Factorial.FactorValue)
                    {
                        // Doesn't matter what the link is being connected to if the the model passed
                        // into ResolveLinks is sitting under a FactorValue. It won't be run from
                        // under FactorValue anyway.
                        linkedObject = allMatches[0];
                    }
                    else
                    {
                        // This is primarily for PLANT where matches for things link Functions should
                        // only come from children and not somewhere else in Plant.
                        // e.g. EmergingPhase in potato has an optional link for 'Target'
                        // Potato doesn't have a target child so we don't want to use scoping 
                        // rules to find the target for some other phase.

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
                        //if (linkedObject is Model)
                        //    Console.WriteLine("    " + field.Name + " linked to " + (linkedObject as Model).FullPath);

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
                Link link = Utility.Reflection.GetAttribute(field, typeof(Link), false) as Link;
                if (link != null)
                    field.SetValue(model, null);
            }
        }

        /// <summary>
        /// Call OnCommencing in the specified model and all child models.
        /// </summary>
        protected static void CallOnCommencing(Model model)
        {
            model.OnSimulationCommencing();
        }

        /// <summary>
        /// Call OnCompleted in the specified model and all child models.
        /// </summary>
        protected static void CallOnCompleted(Model model)
        {
            model.OnSimulationCompleted();
        }

        /// <summary>
        /// Call OnLoaded in the specified model and all child models.
        /// </summary>
        protected static void CallOnLoaded(Model model)
        {
            try
            {
                model.OnLoaded();
            }
            catch (ApsimXException)
            {
            }
        }

        /// <summary>
        /// Go through all other models looking for a [Linl] to the specified 'model'.
        /// Connect any links found.
        /// </summary>
        private static void ResolveExternalLinks(Model model)
        {
            foreach (Model externalModel in model.Scope.FindAll())
                ResolveLinks(externalModel, typeof(Model));
        }

        #endregion

    }
}
