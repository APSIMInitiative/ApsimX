using DocumentFormat.OpenXml.Bibliography;
using Models.Core;
using Models.PMF.Interfaces;
using PdfSharpCore.Pdf.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		/// <returns></returns>
		public double CalcPotentialLeafArea()
        {
			return 0.0;
		}
	}
}
