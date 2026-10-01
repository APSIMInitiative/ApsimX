// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;

namespace Models.GrazPlan
{
    /// <summary>
    /// SupplementModel contains a list of supplement "stores", each of which
    /// consists of an amount and a description of a supplementary feed as a
    /// Supplement from GrazSupp.cs.
    /// Key properties and methods of SupplementModel are:
    /// * Count         Number of valid supplement stores
    /// * Store[]       Supplement attributes for each store (zero-offset)
    /// * StoredKg[]    Amount in each supplement store (zero-offset)
    /// * AddToStore    Adds an amount of supplement to a store. Used in setting
    /// up, in "buy" events, and in storing conserved fodder.
    /// * FeedOut       Transfers feed from a store. Used in the "feed" event.
    /// Notes:
    /// 1.  All SupplementModels have a "fodder" store.  This is where material
    /// passed to the supplement component as a result of fodder conservation
    /// should go.
    /// 2.  If the composition parameters in the AddToStore method (DMP, DMD, MEDM,
    /// CP, DG, EE and ADIP2CP) are set to zero, the class will use the default
    /// value for the supplement name from grazSUPP.  Using this feature with a
    /// supplement not named in grazSUPP will result in an wheat being used.
    /// </summary>
    [Serializable]
    public partial class SupplementModel : SupplementLibrary
    {
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
        /// The paddocks
        /// </summary>
        private PaddockInfo[] Paddocks = new PaddockInfo[0];

        /// <summary>
        /// The FCurrPaddSupp
        /// </summary>
        private FoodSupplement currPaddSupp;

        /// <summary>
        /// Initializes a new instance of the <see cref="SupplementModel"/> class.
        /// </summary>
        public SupplementModel()
            : base()
        {
            AddToStore(0.0, FODDER, ROUGHAGE);
            SuppArray[0].DMPropn = 0.85;
            this.currPaddSupp = new FoodSupplement();
        }

        /// <summary>
        /// Gets or sets the spoilage time.
        /// </summary>
        /// <value>
        /// The spoilage time.
        /// </value>
        public double SpoilageTime { get; set; }

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
            Paddocks[idx].SupptFed = new SupplementRation();
        }

        /// <summary>
        /// Clears the paddock list.
        /// </summary>
        public void ClearPaddockList()
        {
            Paddocks = new PaddockInfo[0];
        }

        /// <summary>
        /// Gets the paddock name at an index
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <returns>The paddock name</returns>
        public string PaddockName(int idx)
        {
            return Paddocks[idx].Name;
        }

        /// <summary>
        /// Finds the amount of supplement fed in a paddock
        /// </summary>
        /// <param name="paddIdx">Index of the padd.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>The amount of supplement fed</returns>
        private FoodSupplement FindFedSuppt(int paddIdx, ref double amount)
        {
            if (paddIdx >= 0 && paddIdx < Paddocks.Length)
            {
                amount = Paddocks[paddIdx].SupptFed.TotalAmount;
                currPaddSupp = Paddocks[paddIdx].SupptFed.AverageSuppt();
            }
            else
                amount = 0.0;
            return this.currPaddSupp;
        }

        /// <summary>
        /// Gets the fed supplement for the paddock name.
        /// </summary>
        /// <param name="paddName">Name of the padd.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>The supplement object that was fed</returns>
        public FoodSupplement GetFedSuppt(string paddName, ref double amount)
        {
            return FindFedSuppt(PaddockIndexOf(paddName), ref amount);
        }

        /// <summary>
        /// Gets the fed suppt.
        /// </summary>
        /// <param name="paddIdx">The padd identifier.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>The supplement object that was fed</returns>
        public FoodSupplement GetFedSuppt(int paddIdx, ref double amount)
        {
            return FindFedSuppt(paddIdx, ref amount);
        }

        /// <summary>
        /// Returns the flag to feed supplement first that would
        /// have been entered when calling a feed() event.
        /// </summary>
        /// <param name="paddIdx">Paddock index</param>
        /// <returns></returns>
        public bool FeedSuppFirst(int paddIdx)
        {
            bool result = false;
            if (paddIdx >= 0 && paddIdx < Paddocks.Length)
            {
                result = Paddocks[paddIdx].FeedSuppFirst;
            }
            return result;
        }

        /// <summary>
        /// Index of the paddock by name
        /// </summary>
        /// <param name="name">The name of a paddock</param>
        /// <returns>The paddock index</returns>
        private int PaddockIndexOf(string name)
        {
            for (int i = 0; i < this.Paddocks.Length; i++)
                if (this.Paddocks[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>
        /// Index of the paddock by ID number
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The paddock index</returns>
        private int PaddockIndexOf(int id)
        {
            for (int i = 0; i < this.Paddocks.Length; i++)
                if (this.Paddocks[i].PaddId == id)
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
        /// <param name="dmp">Proportion of the fresh weight which is dry matter   kg/kg FW</param>
        /// <param name="dmd">Dry matter digestibility of the supplement           kg/kg DM</param>
        /// <param name="medm">Metabolisable energy content of dry matter          MJ/kg DM</param>
        /// <param name="cp">Crude protein content                                 kg/kg DM</param>
        /// <param name="dg">Degradability of the crude protein                    kg/kg CP</param>
        /// <param name="ee">Ether-extractable content                             kg/kg DM</param>
        /// <param name="adip2cp">Ratio of acid detergent insoluble protein to CP  kg/kg CP</param>
        /// <param name="phos">Phosphorus content                                  kg/kg DM</param>
        /// <param name="sulf">Sulphur content                                     kg/kg DM</param>
        /// <param name="ashAlk">Ash alkalinity                                    mol/kg DM</param>
        /// <param name="maxPass">Maximum passage rate                             0-1</param>
        /// <returns>
        /// Index of the supplement in the store
        /// </returns>
        public int AddToStore(double suppKg, string suppName, int roughage = DEFAULT, double dmp = 0.0, double dmd = 0.0,
                   double medm = 0.0, double cp = 0.0, double dg = 0.0, double ee = 0.0, double adip2cp = 0.0,
                   double phos = 0.0, double sulf = 0.0, double ashAlk = 0.0, double maxPass = 0.0)
        {
            int idx = IndexOf(suppName);

            FoodSupplement addSupp = new FoodSupplement(suppName);

            if (idx >= 0)                             // Work out the composition of the supplement being added
                addSupp.Assign(this[idx]);
            else
                addSupp.DefaultFromName();
            addSupp.Name = suppName.ToLower();

            if (roughage == ROUGHAGE)                 // Override the default composition as required
                addSupp.IsRoughage = true;
            else if (roughage != DEFAULT)
                addSupp.IsRoughage = false;

            if (dmp > 0.0)
                addSupp.DMPropn = dmp;
            if (dmd > 0.0)
                addSupp.DMDigestibility = dmd;
            if (medm > 0.0)
                addSupp.ME2DM = medm;
            if (cp > 0.0)
                addSupp.CrudeProt = cp;
            if (dg > 0.0)
                addSupp.DegProt = dg;
            if (ee > 0.0)
                addSupp.EtherExtract = ee;
            if (adip2cp > 0.0)
                addSupp.ADIP2CP = adip2cp;
            if (phos > 0.0)
                addSupp.Phosphorus = phos;
            if (sulf > 0.0)
                addSupp.Sulphur = sulf;
            if (ashAlk > 0.0)
                addSupp.AshAlkalinity = ashAlk;
            if (maxPass > 0.0)
                addSupp.MaxPassage = maxPass;

            if (dmd > 0.0 && medm == 0.0)
                addSupp.ME2DM = addSupp.DefaultME2DM();
            else if (dmd == 0.0 && medm > 0.0)
                addSupp.DMDigestibility = addSupp.DefaultDMD();

            return AddToStore(suppKg, addSupp);
        }

        /// <summary>
        /// Adds the supplement to the store.
        /// </summary>
        /// <param name="suppKg">The supp kg.</param>
        /// <param name="suppComp">The supp comp.</param>
        /// <returns>The supplement index</returns>
        /// <exception cref="System.Exception">Supplement submodel: cannot combine roughage and concentrate, both named  + suppComp.sName</exception>
        public int AddToStore(double suppKg, FoodSupplement suppComp)
        {
            int suppIdx = IndexOf(suppComp.Name);
            if (suppIdx < 0)
                suppIdx = Add(suppComp, suppKg);
            else if (suppKg > 0.0)
            {
                if (suppComp.IsRoughage != SuppArray[suppIdx].IsRoughage)
                    throw new Exception("Supplement submodel: cannot combine roughage and concentrate, both named " + suppComp.Name);
                SuppIntoRation(this, suppIdx, suppComp, suppKg);
            }
            return suppIdx;
        }

        /// <summary>
        /// Feeds the supplement out.
        /// </summary>
        /// <param name="suppName">Name of the supp.</param>
        /// <param name="fedKg">The fed kg.</param>
        /// <param name="paddName">Name of the padd.</param>
        /// <param name="feedSuppFirst">Feed the supplement before pasture consumption. Bail feeding.</param>
        /// <exception cref="System.Exception">
        /// Supplement \ + suppName + \ not recognised
        /// or
        /// </exception>
        /// Paddock \ + paddName + \ not recognised
        public void FeedOut(string suppName, double fedKg, string paddName, bool feedSuppFirst)
        {
            int suppIdx = IndexOf(suppName);
            int paddIdx = PaddockIndexOf(paddName);
            if (suppIdx < 0)
                throw new Exception("Supplement \"" + suppName + "\" not recognised");
            else if (paddIdx < 0)
                throw new Exception("Paddock \"" + paddName + "\" not recognised");

            Paddocks[paddIdx].FeedSuppFirst = feedSuppFirst;
            Transfer(this, suppIdx, Paddocks[paddIdx].SupptFed, 0, fedKg);
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
        public void AddFodder(string destStore, double fodderFW, double DMP, double DMD, double NConc, double PConc, double SConc, double ashAlk)
        {
            if (string.IsNullOrWhiteSpace(destStore))
                destStore = FODDER;

            double protDg = System.Math.Min(0.9, DMD + 0.1);
            double ADIP2CP = 0.19 * (1.0 - protDg);
            double EE = 0.02;
            double MEDM = FoodSupplement.ConvertDMDToME2DM(DMD, true, EE);

            AddToStore(fodderFW, destStore, ROUGHAGE, DMP, DMD, MEDM, FoodSupplement.N2PROTEIN * NConc, protDg, EE, ADIP2CP, PConc, SConc, ashAlk, 0.0);
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
            int srcIdx = IndexOf(srcStore);
            if (srcIdx < 0)
                throw new Exception("Supplement \"" + srcStore + "\" not recognised");

            transferKg = System.Math.Min(transferKg, this[srcIdx].Amount);
            if (transferKg > 0.0)
            {
                int dstIdx = IndexOf(destStore);
                if (dstIdx < 0)
                {
                    FoodSupplement newSupp = new FoodSupplement();
                    newSupp.Assign(this[srcIdx]);
                    newSupp.Name = destStore;
                    dstIdx = AddToStore(0.0, newSupp);
                }
                Transfer(this, srcIdx, this, dstIdx, transferKg);
            }
        }

        /// <summary>
        /// Removes the supplement
        /// </summary>
        /// <param name="paddIdx">Index of the padd.</param>
        /// <param name="suppKg">The supp kg.</param>
        /// <exception cref="System.Exception">Paddock not recognised</exception>
        private void RemoveSuppt(int paddIdx, double suppKg)
        {
            if (paddIdx < 0)
                throw new Exception("Paddock not recognised");

            SupplementRation ration = Paddocks[paddIdx].SupptFed;

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
        /// Removes the eaten supplement
        /// </summary>
        /// <param name="paddName">Name of the padd.</param>
        /// <param name="suppKg">The supp kg.</param>
        public void RemoveEaten(string paddName, double suppKg)
        {
            RemoveSuppt(PaddockIndexOf(paddName), suppKg);
        }

        /// <summary>
        /// Removes the eaten supplement
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

            for (int paddIdx = 0; paddIdx < Paddocks.Length; paddIdx++)
            {
                SupplementRation ration = Paddocks[paddIdx].SupptFed;
                if (ration.Count > 0)
                {
                    for (int dayNum = System.Math.Min(lastDay, ration.Count); dayNum > 0; dayNum--)
                    {
                        ration[dayNum] = ration[dayNum - 1];
                        ration[dayNum].Amount = ration[dayNum - 1].Amount * (SpoilageTime - dayNum) / (SpoilageTime - (dayNum - 1));
                    }
                    ration[0].Amount = 0.0;
                }
            }
        }

        /// <summary>
        /// Mixes the supplement into the ration.
        /// </summary>
        /// <param name="ration">The ration.</param>
        /// <param name="idx">The index.</param>
        /// <param name="supp">The supp.</param>
        /// <param name="amount">The amount.</param>
        private void SuppIntoRation(SupplementRation ration, int idx, FoodSupplement supp, double amount)
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
        /// <param name="dest">The dest ration in a paddock.</param>
        /// <param name="destIdx">Index of the dest.</param>
        /// <param name="amount">The amount.</param>
        /// <exception cref="System.Exception">Invalid transfer of feed</exception>
        private void Transfer(SupplementRation src, int srcIdx, SupplementRation dest, int destIdx, double amount)
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
                        FoodSupplement copy = new FoodSupplement();
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
}
