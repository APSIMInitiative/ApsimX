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

        private int currentGenotype;            // selected genotype
        private int currentGroup = -1;          // selected animal group
        private uint currentTab = 0;
        private GrazType.AnimalType[] genotypeAnimals = new GrazType.AnimalType[20];    // animal types for each genotype in the list

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

        /// <summary>
        /// The array of initial animal groups that get assigned to paddocks 
        /// </summary>
        private AnimalInits[] animalInits = new AnimalInits[0];

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
        private RadioButton rbOneLamb = null;
        private RadioButton rbTwoLambs = null;
        private RadioButton rbThreeLambs = null;
        private Label lblPregnant = null;
        private DoubleEditView dePregnant = null;
        private Label untPregnant = null;
        private Label lblLactating = null;
        private DoubleEditView deLactating = null;
        private Label untLactating = null;
        private DoubleEditView deBirthCS = null;
        private DoubleEditView deYoungWt = null;
        private DoubleEditView deLambGFW = null;
        private Label lblBirthCS = null;
        private Label lblYoungWt = null;
        private Label untYoungWt = null;
        private Label lblLambGFW = null;
        private Label untLambGFW = null;

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
            this.notebook1 = (Notebook)builder.GetObject("notebook1");
            this.notebook1.SwitchPage += TabControl1_SelectedIndexChanged;

            this.gbxGenotype = (Frame)builder.GetObject("gbxGenotype");
            this.edtGenotypeName = (Entry)builder.GetObject("edtGenotypeName");
            this.btnNewGeno = (Button)builder.GetObject("btnNewGeno");
            this.btnDelGeno = (Button)builder.GetObject("btnDelGeno");
            this.rbtnSheep = (Gtk.RadioButton)builder.GetObject("rbtnSheep");
            this.rbtnCattle = (Gtk.RadioButton)builder.GetObject("rbtnCattle");
            this.lblConception3 = (Label)builder.GetObject("lblConception3");
            this.unitConception = (Label)builder.GetObject("unitConception");
            this.lblBreedPFWPeakMilk = (Label)builder.GetObject("lblBreedPFW_PeakMilk");
            this.unitBreedPFWPeakMilk = (Label)builder.GetObject("unitBreedPFW_PeakMilk");
            this.untWoolYield = (Label)builder.GetObject("untWoolYield");
            this.lblWoolYield = (Label)builder.GetObject("lblWoolYield");
            this.lblDamBreed = (Label)builder.GetObject("lblDamBreed");
            this.lblSireBreed = (Label)builder.GetObject("lblSireBreed");
            this.lblBreedMaxMu = (Label)builder.GetObject("lblBreedMaxMu");
            this.unitBreedMaxMu = (Label)builder.GetObject("unitBreedMaxMu");

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
            rbOneLamb = (Gtk.RadioButton)builder.GetObject("rbOneLamb");
            rbTwoLambs = (Gtk.RadioButton)builder.GetObject("rbTwoLambs");
            rbThreeLambs = (Gtk.RadioButton)builder.GetObject("rbThreeLambs");
            lblPregnant = (Label)builder.GetObject("lblPregnant");
            dePregnant = new DoubleEditView(this, (Entry)builder.GetObject("edtPregnant"), 300, 0, 0);
            untPregnant = (Label)builder.GetObject("untPregnant");
            lblLactating = (Label)builder.GetObject("lblLactating");
            deLactating = new DoubleEditView(this, (Entry)builder.GetObject("edtLactating"), 365, 0, 0);
            untLactating = (Label)builder.GetObject("untLactating");
            deBirthCS = new DoubleEditView(this, (Entry)builder.GetObject("edtBirthCS"), 5, 1, 1);
            deYoungWt = new DoubleEditView(this, (Entry)builder.GetObject("edtYoungWt"), 1000, 0, 1);
            deLambGFW = new DoubleEditView(this, (Entry)builder.GetObject("edtLambGFW"), 100, 0, 2);
            lblBirthCS = (Label)builder.GetObject("lblBirthCS");
            lblYoungWt = (Label)builder.GetObject("lblYoungWt");
            untYoungWt = (Label)builder.GetObject("untYoungWt");
            lblLambGFW = (Label)builder.GetObject("lblLambGFW");
            untLambGFW = (Label)builder.GetObject("untLambGFW");

            // configure the treeview of animal groups
            this.lbxAnimalList = (Gtk.TreeView)builder.GetObject("tvAnimals");
            this.lbxAnimalList.Model = groupsList;
            CellRendererText textRenderA = new Gtk.CellRendererText();
            TreeViewColumn columnA = new TreeViewColumn("Animal Groups", textRenderA, "text", 0);
            this.lbxAnimalList.AppendColumn(columnA);
            this.lbxAnimalList.HeadersVisible = false;
            this.lbxAnimalList.CursorChanged += LbxAnimalList_SelectedIndexChanged;

            this.btnNewAnimals.Clicked += BtnNewAnimals_Clicked;
            this.btnDeleteAnimals.Clicked += BtnDeleteAnimals_Clicked;

            // configure the treeview of genotype names
            this.lbxGenotypeList = (Gtk.TreeView)builder.GetObject("tvGenotypes");
            this.lbxGenotypeList.Model = genoList;
            CellRendererText textRender = new Gtk.CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Genotype Names", textRender, "text", 0);
            this.lbxGenotypeList.AppendColumn(column);
            this.lbxGenotypeList.HeadersVisible = false;
            this.lbxGenotypeList.CursorChanged += LbxGenotypeList_SelectedIndexChanged;

            // Genotypes tab events
            this.btnNewGeno.Clicked += BtnNewGeno_Clicked;
            this.btnDelGeno.Clicked += BtnDelGeno_Clicked;
            this.edtGenotypeName.Changed += ChangeGenotypeName;
            this.rbtnSheep.Clicked += ClickAnimal;
            this.rbtnCattle.Clicked += ClickAnimal;
            this.cbxDamBreed.Changed += ChangeBreed;
            this.cbxGeneration.Changed += ChangeGeneration;
            // Animals tab events
            deNumber.OnChange += ChangeNumber;
            this.cbxSex.Changed += this.ChangeSex;
            this.cbxGroupGenotype.Changed += ChangeGroupGenotype;
            this.deWeight.OnChange += this.ChangeEditCtrl;
            this.dePrevWt.OnChange += this.ChangeEditCtrl;
            this.deAge.OnChange += this.ChangeEditCtrl;
            this.rbDryEmpty.Clicked += ClickSheepRepro;
            this.rbPregS.Clicked += ClickSheepRepro;
            this.rbLact.Clicked += ClickSheepRepro;
            this.rbEmpty.Clicked += ClickCattlePreg;
            this.rbPreg.Clicked += ClickCattlePreg;
            this.rbNoLact.Clicked += ClickCattleLact;
            this.rbLac.Clicked += ClickCattleLact;
            this.rbLactCalf.Clicked += ClickCattleLact;

            mainWidget = notebook1;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                // detach events
                this.btnNewGeno.Clicked -= BtnNewGeno_Clicked;
                this.btnDelGeno.Clicked -= BtnDelGeno_Clicked;
                this.edtGenotypeName.Changed -= ChangeGenotypeName;
                this.rbtnSheep.Clicked -= ClickAnimal;
                this.rbtnCattle.Clicked -= ClickAnimal;
                this.cbxDamBreed.Changed -= ChangeBreed;
                this.cbxGeneration.Changed -= ChangeGeneration;
                this.notebook1.SwitchPage -= TabControl1_SelectedIndexChanged;

                this.btnNewAnimals.Clicked -= BtnNewAnimals_Clicked;
                this.cbxGroupGenotype.Changed -= ChangeGroupGenotype;
                this.deNumber.OnChange -= ChangeNumber;
                this.deWeight.OnChange -= this.ChangeEditCtrl;
                this.dePrevWt.OnChange -= this.ChangeEditCtrl;
                this.deAge.OnChange -= this.ChangeEditCtrl;
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// Changing tab pages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_SelectedIndexChanged(object sender, SwitchPageArgs e)
        {
            try
            {
                switch (e.PageNum)
                {
                    case 0:
                        if (this.currentTab == 1)
                            this.ParseCurrGroup();
                        break;
                    case 1:
                        if (animalInits.Length < 1)
                            this.gbxAnimals.Child.HideAll();     // hide data entry on the animals tab
                        else
                        {
                            this.currentGroup = Math.Min(0, animalInits.Length - 1);    // could be -1
                            if (currentGroup >= 0)
                            {
                                this.filling = true;
                                this.SelectedGroupIndex = currentGroup;
                                //this.ClickAnimalList(null);
                                this.filling = false;
                            }
                        }
                        break;
                }
                this.currentTab = e.PageNum;
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// The list of animal groups
        /// </summary>
        public AnimalInits[] Animals
        {
            get
            {
                if (this.currentGroup >= 0)
                    this.ParseCurrGroup();
                return this.animalInits;
            }
            set
            {
                this.animalInits = new AnimalInits[value.Length];
                value.CopyTo(this.animalInits, 0);
            }
        }

        /// <summary>
        /// The list of genotypes in the component's inits
        /// </summary>
        public SingleGenotypeInits[] Genotypes
        {
            get
            {
                if (this.currentGenotype >= 0)
                    this.ParseCurrGenotype();
                return this.genotypeInits;
            }
            set
            {
                this.genotypeInits = new SingleGenotypeInits[value.Length];
                value.CopyTo(this.genotypeInits, 0);
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

            this.genoList.Clear();
            string[] genoNames = new string[this.genotypeInits.Length];
            for (int i = 0; i < this.genotypeInits.Length; i++)
            {
                genoNames[i] = this.genotypeInits[i].GenotypeName;
                this.genoList.AppendValues(this.genotypeInits[i].GenotypeName);
            }
            this.cbxGroupGenotype.Values = genoNames;        // animals tab

            GenotypeInitArgs args = new GenotypeInitArgs();
            args.Genotypes = this.genotypeInits;
            args.ParamSet = this.paramSet;
            for (int idx = 0; idx < this.genotypeInits.Length; idx++)
            {
                args.Index = idx;
                this.GetGenoParams.Invoke(null, args);   // gets params from the stock model

                if (this.genotypeSet != null)
                {
                    this.genotypeAnimals[idx] = this.genotypeSet.Animal;
                }
                else
                    this.genotypeAnimals[idx] = GrazType.AnimalType.Sheep;
            }
            this.currentGenotype = Math.Min(0, this.genotypeInits.Length - 1);
            this.FillCurrGenotype();

            this.filling = true;
            if (this.currentGenotype >= 0)
                this.SelectedGenoIndex = this.currentGenotype;

            // populate the animal groups list
            for (int i = 0; i < animalInits.Length; i++)
            {
                string groupText = GroupText(i);
                this.groupsList.AppendValues(groupText);
            }
            this.currentGroup = Math.Min(0, animalInits.Length - 1);    // could be -1
            //this.ClickAnimalList(null);     // display initial animal group

            this.filling = false;

            this.EnableButtons();
        }

        #region Genotypes tab =================================================

        /// <summary>
        /// Fill the controls on the form
        /// </summary>
        private void FillCurrGenotype()
        {
            GrazType.AnimalType theAnimal;
            SingleGenotypeInits theGenoType;

            if (this.currentGenotype < 0)
                this.gbxGenotype.Hide();
            else
                this.gbxGenotype.Show();


            this.filling = true;

            if (this.currentGenotype >= 0)
            {
                theGenoType = this.genotypeInits[this.currentGenotype];
                theAnimal = this.genotypeAnimals[this.currentGenotype];

                if (theAnimal == GrazType.AnimalType.Sheep)
                {
                    this.deBreedSRW.MaxValue = MAXSHEEPSRW;
                    this.deBreedSRW.MinValue = MINSHEEPSRW;
                }
                else if (theAnimal == GrazType.AnimalType.Cattle)
                {
                    this.deBreedSRW.MaxValue = MAXCATTLESRW;
                    this.deBreedSRW.MinValue = MINCATTLESRW;
                }

                // Enable controls for Peak milk or fleece details
                this.EnablePeakMilkOrFleece(theAnimal);

                this.edtGenotypeName.Text = theGenoType.GenotypeName;

                this.rbtnSheep.Active = (theAnimal == GrazType.AnimalType.Sheep);
                this.rbtnCattle.Active = (theAnimal == GrazType.AnimalType.Cattle);
                if (this.rbtnSheep.Active)
                    this.rbtnSheep.Click();
                else
                    this.rbtnCattle.Click();

                this.cbxGeneration.SelectedIndex = Math.Max(0, Math.Min(theGenoType.Generation, this.cbxGeneration.Values.Length - 1));
                this.ChangeGeneration(null, null);

                if ((theGenoType.Generation == 0) && (theGenoType.DamBreed == ""))                    //sDamBreed
                    this.cbxDamBreed.SelectedIndex = this.cbxDamBreed.IndexOf(theGenoType.GenotypeName);
                else
                    this.cbxDamBreed.SelectedIndex = this.cbxDamBreed.IndexOf(theGenoType.DamBreed);

                this.cbxSireBreed.SelectedIndex = this.cbxSireBreed.IndexOf(theGenoType.SireBreed);
                if (this.cbxSireBreed.SelectedIndex < 0)
                    this.cbxSireBreed.SelectedIndex = this.cbxDamBreed.SelectedIndex;

                this.deBreedSRW.Value = theGenoType.SRW;
                this.deDeath.Value = 100.0 * theGenoType.DeathRate[ADULT];
                this.deWnrDeath.Value = 100 * theGenoType.DeathRate[WNR];
                this.deConception1.Value = 100 * theGenoType.Conceptions[1];
                this.deConception2.Value = 100 * theGenoType.Conceptions[2];

                if (theAnimal == GrazType.AnimalType.Sheep)
                {
                    this.deConception3.Value = 100 * theGenoType.Conceptions[3];
                    this.dePFWMilk.DecPlaces = 2;
                    this.dePFWMilk.MinValue = 0.0;
                    this.dePFWMilk.Value = theGenoType.PotFleeceWt;  // dual purpose control
                    this.deBreedMaxMu.Value = theGenoType.MaxFibreDiam;
                    this.deWoolYield.Value = 100 * theGenoType.FleeceYield;
                }
                else if (theAnimal == GrazType.AnimalType.Cattle)
                {
                    this.dePFWMilk.DecPlaces = 1;
                    this.dePFWMilk.MinValue = Math.Min(10.0, this.deBreedSRW.Value * 0.01);
                    this.dePFWMilk.Value = theGenoType.PeakMilk;
                }
            }
            this.filling = false;
        }

        /// <summary>
        /// When changing the pure/cross bred
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGeneration(object sender, EventArgs e)
        {
            try
            {
                if (this.cbxGeneration.SelectedIndex <= 0)
                {
                    this.lblDamBreed.Text = "Breed";
                    this.lblSireBreed.Hide();
                    this.cbxSireBreed.IsVisible = false;
                }
                else
                {
                    this.lblDamBreed.Text = "Dam breed";
                    this.lblSireBreed.Show();
                    this.cbxSireBreed.IsVisible = true;
                }
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// Switch between sheep and cattle controls
        /// </summary>
        /// <param name="theAnimal"></param>
        private void EnablePeakMilkOrFleece(GrazType.AnimalType theAnimal)
        {
            // Visibility of animal - specific parameters 
            this.lblConception3.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.deConception3.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            //unitConception.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.deWoolYield.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.untWoolYield.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.lblWoolYield.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.deBreedMaxMu.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.unitBreedMaxMu.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            this.lblBreedMaxMu.Visible = (theAnimal == GrazType.AnimalType.Sheep);
            if (theAnimal == GrazType.AnimalType.Sheep)
            {
                this.lblBreedPFWPeakMilk.Text = "Breed potential fleece weight";
                this.unitBreedPFWPeakMilk.Text = "kg";
            }
            else
            {
                //cattle
                this.lblBreedPFWPeakMilk.Text = "Peak milk production";
                this.unitBreedPFWPeakMilk.Text = "kg FCM";
            }
        }


        /// <summary>
        /// Store the current genotype
        /// </summary>
        private void ParseCurrGenotype()
        {
            SingleGenotypeInits theGenoType;

            if (this.currentGenotype >= 0 && !this.filling)
            {
                theGenoType = new SingleGenotypeInits();
                theGenoType.Conceptions = new double[4];
                theGenoType.GenotypeName = this.edtGenotypeName.Text;

                theGenoType.Generation = this.cbxGeneration.SelectedIndex;
                if (theGenoType.Generation > 0)
                {
                    theGenoType.DamBreed = this.cbxDamBreed.SelectedValue;
                    theGenoType.SireBreed = this.cbxSireBreed.SelectedValue;
                }
                else if (this.cbxDamBreed.SelectedValue != null && (this.cbxDamBreed.SelectedValue.ToLower() == theGenoType.GenotypeName.ToLower()))
                {
                    theGenoType.DamBreed = string.Empty;
                    theGenoType.SireBreed = string.Empty;
                }
                else
                {
                    theGenoType.DamBreed = this.cbxDamBreed.SelectedValue;
                    theGenoType.SireBreed = string.Empty;
                }

                theGenoType.SRW = this.deBreedSRW.Value;
                theGenoType.DeathRate[ADULT] = this.deDeath.Value * 0.01;
                theGenoType.DeathRate[WNR] = this.deWnrDeath.Value * 0.01;
                theGenoType.Conceptions[1] = this.deConception1.Value * 0.01;
                theGenoType.Conceptions[2] = this.deConception2.Value * 0.01;

                if (this.genotypeAnimals[this.currentGenotype] == GrazType.AnimalType.Sheep)
                {
                    theGenoType.Conceptions[3] = this.deConception3.Value * 0.01;
                    theGenoType.PotFleeceWt = this.dePFWMilk.Value;
                    theGenoType.MaxFibreDiam = this.deBreedMaxMu.Value;
                    theGenoType.FleeceYield = this.deWoolYield.Value * 0.01;
                    theGenoType.PeakMilk = 0.0;
                }
                else if (this.genotypeAnimals[this.currentGenotype] == GrazType.AnimalType.Cattle)
                {
                    theGenoType.PeakMilk = dePFWMilk.Value;
                    theGenoType.Conceptions[3] = 0.0;
                    theGenoType.PotFleeceWt = 0.0;
                    theGenoType.MaxFibreDiam = 0.0;
                    theGenoType.FleeceYield = 0.0;
                }
                this.genotypeInits[this.currentGenotype] = theGenoType;
            }
        }

        /// <summary>
        /// Add a new genotype to the list on the form
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event arguments</param>
        private void BtnNewGeno_Clicked(object sender, EventArgs e)
        {
            try
            {
                GrazType.AnimalType newAnimal;
                int newBreedIndex;
                string newBreed = "";
                bool found;
                int index;

                if (this.genotypeInits.Length < MAXGENOTYPES)
                {
                    this.ParseCurrGenotype();

                    // Find the first genotype that is not in the inits yet
                    if (this.currentGenotype >= 0)
                        newAnimal = this.genotypeAnimals[currentGenotype];
                    else
                        newAnimal = GrazType.AnimalType.Sheep;
                    newBreedIndex = 0;
                    found = false;
                    while ((newBreedIndex < paramSet.BreedCount(newAnimal)) && !found)
                    {
                        newBreed = paramSet.BreedName(newAnimal, newBreedIndex);

                        found = true;
                        for (index = 0; index < this.genotypeInits.Length; index++)
                        {
                            found = (found && (newBreed.ToLower() != this.genotypeInits[index].GenotypeName.ToLower()));
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
                        Array.Resize(ref this.genotypeInits, this.genotypeInits.Length + 1);
                        this.genotypeInits[this.genotypeInits.Length - 1] = new SingleGenotypeInits();
                        this.genotypeInits[this.genotypeInits.Length - 1].Conceptions = new double[4];

                        SetGenotypeDefaults(this.genotypeInits.Length - 1, newBreed);
                        this.genoList.AppendValues(newBreed);
                        this.SelectedGenoIndex = this.genotypeInits.Length - 1;
                        ClickGenotypeList(null, null);

                        // add to the animals genotypes combo list on the animals tab
                        string[] genoNames = new string[this.genotypeInits.Length];
                        for (int i = 0; i < this.genotypeInits.Length; i++)
                        {
                            genoNames[i] = this.genotypeInits[i].GenotypeName;
                        }
                        this.cbxGroupGenotype.Values = genoNames;

                        this.EnableButtons();
                    }
                }
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// When an item is clicked in the genotype list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickGenotypeList(object sender, EventArgs e)
        {
            try
            {
                if (this.currentGenotype >= 0 && !this.filling)
                    this.ParseCurrGenotype();
                this.currentGenotype = this.SelectedGenoIndex;
                this.FillCurrGenotype();
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// Enable the add/del buttons
        /// </summary>
        private void EnableButtons()
        {
            this.btnNewGeno.Sensitive = (this.genotypeInits.Length < MAXGENOTYPES);
            this.btnDelGeno.Sensitive = (this.genotypeInits.Length > 0);
            this.btnNewAnimals.Sensitive = (this.animalInits.Length < MAXANIMALGROUPS) && (this.genotypeInits.Length > 0);
            this.btnDeleteAnimals.Sensitive = (this.animalInits.Length > 0);
        }

        /// <summary>
        /// Respond to the changing of the breed using the combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeBreed(object sender, EventArgs e)
        {
            try
            {
                if (!filling)
                {
                    string newGenoName = this.MakeUniqueGenoName(this.cbxDamBreed.SelectedValue);
                    this.SetGenotypeDefaults(this.currentGenotype, this.cbxDamBreed.SelectedValue);
                    this.FillCurrGenotype();
                    this.edtGenotypeName.Text = newGenoName;
                    //ChangeGenotypeName(sender, e);          // ensure trigger updates on the Animals tab also
                    this.filling = true;
                    this.SetItem(this.genoList, this.currentGenotype, newGenoName);
                    this.filling = false;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Make adjustments when the genotype name is edited by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGenotypeName(object sender, EventArgs e)
        {
            try
            {
                if (!this.filling)
                    SetItem(this.genoList, this.currentGenotype, this.edtGenotypeName.Text);
            }
            catch (Exception err)
            {
                ShowError(err);
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
            //int nNames = store.IterNChildren();
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
                theGenoType = this.genotypeInits[index];

                this.genotypeAnimals[index] = breedParams.Animal;

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
                this.lbxGenotypeList.GetCursor(out selPath, out selCol);
                return selPath != null ? selPath.Indices[0] : 0;
            }

            set
            {
                if (value >= 0)
                {
                    int[] indices = new int[1] { value };
                    TreePath selPath = new TreePath(indices);
                    this.lbxGenotypeList.SetCursor(selPath, null, false);
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
            try
            {
                if (((Gtk.RadioButton)sender).Active)
                {
                    GrazType.AnimalType currAnimal;

                    if (this.rbtnSheep.Active)
                        currAnimal = GrazType.AnimalType.Sheep;
                    else
                        currAnimal = GrazType.AnimalType.Cattle;

                    this.genotypeAnimals[this.currentGenotype] = currAnimal;

                    List<string> names = new List<string>();

                    int count = this.paramSet.BreedCount(currAnimal);
                    string[] namesArray = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        namesArray[i] = paramSet.BreedName(currAnimal, i);
                    }

                    this.cbxDamBreed.Changed -= ChangeBreed;

                    this.cbxDamBreed.Values = namesArray;
                    this.cbxDamBreed.SelectedIndex = 0;
                    this.cbxSireBreed.Values = namesArray;
                    this.cbxSireBreed.SelectedIndex = 0;

                    this.cbxDamBreed.Changed += ChangeBreed;

                    this.ChangeBreed(null, null);            //Force default SRW values etc
                }
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// Delete the currently selected genotype from genotypeInits[]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelGeno_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (this.currentGenotype >= 0)
                {
                    //TODO when animals tab is working: deleteGroupsWithGenotype(FCurrGenotype);

                    for (int idx = this.currentGenotype + 1; idx <= this.genotypeInits.Length - 1; idx++)
                        this.genotypeInits[idx - 1] = this.genotypeInits[idx];
                    Array.Resize(ref this.genotypeInits, this.genotypeInits.Length - 1);

                    int current = this.SelectedGenoIndex;
                    // repopulate the view
                    this.SetValues();
                    this.SelectedGenoIndex = Math.Min(current, this.genotypeInits.Length - 1);
                    this.EnableButtons();
                }
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbxGenotypeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                this.ClickGenotypeList(sender, e);
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        #endregion========

        #region Animals tab ===================================================

        /// <summary>
        /// Gets or sets the selected index for the animal group treeview
        /// </summary>
        private int SelectedGroupIndex
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                this.lbxAnimalList.GetCursor(out selPath, out selCol);
                return selPath != null ? selPath.Indices[0] : -1;
            }

            set
            {
                if (value >= 0)
                {
                    int[] indices = new int[1] { value };
                    TreePath selPath = new TreePath(indices);
                    this.lbxAnimalList.SetCursor(selPath, null, false);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbxAnimalList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.SelectedGroupIndex >= 0)
                    this.ClickAnimalList(sender);
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// Stores the index of the selected group in CurrGroup and displays its values
        /// </summary>
        private void ClickAnimalList(object sender)
        {
            bool valuesOK;

            // This procedure is called in several places: ClickAnimalList(nil).
            // We only want to check the weights, if user clicked AnimalList, so we have to check the Sender:
            if (this.currentGroup >= 0)
            {
                valuesOK = ((sender != this.lbxAnimalList) || CheckCurrGroup(this.lbxAnimalList, true));
                if (!valuesOK)
                {
                    if (this.SelectedGroupIndex != this.currentGroup)
                        this.SelectedGroupIndex = this.currentGroup;
                }
                else
                {
                    if (this.lbxAnimalList.Data.Count > 0)
                        if (!this.filling)
                            this.ParseCurrGroup();
                    this.currentGroup = this.SelectedGroupIndex;

                    this.FillCurrentGroup();
                }
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
            bool isMature;
            bool isPregnant;
            bool isLactating;
            bool hasYoung;
            int genotypeIdx;
            AnimalInits animalGroup;
            GrazType.AnimalType animalType;

            if (this.currentGroup < 0)
                this.gbxAnimals.Child.HideAll();
            else
                this.gbxAnimals.Child.ShowAll();

            if (this.currentGroup >= 0)
            {
                this.filling = true;    // disable event handlers
                animalGroup = this.animalInits[currentGroup];
                // get the animaltype for this group
                string geno = animalGroup.Genotype;
                genotypeIdx = cbxGroupGenotype.IndexOf(geno);
                if (genotypeIdx >= 0)
                    animalType = genotypeAnimals[genotypeIdx];
                else
                    animalType = GrazType.AnimalType.Sheep;

                this.cbxGroupGenotype.SelectedIndex = genotypeIdx;

                string[] sexNames = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    sexNames[i] = SEXMAP[(int)animalType, i].Text;
                }
                this.cbxSex.Values = sexNames;
                this.cbxSex.SelectedIndex = REPRO2MAP[(int)animalGroup.Sex];
                this.deNumber.Value = animalGroup.Number;
                this.deAge.Value = animalGroup.AgeDays * DAY2MONTH;
                this.deWeight.Value = animalGroup.Weight;
                this.edtPaddock.Text = animalGroup.Paddock;
                this.deTag.Value = animalGroup.Tag;
                this.dePriority.Value = animalGroup.Priority;

                // Highest previous weight is only required for immature animals
                isMature = ((animalType == GrazType.AnimalType.Sheep) && (animalGroup.AgeDays >= 24.0 / DAY2MONTH)) || ((animalType == GrazType.AnimalType.Cattle) && (animalGroup.AgeDays >= 36.0 / DAY2MONTH));
                this.lblPrevWt.Visible = !isMature;
                this.dePrevWt.Visible = !isMature;
                this.untPrevWt.Visible = !isMature;
                this.dePrevWt.Value = Math.Max(animalGroup.MaxPrevWt, animalGroup.Weight);

                // Fleece weight and diameter are only required for sheep        
                this.lblFleece.Visible = (animalType == GrazType.AnimalType.Sheep);
                this.deFleece.Visible = (animalType == GrazType.AnimalType.Sheep);
                this.untFleece.Visible = (animalType == GrazType.AnimalType.Sheep);
                this.deFleece.Value = animalGroup.FleeceWt;

                this.lblFibreDiam.Visible = (animalType == GrazType.AnimalType.Sheep);
                this.deFibreDiam.Visible = (animalType == GrazType.AnimalType.Sheep);
                this.untFibreDiam.Visible = (animalType == GrazType.AnimalType.Sheep);
                this.deFibreDiam.Value = animalGroup.FibreDiam;


                this.pnlRepro.Visible = (animalGroup.Sex == GrazType.ReproType.Empty); // Reproduction-related controls are on a frame

                if (this.pnlRepro.Visible)
                {
                    isPregnant = (animalGroup.Pregnant > 0);
                    isLactating = (animalGroup.Lactating > 0);
                    hasYoung = (animalGroup.NumSuckling > 0);

                    this.rgrpSRepro.Visible = (animalType == GrazType.AnimalType.Sheep);                          // Reproduction options for sheep           
                    if (animalType == GrazType.AnimalType.Sheep)
                    {
                        if (!(isPregnant || isLactating))
                            this.rbDryEmpty.Active = true;
                        else if (isPregnant)
                            this.rbPregS.Active = true;
                        else if (isLactating)
                            this.rbLact.Active = true;
                    }

                    this.rgrpCPreg.Visible = (animalType == GrazType.AnimalType.Cattle);                          // Pregnancy options for cattle             
                    if (animalType == GrazType.AnimalType.Cattle)
                    {
                        if (!isPregnant)
                            this.rbEmpty.Active = true;
                        else
                            this.rbPreg.Active = true;
                    }

                    this.rgrpCLact.Visible = (animalType == GrazType.AnimalType.Cattle);                          // Lactation options for cattle            
                    if (animalType == GrazType.AnimalType.Cattle)
                        if (!isLactating)
                            this.rbNoLact.Active = true;
                        else if (isLactating && hasYoung)
                            this.rbLac.Active = true;
                        else if (isLactating && !hasYoung)
                            this.rbLactCalf.Active = true;

                    this.rgrpNoLambs.Visible = (animalType == GrazType.AnimalType.Sheep) && (isPregnant || isLactating);
                    if (isPregnant)
                    {
                        this.rbOneLamb.Active = animalGroup.NumFoetuses == 1;
                        this.rbTwoLambs.Active = animalGroup.NumFoetuses == 2;
                        this.rbThreeLambs.Active = animalGroup.NumFoetuses == 3;
                    }
                    else if (isLactating)
                    {
                        this.rbOneLamb.Active = animalGroup.NumSuckling == 1;
                        this.rbTwoLambs.Active = animalGroup.NumSuckling == 2;
                        this.rbThreeLambs.Active = animalGroup.NumSuckling == 3;
                    }

                    this.lblPregnant.Visible = isPregnant;
                    this.dePregnant.Visible = isPregnant;
                    this.untPregnant.Visible = isPregnant;
                    this.dePregnant.Value = animalGroup.Pregnant;

                    this.lblLactating.Visible = isLactating;
                    this.deLactating.Visible = isLactating;
                    this.untLactating.Visible = isLactating;
                    this.deLactating.Value = animalGroup.Lactating;

                    this.lblBirthCS.Visible = isLactating;
                    this.deBirthCS.Visible = isLactating;
                    this.deBirthCS.Value = animalGroup.BirthCS;

                    if (animalType == GrazType.AnimalType.Sheep)
                        this.lblYoungWt.Text = "Lamb weight";
                    else if (animalType == GrazType.AnimalType.Cattle)
                        this.lblYoungWt.Text = "Calf weight";
                    this.lblYoungWt.Visible = isLactating && hasYoung;
                    this.deYoungWt.Visible = isLactating && hasYoung;
                    this.untYoungWt.Visible = isLactating && hasYoung;
                    this.deYoungWt.Value = animalGroup.YoungWt;

                    this.lblLambGFW.Visible = (animalType == GrazType.AnimalType.Sheep) && isLactating;
                    this.deLambGFW.Visible = (animalType == GrazType.AnimalType.Sheep) && isLactating;
                    this.untLambGFW.Visible = (animalType == GrazType.AnimalType.Sheep) && isLactating;
                    this.deLambGFW.Value = animalGroup.YoungGFW;
                }



                // Update the descriptive string in the animal groups list box
                string groupText = GroupText(currentGroup);
                TreeIter iter;
                this.groupsList.IterNthChild(out iter, currentGroup);
                this.groupsList.SetValue(iter, 0, groupText);

                this.filling = false;
            }
        }

        /// <summary>
        /// Read the animal group details from the form
        /// </summary>
        private void ParseCurrGroup()
        {
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
                    animalGroup.Pregnant = 0;
                    animalGroup.Lactating = 0;
                    animalGroup.NumFoetuses = 0;
                    animalGroup.NumSuckling = 0;
                    animalGroup.BirthCS = 0;
                    animalGroup.YoungWt = 0.0;
                    animalGroup.YoungGFW = 0.0;
                    animalGroup.MatedTo = "";

                    bool isPregnant = false;
                    bool isLactating = false;
                    bool hasYoung = false;

                    if (animalGroup.Sex == GrazType.ReproType.Empty)
                    {
                        if (animalType == GrazType.AnimalType.Sheep)
                        {
                            isPregnant = rbPregS.Active == true;
                            isLactating = rbLact.Active == true;
                            hasYoung = isLactating;
                            if (isPregnant)
                            {
                                if (rbOneLamb.Active == true)
                                    animalGroup.NumFoetuses = 1;
                                if (rbTwoLambs.Active == true)
                                    animalGroup.NumFoetuses = 2;
                                if (rbThreeLambs.Active == true)
                                    animalGroup.NumFoetuses = 3;
                            }
                            if (hasYoung)
                            {
                                if (rbOneLamb.Active == true)
                                    animalGroup.NumSuckling = 1;
                                if (rbTwoLambs.Active == true)
                                    animalGroup.NumSuckling = 2;
                                if (rbThreeLambs.Active == true)
                                    animalGroup.NumSuckling = 3;
                            }
                        }
                        else if (animalType == GrazType.AnimalType.Cattle)
                        {
                            isPregnant = rbPreg.Active;
                            isLactating = rbLact.Active || rbLactCalf.Active;
                            hasYoung = rbLactCalf.Active;
                            if (isPregnant)
                                animalGroup.NumFoetuses = 1;    // do we allow for twin calves?
                            if (hasYoung)
                                animalGroup.NumSuckling = 1;
                            else
                                animalGroup.NumSuckling = 0;
                        }

                        if (isPregnant)
                            animalGroup.Pregnant = Convert.ToInt32(dePregnant.Value);
                        if (isLactating)
                        {
                            animalGroup.Lactating = Convert.ToInt32(deLactating.Value);
                            animalGroup.BirthCS = deBirthCS.Value;
                        }
                        if (isLactating && hasYoung)
                            animalGroup.YoungWt = deYoungWt.Value;
                        if (isLactating && hasYoung && (animalType == GrazType.AnimalType.Sheep))
                            animalGroup.YoungGFW = deLambGFW.Value;
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
            try
            {
                if (CheckCurrGroup(null, true) && this.animalInits.Length < MAXANIMALGROUPS)
                {
                    Array.Resize(ref this.animalInits, this.animalInits.Length + 1);

                    int newIdx = this.animalInits.Length - 1;
                    if (newIdx > 0)
                    {
                        this.animalInits[newIdx] = this.animalInits[newIdx - 1];
                    }
                    else
                    {
                        this.animalInits[newIdx] = new AnimalInits();

                        this.animalInits[newIdx].Genotype = this.genotypeInits[0].GenotypeName;
                        this.animalInits[newIdx].FibreDiam = 20;
                    }

                    string groupText = GroupText(newIdx);
                    this.groupsList.AppendValues(groupText);
                    this.currentGroup = newIdx;
                    this.SelectedGroupIndex = currentGroup;
                    //this.ClickAnimalList(null);

                    this.EnableButtons();
                }
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeleteAnimals_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (SelectedGroupIndex >= 0 && (animalInits.Length > 0))
                {
                    int current = SelectedGroupIndex;
                    // delete the currentGroup
                    for (int i = currentGroup + 1; i <= animalInits.Length - 1; i++)
                    {
                        animalInits[i - 1] = animalInits[i];
                    }
                    Array.Resize(ref animalInits, animalInits.Length - 1);

                    // remove from the tree
                    TreeIter iter;
                    groupsList.IterNthChild(out iter, currentGroup);
                    groupsList.Remove(ref iter);

                    currentGroup = -1;              // prevent checking and parsing 
                    int newIdx = Math.Min(current, animalInits.Length - 1);
                    if (newIdx >= 0)
                    {
                        SelectedGroupIndex = newIdx;
                    }
                    this.ClickAnimalList(null);          // will also set currentGroup
                    if (animalInits.Length < 1)
                        gbxAnimals.Child.HideAll();
                }

                this.EnableButtons();
            }
            catch (Exception err)
            {
                this.ShowError(err);
            }
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
            {
                try
                {
                    animalInits[currentGroup].Genotype = cbxGroupGenotype.SelectedValue;
                    this.FillCurrentGroup();
                }
                catch (Exception err)
                {
                    this.ShowError(err);
                }
            }
        }

        /// <summary>
        /// Changing the number of stock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeNumber(object sender, EventArgs e)
        {
            if (!this.filling)
            {
                try
                {
                    animalInits[currentGroup].Number = Convert.ToInt32(deNumber.Value);
                    this.FillCurrentGroup();
                }
                catch (Exception err)
                {
                    this.ShowError(err);
                }
            }
        }

        /// <summary>
        /// change the sex name for this animal group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeSex(object sender, EventArgs e)
        {
            if (!this.filling)
            {
                try
                {
                    GrazType.AnimalType animalType = GetAnimalTypeForGroup(currentGroup);
                    animalInits[currentGroup].Sex = SEXMAP[(int)animalType, cbxSex.SelectedIndex].Repro;
                    this.FillCurrentGroup();
                    this.CheckCurrGroup(cbxSex, false);
                }
                catch (Exception err)
                {
                    this.ShowError(err);
                }
            }
        }

        private void ChangeEditCtrl(object sender, EventArgs e)
        {
            if (!filling)
            {
                try
                {
                    this.CheckCurrGroup(sender, false);
                    this.FillCurrentGroup();
                }
                catch (Exception err)
                {
                    this.ShowError(err);
                }
            }
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
            int[] weightDecPlaces = { 2, 1 };
            GrazType.AnimalType theAnimal = GrazType.AnimalType.Sheep;
            double lowFFLW = 0,
            highFFLW = 2000,
            highGFW;
            string errorMessage = string.Empty;
            int errorNum = 0;
            GrazType.ReproType reproStatus;

            if (currentGroup >= 0)
            {
                this.ParseCurrGenotype();
                this.ParseCurrGroup();

                int genotypeIdx = IndexOf(genoList, animalInits[currentGroup].Genotype);
                if (genotypeIdx < 0) //no genotype specified
                    errorNum = 7;
                else
                    theAnimal = genotypeAnimals[genotypeIdx];

                AnimalInits theAnimalGroup = animalInits[currentGroup];
                SingleGenotypeInits theGenoType = genotypeInits[genotypeIdx];

                if (errorNum == 0)
                {
                    if (theAnimalGroup.Pregnant > 0)
                        reproStatus = GrazType.ReproType.EarlyPreg;
                    else
                        reproStatus = theAnimalGroup.Sex;

                    // calc the normal weight range 
                    if (OnCalcNormalWeight != null)
                        OnCalcNormalWeight.Invoke(this.paramSet, genotypeInits, genotypeIdx, reproStatus, Convert.ToInt32(theAnimalGroup.AgeDays), 0.70, 1.30, out lowFFLW, out highFFLW);

                    if ((theAnimalGroup.Weight - theAnimalGroup.FleeceWt < lowFFLW) || (theAnimalGroup.Weight - theAnimalGroup.FleeceWt > highFFLW))
                        errorNum = 1;
                }
                if ((errorNum == 0) && dePrevWt.Visible)
                {
                    if ((theAnimalGroup.MaxPrevWt < theAnimalGroup.Weight) || (theAnimalGroup.MaxPrevWt - theAnimalGroup.FleeceWt > highFFLW))
                        errorNum = 2;
                }
                if ((errorNum == 0) && (theAnimalGroup.Lactating > 0) && (theAnimalGroup.NumSuckling > 0))
                {
                    // Lactating holds the age of the suckling young
                    if (OnCalcNormalWeight != null)
                        OnCalcNormalWeight.Invoke(this.paramSet, genotypeInits, genotypeIdx, GrazType.ReproType.Castrated, theAnimalGroup.Lactating, 0.70, 1.30, out lowFFLW, out highFFLW);

                    if ((theAnimalGroup.YoungWt - theAnimalGroup.YoungGFW < lowFFLW) || (theAnimalGroup.YoungWt - theAnimalGroup.YoungGFW > highFFLW))
                        errorNum = 3;
                }
                if ((errorNum == 0) && (theAnimal == GrazType.AnimalType.Sheep))
                {
                    switch (theAnimalGroup.Sex)
                    {
                        case GrazType.ReproType.Male:
                            highGFW = 1.5 * 1.4 * theGenoType.PotFleeceWt;
                            break;
                        case GrazType.ReproType.Castrated:
                            highGFW = 1.5 * 1.2 * theGenoType.PotFleeceWt;
                            break;
                        default:
                            highGFW = 1.5 * 1.0 * theGenoType.PotFleeceWt;
                            break;
                    }
                    if ((theAnimalGroup.FleeceWt < 0.0) || (theAnimalGroup.FleeceWt > highGFW))
                        errorNum = 4;
                    else if ((theAnimalGroup.FibreDiam < 0.5 * theGenoType.MaxFibreDiam) || (theAnimalGroup.FibreDiam > 1.5 * theGenoType.MaxFibreDiam))
                        errorNum = 5;
                    else if ((theAnimalGroup.Lactating > 0) && (theAnimalGroup.YoungGFW < 0.0) || (theAnimalGroup.YoungGFW > highGFW))
                        errorNum = 6;
                }
                else
                    highGFW = 0.0;


                string errorMsg = string.Empty;
                lblError.Text = string.Empty;
                string decplaces = weightDecPlaces[(int)theAnimal].ToString();
                if (errorNum > 0)
                {
                    switch (errorNum)
                    {
                        case 1:
                            {
                                errorMessage = string.Format("{0,0:f" + decplaces + "} to {1,0:f" + decplaces + "}", lowFFLW + theAnimalGroup.FleeceWt, highFFLW + theAnimalGroup.FleeceWt);
                                if (showErrorMsg)
                                    errorMessage = "Live weight should be in the range " + errorMessage;
                            }
                            break;
                        case 2:
                            {
                                errorMessage = string.Format("{0,0:f" + decplaces + "} to {1,0:f" + decplaces + "}", theAnimalGroup.Weight, highFFLW + theAnimalGroup.FleeceWt);
                                if (showErrorMsg)
                                    errorMessage = "Highest previous weight should be in the range " + errorMessage;
                            }
                            break;
                        case 3:
                            {
                                errorMessage = string.Format("{0,0:f" + decplaces + "} to {1,0:f" + decplaces + "}", lowFFLW + theAnimalGroup.YoungGFW, highFFLW + theAnimalGroup.YoungGFW);
                                if (showErrorMsg)
                                    if (theAnimal == GrazType.AnimalType.Sheep)
                                        errorMessage = "Lamb weight should be in the range " + errorMessage;
                                    else
                                        errorMessage = "Calf weight should be in the range " + errorMessage;
                            }
                            break;
                        case 4:
                            {
                                errorMessage = string.Format("0.00 to {0,2:f1}", highGFW);
                                if (showErrorMsg)
                                    errorMessage = "Fleece weight should be in the range " + errorMessage;
                            }
                            break;
                        case 5:
                            {
                                errorMessage = string.Format("{0,2:f1} to {1,2:f1}", 0.5 * this.genotypeInits[genotypeIdx].MaxFibreDiam, 1.5 * this.genotypeInits[genotypeIdx].MaxFibreDiam);
                                if (showErrorMsg)
                                    errorMessage = "Wool fibre diameter should be in the range " + errorMessage;
                            }
                            break;
                        case 6:
                            {
                                errorMessage = string.Format("0.00 to {0,2:f1", highGFW);
                                if (showErrorMsg)
                                    errorMessage = "Fleece weight should be in the range " + errorMessage;
                            }
                            break;
                        case 7:
                            {
                                if (showErrorMsg)
                                    errorMessage = "One of the animal groups (" + (currentGroup + 1).ToString() + ") does not have a genotype!";
                            }
                            break;
                    }

                    if (showErrorMsg)
                    {
                        lblError.Text = errorMessage;
                    }
                }
            }

            return (errorNum == 0);
        }

        /// <summary>
        /// Sets default values for time pregnant etc and then calls FillGroup() to re-display the controls
        /// </summary>    
        private void ClickSheepRepro(object sender, EventArgs e)
        {
            if (!this.filling)
            {
                if (this.currentGroup >= 0)
                {
                    AnimalInits animals = animalInits[currentGroup];
                    if (this.rbDryEmpty.Active)
                    {
                        // dry, empty
                        animals.Pregnant = 0;
                        animals.Lactating = 0;
                        animals.NumFoetuses = 0;
                        animals.NumSuckling = 0;
                        animals.YoungWt = 0.0;
                        animals.YoungGFW = 0.0;
                        animals.BirthCS = 0.0;
                    }
                    else if (this.rbPregS.Active)
                    {
                        // pregnant, not lactating
                        if (animals.Pregnant == 0)
                            animals.Pregnant = 60;
                        animals.NumFoetuses = Math.Max(1, Math.Max(animals.NumFoetuses, animals.NumSuckling));
                        animals.Lactating = 0;
                        animals.NumSuckling = 0;
                        animals.YoungWt = 0.0;
                        animals.YoungGFW = 0.0;
                        animals.BirthCS = 0.0;
                    }
                    else if (this.rbLact.Active)
                    {
                        // lactating, not pregnant
                        animals.NumSuckling = Math.Max(1, Math.Max(animals.NumFoetuses, animals.NumSuckling));
                        if (animals.Lactating == 0)
                        {
                            animals.Lactating = 30;
                            animals.YoungWt = 10.0;
                            animals.YoungGFW = 0.4;
                            animals.BirthCS = 3.0;
                        }
                        animals.Pregnant = 0;
                        animals.NumFoetuses = 0;
                    }
                    animalInits[currentGroup] = animals;
                    this.FillCurrentGroup();
                    this.CheckCurrGroup(null, false);
                }
            }
        }


        /// <summary>
        /// OnClick handler for rgrpCRepro and rgrpCLact
        /// Set default values for time pregnant, etc and then calls FillGroup() to re-display the controls
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickCattlePreg(object sender, EventArgs e)
        {
            if (!this.filling)
            {
                AnimalInits animals = this.animalInits[this.currentGroup];
                if (this.rbEmpty.Active)
                {
                    animals.Pregnant = 0;
                }
                else if (this.rbPreg.Active)
                {
                    if (animals.Pregnant == 0)
                    {
                        animals.Pregnant = 120;
                    }
                }

                this.animalInits[this.currentGroup] = animals;
                this.FillCurrentGroup();
                this.CheckCurrGroup(null, false);
            }
        }

        /// <summary>
        /// Handler for changing the cattle lactation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickCattleLact(object sender, EventArgs e)
        {
            if (!this.filling)
            {
                AnimalInits animals = this.animalInits[this.currentGroup];
                if (this.rbNoLact.Active)
                {
                    // Not lactating                         
                    animals.Lactating = 0;
                    animals.NumSuckling = 0;
                    animals.YoungWt = 0;
                    animals.BirthCS = 0;
                }
                else if (this.rbLac.Active)
                {
                    // Lactating, calves suckling            
                    if (animals.NumSuckling == 0)
                    {
                        animals.Lactating = 30;
                        animals.YoungWt = 100;
                        animals.BirthCS = 3.0;
                    }
                    animals.NumSuckling = 1;
                }
                else if (this.rbLactCalf.Active)
                {
                    // Lactating, no calves suckling         
                    if ((animals.Lactating == 0) || (animals.NumSuckling > 0))
                    {
                        animals.Lactating = 150;
                        animals.BirthCS = 3.0;
                    }
                    animals.NumSuckling = 0;
                    animals.YoungWt = 0;
                }

                this.animalInits[this.currentGroup] = animals;
                this.FillCurrentGroup();
                this.CheckCurrGroup(null, false);
            }
        }
        #endregion Animals tab ==================
    }
}

