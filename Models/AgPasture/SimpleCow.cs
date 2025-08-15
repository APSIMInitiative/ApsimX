using System;
using Models.Core;
using APSIM.Shared.Utilities;
using Newtonsoft.Json;
using APSIM.Core;

namespace Models.AgPasture;

/// <summary>
/// A simple cow model.
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

    [Description("Stocking rate based on effective hectage (cows/ha)")]
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


    /// <summary></summary>
    [JsonIgnore] public double SilageMade { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double SilageFed { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double SilageNFed { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double SilageMEFed { get; set; }

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
    [Description("Milk solids production as a percentage of cow live weight")]
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
    [Description("Cow walking distance - assumes flat land, add 50% for rolling land (km /day)")]
    [Units("km /day")]
    public double CowWalkingDist { get; set; }

    /// <summary></summary>
    [Separator("Where does the intake N go? Specify the proportion of N intake going to various destinations when milking and dry")]

    [Description("Percent of intake N to production, liveweight and lanes etc. when the cow is lactating and dry")]
    [Units("kgN/100kgN")]
    public double[] CowN2Exported { get; set; } // = { 30.0, 10.0 };

    /// <summary></summary>
    [Description("Percent of intake N to urine when the cow is lactating and dry")]
    [Units("kgN/100kgN")]
    public double[] CowN2UrinePerc { get; set; } // = { 42.0, 54.0 };

    /// <summary></summary>
    [Description("Percent of intake N to dung when the cow is lactating and dry")]
    [Units("kgN/100kgN")]
    public double[] CowN2DungPerc { get; set; } // = { 28.0, 36.0 };

    /// <summary></summary>
    /// <summary></summary>
    [JsonIgnore] public double[] CowN2BodyPerc { get; set; } = { 0.0, 0.0 }; //just use for checking later on would be { 30.0, 10.0 } with above values

    /// <summary></summary>
    [JsonIgnore] public DateTime CowDateInCalf{ get; set; }  // calculated from CowDateCalving and DaysFromCalvingToInCalf
    /// <summary></summary>
    [JsonIgnore] public DateTime CowDateDryOff{ get; set; }  // calculated from CowDateCalving and LactationDuration
    /// <summary></summary>
    [JsonIgnore] public double[] LactationCurveParam = { 20.0, 0.2, -0.04, 0.0 };         // will calculate the last value at Init based on CowBodyWeight and MilkSolidsAsPercentOfCowBodyWeight
    /// <summary></summary>
    [JsonIgnore] public double CowMSEnergyPerKg{ get; set; }  = 80.0;         // MJME/kg MS
    /// <summary></summary>
    [JsonIgnore] public double CowWalkingEnergyPerKm = 2.0;         // MJME/km
    /// <summary></summary>
    [JsonIgnore] public double CalfBirthWeight { get; set; }   // birthweight - calulated as a percentage of liveweight at Init

    /// <summary></summary>
    [JsonIgnore] public double CowMEDemand { get; set; } // ME required for maintenance
    /// <summary></summary>
    [JsonIgnore] public double CowMaintME { get; set; } // ME required for maintenance
    /// <summary></summary>
    [JsonIgnore] public string CowState { get; set; }
    [JsonIgnore] private double WeeksBeforeCalving { get; set; }
    [JsonIgnore] private double LactationWeek { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double CowMSPerDay { get; set; } = 0.0;         // kgMS/day/head - calculated, initialising here
    /// <summary></summary>
    [JsonIgnore] public double[] CowPregnancyParam { get; set; } = { 0.0, 0.0 }; // multiplier and exponential parameters for pregnancy energy - values calculated at Init


// Herd characteristics
    /// <summary></summary>
    [JsonIgnore] public double StockingDensity { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdMEDemand { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdNIntake { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdDMIntake { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdDungNReturned { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdDungWtReturned { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdUrineNReturned { get; set; }
    /// <summary></summary>
    [JsonIgnore] public double HerdNumUrinations { get; set; }


    [EventSubscribe("StartOfSimulation")]
    private void OnStartOfSimulation(object sender, EventArgs e)
    {
        if (100.0 - CowN2Exported[0] - CowN2UrinePerc[0] - CowN2DungPerc[0] > 0.1)
            throw new Exception("Disposition of N intake when the cow is milking must add up to 100");

        if (CowN2Exported[1] + CowN2UrinePerc[1] + CowN2DungPerc[1] != 100.0)
            throw new Exception("Disposition of N intake when the cow is dry must add up to 100");

        if (Structure.Find<SimpleGrazing>(relativeTo: this) == null)
            throw new Exception("SimpleCow needs SimpleGrazing. Please add it to your simulation");

        DateTime tempdate = DateUtilities.GetDate(CowDateCalving, clock.Today.Year);
        CowDateInCalf = tempdate.AddDays(DaysFromCalvingToInCalf);
        CowDateDryOff = tempdate.AddDays(LactationDuration);

        CalfBirthWeight = CowBodyWeight * CalfBirthWeightPercent / 100.0;

        // MJME/day https://www.dairynz.co.nz/media/5789573/facts_and_figures_web_chapter4_cow_feed_requirements.pdf page 4 and Excel regression on table "Maintenance MJ ME/day"
        CowMaintME = CowBodyWeight * 0.0942 + 11.507;

			CowState = "Dry-Pregnant";  // initial state - make sure this updated on day 1

			// First three terms give the target milk solids production in kg MS /cow /season
			// Last value obtained by fitting the final term of the Woods curve to various target milk solids production values - fitted value
			LactationCurveParam[3] = CowBodyWeight * MilkSolidsAsPercentOfCowBodyWeight / 100.0 / 4542.2;

        //Paraemter values fitted from (DairyNZ, 2017, p. 49) with the equation fitted to the values in the table.
        CowPregnancyParam[0] = 1.35 * CalfBirthWeight + 22.41;
        CowPregnancyParam[1] = -0.14;

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
        HerdDMIntake = 0.0;
        HerdNIntake = 0.0;
        HerdMEDemand = 0.0;
        CowMEDemand = 0.0;
        HerdUrineNReturned = 0.0;
        HerdDungNReturned = 0.0;
        HerdDungWtReturned = 0.0;
        //UrineNToSoil = 0.0;
        //DungNToSoil = 0.0;
        CowMSPerDay = 0.0;
        WeeksBeforeCalving = 0.0;
        LactationWeek = 0.0;

        // just were are the cows at right now?
        CowPhysiologicalState();
    }

    /// <summary>
    /// Called by SimpleGrazing component.
    /// </summary>
    /// <param name="grazedDM">The amount of grazed dry matter (kg/ha)</param>
    /// <param name="grazedME">The amount of grazed metabolisable energy (MJ ME/ha)</param>
    /// <param name="grazedN">The amount of grazed nitrogen (kgN/ha)</param>
    public UrineDungReturn.UrineDung OnGrazed(double grazedDM, double grazedME, double grazedN)
    {
		double grazedMEConc = grazedDM / grazedME;  // CHECK!!!!

        // now do the ME and N demand
        CowEnergyAndNDemand();

        // could replace this with looking at milk and pregnancy
        // calculate the stocking density and herd ME demand
        if (CowState == "Dry-Pregnant")
            StockingDensity = DaysPerGrazeWhenDry * Num1HaPaddocks * StockingRate;
        else
            StockingDensity = DaysPerGrazeWhenMilking * Num1HaPaddocks * StockingRate;


        HerdMEDemand = CowMEDemand * StockingDensity;  // in SimpleCow the Demand is also the Intake - not that this is ME /ha (not per cow)

        // compare the ME and N removed from SimpleGrazing against herd demand
        if (grazedME < 0.95 * HerdMEDemand)  // there is an energy deficit so add silage
        {
            SilageFed = (HerdMEDemand - grazedME) / SilageMEConc;
            HerdNIntake = grazedN + SilageFed * SilageNConc / 100.0;
        }
        else if (grazedME > 1.05 * HerdMEDemand)  // there is an energy excess and put the DM associated with that into silage
        {
            SilageMade = (grazedME - HerdMEDemand) / grazedMEConc / 100.0;
            HerdNIntake = grazedN * (1.0 - SilageFed / grazedDM);
        }
        else  // the unlikely event that there is a great match between pasture available and herd demand
            HerdNIntake = grazedN;

        HerdDMIntake = grazedDM + SilageFed - SilageMade;

        // N considerations - urine N info to be sent to SimplePatches
        int Index;
        if (CowState == "Dry-Pregnant")   // change to a test on MS production today
            Index = 1;
        else
            Index = 0;

        HerdUrineNReturned = (grazedN + SilageFed * SilageNConc / 100.0) * CowN2UrinePerc[Index] / 100.0;
        HerdDungNReturned = (grazedN + SilageFed * SilageNConc / 100.0) * CowN2DungPerc[Index] / 100.0;
        return new UrineDungReturn.UrineDung()
        {
            UrineNToSoil = HerdUrineNReturned,
            DungNToSoil = HerdDungNReturned
        };
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
