using Models.Core;
using Models.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.PMF.Struct.Sorghum
{
	/// <summary>
	/// Fixed/static tillering model - port of LeafCulms_Fixed from old
	/// apsim's sorghum model.
	/// </summary>
	[Serializable]
	[ValidParent(ParentType = typeof(Plant))]
    public class LeafCulmsFixed : LeafCulms
	{
		private const double smm2sm = 1e-6;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction slaMax = null;

		/// <summary>
		/// Calculate leaf number.
		/// </summary>
		public override void calcLeafNo()
		{
			//overriding this function to retain existing code on Leaf class, but changing this one to manage tillers and leaves
			//first culm is the main one, that also provides timing for remaining tiller appearance

			// calculate final leaf number up to initiation - would finalLeafNo need a different calc for each culm?

			if (Culms.Count > 0 && stage >= emergence)
			{
				//calc finalLeafNo for first culm (main), and then calc it's dltLeafNo
				//add any new tillers and then calc each tiller in turn
				if (stage <= fi)
				{
					Culms[0].calcFinalLeafNo();
					Culms[0].setCulmNo(0);
					FinalLeafNo = Culms[0].getFinalLeafNo();
					Culms[0].calculateLeafSizes();

				}
				double currentLeafNo = Culms[0].getCurrentLeafNo();
				double dltLeafNoMainCulm = Culms[0].calcLeafAppearance();
				dltLeafNo = dltLeafNoMainCulm; //updates nLeaves
				double newLeafNo = Culms[0].getCurrentLeafNo();

				calcTillerAppearance((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));

				for (int i = 1; i < Culms.Count; i++)
				{
					if (stage <= fi)
					{
						Culms[i].calcFinalLeafNo();
						Culms[i].calculateLeafSizes();
					}
					Culms[i].calcLeafAppearance();
				}
			}
		}

		/// <summary>
		/// Calculate tiller appearance.
		/// </summary>
		/// <param name="newLeafNo"></param>
		/// <param name="currentLeafNo"></param>
		public override void calcTillerAppearance(int newLeafNo, int currentLeafNo)
		{
			//if there are still more tillers to add
			//and the newleaf is greater than 3
			if (plant.SowingData.BudNumber > tillersAdded)
			{
				//tiller emergence is more closely aligned with tip apearance, but we don't track tip, so will use ligule appearance
				//could also use Thermal Time calcs if needed
				//Environmental & Genotypic Control of Tillering in Sorghum ppt - Hae Koo Kim
				//T2=L3, T3=L4, T4=L5, T5=L6

				//logic to add new tillers depends on which tiller, which is defined by FTN (fertileTillerNo)
				//this should be provided at sowing  //what if fertileTillers == 1?
				//2 tillers = T3 + T4
				//3 tillers = T2 + T3 + T4
				//4 tillers = T2 + T3 + T4 + T5
				//more than that is too many tillers - but will assume existing pattern for 3 and 4
				//5 tillers = T2 + T3 + T4 + T5 + T6

				bool newLeaf = newLeafNo > currentLeafNo;
				bool newTiller = newLeaf && newLeafNo >= 3; //is it a new leaf, and it is leaf 3 or more
				if (newTiller)
				{
					//tiller 2 emergences with leaf 3, and then adds 1 each time
					//not sure what I'm supposed to do with tiller 1
					//if there are only 2 tillers, then t2 is not present - T3 & T4 are
					//if there is a fraction - between 2 and 3, 
					//this can be interpreted as a proportion of plants that have 2 and a proportion that have 3. 
					//to keep it simple, the fraction will be applied to the 2nd tiller
					double leafAppearance = Culms.Count + 2; //first culm added will equal 3
					double fraction = 1.0;
					if (plant.SowingData.BudNumber > 2 && plant.SowingData.BudNumber < 3 && leafAppearance < 4)
					{
						fraction = plant.SowingData.BudNumber % 1;// fmod(plant->getFtn(), 1);
						//tillersAdded += fraction;
					}
					else
					{
						if (plant.SowingData.BudNumber - tillersAdded < 1)
							fraction = plant.SowingData.BudNumber - tillersAdded;
						//tillersAdded += 1;
					}

					addTiller(leafAppearance, currentLeafNo, fraction);
					////a new tiller is created with each new leaf, up the number of fertileTillers
					//Culm* newCulm = new Culm(scienceAPI, plant, leafAppearance);
					//newCulm->readParams();
					//newCulm->setCurrentLeafNo(leafAppearance-1);
					//verticalAdjustment = aMaxVert;
					//verticalAdjustment += (Culms.size() - 1) * 0.05;
					//newCulm->setVertLeafAdj(verticalAdjustment);
					//newCulm->setProportion(fraction);
					//newCulm->calcFinalLeafNo();
					//newCulm->calcLeafAppearance();
					//newCulm->calculateLeafSizes();
					//Culms.push_back(newCulm);

					//bell curve distribution is adjusted horizontally by moving the curve to the left.
					//This will cause the first leaf to have the same value as the nth leaf on the main culm.
					//T3&T4 were defined during dicussion at initial tillering meeting 27/06/12
					//all others are an assumption
					//T2 = 3 Leaves
					//T3 = 4 Leaves
					//T4 = 5 leaves
					//T5 = 6 leaves
					//T6 = 7 leaves
				}
			}
		}

		/// <summary>
		/// Calculate DltLAI. Called manually by leaf for now.
		/// </summary>
		public override void areaActual()
		{
			if (phenology.Stage >= endJuv && phenology.Stage < flag)
			{
				double dltDmGreen = leaf.potentialDMAllocation.Structural;
				leaf.DltLAI = Math.Min(dltStressedLAI, dltDmGreen * slaMax.Value() * smm2sm);
				double stress = expansionStress.Value();

				if (leaf.Live.Wt + dltDmGreen > 0.0)
					sla = (lai + dltStressedLAI) / (leaf.Live.Wt + dltDmGreen) * 10000;  // (cm^2/g)
			}
			else
				leaf.DltLAI = dltStressedLAI;
		}
	}
}
