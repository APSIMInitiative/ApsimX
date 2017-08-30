using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant breeding activity</summary>
	/// <summary>This activity provides all functionality for ruminant breeding up until natural weaning</summary>
	/// <summary>It will be applied to the supplied herd if males and females are located together</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class RuminantActivityBreed : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
//		[Link]
//		ISummary Summary = null;

		/// <summary>
		/// Name of herd to breed
		/// </summary>
		[Description("Name of herd to breed")]
		public string HerdName { get; set; }

		/// <summary>
		/// The location of controlled mating settings
		/// </summary>
		private ControlledMatingSettings ControlledMatings { get; set; }

		/// <summary>
		/// Labour settings
		/// </summary>
		private List<LabourFilterGroupSpecified> labour { get; set; }

		/// <summary>
		/// Maximum conception rate for uncontrolled matings
		/// </summary>
		public double MaximumConceptionRateUncontrolled { get; set; }

		/// <summary>An event handler to allow us to initialise herd breeding status.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFInitialiseActivity")]
		private void OnWFInitialiseActivity(object sender, EventArgs e)
		{
			// Assignment of mothers was moved to RuminantHerd resource to ensure this is done even if no breeding activity

			// check for controlled mating settings
			ControlledMatings = Apsim.Children(this, typeof(ControlledMatingSettings)).FirstOrDefault() as ControlledMatingSettings;

			// get labour specifications
			labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
			if (labour.Count() == 0) labour = new List<LabourFilterGroupSpecified>();
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
						   (ind.Gender == Sex.Male & ind.Age >= ind.BreedParams.MinimumAge1stMating) ||
						   (ind.Gender == Sex.Female &
						   ind.Age >= ind.BreedParams.MinimumAge1stMating &
						   ind.Weight >= (ind.BreedParams.MinimumSize1stMating * ind.StandardReferenceWeight)
						   )
						   group ind by ind.Location into grp
						   select grp;

			// for each location where parts of this herd are located
			foreach (var location in breeders)
			{
				// determine all foetus and newborn mortality.
				foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
				{
					if (female.IsPregnant)
					{
						// calculate foetus and newborn mortality 
						// total mortality / (gestation months + 1) to get monthly mortality
						// done here before births to account for post birth motality as well..
						if (WholeFarm.RandomGenerator.NextDouble() < female.BreedParams.PrenatalMortality / (female.BreedParams.GestationLength + 1))
						{
							female.OneOffspringDies();
						}
					}
				}

				// check for births of all pregnant females.
				foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
				{
					if (female.BirthDue)
					{
						int numberOfNewborn = (female.CarryingTwins) ? 2 : 1;
						for (int i = 0; i < numberOfNewborn; i++)
						{
							// Foetal mortality is now performed each timestep at base of this method
							object newCalf = null;
							bool isMale = (WholeFarm.RandomGenerator.NextDouble() > 0.5);
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
							newCalfRuminant.Breed = female.BreedParams.Breed;
							newCalfRuminant.Gender = (isMale) ? Sex.Male : Sex.Female;
							newCalfRuminant.ID = ruminantHerd.NextUniqueID;
							newCalfRuminant.Location = female.Location;
							newCalfRuminant.Mother = female;
							newCalfRuminant.Number = 1;
							newCalfRuminant.SetUnweaned();
							// calf weight from  Freer
							newCalfRuminant.Weight = female.BreedParams.SRWBirth * female.StandardReferenceWeight * (1 - 0.33 * (1 - female.Weight / female.StandardReferenceWeight));
							newCalfRuminant.HighWeight = newCalfRuminant.Weight;
							newCalfRuminant.SaleFlag = HerdChangeReason.Born;
							ruminantHerd.AddRuminant(newCalfRuminant);

							// add to sucklings
							female.SucklingOffspring.Add(newCalfRuminant);
							// remove calf weight from female
							female.Weight -= newCalfRuminant.Weight;
						}
						female.UpdateBirthDetails();
					}
				}

				// uncontrolled conception
				if(ControlledMatings == null)
				{
					// check if males and females of breeding condition are together
					if (location.GroupBy(a => a.Gender).Count() == 2)
					{
						// servicing rate
						int maleCount = location.Where(a => a.Gender == Sex.Male).Count();
						int femaleCount = location.Where(a => a.Gender == Sex.Female).Count();
						double matingsPossible = maleCount * location.FirstOrDefault().BreedParams.MaximumMaleMatingsPerDay * 30;
						double maleLimiter = Math.Min(1.0, matingsPossible / femaleCount);

						foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
						{
							if (!female.IsPregnant && !female.IsLactating && (female.Age - female.AgeAtLastBirth)*30.4 >= female.BreedParams.MinimumDaysBirthToConception)
							{
								// calculate conception
								double conceptionRate = ConceptionRate(female) * maleLimiter;
								conceptionRate = Math.Min(conceptionRate, MaximumConceptionRateUncontrolled);
								if (WholeFarm.RandomGenerator.NextDouble() <= conceptionRate)
								{
									female.UpdateConceptionDetails(WholeFarm.RandomGenerator.NextDouble() < female.BreedParams.TwinRate, conceptionRate);
								}
							}
						}
					}
				}
				// controlled conception
				else if(ControlledMatings!=null && ControlledMatings.IsDueDate())
				{
					foreach (RuminantFemale female in location.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList())
					{
						if (!female.IsPregnant && !female.IsLactating && (female.Age - female.AgeAtLastBirth) * 30.4 >= female.BreedParams.MinimumDaysBirthToConception)
						{
							// calculate conception
							double conceptionRate = ConceptionRate(female);
							if (WholeFarm.RandomGenerator.NextDouble() <= conceptionRate)
							{
								female.UpdateConceptionDetails(WholeFarm.RandomGenerator.NextDouble() < female.BreedParams.TwinRate, conceptionRate);
							}
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

			bool isConceptionReady = false;
			if (female.Age >= female.BreedParams.MinimumAge1stMating && female.NumberOfBirths == 0)
			{
				isConceptionReady = true;
			}
			else
			{
				double IPIcurrent = female.BreedParams.InterParturitionIntervalIntercept * Math.Pow((female.Weight / female.StandardReferenceWeight), female.BreedParams.InterParturitionIntervalCoefficient) * 30.64;
				// calculate inter-parturition interval
				IPIcurrent = Math.Max(IPIcurrent, female.BreedParams.GestationLength * 30.4 + female.BreedParams.MinimumDaysBirthToConception); // 2nd param was 61
				double ageNextConception = female.AgeAtLastConception + (IPIcurrent / 30.4);
				// restrict minimum period between births
				// now done previously to avoid unneeded computation
//				if (ageNextConception - female.AgeAtLastBirth >= female.BreedParams.MinimumDaysBirthToConception)
//				{
					isConceptionReady = (female.Age >= ageNextConception);
//				}
			}

			// if first mating and of age or suffcient time since last birth/conception
			if(isConceptionReady)
			{
				// generalised curve
				switch (female.NumberOfBirths)
				{
					case 0:
						// first mating
						if (female.BreedParams.MinimumAge1stMating >= 24)
						{
							// 1st mated at 24 months or older
							rate = female.BreedParams.ConceptionRateAsymptote[1] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[1] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[1]));
						}
						else if (female.BreedParams.MinimumAge1stMating >= 12)
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
						// females who have had more then one birth (twins should count as one birth)
						if (female.WeightAtConception > female.BreedParams.CriticalCowWeight * female.StandardReferenceWeight)
						{
							rate = female.BreedParams.ConceptionRateAsymptote[3] / (1 + Math.Exp(female.BreedParams.ConceptionRateCoefficent[3] * female.WeightAtConception / female.StandardReferenceWeight + female.BreedParams.ConceptionRateIntercept[3]));
						}
						break;
				}
			}
			return rate / 100;
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
			ResourceRequestList = null;

			if (ControlledMatings != null && ControlledMatings.IsDueDate())
			{
				RuminantHerd ruminantHerd = Resources.RuminantHerd();
				List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.BreedParams.Name == HerdName).ToList();
				int head = herd.Count();
				double AE = herd.Sum(a => a.AdultEquivalent);

				if (head == 0) return null;

				// for each labour item specified
				foreach (var item in labour)
				{
					double daysNeeded = 0;
					switch (item.UnitType)
					{
						case LabourUnitType.Fixed:
							daysNeeded = item.LabourPerUnit;
							break;
						case LabourUnitType.perHead:
							daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
							break;
						case LabourUnitType.perAE:
							daysNeeded = Math.Ceiling(AE / item.UnitSize) * item.LabourPerUnit;
							break;
						default:
							throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
					}
					if (daysNeeded > 0)
					{
						if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
						ResourceRequestList.Add(new ResourceRequest()
						{
							AllowTransmutation = false,
							Required = daysNeeded,
							ResourceType = typeof(Labour),
							ResourceTypeName = "",
							ActivityModel = this,
							FilterDetails = new List<object>() { item }
						}
						);
					}
				}
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void DoActivity()
		{
			return;
		}

		/// <summary>
		/// Method to determine resources required for initialisation of this activity
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> GetResourcesNeededForinitialisation()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform initialisation of this activity.
		/// This will honour ReportErrorAndStop action but will otherwise be preformed regardless of resources available
		/// It is the responsibility of this activity to determine resources provided.
		/// </summary>
		public override void DoInitialisation()
		{
			return;
		}

		/// <summary>
		/// Resource shortfall event handler
		/// </summary>
		public override event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}
	}
}
