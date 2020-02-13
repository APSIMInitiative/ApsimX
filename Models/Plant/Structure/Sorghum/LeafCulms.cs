using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.PMF.Struct.Sorghum
{
	/// <summary>
	/// LeafCulms model ported from old apsim's sorghum model.
	/// </summary>
	/// <remarks>
	/// # TODO:
	/// - Implement constants as IFunctions.
	/// - Fix case to match style guidelines.
	/// </remarks>
	[Serializable]
	[ValidParent(ParentType = typeof(Plant))]
	public class LeafCulms : Model
	{
		/// <summary>
		/// Link to the plant model.
		/// </summary>
		[Link]
		protected Plant plant = null;

		/// <summary>
		/// Link to phenology model.
		/// </summary>
		[Link]
		protected Phenology phenology = null;

		/// <summary>
		/// Link to leaf model.
		/// </summary>
		[Link]
		protected SorghumLeaf leaf = null;

		/// <summary>
		/// Link to summary.
		/// </summary>
		[Link]
		protected Summary summary = null;

		/// <summary>
		/// Link to weather.
		/// </summary>
		[Link]
		protected IWeather weather = null;

		/// <summary>
		/// Link to arbitrator for dltTT.
		/// </summary>
		[Link]
		protected SorghumArbitrator arbitrator = null;

		/// <summary>
		/// Vertical Offset for Largest Leaf Calc.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction aMaxVert = null;

		/// <summary>
		/// Additional Vertical Offset for each additional Tiller.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction aTillerVert = null;

		/// <summary>
		/// Expansion stress.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction expansionStress = null;

		/// <summary>
		/// The Initial Appearance rate for phyllocron.
		/// </summary>
		/// <remarks>
		/// TODO: copy InitialAppearanceRate from CulmStructure.
		/// </remarks>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction appearanceRate1 = null;
		
		/// <summary>
		/// The Final Appearance rate for phyllocron.
		/// </summary>
		/// <remarks>
		/// TODO: copy FinalAppearanceRate from CulmStructure.
		/// </remarks>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction appearanceRate2 = null;

		/// <summary>
		/// 
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction appearanceRate3 = null;

		/// <summary>
		/// Total accumulated TT from emergence to flag leaf.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction ttEmergToFlag = null;

		/// <summary>
		/// Corrects for other growing leaves.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction leafNoCorrection = null;

		/// <summary>
		/// Eqn 14 calc x0 - position of largest leaf.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aX0 = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction noSeed = null;
		
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction initRate = null;
		
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction noRateChange1 = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction noRateChange2 = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction minLeafNo = null;
		
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction maxLeafNo = null;
		
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction ttEmergToFI = null;
		
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction dltTT = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aMaxSlope = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aMaxIntercept = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction leafNoAtEmergence = null;

		/// <summary>
		/// Propensity to tiller.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction tilleringPropensity = null;

		/// <summary>
		/// Tiller supply/demand ratio ratio slope.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction tillerSdSlope = null;

		/// <summary>
		/// SLA Range.
		/// </summary>
		[Units("%")]
		[Link(Type = LinkType.Child, ByName = true)]
		protected IFunction tillerSlaBound = null;

		/// <summary>
		/// Number of tillers added.
		/// </summary>
		protected double tillersAdded;

		/// <summary>
		/// 
		/// </summary>
		protected double tillers;

		/// <summary>
		/// Phenological stage, updated at end-of-day.
		/// </summary>
		protected double stage;

		/// <summary>
		/// 
		/// </summary>
		protected double dltLeafNo;

		/// <summary>
		/// Final leaf number.
		/// </summary>
		public double FinalLeafNo { get; set; }

		/// <summary>
		/// Wrapper around leaf.DltPotentialLAI
		/// </summary>
		protected double dltPotentialLAI
		{
			get
			{
				return leaf.dltPotentialLAI;
			}
			set
			{
				leaf.dltPotentialLAI = value;
			}
		}

		/// <summary>
		/// Wrapper around leaf.DltStressedLAI.
		/// </summary>
		protected double dltStressedLAI
		{
			get
			{
				return leaf.dltStressedLAI;
			}
			set
			{
				leaf.dltStressedLAI = value;
			}
		}

		/// <summary>
		/// Specific leaf area.
		/// </summary>
		protected double sla;

		/// <summary>
		/// Leaf area index.
		/// </summary>
		protected double lai { get { return leaf.LAI; } }

		/// <summary>
		/// Number of leaves?
		/// </summary>
		public double NLeaves { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		protected List<double> leafNo;

		/// <summary>
		/// Total plant leaf area.
		/// </summary>
		protected double tpla;

		// All variables below here existed in old sorghum.

		/// <summary>
		/// fixme - start with lower-case.
		/// </summary>
		protected List<Culm> Culms;

		/// <summary>
		/// Vertical adjustment.
		/// </summary>
		protected double verticalAdjustment;

		/// <summary>
		/// Number of calculated tillers?
		/// </summary>
		protected double calculatedTillers;

		/// <summary>
		/// Tiller supply factor.
		/// </summary>
		protected double supply;

		/// <summary>
		/// Tiller demand factor.
		/// </summary>
		protected double demand;

		/// <summary>
		/// Moving average of daily radiation during leaf 5 expansion.
		/// </summary>
		[Units("R/oCd")]
		protected List<double> radiationValues;

		/// <summary>
		/// Don't think this is used anywhere.
		/// </summary>
		protected double avgRadiation;

		/// <summary>
		/// Don't think this is used anywhere.
		/// </summary>
		protected double thermalTimeCount;

		/// <summary>
		/// Fixme
		/// </summary>
		protected const int emergence = 3;

		/// <summary>
		/// Fixme
		/// </summary>
		protected const int endJuv = 4;

		/// <summary>
		/// Fixme
		/// </summary>
		protected const int fi = 5;

		/// <summary>
		/// Fixme
		/// </summary>
		protected const int flag = 6;

		/// <summary>
		/// Maximum LAI for tillering. Fixme - move to tree.
		/// </summary>
		protected const double maxLAIForTillering = 0.325;

		/// <summary>
		/// Leaf number at which to start aggregation of <see cref="radiationValues"/>.
		/// </summary>
		protected const int startThermalQuotientLeafNo = 3;

		/// <summary>
		/// FTN is calculated after the emergence of this leaf number.
		/// </summary>
		protected const int endThermalQuotientLeafNo = 5;

		/// <summary>
		/// Linear LAI.
		/// </summary>
		protected double linearLAI;

		private CulmParams culmParams;

		/// <summary>
		/// Constructor.
		/// </summary>
		public LeafCulms()
		{
			verticalAdjustment = 0.1;
			avgRadiation = 0.0;
			calculatedTillers = 0.0;
			thermalTimeCount = 0.0;

			linearLAI = 0.0;
			Culms = new List<Culm>();
			//initialize();
		}

		/// <summary>
		/// Individual leaf sizes.
		/// </summary>
		public double[] LeafSizes
		{
			get
			{
				return Culms[0].LeafSizes.ToArray();
			}
		}

		/// <summary>
		/// Remove all then add the first culm (which is the main culm).
		/// Shouldn't be called once sown.
		/// </summary>
		public virtual void initialize()
		{
			Culms.Clear();
			culmParams = new CulmParams()
			{
				LeafNoCorrection = leafNoCorrection,
				AX0 = aX0,
				NoSeed = noSeed,
				InitRate = initRate,
				AppearanceRate1 = appearanceRate1,
				AppearanceRate2 = appearanceRate2,
				NoRateChange = noRateChange1,
				NoRateChange2 = noRateChange2,
				MinLeafNo = minLeafNo,
				MaxLeafNo = maxLeafNo,
				TTEmergToFI = ttEmergToFI,
				Density = plant?.SowingData?.Population ?? 0,
				DltTT = dltTT,
				AMaxS = aMaxSlope,
				AMaxI = aMaxIntercept,
			};

			// Initialise Main

			Culms.Add(new Culm(0, culmParams));
			tillersAdded = 0;
			supply = 0;
			demand = 0;
			tillers = 0.0;

			//Culms[0]->initialize();

			leafNo = Enumerable.Repeat(0d, 13).ToList(); //new List<double>(phenology.Phases.Count);
			leafNo[emergence] = leafNoAtEmergence.Value();
			FinalLeafNo = 0;
			dltLeafNo = 0;
			tpla = 0;
			radiationValues = new List<double>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		[EventSubscribe("StartOfSimulation")]
		protected void StartOfSim(object sender, EventArgs e)
		{
			initialize();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		[EventSubscribe("Sowing")]
		protected void OnSowing(object sender, EventArgs e)
		{
			culmParams.Density = plant.SowingData.Population;
		}

		/// <summary>
		/// Update daily variables. Shouldn't be called manually.
		/// </summary>
		/// <param name="sender">Sender object.</param>
		/// <param name="e">Event arguments.</param>
		[EventSubscribe("EndOfDay")]
		protected void UpdateVars(object sender, EventArgs e)
		{
			if (!plant.IsAlive)
				return;

			stage = phenology.Stage;
			tillers = 0.0;
			for (int i = 0; i < Culms.Count; ++i)
			{
				Culms[i].UpdateVars();
				tillers += Culms[i].getProportion();
			}
			tillers--;

			double gpla = lai / plant.SowingData.Population * 10000;
			double spla = leaf.SenescedLai / plant.SowingData.Population * 10000;
			tpla = gpla + spla;

			int iStage = (int)Math.Floor(stage);
			if (iStage < leafNo.Count)
				leafNo[(int)Math.Floor(stage)] += dltLeafNo;
			NLeaves = leafNo.Sum();
		}
		
		/// <summary>
		/// Calculate leaf number.
		/// </summary>
		/// <param name="sender">Sender object.</param>
		/// <param name="e">Event arguments.</param>
		[EventSubscribe("PrePhenology")]
		protected void OnPrePhenology(object sender, EventArgs e)
		{
			if (plant.IsAlive)
				calcLeafNo();
		}

		/// <summary>
		/// Calculate leaf number.
		/// </summary>
		public virtual void calcLeafNo()
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
					Culms[0].calculateLeafSizes();
					FinalLeafNo = Culms[0].getFinalLeafNo();
				}
				double currentLeafNo = Culms[0].getCurrentLeafNo();
				double dltLeafNoMainCulm = Culms[0].calcLeafAppearance();
				dltLeafNo = dltLeafNoMainCulm; //updates nLeaves
				double newLeafNo = Culms[0].getCurrentLeafNo();

				calcTillerNumber((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));
				calcTillerAppearance((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));

				for (int i = 1; i < (int)Culms.Count; ++i)
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
		/// Calculate potential area. For now, this is manually called by leaf.
		/// </summary>
		public virtual void calcPotentialArea()
		{
			dltPotentialLAI = 0.0;
			dltStressedLAI = 0.0;

			if (phenology.Stage >= emergence && phenology.Stage <= flag)
			{
				for (int i = 0; i < Culms.Count; ++i)
				{
					dltPotentialLAI += Culms[i].calcPotentialLeafArea();
					dltStressedLAI = calcStressedLeafArea();        // dltPotentialLAI * totalStress(0-1)
				}
			}
		}

		/// <summary>
		/// Calculate dltLAI. For now, this is manually called by leaf.
		/// </summary>
		public virtual void areaActual()
		{
			// calculate new sla and see if it is less than slaMax
			// if so then reduce tillers

			double eTT = ttEmergToFlag.Value();

			if (stage > 4 && stage < 6) //   if(stage >= endJuv && stage < flag)?
			{
				double stress = expansionStress.Value();
				double dmGreen = leaf.Live.Wt;
				double dltDmGreen = leaf.potentialDMAllocation.Structural;

				if (dmGreen + dltDmGreen > 0.0)
					sla = (lai + dltStressedLAI) / (dmGreen + dltDmGreen) * 10000;  // (cm^2/g)

				// max SLN (thinnest leaf) possible using Reeves (1960's Kansas) SLA = 429.72 - 18.158 * LeafNo
				double maxSLA = 429.72 - 18.158 * (NLeaves + dltLeafNo);
				maxSLA *= ((100 - tillerSlaBound.Value()) / 100.0);     // sla bound vary 30 - 40%
				maxSLA = Math.Min(400, maxSLA);
				maxSLA = Math.Max(150, maxSLA);

				//		if(SLA > maxSLA && eTT < 400)	// vary 400  -- Try flag - 3 leaves  or 100dd
				bool moreToAdd = (tillersAdded < calculatedTillers) && (linearLAI < maxLAIForTillering);
				//		if(SLA > maxSLA && eTT < 500 && !moreToAdd)	// vary 400  -- Try flag - 3 leaves  or 100dd
				if (sla > maxSLA && (leafNo[0] + dltLeafNo) < FinalLeafNo && !moreToAdd)    // vary 400  -- Try flag - 3 leaves  or 100dd
				{
					double maxLaiPossible = maxSLA * (dmGreen + dltDmGreen) / 10000;
					double remainingLaiAvailable = maxLaiPossible;
					double dltLaiAcc = Culms[0].getLeafArea() * stress;

					remainingLaiAvailable -= Culms[0].getTotalLAI();//main culm existing Lai
					remainingLaiAvailable -= Culms[0].getLeafArea() * stress;//main culm - deltaLai (todays growth)

					// limit the decrease in tillering to 0.3 tillers per day
					double accProportion = 0.0;
					double maxTillerLoss = 0.4;
					for (int i = 1; i < Culms.Count; i++)
					{
						double laiExisting = Culms[i].getTotalLAI() * Culms[i].getProportion();
						double laiRequired = Culms[i].getLeafArea() * stress * Culms[i].getProportion();
						if (remainingLaiAvailable < laiExisting + laiRequired && accProportion < maxTillerLoss) //can't grow all this culm
						{
							double propn = Math.Max(0.0, (remainingLaiAvailable / (laiRequired + laiExisting)));
							double prevPRoportion = Culms[i].getProportion();
							propn = Math.Max(propn, prevPRoportion - maxTillerLoss);
							accProportion += propn;

							Culms[i].setProportion(Math.Min(propn, Culms[i].getProportion()));//can't increase the proportion

							remainingLaiAvailable = 0;
							dltLaiAcc += Culms[i].getLeafArea() * Culms[i].getProportion();
						}
						else
						{
							remainingLaiAvailable -= laiExisting + laiRequired;
							dltLaiAcc += laiRequired;
						}
					}
					leaf.DltLAI = dltLaiAcc;
				}
				else
					leaf.DltLAI = dltStressedLAI;


			}
			leaf.DltLAI = dltStressedLAI;

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

		/// <summary>
		/// Calculate tiller appearance.
		/// </summary>
		/// <param name="newLeafNo"></param>
		/// <param name="currentLeafNo"></param>
		public virtual void calcTillerAppearance(int newLeafNo, int currentLeafNo)
		{
			//if there are still more tillers to add
			//and the newleaf is greater than 3
			if (calculatedTillers > tillersAdded)
			{
				// calculate linear LAI
				double pltsPerMetre = plant.SowingData.Population * plant.SowingData.RowSpacing / 1000.0 * plant.SowingData.SkipRow;
				linearLAI = pltsPerMetre * tpla / 10000.0;

				double laiToday = leaf.calcLAI();
				bool newLeaf = newLeafNo > currentLeafNo;
				//is it a new leaf, and it is > leaf 6 (leaf 5 appearance triggers initial tiller appeaance)
				//	bool newTiller = newLeaf && newLeafNo >= 6 && laiToday < maxLAIForTillering; 
				//bool newTiller = newLeaf && newLeafNo >= 6 && linearLAI < maxLAIForTillering; 
				bool newTiller = newLeafNo >= 6 && linearLAI < maxLAIForTillering;
				double fractionToAdd = arbitrator.DltTT / appearanceRate1.Value();
				fractionToAdd = 0.2;
				if (newTiller)
				{
					addTiller(currentLeafNo, currentLeafNo - 1, fractionToAdd);
				}
			}
		}

		/// <summary>
		/// Calculate tiller number.
		/// </summary>
		/// <param name="newLeafNo"></param>
		/// <param name="currentLeafNo"></param>
		public void calcTillerNumber(int newLeafNo, int currentLeafNo)
		{
			//need to calculate the average R/oCd per day during leaf 5 expansion
			if (newLeafNo == startThermalQuotientLeafNo)
			{
				double avgradn = weather.Radn / arbitrator.DltTT;
				radiationValues.Add(avgradn);
			}

			if (newLeafNo == endThermalQuotientLeafNo && currentLeafNo < endThermalQuotientLeafNo)
			{
				//the final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
				//Calc Supply = R/oCd * LA5 * Phy5
				double L5Area = Culms[0].calcIndividualLeafSize(5);
				double L9Area = Culms[0].calcIndividualLeafSize(9);
				double Phy5 = Culms[0].getLeafAppearanceRate(FinalLeafNo - Culms[0].getCurrentLeafNo());

				supply = MathUtilities.Average(radiationValues) * L5Area * Phy5;
				//Calc Demand = LA9 - LA5
				demand = L9Area - L5Area;
				//double tilleringPropensity = 2.3;
				//double tillerSdSlope = 0.13;
				double sd = supply / demand;
				calculatedTillers = tilleringPropensity.Value() + tillerSdSlope.Value() * sd;
				calculatedTillers = Math.Max(calculatedTillers, 0.0);
				//	calculatedTillers = min(calculatedTillers, 5.0);

				summary.WriteMessage(this, $"Calculated Tiller Number = {calculatedTillers}");
				summary.WriteMessage(this, $"Calculated Supply = {supply}");
				summary.WriteMessage(this, $"Calculated Demand = {demand}");

				AddInitialTillers();
			}
		}

		/// <summary>
		/// Add initial tillers.
		/// </summary>
		public void AddInitialTillers()
		{
			//tiller emergence is more closely aligned with tip apearance, but we don't track tip, so will use ligule appearance
			//could also use Thermal Time calcs if needed
			//Environmental & Genotypic Control of Tillering in Sorghum ppt - Hae Koo Kim
			//T2=L3, T3=L4, T4=L5, T5=L6

			//logic to add new tillers depends on which tiller, which is defined by FTN (calculatedTillers)
			//2 tillers = T3 + T4
			//3 tillers = T2 + T3 + T4
			//4 tillers = T2 + T3 + T4 + T5
			//more than that is too many tillers - but will assume existing pattern for 3 and 4
			//5 tillers = T2 + T3 + T4 + T5 + T6

			//T3, T4, T2, T1, T5, T6

			//as the tiller calc requires leaf 5 to be fully expanded, we can add all tillers up to T5 immediately

			if (calculatedTillers > 2)  //add 2, & 3 & 4
			{
				addTiller(3, 2, 1);
				addTiller(4, 1, 1);
				addTiller(5, 0, 1);
			}
			else if (calculatedTillers > 1) //add 3&4
			{
				addTiller(4, 1, 1);
				addTiller(5, 0, 1);
			}
			else if (calculatedTillers > 0)
			{
				addTiller(4, 1, 1); //add 3
			}

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

		/// <summary>
		/// Add a tiller.
		/// </summary>
		/// <param name="leafAtAppearance"></param>
		/// <param name="Leaves"></param>
		/// <param name="fractionToAdd"></param>
		public void addTiller(double leafAtAppearance, double Leaves, double fractionToAdd)
		{
			double fraction = 1;
			if (calculatedTillers - tillersAdded < 1)
				fraction = calculatedTillers - tillersAdded;

			// get number if tillers 
			// add fractionToAdd 
			// if new tiller is neded add one
			// fraction goes to proportions
			double tillerFraction = Culms.Last().getProportion();
			//tillerFraction +=fractionToAdd;
			fraction = tillerFraction + fractionToAdd - Math.Floor(tillerFraction);
			//a new tiller is created with each new leaf, up the number of fertileTillers
			if (tillerFraction + fractionToAdd > 1)
			{
				Culm newCulm = new Culm(leafAtAppearance, culmParams);

				//bell curve distribution is adjusted horizontally by moving the curve to the left.
				//This will cause the first leaf to have the same value as the nth leaf on the main culm.
				//T3&T4 were defined during dicussion at initial tillering meeting 27/06/12
				//all others are an assumption
				//T2 = 3 Leaves
				//T3 = 4 Leaves
				//T4 = 5 leaves
				//T5 = 6 leaves
				//T6 = 7 leaves
				newCulm.setCulmNo(Culms.Count);
				newCulm.setCurrentLeafNo(0);//currentLeaf);
				verticalAdjustment = aMaxVert.Value() + (tillersAdded * aTillerVert.Value());
				newCulm.setVertLeafAdj(verticalAdjustment);
				newCulm.setProportion(fraction);
				newCulm.calcFinalLeafNo();
				newCulm.calcLeafAppearance();
				newCulm.calculateLeafSizes();
				Culms.Add(newCulm);
			}
			else
			{
				Culms.Last().setProportion(fraction);
			}
			tillersAdded += fractionToAdd;
		}

		/// <summary>
		/// Reduce tillers.
		/// </summary>
		/// <param name="reduceLAI"></param>
		public virtual void reduceTillers(double reduceLAI)
		{
			// when there is not enough biomass reduce the proportion of the last tiller to compensate
			double reduceArea = reduceLAI / plant.SowingData.Population * 10000;
			// get the area of the last tiller
			int nTillers = Culms.Count;
			int lastTiller = Culms.Count - 1;

			double propn = Culms[lastTiller].getProportion();
			if (propn == 0.0)
			{
				lastTiller--;
				propn = Culms[lastTiller].getProportion();
			}
			double culmArea = 0.0;
			// area of this tiller
			List<double> ls = Culms[lastTiller].LeafSizes;
			for (int i = 0; i < ls.Count; i++)
				culmArea += ls[i];

			//culmArea *= propn;
			// set the proportion
			double newPropn = (culmArea * propn - reduceArea) / culmArea;
			Culms[nTillers - 1].setProportion(newPropn);
		}

		/// <summary>
		/// Calculate stressed leaf area.
		/// </summary>
		/// <returns></returns>
		private double calcStressedLeafArea()
		{
			return dltPotentialLAI * expansionStress.Value();
		}
	}
}
