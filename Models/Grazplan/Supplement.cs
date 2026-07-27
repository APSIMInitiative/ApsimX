// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;
using Newtonsoft.Json;

namespace Models.GrazPlan
{

    /// <summary>
    /// # Supplement
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
    /// **Buy**
    ///
    /// Increases the amount of supplement in a store.
    ///
    /// **Feed**
    ///
    /// Transfers an amount of supplement from store to one of the paddocks, where it will be accessible to grazing stock.
    /// It is possible to feed supplement before grazing.
    ///
    /// **Mix**
    ///
    /// Transfers an amount of supplement from one store into another. The transferred supplement is mixed
    /// with any supplement already in the destination store.
    ///
    /// **Conserve**
    ///
    /// Notifies the component that an amount of forage has been conserved. This forage is added to the first item in the stores array.
    ///
    /// **Using Supplement**
    ///
    /// If supplements (e.g. cut and carry forages, grain, silages, …) are to be fed to Stock then they must first be created in “Supplement”. Think of Supplement as the grain silo or silage stack – it creates a space to store the supplements and keeps track of additions and removals but does no other actions.
    ///
    /// Multiple supplements can be named and characterised. If, for example, silages of different quality were required they should be added with different names (e.g. “silage12” for high-quality silage with an ME of 12 and “silage10” for lower quality silage with an ME of 10).
    ///
    /// *To add a new supplement:*
    /// 
    /// Right-click Supplement and select `Add Model` from the context menu and add a model of type `StoreType`.
    /// **Note:** At the moment you will have to specify the properties yourself. Fully parameterised models will be provided in future of the existing ones in supplement.txt
    /// 
    /// *To edit the properties of a supplement:*
    ///
    /// Once these quality parameters are set against a named supplement they are retained and all that is needed is to buy, sell or feed the named supplement. Supplements can also be deleted or have their quality parameters reset to defaults.
    ///
    /// Setting the dry matter percentage to 100: In the quality parameters, note that we set the dry matter percentage to 100. This does not affect the feeding quality of the supplement but means that all buy, sell, feed, etc. commands are given on a dry matter rather than wet matter basis.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public partial class Supplement : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

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
        [JsonIgnore]
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

        /// <summary>Gets the array of attributes and initial amount in each supplement store</summary>
        /// <value>List of stores</value>
        [JsonIgnore]
        public StoreType[] Stores
        {
            get
            {
                StoreType[] result = new StoreType[theModel.Count];
                for (int i = 0; i < theModel.Count; i++)
                {
                    result[i] = new StoreType
                    {
                        Name = theModel[i].Name,
                        Stored = theModel[i].Amount,
                        IsRoughage = theModel[i].IsRoughage,
                        DMContent = theModel[i].DMPropn,
                        DMD = theModel[i].DMDigestibility,
                        MEContent = theModel[i].ME2DM,
                        CPConc = theModel[i].CrudeProt,
                        ProtDg = theModel[i].DegProt,
                        PConc = theModel[i].Phosphorus,
                        SConc = theModel[i].Sulphur,
                        EEConc = theModel[i].EtherExtract,
                        ADIP2CP = theModel[i].ADIP2CP,
                        AshAlk = theModel[i].AshAlkalinity,
                        MaxPassage = theModel[i].MaxPassage
                    };
                }               
                return result;
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
        [Units("-")]
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
        /// Gets the number of supplement stores
        /// </summary>
        /// <value>
        /// The number of stores
        /// </value>
        [Description("Number of supplement stores")]
        [Units("-")]
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
        [Units("-")]
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
        [Units("-")]
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
        [Units("kg")]
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
        [Units("-")]
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
        [JsonIgnore]
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
        /// Gets the supplement store by name.
        /// </summary>
        /// <param name="name">Name of the supplement store.</param>
        /// <returns>The supplement store with the specified name, or null if not found.</returns>
        public StoreType GetSupplementStoreByName(string name)
        {
            return Children.FirstOrDefault(store => store.Name == name) as StoreType;
        }

        /// <summary>
        /// Adds the specified supplement to the store.
        /// </summary>
        public void AddToStore(StoreType supplement)
        {
            theModel.AddToStore(
                supplement.Stored,
                supplement.Name,
                supplement.IsRoughage ? 0 : 1,
                supplement.DMContent,
                supplement.DMD,
                supplement.MEContent,
                supplement.CPConc,
                supplement.ProtDg,
                supplement.EEConc,
                supplement.ADIP2CP,
                supplement.PConc,
                supplement.SConc,
                supplement.AshAlk,
                supplement.MaxPassage);
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
                foreach (Zone zone in Structure.FindAll<Zone>(relativeTo: simulation))
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
            theModel.TotalAmount = 0;
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
        /// Conserve forage to the Supplement component
        /// </summary>
        /// <param name="Data">The fodder</param>
        public delegate void ConserveSuppDelegate(ConserveType Data);

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
            OutputSummary.WriteMessage(this, "Purchase " + amount.ToString() + "kg of " + supplement, MessageType.Diagnostic);
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
            OutputSummary.WriteMessage(this, "Feeding " + amount.ToString() + "kg of " + supplement + " into " + paddock + firstly, MessageType.Diagnostic);
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
            OutputSummary.WriteMessage(this, "Beginning feed schedule: " + name, MessageType.Diagnostic);
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
            OutputSummary.WriteMessage(this, "Ending feed schedule: " + name, MessageType.Diagnostic);
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


        /// <summary> Called when the supplement is created.</summary>
        public override void OnCreated()
        {
            base.OnCreated();
            StoreType fodder = new()
            {
                Name = "fodder",
                IsRoughage = true,
                Stored = 0.0,
                DMContent = 0.85,
                DMD = 0.0,
                MEContent = 0.0,
                CPConc = 0.0,
                ProtDg = 0.0,
                PConc = 0.0,
                SConc = 0.0,
                EEConc = 0.0,
                ADIP2CP = 0.0,
                AshAlk = 0.0,
                MaxPassage = 0.0
            };
            Children.Insert(0, fodder);
        }

        /// <summary>
        /// Called when the model is serialised to the .apsimx file. 
        /// We use this event to ensure that the fodder store is not serialised to the .apsimx file, 
        /// as it is only used as a temporary store for conserved forage and should not be user-facing.
        /// </summary>
        public override void OnSerialising()
        {
            // Ensure that the fodder store is not serialised to the .apsimx file. 
            // It is only used as a temporary store for conserved forage and should not be user-facing.
            StoreType fodderStore = GetSupplementStoreByName("fodder");
            if (fodderStore != null)
                Children.Remove(fodderStore);

        }
    }
}
