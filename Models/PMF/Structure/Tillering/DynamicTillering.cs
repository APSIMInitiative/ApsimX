using Models.Core;
using Models.PMF.Interfaces;
using System;

namespace Models.PMF.Struct
{
    /// <summary>
    /// This is a tillering method to control the number of tillers and leaf area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LeafCulms))]
    public class DynamicTillering : Model, ITilleringMethod
    {
		/// <summary> Culms on the leaf </summary>
		[Link] 
		public LeafCulms culms = null;

		/// <summary> Calculate number of leaves</summary>
		public double CalcLeafNumber()
        {
			////overriding this function to retain existing code on Leaf class, but changing this one to manage tillers and leaves
			////first culm is the main one, that also provides timing for remaining tiller appearance

			//// calculate final leaf number up to initiation - would finalLeafNo need a different calc for each culm?
			//if (Culms.Count > 0 && stage >= emergence)
			//{
			//	//calc finalLeafNo for first culm (main), and then calc it's dltLeafNo
			//	//add any new tillers and then calc each tiller in turn
			//	if (stage <= fi)
			//	{
			//		Culms[0].calcFinalLeafNo();
			//		Culms[0].CulmNo = 0;
			//		Culms[0].calculateLeafSizes();
			//		FinalLeafNo = Culms[0].FinalLeafNo;
			//	}
			//	double currentLeafNo = Culms[0].CurrentLeafNo;
			//	double dltLeafNoMainCulm = Culms[0].calcLeafAppearance();
			//	dltLeafNo = dltLeafNoMainCulm; //updates nLeaves
			//	double newLeafNo = Culms[0].CurrentLeafNo;

			//	CalcTillerNumber((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));
			//	CalcTillerAppearance((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));

			//	for (int i = 1; i < (int)Culms.Count; ++i)
			//	{
			//		if (stage <= fi)
			//		{
			//			Culms[i].calcFinalLeafNo();
			//			Culms[i].calculateLeafSizes();
			//		}
			//		Culms[i].calcLeafAppearance();
			//	}
			//}
			return 0.0;
		}

		/// <summary> calculate the potential leaf area</summary>
		public double CalcPotentialLeafArea()
        {
			return 0.0;
		}

		/// <summary> calculate the actual leaf area</summary>
		public double CalcActualLeafArea(double dltStressedLAI)
		{
			// calculate new sla and see if it is less than slaMax
			// if so then reduce tillers
			return dltStressedLAI;
//			double eTT = ttEmergToFlag.Value();

//			if (stage > 4 && stage < 6) //   if(stage >= endJuv && stage < flag)?
//			{
//				double stress = expansionStress.Value();
//				double dmGreen = leaf.Live.Wt;
//				double dltDmGreen = leaf.potentialDMAllocation.Structural;

//				if (dmGreen + dltDmGreen > 0.0)
//					sla = (lai + dltStressedLAI) / (dmGreen + dltDmGreen) * 10000;  // (cm^2/g)

//				// max SLN (thinnest leaf) possible using Reeves (1960's Kansas) SLA = 429.72 - 18.158 * LeafNo
//				double maxSLA = 429.72 - 18.158 * (NLeaves + dltLeafNo);
//				maxSLA *= ((100 - tillerSlaBound.Value()) / 100.0);     // sla bound vary 30 - 40%
//				maxSLA = Math.Min(400, maxSLA);
//				maxSLA = Math.Max(150, maxSLA);

//				//		if(SLA > maxSLA && eTT < 400)	// vary 400  -- Try flag - 3 leaves  or 100dd
//				bool moreToAdd = (tillersAdded < calculatedTillers) && (linearLAI < maxLAIForTillering);
//				//		if(SLA > maxSLA && eTT < 500 && !moreToAdd)	// vary 400  -- Try flag - 3 leaves  or 100dd
//				if (sla > maxSLA && (leafNo[0] + dltLeafNo) < FinalLeafNo && !moreToAdd)    // vary 400  -- Try flag - 3 leaves  or 100dd
//				{
//					double maxLaiPossible = maxSLA * (dmGreen + dltDmGreen) / 10000;
//					double remainingLaiAvailable = maxLaiPossible;
//					double dltLaiAcc = Culms[0].LeafArea * stress;

//					remainingLaiAvailable -= Culms[0].TotalLAI;//main culm existing Lai
//					remainingLaiAvailable -= Culms[0].LeafArea * stress;//main culm - deltaLai (todays growth)

//					// limit the decrease in tillering to 0.3 tillers per day
//					double accProportion = 0.0;
//					double maxTillerLoss = 0.4;
//					for (int i = 1; i < Culms.Count; i++)
//					{
//						double laiExisting = Culms[i].TotalLAI * Culms[i].Proportion;
//						double laiRequired = Culms[i].LeafArea * stress * Culms[i].Proportion;
//						if (remainingLaiAvailable < laiExisting + laiRequired && accProportion < maxTillerLoss) //can't grow all this culm
//						{
//							double propn = Math.Max(0.0, (remainingLaiAvailable / (laiRequired + laiExisting)));
//							double prevPRoportion = Culms[i].Proportion;
//							propn = Math.Max(propn, prevPRoportion - maxTillerLoss);
//							accProportion += propn;

//							Culms[i].Proportion = Math.Min(propn, Culms[i].Proportion);//can't increase the proportion

//							remainingLaiAvailable = 0;
//							dltLaiAcc += Culms[i].LeafArea * Culms[i].Proportion;
//						}
//						else
//						{
//							remainingLaiAvailable -= laiExisting + laiRequired;
//							dltLaiAcc += laiRequired;
//						}
//					}
//					leaf.DltLAI = dltLaiAcc;
//}
//				else
//					leaf.DltLAI = dltStressedLAI;


//			}
//			leaf.DltLAI = dltStressedLAI;

			/* tLai = 0;
			for(unsigned i=0;i < Culms.size();i++)
			tLai += Culms[i]->getLeafArea()* Culms[i]->getProportion();
			double newSLA = (lai + tLai) / (dmGreen + dltDmGreen) * 10000;
			*/


			//dltLAI = tLai;

			//	// if there is not enough carbon to meet the daily delta lai then reduce the fraction of the last tiller until slaMax is met
			//   if(stage >= endJuv && stage < flag)
			//		{
			//		double maxDltLai = dltDmGreen * slaMax * smm2sm;
			//		if(maxDltLai < dltStressedLAI)
			//			{
			//			reduceTillers(dltStressedLAI - maxDltLai);
			//			dltLAI = Min(dltStressedLAI,dltDmGreen * slaMax * smm2sm);
			//			}
			//		}
			//   else dltLAI = dltStressedLAI;
			////   if (dltLAI < 0.001)dltLAI = 0.0;
		}

	}
}
