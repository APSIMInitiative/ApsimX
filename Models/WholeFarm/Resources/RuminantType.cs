using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;

namespace Models.WholeFarm.Resources
{
    /// <summary>
    /// This stores the parameters for a ruminant Type
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantHerd))]
    public class RuminantType : WFModel
    {
        /// <summary>
        /// Breed
        /// </summary>
        [Description("Breed")]
        [Required]
        public string Breed { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public RuminantType()
		{
			this.SetDefaults();
		}

		/// <summary>
		/// Current value of individuals in the herd
		/// </summary>
		[XmlIgnore]
		public List<AnimalPriceValue> PriceList;

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFInitialiseResource")]
		private void OnWFInitialiseResource(object sender, EventArgs e)
		{
			// setup price list 
			// initialise herd price list
			PriceList = new List<AnimalPriceValue>();

			foreach (AnimalPricing priceGroup in Apsim.Children(this, typeof(AnimalPricing)))
			{
                SirePrice = priceGroup.BreedingSirePrice;
				foreach (AnimalPriceEntry price in Apsim.Children(priceGroup, typeof(AnimalPriceEntry)))
				{
					AnimalPriceValue val = new AnimalPriceValue();
					val.Age = price.Age;
					val.PurchaseValue = price.PurchaseValue;
					val.Gender = price.Gender;
					val.Breed = this.Breed;
					val.SellValue = price.SellValue;
					val.Style = priceGroup.PricingStyle;
					PriceList.Add(val);
				}
			}
			PriceList = PriceList.OrderBy(a => a.Age).ToList();
		}

		/// <summary>
		/// Determine if a price schedule has been provided for this breed
		/// </summary>
		/// <returns>boolean</returns>
		public bool PricingAvailable() {  return (PriceList.Count>0); }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double SirePrice { get; set; }

        /// <summary>
        /// Get value of a specific individual
        /// </summary>
        /// <returns>value</returns>
        public double ValueofIndividual(Ruminant ind, bool PurchasePrice)
		{
			if (PricingAvailable())
			{
				// ordering now done when the list is created for speed
				//AnimalPriceValue getvalue = PriceList.Where(a => a.Age < ind.Age).OrderBy(a => a.Age).LastOrDefault();
				AnimalPriceValue getvalue = PriceList.Where(a => a.Age < ind.Age).LastOrDefault();
				if(PurchasePrice)
				{
					return getvalue.PurchaseValue * ((getvalue.Style == PricingStyleType.perKg) ? ind.Weight : 1.0);
				}
				else
				{
					return getvalue.SellValue * ((getvalue.Style == PricingStyleType.perKg) ? ind.Weight : 1.0);
				}
			}
			else
			{
				return 0;
			}
		}

		#region Grow Activity

		/// <summary>
		/// Energy maintenance efficiency coefficient
		/// </summary>
		[Description("Energy maintenance efficiency coefficient")]
        [Required]
        public double EMaintEfficiencyCoefficient { get; set; }
		/// <summary>
		/// Energy maintenance efficiency intercept
		/// </summary>
		[Description("Energy maintenance efficiency intercept")]
        [Required]
        public double EMaintEfficiencyIntercept { get; set; }
		/// <summary>
		/// Energy growth efficiency coefficient
		/// </summary>
		[Description("Energy growth efficiency coefficient")]
        [Required]
        public double EGrowthEfficiencyCoefficient { get; set; }
		/// <summary>
		/// Energy growth efficiency intercept
		/// </summary>
		[Description("Energy growth efficiency intercept")]
        [Required]
        public double EGrowthEfficiencyIntercept { get; set; }
		/// <summary>
		/// Energy lactation efficiency coefficient
		/// </summary>
		[Description("Energy lactation efficiency coefficient")]
        [Required]
        public double ELactationEfficiencyCoefficient { get; set; }
		/// <summary>
		/// Energy lactation efficiency intercept
		/// </summary>
		[Description("Energy lactation efficiency intercept")]
        [Required]
        public double ELactationEfficiencyIntercept { get; set; }
		/// <summary>
		/// Energy maintenance exponent
		/// </summary>
		[Description("Energy maintenance exponent")]
        [Required]
        public double EMaintExponent { get; set; }
		/// <summary>
		/// Energy maintenance intercept
		/// </summary>
		[Description("Energy maintenance intercept")]
        [Required]
        public double EMaintIntercept { get; set; }
		/// <summary>
		/// Energy maintenance coefficient
		/// </summary>
		[Description("Energy maintenance coefficient")]
        [Required]
        public double EMaintCoefficient { get; set; }
		/// <summary>
		/// Maximum age for energy maintenance calculation (yrs)
		/// </summary>
		[System.ComponentModel.DefaultValueAttribute(6)]
		[Description("Maximum age for energy maintenance calculation (yrs)")]
        [Required]
        public double EnergyMaintenanceMaximumAge { get; set; }
		/// <summary>
		/// Breed factor for maintenence energy
		/// </summary>
		[Description("Breed factor for maintenence energy")]
        [Required]
        public double Kme { get; set; }
		/// <summary>
		/// Parameter for energy for growth #1
		/// </summary>
		[Description("Parameter for energy for growth #1")]
        [Required]
        public double GrowthEnergyIntercept1 { get; set; }
		/// <summary>
		/// Parameter for energy for growth #2
		/// </summary>
		[Description("Parameter for energy for growth #2")]
        [Required]
        public double GrowthEnergyIntercept2 { get; set; }
		/// <summary>
		/// Growth efficiency
		/// </summary>
		[Description("Growth efficiency")]
        [Required]
        public double GrowthEfficiency { get; set; }

		/// <summary>
		/// Standard Reference Weight of female
		/// </summary>
		[Description("Standard Ref. Weight (kg) for a female")]
        [Required]
        public double SRWFemale { get; set; }
		/// <summary>
		/// Standard Reference Weight for male from female multiplier
		/// </summary>
		[Description("Male Standard Ref. Weight multiplier from female")]
        [Required]
        public double SRWMaleMultiplier { get; set; }
		/// <summary>
		/// Standard Reference Weight at birth
		/// </summary>
		[Description("Birth mass (proportion of female SRW)")]
        [Required]
        public double SRWBirth { get; set; }
		/// <summary>
		/// Age growth rate coefficient
		/// </summary>
		[Description("Age growth rate coefficient")]
        [Required]
        public double AgeGrowthRateCoefficient { get; set; }
		/// <summary>
		/// SWR growth scalar
		/// </summary>
		[Description("SWR growth scalar")]
        [Required]
        public double SRWGrowthScalar { get; set; }
		/// <summary>
		/// Intake coefficient in relation to Live Weight
		/// </summary>
		[Description("Intake coefficient in relation to Live Weight")]
        [Required]
        public double IntakeCoefficient { get; set; }
		/// <summary>
		/// Intake intercept In relation to SRW
		/// </summary>
		[Description("Intake intercept In relation to SRW")]
        [Required]
        public double IntakeIntercept { get; set; }
		/// <summary>
		/// Protein requirement coeff (g/kg feed)
		/// </summary>
		[Description("Protein requirement coeff (g/kg feed)")]
        [Required]
        public double ProteinCoefficient { get; set; }
		/// <summary>
		/// Protein degradability
		/// </summary>
		[Description("Protein degradability")]
        [Required]
        public double ProteinDegradability { get; set; }
		/// <summary>
		/// Weight(kg) of 1 animal equivalent(steer)
		/// </summary>
		[Description("Weight(kg) of 1 animal equivalent(steer)")]
        [Required]
        public double BaseAnimalEquivalent { get; set; }
		/// <summary>
		/// Maximum green in diet
		/// </summary>
		[Description("Maximum green in diet")]
        [Required]
        public double GreenDietMax { get; set; }
		/// <summary>
		/// Shape of curve for diet vs pasture
		/// </summary>
		[Description("Shape of curve for diet vs pasture")]
        [Required]
        public double GreenDietCoefficient { get; set; }
		/// <summary>
		/// Proportion green in pasture at zero in diet
		/// was %
		/// </summary>
		[Description("Proportion green in pasture at zero in diet")]
		public double GreenDietZero { get; set; }
		/// <summary>
		/// Coefficient to adjust intake for herbage quality
		/// </summary>
		[Description("Coefficient to adjust intake for herbage quality")]
        [Required]
        public double IntakeTropicalQuality { get; set; }
		/// <summary>
		/// Coefficient to adjust intake for tropical herbage quality
		/// </summary>
		[Description("Coefficient to adjust intake for tropical herbage quality")]
        [Required]
        public double IntakeCoefficientQuality { get; set; }
		/// <summary>
		/// Coefficient to adjust intake for herbage biomass
		/// </summary>
		[Description("Coefficient to adjust intake for herbage biomass")]
        [Required]
        public double IntakeCoefficientBiomass { get; set; }
		/// <summary>
		/// Enforce strict feeding limits
		/// </summary>
		[Description("Enforce strict feeding limits")]
        [Required]
        public bool StrictFeedingLimits { get; set; }
		/// <summary>
		/// Coefficient of juvenile milk intake
		/// </summary>
		[Description("Coefficient of juvenile milk intake")]
        [Required]
        public double MilkIntakeCoefficient { get; set; }
		/// <summary>
		/// Intercept of juvenile milk intake
		/// </summary>
		[Description("Intercept of juvenile milk intake")]
        [Required]
        public double MilkIntakeIntercept { get; set; }
		/// <summary>
		/// Maximum juvenile milk intake
		/// </summary>
		[Description("Maximum juvenile milk intake")]
        [Required]
        public double MilkIntakeMaximum { get; set; }
		/// <summary>
		/// Milk as proportion of LWT for fodder substitution
		/// </summary>
		[Description("Milk as proportion of LWT for fodder substitution")]
        [Required]
        public double MilkLWTFodderSubstitutionProportion { get; set; }
		/// <summary>
		/// Max juvenile (suckling) intake as proportion of LWT
		/// </summary>
		[Description("Max juvenile (suckling) intake as proportion of LWT")]
        [Required]
        public double MaxJuvenileIntake { get; set; }
		/// <summary>
		/// Proportional discount to intake due to milk intake
		/// </summary>
		[Description("Proportional discount to intake due to milk intake")]
        [Required]
        public double ProportionalDiscountDueToMilk { get; set; }
		/// <summary>
		/// Proportion of max body weight needed for survival
		/// </summary>
		[Description("Proportion of max body weight needed for survival")]
        [Required]
        public double ProportionOfMaxWeightToSurvive { get; set; }
		/// <summary>
		/// Lactating Potential intake modifier Coefficient A
		/// </summary>
		[Description("Lactating Potential intake modifier Coefficient A")]
        [Required]
        public double LactatingPotentialModifierConstantA { get; set; }
		/// <summary>
		/// Lactating Potential intake modifier Coefficient B
		/// </summary>
		[Description("Lactating Potential intake modifier Coefficient B")]
        [Required]
        public double LactatingPotentialModifierConstantB { get; set; }
		/// <summary>
		/// Lactating Potential intake modifier Coefficient C
		/// </summary>
		[Description("Lactating Potential intake modifier Coefficient C")]
        [Required]
        public double LactatingPotentialModifierConstantC { get; set; }
		/// <summary>
		/// Maximum size of individual relative to SRW
		/// </summary>
		[Description("Maximum size of individual relative to SRW")]
        [Required]
        public double MaximumSizeOfIndividual { get; set; }
		/// <summary>
		/// Mortality rate base
		/// </summary>
		[Description("Mortality rate base")]
        [Required]
        public double MortalityBase { get; set; }
		/// <summary>
		/// Mortality rate coefficient
		/// </summary>
		[Description("Mortality rate coefficient")]
        [Required]
        public double MortalityCoefficient { get; set; }
		/// <summary>
		/// Mortality rate intercept
		/// </summary>
		[Description("Mortality rate intercept")]
        [Required]
        public double MortalityIntercept { get; set; }
		/// <summary>
		/// Mortality rate exponent
		/// </summary>
		[Description("Mortality rate exponent")]
        [Required]
        public double MortalityExponent { get; set; }
		/// <summary>
		/// Juvenile mortality rate coefficient
		/// </summary>
		[Description("Juvenile mortality rate coefficient")]
        [Required]
        public double JuvenileMortalityCoefficient { get; set; }
		/// <summary>
		/// Juvenile mortality rate maximum
		/// </summary>
		[Description("Juvenile mortality rate maximum")]
        [Required]
        public double JuvenileMortalityMaximum { get; set; }
		/// <summary>
		/// Juvenile mortality rate exponent
		/// </summary>
		[Description("Juvenile mortality rate exponent")]
        [Required]
        public double JuvenileMortalityExponent { get; set; }
		/// <summary>
		/// Wool coefficient
		/// </summary>
		[Description("Wool coefficient")]
        [Required]
        public double WoolCoefficient { get; set; }
		/// <summary>
		/// Cashmere coefficient
		/// </summary>
		[Description("Cashmere coefficient")]
        [Required]
        public double CashmereCoefficient { get; set; }
		#endregion

		#region Breed activity

		/// <summary>
		/// Milk curve shape suckling
		/// </summary>
		[Description("Milk curve shape suckling")]
        [Required]
        public double MilkCurveSuckling { get; set; }
		/// <summary>
		/// Milk curve shape non suckling
		/// </summary>
		[Description("Milk curve shape non suckling")]
        [Required]
        public double MilkCurveNonSuckling { get; set; }
		/// <summary>
		/// Number of days for milking
		/// </summary>
		[Description("Number of days for milking")]
        [Required]
        public double MilkingDays { get; set; }
		/// <summary>
		/// Peak milk yield(kg/day)
		/// </summary>
		[Description("Peak milk yield (kg/day)")]
        [Required]
        public double MilkPeakYield { get; set; }
		/// <summary>
		/// Milk offset day
		/// </summary>
		[Description("Milk offset day")]
        [Required]
        public double MilkOffsetDay { get; set; }
		/// <summary>
		/// Milk peak day
		/// </summary>
		[Description("Milk peak day")]
        [Required]
        public double MilkPeakDay { get; set; }
		/// <summary>
		/// Inter-parturition interval intercept of PW (months)
		/// </summary>
		[Description("Inter-parturition interval intercept of PW (months)")]
        [Required]
        public double InterParturitionIntervalIntercept { get; set; }
		/// <summary>
		/// Inter-parturition interval coefficient of PW (months)
		/// </summary>
		[Description("Inter-parturition interval coefficient of PW (months)")]
        [Required]
        public double InterParturitionIntervalCoefficient { get; set; }
		/// <summary>
		/// Months between conception and parturition
		/// </summary>
		[Description("Months between conception and parturition")]
        [Required]
        public double GestationLength { get; set; }
		/// <summary>
		/// Minimum age for 1st mating (months)
		/// </summary>
		[Description("Minimum age for 1st mating (months)")]
        [Required]
        public double MinimumAge1stMating { get; set; }
		/// <summary>
		/// Minimum size for 1st mating, proportion of SRW
		/// </summary>
		[Description("Minimum size for 1st mating, proportion of SRW")]
        [Required]
        public double MinimumSize1stMating { get; set; }
		/// <summary>
		/// Minimum number of days between last birth and conception
		/// </summary>
		[Description("Minimum number of days between last birth and conception")]
        [Required]
        public double MinimumDaysBirthToConception { get; set; }
		/// <summary>
		/// Rate at which twins are concieved
		/// </summary>
		[Description("Rate at which twins are concieved")]
        [Required]
        public double TwinRate { get; set; }
		/// <summary>
		/// Proportion of SRW for zero calving/lambing rate
		/// </summary>
		[Description("Proportion of SRW for zero Calving/lambing rate")]
        [Required]
        public double CriticalCowWeight { get; set; }
		/// <summary>
		/// Conception rate coefficient of breeder PW
		/// </summary>
		[Description("Conception rate coefficient of breeder PW (12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required]
        public double[] ConceptionRateCoefficent { get; set; }
		/// <summary>
		/// Conception rate intercept of breeder PW
		/// </summary>
		[Description("Conception rate intercept of breeder PW (12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required]
        public double[] ConceptionRateIntercept { get; set; }
		/// <summary>
		/// Conception rate assymtote
		/// </summary>
		[Description("Conception rate assymtote (12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required]
        public double[] ConceptionRateAsymptote { get; set; }
		/// <summary>
		/// Maximum number of matings per male per day
		/// </summary>
		[Description("Maximum number of matings per male per day")]
        [Required]
        public double MaximumMaleMatingsPerDay { get; set; }
		/// <summary>
		/// Prenatal mortality rate
		/// </summary>
		[Description("Mortality rate from conception to birth (proportion)")]
        [Required]
        public double PrenatalMortality { get; set; }
		/// <summary>
		/// Maximum conception rate from uncontrolled breeding 
		/// </summary>
		[Description("Maximum conception rate from uncontrolled breeding")]
        [Required]
        public double MaximumConceptionUncontrolledBreeding { get; set; }

		#endregion

		#region other

		/// <summary>
		/// Methane production from intake intercept
		/// </summary>
		[Description("Methane production from intake intercept")]
        [Required]
        public double MethaneProductionIntercept { get; set; }

		/// <summary>
		/// Methane production from intake coefficient
		/// </summary>
		[Description("Methane production from intake coefficient")]
        [Required]
        public double MethaneProductionCoefficient { get; set; }

		#endregion

	}




}