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
        private int FCurrGenotype;
        private GrazType.AnimalType[] FGenotypeAnimals = new GrazType.AnimalType[20];
        private TAnimalParamSet paramSet;
        private TStockGeno[] genoTypeInits;

        private Notebook notebook1 = null;
        private Notebook notebook2 = null;
        // genotypes tab
        private Gtk.TreeView tvGenotype = null;
        private Button btnNewGeno = null;
        private Button btnDelGeno = null;
        private DropDownView cbxDamBreed = null;
        private DropDownView cbxSireBreed = null;
        private RadioButton rbtnSheep = null;
        private RadioButton rbtnCattle = null;
        /// <summary>
        /// The list of genotypes in the treeview
        /// </summary>
        private ListStore genoList = new ListStore(typeof(string));

        // animals tab
        private DropDownView cbxGroupGenotype = null;

        public StockView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.StockView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            notebook1.SwitchPage += TabControl1_SelectedIndexChanged;
            notebook2 = (Notebook)builder.GetObject("notebook2");
            // genotypes tab
            tvGenotype = (Gtk.TreeView)builder.GetObject("tvGenotypes");
            tvGenotype.Model = genoList;
            btnNewGeno = (Button)builder.GetObject("btnNewGeno");
            btnDelGeno = (Button)builder.GetObject("btnDelGeno");
            btnNewGeno.Clicked += btnNewGeno_Clicked;
            btnDelGeno.Clicked += btnDelGeno_Clicked;
            cbxDamBreed = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxDamBreed"));
            cbxSireBreed = new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxSireBreed"));
            rbtnSheep = (Gtk.RadioButton)builder.GetObject("rbtnSheep");
            rbtnCattle = (Gtk.RadioButton)builder.GetObject("rbtnCattle");

            rbtnSheep.Clicked += ClickAnimal;
            rbtnCattle.Clicked += ClickAnimal;

            cbxGroupGenotype = (new Views.DropDownView(this, (ComboBox)builder.GetObject("cbxGroupGenotype")));

            _mainWidget = notebook1;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        public TStockGeno[] Genotypes
        {
            get {
                return genoTypeInits;
            }
            set {
                genoTypeInits = value;
            }
        }

        /// <summary>
        /// Set up the form controls with the intial values from the model
        /// </summary>
        public void SetValues()
        {
            /*            
              tempParams: TAnimalParamSet;
              bWeaner: Boolean;
            */

            this.paramSet = StockList.MakeParamSet("");   // can use the param filename from component inits

            string[] genoNames = new string[genoTypeInits.Length];
            for (int i = 0; i < genoTypeInits.Length; i++)
                genoNames[i] = genoTypeInits[i].Name;

            genoList.Clear();
            genoList.AppendValues(genoNames);

            cbxGroupGenotype.Values = genoNames;

            for (int idx = 0; idx < genoTypeInits.Length; idx++)
            {
                /*   tempParams := ParamsFromGenotypeInits(ParamSet, FValues.Genotypes, Idx);
                   if (tempParams <> NIL) then
                   begin
                     FGenotypeAnimals[Idx] := tempParams.Animal;

                     FValues.Genotypes[Idx].SRW := tempParams.BreedSRW;
                     { Read back any default-handling that }
                     FValues.Genotypes[Idx].PotFleeceWt := tempParams.PotentialGFW;
                     { was done while creating tempParams }
                     FValues.Genotypes[Idx].MaxFibreDiam := tempParams.MaxMicrons;
                     FValues.Genotypes[Idx].FleeceYield := tempParams.FleeceYield;
                     FValues.Genotypes[Idx].PeakMilk := tempParams.PotMilkYield;
                     for bWeaner := False to True do
                       FValues.Genotypes[Idx].DeathRate[bWeaner] :=
                         tempParams.AnnualDeaths[bWeaner];
                     FValues.Genotypes[Idx].Conceptions := tempParams.Conceptions;
                   end
                   else
                     FGenotypeAnimals[Idx] := Sheep;
                   tempParams.Free;
                 */
            }
            FCurrGenotype = 0; // Math.Min(0, genoTypeInits.Length - 1); 
            FillCurrGenotype();
            /*
            lbxGenotypeList.ItemIndex := FCurrGenotype;

            lbxAnimalList.Clear; { Populate the animal groups list box }
            for Idx := 0 to FValues.NoGroups - 1 do
              lbxAnimalList.Items.Add(GroupText(Idx));

            //setup all the enterprises
            while pcEnterprises.PageCount > 1 do
              pcEnterprises.Pages[pcEnterprises.PageCount - 1].Free;

            Panel1.Visible := False;
            pcEnterprises.Pages[0].TabVisible := False;
            if FValues.Enterprises <> nil then
            begin
              for i := 0 to FValues.Enterprises.Count - 1 do
                addTabPage(FValues.Enterprises.byIndex(i).name);
              pcEnterprises.ActivePageIndex := 0;
            end;

            FCurrGroup := Min(0, FValues.NoGroups - 1);
            { Decide on and display the initial value }
            FillCurrGroup; { of the currently selected animal group }
            lbxAnimalList.ItemIndex := FCurrGroup;

            enableButtons;

                      */
        }

        private void FillCurrGenotype()
        {

            GrazType.AnimalType theAnimal;
            SingleGenotypeInits theGenoType;
            /*
            if (FCurrGenotype < 0)
                gbxGenotype.Hide;
            else
                gbxGenotype.Show;

            FILLING:= True;

            theGenoType:= FValues.Genotypes[FCurrGenotype]; */
            theAnimal = FGenotypeAnimals[FCurrGenotype];
            /*
            if (theAnimal = GrazType.Sheep) then
            begin
      edtBreedSRW.maxValue := MAXSHEEPSRW;
            edtBreedSRW.minValue := MINSHEEPSRW;
            end
    else if (theAnimal = GrazType.Cattle) then
    begin
      edtBreedSRW.maxValue := MAXCATTLESRW;
            edtBreedSRW.minValue := MINCATTLESRW;
            end;

            lblConception3.Visible := (theAnimal = Sheep);
            { Visibility of animal - specific parameters }
            edtConception3.Visible := (theAnimal = Sheep);
            unitConception.Visible := (theAnimal = Sheep);
            pnlWool.Visible := (theAnimal = GrazType.Sheep);
            pnlMilk.Visible := (theAnimal = GrazType.Cattle);

            edtGenotypeName.Text := theGenoType.sGenotypeName; */
            rbtnSheep.Active = (theAnimal == GrazType.AnimalType.Sheep);
            rbtnCattle.Active = (theAnimal == GrazType.AnimalType.Cattle);
            /*cbxGeneration.ItemIndex :=
              Max(0, Min(theGenoType.iGeneration, cbxGeneration.Items.Count - 1));
            ChangeGeneration(NIL);

            if (theGenoType.iGeneration = 0) and(theGenoType.sDamBreed = '') then
            //sDamBreed
      cbxDamBreed.ItemIndex := cbxDamBreed.Items.IndexOf
        (theGenoType.sGenotypeName)
            else
      cbxDamBreed.ItemIndex := cbxDamBreed.Items.IndexOf(theGenoType.sDamBreed);
            cbxSireBreed.ItemIndex := cbxSireBreed.Items.IndexOf
              (theGenoType.sSireBreed);
            if (cbxSireBreed.ItemIndex < 0) then
              cbxSireBreed.ItemIndex := cbxDamBreed.ItemIndex;

            edtBreedSRW.Value := theGenoType.SRW;
            edtDeathRate.Value := theGenoType.DeathRate[False];
            edtWnrDeathRate.Value := theGenoType.DeathRate[True];
            edtConception1.Value := theGenoType.Conceptions[1];
            edtConception2.Value := theGenoType.Conceptions[2];

            if (theAnimal = GrazType.Sheep) then
            begin
              edtConception3.Value := theGenoType.Conceptions[3];
            edtBreedPFW.Value := theGenoType.PotFleeceWt;
            edtBreedMaxMu.Value := theGenoType.MaxFibreDiam;
            edtWoolYield.Value := theGenoType.FleeceYield;
            end
            else if (theAnimal = GrazType.Cattle) then
              edtPeakMilk.minValue := Min(10.0, edtBreedSRW.Value * 0.01);
            edtPeakMilk.Value := theGenoType.PeakMilk;

            FILLING:= False;
            */

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

            //if (currAnimal <> FGenotypeAnimals[FCurrGenotype]) then
              //  deleteGroupsWithGenotype(FCurrGenotype);

            FGenotypeAnimals[FCurrGenotype] = currAnimal;

            List<string> names = new List<string>();
            
            int count = this.paramSet.iBreedCount(currAnimal);
            string[] namesArray = new string[count];
            for (int i = 0; i < count; i++)
            {
                namesArray[i] = paramSet.sBreedName(currAnimal, i);
            }
            cbxDamBreed.Values = namesArray;
            cbxDamBreed.SelectedIndex = 0; 
            cbxSireBreed.Values = namesArray;
            cbxSireBreed.SelectedIndex = 0;

            //ChangeBreed(NIL);            //Force default SRW values etc 
        }

        private void btnNewGeno_Clicked(object sender, EventArgs e)
        {

        }

        private void btnDelGeno_Clicked(object sender, EventArgs e)
        {

        }

        public string[] GenotypeNames
        {
            get;
            set;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            // detach events
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
    }
}
