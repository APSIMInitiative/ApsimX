using Models.Core;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>Ruminant growth activity</summary>
	/// <summary>This activity determines potential intake for the Feeding activities and feeding arbitrator for all ruminants</summary>
	/// <summary>This activity includes deaths</summary>
	/// <summary>See Breed activity for births, calf mortality etc</summary>
	/// <version>1.1</version>
	/// <updates>First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityGrow : Model
	{
		[Link]
		private Resources Resources = null;

		/// <summary>
		/// Gross energy content of forage (MJ/kg DM)
		/// </summary>
		[Description("Gross energy content of forage")]
		[Units("MJ/kg DM")]
		public double EnergyGross { get; set; }

		/// <summary>Function to determine all individuals potnetial intake and suckling intake after milk consumption from mother</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFPotentialIntake")]
		private void OnWFPotentialIntake(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			// Natural weaning takes place here before animals eat or take milk from mother.
			foreach (var ind in herd.Where(a => a.Weaned == false))
			{
				if (ind.Age >= ind.BreedParams.GestationLength + 1)
				{
					ind.Wean();
				}
			}

			// Calculate potential intake and reset stores
			foreach (var ind in herd)
			{
				// reset tallies at start of the month
				ind.DietDryMatterDigestibility = 0;
				ind.PercentNOfIntake = 0;
				ind.Intake = 0;
				ind.MilkIntake = 0;
				CalculatePotentialIntake(ind);
			}

			// TODO: Future cohort based run may speed up simulation
			// Calculate by cohort method and assign values to individuals.
			// Need work out what grouping should be based on Name, Breed, Gender, Weight, Parity...
			// This approach will not currently work as individual may have individual weights and females may be in various states of breeding.
			//
			//var cohorts = herd.GroupBy(a => new { a.BreedParams.Breed, a.Gender, a.Age, lactating = (a.DryBreeder | a.Milk) });
			//foreach (var cohort in cohorts)
			//{
			//	CalculatePotentialIntake(cohort.FirstOrDefault());
			//	double potintake = cohort.FirstOrDefault().PotentialIntake;
			//	foreach (var ind in cohort)
			//	{
			//		ind.PotentialIntake = potintake;
			//	}
			//}
		}

		private void CalculatePotentialIntake(Ruminant ind)
		{
			// calculate daily potential intake for the selected individual/cohort
			double standardReferenceWeight = ind.StandardReferenceWeight;

			ind.NormalisedAnimalWeight = standardReferenceWeight - ((1 - ind.BreedParams.SRWBirth) * standardReferenceWeight) * Math.Exp(-(ind.BreedParams.AgeGrowthRateCoefficient * (ind.Age * 30.4)) / (Math.Pow(standardReferenceWeight, ind.BreedParams.SRWGrowthScalar)));
			double liveWeightForIntake = ind.NormalisedAnimalWeight;
			ind.HighWeight = Math.Max(ind.HighWeight, ind.Weight);
			if (ind.HighWeight < ind.NormalisedAnimalWeight)
			{
				liveWeightForIntake = ind.HighWeight;
			}

			// Calculate potential intake based on current weight compared to SRW and previous highest weight
			double potentialIntake = 0;

			// calculate milk intake shortfall for sucklings
			if (!ind.Weaned)
			{
				// potential milk intake/animal/day
				double potentialMilkIntake = ind.BreedParams.MilkIntakeIntercept + ind.BreedParams.IntakeCoefficient * ind.Weight;

				// get mother
				ind.MilkIntake = Math.Min(potentialMilkIntake, ind.MothersMilkAvailable);

				// if milk supply low, calf will subsitute forage up to a specified % of bodyweight (R_C60)
				if (ind.MilkIntake < ind.Weight * ind.BreedParams.MilkLWTFodderSubstitutionProportion)
				{
					potentialIntake = Math.Max(0.0, ind.Weight * ind.BreedParams.MaxJuvenileIntake - ind.MilkIntake * ind.BreedParams.ProportionalDiscountDueToMilk);
				}

				// This has been removed and replaced with prop of LWT based on milk supply.
				// Reference: SCA Metabolic LWTs
				//potentialIntake = ind.BreedParams.IntakeCoefficient * standardReferenceWeight * (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)) * (ind.BreedParams.IntakeIntercept - (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)));
			}
			else
			{
				// Reference: SCA based actual LWTs
				potentialIntake = ind.BreedParams.IntakeCoefficient * liveWeightForIntake * (ind.BreedParams.IntakeIntercept - liveWeightForIntake / standardReferenceWeight);

				if (ind.Gender == Sex.Female)
				{
					RuminantFemale femaleind = ind as RuminantFemale;

					// Increase potential intake for lactating breeder
					if (femaleind.IsLactating)
					{
						double dayOfLactation = Math.Max((ind.Age - femaleind.AgeAtLastBirth) * 30.4, 0);
						if (dayOfLactation > ind.BreedParams.MilkingDays)
						{
							// Reference: Intake multiplier for lactating cow (M.Freer)
							// TODO: Need to look at equation to fix Math.Pow() ^ issue
							//					double intakeMilkMultiplier = 1 + 0.57 * Math.Pow((dayOfLactation / 81.0), 0.7) * Math.Exp(0.7 * (1 - (dayOfLactation / 81.0)));
							double intakeMilkMultiplier = 1 + ind.BreedParams.LactatingPotentialModifierConstantA * Math.Pow((dayOfLactation / ind.BreedParams.LactatingPotentialModifierConstantB), ind.BreedParams.LactatingPotentialModifierConstantC) * Math.Exp(ind.BreedParams.LactatingPotentialModifierConstantC * (1 - (dayOfLactation / ind.BreedParams.LactatingPotentialModifierConstantB)));
							// To make this flexible for sheep and goats, added three new Ruminant Coeffs
							// Feeding standard values for Beef, Dairy suck, Dairy non-suck and sheep are:
							// For 0.57 (A) use .42, .58, .85 and .69; for 0.7 (B) use 1.7, 0.7, 0.7 and 1.4, for 81 (C) use 62, 81, 81, 28
							// added LactatingPotentialModifierConstantA, LactatingPotentialModifierConstantB and LactatingPotentialModifierConstantC
							potentialIntake *= intakeMilkMultiplier;
						}
					}
				}
				//TODO: option to restrict potential further due to stress (e.g. heat, cold, rain)

				// get monthly intake
				potentialIntake *= 30.4;
			}
			ind.PotentialIntake = potentialIntake;
		}

		/// <summary>Function to calculate growth of herd for the monthly timestep</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalWeightGain")]
		private void OnWFAnimalWeightGain(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			double totalMethane = 0;

			// grow all weaned individuals

			foreach (Ruminant ind in herd.Where(a => (a.Weaned == true)))
			{
				// calculate protein concentration

				// Calculate diet dry matter digestibilty from the %N of the current diet intake.
				// Reference: Ash and McIvor
				//ind.DietDryMatterDigestibility = 36.7 + 9.36 * ind.PercentNOfIntake / 62.5;
				// Now tracked via incoming food DMD values

				// TODO: NABSA restricts Diet_DMD to 75% before supplements. Why?
				// Our flow has already taken in supplements by this stage and cannot be distinguished
				// Maybe this limit should be placed on some feed to limit DMD to 75% for non supp feeds

				// TODO: Check equation. NABSA doesn't include the 0.9
				// Crude protein required generally 130g per kg of digestable feed.
				double crudeProteinRequired = ind.BreedParams.ProteinCoefficient * ind.DietDryMatterDigestibility / 100;

				// adjust for efficiency of use of protein, 90% degradable
				double crudeProteinSupply = (ind.PercentNOfIntake * 62.5) * 0.9;
				// This was proteinconcentration * 0.9

				// prevent future divide by zero issues.
				if (crudeProteinSupply == 0.0) crudeProteinSupply = 0.001;

				double ratioSupplyRequired = Math.Min(0.3, Math.Max(1.3, crudeProteinSupply / crudeProteinRequired));

				// TODO: check if we still need to apply modification to only the non-supplemented component of intake

				ind.Intake *= ratioSupplyRequired;
				ind.Intake = Math.Min(ind.Intake, ind.PotentialIntake * 1.2);

				// TODO: nabsa adjusts potential intake for digestability of fodder here.
				// I'm sure it can be done here, but prob as this is after the 1.2x cap has been performed.
				// calculate from the pools of fodder fed to this individual
				//if (0.8 - ind.BreedParams.IntakeTropicalQuality - dietDMD / 100 >= 0)
				//{
				//	ind.PotentialIntake *= ind.BreedParams.IntakeCoefficientQuality * (0.8 - ind.BreedParams.IntakeTropicalQuality - dietDMD / 100);
				//}

				// calculate energy
				// includes mortality and growth
				double methane = 0;

				CalculateEnergy(ind, out methane);

				// ? call methane produced event
				// or sum and produce one event for breed at end of loop
				totalMethane += methane;

				// calculate manure
				// ? call manure produce event
				// or sum and produce one event for breed at end of loop

				// grow wool and cashmere
				ind.Wool += ind.BreedParams.WoolCoefficient * ind.Intake;
				ind.Cashmere += ind.BreedParams.CashmereCoefficient * ind.Intake;
			}
		}

		/// <summary>
		/// Function to calculate energy from intake and subsequent growth
		/// </summary>
		/// <param name="ind">Ruminant individual class</param>
		/// <param name="methaneProduced">Sets output variable to value of methane produced</param>
		/// <returns></returns>
		private void CalculateEnergy(Ruminant ind, out double methaneProduced)
		{
			// Sme 1 for females and castrates
			// TODO: castrates not implemented
			double Sme = 1;
			// Sme 1.15 for all males.
			if (ind.Gender == Sex.Male) Sme = 1.15;

			double energyDiet = EnergyGross * ind.DietDryMatterDigestibility / 100.0;
			// Reference: Nutrient Requirements of domesticated ruminants (p7)
			double energyMetabolic = energyDiet * 0.81;
			double energyMetablicFromIntake = energyMetabolic * ind.Intake;

			double km = ind.BreedParams.EMaintCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.EMaintIntercept;
			// Reference: SCA p.49
			double kg = ind.BreedParams.EGrowthCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.EGrowthIntercept;

			double energyPredictedBodyMassChange = 0;
			double energyMaintenance = 0;
			if (!ind.Weaned)
			{
				// old code
				// dum = potential milk intake daily
				// dumshort = potential intake. check that it isnt monthly

				// average energy efficiency for maintenance
				double kml = ((ind.MilkIntake * 0.7) + (ind.PotentialIntake * km)) / (ind.MilkIntake + ind.PotentialIntake);
				// average energy efficiency for growth
				double kgl = ((ind.MilkIntake * 0.7) + (ind.PotentialIntake * kg)) / (ind.MilkIntake + ind.PotentialIntake);

				double energyMilkConsumed = ind.MilkIntake * 3.2;
				// limit calf intake of milk per day
				energyMilkConsumed = Math.Min(ind.BreedParams.MilkIntakeMaximum * 3.2, energyMilkConsumed);

				energyMaintenance = (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / kml) * Math.Exp(-ind.BreedParams.EMaintExponent * ind.Age);
				ind.EnergyBalance = energyMilkConsumed - energyMaintenance + energyMetablicFromIntake;

				double feedingValue = 0;
				if (ind.EnergyBalance > 0)
				{
					feedingValue = 2 * 0.7 * ind.EnergyBalance / (kgl * energyMaintenance) - 1;
				}
				else
				{
					//(from Hirata model)
					feedingValue = 2 * ind.EnergyBalance / (0.85 * energyMaintenance) - 1;
				}
				double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + feedingValue + (ind.BreedParams.GrowthEnergyIntercept2 - feedingValue) / (1 + Math.Exp(-6 * (ind.Weight / ind.NormalisedAnimalWeight - 0.4)));

				energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * 0.7 * ind.EnergyBalance / energyEmptyBodyGain;
			}
			else
			{
				//all weaned individuals

				double energyMilk = 0;
				double energyFoetus = 0;

				if (ind.Gender == Sex.Female)
				{
					RuminantFemale femaleind = ind as RuminantFemale;

					// calculate energy for lactation
					if (femaleind.IsLactating)
					{
						// Reference: SCA p.
						double kl = ind.BreedParams.ELactationCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.ELactationIntercept;
						double milkTime = Math.Max(0.0, (ind.Age - femaleind.AgeAtLastBirth + 1) * 30.4);
						if (milkTime <= ind.BreedParams.MilkingDays)
						{
							double milkCurve = 0;
							if (femaleind.DryBreeder) // no suckling calf
							{
								milkCurve = ind.BreedParams.MilkCurveNonSuckling;
							}
							else // suckling calf
							{
								milkCurve = ind.BreedParams.MilkCurveSuckling;
							}
							//TODO: check this equation that I redefined it correctly.
							double milkProduction = ind.BreedParams.MilkPeakYield * ind.Weight / ind.NormalisedAnimalWeight * (Math.Pow(((milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay), milkCurve)) * Math.Exp(milkCurve * (1 - (milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay));
							milkProduction = Math.Max(milkProduction, 0.0);
							// Reference: Potential milk prodn, 3.2 MJ/kg milk - Jouven et al 2008
							energyMilk = milkProduction * 3.2 / kl;
							if (ind.EnergyBalance < (-0.5936 / 0.322 * energyMilk))
							{
								ind.EnergyBalance = (-0.5936 / 0.322 * energyMilk);
							}
							milkProduction = Math.Max(0.0, milkProduction * (0.5936 + 0.322 * ind.EnergyBalance / energyMilk));
							// Reference: Adjusted milk prodn, 3.2 MJ/kg milk - Jouven et al 2008
							energyMilk = milkProduction * 3.2 / kl;
						}
					}

					// Determine energy required for foetal development
					if (femaleind.IsPregnant)
					{
						double standardReferenceWeight = ind.StandardReferenceWeight;
						// Potential birth weight
						// Reference: Freer
						double potentialBirthWeight = ind.BreedParams.SRWBirth * standardReferenceWeight * (1 - 0.33 * (1 - ind.Weight / standardReferenceWeight));
						double foetusAge = (femaleind.Age - femaleind.AgeAtLastConception + 1) * 30.4;
						//TODO: Check foetus gage correct
						energyFoetus = potentialBirthWeight * 349.16 * 0.000058 * Math.Exp(345.67 - 0.000058 * foetusAge - 349.16 * Math.Exp(-0.000058 * foetusAge)) / 0.13;
					}
				}

				//TODO: add draft energy requirement

				// set maintenance age to maximum of 6 years
				double maintenanceAge = Math.Min(ind.Age * 30.4, 2190);

				// Reference: SCA p.24
				// Regference p19 (1.20). Does not include MEgraze or Ecold, also skips M,
				// 0.000082 is -0.03 Age in Years/365 for days 
				energyMaintenance = ind.BreedParams.Kme * Sme * (0.26 * Math.Pow(ind.Weight, 0.75) / km) * Math.Exp(-0.000082 * maintenanceAge) + (0.09 * energyMetablicFromIntake);
				ind.EnergyBalance = energyMetablicFromIntake - energyMaintenance - energyMilk - energyFoetus; // milk will be zero for non lactating individuals.

				// Reference: Feeding_value = Ajustment for rate of loss or gain (SCA p.43, ? different from Hirata model)
				double feedingValue = 0;
				if (ind.EnergyBalance > 0)
				{
					feedingValue = 2 * ((kg * ind.EnergyBalance) / (km * energyMaintenance) - 1);
				}
				else
				{
					feedingValue = 2 * (ind.EnergyBalance / (0.8 * energyMaintenance) - 1);  //(from Hirata model)
				}
				double weightToReferenceRatio = Math.Min(1.0, ind.Weight / ind.StandardReferenceWeight);

				// Reference:  MJ of Energy required per kg Empty body gain (SCA p.43)
				double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + feedingValue + (ind.BreedParams.GrowthEnergyIntercept1 - feedingValue) / (1 + Math.Exp(-6 * (weightToReferenceRatio - 0.4)));
				// Determine Empty body change from Eebg and Ebal, and increase by 9% for LW change
				energyPredictedBodyMassChange = 0;
				if (ind.EnergyBalance > 0)
				{
					energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * kg * ind.EnergyBalance / energyEmptyBodyGain;
				}
				else
				{
					// Reference: from Hirata model
					energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * km * ind.EnergyBalance / (0.8 * energyEmptyBodyGain);
				}
			}
			energyPredictedBodyMassChange *= 30.4;  // Convert to monthly
			ind.PreviousWeight = ind.Weight;
			ind.Weight += energyPredictedBodyMassChange;
			ind.Weight = Math.Max(0.0, ind.Weight);
			ind.Weight = Math.Min(ind.Weight, ind.StandardReferenceWeight * ind.BreedParams.MaximumSizeOfIndividual);

			// Function to calculate approximate methane produced by animal, based on feed intake
			// Function based on Freer spreadsheet
			methaneProduced = 0.02 * ind.Intake * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
			methaneProduced /= 55.28 * 1000; // grams per day
		}

		/// <summary>
		/// Function to age individuals and remove those that died in timestep
		/// This needs to be undertaken prior to herd management
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAgeResources")]
		private void OnWFAgeResources(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			// grow all individuals
			foreach (Ruminant ind in herd)
			{
				ind.Age++;
			}
		}

		/// <summary>Function to determine which animlas have died and remove from the population</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalDeath")]
		private void OnWFAnimalDeath(object sender, EventArgs e)
		{
			// remove individuals that died
			// currently performed in the month after weight has been adjusted
			// and before breeding, trading, culling etc (See Clock event order)

			// Calculated by
			// critical weight &
			// juvenile (unweaned) death based on mothers weight &
			// adult weight adjusted base mortality.

			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			// weight based mortality
			List<Ruminant> died = herd.Where(a => a.Weight < (a.HighWeight * (1.0 - a.BreedParams.ProportionOfMaxWeightToSurvive))).ToList();
			// set died flag
			died.Select(a => { a.SaleFlag = Common.HerdChangeReason.Died; return a; }).ToList();
			ruminantHerd.RemoveRuminant(died);

			TMyRandom randomGenerator = new TMyRandom(10);

			foreach (var ind in ruminantHerd.Herd)
			{
				double mortalityRate = 0;
				if (!ind.Weaned)
				{
					mortalityRate = 0;
					if (ind.Mother.Weight < ind.BreedParams.CriticalCowWeight * ind.StandardReferenceWeight)
					{
						mortalityRate = ind.BreedParams.JuvenileMortalityMaximum;
					}
					else
					{
						mortalityRate = Math.Exp(-Math.Pow(ind.BreedParams.JuvenileMortalityCoefficient * (ind.Weight / ind.NormalisedAnimalWeight), ind.BreedParams.JuvenileMortalityExponent)) / 100;
					}
					mortalityRate += mortalityRate + ind.BreedParams.MortalityBase;
					mortalityRate = Math.Max(mortalityRate, ind.BreedParams.JuvenileMortalityMaximum);
				}
				else
				{
					mortalityRate = 1 - (1 - ind.BreedParams.MortalityBase) * (1 - Math.Exp(Math.Pow(-(ind.BreedParams.MortalityCoefficient * (ind.Weight / ind.NormalisedAnimalWeight - ind.BreedParams.MortalityIntercept)), ind.BreedParams.MortalityExponent)));
				}
				if (randomGenerator.RandNo <= mortalityRate)
				{
					ind.Died = true;
				}
			}

			died = herd.Where(a => a.Died).ToList();
			died.Select(a => { a.SaleFlag = Common.HerdChangeReason.Died; return a; }).ToList();
			ruminantHerd.RemoveRuminant(died);

			//// mortality calculation (calculation actually calculates the survival probability)
			//for (int i = herd.Count; i >= 0; i--)
			//{
			//	double mortalityRate = 0;
			//	if (!herd[i].Weaned)
			//	{
			//		mortalityRate = 0;
			//		if (herd[i].Mother.Weight < herd[i].BreedParams.CriticalCowWeight * herd[i].StandardReferenceWeight)
			//		{
			//			mortalityRate = herd[i].BreedParams.JuvenileMortalityMaximum;
			//		}
			//		else
			//		{
			//			mortalityRate = Math.Exp(-Math.Pow(herd[i].BreedParams.JuvenileMortalityCoefficient * (herd[i].Weight / herd[i].NormalisedAnimalWeight), herd[i].BreedParams.JuvenileMortalityExponent)) / 100;
			//		}
			//		mortalityRate += mortalityRate + herd[i].BreedParams.MortalityBase;
			//		mortalityRate = Math.Max(mortalityRate, herd[i].BreedParams.JuvenileMortalityMaximum);
			//	}
			//	else
			//	{
			//		mortalityRate = 1 - (1 - herd[i].BreedParams.MortalityBase) * (1 - Math.Exp(Math.Pow(-(herd[i].BreedParams.MortalityCoefficient * (herd[i].Weight / herd[i].NormalisedAnimalWeight - herd[i].BreedParams.MortalityIntercept)), herd[i].BreedParams.MortalityExponent)));
			//	}
			//	if (randomGenerator.RandNo <= mortalityRate)
			//	{
			//		herd[i].Died = true;
			//	}
			//}

		}
	}
}
