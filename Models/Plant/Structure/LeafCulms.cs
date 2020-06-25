using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.PMF.Struct
{
	/// <summary>
	/// LeafCulms model ported from LeafCulms and LeafCulms_Fixed in
	/// the apsim classic sorghum model.
	/// </summary>
	/// <remarks>
	/// # TODO:
	/// - Implement constants as IFunctions.
	/// - Fix case to match style guidelines.
	/// </remarks>
	[Serializable]
	[ValidParent(ParentType = typeof(Plant))]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class LeafCulms : Model
	{
		/// <summary>
		/// Link to the plant model.
		/// </summary>
		[Link]
		private Plant plant = null;

		/// <summary>
		/// Link to phenology model.
		/// </summary>
		[Link]
		private Phenology phenology = null;

		/// <summary>
		/// Link to leaf model.
		/// </summary>
		[Link]
		private SorghumLeaf leaf = null;

		/// <summary>
		/// Link to summary.
		/// </summary>
		[Link]
		private Summary summary = null;

		/// <summary>
		/// Link to weather.
		/// </summary>
		[Link]
		private IWeather weather = null;

		/// <summary>
		/// Vertical Offset for Largest Leaf Calc.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aMaxVert = null;

		/// <summary>
		/// Additional Vertical Offset for each additional Tiller.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aTillerVert = null;

		/// <summary>
		/// Expansion stress.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction expansionStress = null;

		/// <summary>
		/// The Initial Appearance rate for phyllocron.
		/// </summary>
		/// <remarks>
		/// TODO: copy InitialAppearanceRate from CulmStructure.
		/// </remarks>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction appearanceRate1 = null;
		
		/// <summary>
		/// The Final Appearance rate for phyllocron.
		/// </summary>
		/// <remarks>
		/// TODO: copy FinalAppearanceRate from CulmStructure.
		/// </remarks>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction appearanceRate2 = null;

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// fixme: temporarily making this protected so we don't get an
		/// unused variable warning.
		/// </remarks>
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

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction slaMax = null;

		[Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
		private IFunction leafAreaCalcTypeSwitch = null;

		/// <summary>bellCurveParams[0]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction a0 = null;

		/// <summary>bellCurveParams[1]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction a1 = null;

		/// <summary>bellCurveParams[2]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction b0 = null;

		/// <summary>bellCurveParams[3]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction b1 = null;

		/// <summary>largestLeafParams[0]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aMaxA = null;

		/// <summary>largestLeafParams[1]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aMaxB = null;

		/// <summary>largestLeafParams[2]</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction aMaxC = null;

		/// <summary>
		/// Propensity to tiller.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction tilleringPropensity = null;

		/// <summary>
		/// Tiller supply/demand ratio ratio slope.
		/// </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction tillerSdSlope = null;

		/// <summary>
		/// SLA Range.
		/// </summary>
		[Units("%")]
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction tillerSlaBound = null;

		/// <summary>
		/// If true, tillering will be calculated on the fly.
		/// Otherwise, number of tillers must be supplied via fertile
		/// tiller number in the sowing rule.
		/// </summary>
		[Description("Dynamic tillering enabled")]
		public bool DynamicTillering { get; set; }

		/// <summary>
		/// Number of tillers added.
		/// </summary>
		private double tillersAdded;

		/// <summary>
		/// 
		/// </summary>
		private double tillers;

		/// <summary>
		/// Phenological stage, updated at end-of-day.
		/// </summary>
		private double stage;

		/// <summary>
		/// 
		/// </summary>
		private double dltLeafNo;

		/// <summary>
		/// Final leaf number.
		/// </summary>
		public double FinalLeafNo { get; set; }

		/// <summary>
		/// Leaf number.
		/// </summary>
		public double LeafNo { get { return Culms[0].CurrentLeafNo; } }

		/// <summary>
		/// Wrapper around leaf.DltPotentialLAI
		/// </summary>
		private double dltPotentialLAI
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
		private double dltStressedLAI
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
		private double sla;

		/// <summary>
		/// Leaf area index.
		/// </summary>
		private double lai { get { return leaf.LAI; } }

		/// <summary>
		/// Number of leaves?
		/// </summary>
		public double NLeaves { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		private List<double> leafNo;

		/// <summary>
		/// Total plant leaf area.
		/// </summary>
		private double tpla;

		// All variables below here existed in old sorghum.

		/// <summary>
		/// fixme - start with lower-case.
		/// </summary>
		private List<Culm> Culms;

		/// <summary>
		/// Vertical adjustment.
		/// </summary>
		private double verticalAdjustment;

		/// <summary>
		/// Number of calculated tillers?
		/// </summary>
		private double calculatedTillers;

		/// <summary>
		/// Tiller supply factor.
		/// </summary>
		private double supply;

		/// <summary>
		/// Tiller demand factor.
		/// </summary>
		private double demand;

		/// <summary>
		/// Moving average of daily radiation during leaf 5 expansion.
		/// </summary>
		[Units("R/oCd")]
		private List<double> radiationValues;

		/// <summary>
		/// Don't think this is used anywhere.
		/// </summary>
		/// <remarks>
		/// fixme: temporarily making this protected so it doesn't trigger an
		/// unused variable warning.
		/// </remarks>
		protected double avgRadiation;

		/// <summary>
		/// Don't think this is used anywhere.
		/// </summary>
		/// <remarks>
		/// fixme: temporarily making this protected so it doesn't trigger an
		/// unused variable warning.
		/// </remarks>
		protected double thermalTimeCount;

		/// <summary>
		/// Fixme
		/// </summary>
		private const int emergence = 3;

		/// <summary>
		/// Fixme
		/// </summary>
		private const int endJuv = 4;

		/// <summary>
		/// Fixme
		/// </summary>
		private const int fi = 5;

		/// <summary>
		/// Fixme
		/// </summary>
		private const int flag = 6;

		/// <summary>
		/// Maximum LAI for tillering. Fixme - move to tree.
		/// </summary>
		private const double maxLAIForTillering = 0.325;

		/// <summary>
		/// Leaf number at which to start aggregation of <see cref="radiationValues"/>.
		/// </summary>
		private const int startThermalQuotientLeafNo = 3;

		/// <summary>
		/// FTN is calculated after the emergence of this leaf number.
		/// </summary>
		private const int endThermalQuotientLeafNo = 5;

		private const double smm2sm = 1e-6;

		/// <summary>
		/// Linear LAI.
		/// </summary>
		private double linearLAI;

		/// <summary>Total TT required to get from emergence to floral init.</summary>
		[JsonIgnore]
		public double TTTargetFI { get; private set; }

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
		public void Initialize()
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
				AppearanceRate3 = appearanceRate3,
				NoRateChange = noRateChange1,
				NoRateChange2 = noRateChange2,
				LeafNoAtEmergence = leafNoAtEmergence,
				MinLeafNo = minLeafNo,
				MaxLeafNo = maxLeafNo,
				TTEmergToFI = ttEmergToFI,
				Density = plant?.SowingData?.Population ?? 0,
				DltTT = dltTT,
				AMaxS = aMaxSlope,
				AMaxI = aMaxIntercept,
				A0 = a0,
				A1 = a1,
				B0 = b0,
				B1 = b1,
				AMaxA = aMaxA,
				AMaxB = aMaxB,
				AMaxC = aMaxC,
			};

			// Initialise Main

			Culms.Add(new Culm(0, culmParams));
			tillersAdded = 0;
			supply = 0;
			demand = 0;
			tillers = 0.0;
			TTTargetFI = 0;

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
		private void StartOfSim(object sender, EventArgs e)
		{
			Initialize();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		[EventSubscribe("Sowing")]
		private void OnSowing(object sender, EventArgs e)
		{
			culmParams.Density = plant.SowingData.Population;
		}

		/// <summary>
		/// Update daily variables. Shouldn't be called manually.
		/// </summary>
		/// <param name="sender">Sender object.</param>
		/// <param name="e">Event arguments.</param>
		[EventSubscribe("EndOfDay")]
		private void UpdateVars(object sender, EventArgs e)
		{
			if (!plant.IsAlive)
				return;

			stage = phenology.Stage;
			tillers = 0.0;
			for (int i = 0; i < Culms.Count; ++i)
			{
				Culms[i].UpdateVars();
				tillers += Culms[i].Proportion;
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
		private void OnPrePhenology(object sender, EventArgs e)
		{
			if (plant.IsAlive)
			{
				CalcLeafNo();
				TTTargetFI = GetTTFi();
			}
		}

		/// <summary>
		/// In old apsim, we update the stage variable of each plant part
		/// immediately after the call to phenology::development();
		/// 
		/// This stage variable can probably be refactored out now
		/// in favour of just using phenology.Stage, but I will
		/// do a separate pull request for this to ensure that the
		/// changes don't get masked by the effects of the tillering
		/// implementation.
		/// </summary>
		/// <param name="sender">Sender object.</param>
		/// <param name="e">Event arguments.</param>
		[EventSubscribe("PostPhenology")]
		private void PostPhenology(object sender, EventArgs e)
		{
			stage = phenology.Stage;
		}

		private double GetTTFi()
		{
			// fixme
			return (double)Apsim.Get(this, "[Phenology].TTEmergToFloralInit.Value()");
		}

		/// <summary>
		/// Calculate leaf number.
		/// </summary>
		public void CalcLeafNo()
		{
			if (DynamicTillering)
				CalcLeafNoDynamic();
			else
				CalcLeafNoFixed();
		}

		private void CalcLeafNoDynamic()
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
					Culms[0].CulmNo = 0;
					Culms[0].calculateLeafSizes();
					FinalLeafNo = Culms[0].FinalLeafNo;
				}
				double currentLeafNo = Culms[0].CurrentLeafNo;
				double dltLeafNoMainCulm = Culms[0].calcLeafAppearance();
				dltLeafNo = dltLeafNoMainCulm; //updates nLeaves
				double newLeafNo = Culms[0].CurrentLeafNo;

				CalcTillerNumber((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));
				CalcTillerAppearance((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));

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
		/// Calculate leaf number.
		/// </summary>
		private void CalcLeafNoFixed()
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
					Culms[0].CulmNo = 0;
					FinalLeafNo = Culms[0].FinalLeafNo;
					if (leafAreaCalcTypeSwitch == null)
						Culms[0].calculateLeafSizes();
					else
						Culms[0].CalcLeafSizes();
				}

				double currentLeafNo = Culms[0].CurrentLeafNo;
				double dltLeafNoMainCulm = Culms[0].calcLeafAppearance();
				dltLeafNo = dltLeafNoMainCulm; //updates nLeaves
				double newLeafNo = Culms[0].CurrentLeafNo;

				CalcTillerAppearance((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));

				for (int i = 1; i < Culms.Count; i++)
				{
					if (stage <= fi)
					{
						Culms[i].calcFinalLeafNo();
						if (leafAreaCalcTypeSwitch == null)
							Culms[i].calculateLeafSizes();
						else
							Culms[i].CalcLeafSizes();
					}
					Culms[i].calcLeafAppearance();
				}
			}
		}

		/// <summary>
		/// Calculate potential area. For now, this is manually called by leaf.
		/// </summary>
		public void CalcPotentialArea()
		{
			dltPotentialLAI = 0.0;
			dltStressedLAI = 0.0;

			if (stage >= emergence && stage <= flag)
			{
				// Fixme - this function can be simplified. Just need to double-check the effects of doing so.
				if (leafAreaCalcTypeSwitch != null)
				{
					dltPotentialLAI = Culms[0].LeafAreaPotBellShapeCurve(leafNo.ToArray());
					dltStressedLAI = CalcStressedLeafArea();
				}
				else
				{
					for (int i = 0; i < Culms.Count; ++i)
					{
						dltPotentialLAI += Culms[i].calcPotentialLeafArea();
						dltStressedLAI = CalcStressedLeafArea();        // dltPotentialLAI * totalStress(0-1)
					}
				}
			}
		}

		/// <summary>
		/// Calculate dltLAI. For now, this is manually called by leaf.
		/// </summary>
		public void AreaActual()
		{
			if (DynamicTillering)
				AreaActualDynamic();
			else
				AreaActualFixed();
		}

		private void AreaActualDynamic()
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
					double dltLaiAcc = Culms[0].LeafArea * stress;

					remainingLaiAvailable -= Culms[0].TotalLAI;//main culm existing Lai
					remainingLaiAvailable -= Culms[0].LeafArea * stress;//main culm - deltaLai (todays growth)

					// limit the decrease in tillering to 0.3 tillers per day
					double accProportion = 0.0;
					double maxTillerLoss = 0.4;
					for (int i = 1; i < Culms.Count; i++)
					{
						double laiExisting = Culms[i].TotalLAI * Culms[i].Proportion;
						double laiRequired = Culms[i].LeafArea * stress * Culms[i].Proportion;
						if (remainingLaiAvailable < laiExisting + laiRequired && accProportion < maxTillerLoss) //can't grow all this culm
						{
							double propn = Math.Max(0.0, (remainingLaiAvailable / (laiRequired + laiExisting)));
							double prevPRoportion = Culms[i].Proportion;
							propn = Math.Max(propn, prevPRoportion - maxTillerLoss);
							accProportion += propn;

							Culms[i].Proportion = Math.Min(propn, Culms[i].Proportion);//can't increase the proportion

							remainingLaiAvailable = 0;
							dltLaiAcc += Culms[i].LeafArea * Culms[i].Proportion;
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
		/// Calculate DltLAI. Called manually by leaf for now.
		/// </summary>
		private void AreaActualFixed()
		{
			if (stage >= endJuv && stage < flag)
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

		/// <summary>
		/// Calculate tiller appearance.
		/// </summary>
		/// <param name="newLeafNo"></param>
		/// <param name="currentLeafNo"></param>
		public void CalcTillerAppearance(int newLeafNo, int currentLeafNo)
		{
			if (DynamicTillering)
				CalcTillerAppearanceDynamic(newLeafNo, currentLeafNo);
			else
				CalcTillerAppearanceFixed(newLeafNo, currentLeafNo);
		}

		private void CalcTillerAppearanceDynamic(int newLeafNo, int currentLeafNo)
		{
			//if there are still more tillers to add
			//and the newleaf is greater than 3
			if (calculatedTillers > tillersAdded)
			{
				// calculate linear LAI
				double pltsPerMetre = plant.SowingData.Population * plant.SowingData.RowSpacing / 1000.0 * plant.SowingData.SkipDensityScale;
				linearLAI = pltsPerMetre * tpla / 10000.0;

				double laiToday = leaf.calcLAI();
				bool newLeaf = newLeafNo > currentLeafNo;
				//is it a new leaf, and it is > leaf 6 (leaf 5 appearance triggers initial tiller appeaance)
				//	bool newTiller = newLeaf && newLeafNo >= 6 && laiToday < maxLAIForTillering; 
				//bool newTiller = newLeaf && newLeafNo >= 6 && linearLAI < maxLAIForTillering; 
				bool newTiller = newLeafNo >= 6 && linearLAI < maxLAIForTillering;
				double fractionToAdd = dltTT.Value() / appearanceRate1.Value();
				fractionToAdd = 0.2;
				if (newTiller)
				{
					AddTiller(currentLeafNo, currentLeafNo - 1, fractionToAdd);
				}
			}
		}

		private void CalcTillerAppearanceFixed(int newLeafNo, int currentLeafNo)
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

					AddTiller(leafAppearance, currentLeafNo, fraction);
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
		/// Calculate tiller number.
		/// </summary>
		/// <param name="newLeafNo"></param>
		/// <param name="currentLeafNo"></param>
		public void CalcTillerNumber(int newLeafNo, int currentLeafNo)
		{
			//need to calculate the average R/oCd per day during leaf 5 expansion
			if (newLeafNo == startThermalQuotientLeafNo)
			{
				double avgradn = weather.Radn / dltTT.Value();
				radiationValues.Add(avgradn);
			}

			if (newLeafNo == endThermalQuotientLeafNo && currentLeafNo < endThermalQuotientLeafNo)
			{
				//the final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
				//Calc Supply = R/oCd * LA5 * Phy5
				double L5Area = Culms[0].calcIndividualLeafSize(5);
				double L9Area = Culms[0].calcIndividualLeafSize(9);
				double Phy5 = Culms[0].getLeafAppearanceRate(FinalLeafNo - Culms[0].CurrentLeafNo);

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
				AddTiller(3, 2, 1);
				AddTiller(4, 1, 1);
				AddTiller(5, 0, 1);
			}
			else if (calculatedTillers > 1) //add 3&4
			{
				AddTiller(4, 1, 1);
				AddTiller(5, 0, 1);
			}
			else if (calculatedTillers > 0)
			{
				AddTiller(4, 1, 1); //add 3
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
		private void AddTiller(double leafAtAppearance, double Leaves, double fractionToAdd)
		{
			double fraction = 1;
			if (calculatedTillers - tillersAdded < 1)
				fraction = calculatedTillers - tillersAdded;

			// get number if tillers 
			// add fractionToAdd 
			// if new tiller is neded add one
			// fraction goes to proportions
			double tillerFraction = Culms.Last().Proportion;
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
				newCulm.CulmNo = Culms.Count;
				newCulm.CurrentLeafNo = 0;//currentLeaf);
				verticalAdjustment = aMaxVert.Value() + (tillersAdded * aTillerVert.Value());
				newCulm.VertAdjValue = verticalAdjustment;
				newCulm.Proportion = fraction;
				newCulm.calcFinalLeafNo();
				newCulm.calcLeafAppearance();
				newCulm.calculateLeafSizes();
				Culms.Add(newCulm);
			}
			else
			{
				Culms.Last().Proportion = fraction;
			}
			tillersAdded += fractionToAdd;
		}

		/// <summary>
		/// Reduce tillers.
		/// </summary>
		/// <param name="reduceLAI"></param>
		public void ReduceTillers(double reduceLAI)
		{
			// when there is not enough biomass reduce the proportion of the last tiller to compensate
			double reduceArea = reduceLAI / plant.SowingData.Population * 10000;
			// get the area of the last tiller
			int nTillers = Culms.Count;
			int lastTiller = Culms.Count - 1;

			double propn = Culms[lastTiller].Proportion;
			if (propn == 0.0)
			{
				lastTiller--;
				propn = Culms[lastTiller].Proportion;
			}
			double culmArea = 0.0;
			// area of this tiller
			List<double> ls = Culms[lastTiller].LeafSizes;
			for (int i = 0; i < ls.Count; i++)
				culmArea += ls[i];

			//culmArea *= propn;
			// set the proportion
			double newPropn = (culmArea * propn - reduceArea) / culmArea;
			Culms[nTillers - 1].Proportion = newPropn;
		}

		/// <summary>
		/// Calculate stressed leaf area.
		/// </summary>
		/// <returns></returns>
		private double CalcStressedLeafArea()
		{
			return dltPotentialLAI * expansionStress.Value();
		}
	}
}
