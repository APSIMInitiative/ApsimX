using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models;
using Models.Core;
using APSIM.Shared.Utilities;



namespace Models.Aqua
    {


    ///<summary>
    /// Aquaculture Prawns. 
    /// Simple prawn growth model.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Prawns : Model
        {




        #region Links


        ///// <summary>The clock</summary>
        //[Link]
        //private Clock Clock = null;


        [Link]
        private PondWater PondWater = null;

        [Link]
        private FoodInPond FoodInPond = null;


        /// <summary>The summary</summary>
        [Link]
        private ISummary Summary = null;


        #endregion





        #region Module Constants


        /// <summary>
        /// Name of the prawn species
        /// </summary>
        [Description("Name of the prawn species")]
        [Units("")]
        public string PrawnSpecies { get; set; }



        //INTAKE


        /// <summary>
        /// Dry matter intake rate of prawns weighing 1g under optimal conditions
        /// (g DM/d)
        /// </summary>
        [Description("Dry matter intake rate of prawns weighing 1g under optimal conditions")]
        [Units("(g DM/d)")]
        public double Ki1 { get; set; }

        /// <summary>
        /// Allometric exponent for consumption-weight relationship
        /// (unitless)
        /// </summary>
        [Description("Allometric exponent for consumption-weight relationship")]
        [Units("")]
        public double Ki2 { get; set; }

        /// <summary>
        /// Stocking density at which consumption rate notionally declines to zero
        /// (g/m^2)
        /// </summary>
        [Description("Stocking density at which consumption rate notionally declines to zero")]
        [Units("(g/m^2)")]
        public double Ki3 { get; set; }

        /// <summary>
        /// Consumption efficiency of prawns
        /// (0-1)
        /// </summary>
        [Description("Consumption efficiency of prawns")]
        [Units("(0-1)")]
        public double Ki4 { get; set; }



        /// <summary>
        /// DM digestibility: constant 
        /// (g/g)
        /// </summary>
        [Description("DM digestibility: constant")]
        [Units("(g/g)")]
        public double Ki5 { get; set; }

        /// <summary>
        /// DM digestibility: DE/DM coefficient
        /// (g/kJ) nb. DE/DM ratio is same, whether expressed as kJ/g or MJ/kg 
        /// </summary>
        [Description("DM digestibility: DE/DM coefficient")]
        [Units("(g/kJ)")]
        public double Ki6 { get; set; }



        /// <summary>
        /// N digestibility: constant 
        /// (g/g)
        /// </summary>
        [Description("N digestibility: constant")]
        [Units("(g/g)")]
        public double Ki7 { get; set; }

        /// <summary>
        /// N digestibility: DE/DM coefficient
        /// (g/kJ) nb. DE/DM ratio is same, whether expressed as kJ/g or MJ/kg 
        /// </summary>
        [Description("N digestibility: DE/DM coefficient")]
        [Units("(g/kJ)")]
        public double Ki8 { get; set; }




        //TEMPERATURE


        /// <summary>
        /// Optimum temperature for prawn function
        /// (oC)
        /// </summary>
        [Description("Optimum temperature for prawn function")]
        [Units("(oC)")]
        public double Kt1 { get; set; }

        /// <summary>
        /// Temperature below which prawns cease to function
        /// (oC)
        /// </summary>
        [Description("Temperature below which prawns cease to function")]
        [Units("(oC)")]
        public double Kt2 { get; set; }

        /// <summary>
        /// Curvature of temperature response function
        /// (unitless)
        /// </summary>
        [Description("Curvature of temperature response function")]
        [Units("")]
        public double Kt3 { get; set; }




        //GROWTH


        /// <summary>
        /// Maintenance energy requirement in kJ for a 1g prawn.
        /// (kJ/g^0.8)
        /// This value is for a G8 prawn in Glencross et al. 2013;
        /// (for the Jackson and Yang (1998) dataset, the value is 0.19) 
        /// </summary>
        [Description("Maintenance energy requirement in kJ for a 1g prawn.")]
        [Units("(kJ/g^0.8)")]
        public double Km1 { get; set; }

        /// <summary>
        /// Allometric exponent for maintenance energy requirement
        /// (unitless)
        /// </summary>
        [Description("Allometric exponent for maintenance energy requirement")]
        [Units("")]
        public double Km2 { get; set; }

        /// <summary>
        /// Relative rate of change in maintenance energy requirement with salinity
        /// (ppt)
        /// </summary>
        [Description("Relative rate of change in maintenance energy requirement with salinity")]
        [Units("(ppt)")]
        public double Ks1 { get; set; }

        /// <summary>
        /// Efficiency of energy use for growth
        /// (kJ/kJ)
        /// </summary>
        [Description("Efficiency of energy use for growth")]
        [Units("(kJ/kJ)")]
        public double Kg1 { get; set; }

        /// <summary>
        /// Efficiency of nitrogen use for growth
        /// (g/g)
        /// Estimated very approximately by assuming 
        /// a diet below 0.030 g CP/g DM will limit early prawn growth (at a typical DE:DM ratio of 16)
        /// and back calculating the corresponding efficiency.
        /// </summary>
        [Description("Efficiency of nitrogen use for growth")]
        [Units("(g/g)")]
        public double Kg4 { get; set; }



        //DEATH


        /// <summary>
        /// Background mortaility rate of prawns
        /// (/d)
        /// </summary>
        [Description("Background mortaility rate of prawns")]
        [Units("(/d)")]
        public double Kd1 { get; set; }


        /// <summary>
        /// Coefficient for additional mortality
        /// (/d)
        /// </summary>
        [Description("Coefficient for additional mortality")]
        [Units("(/d)")]
        public double Kd2 { get; set; }


        /// <summary>
        /// Threshold salinity above which no extra mortality takes place
        /// (ppt)
        /// </summary>
        [Description("Threshold salinity above which no extra mortality takes place")]
        [Units("(ppt)")]
        public double Kd3 { get; set; }


        /// <summary>
        /// Threshold ammonium-N concentration below which no extra mortality takes place
        /// (mg/litre)
        /// </summary>
        [Description("Threshold ammonium-N concentration below which no extra mortality takes place")]
        [Units("(mg/litre)")]
        public double Kd4 { get; set; }


        /// <summary>
        /// Exponent for ammonium-N vs mortality relationship
        /// (unitless)
        /// </summary>
        [Description("Exponent for ammonium-N vs mortality relationship")]
        [Units("")]
        public double Kd5 { get; set; }



        #endregion





        #region Local Variables

        /// <summary>
        /// Convert from kg to grams
        /// </summary>
        double kg2g = 1000.0;

        /// <summary>
        /// Convert from grams to kg
        /// </summary>
        double g2kg = 0.001; 



        PrawnCohort prawns;


        double stressTemp;
        double stressStock;
        double stressSalinity;


        /// <summary>
        /// Food destroyed during the process of eating
        /// (Prawns are careless and wasteful feeders)
        /// (Per Prawn)
        /// </summary>
        Food consumedFoodPP;

        /// <summary>
        /// Of the Food Consumed this is the food actually ingested by the prawn.
        /// (Per Prawn)
        /// </summary>
        Food intakeFoodPP;

        /// <summary>
        /// Of the food ingested by the prawn this is the food actually digested by the prawn.
        /// (Per Prawn)
        /// </summary>
        Food digestedFoodPP;


        double energyMaintenancePP; 
        double weightGainPP;
        double nitrogenGainPP;


        int deaths;


        Food consumedFood;
        Food intakeFood;
        Food digestedFood;
        Food wastedFood;


        double faecesDM;
        double faecesN;
        double excretedAmmonium;


        Feed deadPrawnsAsFeed;


        #endregion





        #region Prawn Class



        [Serializable]
        private class PrawnCohort
            {

            public int NumberOfPrawns;

            /// <summary>
            /// Live weight of a single prawn
            /// (g/prawn)
            /// </summary>
            public double LiveWeight;


            /// <summary>
            /// Masss of nitrogen in the body of a single prawn
            /// (g N/prawn)
            /// </summary>
            public double NitrogenMass;


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="NumberOfPrawns"></param>
            /// <param name="LiveWeight"></param>
            /// <param name="NitrogenMass"></param>
            public PrawnCohort(int NumberOfPrawns, double LiveWeight, double NitrogenMass)
                {
                this.NumberOfPrawns = NumberOfPrawns;
                this.LiveWeight = LiveWeight;
                this.NitrogenMass = NitrogenMass;
                }


            /// <summary>
            /// Stocking Density
            /// </summary>
            /// <param name="AreaPondFloor">(m^2)</param>
            /// <returns>(g/m^2)</returns>
            public double StockingDensity(double AreaPondFloor)
                {
                return MathUtilities.Divide((this.NumberOfPrawns * this.LiveWeight), AreaPondFloor, 0.0);
                }


            }



        #endregion






        #region Stress Methods


        /// <summary>
        ///  Reduction in Food Consumption due to high stocking density
        ///  aka. Zsd
        /// </summary>
        /// <param name="StockingDensity">(g/m^2)</param>
        /// <returns>(0-1)</returns>
        private double Stress_Stock(double StockingDensity)
            {
            double stress;
            stress = (StockingDensity / Ki3);
            stress = Math.Max(0, 1 - stress);
            return stress;
            }



        /// <summary>
        /// Temperature Response Function
        /// (Prawn Stress due to Temperature)
        /// aka. Zt
        /// </summary>
        /// <param name="TempPondFloor">(oC)</param>
        /// <returns>(0-1)</returns>
        private double Stress_Temp(double TempPondFloor)
            {
            double stress;
            stress = (TempPondFloor - Kt1) / (Kt2 - Kt1);
            stress = Math.Min(1, stress);
            stress = Math.Pow(stress, Kt3);
            stress = Math.Max(0, 1 - stress);
            return stress;
            }



        /// <summary>
        /// Salinity effect on maintenance energy requirement
        /// aka. Zs
        /// </summary>
        /// <param name="Salinity">(ppt)</param>
        /// <returns>(0-1)</returns>
        private double Stress_Salinity(double Salinity)
            {
            double stress;
            double Sref = 30.0; //(ppt) Reference Salinity
            stress = Ks1 * (Salinity - Sref);
            stress = 1 + stress;
            return stress;
            }



        #endregion






        #region Consumption, Intake, Digestion Methods


        /// <summary>
        /// Calculate todays potential total consumption of Dry Matter (per prawn).
        /// Function was derived by reverse-engineering the prawn growth rate equation of Jackson and Yang (1998).
        /// Which is why we first calculate intake, then work out what the consumption must have been.
        /// </summary>
        /// <param name="Prawns"></param>
        /// <param name="StressStock"></param>
        /// <param name="StressTemp"></param>
        /// <returns>(g DM/prawn/d</returns>
        private double PotentialDMConsumedPerPrawn(PrawnCohort Prawns, double StressStock, double StressTemp)
            {
            double intake, consumed;

            intake = Ki1 * Math.Pow(Prawns.LiveWeight, Ki2);  
            intake = intake * StressStock * stressTemp;
            consumed = (1 / Ki4) * intake;  //invert consumption efficiency to go back the other way.
            return consumed;
            }


        /// <summary>
        /// Restricts the Potential total amount of DM Consumed (per prawn) by what is actually available in the pond to be consumed. 
        /// </summary>
        /// <param name="FoodAvailable">Food that is currently in the pond</param>
        /// <param name="Prawns">Prawns that are currently in the pond</param>
        /// <param name="PotentialDMConsumedPP">Amount of DM each prawn would like to eat</param>
        /// <returns>Amount of DM each prawn will actually get to eat. (g DM/prawn)</returns>
        private double CheckEnoughFoodToConsumeToday(Food FoodAvailable, PrawnCohort Prawns, double PotentialDMConsumedPP)
            {
            double potDMConsumed_kg = PotentialDMConsumedPP * Prawns.NumberOfPrawns * g2kg; //convert from grams to kg.

            //Check there is any food in the pond.
            if ((FoodAvailable.TotalDM <= 0.0) && (Prawns.NumberOfPrawns > 0))
                {
                Summary.WriteWarning(this, "There is no food in the pond. The prawns are starving");
                return 0.0; 
                }

            //Check there is enough food in the pond
            if (potDMConsumed_kg > FoodAvailable.TotalDM)
                {
                Summary.WriteWarning(this, "Not enough food in pond for prawns to eat today." +
                   Environment.NewLine + "Reducing dry matter consumed from " + Math.Round(potDMConsumed_kg,3) + " (kg) to " + Math.Round(FoodAvailable.TotalDM, 3) + " (kg)");

                //just give them what is there
                return MathUtilities.Divide(FoodAvailable.TotalDM, Prawns.NumberOfPrawns, 0.0) * kg2g;  //convert kg to grams
                }

            return PotentialDMConsumedPP;
            }




        /// <summary>
        /// Returns the feed that was destroyed by a single prawn in the process of feeding today.
        /// Prawns are messy feeders and don't intake everything that they consume. 
        /// </summary>
        /// <param name="FoodInPond">Food currently in the pond</param>
        /// <param name="CurrentFeed">A feed that is currently available for eating today</param>
        /// <param name="TotalDMConsumed"></param>
        /// <returns>(/prawn/d)</returns>
        private Feed FeedConsumedPerPrawn(Food FoodInPond, Feed CurrentFeed, double TotalDMConsumed)
            {
            double feedFrac = MathUtilities.Divide(CurrentFeed.DryMatter, FoodInPond.TotalDM, 0.0);
            double dm, n, de;

            dm = feedFrac * TotalDMConsumed;
            n = CurrentFeed.NperKgOfDM * dm;
            de = CurrentFeed.DEperKgOfDM * dm;

            Feed consumed = new Feed(CurrentFeed.FeedName, dm, n, de);
            return consumed;
            }


        /// <summary>
        /// Returns the feed actually ingested by the prawn today (given the feed that was consumed by that prawn today).
        /// Prawns are messy feeders and don't intake everything that they consume. 
        /// </summary>
        /// <param name="ConsumedFeedPP">Feed that the prawn has consumed today</param>
        /// <returns>(/prawn/d)</returns>
        private Feed FeedIntakePerPrawn(Feed ConsumedFeedPP)
            {
            double dm, n, de;

            dm = Ki4 * ConsumedFeedPP.DryMatter;
            n = Ki4 * ConsumedFeedPP.Nitrogen;
            de = Ki4 * ConsumedFeedPP.DigestibleEnergy;

            Feed intake = new Feed(ConsumedFeedPP.FeedName, dm, n, de);
            return intake;
            }


        /// <summary>
        /// Returns the feed actually digested by the prawn today (given the feed that was ingested by that prawn today).
        /// </summary>
        /// <param name="IntakeFeedPP">Feed that the prawn has ingested today</param>
        /// <returns>(/prawn/d)</returns>
        private Feed FeedDigestedPerPrawn(Feed IntakeFeedPP)
            {
            double dm, n, de;

            double intake_de2dm = MathUtilities.Divide(IntakeFeedPP.DigestibleEnergy, IntakeFeedPP.DryMatter, 0.0);

            dm = (Ki5 + Ki6 * intake_de2dm) * IntakeFeedPP.DryMatter;
            n = (Ki7 + Ki8 * intake_de2dm) * IntakeFeedPP.Nitrogen;
            de = IntakeFeedPP.DigestibleEnergy;  //as the name suggests all of it can be digested.

            Feed digested = new Feed(IntakeFeedPP.FeedName, dm, n, de);
            return digested;
            }



        #endregion




        #region Growth Methods


        /// <summary>
        /// Maintenance energy requirement.
        /// Energy required to maintain this prawns weight
        /// </summary>
        /// <param name="Prawns"></param>
        /// <param name="StressTemp"></param>
        /// <param name="StressSalinity"></param>
        /// <returns>(kJ/prawn/d)</returns>
        private double EnergyMaintenance(PrawnCohort Prawns, double StressTemp, double StressSalinity)
            {
            double energy;

            energy = Km1 * Math.Pow(Prawns.LiveWeight, Km2);
            energy = energy * StressTemp * StressSalinity;
            return energy;
            }



        /// <summary>
        /// Returns the Potential Live Weight gain in a prawn (only restricted by the energy it has digested).
        /// </summary>
        /// <param name="DigestedFoodPP">Food the prawn digested</param>
        /// <param name="EnergyMaintenance">Energy required to maintain this prawns weight</param>
        /// <returns>g Live Weight/prawn</returns>
        private double EnergyLimitedWeightGain(Food DigestedFoodPP, double EnergyMaintenance)
            {
            //ECG = 5.4 kJ/g
            //NCG = 0.030 g N/g
            //
            //These values have been calculated from body composition information provided in Sarac et al (1994) 
            //dry matter content = 0.26 g/g (an average of 3 quoted values)
            //crude protein: dry weight = 0.72 g/g 
            //gross energy: dry weight = 20.8 kJ/g
            //and the standard CP:N ratio of 6.25

            double ecg = 5.4; // (kJ/g) Energy content of live weight gain

            double energy = Kg1 * (DigestedFoodPP.TotalDE - EnergyMaintenance);
            return energy / ecg; 
            }



        /// <summary>
        /// Returns the Potential Live Weight gain in a prawn (only restricted by the nitrogen it has digested).
        /// </summary>
        /// <param name="DigestedFoodPP">Food the prawn has digested</param>
        /// <returns>g Live Weight/prawn</returns>
        private double NitrogenLimitedWeightGain(Food DigestedFoodPP)
            {
            //ECG = 5.4 kJ/g
            //NCG = 0.030 g N/g
            //
            //These values have been calculated from body composition information provided in Sarac et al (1994) 
            //dry matter content = 0.26 g/g (an average of 3 quoted values)
            //crude protein: dry weight = 0.72 g/g 
            //gross energy: dry weight = 20.8 kJ/g
            //and the standard CP:N ratio of 6.25

            double ncg = 0.030; // (g N/g) Nitrogen content of live weight gain

            double nitrogen = Kg4 * DigestedFoodPP.TotalN;
            return nitrogen / ncg;
            }



        /// <summary>
        /// Actual weight gain of an individual prawn based on the food it has digested.
        /// Assumed to be the lesser of the Energy Limited and Nitrogen Limited growth. 
        /// </summary>
        /// <param name="DigestedFoodPP">Food the prawn has digested</param>
        /// <param name="EnergyMaintenance">Energy required to maintain this prawns weight</param>
        /// <returns>g Live Weight/prawn</returns>
        private double WeightGainPerPrawn(Food DigestedFoodPP, double EnergyMaintenance)
            {
            double energyLimited = EnergyLimitedWeightGain(DigestedFoodPP, EnergyMaintenance);
            double nitrogenLimited = NitrogenLimitedWeightGain(DigestedFoodPP);

            return Math.Min(energyLimited, nitrogenLimited);
            }


        /// <summary>
        /// Actual nitrogen gain of an individual prawn based on the weight it has gained.
        /// </summary>
        /// <param name="WeightGainPerPrawn">Weight the prawn has gained</param>
        /// <returns>g N/prawn</returns>
        private double NitrogenGainPerPrawn(double WeightGainPerPrawn)
            {
            double ncg = 0.030; // (g N/g) Nitrogen content of live weight gain

            return ncg * WeightGainPerPrawn;
            }



        #endregion




        #region Mortality Methods


        /// <summary>
        /// Number of prawns that died today. 
        /// Background deaths (approximately equivalent to 20% per year)
        /// plus additional deaths due low salinity and high ammonium.
        /// (using an equation fitted to the data of Li et al 2007)
        /// </summary>
        /// <param name="Prawns">Current Prawns</param>
        /// <param name="Salinity">Salinity (ppt)</param>
        /// <param name="Ammonium">Ammonium (mg/litre)</param>
        /// <returns>(prawns/d)</returns>
        private int Mortality(PrawnCohort Prawns, double Salinity, double Ammonium)
            {
            double background = Kd1;

            double lowSalinity = MathUtilities.Divide(Salinity, Kd3, 0.0);
            lowSalinity = 1.0 - lowSalinity; 
            lowSalinity = Math.Max(0, lowSalinity); 

            double highAmmonium = MathUtilities.Divide(Ammonium, Kd4, 0.0);
            highAmmonium = highAmmonium - 1.0;  
            highAmmonium = Math.Max(0, highAmmonium);
            highAmmonium = Math.Pow(highAmmonium, Kd5);

            double additional = Kd2 * lowSalinity * highAmmonium;

            double deathRate =  background + additional;

            double deadPrawnNumber = Prawns.NumberOfPrawns * (1.0 - Math.Exp(-deathRate));

            return (int)deadPrawnNumber;
            }



        /// <summary>
        /// Return the prawns that died today as a feed type to add back into the pond.
        /// </summary>
        /// <param name="Deaths">Number of prawns that died today</param>
        /// <param name="Prawns">Current Prawns that are in the pond</param>
        /// <returns>Feed to be added to the pond</returns>
        private Feed DeadPrawnsAsFeed(int Deaths, PrawnCohort Prawns)
            {
            double Kdm = 0.26; //dry matter content of prawns
            double de2dm = 16; //a reasonable guess.

            double dm, n, de;

            dm = Kdm * Deaths * (Prawns.LiveWeight * g2kg);
            n = Deaths * (Prawns.NitrogenMass * g2kg);
            de = de2dm * dm;

            Feed deadPrawnsAsFeed = new Feed("DeadPrawns", dm, n, de);
            return deadPrawnsAsFeed;
            }


        #endregion




        #region Output Methods


        private Feed FeedConsumed(PrawnCohort Prawns, Feed ConsumedFeedPP)
            {
            double dm, n, de;

            dm = ConsumedFeedPP.DryMatter * Prawns.NumberOfPrawns * g2kg;
            n = ConsumedFeedPP.Nitrogen * Prawns.NumberOfPrawns * g2kg;
            de = ConsumedFeedPP.DigestibleEnergy * Prawns.NumberOfPrawns;

            Feed consumed = new Feed(ConsumedFeedPP.FeedName, dm, n, de);
            return consumed;
            }



        private Feed FeedIntake(PrawnCohort Prawns, Feed IntakeFeedPP)
            {
            double dm, n, de; 

            dm = IntakeFeedPP.DryMatter * Prawns.NumberOfPrawns * g2kg;
            n = IntakeFeedPP.Nitrogen * Prawns.NumberOfPrawns * g2kg;
            de = IntakeFeedPP.DigestibleEnergy * Prawns.NumberOfPrawns;

            Feed intake = new Feed(IntakeFeedPP.FeedName, dm, n, de);
            return intake;
            }



        private Feed FeedDigested(PrawnCohort Prawns, Feed DigestedFeedPP)
            {
            double dm, n, de; 

            dm = DigestedFeedPP.DryMatter * Prawns.NumberOfPrawns * g2kg;
            n = DigestedFeedPP.Nitrogen * Prawns.NumberOfPrawns * g2kg;
            de = DigestedFeedPP.DigestibleEnergy * Prawns.NumberOfPrawns;

            Feed digested = new Feed(DigestedFeedPP.FeedName, dm, n, de);
            return digested;
            }



        private Feed FeedWasted(Feed ConsumedFeed)
            {
            double dm, n, de;

            dm = (1-Ki4) * ConsumedFeed.DryMatter;
            n = (1-Ki4) * ConsumedFeed.Nitrogen;
            de = (1-Ki4) * ConsumedFeed.DigestibleEnergy;

            Feed wasted = new Feed(ConsumedFeed.FeedName, dm, n, de);
            return wasted;
            }


        #endregion




        #region Outputs



        /// <summary>
        /// Number of prawns currently in the pond.
        /// </summary>
        [XmlIgnore]
        [Units("")]
        public int NumOfPrawns { get { return prawns.NumberOfPrawns; } }


        /// <summary>
        /// Average live weight of a single prawn
        /// (g /prawn)
        /// </summary>
        [XmlIgnore]
        [Units("g")]
        public double LiveWeight { get { return prawns.LiveWeight; } }


        /// <summary>
        /// Average mass of nitrogen in a single prawn
        /// (g N/prawn)
        /// </summary>
        [XmlIgnore]
        [Units("g")]
        public double Nitrogen { get { return prawns.NitrogenMass; } }




        /// <summary>
        /// Stocking Density Feeding Stress
        /// (0-1)
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double StressStock { get { return stressStock; } }


        /// <summary>
        /// Temperature Stress
        /// (0-1)
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double StressTemp { get { return stressTemp; } }


        /// <summary>
        /// Salinity Stress on energy required for a prawn to maintain it's weight.
        /// (0-1)
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double StressSalinity { get { return stressSalinity; } }




        /// <summary>
        /// Dry Matter consumed by the prawns
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double ConsumedDM { get { return consumedFood.TotalDM; } }

        /// <summary>
        /// Nitrogen consumed by the prawns
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double ConsumedN { get { return consumedFood.TotalN; } }

        /// <summary>
        /// Digestible Energy consumed by the prawns
        /// (MJ/d)
        /// </summary>
        [XmlIgnore]
        [Units("MJ/d")]
        public double ConsumedDE { get { return consumedFood.TotalDE; } }




        /// <summary>
        /// Dry Matter ingested by the prawns
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double IntakeDM { get { return intakeFood.TotalDM; } }

        /// <summary>
        /// Nitrogen ingested by the prawns
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double IntakeN { get { return intakeFood.TotalN; } }

        /// <summary>
        /// Digestible Energy ingested by the prawns
        /// (MJ/d)
        /// </summary>
        [XmlIgnore]
        [Units("MJ/d")]
        public double IntakeDE { get { return intakeFood.TotalDE; } }




        /// <summary>
        /// Dry Matter digested by the prawns
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double DigestedDM { get { return digestedFood.TotalDM; } }

        /// <summary>
        /// Nitrogen digested by the prawns
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double DigestedN { get { return digestedFood.TotalN; } }

        /// <summary>
        /// Digestible Energy digested by the prawns
        /// (MJ/d)
        /// </summary>
        [XmlIgnore]
        [Units("MJ/d")]
        public double DigestedDE { get { return digestedFood.TotalDE; } }





        /// <summary>
        /// Dry Matter that was excreted as faeces
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double FaecesDM { get { return faecesDM; } }


        /// <summary>
        /// Mass of Nitrogen that was excreted as faeces
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double FaecesN { get { return faecesN; } }


        /// <summary>
        /// Ammonium-N that was excreted
        /// (kg/d)
        /// </summary>
        [XmlIgnore]
        [Units("kg/d")]
        public double ExcretedNH4 { get { return excretedAmmonium; } }




        /// <summary>
        /// Number of prawns that died today.
        /// </summary>
        [XmlIgnore]
        [Units("")]
        public int Deaths { get { return deaths; } }


        #endregion




        #region Clock Event Handlers



        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
            {
            prawns = new PrawnCohort(0, 0.0, 0.0);
            }


        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
            {

            }


        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
            {

            consumedFoodPP = new Food();
            intakeFoodPP = new Food();
            digestedFoodPP = new Food();

            consumedFood = new Food();
            intakeFood = new Food();
            digestedFood = new Food();
            wastedFood = new Food();

            stressStock = Stress_Stock(prawns.StockingDensity(PondWater.SurfaceArea));
            stressTemp = Stress_Temp(PondWater.PondTemp);
            stressSalinity = Stress_Salinity(PondWater.Salinity);

            double totalDMConsumedPP = PotentialDMConsumedPerPrawn(prawns, stressStock, stressTemp);
            totalDMConsumedPP = CheckEnoughFoodToConsumeToday(FoodInPond.Food, prawns, totalDMConsumedPP);


            foreach (Feed feed in FoodInPond.Food)
                {
                Feed consumedFeedPP = FeedConsumedPerPrawn(FoodInPond.Food, feed, totalDMConsumedPP);
                Feed intakeFeedPP = FeedIntakePerPrawn(consumedFeedPP);
                Feed digestedFeedPP = FeedDigestedPerPrawn(intakeFeedPP);

                consumedFoodPP.AddFeed(consumedFeedPP);
                intakeFoodPP.AddFeed(intakeFeedPP);
                digestedFoodPP.AddFeed(digestedFeedPP);
                }


            energyMaintenancePP = EnergyMaintenance(prawns, stressTemp, stressSalinity);
            weightGainPP = WeightGainPerPrawn(digestedFoodPP, energyMaintenancePP);
            nitrogenGainPP = NitrogenGainPerPrawn(weightGainPP);

            prawns.LiveWeight = prawns.LiveWeight + weightGainPP;
            prawns.NitrogenMass = prawns.NitrogenMass + nitrogenGainPP;


            deaths = Mortality(prawns, PondWater.Salinity, PondWater.N);  //TODO: PondWater.N should be PondWater.NH4
            prawns.NumberOfPrawns = Math.Max(prawns.NumberOfPrawns - deaths, 0);


            //Calculate the total outputs now we have the correct number of prawns still alive.
            foreach (Feed consumedFeedPP in consumedFoodPP)
                {
                Feed consumedFeed = FeedConsumed(prawns, consumedFeedPP);
                Feed wastedFeed = FeedWasted(consumedFeed);

                consumedFood.AddFeed(consumedFeed);
                wastedFood.AddFeed(wastedFeed);
                }

            foreach (Feed intakeFeedPP in intakeFoodPP)
                {
                Feed intakeFeed = FeedIntake(prawns, intakeFeedPP);
                intakeFood.AddFeed(intakeFeed);
                }

            foreach (Feed digestedFeedPP in digestedFoodPP)
                {
                Feed digestedFeed = FeedDigested(prawns, digestedFeedPP);
                digestedFood.AddFeed(digestedFeed);
                }


            faecesDM = intakeFood.TotalDM - digestedFood.TotalDM;
            faecesN = intakeFood.TotalN - digestedFood.TotalN;
            excretedAmmonium = Math.Max(0, digestedFood.TotalN - (nitrogenGainPP * prawns.NumberOfPrawns));  //can't be negative.


            FoodInPond.Food.RemoveFromExisting(consumedFood);


            deadPrawnsAsFeed = DeadPrawnsAsFeed(deaths, prawns);
            FoodInPond.Food.AddFeed(deadPrawnsAsFeed);

            }


        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
            {

            }

        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            consumedFoodPP = new Food();
            intakeFoodPP = new Food();
            digestedFoodPP = new Food();

            consumedFood = new Food();
            intakeFood = new Food();
            digestedFood = new Food();
            wastedFood = new Food();
        }


        #endregion





        #region Manager Commands


        /// <summary>
        /// Add Prawns to the Pond.
        /// Any existing prawns in the pond are removed first.
        /// </summary>
        /// <param name="NumberOfPrawns"></param>
        /// <param name="LiveWeight">(g/prawn)</param>
        /// <param name="NitrogenMass">(g N/prawn)</param>
        public void AddPrawnsToPond(int NumberOfPrawns, double LiveWeight, double NitrogenMass)
            {
            prawns = new PrawnCohort(NumberOfPrawns, LiveWeight, NitrogenMass);
            }



        /// <summary>
        /// Harvest the Pond of Prawns.
        /// All prawns are removed from the pond.
        /// </summary>
        public void HarvestPond()
            {
            prawns = new PrawnCohort(0, 0.0, 0.0);
            }


        #endregion


        }




    }
