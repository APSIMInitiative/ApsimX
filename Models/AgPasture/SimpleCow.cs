using System;
using Models.Core;
using APSIM.Shared.Utilities;
using Newtonsoft.Json;
using APSIM.Core;

namespace Models.AgPasture;

/// <summary>
/// A simple cow intake and N partitioning model.
/// </summary>
[Serializable]
[ViewName("UserInterface.Views.PropertyView")]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ValidParent(ParentType = typeof(Zone))]
[ValidParent(ParentType = typeof(Simulation))]
public class SimpleCow : Model, IStructureDependency
{
    /// <summary>Structure instance supplied by APSIM.core.</summary>
    [field: NonSerialized]
    public IStructure Structure { private get; set; }


    [Link] IClock clock = null;

    // Farm context

    /// <summary></summary>
    [Separator("Farm context")]

    [Description("Stocking rate based on effective hectarage (cows/ha)")]
    [Units("cows/ha")]
    public double StockingRate { get; set; }

    /// <summary></summary>
    [Description("Number of paddocks on the farm (sets the stocking density)")]
    [Units("-")]
    public int Num1HaPaddocks { get; set; }

    /// <summary></summary>
    [Description("Number of days over which a grazing event takes place when the cows are milking")]
    [Units("days")]
    public int DaysPerGrazeWhenMilking{ get; set; }

    /// <summary></summary>
    [Description("Number of days over which a grazing event takes place when the cows are dry")]
    [Units("days")]
    public int DaysPerGrazeWhenDry{ get; set; }

    /// <summary></summary>
    [Description("N concentration of the supplements fed out")]
    [Units("%N in DM")]
    public double SilageNConc { get; set; }

    /// <summary></summary>
    [Description("ME concentration of the supplements fed out")]
    [Units("MJME/kgDM")]
    public double SilageMEConc { get; set; }


    // Cow characteristics
    /// <summary></summary>
    [Separator("Cow characteristics")]

    [Description("Cow mature live weight (kg)")]
    [Units("kg")]
    public double CowBodyWeight  { get; set; }

    /// <summary></summary>
    [Description("Calf birth weight as a percentage of cow liveweight - try 7% (%)")]
    [Units("%")]
    public double CalfBirthWeightPercent  { get; set; }

    /// <summary></summary>
    [Description("Season milk solids production as a percentage of cow live weight")]
    [Units("%")]
    public double MilkSolidsAsPercentOfCowBodyWeight  { get; set; }

    /// <summary></summary>
    [Description("Calving date (dd-mmm)")]
    [Units("dd-mmm")]
    public string CowDateCalving { get; set; }

    /// <summary></summary>
    [Description("Days from calving to in-calf")]
    [Units("days")]
    public int DaysFromCalvingToInCalf  { get; set; }

    /// <summary></summary>
    [Description("Lactation duration")]
    [Units("days")]
    public int LactationDuration  { get; set; }

    /// <summary></summary>
    [Description("Cow walking distance - assumes flat land, user should add 50% for rolling land (km /day)")]
    [Units("km /day")]
    public double CowWalkingDist { get; set; }


    /// <summary></summary>
    [Description("Number of urinations per cow per day")]
    [Units("Urinations /cow /day")]
    public double CowNumUrinations { get; set; }





    // calculated variables related to the cow

    /// <summary>Date that the cows get in-calf</summary>
    //[Description("In-calf date (dd-mmm-yyyy)")]
    [Units("dd-mmm-yyyy")]
    [JsonIgnore] public DateTime CowDateInCalf{ get; set; }  // calculated from CowDateCalving and DaysFromCalvingToInCalf

    /// <summary></summary>
    //[Description("Date cows dried off (dd-mmm-yyyy)")]
    [Units("dd-mmm-yyyy")]
    [JsonIgnore] public DateTime CowDateDryOff{ get; set; }  // calculated from CowDateCalving and LactationDuration


    // Internal parameters

    /// <summary>Lactation curve parameters from Woods</summary>
    //[Description("Lactation curve parameters")]
    [Units("-")]
    [JsonIgnore] public double[] LactationCurveParam = { 20.0, 0.2, -0.04, 0.0 };         // will calculate the last value at Init based on CowBodyWeight and MilkSolidsAsPercentOfCowBodyWeight

    /// <summary>Energy contained in a kg of milk solids</summary>
    //[Description("Milk solids energy content")]
    [Units("MJ ME / kg MS")]
    [JsonIgnore] public double CowMSEnergyPerKg{ get; set; }  = 80.0;         // MJME/kg MS

    /// <summary>Energy required per kilometer of walking</summary>
    //[Description("Energy to walk one km")]
    [Units("MJ ME / km")]
    [JsonIgnore] public double CowWalkingEnergyPerKm = 2.0;         // MJME/km

    /// <summary>Birth weight of the calf</summary>
    //[Description("Calf birth weight")]
    [Units("kg")]
    [JsonIgnore] public double CalfBirthWeight { get; set; }   // birthweight - calulated as a percentage of liveweight at Init

    /// <summary>Cow energy demand (all purposes)</summary>
    //[Description("Cow energy demand")]
    [Units("MJ ME /cow /day")]
    [JsonIgnore] public double CowMEDemand { get; set; } // ME required for maintenance

    /// <summary>Cow energy demand for maintenance</summary>
    //[Description("Cow energy demand for maintenance")]
    [Units("MJ ME /cow /day")]
    [JsonIgnore] public double CowMaintME { get; set; } // ME required for maintenance

    /// <summary>Physiological state of the cow</summary>
    //[Description("Physiological state of the cow")]
    [Units("-")]
    [JsonIgnore] public string CowState { get; set; }

    /// <summary>Weeks before calving</summary>
    //[Description("Weeks before calving")]
    [Units("weeks")]
    [JsonIgnore] public double WeeksBeforeCalving { get; set; }

    /// <summary>Weeks since the start of lactation</summary>
    //[Description("Weeks since the start of lactation")]
    [Units("weeks")]
    [JsonIgnore] public double LactationWeek { get; set; }

    /// <summary>Milk solids production</summary>
    //[Description("Milk solids production")]
    [Units("kg MS /cow /day")]
    [JsonIgnore] public double CowMSPerDay { get; set; } = 0.0;         // kgMS/day/head - calculated, initialising here

    /// <summary>Cow dry matter intake</summary>
    [Units("kg DM /cow /day")]
    [JsonIgnore] public double CowDMIntake { get; set; }

    /// <summary>Cow dry matter intake from pasture</summary>
    [Units("kg DM /cow /day")]
    [JsonIgnore] public double CowPastureIntake { get; set; }

    /// <summary>Cow dry matter intake from silage</summary>
    [Units("kg DM /cow /day")]
    [JsonIgnore] public double CowSilageIntake { get; set; }

    /// <summary>Cow N intake</summary>
    [Units("kg N /cow /day")]
    [JsonIgnore] public double CowNIntake { get; set; }

    /// <summary>Parameters used in the calculation of pregnancy energy demand</summary>
    //[Description("Energy for pregnancy parameters")]
    [Units("-")]
    [JsonIgnore] public double[] CowPregnancyParam { get; set; } = { 0.0, 0.0 }; // multiplier and exponential parameters for pregnancy energy - values calculated at Init

    // Herd characteristics

    /// <summary>Herd stocking density</summary>
    //[Description("Herd stocking density")]
    [Units("head /ha /day")]
    [JsonIgnore] public double StockingDensity { get; set; }

    /// <summary>Energy demand of the herd</summary>
    //[Description("Herd energy demand")]
    [Units("MJ ME /ha /day")]
    [JsonIgnore] public double HerdMEDemand { get; set; }

    /// <summary></summary>
    //[Description("Herd N intake")]
    [Units("kg N /ha /day")]
    [JsonIgnore] public double HerdNIntake { get; set; }

    /// <summary>Herd dry matter intake</summary>
    //[Description("Herd dry matter intake")]
    [Units("kg DM /ha /day")]
    [JsonIgnore] public double HerdDMIntake { get; set; }

    /// <summary>Herd metabolisable energy intake</summary>
    //[Description("Herd ME intake")]
    [Units("MJ ME /ha /day")]
    [JsonIgnore] public double HerdMEConcIntake { get; set; }

    /// <summary>Digestibility of the herd intake</summary>
    //[Description("Digestibility of the herd intake")]
    [Units("kg DM / kg DM")]
    [JsonIgnore] public double HerdDigesitbilityIntake { get; set; }

    /// <summary>Herd N retuned to pasture in dung</summary>
    //[Description("Herd N retuned to pasture in dung")]
    [Units("kg N /ha /day")]
    [JsonIgnore] public double HerdDungNReturned { get; set; }

    /// <summary>Herd weight of dung returned to pasture</summary>
    //[Description("Herd weight of dung returned to pasture")]
    [Units("kg DM /ha /day")]
    [JsonIgnore] public double HerdDungWtReturned { get; set; }

    /// <summary>Herd N returned to pasture in urine</summary>
    //[Description("Herd N returned to pasture in urine")]
    [Units("kg N /ha /day")]
    [JsonIgnore] public double HerdUrineNReturned { get; set; }

    /// <summary>Herd N partitioned to milk</summary>
    //[Description("Herd N partitioned to milk")]
    [Units("kg N /ha /day")]
    [JsonIgnore] public double HerdNToMilk { get; set; }

    /// <summary>Herd N partitioned to pregnancy</summary>
    //[Description("Herd N partitioned to pregnancy")]
    [Units("kg N /ha /day")]
    [JsonIgnore] public double HerdNToPregnancy { get; set; }

    /// <summary>Herd number of urinations Summary</summary>
    //[Description("Herd number of urinations Description")]
    [Units("-")]
    [JsonIgnore] public double HerdNumUrinations { get; set; }




    /// <summary>Amount of silage made on the paddock</summary>
    //[Description("Amount of silage made")]
    [Units("kg DM/ha")]
    [JsonIgnore] public double SilageMade { get; set; }

    /// <summary>Amount of silage fed out to the cows</summary>
    //[Description("Amount of silage fed out")]
    [Units("kg DM/ha")]
    [JsonIgnore] public double SilageFed { get; set; }

    /// <summary>Amount of N in the silage fed out to the cows</summary>
    //[Description("Amount of N in the silage fed out")]
    [Units("kg N/ha")]
    [JsonIgnore] public double SilageNFed { get; set; }

    /// <summary>Metabolisable energy in SilageFed</summary>
    //[Description("Amount of ME in the silage fed out")]
    [Units("MJ ME/ha")]
    [JsonIgnore] public double SilageMEFed { get; set; }

    /// <summary>Digestibility of the silage</summary>
    //[Description("Digestibility of the silage")]
    [Units("kg DM / kg DM")]
    [JsonIgnore] public double SilageDigestibility { get; set; }





    [EventSubscribe("StartOfSimulation")]
    private void OnStartOfSimulation(object sender, EventArgs e)
    {
        if (Structure.Find<SimpleGrazing>(relativeTo: this) == null)
            throw new Exception("SimpleCow needs SimpleGrazing. Please add it to your simulation");

        DateTime tempdate = DateUtilities.GetDate(CowDateCalving, clock.Today.Year);
        CowDateInCalf = tempdate.AddDays(DaysFromCalvingToInCalf);
        CowDateDryOff = tempdate.AddDays(LactationDuration);

        CalfBirthWeight = CowBodyWeight * CalfBirthWeightPercent / 100.0;

        // MJME/day https://www.dairynz.co.nz/media/5789573/facts_and_figures_web_chapter4_cow_feed_requirements.pdf page 4 and Excel regression on table "Maintenance MJ ME/day"
        CowMaintME = CowBodyWeight * 0.0942 + 11.507;

		CowState = "Dry-Pregnant";  // initial state - make sure this updated on day 1

		// First three terms (define above) give the target milk solids production in kg MS /cow /season
		// Last value (here, below) obtained by fitting the final term of the Woods curve to various target milk solids production values - fitted value
		LactationCurveParam[3] = CowBodyWeight * MilkSolidsAsPercentOfCowBodyWeight / 100.0 / 4542.2;

        // Parameter values fitted from (DairyNZ, 2017, p. 49) with the equation fitted to the values in the table.
        CowPregnancyParam[0] = 1.35 * CalfBirthWeight + 22.41;
        CowPregnancyParam[1] = -0.14;

        SilageDigestibility = SilageMEConc / 16;
    }

    /// <summary>
    /// Perform daily calculations
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    [EventSubscribe("DoDailyInitialisation")]
    private void OnDoDailyInitialisation(object sender, EventArgs e)
    {
        // zero out the supplementary feeding data etc from yesterday - only really need to do this the day after a grazing
        SilageMade = 0.0;

        SilageFed = 0.0;
        SilageNFed = 0.0;
        SilageMEFed = 0.0;
        HerdUrineNReturned = 0.0;

        // zero out the daily intake etc. values - these should only hold non-zero values on a grazing day
        StockingDensity = 0.0;
        HerdMEDemand = 0.0;

        HerdDMIntake = 0.0;
        HerdNIntake = 0.0;
        HerdMEConcIntake = 0.0;
        HerdDigesitbilityIntake = 0.0;

        HerdUrineNReturned = 0.0;
        HerdDungNReturned = 0.0;
        HerdDungWtReturned = 0.0;
        HerdNToPregnancy = 0.0;
        HerdNToMilk = 0.0;

        CowMEDemand = 0.0;
        CowMSPerDay = 0.0;
        CowDMIntake = 0.0;
        CowPastureIntake = 0.0;
        CowSilageIntake = 0.0;
        CowNIntake = 0.0;
        WeeksBeforeCalving = 0.0;
        LactationWeek = 0.0;

        // update the cow physiological state
        CowPhysiologicalState();
    }

    /// <summary>
    /// Called by SimpleGrazing component.
    /// </summary>
    /// <param name="pastureRemovedDM">The amount of grazed dry matter (kg/ha)</param>
    /// <param name="pastureRemovedME">The amount of grazed metabolisable energy (MJ ME/ha)</param>
    /// <param name="pastureRemovedN">The amount of grazed nitrogen (kgN/ha)</param>
    public (double numUrinations, double urineN, double dungN) OnGrazed(double pastureRemovedDM, double pastureRemovedME, double pastureRemovedN)
    {
		double pastureRemovedMEConc = pastureRemovedME / pastureRemovedDM;
        double pastureRemovedDigestibility = pastureRemovedMEConc / 16.0;

        // now do the ME and N demand calculations
        CowEnergyAndNDemand();

        // could replace this with looking at milk and pregnancy
        // calculate the stocking density and herd ME demand
        if (CowState == "Dry-Pregnant")
            StockingDensity = DaysPerGrazeWhenDry * Num1HaPaddocks * StockingRate;
        else
            StockingDensity = DaysPerGrazeWhenMilking * Num1HaPaddocks * StockingRate;

        HerdMEDemand = CowMEDemand * StockingDensity;  // in SimpleCow the Demand is also the Intake - not that this is ME /ha (not per cow)

        double pastureGrazedME = Math.Min(pastureRemovedME, HerdMEDemand);
        double pastureGrazedDM = Math.Min(pastureGrazedME / pastureRemovedME, 1.0) * pastureRemovedDM;
        double pastureGrazedN = Math.Min(pastureGrazedME / pastureRemovedME, 1.0) * pastureRemovedN;

        if ((HerdMEDemand - pastureGrazedME) > 0.05 * HerdMEDemand) // feed/energy shortfall, need to feed out some silage
        {
            SilageMEFed = HerdMEDemand - pastureGrazedME;
            SilageFed = SilageMEFed / SilageMEConc;
            SilageNFed = SilageFed * SilageNConc / 100.0;
        }
        else if ((pastureRemovedME - pastureGrazedME) < 0.05 * HerdMEDemand) // excess feed, need to make some silage
            SilageMade = (pastureRemovedDM - pastureGrazedDM);

        HerdNIntake = pastureGrazedN + SilageNFed;
        HerdDMIntake = pastureGrazedDM + SilageFed;
        HerdMEConcIntake = pastureRemovedMEConc * pastureGrazedDM / HerdDMIntake + SilageMEConc * SilageFed / HerdDMIntake;
        HerdDigesitbilityIntake = pastureRemovedDigestibility * pastureGrazedDM / HerdDMIntake + SilageDigestibility * SilageFed / HerdDMIntake;

        CowDMIntake = HerdDMIntake / StockingDensity;
        CowPastureIntake = pastureGrazedDM / StockingDensity;
        CowSilageIntake = SilageFed / StockingDensity;
        CowNIntake = HerdNIntake / StockingDensity;

        // N to body assume zero for now

        // N to milk based on:
        //          CowMSPerDay calculated 'above'
        //          New Zealand Dairy Statistics 2022-23 page 29 table 4.3 gives 3.94 / (3.94 + 4.90) = 0.445 of MS is protein
        //          N is then protein * 6.25/100
        HerdNToMilk = CowMSPerDay  * 0.445 / 6.25 * StockingDensity;                  // milk solids to protein to N

        // N to pregnancy from ARC 1980
        //      Standard birth weight - 40 kg
        //              A        B       C
        //      Calf    5.358	15.229	0.00538
        //      Uterus  8.536	13.12	0.00262

        double cumPrCalf = CalfBirthWeight / 40.0 * Math.Exp(5.358 - 15.229 * Math.Exp(-1.0 * 0.00538 * (283 - WeeksBeforeCalving * 7.0)));
        double cumPrUterus = CalfBirthWeight / 40.0 * Math.Exp(8.536 - 13.12 * Math.Exp(-1.0 * 0.00262 * (283 - WeeksBeforeCalving * 7.0)));

        double CalfToday = cumPrCalf * (15.229 * 0.00538 * Math.Exp(-1.0 * 0.00538 * (283 - WeeksBeforeCalving * 7.0)));
        double UrterusToday = cumPrUterus * (13.12 * 0.00262 * Math.Exp(-1.0 * 0.00262 * (283 - WeeksBeforeCalving * 7.0)));

        HerdNToPregnancy = (CalfToday + UrterusToday ) / 6.25 * StockingDensity;

        double herdNForExcretion = HerdNIntake                                   // intake N
                                 - HerdNToMilk                                   // milk solids N
                                 - HerdNToPregnancy;                             // subtract the N to foetus and uterus
                                                                                 // ****  if ever do LW changes then also needs adding in

        // N to dung based on digestibility and 2.6% N in dung but maximum 90% of N intake
        double herdDungWt = HerdDMIntake * (1.0 - HerdDigesitbilityIntake);
        HerdDungNReturned = Math.Min(herdDungWt * 0.026, 0.9 * herdNForExcretion);

        // N to urine based on difference
        HerdUrineNReturned = herdNForExcretion - HerdDungNReturned;

        HerdNumUrinations = CowNumUrinations * StockingDensity;
        // for patching will need to figure out (including where/who)
        //      selecting from the binomial distribution - did the difference in mean get sorted out?
        //      sort in order of amount of N
        //      calculate area
        //      accumulate area into a grid's worth
        //      apply and repeat until all urine added

        // ******** check that when patches are enabled that the amount of N being applied is from the model not from the testing monthly amounts

        return (HerdNumUrinations, HerdUrineNReturned, HerdDungNReturned);
    }

    private void CowPhysiologicalState()
    {
        if (DateUtilities.WithinDates(CowDateCalving, clock.Today, CowDateInCalf.ToString("dd-MMM")))
            CowState = "Milking-Notpreg";
        else if (DateUtilities.WithinDates(CowDateInCalf.ToString("dd-MMM"), clock.Today, CowDateDryOff.ToString("dd-MMM")))
            CowState = "Milking-Pregnant";
        else if (DateUtilities.WithinDates(CowDateDryOff.ToString("dd-MMM"), clock.Today, CowDateCalving))
            CowState = "Dry-Pregnant";
        else
            throw new Exception("Error in calculating CowState");


        // where are we right now?
        if (DateUtilities.CompareDates(CowDateCalving, clock.Today) > 0)
            WeeksBeforeCalving = (DateUtilities.GetDate(CowDateCalving, (clock.Today.Year + 1)) - clock.Today).TotalDays / 7.0;
        else
            WeeksBeforeCalving = (DateUtilities.GetDate(CowDateCalving, clock.Today.Year) - clock.Today).TotalDays / 7.0;

        if (DateUtilities.CompareDates(CowDateCalving, clock.Today) <= 0)
            LactationWeek = (clock.Today - DateUtilities.GetDate(CowDateCalving, (clock.Today.Year - 1))).TotalDays / 7.0;
        else
            LactationWeek = (clock.Today - DateUtilities.GetDate(CowDateCalving, (clock.Today.Year))).TotalDays / 7.0;


        if (LactationWeek <= LactationDuration / 7.0)
            CowMSPerDay = LactationCurveParam[0] * Math.Pow(LactationWeek, LactationCurveParam[1]) * Math.Exp(LactationCurveParam[2] * LactationWeek) * LactationCurveParam[3];
        else
        {
            CowMSPerDay = 0.0;
            LactationWeek = -1;   // not lactating
        }
    }

    private void CowEnergyAndNDemand()
    {
        double energyPregnancy = 0.0;
        if (WeeksBeforeCalving <= 40.0)
            energyPregnancy = CowPregnancyParam[0] * Math.Exp(CowPregnancyParam[1] * WeeksBeforeCalving);
        else
        {
            energyPregnancy = 0.0;
            WeeksBeforeCalving = -1;   // not pregnant
        }

        CowMEDemand = CowMaintME
                    + energyPregnancy
                    + CowWalkingDist * CowWalkingEnergyPerKm
                    + CowMSPerDay * CowMSEnergyPerKg;  //=72.154*EXP(-0.143*L3)
    }
}
