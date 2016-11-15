using Models.Core;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>Ruminant breeding activity</summary>
	/// <summary>This activity provides all functionality for ruminant breeding up until natural weaning</summary>
	/// <summary>It will be applied to the supplied herd if males and females are located together</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]

	public class RuminantActivityBreed : Model
	{
		[Link]
		private Resources Resources = null;
		[Link]
		ISummary Summary = null;

		private TMyRandom randomGenerator = new TMyRandom(10);

		/// <summary>
		/// Name of herd to breed
		/// </summary>
		[Description("Name of herd to breed")]
		public string HerdName { get; set; }

		/// <summary>
		/// Determines if matings are controlled
		/// </summary>
		[Description("Controlled matings")]
		public bool ControlledMatings { get; set; }

		/// <summary>
		/// Maximum conception rate for uncontrolled matings
		/// </summary>
		public double MaximumConceptionRateUncontrolled { get; set; }

		/// <summary>An event handler to allow us to initialise herd breeding status.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			// This needs to happen after all herd creation has been performed
			// Therefore we use StartOfSimulation event

			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.HerdName == HerdName).ToList();

			// get list of females of breeding age and condition
			List<RuminantFemale> breedFemales = herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating + 12 & a.Weight >= (a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight) & a.Weight >= (a.BreedParams.CriticalCowWeight * a.StandardReferenceWeight)).OrderByDescending(a => a.Age).ToList().Cast<RuminantFemale>().ToList();

			// get list of all sucking individuals
			List<Ruminant> sucklingList = herd.Where(a => a.Weaned == false).ToList();

			if (breedFemales.Count() == 0)
			{
				if (sucklingList.Count > 0)
				{
					Summary.WriteWarning(this, String.Format("No breeding females to assign sucklings for herd ({0})", HerdName));
				}
				return;
			}

			// gestation interval at smallest size generalised curve
			double minAnimalWeight = breedFemales[0].StandardReferenceWeight - ((1 - breedFemales[0].BreedParams.SRWBirth) * breedFemales[0].StandardReferenceWeight) * Math.Exp(-(breedFemales[0].BreedParams.AgeGrowthRateCoefficient * (breedFemales[0].BreedParams.MinimumAge1stMating * 30.4)) / (Math.Pow(breedFemales[0].StandardReferenceWeight, breedFemales[0].BreedParams.SRWGrowthScalar)));
			double IPIminsize = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (minAnimalWeight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient) * 30.64;
			// restrict minimum period between births
			IPIminsize = Math.Max(IPIminsize, breedFemales[0].BreedParams.GestationLength + 61);

			// assign calves to cows
			foreach (var suckling in sucklingList)
			{
				if (breedFemales.Count > 0)
				{
					breedFemales[0].DryBreeder = false;

					//Initialise female milk production in at birth so ready for sucklings to consume
					double milkTime = 15; // equivalent to mid month production
					double milkProduction = breedFemales[0].BreedParams.MilkPeakYield * breedFemales[0].Weight / breedFemales[0].NormalisedAnimalWeight * (Math.Pow(((milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay), breedFemales[0].BreedParams.MilkCurveSuckling)) * Math.Exp(breedFemales[0].BreedParams.MilkCurveSuckling * (1 - (milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay));
					breedFemales[0].MilkProduction = Math.Max(milkProduction, 0.0);
					breedFemales[0].MilkAmount = milkProduction * 30.4;

					// generalised curve
					double IPIcurrent = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (breedFemales[0].Weight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient) * 30.64;
					// restrict minimum period between births
					IPIcurrent = Math.Max(IPIcurrent, breedFemales[0].BreedParams.GestationLength + 61);

					breedFemales[0].NumberOfBirths = Convert.ToInt32((breedFemales[0].Age - suckling.Age - breedFemales[0].BreedParams.GestationLength - breedFemales[0].BreedParams.MinimumAge1stMating) / ((IPIcurrent + IPIminsize) / 2));

					//breedFemales[0].Parity = breedFemales[0].Age - suckling.Age - 9;
					// I removed the -9 as this would make it conception month not birth month
					breedFemales[0].AgeAtLastBirth = breedFemales[0].Age - suckling.Age;
					breedFemales[0].AgeAtLastConception = breedFemales[0].AgeAtLastBirth - breedFemales[0].BreedParams.GestationLength;

					// suckling mother set
					suckling.Mother = breedFemales[0];

					// check if a twin and if so apply next individual to same mother.
					// otherwise remove this mother from the list
					if (randomGenerator.RandNo >= breedFemales[0].BreedParams.TwinRate)
					{
						breedFemales.RemoveAt(0);
					}
				}
				else
				{
					Summary.WriteWarning(this, String.Format("Insufficient breeding females to assign sucklings for herd ({0})", HerdName));
					return;
				}
			}

			// assing values for the remaining females who haven't just bred.
			foreach (var female in breedFemales)
			{
				female.DryBreeder = true;
				// generalised curve
				double IPIcurrent = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (breedFemales[0].Weight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient) * 30.64;
				// restrict minimum period between births
				IPIcurrent = Math.Max(IPIcurrent, breedFemales[0].BreedParams.GestationLength + 61);
				breedFemales[0].NumberOfBirths = Convert.ToInt32((breedFemales[0].Age - breedFemales[0].BreedParams.MinimumAge1stMating) / ((IPIcurrent + IPIminsize) / 2)) - 1;
				female.AgeAtLastBirth = breedFemales[0].Age - 12;
			}
		}

		/// <summary>An event handler to perform herd breeding </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalBreeding")]
		private void OnWFAnimalBreeding(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.BreedParams.Name == HerdName).ToList();

			// get list of all individuals of breeding age and condition
			// grouped by location
			var breeders = from ind in herd
						   where
						   (ind.Gender == Sex.Male & ind.Age >= ind.BreedParams.MinimumAge1stMating) ^
						   (ind.Gender == Sex.Female &
						   ind.Age >= ind.BreedParams.MinimumAge1stMating &
						   ind.Weight >= (ind.BreedParams.MinimumSize1stMating * ind.StandardReferenceWeight)
						   )
						   group ind by ind.Location into grp
						   select grp;

			// for each location where parts of this herd are located
			foreach (var location in breeders)
			{
				// check for births of all pregnant females.
				foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
				{
					if (female.BirthDue)
					{
						int numberOfNewborn = (female.CarryingTwins) ? 2 : 1;
						for (int i = 0; i < numberOfNewborn; i++)
						{
							// Determine if the offspring died during pregancy from conception to after birth
							// This is currently only performed at time of birth rather than monthly during pregnancy
							// and so does not reflect changes in female intake etc after dead of foetus.

							// this now occurs monthly at the ned of this breeding method.
							//if (randomGenerator.RandNo > female.BreedParams.PrenatalMortality)
							//{
							object newCalf = null;
							bool isMale = (randomGenerator.RandNo > 0.5);
							if (isMale)
							{
								newCalf = new RuminantMale();
							}
							else
							{
								newCalf = new RuminantFemale();
							}
							Ruminant newCalfRuminant = newCalf as Ruminant;
							newCalfRuminant.Age = 0;
							newCalfRuminant.HerdName = female.HerdName;
							newCalfRuminant.BreedParams = female.BreedParams;
							newCalfRuminant.Gender = (isMale) ? Sex.Male : Sex.Female;
							newCalfRuminant.ID = ruminantHerd.NextUniqueID;
							newCalfRuminant.Location = female.Location;
							newCalfRuminant.Mother = female;
//							newCalfRuminant.Number = 1;
							newCalfRuminant.SetUnweaned();
							// calf weight from  Freer
							newCalfRuminant.Weight = female.BreedParams.SRWBirth * female.StandardReferenceWeight * (1 - 0.33 * (1 - female.Weight / female.StandardReferenceWeight));
							newCalfRuminant.HighWeight = newCalfRuminant.Weight;
							newCalfRuminant.SaleFlag = Common.HerdChangeReason.Born;
							ruminantHerd.AddRuminant(newCalfRuminant);

							// add to sucklings
							female.SucklingOffspring.Add(newCalfRuminant);

							//}
						}
						female.UpdateBirthDetails();
					}
				}

				// Conception
				// check if males and females of breeding condition are together
				if (location.GroupBy(a => a.Gender).Count() == 2)
				{
					// servicing rate
					int maleCount = location.Where(a => a.Gender == Sex.Male).Count();
					int femaleCount = location.Where(a => a.Gender == Sex.Female).Count();
					double matingsPossible = maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30;
					double maleLimiter = Math.Max(1.0, matingsPossible/ femaleCount);

					foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
					{
						//TODO: ensure enough time since last calf
						if (!female.IsPregnant & !female.IsLactating)
						{
							// calculate conception
							double conceptionRate = ConceptionRate(female) * maleLimiter;
							if (randomGenerator.RandNo <= conceptionRate)
							{
								female.UpdateConceptionDetails(randomGenerator.RandNo > female.BreedParams.TwinRate, conceptionRate);
							}
						}
					}
				}

				// determine all foetus and newborn mortality.
				foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
				{
					if (female.IsPregnant)
					{
						// calculate foetus and newborn mortality 
						// total mortality / gestation months to get monthly mortality
						
						// TODO: check if need to be done before births to get last month mortality
						if(randomGenerator.RandNo > female.BreedParams.PrenatalMortality/female.BreedParams.GestationLength)
						{
							female.OneOffspringDies();
						}
					}
				}


			}
		}

		/// <summary>
		/// Calculate conception rate for a female
		/// </summary>
		/// <param name="female">Female to calculate conception rate for</param>
		/// <returns></returns>
		private double ConceptionRate(RuminantFemale female)
		{
			double rate = 0;
			switch (female.NumberOfBirths)
			{
				case 0:
					// first mating
					if(female.BreedParams.MinimumAge1stMating >= 24)
					{
						// 1st mated at 24 months or older
						rate = female.BreedParams.ConceptionRateAsymptote[1] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[1] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[1]));
					}
					else if(female.BreedParams.MinimumAge1stMating >= 12)
					{
						// 1st mated between 12 and 24 months
						double rate24 = female.BreedParams.ConceptionRateAsymptote[1] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[1] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[1]));
						double rate12 = female.BreedParams.ConceptionRateAsymptote[0] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[0] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[0]));
						rate = (rate12 + rate24) / 2;
						// Not sure what the next code was doing in old version
						//Concep_rate = ((730 - Anim_concep(rumcat)) * temp1 + (Anim_concep(rumcat) - 365) * temp2) / 365 ' interpolate between 12 & 24 months
					}
					break;
				case 1:
					// second offspring mother
					rate = female.BreedParams.ConceptionRateAsymptote[2] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[2] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[2]));
					break;
				default:
					// females who have had more then one birth (twins should could as one birth)
					if(female.WeightAtConception > female.BreedParams.CriticalCowWeight * female.StandardReferenceWeight)
					{
						rate = female.BreedParams.ConceptionRateAsymptote[3] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[3] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[3]));
					}
					break;
			}
			return rate;
		}

	}
}
