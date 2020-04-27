// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------

namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Models.Core;

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
    public class SupplementModel : SupplementLibrary
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
                Paddocks[paddIdx].SupptFed.AverageSuppt(out this.currPaddSupp);
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

        /// <summary>
        /// Paddock information about the supplement fed
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
            public SupplementRation SupptFed;   // Entry N is the supplement fed out N days ago

            /// <summary>
            /// For bail feeding
            /// </summary>
            public bool FeedSuppFirst = false;
        }
    }

    /// <summary>
    /// A stored supplement name and quantity
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
        /// Gets or sets the amount of supplement.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public double Stored { get; set; }
    }

    /// <summary>
    /// Paddock and amount of ration
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

        /// <summary>
        /// Gets or sets the flag to feed supplement before pasture. Bail feeding.
        /// </summary>
        public bool FeedSuppFirst { get; set; }
    }

    /// <summary>
    /// Paddock and amount eaten
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
    /// Buy an amount of supplement by name
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
    /// Feed an amount of supplement by name
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
    /// Mix an amount of supplement
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
    /// The type used when calling OnConserve()
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
    /// #GrazPlan Supplement
    /// This component represents one or more stores of supplementary feed. 
    /// 
    /// A component instance represents the stores and paddock-available amounts of several supplements. 
    /// Each supplement type is distinguished by a name and is represented by the amount in store together 
    /// with a number of attributes relating to its quality as a diet for animals.
    /// 
    /// Feed may be bought and then (logically) placed in one of the "paddocks" to which animals in the 
    /// Stock component may be assigned. Feed which has been placed in a paddock is accessible to grazing stock 
    /// in that paddock. If more than one supplement is placed into a paddock, the animals access a mixture.
    /// 
    /// **Mangement Operations in Supplement**
    /// 
    ///**1. Buy**
    ///
    /// * Increases the amount of supplement in a store.
    /// 
    ///**2. Feed**
    ///
    /// * Transfers an amount of supplement from store to one of the paddocks, where it will be accessible to grazing stock.
    /// It is possible to feed supplement before grazing.
    /// 
    ///**3. Mix**
    ///
    /// * Transfers an amount of supplement from one store into another. The transferred supplement is mixed
    /// with any supplement already in the destination store. 
    /// 
    ///**4. Conserve**
    ///
    /// * Notifies the component that an amount of forage has been conserved. This forage is added to the first item in the stores array.
    /// 
    /// ---
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
        private Simulation simulation = null;

        /// <summary>
        /// Link to the Stock component.
        /// </summary>
        [Link(IsOptional = true)]
        private Stock animals = null;

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary OutputSummary = null;

        /// <summary>
        /// Used to keep track of the selected SupplementItem in the user interface
        /// </summary>
        [XmlIgnore]
        public int CurIndex = 0;

        /// <summary>
        /// The model
        /// </summary>
        private SupplementModel theModel;

        /// <summary>
        /// The paddocks given
        /// </summary>
        private bool paddocksGiven;

        /// <summary>
        /// Has this model received it's DoManagement event today.
        /// This is needed to ensure the feeding schedule happens on
        /// the first day when FeedBegin is called after this model
        /// has already received it's DoManagement.
        /// </summary>
        private bool haveReceivedDoManagementToday = false;

        /// <summary>
        /// A list of feeding instances to be applied every day.
        /// </summary>
        private List<SupplementFeeding> feedingSchedule = new List<SupplementFeeding>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Supplement" /> class.
        /// </summary>
        public Supplement()
            : base()
        {
            theModel = new SupplementModel();
        }

        /// <summary>
        /// Gets or sets the time over which an amount of supplement placed in a paddock will become inaccessible to grazing stock
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
        /// Gets or sets the array of attributes and initial amount in each supplement store
        /// </summary>
        /// <value>
        /// List of stores
        /// </value>
        [Description("Attributes and initial amount in each supplement store")]
        public StoreType[] Stores
        {
            get
            {
                StoreType[] result = new StoreType[theModel.Count];
                for (int i = 0; i < theModel.Count; i++)
                {
                    result[i] = new StoreType();
                    result[i].Name = theModel[i].Name;
                    result[i].Stored = theModel[i].Amount;
                    result[i].IsRoughage = theModel[i].IsRoughage;
                    result[i].DMContent = theModel[i].DMPropn;
                    result[i].DMD = theModel[i].DMDigestibility;
                    result[i].MEContent = theModel[i].ME2DM;
                    result[i].CPConc = theModel[i].CrudeProt;
                    result[i].ProtDg = theModel[i].DegProt;
                    result[i].PConc = theModel[i].Phosphorus;
                    result[i].SConc = theModel[i].Sulphur;
                    result[i].EEConc = theModel[i].EtherExtract;
                    result[i].ADIP2CP = theModel[i].ADIP2CP;
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
                    theModel[jdx].DMPropn = value[i].DMContent;
                    theModel[jdx].DMDigestibility = value[i].DMD;
                    theModel[i].ME2DM = value[i].MEContent;
                    theModel[jdx].CrudeProt = value[i].CPConc;
                    theModel[jdx].DegProt = value[i].ProtDg;
                    theModel[jdx].Phosphorus = value[i].PConc;
                    theModel[jdx].Sulphur = value[i].SConc;
                    theModel[jdx].EtherExtract = value[i].EEConc;
                    theModel[jdx].ADIP2CP = value[i].ADIP2CP;
                    theModel[jdx].AshAlkalinity = value[i].AshAlk;
                    theModel[jdx].MaxPassage = value[i].MaxPassage;
                    //// RegisterNewStore(value[i].Name); // I don't think this is feasible under ApsimX
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of paddock names
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
                if (value != null)
                {
                    paddocksGiven = value.Length > 0;
                    if (paddocksGiven)
                        for (int i = 0; i < value.Length; i++)
                            theModel.AddPaddock(i, value[i]);
                }
            }
        }

        /// <summary>
        /// Gets or set the number of stores
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
        /// Gets or set the number of paddocks recognised by the component instance
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
        /// Gets the name of each paddock recognised by the component instance
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
        /// Gets the amount of supplement currently accessible to stock in each paddock recognised by the component instance
        /// </summary>
        /// <value>
        /// The list of supplement amounts in each paddock
        /// </value>
        [Description("Amount of supplement currently accessible to stock in each paddock recognised by the component instance")]
        public double[] PaddAmounts
        {
            get
            {
                double amount;
                double[] result = new double[theModel.PaddockCount];
                for (int i = 0; i < theModel.PaddockCount; i++)
                {
                    amount = 0;
                    theModel.GetFedSuppt(i, ref amount);
                    result[i] = amount;
                }

                return result;
            }
        }


        /// <summary>
        /// Gets the amount and attributes of supplementary feed present in each paddock
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
                    FoodSupplement supp = theModel.GetFedSuppt(i, ref amount);
                    result[i].Paddock = theModel.PaddockName(i);
                    result[i].Amount = amount;
                    result[i].IsRoughage = supp.IsRoughage;
                    result[i].DMContent = supp.DMPropn;
                    result[i].DMD = supp.DMDigestibility;
                    result[i].MEContent = supp.ME2DM;
                    result[i].CPConc = supp.CrudeProt;
                    result[i].ProtDg = supp.DegProt;
                    result[i].PConc = supp.Phosphorus;
                    result[i].SConc = supp.Sulphur;
                    result[i].EEConc = supp.EtherExtract;
                    result[i].ADIP2CP = supp.ADIP2CP;
                    result[i].AshAlk = supp.AshAlkalinity;
                    result[i].MaxPassage = supp.MaxPassage;
                    result[i].FeedSuppFirst = theModel.FeedSuppFirst(i);
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
        /// <returns>The supplement store type</returns>
        public StoreType this[string suppName]
        {
            get
            {
                int i = theModel.IndexOf(suppName);
                if (i < 0)
                    return null;
                StoreType result = new StoreType();
                result.Name = theModel[i].Name;
                result.Stored = theModel[i].Amount;
                result.IsRoughage = theModel[i].IsRoughage;
                result.DMContent = theModel[i].DMPropn;
                result.DMD = theModel[i].DMDigestibility;
                result.MEContent = theModel[i].ME2DM;
                result.CPConc = theModel[i].CrudeProt;
                result.ProtDg = theModel[i].DegProt;
                result.PConc = theModel[i].Phosphorus;
                result.SConc = theModel[i].Sulphur;
                result.EEConc = theModel[i].EtherExtract;
                result.ADIP2CP = theModel[i].ADIP2CP;
                result.AshAlk = theModel[i].AshAlkalinity;
                result.MaxPassage = theModel[i].MaxPassage;
                return result;
            }
        }

        /// <summary>
        /// Gets the <see cref="SupplementItem"/> with the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="SupplementItem"/>.
        /// </value>
        /// <param name="idx">The index.</param>
        /// <returns>The SupplementItem</returns>
        public SupplementItem this[int idx]
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
                theModel.AddPaddock(-1, string.Empty);
                int paddId = 0;
                foreach (Zone zone in Apsim.FindAll(simulation, typeof(Zone)))
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
            if (animals != null)
            {
                // get the supplement eaten from the Stock component
                SupplementEaten[] eaten = animals.SuppEaten;

                for (int idx = 0; idx < eaten.Length; idx++)
                    theModel.RemoveEaten(eaten[idx].Paddock, eaten[idx].Eaten);
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
            Conserve(conserved.Name, conserved.FreshWt, conserved.DMContent, conserved.DMD, conserved.NConc, conserved.PConc, conserved.SConc, conserved.AshAlk);
        }

        /// <summary>
        /// Conserves the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="freshWt">The fresh wt.</param>
        /// <param name="DMContent">Content of the dm.</param>
        /// <param name="dmd">The DMD.</param>
        /// <param name="NConc">The n conc.</param>
        /// <param name="PConc">The p conc.</param>
        /// <param name="SConc">The s conc.</param>
        /// <param name="AshAlk">The ash alk.</param>
        public void Conserve(string name, double freshWt, double DMContent, double dmd, double NConc, double PConc, double SConc, double AshAlk)
        {
            theModel.AddFodder(name, freshWt, DMContent, dmd, NConc, PConc, SConc, AshAlk);
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
        /// <param name="amount">Amount (kg fresh weight) of the supplement to be included in the store</param>
        /// <param name="supplement">The supplement.</param>
        public void Buy(double amount, string supplement)
        {
            OutputSummary.WriteMessage(this, "Purchase " + amount.ToString() + "kg of " + supplement);
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
        /// <param name="feedSuppFirst">Feed supplement before pasture. Bail feeding.</param>
        public void Feed(string supplement, double amount, string paddock, bool feedSuppFirst = false)
        {
            if (feedSuppFirst)
                throw new NotImplementedException("The feedSuppFirst argument to Supplement.Feed is not yet implemented. See GitHub issue #4440.");

            string firstly = feedSuppFirst ? " (Feeding supplement before pasture)" : string.Empty;
            OutputSummary.WriteMessage(this, "Feeding " + amount.ToString() + "kg of " + supplement + " into " + paddock + firstly);
            theModel.FeedOut(supplement, amount, paddock, feedSuppFirst);
        }


        /// <summary>
        /// Begin feeding the specified supplement every day.
        /// </summary>
        /// <param name="name">Feeding name. Used to end feeding.</param>
        /// <param name="supplement">The supplement.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="paddock">The paddock.</param>
        /// <param name="feedSuppFirst">Feed supplement before pasture. Bail feeding.</param>
        public void FeedBegin(string name, string supplement, double amount, string paddock, bool feedSuppFirst = false)
        {
            OutputSummary.WriteMessage(this, "Beginning feed schedule: " + name);
            var feeding = new SupplementFeeding(name, supplement, amount, paddock, feedSuppFirst);
            feedingSchedule.Add(feeding);
            if (haveReceivedDoManagementToday)
                feeding.Feed(this);
        }

        /// <summary>
        /// End feeding the specified supplement every day.
        /// </summary>
        /// <param name="name">Feeding name. Matches name passed into FeedBegin.</param>
        public void FeedEnd(string name)
        {
            OutputSummary.WriteMessage(this, "Ending feed schedule: " + name);
            feedingSchedule.RemoveAll(feed => feed.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>Invoked by clock at the start of every day.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            haveReceivedDoManagementToday = false;
        }

        /// <summary>
        /// Invoked by Clock to do our management for the day.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            haveReceivedDoManagementToday = true;
            feedingSchedule.ForEach(f => f.Feed(this));
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
        /// <returns>The supplement index</returns>
        public int Add(string suppName)
        {
            int defSuppNo = SupplementLibrary.DefaultSuppConsts.IndexOf(suppName);
            return theModel.AddToStore(0.0, SupplementLibrary.DefaultSuppConsts[defSuppNo]);
        }

        /// <summary>
        /// Adds the specified FoodSupplement.
        /// </summary>
        /// <param name="supplement">Supplement to be added</param>
        /// <returns>Index of the added supplement</returns>
        public int Add(FoodSupplement supplement)
        {
            return theModel.AddToStore(0.0, supplement);
        }

        /// <summary>
        /// Deletes the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        public void Delete(int idx)
        {
            theModel.Delete(idx);
        }

        /// <summary>
        /// Returns the index of FoodSupplement in the array of supplements
        /// </summary>
        /// <param name="item">The supplement item</param>
        /// <returns>The array index, or -1 if not found</returns>
        public int IndexOf(SupplementItem item)
        {
            return theModel.IndexOf(item);
        }

        /// <summary>
        /// Returns true if the currently named supplement is already in the mix
        /// </summary>
        /// <param name="suppName">Supplement name</param>
        /// <returns>The index of the supplement or -1 if not found</returns>
        public int IndexOf(string suppName)
        {
            return theModel.IndexOf(suppName);
        }


        /// <summary>
        /// This class encapsulates an amount of feed of a particular type that will
        /// be fed each day.
        /// </summary>
        [Serializable]
        private class SupplementFeeding
        {
            private string supplement;
            private double amount;
            private string paddock;
            private bool feedSuppFirst;

            /// <summary>Constructor.</summary>
            /// <param name="nam">Name of feed schedule.</param>
            /// <param name="sup">The supplement.</param>
            /// <param name="amt">The amount.</param>
            /// <param name="pad">The paddock.</param>
            /// <param name="feedSupFirst">Feed supplement before pasture. Bail feeding.</param>
            public SupplementFeeding(string nam, string sup, double amt, string pad, bool feedSupFirst)
            {
                Name = nam;
                supplement = sup;
                amount = amt;
                paddock = pad;
                feedSuppFirst = feedSupFirst;
            }

            /// <summary>Name of feeding.</summary>
            public string Name { get; }

            /// <summary>
            /// Tell supplement to do a feed.
            /// </summary>
            public void Feed(Supplement supp)
            {
                supp.Feed(supplement, amount, paddock, feedSuppFirst);
            }
        }
    }
}
