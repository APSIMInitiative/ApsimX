namespace Models.PMF
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
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
        OrganNutrientStates Total { get; }

        /// <summary>Gets the live biomass</summary>
        OrganNutrientStates Live { get; }

        /// <summary>Gets the live biomass</summary>
        OrganNutrientStates Dead { get; }

        /// <summary>Gets the senescence rate</summary>
        double senescenceRate { get; }

        /// <summary>Gets the DMConversion efficiency</summary>
        double dmConversionEfficiency { get; }
        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        OrganNutrientStates Allocated { get; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        OrganNutrientStates Senesced { get; }

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
    public class NutrientPoolStates : Model
    {
        /// <summary>Gets or sets the structural.</summary>
        [Units("g/m2")]
        public double Structural { get; set; }
        /// <summary>Gets or sets the storage.</summary>
        [Units("g/m2")]
        public double Storage { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        [Units("g/m2")]
        public double Metabolic { get; set; }
        /// <summary>Gets the total amount of biomass.</summary>
        [Units("g/m2")]
        public double Total
        { get { return Structural + Metabolic + Storage; } }

        /// <summary>the constructor.</summary>
        public NutrientPoolStates()
        {
            Structural = new double();
            Metabolic = new double();
            Storage = new double();
            Clear();
        }

        /// <summary>Clear</summary>
        public void Clear()
        {
            Structural = 0;
            Storage = 0;
            Metabolic = 0;
        }

        /// <summary>Pools can not be negative.  Test for negatives each time an opperator is applied</summary>
        private void testPools(NutrientPoolStates p)
        {
            if (p.Structural < -0.0000000000001)
                    throw new Exception(this.FullPath + ".Structural was set to negative value");
            if (p.Metabolic < -0.0000000000001)
                throw new Exception(this.FullPath + ".Metabolic was set to negative value");
            if (p.Storage < -0.0000000000001)
                throw new Exception(this.FullPath + ".Storage was set to negative value");
            if (Double.IsNaN(p.Structural))
                throw new Exception(this.FullPath + ".Structural was set to nan");
            if (Double.IsNaN(p.Metabolic))
                throw new Exception(this.FullPath + ".Metabolic was set to nan");
            if (Double.IsNaN(p.Storage))
                throw new Exception(this.FullPath + ".Storage was set to nan");
        }
        
        /// <summary>Add Delta</summary>
        public void AddDelta(NutrientPoolStates delta)
        {
            if (delta.Structural < -0.0000000000001)
                throw new Exception(this.FullPath + ".Structural trying to add a negative");
            if (delta.Metabolic < -0.0000000000001)
                throw new Exception(this.FullPath + ".Metabolic trying to add a negative");
            if (delta.Storage < -0.0000000000001)
                throw new Exception(this.FullPath + ".Storage trying to add a negative");

            Structural += delta.Structural;
            Metabolic += delta.Metabolic;
            Storage += delta.Storage;

            testPools(this);
        }

        /// <summary>subtract Delta</summary>
        public void SubtractDelta(NutrientPoolStates delta)
        {
            if (delta.Structural < -0.0000000000001)
                throw new Exception(this.FullPath + ".Structural trying to subtract a negative");
            if (delta.Metabolic < -0.0000000000001)
                throw new Exception(this.FullPath + ".Metabolic trying to subtract a negative");
            if (delta.Storage < -0.0000000000001)
                throw new Exception(this.FullPath + ".Storage trying to subtract a negative");

            Structural -= delta.Structural;
            Metabolic -= delta.Metabolic;
            Storage -= delta.Storage;

            testPools(this);
        }

        /// <summary>Set to new value</summary>
        public void SetTo(NutrientPoolStates newValue)
        {
            Structural = newValue.Structural;
            Metabolic = newValue.Metabolic;
            Storage = newValue.Storage;

            testPools(this);
        }

        /// <summary>multiply by value</summary>
        public void MultiplyBy(double multiplier)
        {
            Structural *= multiplier;
            Metabolic *= multiplier;
            Storage *= multiplier;

            testPools(this);
        }

        /// <summary>divide by value</summary>
        public void DivideBy(double divisor)
        {
            Structural /= divisor;
            Metabolic /= divisor;
            Storage /= divisor;

            testPools(this);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolStates operator /(NutrientPoolStates a, double b)
        {
            return new NutrientPoolStates
            {
                Structural = a.Structural / b,
                Metabolic = a.Metabolic / b,
                Storage = a.Storage / b,
            };
        }

        /// <summary>return pools multiplied by value</summary>
        public static NutrientPoolStates operator *(NutrientPoolStates a, double b)
        {
            return new NutrientPoolStates
            {
                Structural = a.Structural * b,
                Metabolic = a.Metabolic * b,
                Storage = a.Storage * b,
            };
        }

        /// <summary>return sum or two pools</summary>
        public static NutrientPoolStates operator +(NutrientPoolStates a, NutrientPoolStates b)
        {
            return new NutrientPoolStates
            {
                Structural = a.Structural + b.Structural,
                Storage = a.Storage + b.Storage,
                Metabolic = a.Metabolic + b.Metabolic,
            };
        }

        /// <summary>return pool a - pool b</summary>
        public static NutrientPoolStates operator -(NutrientPoolStates a, NutrientPoolStates b)
        {
            return new NutrientPoolStates
            {
                Structural = a.Structural - b.Structural,
                Storage = a.Storage - b.Storage,
                Metabolic = a.Metabolic - b.Metabolic,
            };
        }

    }

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    public class OrganNutrientStates : Model
    {
        /// <summary> The weight of the organ</summary>
        public NutrientPoolStates Weight { get { return Carbon / CarbonConcentration; } }

        /// <summary> The weight of the organ</summary>
        public double Wt { get { return Weight.Total; } }

        /// <summary> The Nitrogen of the organ</summary>
        public double N { get { return Nitrogen.Total; } }

        /// <summary> The Phosphorus of the organ</summary>
        public double P { get { return Phosphorus.Total; } }

        /// <summary> The Potassium of the organ</summary>
        public double K { get { return Phosphorus.Total; } }

        /// <summary> The N concentration of the organ</summary>
        public double NConc { get { return Wt > 0 ? N / Wt : 0; } }

        /// <summary> The P concentration of the organ</summary>
        public double PConc { get { return Wt > 0 ? P / Wt : 0; } }

        /// <summary> The K concentration of the organ</summary>
        public double KConc { get { return Wt > 0 ? K / Wt : 0; } }


        /// <summary> The concentraion of carbon in total dry weight</summary>
        public double CarbonConcentration { get; set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolStates Carbon { get; set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolStates Nitrogen { get; set; }

        /// <summary> The organs phosphorus </summary>
        public NutrientPoolStates Phosphorus { get; set; }

        /// <summary> The organs Potasium components </summary>
        public NutrientPoolStates Potassium { get; set; }

        /// <summary> The organs Carbon components </summary>
        public OrganNutrientStates(double Cconc)
        {
            Carbon = new NutrientPoolStates();
            Nitrogen = new NutrientPoolStates();
            Phosphorus = new NutrientPoolStates();
            Potassium = new NutrientPoolStates();
            CarbonConcentration = Cconc;
        }

        /// <summary> Clear the components </summary>
        public void Clear()
        {
            Carbon.Clear();
            Nitrogen.Clear();
            Phosphorus.Clear();
            Potassium.Clear();
        }

        /// <summary>Add Delta</summary>
        public void SetTo(OrganNutrientStates newValue)
        {
            Carbon = newValue.Carbon;
            Nitrogen = newValue.Nitrogen;
            Phosphorus = newValue.Phosphorus;
            Potassium = newValue.Potassium;
        }

        /// <summary> Multiply components by factor </summary>
        public void MultiplyBy(double  Multiplier)
        {
            Carbon.MultiplyBy(Multiplier);
            Nitrogen.MultiplyBy(Multiplier);
            Phosphorus.MultiplyBy(Multiplier);
            Potassium.MultiplyBy(Multiplier);
        }

        /// <summary> Multiply components by factor </summary>
        public void DivideBy(double divisor)
        {
            Carbon.DivideBy(divisor);
            Nitrogen.DivideBy(divisor);
            Phosphorus.DivideBy(divisor);
            Potassium.DivideBy(divisor);
        }

        /// <summary> Add delta to states </summary>
        public void AddDelta(OrganNutrientStates delta)
        {
            Carbon.AddDelta(delta.Carbon);
            Nitrogen.AddDelta(delta.Nitrogen);
            Phosphorus.AddDelta(delta.Phosphorus);
            Potassium.AddDelta(delta.Potassium);
        }

        /// <summary> subtract delta to states </summary>
        public void SubtractDelta(OrganNutrientStates delta)
        {
            Carbon.SubtractDelta(delta.Carbon);
            Nitrogen.SubtractDelta(delta.Nitrogen);
            Phosphorus.SubtractDelta(delta.Phosphorus);
            Potassium.SubtractDelta(delta.Potassium);
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientStates operator /(OrganNutrientStates a, double b)
        {
            return new OrganNutrientStates(1)
            {
                Carbon = a.Carbon / b,
                Nitrogen = a.Nitrogen / b,
                Phosphorus = a.Phosphorus / b,
                Potassium = a.Potassium / b
            };
        }

        /// <summary>return pools multiplied by value</summary>
        public static OrganNutrientStates operator *(OrganNutrientStates a, double b)
        {
            return new OrganNutrientStates(1)
            {
                Carbon = a.Carbon * b,
                Nitrogen = a.Nitrogen * b,
                Phosphorus = a.Phosphorus * b,
                Potassium = a.Potassium * b
            };
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientStates operator +(OrganNutrientStates a, OrganNutrientStates b)
        {
            return new OrganNutrientStates(1)
            {
                Carbon = a.Carbon + b.Carbon,
                Nitrogen = a.Nitrogen + b.Carbon,
                Phosphorus = a.Phosphorus + b.Carbon,
                Potassium = a.Potassium + b.Carbon
            };
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientStates operator -(OrganNutrientStates a, OrganNutrientStates b)
        {
            return new OrganNutrientStates(1)
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
    public class PlantNutrientDeltas : Model
    {
        /// <summary>The organs on the plant /// </summary>
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
        public double Start { get; set; }
        /// <summary>Gets or sets the end.</summary>
        public double End { get; set; }
        /// <summary>Gets or sets the balance error.</summary>
        public double BalanceError { get; set; }

        /// <summary>The constructor</summary>
        public PlantNutrientDeltas(List<OrganNutrientDelta> orgs)
        {
            ArbitratingOrgans = new List<OrganNutrientDelta>();
            foreach (OrganNutrientDelta org in orgs)
                ArbitratingOrgans.Add(org);
            Start = new double();
            End = new double();
            BalanceError = new double();
        }

        /// <summary>Clear</summary>    
        public void Clear()
        {
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
        public NutrientPoolStates ReAllocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        public NutrientPoolStates ReTranslocation{ get; set; }

        /// <summary>Gets the total supply.</summary>
        public double Total
        { get { return Fixation + ReAllocation.Total + ReTranslocation.Total + Uptake; } }

        /// <summary>The constructor.</summary>
        public OrganNutrientSupplies()
        {
            Fixation = new double();
            ReAllocation = new NutrientPoolStates();
            Uptake = new double();
            ReTranslocation = new NutrientPoolStates();
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
