namespace Models.PMF
{
    using Models.Core;
    using System;
    using System.Collections.Generic;
    
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
        /// <summary>Phospherous</summary>
        public double P { get; private set; }
        /// <summary>Potassium</summary>
        public double K { get; private set; }

        /// <summary>Constructor</summary>
        public NutrientsStates(double c, double n, double p, double k)
        {
            C = c;
            N = n;
            P = p;
            K = k;
        }
    }

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    public class OrganNutrientsState : Model, IParentOfNutrientsPoolState
    {
        /// <summary> The organs Carbon components </summary>
        public Organ parentOrgan = null;

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
        public void UpdateProperties()
        {
            Weight = Carbon / CarbonConcentration;
            Wt = Weight.Total;
            N = Nitrogen.Total;
            P = Phosphorus.Total;
            K = Potassium.Total;
            NConc = Wt > 0 ? N / Wt : 0;
            PConc = Wt > 0 ? P / Wt : 0;
            KConc = Wt > 0 ? K / Wt : 0;
            if (parentOrgan != null)
                parentOrgan.UpdateProperties();
        }

        /// <summary>Constructor </summary>
        public OrganNutrientsState(double Cconc, Organ parentCaller)
        {
            Carbon = new NutrientPoolsState(0, 0, 0, null);
            Nitrogen = new NutrientPoolsState(0, 0, 0, null);
            Phosphorus = new NutrientPoolsState(0, 0, 0, null);
            Potassium = new NutrientPoolsState(0, 0, 0, null);
            CarbonConcentration = Cconc;
            parentOrgan = parentCaller;
        }

        /// <summary> Clear the components </summary>
        public void Clear()
        {
            Carbon.Clear();
            Nitrogen.Clear();
            Phosphorus.Clear();
            Potassium.Clear();
            UpdateProperties();
        }

        /// <summary>Set all nutrient pools to newValue</summary>
        public void SetTo(OrganNutrientsState newValue)
        {
            Carbon = newValue.Carbon;
            Nitrogen = newValue.Nitrogen;
            Phosphorus = newValue.Phosphorus;
            Potassium = newValue.Potassium;
            UpdateProperties();
        }

        /// <summary> Multiply components by factor </summary>
        public void MultiplyBy(double Multiplier)
        {
            Carbon.MultiplyBy(Multiplier, this);
            Nitrogen.MultiplyBy(Multiplier, this);
            Phosphorus.MultiplyBy(Multiplier, this);
            Potassium.MultiplyBy(Multiplier, this);
            UpdateProperties();
        }

        /// <summary> Multiply components by factor </summary>
        public void DivideBy(double divisor)
        {
            Carbon.DivideBy(divisor, this);
            Nitrogen.DivideBy(divisor, this);
            Phosphorus.DivideBy(divisor, this);
            Potassium.DivideBy(divisor, this);
            UpdateProperties();
        }

        /// <summary> Add delta to states </summary>
        public void AddDelta(OrganNutrientsState delta)
        {
            Carbon.AddDelta(delta.Carbon, this);
            Nitrogen.AddDelta(delta.Nitrogen, this);
            Phosphorus.AddDelta(delta.Phosphorus, this);
            Potassium.AddDelta(delta.Potassium, this);
            UpdateProperties();
        }

        /// <summary> subtract delta to states </summary>
        public void SubtractDelta(OrganNutrientsState delta)
        {
            Carbon.SubtractDelta(delta.Carbon, this);
            Nitrogen.SubtractDelta(delta.Nitrogen, this);
            Phosphorus.SubtractDelta(delta.Phosphorus, this);
            Potassium.SubtractDelta(delta.Potassium, this);
            UpdateProperties();
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState operator /(OrganNutrientsState a, double b)
        {
            return new OrganNutrientsState(1, null)
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
            return new OrganNutrientsState(1, null)
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
            return new OrganNutrientsState(1, null)
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
            return new OrganNutrientsState(1, null)
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
            return new OrganNutrientsState(1, null)
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
            return new OrganNutrientsState(1, null)
            {
                Carbon = a.Carbon - b.Carbon,
                Nitrogen = a.Nitrogen - b.Carbon,
                Phosphorus = a.Phosphorus - b.Carbon,
                Potassium = a.Potassium - b.Carbon
            };
        }
    }

    /// <summary>
    /// This is a composite biomass class, representing the sum of 1 or more biomass objects.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeStates : OrganNutrientsState, ICustomDocumentation
    {
        private List<OrganNutrientsState> components = new List<OrganNutrientsState>();

        /// <summary> The concentraion of carbon in total dry weight</summary>
        [Description("Carbon Concentration of biomass")]
        public new double CarbonConcentration { get; set; } = 0.4;

        /// <summary>List of Organ states to include in composite state</summary>
        [Description("List of organs to agregate into composite biomass.")]
        public string[] Propertys { get; set; }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (string PropertyName in Propertys)
            {
                OrganNutrientsState c = (OrganNutrientsState)(this.FindByPath(PropertyName)?.Value);
                if (c == null)
                    throw new Exception("Cannot find: " + PropertyName + " in composite state: " + this.Name);
            }
        }

        /// <summary>/// Add components together to give composite/// </summary>

        [EventSubscribe("PartitioningComplete")]
        public void onPartitioningComplete(object sender, EventArgs e)
        {
            Clear();
            foreach (string PropertyName in Propertys)
            {
                OrganNutrientsState c = (OrganNutrientsState)(this.FindByPath(PropertyName)?.Value);
                AddDelta(c);
            }
        }

        /// <summary>/// The constructor </summary>
        public CompositeStates() : base(0.4, null) { }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name + " Biomass", headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in this.FindAllChildren<IModel>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);

                tags.Add(new AutoDocumentation.Paragraph(this.Name + " summarises the following biomass objects:", indent));
            }
        }
    }
}
