namespace Models.PMF
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// </summary>
    public interface IAmOrganHearMeRoar
    {
        //<summary>The Carbon Element</summary>
        //OrganNutrientDelta Carbon { get; }

        //<summary>The Carbon Element</summary>
        //OrganNutrientDelta Nitrogen { get; }

        /// <summary>Gets the total biomass</summary>
        OrganNutrientsState Total { get; }

        /// <summary>Gets the live biomass</summary>
        OrganNutrientsState Live { get; }

        /// <summary>Gets the live biomass</summary>
        OrganNutrientsState Dead { get; }

        /// <summary>Gets the senescence rate</summary>
        double senescenceRate { get; }

        /// <summary>Gets the DMConversion efficiency</summary>
        double dmConversionEfficiency { get; }
        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        OrganNutrientsState Allocated { get; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        OrganNutrientsState Senesced { get; }

        /// <summary> get the organs uptake object if it has one </summary>
        IWaterNitrogenUptake WaterNitrogenUptakeObject { get; }
    }


    /// <summary>
    /// The class that holds states of Structural, Metabolic and Storage components of a resource
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientPoolsState : Model
    {
        /// <summary>Gets or sets the structural.</summary>
        [Units("g/m2")]
        public double Structural { get; private set; }
        /// <summary>Gets or sets the storage.</summary>
        [Units("g/m2")]
        public double Storage { get; private set; }
        /// <summary>Gets or sets the metabolic.</summary>
        [Units("g/m2")]
        public double Metabolic { get; private set; }
        /// <summary>Gets the total amount of biomass.</summary>
        [Units("g/m2")]
        public double Total { get; private set; }

        private double tolerence = 1e-12;

        private void updateTotal()
        { Total = Structural + Metabolic + Storage; }

        /// <summary>the constructor.</summary>
        public NutrientPoolsState(double structural, double metabolic, double storage)
        {
            Structural = structural;
            Metabolic = metabolic;
            Storage = storage;
            updateTotal();
            testPools(this);
        }

        /// <summary>Clear</summary>
        public void Clear()
        {
            Structural = 0;
            Storage = 0;
            Metabolic = 0;
            Total = 0;
        }

        /// <summary>Pools can not be negative.  Test for negatives each time an opperator is applied</summary>
        private void testPools(NutrientPoolsState p)
        {
            if (p.Structural < 0)
            {
                if (p.Structural < -tolerence) //Throw if really negative
                    throw new Exception(this.FullPath + ".Structural was set to negative value");
                else  // if negative in floating point tollerence, zero the pool
                    this.Structural = 0.0;
            }
            if (p.Metabolic < 0)
            {
                if (p.Metabolic < -tolerence) //Throw if really negative
                    throw new Exception(this.FullPath + ".Metabolic was set to negative value");
                else  // if negative in floating point tollerence, zero the pool
                    this.Metabolic = 0.0;
            }
            if (p.Storage < 0)
            {
                if (p.Storage < -tolerence) //Throw if really negative
                    throw new Exception(this.FullPath + ".Storage was set to negative value");
                else  // if negative in floating point tollerence, zero the pool
                    this.Storage = 0.0;
            }
            if (Double.IsNaN(p.Structural))
                throw new Exception(this.FullPath + ".Structural was set to nan");
            if (Double.IsNaN(p.Metabolic))
                throw new Exception(this.FullPath + ".Metabolic was set to nan");
            if (Double.IsNaN(p.Storage))
                throw new Exception(this.FullPath + ".Storage was set to nan");
        }
        
        /// <summary>Add Delta</summary>
        public void AddDelta(NutrientPoolsState delta)
        {
            if (delta.Structural < -tolerence)
                throw new Exception(this.FullPath + ".Structural trying to add a negative");
            if (delta.Metabolic < -tolerence)
                throw new Exception(this.FullPath + ".Metabolic trying to add a negative");
            if (delta.Storage < -tolerence)
                throw new Exception(this.FullPath + ".Storage trying to add a negative");

            Structural += delta.Structural;
            Metabolic += delta.Metabolic;
            Storage += delta.Storage;
            updateTotal();
            testPools(this);
        }

        /// <summary>subtract Delta</summary>
        public void SubtractDelta(NutrientPoolsState delta)
        {
            if (delta.Structural < -tolerence)
                throw new Exception(this.FullPath + ".Structural trying to subtract a negative");
            if (delta.Metabolic < -tolerence)
                throw new Exception(this.FullPath + ".Metabolic trying to subtract a negative");
            if (delta.Storage < -tolerence)
                throw new Exception(this.FullPath + ".Storage trying to subtract a negative");

            Structural -= delta.Structural;
            Metabolic -= delta.Metabolic;
            Storage -= delta.Storage;
            updateTotal();
            testPools(this);
        }

        /// <summary>Set to new value</summary>
        public void SetTo(NutrientPoolsState newValue)
        {
            Structural = newValue.Structural;
            Metabolic = newValue.Metabolic;
            Storage = newValue.Storage;
            updateTotal();
            testPools(this);
        }

        /// <summary>multiply by value</summary>
        public void MultiplyBy(double multiplier)
        {
            Structural *= multiplier;
            Metabolic *= multiplier;
            Storage *= multiplier;
            updateTotal();
            testPools(this);
        }

        /// <summary>divide by value</summary>
        public void DivideBy(double divisor)
        {
            Structural /= divisor;
            Metabolic /= divisor;
            Storage /= divisor;
            updateTotal();
            testPools(this);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator /(NutrientPoolsState a, double b)
        {
            return new NutrientPoolsState(
            MathUtilities.Divide(a.Structural, b, 0),
            MathUtilities.Divide(a.Metabolic, b, 0),
            MathUtilities.Divide(a.Storage, b, 0));
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator /(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
            MathUtilities.Divide(a.Structural, b.Structural, 0),
            MathUtilities.Divide(a.Metabolic, b.Metabolic, 0),
            MathUtilities.Divide(a.Storage, b.Storage, 0));
        }

        /// <summary>return pools multiplied by value</summary>
        public static NutrientPoolsState operator *(NutrientPoolsState a, double b)
        {
            return new NutrientPoolsState(
                a.Structural * b,
                a.Metabolic * b,
                a.Storage * b);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator *(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural * b.Structural,
                a.Metabolic * b.Metabolic,
                a.Storage * b.Storage);
        }

        /// <summary>return sum or two pools</summary>
        public static NutrientPoolsState operator +(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural + b.Structural,
                a.Storage + b.Storage,
                a.Metabolic + b.Metabolic);
        }

        /// <summary>return pool a - pool b</summary>
        public static NutrientPoolsState operator -(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural - b.Structural,
                a.Storage - b.Storage,
                a.Metabolic - b.Metabolic);
        }

    }

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    public class OrganNutrientsState : Model
    {
        /// <summary> The weight of the organ</summary>
        public NutrientPoolsState Weight { get; private set; }

        /// <summary> The weight of the organ</summary>
        public double Wt { get; private set; }

        /// <summary> The Nitrogen of the organ</summary>
        public double N { get; private set; }

        /// <summary> The Phosphorus of the organ</summary>
        public double P { get; private set; }

        /// <summary> The Potassium of the organ</summary>
        public double K { get; private set; }

        /// <summary> The N concentration of the organ</summary>
        public double NConc { get; private set; }

        /// <summary> The P concentration of the organ</summary>
        public double PConc { get; private set; }

        /// <summary> The K concentration of the organ</summary>
        public double KConc { get; private set; }


        /// <summary> The concentraion of carbon in total dry weight</summary>
        public double CarbonConcentration { get; private set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolsState Carbon { get; private set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolsState Nitrogen { get; private set; }

        /// <summary> The organs phosphorus </summary>
        public NutrientPoolsState Phosphorus { get; private set; }

        /// <summary> The organs Potasium components </summary>
        public NutrientPoolsState Potassium { get; private set; }

        /// <summary> update variables derived from NutrientPoolsStates </summary>
        public void updateAgregateValues()
        {
            Weight = Carbon / CarbonConcentration;
            Wt = Weight.Total;
            N = Nitrogen.Total;
            P = Phosphorus.Total;
            K = Potassium.Total;
            NConc = Wt > 0 ? N / Wt : 0;
            PConc = Wt > 0 ? P / Wt : 0;
            KConc = Wt > 0 ? K / Wt : 0;
        }

        /// <summary> The organs Carbon components </summary>
        public OrganNutrientsState(double Cconc)
        {
            Carbon = new NutrientPoolsState(0,0,0);
            Nitrogen = new NutrientPoolsState(0, 0, 0);
            Phosphorus = new NutrientPoolsState(0, 0, 0);
            Potassium = new NutrientPoolsState(0, 0, 0);
            CarbonConcentration = Cconc;
            updateAgregateValues();
        }

        /// <summary> Clear the components </summary>
        public void Clear()
        {
            Carbon.Clear();
            Nitrogen.Clear();
            Phosphorus.Clear();
            Potassium.Clear();
            updateAgregateValues();
        }

        /// <summary>Add Delta</summary>
        public void SetTo(OrganNutrientsState newValue)
        {
            Carbon = newValue.Carbon;
            Nitrogen = newValue.Nitrogen;
            Phosphorus = newValue.Phosphorus;
            Potassium = newValue.Potassium;
            updateAgregateValues();
        }

        /// <summary> Multiply components by factor </summary>
        public void MultiplyBy(double  Multiplier)
        {
            Carbon.MultiplyBy(Multiplier);
            Nitrogen.MultiplyBy(Multiplier);
            Phosphorus.MultiplyBy(Multiplier);
            Potassium.MultiplyBy(Multiplier);
            updateAgregateValues();
        }

        /// <summary> Multiply components by factor </summary>
        public void DivideBy(double divisor)
        {
            Carbon.DivideBy(divisor);
            Nitrogen.DivideBy(divisor);
            Phosphorus.DivideBy(divisor);
            Potassium.DivideBy(divisor);
            updateAgregateValues();
        }

        /// <summary> Add delta to states </summary>
        public void AddDelta(OrganNutrientsState delta)
        {
            Carbon.AddDelta(delta.Carbon);
            Nitrogen.AddDelta(delta.Nitrogen);
            Phosphorus.AddDelta(delta.Phosphorus);
            Potassium.AddDelta(delta.Potassium);
            updateAgregateValues();
        }

        /// <summary> subtract delta to states </summary>
        public void SubtractDelta(OrganNutrientsState delta)
        {
            Carbon.SubtractDelta(delta.Carbon);
            Nitrogen.SubtractDelta(delta.Nitrogen);
            Phosphorus.SubtractDelta(delta.Phosphorus);
            Potassium.SubtractDelta(delta.Potassium);
            updateAgregateValues();
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator /(OrganNutrientsState a, double b)
        {
            return new OrganNutrientsState(1)
            {
                Carbon = a.Carbon / b,
                Nitrogen = a.Nitrogen / b,
                Phosphorus = a.Phosphorus / b,
                Potassium = a.Potassium / b
            };
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator /(OrganNutrientsState a, OrganNutrientsState b)
        {
            return new OrganNutrientsState(1)
            {
                Carbon = a.Carbon / b.Carbon,
                Nitrogen = a.Nitrogen / b.Nitrogen,
                Phosphorus = a.Phosphorus / b.Phosphorus,
                Potassium = a.Potassium / b.Potassium,
            };
        }
        /// <summary>return pools multiplied by value</summary>
        public static OrganNutrientsState operator *(OrganNutrientsState a, double b)
        {
            return new OrganNutrientsState(1)
            {
                Carbon = a.Carbon * b,
                Nitrogen = a.Nitrogen * b,
                Phosphorus = a.Phosphorus * b,
                Potassium = a.Potassium * b
            };
        }


        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator *(OrganNutrientsState a, OrganNutrientsState b)
        {
            return new OrganNutrientsState(1)
            {
                Carbon = a.Carbon * b.Carbon,
                Nitrogen = a.Nitrogen * b.Nitrogen,
                Phosphorus = a.Phosphorus * b.Phosphorus,
                Potassium = a.Potassium * b.Potassium,
            };
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientsState operator +(OrganNutrientsState a, OrganNutrientsState b)
        {
            return new OrganNutrientsState(1)
            {
                Carbon = a.Carbon + b.Carbon,
                Nitrogen = a.Nitrogen + b.Carbon,
                Phosphorus = a.Phosphorus + b.Carbon,
                Potassium = a.Potassium + b.Carbon
            };
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientsState operator -(OrganNutrientsState a, OrganNutrientsState b)
        {
            return new OrganNutrientsState(1)
            {
                Carbon = a.Carbon - b.Carbon,
                Nitrogen = a.Nitrogen - b.Carbon,
                Phosphorus = a.Phosphorus - b.Carbon,
                Potassium = a.Potassium - b.Carbon
            };
        }



    }

    /// <summary>
    /// The daily state of flows throughout the plant
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(BiomassArbitrator))]
    public class PlantNutrientsDelta : Model
    {
        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        /// <summary>List of Organ states to include in composite state</summary>
        [Description("Supply List of organs to customise order.")]
        public string[] Propertys { get; set; }

        /// <summary>The organs on the plant /// </summary>
        [JsonIgnore]
        public List<OrganNutrientDelta> ArbitratingOrgans { get; set; }

        /// <summary>The total supply of resoure that may be allocated /// </summary>
        public double TotalPlantSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.Total); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalReAllocationSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.ReAllocation.Total); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalUptakeSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.Uptake); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalFixationSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.Fixation); } }
        /// <summary>The total supply from Retranslocation  /// </summary>
        public double TotalReTranslocationSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.ReTranslocation.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantDemand { get { return ArbitratingOrgans.Sum(o => o.Demands.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantPriorityScalledDemand { get { return ArbitratingOrgans.Sum(o => o.PriorityScaledDemand.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantDemandsAllocated { get { return ArbitratingOrgans.Sum(o => o.DemandsAllocated.Total); } }

        //Error checking variables
        /// <summary>Gets or sets the start.</summary>
        [JsonIgnore]
        public double Start { get; set; }
        /// <summary>Gets or sets the end.</summary>
        [JsonIgnore]
        public double End { get; set; }
        /// <summary>Gets or sets the balance error.</summary>
        [JsonIgnore]
        public double BalanceError { get; set; }

        /// <summary>The constructor</summary>
        public PlantNutrientsDelta()
        {
            ArbitratingOrgans = new List<OrganNutrientDelta>();
            Start = new double();
            End = new double();
            BalanceError = new double();
        }

        /// <summary>Clear</summary>    
        public void Clear()
        {
        }

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            ArbitratingOrgans = new List<OrganNutrientDelta>();
            //If Propertys has a list of organ names then use that as a custom ordered list
            var organs = Propertys?.Select(organName => plant.FindChild(organName));
            organs = organs ?? plant.FindAllChildren<Organ>();

            foreach (var organ in organs)
            {
                //Should we throw an exception here if the organ does not have an OrganNutrientDelta? 
                var nutrientDelta = organ.FindChild(Name) as OrganNutrientDelta;
                if (nutrientDelta != null)
                    ArbitratingOrgans.Add(nutrientDelta);
            }

            Start = new double();
            End = new double();
            BalanceError = new double();
        }
    }

    /// <summary>
    /// The class that holds the states for resource supplies from ReAllocation, Uptake, Fixation and ReTranslocation
    /// </summary>
    [Serializable]
    public class OrganNutrientSupplies: Model
    {
        /// <summary>Gets or sets the fixation.</summary>
        public double Fixation { get; set; }
        /// <summary>Gets or sets the reallocation.</summary>
        public NutrientPoolsState ReAllocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        public NutrientPoolsState ReTranslocation{ get; set; }

        /// <summary>Gets the total supply.</summary>
        public double Total
        { get { return Fixation + ReAllocation.Total + ReTranslocation.Total + Uptake; } }

        /// <summary>The constructor.</summary>
        public OrganNutrientSupplies()
        {
            Fixation = new double();
            ReAllocation = new NutrientPoolsState(0,0,0);
            Uptake = new double();
            ReTranslocation = new NutrientPoolsState(0,0,0);
        }

        internal void Clear()
        {
            Fixation = 0;
            ReAllocation.Clear();
            Uptake = 0;
            ReTranslocation.Clear();
        }
    }
}
