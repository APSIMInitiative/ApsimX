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
        private const int MAX_GENOTYPES = 20;
        private int FCurrGenotype;
        private GrazType.AnimalType[] FGenotypeAnimals = new GrazType.AnimalType[20];
        private AnimalParamSet paramSet;
        private AnimalParamSet genotypeSet;
        private bool FILLING = false;

        private const double MINSHEEPSRW = 5.0;
        private const double MAXSHEEPSRW = 100.0;
        private const double MINCATTLESRW = 30.0;
        private const double MAXCATTLESRW = 1200.0;
        
        /// <summary>
        /// The array of genotype specifications from the component inits
        /// </summary>
        private StockGeno[] genotypeInits;

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
        private Label lblBreedPFW_PeakMilk = null;
        private Label unitBreedPFW_PeakMilk = null;
        private Label untWoolYield = null;
        private Label lblWoolYield = null;
        private Label lblDamBreed = null;
        private Label lblSireBreed = null;
        private Label lblBreedMaxMu = null;
        private Label unitBreedMaxMu = null;

        // the wrappers for the edit controls that have floating points
        private DoubleEditView deWnrDeath = null;
        private DoubleEditView deDeath = null;
        private DoubleEditView dePFW_Milk = null;
        private DoubleEditView deBreedSRW = null;
        private DoubleEditView deBreedMaxMu = null;
        private DoubleEditView deWoolYield = null;
        private DoubleEditView deConception1 = null;
        private DoubleEditView deConception2 = null;
        private DoubleEditView deConception3 = null;

        /// <summary>
        /// The list of genotypes in the treeview
        /// </summary>
        private ListStore genoList = new ListStore(typeof(string));

        // animals tab
        private DropDownView cbxGroupGenotype = null;

        public event EventHandler<GenotypeInitArgs> GetGenoParams;

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
            lblBreedPFW_PeakMilk = (Label)builder.GetObject("lblBreedPFW_PeakMilk");
            unitBreedPFW_PeakMilk = (Label)builder.GetObject("unitBreedPFW_PeakMilk");
            untWoolYield = (Label)builder.GetObject("untWoolYield");
            lblWoolYield = (Label)builder.GetObject("lblWoolYield");
            lblDamBreed = (Label)builder.GetObject("lblDamBreed");
            lblSireBreed = (Label)builder.GetObject("lblSireBreed");
            lblBreedMaxMu = (Label)builder.GetObject("lblBreedMaxMu");
            unitBreedMaxMu = (Label)builder.GetObject("unitBreedMaxMu");

            cbxDamBreed = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxDamBreed"));
            cbxSireBreed = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxSireBreed"));
            cbxGeneration = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxGeneration"));
            cbxGroupGenotype = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxGroupGenotype"));   // animals tab

            deWnrDeath = new DoubleEditView(this, (Entry)builder.GetObject("edtWnrDeathRate"), 100, 0, 1);
            deDeath = new DoubleEditView(this, (Entry)builder.GetObject("edtDeathRate"), 100, 0, 1);
            dePFW_Milk = new DoubleEditView(this, (Entry)builder.GetObject("edtBreedPFW_PeakMilk"), 100, 0, 2);
            deBreedSRW = new DoubleEditView(this, (Entry)builder.GetObject("edtBreedSRW"));
            deBreedSRW.DecPlaces = 1;
            deBreedMaxMu = new DoubleEditView(this, (Entry)builder.GetObject("edtBreedMaxMu"), 50, 5, 1);
            deWoolYield = new DoubleEditView(this, (Entry)builder.GetObject("edtWoolYield"), 100, 50, 1);
            deConception1 = new DoubleEditView(this, (Entry)builder.GetObject("edtConception1"), 100, 0, 0);
            deConception2 = new DoubleEditView(this, (Entry)builder.GetObject("edtConception2"), 100, 0, 0);
            deConception3 = new DoubleEditView(this, (Entry)builder.GetObject("edtConception3"), 100, 0, 0);

            cbxGeneration.Values = new string[] { "Purebred", "First cross", "Second cross", "Third cross", "Fourth cross", "Fifth cross", "Sixth cross" };

            // configure the treeview of genotype names
            lbxGenotypeList = (Gtk.TreeView)builder.GetObject("tvGenotypes");
            lbxGenotypeList.Model = genoList;
            CellRendererText textRender = new Gtk.CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Genotype Names", textRender, "text", 0);
            lbxGenotypeList.AppendColumn(column);
            lbxGenotypeList.HeadersVisible = false;
            lbxGenotypeList.CursorChanged += lbxGenotypeList_SelectedIndexChanged;

            btnNewGeno.Clicked += btnNewGeno_Clicked;
            btnDelGeno.Clicked += btnDelGeno_Clicked;
            edtGenotypeName.Changed += ChangeGenotypeName;
            rbtnSheep.Clicked += ClickAnimal;
            rbtnCattle.Clicked += ClickAnimal;
            cbxDamBreed.Changed += ChangeBreed;
            cbxGeneration.Changed += ChangeGeneration;

            _mainWidget = notebook1;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// The list of genotypes in the component's inits
        /// </summary>
        public StockGeno[] Genotypes
        {
            get
            {
                if (FCurrGenotype >= 0)
                    ParseCurrGenotype();
                return genotypeInits;
            }
            set
            {
                genotypeInits = new StockGeno[value.Length];
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
                genoNames[i] = genotypeInits[i].Name;
                genoList.AppendValues(genotypeInits[i].Name);
            }
            cbxGroupGenotype.Values = genoNames;        // animals tab

            GenotypeInitArgs args = new GenotypeInitArgs();
            args.Genotypes = genotypeInits;
            args.ParamSet = this.paramSet;
            for (int idx = 0; idx < genotypeInits.Length; idx++)
            {
                args.index = idx;
                GetGenoParams.Invoke(null, args);   // gets params from the stock model

                if (this.genotypeSet != null)
                {
                    FGenotypeAnimals[idx] = this.genotypeSet.Animal;
                }
                else
                    FGenotypeAnimals[idx] = GrazType.AnimalType.Sheep;
            }
            FCurrGenotype = Math.Min(0, genotypeInits.Length - 1);
            FillCurrGenotype();

            FILLING = true;
            if (FCurrGenotype >= 0)
                SelectedGenoIndex = FCurrGenotype;
            FILLING = false;

            EnableButtons();
        }

        /// <summary>
        /// Fill the controls on the form
        /// </summary>
        private void FillCurrGenotype()
        {
            GrazType.AnimalType theAnimal;
            StockGeno theGenoType;

            if (FCurrGenotype < 0)
                gbxGenotype.Hide();
            else
                gbxGenotype.Show();


            FILLING = true;

            if (FCurrGenotype >= 0)
            {
                theGenoType = this.genotypeInits[FCurrGenotype];
                theAnimal = FGenotypeAnimals[FCurrGenotype];

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

                edtGenotypeName.Text = theGenoType.Name;

                rbtnSheep.Active = (theAnimal == GrazType.AnimalType.Sheep);
                rbtnCattle.Active = (theAnimal == GrazType.AnimalType.Cattle);

                cbxGeneration.SelectedIndex = Math.Max(0, Math.Min(theGenoType.Generation, cbxGeneration.Values.Length - 1));
                ChangeGeneration(null, null);

                if ((theGenoType.Generation == 0) && (theGenoType.DamBreed == ""))                    //sDamBreed
                    cbxDamBreed.SelectedIndex = cbxDamBreed.IndexOf(theGenoType.Name);
                else
                    cbxDamBreed.SelectedIndex = cbxDamBreed.IndexOf(theGenoType.DamBreed);

                cbxSireBreed.SelectedIndex = cbxSireBreed.IndexOf(theGenoType.SireBreed);
                if (cbxSireBreed.SelectedIndex < 0)
                    cbxSireBreed.SelectedIndex = cbxDamBreed.SelectedIndex;

                deBreedSRW.Value = theGenoType.SRW;
                deDeath.Value = 100 * theGenoType.DeathRate;
                deWnrDeath.Value = 100 * theGenoType.WnrDeathRate;
                deConception1.Value = 100 * theGenoType.Conception[1];
                deConception2.Value = 100 * theGenoType.Conception[2];

                if (theAnimal == GrazType.AnimalType.Sheep)
                {
                    deConception3.Value = 100 * theGenoType.Conception[3];
                    dePFW_Milk.DecPlaces = 2;
                    dePFW_Milk.MinValue = 0.0;
                    dePFW_Milk.Value = theGenoType.RefFleeceWt;
                    deBreedMaxMu.Value = theGenoType.MaxFibreDiam;
                    deWoolYield.Value = 100 * theGenoType.FleeceYield;
                }
                else if (theAnimal == GrazType.AnimalType.Cattle)
                {
                    dePFW_Milk.DecPlaces = 1;
                    dePFW_Milk.MinValue = Math.Min(10.0, deBreedSRW.Value * 0.01);
                    dePFW_Milk.Value = theGenoType.PeakMilk;
                }
            }
            FILLING = false;
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
                lblBreedPFW_PeakMilk.Text = "Breed potential fleece weight";
                unitBreedPFW_PeakMilk.Text = "kg";
            }
            else
            {
                //cattle
                lblBreedPFW_PeakMilk.Text = "Peak milk production";
                unitBreedPFW_PeakMilk.Text = "kg FCM";
            }
        }

        /// <summary>
        /// Store the current genotype
        /// </summary>
        private void ParseCurrGenotype()
        {
            StockGeno theGenoType;

            if (FCurrGenotype >= 0 && !FILLING)
            {
                theGenoType = new StockGeno();
                theGenoType.Conception = new double[4];
                theGenoType.Name = edtGenotypeName.Text;

                theGenoType.Generation = cbxGeneration.SelectedIndex;
                if (theGenoType.Generation > 0)
                {
                    theGenoType.DamBreed = cbxDamBreed.SelectedValue;
                    theGenoType.SireBreed = cbxSireBreed.SelectedValue;
                }
                else if (cbxDamBreed.SelectedValue.ToLower() == theGenoType.Name.ToLower())
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
                theGenoType.DeathRate = deDeath.Value * 0.01;
                theGenoType.WnrDeathRate = deWnrDeath.Value * 0.01;
                theGenoType.Conception[1] = deConception1.Value * 0.01;
                theGenoType.Conception[2] = deConception2.Value * 0.01;

                if (FGenotypeAnimals[FCurrGenotype] == GrazType.AnimalType.Sheep)
                {
                    theGenoType.Conception[3] = deConception3.Value * 0.01;
                    theGenoType.RefFleeceWt = dePFW_Milk.Value;
                    theGenoType.MaxFibreDiam = deBreedMaxMu.Value;
                    theGenoType.FleeceYield = deWoolYield.Value * 0.01;
                    theGenoType.PeakMilk = 0.0;
                }
                else if (FGenotypeAnimals[FCurrGenotype] == GrazType.AnimalType.Cattle)
                {
                    theGenoType.PeakMilk = dePFW_Milk.Value;
                    theGenoType.Conception[3] = 0.0;
                    theGenoType.RefFleeceWt = 0.0;
                    theGenoType.MaxFibreDiam = 0.0;
                    theGenoType.FleeceYield = 0.0;
                }
                this.genotypeInits[FCurrGenotype] = theGenoType;
            }
        }

        /// <summary>
        /// Add a new genotype to the list on the form
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event arguments</param>
        private void btnNewGeno_Clicked(object sender, EventArgs e)
        {
            GrazType.AnimalType newAnimal;
            int iNewBreed;
            string sNewBreed = "";
            bool bFound;
            int Idx;

            if (genotypeInits.Length < MAX_GENOTYPES)
            {
                ParseCurrGenotype();

                newAnimal = GrazType.AnimalType.Sheep;
                iNewBreed = 0;
                bFound = false;
                while ((iNewBreed < paramSet.iBreedCount(newAnimal)) && !bFound)
                {
                    sNewBreed = paramSet.sBreedName(GrazType.AnimalType.Sheep, iNewBreed);

                    bFound = true;
                    for (Idx = 0; Idx < genotypeInits.Length; Idx++)
                    {
                        bFound = (bFound && (sNewBreed.ToLower() != genotypeInits[Idx].Name.ToLower()));
                    }
                    if (!bFound && (newAnimal == GrazType.AnimalType.Sheep) && (iNewBreed == paramSet.iBreedCount(newAnimal)))
                    {
                        newAnimal = GrazType.AnimalType.Cattle;
                        iNewBreed = 0;
                    }
                    else
                        iNewBreed++;
                }

                if (!bFound)
                {
                    MessageDialog msgError = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, "Error adding more genotypes");
                    msgError.Title = "Error";
                    msgError.Run();
                    msgError.Destroy();
                }
                else
                {
                    Array.Resize(ref genotypeInits, genotypeInits.Length + 1);
                    genotypeInits[genotypeInits.Length - 1] = new StockGeno();
                    genotypeInits[genotypeInits.Length - 1].Conception = new double[4];
                }

                SetGenotypeDefaults(genotypeInits.Length - 1, sNewBreed);
                genoList.AppendValues(sNewBreed);
                SelectedGenoIndex = genotypeInits.Length - 1;
                ClickGenotypeList(null, null);

                EnableButtons();
            }
        }

        /// <summary>
        /// When an item is clicked in the genotype list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickGenotypeList(object sender, EventArgs e)
        {
            if (FCurrGenotype >= 0 && !FILLING)
                ParseCurrGenotype();
            FCurrGenotype = SelectedGenoIndex;
            FillCurrGenotype(); 
        }

        /// <summary>
        /// Enable the add/del buttons
        /// </summary>
        private void EnableButtons()
        {
            btnNewGeno.Sensitive = (genotypeInits.Length < MAX_GENOTYPES);
            btnDelGeno.Sensitive = (genotypeInits.Length > 0);
        }

        /// <summary>
        /// Respond to the changing of the breed using the combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeBreed(object sender, EventArgs e)
        {
            if (!FILLING)
            {
                string newGenoName = MakeUniqueGenoName(cbxDamBreed.SelectedValue);
                SetGenotypeDefaults(FCurrGenotype, cbxDamBreed.SelectedValue);
                FillCurrGenotype();
                edtGenotypeName.Text = newGenoName;
                //ChangeGenotypeName(sender, e);          // ensure trigger updates on the Animals tab also
                FILLING = true;            SetItem(genoList, FCurrGenotype, newGenoName); FILLING = false;
            }
        }

        /// <summary>
        /// Make adjustments when the genotype name is edited by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGenotypeName(object sender, EventArgs e)
        {
            if (!FILLING)
            {
                SetItem(genoList, FCurrGenotype, edtGenotypeName.Text);
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

        private double RoundValue(double fX, double fScale)
        {
            return fScale * Math.Round(fX / fScale);
        }

        /// <summary>
        /// Store the settings for the breed name in the list of genotypes that have been defined.
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="sBreedName"></param>
        private void SetGenotypeDefaults(int idx, string sBreedName)
        {
            AnimalParamSet breedParams;
            StockGeno theGenoType;

            breedParams = paramSet.Match(sBreedName);
            if (breedParams != null)
            {
                theGenoType = genotypeInits[idx];

                FGenotypeAnimals[idx] = breedParams.Animal;

                theGenoType.Name = breedParams.sName;
                theGenoType.DamBreed = string.Empty;
                theGenoType.SireBreed = string.Empty;
                theGenoType.Generation = 0;
                theGenoType.SRW = breedParams.BreedSRW;
                theGenoType.DeathRate = RoundValue(breedParams.AnnualDeaths(false), 0.001);
                theGenoType.WnrDeathRate = RoundValue(breedParams.AnnualDeaths(true), 0.001);
                theGenoType.Conception[1] = RoundValue(breedParams.Conceptions[1], 0.01);
                theGenoType.Conception[2] = RoundValue(breedParams.Conceptions[2], 0.01);

                if (breedParams.Animal == GrazType.AnimalType.Sheep)
                {
                    theGenoType.Conception[3] = RoundValue(breedParams.Conceptions[3], 0.01);
                    theGenoType.RefFleeceWt = breedParams.PotentialGFW;
                    theGenoType.MaxFibreDiam = breedParams.MaxMicrons;
                    theGenoType.FleeceYield = breedParams.FleeceYield;
                    theGenoType.PeakMilk = 0.0;
                }
                else if (breedParams.Animal == GrazType.AnimalType.Cattle)
                {
                    theGenoType.PeakMilk = breedParams.PotMilkYield;
                    theGenoType.Conception[3] = 0.0;
                    theGenoType.RefFleeceWt = 0.0;
                    theGenoType.MaxFibreDiam = 0.0;
                    theGenoType.FleeceYield = 0.0;
                }
            }
        }

        /// <summary>
        /// Selected index for the treeview
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
        /// Sets the value of TheAnimal, then initialises the breed list boxes and 
        /// sets up visibility of some animal-specific genotypic parameters. 
        /// Changes naming schemes on the Animals tab to suit sheep or cattle. 
        /// Lastly, clears the list of animal groups. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickAnimal(object sender, EventArgs e)
        {
            GrazType.AnimalType currAnimal;

            if (rbtnSheep.Active)
                currAnimal = GrazType.AnimalType.Sheep;
            else
                currAnimal = GrazType.AnimalType.Cattle;

            FGenotypeAnimals[FCurrGenotype] = currAnimal;

            List<string> names = new List<string>();

            int count = this.paramSet.iBreedCount(currAnimal);
            string[] namesArray = new string[count];
            for (int i = 0; i < count; i++)
            {
                namesArray[i] = paramSet.sBreedName(currAnimal, i);
            }

            cbxDamBreed.Changed -= ChangeBreed;

            cbxDamBreed.Values = namesArray;
            cbxDamBreed.SelectedIndex = 0;
            cbxSireBreed.Values = namesArray;
            cbxSireBreed.SelectedIndex = 0;

            cbxDamBreed.Changed += ChangeBreed;

            ChangeBreed(null, null);            //Force default SRW values etc 
        }

        /// <summary>
        /// Delete the currently selected genotype from genotypeInits[]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDelGeno_Clicked(object sender, EventArgs e)
        {
            if (FCurrGenotype >= 0)
            {
                //TODO when animals tab is working: deleteGroupsWithGenotype(FCurrGenotype);

                for (int idx = FCurrGenotype + 1; idx <= genotypeInits.Length - 1; idx++)
                        genotypeInits[idx - 1] = genotypeInits[idx];
                Array.Resize(ref genotypeInits, genotypeInits.Length - 1);

                int current = SelectedGenoIndex;
                // repopulate the view
                SetValues();
                SelectedGenoIndex = Math.Min(current, genotypeInits.Length - 1);
                EnableButtons();
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            // detach events
            btnNewGeno.Clicked -= btnNewGeno_Clicked;
            btnDelGeno.Clicked -= btnDelGeno_Clicked;
            edtGenotypeName.Changed -= ChangeGenotypeName;
            rbtnSheep.Clicked -= ClickAnimal;
            rbtnCattle.Clicked -= ClickAnimal;
            cbxDamBreed.Changed -= ChangeBreed;
            cbxGeneration.Changed -= ChangeGeneration;
            notebook1.SwitchPage -= TabControl1_SelectedIndexChanged;
        }

        private void TabControl1_SelectedIndexChanged(object sender, SwitchPageArgs e)
        {
            switch (e.PageNum)
            {
                case 0:

                    break;
            }
        }

        private void lbxGenotypeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClickGenotypeList(sender, e);
        }
    }
}

