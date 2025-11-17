using System;
using System.Collections.Generic;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;

namespace Models.PMF
{

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    public class NutrientsStates : Model
    {
        /// <summary>Carbon</summary>
        public double C { get; private set; }
        /// <summary>Nitrogen</summary>
        public double N { get; private set; }

        /// <summary>Constructor</summary>
        public NutrientsStates(double c, double n, double p, double k)
        {
            C = c;
            N = n;
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
        public NutrientPoolsState Weight => Cconc > 0 ? Carbon / Cconc : new NutrientPoolsState(0, 0, 0);

        /// <summary> The weight of the organ (g)</summary>
        public double Wt => Weight.Total;

        /// <summary> The Carbon of the organ</summary>
        public double C => Carbon.Total;

        /// <summary> The Nitrogen of the organ</summary>
        public double N => Nitrogen.Total;

        /// <summary> The N concentration of the organ (g/g)</summary>
        public double NConc => Wt > 0 ? N / Wt : 0;


        /// <summary> The concentraion of carbon in total dry weight (g/g)</summary>
        public double Cconc { get; private set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolsState Carbon { get; private set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolsState Nitrogen { get; private set; }

        /// <summary>Constructor </summary>
        public OrganNutrientsState(NutrientPoolsState carbon, NutrientPoolsState nitrogen, double cconc)
        {
            Set(carbon:carbon, nitrogen:nitrogen);
            Cconc = cconc;
        }

        /// <summary>Constructor </summary>
        public OrganNutrientsState(double cconc)
        {
            Carbon = new NutrientPoolsState();
            Nitrogen = new NutrientPoolsState();
            Cconc = cconc;
        }

        /// <summary>Constructor </summary>
        public OrganNutrientsState()
        {
            Carbon = new NutrientPoolsState();
            Nitrogen = new NutrientPoolsState();
            Cconc = 1.0;
        }

        /// <summary>Constructor </summary>
        public void Clear()
        {
            Carbon.Clear();
            Nitrogen.Clear();
        }

        /// <summary>Set the current state </summary>
        public void Set(NutrientPoolsState carbon, NutrientPoolsState nitrogen)
        {
            Carbon = carbon;
            Nitrogen = nitrogen;
        }

        /// <summary>Set the current state and change the cconc</summary>
        public void Set(OrganNutrientsState set, double cconc)
        {
            Set(carbon:set.Carbon,nitrogen:set.Nitrogen);
            Cconc = cconc;
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator /(OrganNutrientsState a, double b)
        {
            OrganNutrientsState ret = new OrganNutrientsState(a.Cconc);
            ret.Carbon = a.Carbon / b;
            ret.Nitrogen = a.Nitrogen / b;
            return ret;
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator /(OrganNutrientsState a, OrganNutrientsState b)
        {
            OrganNutrientsState ret = new OrganNutrientsState(a.Cconc);
            ret.Carbon = a.Carbon / b.Carbon;
            ret.Nitrogen = a.Nitrogen / b.Nitrogen;
            return ret;
        }

        /// <summary>return pools multiplied by value</summary>
        public static OrganNutrientsState operator *(OrganNutrientsState a, double b)
        {
            OrganNutrientsState ret = new OrganNutrientsState(a.Cconc);
            ret.Carbon = a.Carbon * b;
            ret.Nitrogen = a.Nitrogen * b;
            return ret;
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator *(OrganNutrientsState a, OrganNutrientsState b)
        {
            OrganNutrientsState ret = new OrganNutrientsState(a.Cconc);
            ret.Carbon = a.Carbon * b.Carbon;
            ret.Nitrogen = a.Nitrogen * b.Nitrogen;
            return ret;
        }

         /// <summary>return sum or two pools</summary>
        public static OrganNutrientsState operator +(OrganNutrientsState a, OrganNutrientsState b)
        {
            OrganNutrientsState ret = new OrganNutrientsState(a.Cconc);
            ret.Carbon = a.Carbon + b.Carbon;
            ret.Nitrogen = a.Nitrogen + b.Nitrogen;
            return ret;
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientsState operator -(OrganNutrientsState a, OrganNutrientsState b)
        {
            OrganNutrientsState ret = new OrganNutrientsState(a.Cconc);
            ret.Carbon = a.Carbon - b.Carbon;
            ret.Nitrogen = a.Nitrogen - b.Nitrogen;
            return ret;
        }

        /// <summary>Initializes a new instance of the <see cref="Biomass"/> class from the OrganNutrientState passed in</summary>
        public Biomass ToBiomass
        {
            get
            {
                Biomass retBiomass = new Biomass();
                retBiomass.StructuralWt = this.Weight.Structural;
                retBiomass.MetabolicWt = this.Weight.Metabolic;
                retBiomass.StorageWt = this.Weight.Storage;
                retBiomass.StructuralN = this.Nitrogen.Structural;
                retBiomass.MetabolicN = this.Nitrogen.Metabolic;
                retBiomass.StorageN = this.Nitrogen.Storage;
                return retBiomass;
            }
        }
    }


    /// <summary>
    /// This is a composite biomass class, representing the sum of 1 or more biomass objects.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeStates : OrganNutrientsState, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        private List<OrganNutrientsState> components = new List<OrganNutrientsState>();

        /// <summary>List of Organ states to include in composite state</summary>
        [Description("List of organs to agregate into composite biomass.")]
        public string[] Propertys { get; set; }

        [Link(Type = LinkType.Ancestor)]
        Plant parentPlant = null;


        /// <summary>/// Add components together to give composite/// </summary>

        [EventSubscribe("PartitioningComplete")]
        public void onPartitioningComplete(object sender, EventArgs e)
        {
            Clear();
            if (parentPlant.IsAlive)
            {
                foreach (string PropertyName in Propertys)
                {
                    OrganNutrientsState c = (OrganNutrientsState)Structure.Get(PropertyName);
                    AddDelta(c);
                }
            }
        }
        private void AddDelta(OrganNutrientsState delta)
        {
            double agrigatedCconc = MathUtilities.Divide((this.Carbon.Total + delta.Carbon.Total) , (this.Wt + delta.Wt),1);
            Set(this + delta,agrigatedCconc);
        }

        /// <summary>/// The constructor </summary>
        public CompositeStates() : base() { }

    }
}
