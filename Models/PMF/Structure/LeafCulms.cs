using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Utilities;
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
	[ViewName("UserInterface.Views.PropertyView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class LeafCulms : Model
	{
		/// <summary>Link to the plant model.</summary>
		[Link]
		private Plant plant = null;

		/// <summary>The parent leaf class</summary>
		[Link]
		SorghumLeaf leaf = null;

		/// <summary> Tillering Method that uses a fixed number of tillers</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private ITilleringMethod fixedTillering = null;

		/// <summary> Tillering Method that manages number of tillers dynamically</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		private ITilleringMethod dynamicTillering = null;

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
		private IFunction leafNumSeed = null;
		
		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction leafInitRate = null;
		
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
		private IFunction leafNoAtEmergence = null;

		/// <summary> Subsequent tillers are slightly smaller - adjust that size using a percentage</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		public IFunction VerticalTillerAdjustment = null;

		/// <summary> Maximum values that Subsequent tillers can be adjusted</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		public IFunction MaxVerticalTillerAdjustment = null;

		/// <summary> LeafAppearance Rate</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		public LinearInterpolationFunction LeafAppearanceRate = null;

		[Link(Type = LinkType.Child, ByName = true)]
		private IFunction numberOfLeaves = null;

		private ITilleringMethod tillering => leaf.TilleringMethod == 0 ? fixedTillering : dynamicTillering;

		/// <summary>
		/// 
		/// </summary>
		public double dltLeafNo;

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
		public double dltPotentialLAI { get; set; }

		/// <summary>
		/// Wrapper around leaf.DltStressedLAI.
		/// </summary>
		public double dltStressedLAI { get; set; }

		/// <summary>
		/// Number of leaves?
		/// </summary>
		public double NLeaves { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		private List<double> leafNo;

		// All variables below here existed in old sorghum.

		/// <summary>
		/// fixme - start with lower-case.
		/// </summary>
		public List<Culm> Culms;

		/// <summary>
		/// Fixme
		/// </summary>
		private const int emergence = 3;

		/// <summary>Total TT required to get from emergence to floral init.</summary>
		[JsonIgnore]
		public double TTTargetFI { get; private set; }

		private CulmParams culmParams;

		/// <summary>
		/// Constructor.
		/// </summary>
		public LeafCulms()
		{
			Culms = new List<Culm>();
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
				NoSeed = leafNumSeed,
				InitRate = leafInitRate,
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
			};

			// Initialise Main
			Culms.Add(new Culm(0, culmParams));
			TTTargetFI = 0;


			leafNo = Enumerable.Repeat(0d, 13).ToList(); //new List<double>(phenology.Phases.Count);
			leafNo[emergence] = leafNoAtEmergence.Value();
			FinalLeafNo = 0;
			dltLeafNo = 0;
			NLeaves = 0;
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

		///// <summary>
		///// Update daily variables. Shouldn't be called manually.
		///// </summary>
		///// <param name="sender">Sender object.</param>
		///// <param name="e">Event arguments.</param>
		//[EventSubscribe("EndOfDay")]
		//private void UpdateVars(object sender, EventArgs e)
		//{
		//	if (!plant.IsAlive)
		//		return;

		//	stage = phenology.Stage;
		//	tillers = 0.0;
		//	for (int i = 0; i < Culms.Count; ++i)
		//	{
		//		Culms[i].UpdateVars();
		//		tillers += Culms[i].Proportion;
		//	}
		//	tillers--;

		//	double gpla = lai / plant.SowingData.Population * 10000;
		//	double spla = leaf.SenescedLai / plant.SowingData.Population * 10000;
		//	tpla = gpla + spla;

		//	int iStage = (int)Math.Floor(stage);
		//	if (iStage < leafNo.Count)
		//		leafNo[(int)Math.Floor(stage)] += dltLeafNo;
		//	NLeaves = leafNo.Sum();
		//}
		
		/// <summary>Event from sequencer telling us to do our potential growth.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("DoPotentialPlantGrowth")]
		private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
		{
			if (plant.IsAlive)
			{
				FinalLeafNo = numberOfLeaves.Value();
				Culms.ForEach(c => c.FinalLeafNo = FinalLeafNo);

				dltLeafNo = tillering.CalcLeafNumber();

				//CalcPotentialArea();
				dltPotentialLAI = tillering.CalcPotentialLeafArea();
				double expStress = expansionStress.Value();
				dltStressedLAI = dltPotentialLAI * expStress;
				Culms.ForEach(c=> c.DltStressedLAI = c.DltLAI * expStress);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public double CalculateActualArea()
		{
            double actualLAI = tillering.CalcActualLeafArea(dltStressedLAI);
			Culms.ForEach(c => c.TotalLAI = c.TotalLAI + c.DltStressedLAI);

			return actualLAI;
        }
	}
}
