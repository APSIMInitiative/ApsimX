// -----------------------------------------------------------------------
// <copyright file="Class1.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Models.GrazPlan
{
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TSupplementModel contains a list of supplement "stores", each of which
    /// consists of an amount and a description of a supplementary feed as a
    /// TSupplement from GrazSupp.cs.
    /// Key properties and methods of TSupplementModel are:
    /// * Count         Number of valid supplement stores
    /// * Store[]       Supplement attributes for each store (zero-offset)
    /// * StoredKg[]    Amount in each supplement store (zero-offset)
    /// * AddToStore    Adds an amount of supplement to a store. Used in setting
    /// up, in "buy" events, and in storing conserved fodder.
    /// * FeedOut       Transfers feed from a store. Used in the "feed" event.
    /// Notes:
    /// 1.  All TSupplementModels have a "fodder" store.  This is where material
    /// passed to the supplement component as a result of fodder conservation
    /// should go.
    /// 2.  If the composition parameters in the AddToStore method (DMP, DMD, MEDM,
    /// CP, DG, EE and ADIP2CP) are set to zero, the class will use the default
    /// value for the supplement name from grazSUPP.  Using this feature with a
    /// supplement not named in grazSUPP will result in an wheat being used.
    /// </summary>
    [Serializable]
    public class TSupplementModel : TSupplementLibrary
    {
        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        private class PaddockInfo
        {
            /// <summary>
            /// The name
            /// </summary>
            public string Name;
            /// <summary>
            /// The padd identifier
            /// </summary>
            public int PaddId;
            /// <summary>
            /// The suppt fed
            /// </summary>
            public TSupplementRation SupptFed;   // Entry N is the supplement fed out N days ago
        }

        /// <summary>
        /// The paddocks
        /// </summary>
        private PaddockInfo[] Paddocks = new PaddockInfo[0];
        /// <summary>
        /// The f curr padd supp
        /// </summary>
        private TSupplement FCurrPaddSupp;

        /// <summary>
        /// The default
        /// </summary>
        private const int DEFAULT = -1;
        /// <summary>
        /// The roughage
        /// </summary>
        private const int ROUGHAGE = 0;
        /// <summary>
        /// The fodder
        /// </summary>
        private const string FODDER = "fodder";

        /// <summary>
        /// Gets or sets the spoilage time.
        /// </summary>
        /// <value>
        /// The spoilage time.
        /// </value>
        public double SpoilageTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TSupplementModel"/> class.
        /// </summary>
        public TSupplementModel()
            : base()
        {
            AddToStore(0.0, FODDER, ROUGHAGE);
            fSuppts[0].DM_Propn = 0.85;
            FCurrPaddSupp = new TSupplement();
        }

        /// <summary>
        /// Adds the paddock.
        /// </summary>
        /// <param name="paddId">The padd identifier.</param>
        /// <param name="paddName">Name of the padd.</param>
        public void AddPaddock(int paddId, string paddName)
        {
            int idx = Paddocks.Length;
            Array.Resize(ref Paddocks, idx + 1);
            Paddocks[idx] = new PaddockInfo();
            Paddocks[idx].Name = paddName.ToLower();
            Paddocks[idx].PaddId = paddId;
            Paddocks[idx].SupptFed = new TSupplementRation();
        }

        /// <summary>
        /// Clears the paddock list.
        /// </summary>
        public void ClearPaddockList()
        {
            Paddocks = new PaddockInfo[0];
        }

        /// <summary>
        /// Gets the paddock count.
        /// </summary>
        /// <value>
        /// The paddock count.
        /// </value>
        public int PaddockCount
        {
            get
            {
                return Paddocks.Length;
            }
        }

        /// <summary>
        /// Paddocks the name.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <returns></returns>
        public string PaddockName(int idx)
        {
            return Paddocks[idx].Name;
        }

        /// <summary>
        /// Finds the fed suppt.
        /// </summary>
        /// <param name="paddIdx">Index of the padd.</param>
        /// <param name="amount">The amount.</param>
        /// <returns></returns>
        private TSupplement FindFedSuppt(int paddIdx, ref double amount)
        {
            if (paddIdx >= 0 && paddIdx < Paddocks.Length)
            {
                amount = Paddocks[paddIdx].SupptFed.TotalAmount;
                Paddocks[paddIdx].SupptFed.AverageSuppt(out FCurrPaddSupp);
            }
            else
                amount = 0.0;
            return FCurrPaddSupp;
        }

        /// <summary>
        /// Gets the fed suppt.
        /// </summary>
        /// <param name="paddName">Name of the padd.</param>
        /// <param name="amount">The amount.</param>
        /// <returns></returns>
        public TSupplement GetFedSuppt(string paddName, ref double amount)
        {
            return FindFedSuppt(PaddockIndexOf(paddName), ref amount);
        }

        /// <summary>
        /// Gets the fed suppt.
        /// </summary>
        /// <param name="paddIdx">The padd identifier.</param>
        /// <param name="amount">The amount.</param>
        /// <returns></returns>
        public TSupplement GetFedSuppt(int paddIdx, ref double amount)
        {
            return FindFedSuppt(paddIdx, ref amount);
        }

        /// <summary>
        /// Paddocks the index of.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private int PaddockIndexOf(string name)
        {
            for (int i = 0; i < Paddocks.Length; i++)
                if (Paddocks[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>
        /// Paddocks the index of.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns></returns>
        private int PaddockIndexOf(int ID)
        {
            for (int i = 0; i < Paddocks.Length; i++)
                if (Paddocks[i].PaddId == ID)
                    return i;
            return -1;
        }

        /// <summary>
        /// Adds an amount of a supplement to a store.
        /// * If the store name already exists in the FStores array, the method adds
        /// the supplement to that store.  Otherwise a new store is created.
        /// * The DMP, DMD, MEDM, CP, DG, EE and ADIP2CP parameters may be set to zero,
        /// in which case the default values for the supplement name are used.
        /// Defaults are taken from the current store if the name is already defined,
        /// and from grazSUPP.PAS otherwise.  If defaults cannot be found for a name,
        /// wheat is used as the default composition.
        /// </summary>
        /// <param name="suppKg">Amount (kg fresh weight) of the supplement to be included in the store.</param>
        /// <param name="suppName">Name of the supplement.</param>
        /// <param name="roughage">The roughage.</param>
        /// <param name="DMP">Proportion of the fresh weight which is dry matter   kg/kg FW</param>
        /// <param name="DMD">Dry matter digestibility of the supplement           kg/kg DM</param>
        /// <param name="MEDM">Metabolisable energy content of dry matter           MJ/kg DM</param>
        /// <param name="CP">Crude protein content                                kg/kg DM</param>
        /// <param name="DG">Degradability of the crude protein                   kg/kg CP</param>
        /// <param name="EE">Ether-extractable content                            kg/kg DM</param>
        /// <param name="ADIP2CP">Ratio of acid detergent insoluble protein to CP      kg/kg CP</param>
        /// <param name="phos">Phosphorus content                                   kg/kg DM</param>
        /// <param name="sulf">Sulphur content                                      kg/kg DM</param>
        /// <param name="ashAlk">Ash alkalinity                                       mol/kg DM</param>
        /// <param name="maxPass">Maximum passage rate                                 0-1</param>
        /// <returns>
        /// Index of the supplement in the store
        /// </returns>
        public int AddToStore(double suppKg, string suppName, int roughage = DEFAULT, double DMP = 0.0, double DMD = 0.0,
                   double MEDM = 0.0, double CP = 0.0, double DG = 0.0, double EE = 0.0, double ADIP2CP = 0.0,
                   double phos = 0.0, double sulf = 0.0, double ashAlk = 0.0, double maxPass = 0.0)
        {
            int idx = IndexOf(suppName);

            TSupplement addSupp = new TSupplement(suppName);

            if (idx >= 0)                             // Work out the composition of the supplement being added
                addSupp.Assign(this[idx]);
            else
                addSupp.DefaultFromName();
            addSupp.sName = suppName.ToLower();

            if (roughage == ROUGHAGE)                 // Override the default composition as required
                addSupp.IsRoughage = true;
            else if (roughage != DEFAULT)
                addSupp.IsRoughage = false;

            if (DMP > 0.0)
                addSupp.DM_Propn = DMP;
            if (DMD > 0.0)
                addSupp.DM_Digestibility = DMD;
            if (MEDM > 0.0)
                addSupp.ME_2_DM = MEDM;
            if (CP > 0.0)
                addSupp.CrudeProt = CP;
            if (DG > 0.0)
                addSupp.DgProt = DG;
            if (EE > 0.0)
                addSupp.EtherExtract = EE;
            if (ADIP2CP > 0.0)
                addSupp.ADIP_2_CP = ADIP2CP;
            if (phos > 0.0)
                addSupp.Phosphorus = phos;
            if (sulf > 0.0)
                addSupp.Sulphur = sulf;
            if (ashAlk > 0.0)
                addSupp.AshAlkalinity = ashAlk;
            if (maxPass > 0.0)
                addSupp.MaxPassage = maxPass;

            if (DMD > 0.0 && MEDM == 0.0)
                addSupp.ME_2_DM = addSupp.DefaultME2DM();
            else if (DMD == 0.0 && MEDM > 0.0)
                addSupp.DM_Digestibility = addSupp.DefaultDMD();

            return AddToStore(suppKg, addSupp);
        }

        /// <summary>
        /// Adds to store.
        /// </summary>
        /// <param name="suppKg">The supp kg.</param>
        /// <param name="suppComp">The supp comp.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Supplement submodel: cannot combine roughage and concentrate, both named  + suppComp.sName</exception>
        public int AddToStore(double suppKg, TSupplement suppComp)
        {
            int suppIdx = IndexOf(suppComp.sName);
            if (suppIdx < 0)
                suppIdx = Add(suppComp, suppKg);
            else if (suppKg > 0.0)
            {
                if (suppComp.IsRoughage != fSuppts[suppIdx].IsRoughage)
                    throw new Exception("Supplement submodel: cannot combine roughage and concentrate, both named " + suppComp.sName);
                SuppIntoRation(this, suppIdx, suppComp, suppKg);
            }
            return suppIdx;
        }

        /// <summary>
        /// Feeds the out.
        /// </summary>
        /// <param name="suppName">Name of the supp.</param>
        /// <param name="fedKg">The fed kg.</param>
        /// <param name="paddName">Name of the padd.</param>
        /// <exception cref="System.Exception">
        /// Supplement \ + suppName + \ not recognised
        /// or
        /// </exception>
        /// Paddock \ + paddName + \ not recognised
        public void FeedOut(string suppName, double fedKg, string paddName)
        {
            int iSupp = IndexOf(suppName);
            int iPadd = PaddockIndexOf(paddName);
            if (iSupp < 0)
                throw new Exception("Supplement \"" + suppName + "\" not recognised");
            else if (iPadd < 0)
                throw new Exception("Paddock \"" + paddName + "\" not recognised");

            Transfer(this, iSupp, Paddocks[iPadd].SupptFed, 0, fedKg);
        }

        /// <summary>
        /// Adds the fodder.
        /// </summary>
        /// <param name="destStore">The dest store.</param>
        /// <param name="fodderFW">The fodder fw.</param>
        /// <param name="DMP">The DMP.</param>
        /// <param name="DMD">The DMD.</param>
        /// <param name="NConc">The n conc.</param>
        /// <param name="PConc">The p conc.</param>
        /// <param name="SConc">The s conc.</param>
        /// <param name="ashAlk">The ash alk.</param>
        public void AddFodder(string destStore, double fodderFW, double DMP, double DMD, double NConc,
                              double PConc, double SConc, double ashAlk)
        {
            if (string.IsNullOrWhiteSpace(destStore))
                destStore = FODDER;

            double protDg = System.Math.Min(0.9, DMD + 0.1);
            double ADIP2CP = 0.19 * (1.0 - protDg);
            double EE = 0.02;
            double MEDM = TSupplement.ConvertDMD_To_ME2DM(DMD, true, EE);

            AddToStore(fodderFW, destStore, ROUGHAGE, DMP, DMD, MEDM, TSupplement.N2PROTEIN * NConc, protDg, EE, ADIP2CP,
                PConc, SConc, ashAlk, 0.0);
        }

        /// <summary>
        /// Blends the specified source store.
        /// </summary>
        /// <param name="srcStore">The source store.</param>
        /// <param name="transferKg">The transfer kg.</param>
        /// <param name="destStore">The dest store.</param>
        /// <exception cref="System.Exception">Supplement \ + srcStore + \ not recognised</exception>
        public void Blend(string srcStore, double transferKg, string destStore)
        {
            int iSrc = IndexOf(srcStore);
            if (iSrc < 0)
                throw new Exception("Supplement \"" + srcStore + "\" not recognised");

            transferKg = System.Math.Min(transferKg, this[iSrc].Amount);
            if (transferKg > 0.0)
            {
                int iDest = IndexOf(destStore);
                if (iDest < 0)
                {
                    TSupplement newSupp = new TSupplement();
                    newSupp.Assign(this[iSrc]);
                    newSupp.sName = destStore;
                    iDest = AddToStore(0.0, newSupp);
                }
                Transfer(this, iSrc, this, iDest, transferKg);
            }
        }

        /// <summary>
        /// Removes the suppt.
        /// </summary>
        /// <param name="paddIdx">Index of the padd.</param>
        /// <param name="suppKg">The supp kg.</param>
        /// <exception cref="System.Exception">Paddock not recognised</exception>
        private void RemoveSuppt(int paddIdx, double suppKg)
        {
            if (paddIdx < 0)
                throw new Exception("Paddock not recognised");

            TSupplementRation ration = Paddocks[paddIdx].SupptFed;

            if (suppKg > 0.0 && ration.TotalAmount > 0.0)
            {
                double removePropn = suppKg / ration.TotalAmount;
                if (removePropn < 1.0 - 1.0e-6)
                    ration.TotalAmount -= suppKg;
                else
                    ration.TotalAmount = 0.0;
            }
        }

        /// <summary>
        /// Removes the eaten.
        /// </summary>
        /// <param name="paddName">Name of the padd.</param>
        /// <param name="suppKg">The supp kg.</param>
        public void RemoveEaten(string paddName, double suppKg)
        {
            RemoveSuppt(PaddockIndexOf(paddName), suppKg);
        }

        /// <summary>
        /// Removes the eaten.
        /// </summary>
        /// <param name="paddId">The padd identifier.</param>
        /// <param name="suppKg">The supp kg.</param>
        public void RemoveEaten(int paddId, double suppKg)
        {
            RemoveSuppt(PaddockIndexOf(paddId), suppKg);
        }

        /// <summary>
        /// Completes the time step.
        /// </summary>
        public void CompleteTimeStep()
        {
            int lastDay = (int)(SpoilageTime - 1.0e-6);

            for (int iPadd = 0; iPadd < Paddocks.Length; iPadd++)
            {
                TSupplementRation ration = Paddocks[iPadd].SupptFed;
                if (ration.Count > 0)
                {
                    for (int iDay = System.Math.Min(lastDay, ration.Count); iDay > 0; iDay--)
                    {
                        ration[iDay] = ration[iDay - 1];
                        ration[iDay].Amount = ration[iDay - 1].Amount * (SpoilageTime - iDay) / (SpoilageTime - (iDay - 1));
                    }
                    ration[0].Amount = 0.0;
                }
            }
        }

        /// <summary>
        /// Supps the into ration.
        /// </summary>
        /// <param name="ration">The ration.</param>
        /// <param name="idx">The index.</param>
        /// <param name="supp">The supp.</param>
        /// <param name="amount">The amount.</param>
        private void SuppIntoRation(TSupplementRation ration, int idx, TSupplement supp, double amount)
        {
            if (amount > 0.0)
            {
                double propn = amount / (amount + ration[idx].Amount);
                ration[idx].Mix(supp, ration[idx], propn);
                ration[idx].Amount += amount;
            }
        }

        /// <summary>
        /// Transfers the specified source.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="srcIdx">Index of the source.</param>
        /// <param name="dest">The dest.</param>
        /// <param name="destIdx">Index of the dest.</param>
        /// <param name="amount">The amount.</param>
        /// <exception cref="System.Exception">Invalid transfer of feed</exception>
        private void Transfer(TSupplementRation src, int srcIdx, TSupplementRation dest, int destIdx, double amount)
        {
            if (srcIdx < 0 || srcIdx >= src.Count || destIdx < 0 || destIdx > dest.Count)
                throw new Exception("Invalid transfer of feed");
            if (src[srcIdx].Amount > 0)
            {
                if (amount > 0.0)
                {
                    if (destIdx < dest.Count)
                        SuppIntoRation(dest, destIdx, src[srcIdx], amount);
                    else
                    {
                        TSupplement copy = new TSupplement();
                        copy.Assign(src[srcIdx]);
                        dest.Add(copy, amount);
                    }
                    src[srcIdx].Amount -= amount;
                }
            }
            else
                dest[destIdx].Amount = 0; 
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class StoreType : SuppInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public double Stored { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SuppToStockType : SuppInfo
    {
        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        public string Paddock { get; set; }
        /// <summary>
        /// Gets or sets the amount of ration (kg).
        /// </summary>
        /// <value>
        /// The amount of ration (kg).
        /// </value>
        public double Amount { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SuppEatenType
    {
        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        public string Paddock { get; set; }
        /// <summary>
        /// Gets or sets the amount of ration eaten (kg).
        /// </summary>
        /// <value>
        /// The amount of ration eaten (kg).
        /// </value>
        public double Eaten { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class BuySuppType
    {
        /// <summary>
        /// Gets or sets the supplement name.
        /// </summary>
        /// <value>
        /// The supplement name.
        /// </value>
        public string Supplement { get; set; }
        /// <summary>
        /// Gets or sets the amount of supplement eaten (kg).
        /// </summary>
        /// <value>
        /// The amount of supplement eaten (kg).
        /// </value>
        public double Amount { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class FeedSuppType
    {
        /// <summary>
        /// Gets or sets the supplement name.
        /// </summary>
        /// <value>
        /// The supplement name.
        /// </value>
        public string Supplement { get; set; }
        /// <summary>
        /// Gets or sets the amount of supplement offered (kg).
        /// </summary>
        /// <value>
        /// The amount of supplement offered (kg).
        /// </value>
        public double Amount { get; set; }
        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        public string Paddock { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MixSuppType
    {
        /// <summary>
        /// Gets or sets the source supplement name.
        /// </summary>
        /// <value>
        /// The source supplement name.
        /// </value>
        public string Source { get; set; }
        /// <summary>
        /// Gets or sets the amount of supplement transferred (kg).
        /// </summary>
        /// <value>
        /// The amount of supplement transferred (kg).
        /// </value>
        public double Amount { get; set; }
        /// <summary>
        /// Gets or sets the destination supplement name.
        /// </summary>
        /// <value>
        /// The destination supplement name.
        /// </value>
        public string Destination { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConserveType
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name;
        /// <summary>
        /// Gets or sets the fresh weight (kg)
        /// </summary>
        /// <value>The fresh weight (kg)</value>
        public double FreshWt;
        /// <summary>
        /// Gets or sets the dry matter content of the supplement (kg/kg FW).
        /// </summary>
        /// <value>Dry matter content of the supplement (kg/kg)</value>
        public double DMContent;
        /// <summary>
        /// Gets or sets the dry matter digestibility of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Dry matter digestibiility of the supplement (kg/kg)</value>
        public double DMD;
        /// <summary>
        /// Gets or sets the phosphorus content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Phosphorus content of the supplement (kg/kg)</value>
        public double NConc;
        /// <summary>
        /// Gets or sets the nitrogen content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Nitrogen content of the supplement (kg/kg)</value>
        public double PConc;
        /// <summary>
        /// Gets or sets the sulfur content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Sulfur content of the supplement (kg/kg)</value>
        public double SConc;
        /// <summary>
        /// Gets or sets the ash alkalinity of the supplement (mol/kg DM).
        /// </summary>
        /// <value>Ash alkalinity of the supplement (mol/kg)</value>
        public double AshAlk;
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.SupplementView")]
    [PresenterName("UserInterface.Presenters.SupplementPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class Supplement : Model
    {

        /// <summary>
        /// The simulation
        /// </summary>
        [Link]
        Simulation Simulation = null;

        /// <summary>
        /// Link to the Stock component.
        /// </summary>
        [Link(IsOptional =true)]
        Stock Animals = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Supplement" /> class.
        /// </summary>
        public Supplement()
            : base()
        {
            theModel = new TSupplementModel();
        }

        /// <summary>
        /// The model
        /// </summary>
        private TSupplementModel theModel;
        /// <summary>
        /// The paddocks given
        /// </summary>
        private bool paddocksGiven;

        /// <summary>
        /// Time over which an amount of supplement placed in a paddock will become inaccessible to grazing stock
        /// Default value is 0.0, i.e. supplement only persists for the time step that it is fed out
        /// </summary>
        /// <value>
        /// The spoilage time in days
        /// </value>
        [Description("Time over which an amount of supplement placed in a paddock will become inaccessible to grazing stock")]
        [Units("d")]
        public double SpoilageTime
        {
            get
            {
                return theModel.SpoilageTime;
            }
            set
            {
                theModel.SpoilageTime = value;
            }
        }

        /// <summary>
        /// Attributes and initial amount in each supplement store
        /// </summary>
        /// <value>
        /// List of stores
        /// </value>
        [Description("Attributes and initial amount in each supplement store")]
        public StoreType[] stores
        {
            get
            {
                StoreType[] result = new StoreType[theModel.Count];
                for (int i = 0; i < theModel.Count; i++)
                {
                    result[i] = new StoreType();
                    result[i].Name = theModel[i].sName;
                    result[i].Stored = theModel[i].Amount;
                    result[i].IsRoughage = theModel[i].IsRoughage;
                    result[i].DMContent = theModel[i].DM_Propn;
                    result[i].DMD = theModel[i].DM_Digestibility;
                    result[i].MEContent = theModel[i].ME_2_DM;
                    result[i].CPConc = theModel[i].CrudeProt;
                    result[i].ProtDg = theModel[i].DgProt;
                    result[i].PConc = theModel[i].Phosphorus;
                    result[i].SConc = theModel[i].Sulphur;
                    result[i].EEConc = theModel[i].EtherExtract;
                    result[i].ADIP2CP = theModel[i].ADIP_2_CP;
                    result[i].AshAlk = theModel[i].AshAlkalinity;
                    result[i].MaxPassage = theModel[i].MaxPassage;
                }
                return result;
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                {
                    int jdx = theModel.AddToStore(value[i].Stored, value[i].Name);
                    theModel[jdx].IsRoughage = value[i].IsRoughage;
                    theModel[jdx].DM_Propn = value[i].DMContent;
                    theModel[jdx].DM_Digestibility = value[i].DMD;
                    theModel[i].ME_2_DM = value[i].MEContent;
                    theModel[jdx].CrudeProt = value[i].CPConc;
                    theModel[jdx].DgProt = value[i].ProtDg;
                    theModel[jdx].Phosphorus = value[i].PConc;
                    theModel[jdx].Sulphur = value[i].SConc;
                    theModel[jdx].EtherExtract = value[i].EEConc;
                    theModel[jdx].ADIP_2_CP = value[i].ADIP2CP;
                    theModel[jdx].AshAlkalinity = value[i].AshAlk;
                    theModel[jdx].MaxPassage = value[i].MaxPassage;
                    // RegisterNewStore(value[i].Name); // I don't think this is feasible under ApsimX
                }
            }
        }

        /// <summary>
        /// List of paddock names
        /// If the variable is not given, or if it has zero length, the component will autodetect paddocks
        /// by querying for modules that own the area variable
        /// </summary>
        /// <value>
        /// The list of paddocks
        /// </value>
        [Description("List of paddock names")]
        public string[] PaddockList
        {
            get
            {
                string[] result = new string[theModel.PaddockCount];
                for (int i = 0; i < theModel.PaddockCount; i++)
                    result[i] = theModel.PaddockName(i);
                return result;
            }
            set
            {
                theModel.ClearPaddockList();
                paddocksGiven = value.Length > 0;
                if (paddocksGiven)
                    for (int i = 0; i < value.Length; i++)
                        theModel.AddPaddock(i, value[i]);
            }
        }

        /// <summary>
        /// Number of stores
        /// </summary>
        /// <value>
        /// The number of stores
        /// </value>
        [Description("Number of supplement stores")]
        public int NoStores
        {
            get
            {
                return theModel.Count;
            }
        }

        /// <summary>
        /// Number of paddocks recognised by the component instance
        /// </summary>
        /// <value>
        /// The number of paddocks
        /// </value>
        [Description("Number of paddocks recognised by the component instance")]
        public int NoPaddocks
        {
            get
            {
                return theModel.PaddockCount;
            }
        }

        /// <summary>
        /// Name of each paddock recognised by the component instance
        /// </summary>
        /// <value>
        /// The list of paddock names
        /// </value>
        [Description("Name of each paddock recognised by the component instance")]
        public string[] PaddNames
        {
            get
            {
                string[] result = new string[theModel.PaddockCount];
                for (int i = 0; i < theModel.PaddockCount; i++)
                    if (string.IsNullOrWhiteSpace(theModel.PaddockName(i)))
                        result[i] = "(null)";
                    else
                        result[i] = theModel.PaddockName(i);
                return result;
            }
        }

        /// <summary>
        /// Amount of supplement currently accessible to stock in each paddock recognised by the component instance
        /// </summary>
        /// <value>
        /// The list of supplement amounts in each paddock
        /// </value>
        [Description("Amount of supplement currently accessible to stock in each paddock recognised by the component instance")]
        public double[] PaddAmounts
        {
            get
            {
                double[] result = new double[theModel.PaddockCount];
                for (int i = 0; i < theModel.PaddockCount; i++)
                    result[i] = theModel[i].Amount;
                return result;
            }
        }

        /// <summary>
        /// Amount and attributes of supplementary feed present in each paddock
        /// </summary>
        /// <value>
        /// The list of amount and attributes of supplementary feed present in each paddock
        /// </value>
        [Description("Amount and attributes of supplementary feed present in each paddock")]
        public SuppToStockType[] SuppToStock
        {
            get
            {
                SuppToStockType[] result = new SuppToStockType[theModel.PaddockCount];
                for (int i = 0; i < theModel.PaddockCount; i++)
                {
                    result[i] = new SuppToStockType();
                    double amount = 0.0;
                    TSupplement supp = theModel.GetFedSuppt(i, ref amount);
                    result[i].Paddock = theModel.PaddockName(i);
                    result[i].Amount = amount;
                    result[i].IsRoughage = supp.IsRoughage;
                    result[i].DMContent = supp.DM_Propn;
                    result[i].DMD = supp.DM_Digestibility;
                    result[i].MEContent = supp.ME_2_DM;
                    result[i].CPConc = supp.CrudeProt;
                    result[i].ProtDg = supp.DgProt;
                    result[i].PConc = supp.Phosphorus;
                    result[i].SConc = supp.Sulphur;
                    result[i].EEConc = supp.EtherExtract;
                    result[i].ADIP2CP = supp.ADIP_2_CP;
                    result[i].AshAlk = supp.AshAlkalinity;
                    result[i].MaxPassage = supp.MaxPassage;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the <see cref="StoreType"/> with the specified supp name.
        /// </summary>
        /// <value>
        /// The <see cref="StoreType"/>.
        /// </value>
        /// <param name="suppName">Name of the supp.</param>
        /// <returns></returns>
        public StoreType this[string suppName]
        {
            get
            {
                int i = theModel.IndexOf(suppName);
                if (i < 0)
                    return null;
                StoreType result = new StoreType();
                result.Name = theModel[i].sName;
                result.Stored = theModel[i].Amount;
                result.IsRoughage = theModel[i].IsRoughage;
                result.DMContent = theModel[i].DM_Propn;
                result.DMD = theModel[i].DM_Digestibility;
                result.MEContent = theModel[i].ME_2_DM;
                result.CPConc = theModel[i].CrudeProt;
                result.ProtDg = theModel[i].DgProt;
                result.PConc = theModel[i].Phosphorus;
                result.SConc = theModel[i].Sulphur;
                result.EEConc = theModel[i].EtherExtract;
                result.ADIP2CP = theModel[i].ADIP_2_CP;
                result.AshAlk = theModel[i].AshAlkalinity;
                result.MaxPassage = theModel[i].MaxPassage;
                return result;
            }
        }

        /// <summary>
        /// Gets the <see cref="TSupplementItem"/> with the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="TSupplementItem"/>.
        /// </value>
        /// <param name="idx">The index.</param>
        /// <returns></returns>
        public TSupplementItem this[int idx]
        {
            get
            {
                if (idx >= 0 && idx < theModel.Count)
                    return theModel[idx];
                else
                    return null;
            }
        }

        /// <summary>
        /// Runs at the start of the simulation
        /// Sets up the list of paddocks, if that hasn't been provided explicitly
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.Exception">Invalid AribtrationMethod selected</exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (!paddocksGiven)
            {
                theModel.AddPaddock(-1, "");
                int paddId = 0;
                foreach (Zone zone in Apsim.FindAll(Simulation, typeof(Zone)))
                    if (zone.Area > 0.0)
                        theModel.AddPaddock(paddId++, zone.Name);
            }
        }

        /// <summary>
        /// Simulation has completed.
        /// Clear values from this run, so they don't carry over into the next
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (!paddocksGiven)
                theModel.ClearPaddockList();
            theModel.Clear();
        }

        /// <summary>
        /// Performs every-day calculations - end of day processes
        /// Determine the amount of supplementary feed eaten
        /// This event determines the amount of supplementary feed eaten by livestock and removes
        /// it from the amount present in each paddock. It then computes ''spoilage'' of supplement
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            if (Animals != null)
            {
                // get the supplement eaten from the Stock component
                TSupplementEaten[] eaten = Animals.SuppEaten;

                for (int Idx = 0; Idx < eaten.Length; Idx++)
                    theModel.RemoveEaten(eaten[Idx].paddock, eaten[Idx].eaten);
            }
            theModel.CompleteTimeStep();
        }

        /// <summary>
        /// Notifies the component that an amount of forage has been conserved
        /// </summary>
        /// <param name="conserved">Describes the conserved forage.</param>
        [EventSubscribe("Conserve")]
        private void OnConserve(ConserveType conserved)
        {
            Conserve(conserved.Name, conserved.FreshWt, conserved.DMContent, conserved.DMD,
                conserved.NConc, conserved.PConc, conserved.SConc, conserved.AshAlk);
        }

        /// <summary>
        /// Conserves the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="freshWt">The fresh wt.</param>
        /// <param name="DMContent">Content of the dm.</param>
        /// <param name="DMD">The DMD.</param>
        /// <param name="NConc">The n conc.</param>
        /// <param name="PConc">The p conc.</param>
        /// <param name="SConc">The s conc.</param>
        /// <param name="AshAlk">The ash alk.</param>
        public void Conserve(string name, double freshWt, double DMContent, double DMD,
                double NConc, double PConc, double SConc, double AshAlk)
        {
            theModel.AddFodder(name, freshWt, DMContent, DMD, NConc, PConc, SConc, AshAlk);
        }

        /// <summary>
        /// Called to buy new supplements into the store
        /// </summary>
        /// <param name="purchase">Specifies the supplement and amount being purchased.</param>
        [EventSubscribe("Buy")]
        private void OnBuy(BuySuppType purchase)
        {
            Buy(purchase.Amount, purchase.Supplement);
        }

        /// <summary>
        /// Buys the specified amount.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="supplement">The supplement.</param>
        public void Buy(double amount, string supplement)
        {
            theModel.AddToStore(amount, supplement);
        }

        /// <summary>
        /// Called to feed a supplement from the store
        /// </summary>
        /// <param name="feed">Specifies the supplement and amount being offered.</param>
        [EventSubscribe("Feed")]
        private void OnFeed(FeedSuppType feed)
        {
            Feed(feed.Supplement, feed.Amount, feed.Paddock);
        }

        /// <summary>
        /// Feeds the specified supplement.
        /// </summary>
        /// <param name="supplement">The supplement.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="paddock">The paddock.</param>
        public void Feed(string supplement, double amount, string paddock)
        {
            theModel.FeedOut(supplement, amount, paddock);
        }

        /// <summary>
        /// Called to buy mix supplements in the store
        /// </summary>
        /// <param name="mix">Specifies the source and destination supplements, and the amount being mixed.</param>
        [EventSubscribe("Mix")]
        private void OnMix(MixSuppType mix)
        {
            Mix(mix.Source, mix.Amount, mix.Destination);
        }

        /// <summary>
        /// Mixes the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="destination">The destination.</param>
        public void Mix(string source, double amount, string destination)
        {
            theModel.Blend(source, amount, destination);
        }

        /// <summary>
        /// Adds the specified supp name.
        /// </summary>
        /// <param name="suppName">Name of the supp.</param>
        /// <returns></returns>
        public int Add(string suppName)
        {
            int iDefSuppNo = TSupplementLibrary.DefaultSuppConsts.IndexOf(suppName);
            return theModel.AddToStore(0.0, TSupplementLibrary.DefaultSuppConsts[iDefSuppNo]);
        }

        /// <summary>
        /// Deletes the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        public void Delete(int idx)
        {
            theModel.Delete(idx);
        }
    }
}
