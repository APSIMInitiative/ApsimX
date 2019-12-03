// -----------------------------------------------------------------------
// <copyright file="StockView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Gtk;
    using Interfaces;
    using Models.GrazPlan;

    public class StockView : ViewBase, IStockView
    {
        // death rate item in the SingleGenoTypeInits.DeathRate array
        private const int ADULT = 0;
        private const int WNR = 1;
        
        private const int MAXGENOTYPES = 20;
        private const int MAXANIMALGROUPS = 100;
        private const double DAY2MONTH = 12.0 / 365.25;

        private struct SexRecord
        {
            public string Text;
            public GrazType.ReproType Repro;
        }
        private readonly int[] REPRO2MAP = { 0, 2, 1 };
        private readonly SexRecord[,] SEXMAP = { { new SexRecord() {Text = "wethers", Repro = GrazType.ReproType.Castrated }, new SexRecord() {Text="ewes", Repro = GrazType.ReproType.Empty }, new SexRecord() {Text ="rams", Repro=GrazType.ReproType.Male} },
                                                 { new SexRecord() {Text = "steers",  Repro = GrazType.ReproType.Castrated }, new SexRecord() {Text="cows", Repro = GrazType.ReproType.Empty }, new SexRecord() {Text = "bulls", Repro = GrazType.ReproType.Male} } };

        private int currentGenotype;    // selected genotype
        private int currentGroup = -1;       // selected animal group
        private GrazType.AnimalType[] genotypeAnimals = new GrazType.AnimalType[20];    // animal types for each genotype in the list

        /// <summary>
        /// The array of initial animal groups that get assigned to paddocks 
        /// </summary>
        private AnimalInits[] animalInits = new AnimalInits[0];

        private AnimalParamSet paramSet;
        private AnimalParamSet genotypeSet;
        private bool filling = false;

        private const double MINSHEEPSRW = 5.0;
        private const double MAXSHEEPSRW = 100.0;
        private const double MINCATTLESRW = 30.0;
        private const double MAXCATTLESRW = 1200.0;

        /// <summary>
        /// The array of genotype specifications from the component inits
        /// </summary>
        private SingleGenotypeInits[] genotypeInits;


        private Notebook notebook1 = null;
        // genotypes tab
        private Frame gbxGenotype = null;
        private Entry edtGenotypeName = null;
        private Gtk.TreeView lbxGenotypeList = null;
        private Button btnNewGeno = null;
        private Button btnDelGeno = null;
        private DropDownView cbxGeneration = null;
        private DropDownView cbxDamBreed = null;
        private DropDownView cbxSireBreed = null;
        private RadioButton rbtnSheep = null;
        private RadioButton rbtnCattle = null;
        private Label lblConception3 = null;
        private Label unitConception = null;
        private Label lblBreedPFWPeakMilk = null;
        private Label unitBreedPFWPeakMilk = null;
        private Label untWoolYield = null;
        private Label lblWoolYield = null;
        private Label lblDamBreed = null;
        private Label lblSireBreed = null;
        private Label lblBreedMaxMu = null;
        private Label unitBreedMaxMu = null;

        // the wrappers for the edit controls that have floating points
        private DoubleEditView deWnrDeath = null;
        private DoubleEditView deDeath = null;
        private DoubleEditView dePFWMilk = null;
        private DoubleEditView deBreedSRW = null;
        private DoubleEditView deBreedMaxMu = null;
        private DoubleEditView deWoolYield = null;
        private DoubleEditView deConception1 = null;
        private DoubleEditView deConception2 = null;
        private DoubleEditView deConception3 = null;

        // animals tab
        private Frame gbxAnimals = null;
        private Gtk.TreeView lbxAnimalList = null;
        private DropDownView cbxGroupGenotype = null;
        private DropDownView cbxSex = null;
        private Button btnNewAnimals = null;
        private Button btnDeleteAnimals = null;
        private DoubleEditView deNumber = null;
        private DoubleEditView deAge = null;
        private DoubleEditView deTag = null;
        private DoubleEditView deWeight = null;
        private DoubleEditView dePriority = null;
        private Entry edtPaddock = null;
        private DoubleEditView dePrevWt = null;
        private DoubleEditView deFibreDiam = null;
        private DoubleEditView deFleece = null;
        private Label lblPrevWt = null;
        private Label untPrevWt = null;
        private Label lblFleece = null;
        private Label untFleece = null;
        private Label lblFibreDiam = null;
        private Label untFibreDiam = null;
        private Label lblError = null;
        // Animals reproduction frame
        private Frame pnlRepro = null;
        private Frame rgrpSRepro = null;
        private Frame rgrpCPreg = null;
        private RadioButton rbDryEmpty = null;
        private RadioButton rbPregS = null;
        private RadioButton rbLact = null;
        private RadioButton rbEmpty = null;
        private RadioButton rbPreg = null;
        private Frame rgrpCLact = null;
        private RadioButton rbNoLact = null;
        private RadioButton rbLac = null;
        private RadioButton rbLactCalf = null;
        private Frame rgrpNoLambs = null;

        /// <summary>
        /// The list of genotypes in the treeview
        /// </summary>
        private ListStore genoList = new ListStore(typeof(string));

        /// <summary>
        /// The list of animal groups in the listbox
        /// </summary>
        private ListStore groupsList = new ListStore(typeof(string));

        public event EventHandler<GenotypeInitArgs> GetGenoParams;
        public event NormalWeightDelegate OnCalcNormalWeight;

        public StockView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.StockView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            notebook1.SwitchPage += TabControl1_SelectedIndexChanged;

            gbxGenotype = (Frame)builder.GetObject("gbxGenotype");
            edtGenotypeName = (Entry)builder.GetObject("edtGenotypeName");
            btnNewGeno = (Button)builder.GetObject("btnNewGeno");
            btnDelGeno = (Button)builder.GetObject("btnDelGeno");
            rbtnSheep = (Gtk.RadioButton)builder.GetObject("rbtnSheep");
            rbtnCattle = (Gtk.RadioButton)builder.GetObject("rbtnCattle");
            lblConception3 = (Label)builder.GetObject("lblConception3");
            unitConception = (Label)builder.GetObject("unitConception");
            lblBreedPFWPeakMilk = (Label)builder.GetObject("lblBreedPFW_PeakMilk");
            unitBreedPFWPeakMilk = (Label)builder.GetObject("unitBreedPFW_PeakMilk");
            untWoolYield = (Label)builder.GetObject("untWoolYield");
            lblWoolYield = (Label)builder.GetObject("lblWoolYield");
            lblDamBreed = (Label)builder.GetObject("lblDamBreed");
            lblSireBreed = (Label)builder.GetObject("lblSireBreed");
            lblBreedMaxMu = (Label)builder.GetObject("lblBreedMaxMu");
            unitBreedMaxMu = (Label)builder.GetObject("unitBreedMaxMu");

            cbxDamBreed = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxDamBreed"));
            cbxSireBreed = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxSireBreed"));
            cbxGeneration = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxGeneration"));

            deWnrDeath = new DoubleEditView(this, (Entry)builder.GetObject("edtWnrDeathRate"), 100, 0, 1);
            deDeath = new DoubleEditView(this, (Entry)builder.GetObject("edtDeathRate"), 100, 0, 1);
            dePFWMilk = new DoubleEditView(this, (Entry)builder.GetObject("edtBreedPFW_PeakMilk"), 100, 0, 2);
            deBreedSRW = new DoubleEditView(this, (Entry)builder.GetObject("edtBreedSRW"));
            deBreedSRW.DecPlaces = 1;
            deBreedMaxMu = new DoubleEditView(this, (Entry)builder.GetObject("edtBreedMaxMu"), 50, 5, 1);
            deWoolYield = new DoubleEditView(this, (Entry)builder.GetObject("edtWoolYield"), 100, 50, 1);
            deConception1 = new DoubleEditView(this, (Entry)builder.GetObject("edtConception1"), 100, 0, 0);
            deConception2 = new DoubleEditView(this, (Entry)builder.GetObject("edtConception2"), 100, 0, 0);
            deConception3 = new DoubleEditView(this, (Entry)builder.GetObject("edtConception3"), 100, 0, 0);

            cbxGeneration.Values = new string[] { "Purebred", "First cross", "Second cross", "Third cross", "Fourth cross", "Fifth cross", "Sixth cross" };

            // Animals tab
            gbxAnimals = (Frame)builder.GetObject("gbxAnimals");
            cbxGroupGenotype = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxGroupGenotype"));
            btnNewAnimals = (Button)builder.GetObject("btnNewAnimals");
            btnDeleteAnimals = (Button)builder.GetObject("btnDeleteAnimals");
            cbxSex = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxSex"));
            deNumber = new DoubleEditView(this, (Entry)builder.GetObject("edtNumber"), 100000, 0, 0);
            deAge = new DoubleEditView(this, (Entry)builder.GetObject("edtAge"), 100, 0, 1);
            deWeight = new DoubleEditView(this, (Entry)builder.GetObject("edtWeight"), 1200, 0, 1);
            edtPaddock = (Entry)builder.GetObject("edtPaddock");
            deTag = new DoubleEditView(this, (Entry)builder.GetObject("edtTag"), 1000, 0, 0);
            dePriority = new DoubleEditView(this, (Entry)builder.GetObject("edtPriority"), 10, 0, 0);
            dePrevWt = new DoubleEditView(this, (Entry)builder.GetObject("edtPrevWt"), 1200, 0, 1);
            deFibreDiam = new DoubleEditView(this, (Entry)builder.GetObject("edtFibreDiam"), 35, 0, 1);
            deFleece = new DoubleEditView(this, (Entry)builder.GetObject("edtFleece"), 25, 0, 1);
            lblPrevWt = (Label)builder.GetObject("lblPrevWt");
            untPrevWt = (Label)builder.GetObject("untPrevWt");
            lblFleece = (Label)builder.GetObject("lblFleece");
            untFleece = (Label)builder.GetObject("untFleece");
            lblFibreDiam = (Label)builder.GetObject("lblFibreDiam");
            untFibreDiam = (Label)builder.GetObject("untFibreDiam");
            lblError = (Label)builder.GetObject("lblError");
            pnlRepro = (Frame)builder.GetObject("pnlRepro");
            rgrpSRepro = (Frame)builder.GetObject("rgrpSRepro");
            rgrpCPreg = (Frame)builder.GetObject("rgrpCPreg");
            rbDryEmpty = (Gtk.RadioButton)builder.GetObject("rbDryEmpty");
            rbPregS = (Gtk.RadioButton)builder.GetObject("rbPregS");
            rbLact = (Gtk.RadioButton)builder.GetObject("rbLact");
            rbEmpty = (Gtk.RadioButton)builder.GetObject("rbEmpty");
            rbPreg = (Gtk.RadioButton)builder.GetObject("rbPreg");
            rgrpCLact = (Frame)builder.GetObject("rgrpCLact");
            rbNoLact = (Gtk.RadioButton)builder.GetObject("rbNoLact");
            rbLac = (Gtk.RadioButton)builder.GetObject("rbLac");
            rbLactCalf = (Gtk.RadioButton)builder.GetObject("rbLactCalf");
            rgrpNoLambs = (Frame)builder.GetObject("rgrpNoLambs");

            // configure the treeview of animal groups
            lbxAnimalList = (Gtk.TreeView)builder.GetObject("tvAnimals");
            lbxAnimalList.Model = groupsList;
            CellRendererText textRenderA = new Gtk.CellRendererText();
            TreeViewColumn columnA = new TreeViewColumn("Animal Groups", textRenderA, "text", 0);
            lbxAnimalList.AppendColumn(columnA);
            lbxAnimalList.HeadersVisible = false;
            lbxAnimalList.CursorChanged += LbxAnimalList_SelectedIndexChanged;

            btnNewAnimals.Clicked += BtnNewAnimals_Clicked;
            btnDeleteAnimals.Clicked += BtnDeleteAnimals_Clicked;

            // configure the treeview of genotype names
            lbxGenotypeList = (Gtk.TreeView)builder.GetObject("tvGenotypes");
            lbxGenotypeList.Model = genoList;
            CellRendererText textRender = new Gtk.CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Genotype Names", textRender, "text", 0);
            lbxGenotypeList.AppendColumn(column);
            lbxGenotypeList.HeadersVisible = false;
            lbxGenotypeList.CursorChanged += LbxGenotypeList_SelectedIndexChanged;

            btnNewGeno.Clicked += BtnNewGeno_Clicked;
            btnDelGeno.Clicked += BtnDelGeno_Clicked;
            edtGenotypeName.Changed += ChangeGenotypeName;
            rbtnSheep.Clicked += ClickAnimal;
            rbtnCattle.Clicked += ClickAnimal;
            cbxDamBreed.Changed += ChangeBreed;
            cbxGeneration.Changed += ChangeGeneration;
            deNumber.OnChange += ChangeNumber;
            this.cbxSex.Changed += this.ChangeSex;
            cbxGroupGenotype.Changed += ChangeGroupGenotype;
            deWeight.OnChange += this.ChangeEditCtrl;
            dePrevWt.OnChange += this.ChangeEditCtrl;
            deAge.OnChange += this.ChangeEditCtrl;

            mainWidget = notebook1;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            // detach events
            btnNewGeno.Clicked -= BtnNewGeno_Clicked;
            btnDelGeno.Clicked -= BtnDelGeno_Clicked;
            edtGenotypeName.Changed -= ChangeGenotypeName;
            rbtnSheep.Clicked -= ClickAnimal;
            rbtnCattle.Clicked -= ClickAnimal;
            cbxDamBreed.Changed -= ChangeBreed;
            cbxGeneration.Changed -= ChangeGeneration;
            notebook1.SwitchPage -= TabControl1_SelectedIndexChanged;

            btnNewAnimals.Clicked -= BtnNewAnimals_Clicked;
            cbxGroupGenotype.Changed -= ChangeGroupGenotype;
            deNumber.OnChange -= ChangeNumber;
            deWeight.OnChange -= this.ChangeEditCtrl;
            dePrevWt.OnChange -= this.ChangeEditCtrl;
            deAge.OnChange -= this.ChangeEditCtrl;

        }

        /// <summary>
        /// The list of animal groups
        /// </summary>
        public AnimalInits[] AnimalGroups
        {
            get
            {
                if (currentGroup >= 0)
                    ParseCurrGroup();
                return animalInits;
            }
            set
            {
                animalInits = new AnimalInits[value.Length];
                value.CopyTo(animalInits, 0);
            }
        }

        /// <summary>
        /// The list of genotypes in the component's inits
        /// </summary>
        public SingleGenotypeInits[] Genotypes
        {
            get
            {
                if (currentGenotype >= 0)
                    ParseCurrGenotype();
                return genotypeInits;
            }
            set
            {
                genotypeInits = new SingleGenotypeInits[value.Length];
                value.CopyTo(genotypeInits, 0);
            }
        }

        /// <summary>
        /// Responds after a GetGenoParams event is called
        /// </summary>
        /// <param name="animalParams"></param>
        public void SetGenoParams(AnimalParamSet animalParams)
        {
            this.genotypeSet = animalParams;
        }

        /// <summary>
        /// Set up the form controls with the intial values from the model
        /// </summary>
        public void SetValues()
        {
            this.paramSet = StockList.MakeParamSet("");   // can use the param filename from component inits

            genoList.Clear();
            string[] genoNames = new string[genotypeInits.Length];
            for (int i = 0; i < genotypeInits.Length; i++)
            {
                genoNames[i] = genotypeInits[i].GenotypeName;
                genoList.AppendValues(genotypeInits[i].GenotypeName);
            }
            cbxGroupGenotype.Values = genoNames;        // animals tab

            GenotypeInitArgs args = new GenotypeInitArgs();
            args.Genotypes = genotypeInits;
            args.ParamSet = this.paramSet;
            for (int idx = 0; idx < genotypeInits.Length; idx++)
            {
                args.Index = idx;
                GetGenoParams.Invoke(null, args);   // gets params from the stock model

                if (this.genotypeSet != null)
                {
                    genotypeAnimals[idx] = this.genotypeSet.Animal;
                }
                else
                    genotypeAnimals[idx] = GrazType.AnimalType.Sheep;
            }
            currentGenotype = Math.Min(0, genotypeInits.Length - 1);
            FillCurrGenotype();

            filling = true;
            if (currentGenotype >= 0)
                SelectedGenoIndex = currentGenotype;
            filling = false;

            EnableButtons();
        }

        /// <summary>
        /// Fill the controls on the form
        /// </summary>
        private void FillCurrGenotype()
        {
            GrazType.AnimalType theAnimal;
            SingleGenotypeInits theGenoType;

            if (currentGenotype < 0)
                gbxGenotype.Hide();
            else
                gbxGenotype.Show();


            filling = true;

            if (currentGenotype >= 0)
            {
                theGenoType = this.genotypeInits[currentGenotype];
                theAnimal = genotypeAnimals[currentGenotype];

                if (theAnimal == GrazType.AnimalType.Sheep)
                {
                    deBreedSRW.MaxValue = MAXSHEEPSRW;
                    deBreedSRW.MinValue = MINSHEEPSRW;
                }
                else if (theAnimal == GrazType.AnimalType.Cattle)
                {
                    deBreedSRW.MaxValue = MAXCATTLESRW;
                    deBreedSRW.MinValue = MINCATTLESRW;
                }

                // Enable controls for Peak milk or fleece details
                EnablePeakMilkOrFleece(theAnimal);

                edtGenotypeName.Text = theGenoType.GenotypeName;

                rbtnSheep.Active = (theAnimal == GrazType.AnimalType.Sheep);
                rbtnCattle.Active = (theAnimal == GrazType.AnimalType.Cattle);
                if (rbtnSheep.Active)
                    rbtnSheep.Click();
                else
                    rbtnCattle.Click();

                cbxGeneration.SelectedIndex = Math.Max(0, Math.Min(theGenoType.Generation, cbxGeneration.Values.Length - 1));
                ChangeGeneration(null, null);

                if ((theGenoType.Generation == 0) && (theGenoType.DamBreed == ""))                    //sDamBreed
                    cbxDamBreed.SelectedIndex = cbxDamBreed.IndexOf(theGenoType.GenotypeName);
                else
                    cbxDamBreed.SelectedIndex = cbxDamBreed.IndexOf(theGenoType.DamBreed);

                cbxSireBreed.SelectedIndex = cbxSireBreed.IndexOf(theGenoType.SireBreed);
                if (cbxSireBreed.SelectedIndex < 0)
                    cbxSireBreed.SelectedIndex = cbxDamBreed.SelectedIndex;

                deBreedSRW.Value = theGenoType.SRW;
                deDeath.Value = 100.0 * theGenoType.DeathRate[ADULT];
                deWnrDeath.Value = 100 * theGenoType.DeathRate[WNR];
                deConception1.Value = 100 * theGenoType.Conceptions[1];
                deConception2.Value = 100 * theGenoType.Conceptions[2];

                if (theAnimal == GrazType.AnimalType.Sheep)
                {
                    deConception3.Value = 100 * theGenoType.Conceptions[3];
                    dePFWMilk.DecPlaces = 2;
                    dePFWMilk.MinValue = 0.0;
                    dePFWMilk.Value = theGenoType.PotFleeceWt;  // dual purpose control
                    deBreedMaxMu.Value = theGenoType.MaxFibreDiam;
                    deWoolYield.Value = 100 * theGenoType.FleeceYield;
                }
                else if (theAnimal == GrazType.AnimalType.Cattle)
                {
                    dePFWMilk.DecPlaces = 1;
                    dePFWMilk.MinValue = Math.Min(10.0, deBreedSRW.Value * 0.01);
                    dePFWMilk.Value = theGenoType.PeakMilk;
                }
            }
            filling = false;
        }

        /// <summary>
        /// When changing the pure/cross bred
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGeneration(object sender, EventArgs e)
        {
            if (cbxGeneration.SelectedIndex <= 0)
            {
                lblDamBreed.Text = "Breed";
                lblSireBreed.Hide();
                cbxSireBreed.IsVisible = false;
            }
            else
            {
                lblDamBreed.Text = "Dam breed";
                lblSireBreed.Show();
                cbxSireBreed.IsVisible = true;
            }
        }

        /// <summary>
        /// Switch between sheep and cattle controls
        /// </summary>
        /// <param name="theAnimal"></param>
        private void EnablePeakMilkOrFleece(GrazType.AnimalType theAnimal)
        {
            // Visibility of animal - specific parameters 
            lblConception3.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            deConception3.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            //unitConception.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            deWoolYield.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            untWoolYield.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            lblWoolYield.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            deBreedMaxMu.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            unitBreedMaxMu.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            lblBreedMaxMu.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            if (theAnimal == GrazType.AnimalType.Sheep)
            {
                lblBreedPFWPeakMilk.Text = "Breed potential fleece weight";
                unitBreedPFWPeakMilk.Text = "kg";
            }
            else
            {
                //cattle
                lblBreedPFWPeakMilk.Text = "Peak milk production";
                unitBreedPFWPeakMilk.Text = "kg FCM";
            }
        }


        /// <summary>
        /// Store the current genotype
        /// </summary>
        private void ParseCurrGenotype()
        {
            SingleGenotypeInits theGenoType;

            if (currentGenotype >= 0 && !filling)
            {
                theGenoType = new SingleGenotypeInits();
                theGenoType.Conceptions = new double[4];
                theGenoType.GenotypeName = edtGenotypeName.Text;

                theGenoType.Generation = cbxGeneration.SelectedIndex;
                if (theGenoType.Generation > 0)
                {
                    theGenoType.DamBreed = cbxDamBreed.SelectedValue;
                    theGenoType.SireBreed = cbxSireBreed.SelectedValue;
                }
                else if (cbxDamBreed.SelectedValue != null && (cbxDamBreed.SelectedValue.ToLower() == theGenoType.GenotypeName.ToLower()))
                {
                    theGenoType.DamBreed = string.Empty;
                    theGenoType.SireBreed = string.Empty;
                }
                else
                {
                    theGenoType.DamBreed = cbxDamBreed.SelectedValue;
                    theGenoType.SireBreed = string.Empty;
                }

                theGenoType.SRW = deBreedSRW.Value;
                theGenoType.DeathRate[ADULT] = deDeath.Value * 0.01;
                theGenoType.DeathRate[WNR] = deWnrDeath.Value * 0.01;
                theGenoType.Conceptions[1] = deConception1.Value * 0.01;
                theGenoType.Conceptions[2] = deConception2.Value * 0.01;

                if (genotypeAnimals[currentGenotype] == GrazType.AnimalType.Sheep)
                {
                    theGenoType.Conceptions[3] = deConception3.Value * 0.01;
                    theGenoType.PotFleeceWt = dePFWMilk.Value;
                    theGenoType.MaxFibreDiam = deBreedMaxMu.Value;
                    theGenoType.FleeceYield = deWoolYield.Value * 0.01;
                    theGenoType.PeakMilk = 0.0;
                }
                else if (genotypeAnimals[currentGenotype] == GrazType.AnimalType.Cattle)
                {
                    theGenoType.PeakMilk = dePFWMilk.Value;
                    theGenoType.Conceptions[3] = 0.0;
                    theGenoType.PotFleeceWt = 0.0;
                    theGenoType.MaxFibreDiam = 0.0;
                    theGenoType.FleeceYield = 0.0;
                }
                this.genotypeInits[currentGenotype] = theGenoType;
            }
        }

        /// <summary>
        /// Add a new genotype to the list on the form
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event arguments</param>
        private void BtnNewGeno_Clicked(object sender, EventArgs e)
        {
            GrazType.AnimalType newAnimal;
            int newBreedIndex;
            string newBreed = "";
            bool found;
            int index;

            if (genotypeInits.Length < MAXGENOTYPES)
            {
                ParseCurrGenotype();

                // Find the first genotype that is not in the inits yet
                if (currentGenotype >= 0)
                    newAnimal = genotypeAnimals[currentGenotype];
                else
                    newAnimal = GrazType.AnimalType.Sheep;
                newBreedIndex = 0;
                found = false;
                while ((newBreedIndex < paramSet.BreedCount(newAnimal)) && !found)
                {
                    newBreed = paramSet.BreedName(newAnimal, newBreedIndex);

                    found = true;
                    for (index = 0; index < genotypeInits.Length; index++)
                    {
                        found = (found && (newBreed.ToLower() != genotypeInits[index].GenotypeName.ToLower()));
                    }
                    if (!found && (newBreedIndex == paramSet.BreedCount(newAnimal) - 1))
                    {
                        if (newAnimal == GrazType.AnimalType.Sheep)
                            newAnimal = GrazType.AnimalType.Cattle;
                        else
                            newAnimal = GrazType.AnimalType.Sheep;
                        newBreedIndex = 0;
                    }
                    else
                        newBreedIndex++;
                }

                if (!found)
                {
                    MessageDialog msgError = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, "Error adding more genotypes");
                    msgError.Title = "Error";
                    msgError.Run();
                    msgError.Destroy();
                }
                else
                {
                    Array.Resize(ref genotypeInits, genotypeInits.Length + 1);
                    genotypeInits[genotypeInits.Length - 1] = new SingleGenotypeInits();
                    genotypeInits[genotypeInits.Length - 1].Conceptions = new double[4];

                    SetGenotypeDefaults(genotypeInits.Length - 1, newBreed);
                    genoList.AppendValues(newBreed);
                    SelectedGenoIndex = genotypeInits.Length - 1;
                    ClickGenotypeList(null, null);

                    // add to the animals genotypes combo list on the animals tab
                    string[] genoNames = new string[genotypeInits.Length];
                    for (int i = 0; i < genotypeInits.Length; i++)
                    {
                        genoNames[i] = genotypeInits[i].GenotypeName;
                    }
                    cbxGroupGenotype.Values = genoNames;

                    EnableButtons();
                }
            }
        }

        /// <summary>
        /// When an item is clicked in the genotype list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickGenotypeList(object sender, EventArgs e)
        {
            if (currentGenotype >= 0 && !filling)
                ParseCurrGenotype();
            currentGenotype = SelectedGenoIndex;
            FillCurrGenotype();
        }

        /// <summary>
        /// Enable the add/del buttons
        /// </summary>
        private void EnableButtons()
        {
            btnNewGeno.Sensitive = (genotypeInits.Length < MAXGENOTYPES);
            btnDelGeno.Sensitive = (genotypeInits.Length > 0);
            btnNewAnimals.Sensitive = (animalInits.Length < MAXANIMALGROUPS) &&(genotypeInits.Length > 0);
            btnDeleteAnimals.Sensitive = (animalInits.Length > 0);
        }

        /// <summary>
        /// Respond to the changing of the breed using the combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeBreed(object sender, EventArgs e)
        {
            if (!filling)
            {
                string newGenoName = MakeUniqueGenoName(cbxDamBreed.SelectedValue);
                SetGenotypeDefaults(currentGenotype, cbxDamBreed.SelectedValue);
                FillCurrGenotype();
                edtGenotypeName.Text = newGenoName;
                //ChangeGenotypeName(sender, e);          // ensure trigger updates on the Animals tab also
                filling = true;
                SetItem(genoList, currentGenotype, newGenoName);
                filling = false;
            }
        }

        /// <summary>
        /// Make adjustments when the genotype name is edited by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGenotypeName(object sender, EventArgs e)
        {
            if (!filling)
            {
                SetItem(genoList, currentGenotype, edtGenotypeName.Text);
            }
        }
        /// <summary>
        /// Create a unique genotype name that doesn't exist in the list of names.
        /// </summary>
        /// <param name="newGenoName"></param>
        /// <returns></returns>
        private string MakeUniqueGenoName(string newGenoName)
        {
            string result = newGenoName;
            int i = 2;

            while ((IndexOf(genoList, result) >= 0) || (IndexOf(genoList, result.ToLower()) >= 0))
            {
                result = newGenoName + " #" + i.ToString();
                i++;
            }
            return result;
        }

        /// <summary>
        /// Get the index of the search string in the ListStore
        /// </summary>
        /// <param name="store"></param>
        /// <param name="search"></param>
        /// <returns>-1 if not found</returns>
        private int IndexOf(ListStore store, string search)
        {
            int result = -1;
            int nNames = store.IterNChildren();
            TreeIter iter;
            int i = 0;
            if (store.GetIterFirst(out iter))
            {
                do
                {
                    if (string.Compare(search, (string)store.GetValue(iter, 0), true) == 0)
                        result = i;
                    i++;
                }
                while (store.IterNext(ref iter) && result == -1);
            }

            return result;
        }

        /// <summary>
        /// Get the string of the item at an index
        /// </summary>
        /// <param name="store">The list store</param>
        /// <param name="index">The index of the item</param>
        /// <returns>The string value at the index</returns>
        private string GetItem(ListStore store, int index)
        {
            string result = string.Empty;
            TreeIter iter;
            int i = 0;
            bool more = store.GetIterFirst(out iter);
            while (more)
            {
                if (i == index)
                {
                    result = (string)store.GetValue(iter, 0);
                    more = false;
                }
                else
                    more = store.IterNext(ref iter);
                i++;
            }

            return result;
        }

        /// <summary>
        /// Sets the item in the list with a new string value
        /// </summary>
        /// <param name="store">The list store</param>
        /// <param name="index">Set this item</param>
        /// <param name="value">The new string</param>
        private void SetItem(ListStore store, int index, string value)
        {
            TreeIter iter;
            int i = 0;
            bool more = store.GetIterFirst(out iter);
            while (more)
            {
                if (i == index)
                {
                    store.SetValue(iter, 0, value);
                    more = false;
                }
                more = store.IterNext(ref iter);
                i++;
            }
        }

        /// <summary>
        /// Round the floating point value
        /// </summary>
        /// <param name="x"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private double RoundValue(double x, double scale)
        {
            return scale * Math.Round(x / scale);
        }

        /// <summary>
        /// Store the settings for the breed name in the list of genotypes that have been defined.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="breedName"></param>
        private void SetGenotypeDefaults(int index, string breedName)
        {
            AnimalParamSet breedParams;
            SingleGenotypeInits theGenoType;

            breedParams = paramSet.Match(breedName);
            if (breedParams != null)
            {
                theGenoType = genotypeInits[index];

                genotypeAnimals[index] = breedParams.Animal;

                theGenoType.GenotypeName = breedParams.Name;
                theGenoType.DamBreed = string.Empty;
                theGenoType.SireBreed = string.Empty;
                theGenoType.Generation = 0;
                theGenoType.SRW = breedParams.BreedSRW;
                theGenoType.DeathRate[ADULT] = RoundValue(breedParams.AnnualDeaths(false), 0.001);
                theGenoType.DeathRate[WNR] = RoundValue(breedParams.AnnualDeaths(true), 0.001);
                theGenoType.Conceptions[1] = RoundValue(breedParams.Conceptions[1], 0.01);
                theGenoType.Conceptions[2] = RoundValue(breedParams.Conceptions[2], 0.01);

                if (breedParams.Animal == GrazType.AnimalType.Sheep)
                {
                    theGenoType.Conceptions[3] = RoundValue(breedParams.Conceptions[3], 0.01);
                    theGenoType.PotFleeceWt = breedParams.PotentialGFW;
                    theGenoType.MaxFibreDiam = breedParams.MaxMicrons;
                    theGenoType.FleeceYield = breedParams.FleeceYield;
                    theGenoType.PeakMilk = 0.0;
                }
                else if (breedParams.Animal == GrazType.AnimalType.Cattle)
                {
                    theGenoType.PeakMilk = breedParams.PotMilkYield;
                    theGenoType.Conceptions[3] = 0.0;
                    theGenoType.PotFleeceWt = 0.0;
                    theGenoType.MaxFibreDiam = 0.0;
                    theGenoType.FleeceYield = 0.0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected index for the treeview
        /// </summary>
        private int SelectedGenoIndex
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                lbxGenotypeList.GetCursor(out selPath, out selCol);
                return selPath != null ? selPath.Indices[0] : 0;
            }

            set
            {
                if (value >= 0)
                {
                    int[] indices = new int[1] { value };
                    TreePath selPath = new TreePath(indices);
                    lbxGenotypeList.SetCursor(selPath, null, false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected index for the animal group treeview
        /// </summary>
        private int SelectedGroupIndex
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                lbxAnimalList.GetCursor(out selPath, out selCol);
                return selPath != null ? selPath.Indices[0] : 0;
            }

            set
            {
                if (value >= 0)
                {
                    int[] indices = new int[1] { value };
                    TreePath selPath = new TreePath(indices);
                    lbxAnimalList.SetCursor(selPath, null, false);
                }
            }
        }

        /// <summary>
        /// Sets the value of TheAnimal, then initialises the breed list boxes and 
        /// sets up visibility of some animal-specific genotypic parameters. 
        /// Changes naming schemes on the Animals tab to suit sheep or cattle. 
        /// Lastly, clears the list of animal groups. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickAnimal(object sender, EventArgs e)
        {
            if (((Gtk.RadioButton)sender).Active)
            {
                GrazType.AnimalType currAnimal;

                if (rbtnSheep.Active)
                    currAnimal = GrazType.AnimalType.Sheep;
                else
                    currAnimal = GrazType.AnimalType.Cattle;

                genotypeAnimals[currentGenotype] = currAnimal;

                List<string> names = new List<string>();

                int count = this.paramSet.BreedCount(currAnimal);
                string[] namesArray = new string[count];
                for (int i = 0; i < count; i++)
                {
                    namesArray[i] = paramSet.BreedName(currAnimal, i);
                }

                cbxDamBreed.Changed -= ChangeBreed;

                cbxDamBreed.Values = namesArray;
                cbxDamBreed.SelectedIndex = 0;
                cbxSireBreed.Values = namesArray;
                cbxSireBreed.SelectedIndex = 0;

                cbxDamBreed.Changed += ChangeBreed;

                ChangeBreed(null, null);            //Force default SRW values etc
            }
        }

        /// <summary>
        /// Delete the currently selected genotype from genotypeInits[]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelGeno_Clicked(object sender, EventArgs e)
        {
            if (currentGenotype >= 0)
            {
                //TODO when animals tab is working: deleteGroupsWithGenotype(FCurrGenotype);

                for (int idx = currentGenotype + 1; idx <= genotypeInits.Length - 1; idx++)
                    genotypeInits[idx - 1] = genotypeInits[idx];
                Array.Resize(ref genotypeInits, genotypeInits.Length - 1);

                int current = SelectedGenoIndex;
                // repopulate the view
                SetValues();
                SelectedGenoIndex = Math.Min(current, genotypeInits.Length - 1);
                EnableButtons();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_SelectedIndexChanged(object sender, SwitchPageArgs e)
        {
            switch (e.PageNum)
            {
                case 0:

                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbxGenotypeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClickGenotypeList(sender, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbxAnimalList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClickAnimalList(sender);
        }

        /// <summary>
        /// Stores the index of the selected group in CurrGroup and displays its values
        /// </summary>
        private void ClickAnimalList(object sender)
        {
            bool valuesOK;

            // This procedure is called in several places: ClickAnimalList(nil).
            // We only want to check the weights, if user clicked AnimalList, so we have to check the Sender:
            valuesOK = ((sender != lbxAnimalList) || CheckCurrGroup(lbxAnimalList, true));
            if (!valuesOK)
                SelectedGroupIndex = currentGroup;
            else
            {
                if (lbxAnimalList.Data.Count > 0)
                    ParseCurrGroup();
                currentGroup = SelectedGroupIndex;

                FillCurrentGroup();
            }
        }

        /// <summary>
        /// Populates the animal list box for a given animal group, including setting 
        /// the visibility of controls which only apply in particular circumstances.  
        /// This method is called whenever the user changes the selected animal group
        /// and also when either the "group text" or the layout of the animal group   
        /// box is supposed to change.
        /// </summary>
        private void FillCurrentGroup()
        {
            bool bIsMature;
            bool bIsPregnant;
            bool bIsLactating;
            bool bHasYoung;
            int iGenotype;
            AnimalInits animalGroup;
            GrazType.AnimalType animalType;
            int Idx;

            if (currentGroup < 0)
                gbxAnimals.Hide();
            else
                gbxAnimals.Show();

            if (currentGroup >= 0)
            {
                this.filling = true;    // disable event handlers
                animalGroup = this.animalInits[currentGroup];
                // get the animaltype for this group
                string geno = animalGroup.Genotype;
                iGenotype = cbxGroupGenotype.IndexOf(geno);
                if (iGenotype >= 0)
                    animalType = genotypeAnimals[iGenotype];
                else
                    animalType = GrazType.AnimalType.Sheep;

                cbxGroupGenotype.SelectedIndex = iGenotype;

                string[] sexNames = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    sexNames[i] = SEXMAP[(int)animalType, i].Text;
                }
                cbxSex.Values = sexNames;
                cbxSex.SelectedIndex = REPRO2MAP[(int)animalGroup.Sex];
                deNumber.Value = animalGroup.Number;
                deAge.Value = animalGroup.AgeDays * DAY2MONTH;
                deWeight.Value = animalGroup.Weight;
                edtPaddock.Text = animalGroup.Paddock;
                deTag.Value = animalGroup.Tag;
                dePriority.Value = animalGroup.Priority;

                // Highest previous weight is only required for immature animals
                bIsMature = ((animalType == GrazType.AnimalType.Sheep) && (animalGroup.AgeDays >= 24.0 / DAY2MONTH)) || ((animalType == GrazType.AnimalType.Cattle) && (animalGroup.AgeDays >= 36.0 / DAY2MONTH));
                lblPrevWt.Visible = !bIsMature;
                dePrevWt.Visible = !bIsMature;
                untPrevWt.Visible = !bIsMature;
                dePrevWt.Value = Math.Max(animalGroup.MaxPrevWt, animalGroup.Weight);

                // Fleece weight and diameter are only required for sheep        
                lblFleece.Visible = (animalType == GrazType.AnimalType.Sheep);
                deFleece.Visible = (animalType == GrazType.AnimalType.Sheep);
                untFleece.Visible = (animalType == GrazType.AnimalType.Sheep);
                deFleece.Value = animalGroup.FleeceWt;

                lblFibreDiam.Visible = (animalType == GrazType.AnimalType.Sheep);
                deFibreDiam.Visible = (animalType == GrazType.AnimalType.Sheep);
                untFibreDiam.Visible = (animalType == GrazType.AnimalType.Sheep);
                deFibreDiam.Value = animalGroup.FibreDiam;


                pnlRepro.Visible = (animalGroup.Sex == GrazType.ReproType.Empty); // Reproduction-related controls are on a frame

                if (pnlRepro.Visible)
                {
                    bIsPregnant = (animalGroup.Pregnant > 0);
                    bIsLactating = (animalGroup.Lactating > 0);
                    bHasYoung = (animalGroup.NumSuckling > 0);

                    rgrpSRepro.Visible = (animalType == GrazType.AnimalType.Sheep);                          // Reproduction options for sheep           
                    if (animalType == GrazType.AnimalType.Sheep)
                    {
                        if (!(bIsPregnant || bIsLactating))
                            rbDryEmpty.Active = true;
                        else if (bIsPregnant)
                            rbPregS.Active = true;
                        else if (bIsLactating)
                            rbLact.Active = true;
                    }

                    rgrpCPreg.Visible = (animalType == GrazType.AnimalType.Cattle);                          // Pregnancy options for cattle             
                    if (animalType == GrazType.AnimalType.Cattle)
                    {
                        if (!bIsPregnant)
                            rbEmpty.Active = true;
                        else
                            rbPreg.Active = true;
                    }

                    rgrpCLact.Visible = (animalType == GrazType.AnimalType.Cattle);                          // Lactation options for cattle            
                    if (animalType == GrazType.AnimalType.Cattle)
                        if (!bIsLactating)
                            rbNoLact.Active = true;
                        else if (bIsLactating && bHasYoung)
                            rbLac.Active = true;
                        else if (bIsLactating && !bHasYoung)
                            rbLactCalf.Active = true;
                    
                    rgrpNoLambs.Visible  = (animalType == GrazType.AnimalType.Sheep) && (bIsPregnant || bIsLactating);
                    /*if bIsPregnant then
                      rgrpNoLambs.ItemIndex := No_Foetuses - 1
                    else if bisLactating then
                      rgrpNoLambs.ItemIndex := No_Suckling - 1;

                    lblPregnant.Visible  := bIsPregnant;
                    edtPregnant.Visible  := bIsPregnant;
                    untPregnant.Visible  := bIsPregnant;
                    edtPregnant.Value    := Pregnant;

                    lblLactating.Visible := bIsLactating;
                    edtLactating.Visible := bIsLactating;
                    untLactating.Visible := bIsLactating;
                    edtLactating.Value   := Lactating;

                    lblBirthCS.Visible   := bIsLactating;
                    edtBirthCS.Visible   := bIsLactating;
                    edtBirthCS.Value     := Birth_CS;

                    if (theAnimal = Sheep) then
                    lblYoungWt.Caption       := '&Lamb weight'
                    else if (theAnimal = Cattle) then
                    lblYoungWt.Caption       := 'Cal&f weight';
                    lblYoungWt.Visible   := bIsLactating and bHasYoung;
                    edtYoungWt.Visible   := bIsLactating and bHasYoung;
                    untYoungWt.Visible   := bIsLactating and bHasYoung;
                    edtYoungWt.Value     := Young_Wt;

                    lblLambGFW.Visible   := (TheAnimal = Sheep) and bIsLactating;
                    edtLambGFW.Visible   := (TheAnimal = Sheep) and bIsLactating;
                    untLambGFW.Visible   := (TheAnimal = Sheep) and bIsLactating;
                    edtLambGFW.Value     := Young_GFW; */
                }



                // Update the descriptive string in the animal groups list box
                string groupText = GroupText(currentGroup);
                TreeIter iter;
                groupsList.IterNthChild(out iter, currentGroup);
                groupsList.SetValue(iter, 0, groupText);

                this.filling = false;
            }
        }

        /// <summary>
        /// Read the animal group details from the form
        /// </summary>
        private void ParseCurrGroup()
        {
            int iGenotype;
            bool bIsPregnant = false;
            bool bIsLactating = false;
            bool bHasYoung = false;

            if (currentGroup >= 0)
            {
                AnimalInits animalGroup = this.animalInits[currentGroup];
                GrazType.AnimalType animalType = this.GetAnimalTypeForGroup(currentGroup);

                if (!filling)
                {
                    animalGroup.Genotype = cbxGroupGenotype.SelectedValue;
                    animalGroup.Number = Convert.ToInt32(Math.Round(deNumber.Value));
                    animalGroup.AgeDays = Convert.ToInt32(deAge.Value / DAY2MONTH);
                    animalGroup.Sex = SEXMAP[(int)animalType, cbxSex.SelectedIndex].Repro;
                    animalGroup.Weight = deWeight.Value;
                    animalGroup.Paddock = edtPaddock.Text;
                    animalGroup.Tag = Convert.ToInt32(deTag.Value);
                    animalGroup.Priority = Convert.ToInt32(dePriority.Value);

                    if (dePrevWt.Visible)
                        animalGroup.MaxPrevWt = dePrevWt.Value;
                    else
                        animalGroup.MaxPrevWt = animalGroup.Weight;
                    if (animalType == GrazType.AnimalType.Sheep)
                    {
                        animalGroup.FleeceWt = deFleece.Value;
                        animalGroup.FibreDiam = deFibreDiam.Value;
                    }
                    else
                    {
                        animalGroup.FleeceWt = 0;
                        animalGroup.FibreDiam = 0;
                    }

                    // Default values for reproductive inputs
                    animalGroup.BirthCS = 0;
                    animalGroup.Lactating = 0;
                    animalGroup.MatedTo = "";
                    animalGroup.Pregnant = 0;

                    
                    if (animalGroup.Sex == GrazType.ReproType.Empty)
                    { /*
                    if (animalType == GrazType.AnimalType.Sheep)
                    {
                        bIsPregnant = (rgrpSRepro.ItemIndex = 1);
                        bIsLactating = (rgrpSRepro.ItemIndex = 2);
                        bHasYoung = bIsLactating;
                        if (bIsPregnant)
                            animalGroup.No_Foetuses = 1 + rgrpNoLambs.ItemIndex;
                        if (bHasYoung)
                            animalGroup.No_Suckling = 1 + rgrpNoLambs.ItemIndex;
                    }
                    else if (animalType == GrazType.AnimalType.Cattle)
                    {
                        bIsPregnant = (rgrpCPreg.ItemIndex = 1);
                        bIsLactating = (rgrpCLact.ItemIndex in [1,2]);
                        bHasYoung = (rgrpCLact.ItemIndex in [0,1]);
                        if (bisPregnant)
                            animalGroup.No_Foetuses = 1;
                        if (bHasYoung)
                            animalGroup.No_Suckling = 1;
                    }

                    if (bIsPregnant)
                        animalGroup.Pregnant = Round(edtPregnant.Value);
                    if (bIsLactating) 
                    {
                        animalGroup.Lactating = Round(edtLactating.Value );
                        animalGroup.BirthCS  = edtBirthCS.Value;
                    }
                    if (bIsLactating and bHasYoung)
                        animalGroup.Young_Wt = edtYoungWt.Value;
                    if (bIsLactating and bHasYoung && (animalType == GrazType.AnimalType.Sheep))
                        animalGroup.Young_GFW = edtLambGFW.Value; */
                    } // reproduction values 

                    this.animalInits[currentGroup] = animalGroup;
                }
            }
        }

        /// <summary>
        /// Add new animal group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnNewAnimals_Clicked(object sender, EventArgs e)
        {
            if (CheckCurrGroup(null, true) && this.animalInits.Length < MAXANIMALGROUPS)
            {
                Array.Resize(ref this.animalInits, this.animalInits.Length + 1);
                this.animalInits[this.animalInits.Length - 1] = new AnimalInits();

                this.animalInits[this.animalInits.Length - 1].Genotype = this.genotypeInits[0].GenotypeName;
                this.animalInits[this.animalInits.Length - 1].FibreDiam = 20;

                string groupText = GroupText(animalInits.Length - 1);
                groupsList.AppendValues(groupText);
                SelectedGroupIndex = animalInits.Length - 1;
                ClickAnimalList(null);

                EnableButtons();
            }
        }

        private void BtnDeleteAnimals_Clicked(object sender, EventArgs e)
        {

            EnableButtons();
        }

        public static string[,] MALENAMES = { { "wether", "ram" }, { "steer", "bull" } };

        /// <summary>
        /// Create an identifying string for the animal group
        /// </summary>
        /// <param name="idx">The index in the animalInits[]</param>
        /// <returns>The compound description</returns>
        private string GroupText(int idx)
        {
            AnimalInits theAnimalGroup = animalInits[idx];
            GrazType.AnimalType animalType = GetAnimalTypeForGroup(idx);

            string result = "";
            if ((theAnimalGroup.Sex == GrazType.ReproType.Male) || (theAnimalGroup.Sex == GrazType.ReproType.Castrated))
                result = MALENAMES[(int)animalType, (int)theAnimalGroup.Sex];
            else if (animalType == GrazType.AnimalType.Sheep)
                result = "ewe";
            else if (theAnimalGroup.AgeDays < 2 * 365)
                result = "heifer";
            else
                result = "cow";

            result = string.Format("{0} {1} {2}s {3,2:f1} mo. {4, 3:f1} kg", theAnimalGroup.Number, theAnimalGroup.Genotype, result, theAnimalGroup.AgeDays * DAY2MONTH, theAnimalGroup.Weight);

            return result;
        }

        
        /// <summary>
        /// Change the genotype of the selected animal group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGroupGenotype(object sender, EventArgs e)
        {
            if (!this.filling)
                animalInits[currentGroup].Genotype = cbxGroupGenotype.SelectedValue;
            FillCurrentGroup();
        }

        /// <summary>
        /// Changing the number of stock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeNumber(object sender, EventArgs e)
        {
            if (!this.filling)
                animalInits[currentGroup].Number = Convert.ToInt32(deNumber.Value);
            FillCurrentGroup();
        }

        /// <summary>
        /// change the sex name for this animal group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeSex(object sender, EventArgs e)
        {
            if (!filling)
            {
                GrazType.AnimalType animalType = GetAnimalTypeForGroup(currentGroup);
                animalInits[currentGroup].Sex = SEXMAP[(int)animalType, cbxSex.SelectedIndex].Repro;
                this.FillCurrentGroup();
                this.CheckCurrGroup(cbxSex, false);
            }
        }

        private void ChangeEditCtrl(object sender, EventArgs e)
        {
            if (!filling)
                CheckCurrGroup(sender, false);
            FillCurrentGroup();
        }

        /// <summary>
        /// Get the animal type (sheep, cattle) for the specified animal group in the list
        /// </summary>
        /// <param name="groupIdx"></param>
        /// <returns></returns>
        private GrazType.AnimalType GetAnimalTypeForGroup(int groupIdx)
        {
            int genotypeIdx = IndexOf(genoList, animalInits[groupIdx].Genotype);
            GrazType.AnimalType animalType = this.genotypeAnimals[genotypeIdx];

            return animalType;
        }

        /// <summary>
        /// The following error conditions are identified:                               
        /// 1. Fleece-free weight outside the valid range.                               
        /// 2. Highest previous weight invalid                                           
        /// 3. Lamb/calf weight outside the valid range.                                 
        /// 4. Fleece weight in sheep outside the valid range.                           
        /// 5. Fibre diameter outside the valid range                                    
        /// 6. Lamb fleece weight outside the valid range.                               
        /// </summary>
        /// <param name="showErrorMsg"></param>
        /// <returns></returns>
        private bool CheckCurrGroup(object sender, bool showErrorMsg)
        {
            int[] WtDecPlaces = { 2, 1 };
            GrazType.AnimalType theAnimal = GrazType.AnimalType.Sheep;
            double dLowFFLW = 0,
            dHighFFLW = 2000,
            dHighGFW;
            string sMessage;
            int iError = 0;
            GrazType.ReproType Repro;

            if (currentGroup >= 0)
            {
                ParseCurrGenotype();
                ParseCurrGroup();

                int genotypeIdx = IndexOf(genoList, animalInits[currentGroup].Genotype);
                if (genotypeIdx < 0) //no genotype specified
                    iError = 7;
                else
                    theAnimal = genotypeAnimals[genotypeIdx];

                AnimalInits theAnimalGroup = animalInits[currentGroup];
                SingleGenotypeInits theGenoType = genotypeInits[genotypeIdx];

                if (iError == 0)
                {
                    if (theAnimalGroup.Pregnant > 0)
                        Repro = GrazType.ReproType.EarlyPreg;
                    else
                        Repro = theAnimalGroup.Sex;

                    // calc the normal weight range 
                    if (OnCalcNormalWeight != null)
                        OnCalcNormalWeight.Invoke(this.paramSet, genotypeInits, genotypeIdx, Repro, Convert.ToInt32(theAnimalGroup.AgeDays), 0.70, 1.30, out dLowFFLW, out dHighFFLW);

                    if ((theAnimalGroup.Weight - theAnimalGroup.FleeceWt < dLowFFLW) || (theAnimalGroup.Weight - theAnimalGroup.FleeceWt > dHighFFLW))
                        iError = 1;
                }
                if ((iError == 0) && dePrevWt.Visible)
                {
                    if ((theAnimalGroup.MaxPrevWt < theAnimalGroup.Weight) || (theAnimalGroup.MaxPrevWt - theAnimalGroup.FleeceWt > dHighFFLW))
                        iError = 2;
                }
                if ((iError == 0) && (theAnimalGroup.Lactating > 0) /*&& (theAnimalGroup.NoSuckling > 0)*/)
                {
                    // Lactating holds the age of the suckling young
                    if (OnCalcNormalWeight != null)
                        OnCalcNormalWeight.Invoke(this.paramSet, genotypeInits, genotypeIdx, GrazType.ReproType.Castrated, theAnimalGroup.Lactating, 0.70, 1.30, out dLowFFLW, out dHighFFLW);

                    if ((theAnimalGroup.YoungWt - theAnimalGroup.YoungGFW < dLowFFLW) || (theAnimalGroup.YoungWt - theAnimalGroup.YoungGFW > dHighFFLW))
                        iError = 3;
                }
                if ((iError == 0) && (theAnimal == GrazType.AnimalType.Sheep))
                {
                    switch (theAnimalGroup.Sex)
                    {
                        case GrazType.ReproType.Male:
                            dHighGFW = 1.5 * 1.4 * theGenoType.PotFleeceWt;
                            break;
                        case GrazType.ReproType.Castrated:
                            dHighGFW = 1.5 * 1.2 * theGenoType.PotFleeceWt;
                            break;
                        default:
                            dHighGFW = 1.5 * 1.0 * theGenoType.PotFleeceWt;
                            break;
                    }
                    if ((theAnimalGroup.FleeceWt < 0.0) || (theAnimalGroup.FleeceWt > dHighGFW))
                        iError = 4;
                    else if ((theAnimalGroup.FibreDiam < 0.5 * theGenoType.MaxFibreDiam) || (theAnimalGroup.FibreDiam > 1.5 * theGenoType.MaxFibreDiam))
                        iError = 5;
                    else if ((theAnimalGroup.Lactating > 0) && (theAnimalGroup.YoungGFW < 0.0) || (theAnimalGroup.YoungGFW > dHighGFW))
                        iError = 6;
                }
                else
                    dHighGFW = 0.0;


                string errorMsg = string.Empty;
                lblError.Text = string.Empty;
                string decplaces = WtDecPlaces[(int)theAnimal].ToString();
                if (iError > 0)
                {
                    switch (iError)
                    {
                        case 1:
                            {
                                sMessage = string.Format("{0,0:f" + decplaces + "} to {1,0:f" + decplaces + "}", dLowFFLW + theAnimalGroup.FleeceWt, dHighFFLW + theAnimalGroup.FleeceWt);
                                if (showErrorMsg)
                                    sMessage = "Live weight should be in the range " + sMessage;
                            }
                            break;
                        case 2:
                            {
                                sMessage = string.Format("{0,0:f" + decplaces + "} to {1,0:f" + decplaces + "}", theAnimalGroup.Weight, dHighFFLW + theAnimalGroup.FleeceWt);
                                if (showErrorMsg)
                                    sMessage = "Highest previous weight should be in the range " + sMessage;
                            }
                            break;
                        case 3:
                            {
                                sMessage = string.Format("{0,0:f" + decplaces + "} to {1,0:f" + decplaces + "}", dLowFFLW + theAnimalGroup.YoungGFW, dHighFFLW + theAnimalGroup.YoungGFW);
                                if (showErrorMsg)
                                    if (theAnimal == GrazType.AnimalType.Sheep)
                                        sMessage = "Lamb weight should be in the range " + sMessage;
                                    else
                                        sMessage = "Calf weight should be in the range " + sMessage;
                            }
                            break;
                        case 4:
                            {
                                sMessage = string.Format("0.00 to {0,2:f1", dHighGFW);
                                if (showErrorMsg)
                                    sMessage = "Fleece weight should be in the range " + sMessage;
                            }
                            break;
                        case 5:/*
             ErrorCtrl := edtFibreDiam;
             sMessage  := Format('%1.f to %.1f', [ 0.5*FValues.Genotypes[iGenotype].MaxFibreDiam,
                                                   1.5*FValues.Genotypes[iGenotype].MaxFibreDiam ] );
             if bShowErrorMsg then
               sMessage := 'Wool fibre diameter should be in the range'#13#10 + sMessage;
                    */
                            errorMsg = "Error in weights";
                            break;
                        case 6:
                        /*
                         
                 ErrorCtrl := edtLambGFW;
                 sMessage  := Format('0.00 to %.2f', [ dHighGFW ] );
                 if bShowErrorMsg then
                   sMessage := 'Fleece weight should be in the range'#13#10 + sMessage;
               
                     */
                        case 7:
                            {
                                if (showErrorMsg)
                                    sMessage = "One of the animal groups (" + (currentGroup + 1).ToString() + ") does not have a genotype!";
                            }
                            break;
                    }

                    if (showErrorMsg)
                    {
                        lblError.Text = errorMsg;
                    }
                }
            }

            return (iError == 0);
        }

      
    }
}

