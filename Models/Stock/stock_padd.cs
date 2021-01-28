// -----------------------------------------------------------------------
// GrazPlan animal model paddock and forage objects
// -----------------------------------------------------------------------

namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF.Interfaces;

    /*
     GRAZPLAN animal biology model for AusFarm - PaddockList && ForageList classes                                                                   
                                                                               
     * PaddockList contains information about paddocks within the animal      
       biology model. Paddocks have the following attributes:                  
       - ID   (integer)                                                        
       - name (text)                                                           
       - area (ha)                                                             
       - slope (degrees) - this is converted to a steepness value              
       - amount and composition of forage present in the paddock               
       - amount and composition of supplement present in the paddock           
       This information is held in PaddockInfo objects; PaddockList is a     
       list of PaddockInfo.                                                   
    */

    /// <summary>
    /// Chemistry data for the forage
    /// </summary>
    [Serializable]
    public struct ChemData
    {
        /// <summary>
        /// Mass in kg/ha 
        /// </summary>
        public double MassKgHa;

        /// <summary>
        /// N in kg/ha
        /// </summary>
        public double NitrogenKgHa;
        
        /// <summary>
        /// P in kg/ha
        /// </summary>
        public double PhosphorusKgHa;
        
        /// <summary>
        /// S in kg/ha
        /// </summary>
        public double SulphurKgHa;
        
        /// <summary>
        /// Ash alkalinity mol/ha
        /// </summary>
        public double AshAlkMolHa;
    }

    /// <summary>
    /// Up to 12 classes with separate digestible and indigestible pools
    /// </summary>
    [Serializable]
    public class ForageInfo
    {
        /// <summary>
        /// Missing value
        /// </summary>
        private const double MISSINGPOINT = 99999.9;

        /// <summary>
        /// Width of the digestibility class
        /// </summary>
        private const double CLASSWIDTH = 0.10;

        /// <summary>
        /// Highest disgestibility value
        /// </summary>
        private const double HIGHESTDMD = 0.85;

        /// <summary>
        /// Used in CalcDMDDistribution()
        /// </summary>
        private const double EPSILON = 1.0E-6;

        /// <summary>
        /// Maximum number of chemistry classes
        /// </summary>
        private const int MAXCHEMCLASSES = 24;

        /// <summary>
        /// Use the forage data
        /// The computed attributes of this "forage", in the form used by AnimalGroup
        /// </summary>
        private bool useForageData;

        /// <summary>
        /// The grazing forage data
        /// </summary>
        private GrazType.GrazingInputs forageData = new GrazType.GrazingInputs();

        /// <summary>
        /// The bulk density of the green
        /// </summary>
        private double greenBulkDensity;

        /// <summary>
        /// Mass of the legume
        /// </summary>
        private double legumeMass = 0;

        /// <summary>
        /// Mass of the C4 grass
        /// </summary>
        private double C4GrassMass = 0;

        /// <summary>
        /// 0 = non-seed, UNRIPE or RIPE
        /// </summary>
        private int seedType = 0;   

        /// <summary>
        /// The herbage bottom
        /// </summary>
        private double bottomMM;

        /// <summary>
        /// The herbage top
        /// </summary>
        private double topMM;

        /// <summary>
        /// Chemistry details for each chem class
        /// </summary>
        private ChemData[] chemData = new ChemData[MAXCHEMCLASSES - 1];

        /// <summary>
        /// Herbage dmd fraction
        /// </summary>
        private double[] herbageDMDFract = new double[GrazType.DigClassNo + 1]; // [1..DigClassNo]
        
        /// <summary>
        /// See ripe fraction
        /// </summary>
        private double[] seedRipeFract = new double[3];  // [1..2]

        /// <summary>
        /// Construct a forage info
        /// </summary>
        public ForageInfo()
        {
        }

        /// <summary>
        /// Gets or sets the green mass of the forage
        /// </summary>
        [Units("kg/ha")]
        public double GreenMass { get; set; }

        /// <summary>
        /// Gets the total live herbage used as input in GrazingInputs
        /// Units: the same as the forage object
        /// </summary>
        [Units("kg/ha")]
        public double TotalLive
        {
            get
            {
                return this.forageData.TotalGreen;
            }
        }

        /// <summary>
        /// Gets the total dead herbage used as input in GrazingInputs
        /// Units: the same as the forage object
        /// </summary>
        [Units("kg/ha")]
        public double TotalDead
        {
            get
            {
                return this.forageData.TotalDead;
            }
        }

        /// <summary>
        /// Gets or sets the full identifier for this forage e.g. Crop or pasture component full path name
        /// </summary>
        [Units("-")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the cohortID from the incoming AvailableToAnimal forage component
        /// </summary>
        [Units("-")]
        public string CohortID { get; set; }

        /// <summary>
        /// Gets or sets the forage organ
        /// </summary>
        [Units("-")]
        public string Organ { get; set; }

        /// <summary>
        /// Gets or sets the forage item age class
        /// </summary>
        [Units("-")]
        public string AgeClass { get; set; }

        /// <summary>
        /// Gets or sets the paddock of this forage
        /// </summary>
        [Units("-")]
        public PaddockInfo InPaddock { get; set; }

        /// <summary>
        /// Gets or sets the amount of this forage removed (output)
        /// </summary>
        [Units("-")]
        public GrazType.GrazingOutputs RemovalKG { get; set; }

        /// <summary>
        /// Gets or sets the bottom of forage
        /// </summary>
        [Units("mm")]
        public double Bottom
        {
            get { return this.GetBottom(); }
            set { this.bottomMM = value; }
        }

        /// <summary>
        /// Gets or sets the top of forage
        /// </summary>
        [Units("mm")]
        public double Top
        {
            get { return this.GetTop(); }
            set { this.topMM = value; }
        }

        /// <summary>
        /// Gets the bottom position
        /// </summary>
        /// <returns>The bottom mm</returns>
        private double GetBottom()
        {
            if (this.bottomMM == MISSINGPOINT)
                return 0.0;
            else
                return this.bottomMM;
        }

        /// <summary>
        /// Gets the top postion
        /// </summary>
        /// <returns>The top in mm</returns>
        private double GetTop()
        {
            return this.topMM;
        }

        /// <summary>
        /// Summarise the initial herbage 
        /// </summary>
        public void SummariseInitHerbage()
        {
            double fHR;
            //// double dBulkDensity;  // Herbage bulk density in kg/m^3
            int classIdx;
            //// int iChem;

            if (this.useForageData)
            {
                this.GreenMass = this.forageData.TotalGreen;
                this.greenBulkDensity = GrazType.REF_HERBAGE_BD;
                if ((this.forageData.TotalGreen + this.forageData.TotalDead) > 0.0)
                {
                    fHR = 0.0;
                    for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                        fHR = fHR + (this.forageData.Herbage[classIdx].Biomass * this.forageData.Herbage[classIdx].HeightRatio);
                    fHR = fHR / (this.forageData.TotalGreen + this.forageData.TotalDead);
                    if (fHR > 0.0)
                        this.greenBulkDensity = GrazType.REF_HERBAGE_BD / fHR;
                }
            }
            else
            {
                this.GreenMass = 0.0;
                /*FGreenBulkDensity = 0.0;
                if (FIsGreen)
                {
                    dBulkDensity = this.dHerbageBulkDensity();
                    for (iChem = 0; iChem <= this.CHEMCOUNT[(int)FChemistryType] - 1; iChem++)
                    {
                        FGreenMass = FGreenMass + this.FChemData[iChem].MassKgHa;
                        FGreenBulkDensity = FGreenBulkDensity + this.FChemData[iChem].MassKgHa * dBulkDensity;
                    }
                }

                if (FGreenMass > 0.0)
                    FGreenBulkDensity = FGreenBulkDensity / FGreenMass;
                else
                    FGreenBulkDensity = GrazType.REF_HERBAGE_BD;*/
            }
        }

        /// <summary>
        /// Calculate the proportions in a DMD distribution
        /// </summary>
        /// <param name="meanDMD">The mean DMD</param>
        /// <param name="maxDMD">Upper range</param>
        /// <param name="minDMD">Lower range</param>
        /// <returns>A digestibility distribution</returns>
        public static double[] CalcDMDDistribution(double meanDMD, double maxDMD, double minDMD)
        {
            int highClass;
            int lowClass;
            double relDMD;

            double[] result = new double[GrazType.DigClassNo + 1];

            highClass = 1 + Convert.ToInt32(Math.Truncate((HIGHESTDMD - maxDMD + EPSILON) / CLASSWIDTH), CultureInfo.InvariantCulture);
            lowClass = 1 + Convert.ToInt32(Math.Truncate((HIGHESTDMD - minDMD - EPSILON) / CLASSWIDTH), CultureInfo.InvariantCulture);
            if (highClass != lowClass)
                relDMD = Math.Max(0.0, Math.Min((meanDMD - GrazType.ClassDig[lowClass]) / (GrazType.ClassDig[highClass] - GrazType.ClassDig[lowClass]), 1.0));
            else
                relDMD = 1.0;

            switch (lowClass - highClass + 1)
            {
                case 1: result[highClass] = 1.0;
                    break;
                case 2:
                    {
                        result[highClass + 0] = relDMD;
                        result[highClass + 1] = 1.0 - relDMD;
                    }
                    break;
                case 3:
                    {
                        result[highClass + 0] = Math.Pow(relDMD, 2);
                        result[highClass + 1] = 2.0 * relDMD * (1.0 - relDMD);
                        result[highClass + 2] = Math.Pow(1.0 - relDMD, 2);
                    }
                    break;
                case 4:
                    {
                        result[highClass + 0] = Math.Pow(relDMD, 3);
                        result[highClass + 1] = 3.0 * Math.Pow(relDMD, 2) * (1.0 - relDMD);
                        result[highClass + 2] = 3.0 * relDMD * Math.Pow(1.0 - relDMD, 2);
                        result[highClass + 3] = Math.Pow(1.0 - relDMD, 3);
                    }
                    break;
                case 5:
                    {
                        result[highClass + 0] = Math.Pow(relDMD, 4);
                        result[highClass + 1] = 4.0 * Math.Pow(relDMD, 3) * (1.0 - relDMD);
                        result[highClass + 2] = 6.0 * Math.Pow(relDMD, 2) * Math.Pow(1.0 - relDMD, 2);
                        result[highClass + 3] = 4.0 * relDMD * Math.Pow(1.0 - relDMD, 3);
                        result[highClass + 4] = Math.Pow(1.0 - relDMD, 4);
                    }
                    break;
                default:
                    throw new Exception("Error in DMD distribution calculation");
            }

            return result;
        }

        /// <summary>
        /// Populate the intake record
        /// </summary>
        /// <param name="intakeRecord">The intake record to return</param>
        /// <param name="dmdClass">Digestibility class</param>
        /// <param name="useMeanDMD">Use mean digestibility</param>
        /// <param name="totalMass">Total herbage mass</param>
        /// <param name="meanDMD">Mean digestiblity</param>
        /// <param name="massFract">Mass fraction</param>
        /// <param name="ddm_N">Digestible N</param>
        /// <param name="idm_N">Indigestible N</param>
        /// <param name="ddm_P">Digestible P</param>
        /// <param name="idm_P">Indigestible P</param>
        /// <param name="ddm_S">Digestible sulphur</param>
        /// <param name="idm_S">Indigestible sulphur</param>
        /// <param name="ddm_AA">Digestible AshAlk</param>
        /// <param name="idm_AA">Indigestible AshAlk</param>
        /// <param name="bulkDensity">Bulk density</param>
        public void PopulateIntakeRecord(
                                        ref GrazType.IntakeRecord intakeRecord,
                                        int dmdClass,
                                        bool useMeanDMD,
                                        double totalMass,
                                        double meanDMD,
                                        double massFract,
                                        double ddm_N, double idm_N,
                                        double ddm_P, double idm_P,
                                        double ddm_S, double idm_S,
                                        double ddm_AA, double idm_AA,
                                        double bulkDensity)
        {
            double propnDMD;
            double propnIDM;

            if (useMeanDMD)
                intakeRecord.Digestibility = meanDMD;
            else
                intakeRecord.Digestibility = GrazType.ClassDig[dmdClass];

            if (massFract > 0.0)
            {
                propnDMD = massFract * (intakeRecord.Digestibility / meanDMD);
                if ((1.0 - meanDMD) != 0)
                    propnIDM = massFract * ((1.0 - intakeRecord.Digestibility) / (1.0 - meanDMD));
                else                // trap div/0
                    propnIDM = 0;

                intakeRecord.Biomass = massFract * totalMass;
                intakeRecord.CrudeProtein = GrazType.N2Protein * ((propnDMD * ddm_N) + (propnIDM * idm_N)) / intakeRecord.Biomass;
                intakeRecord.Degradability = Math.Min(0.90, intakeRecord.Digestibility + 0.10);
                intakeRecord.PhosContent = ((propnDMD * ddm_P) + (propnIDM * idm_P)) / intakeRecord.Biomass;
                intakeRecord.SulfContent = ((propnDMD * ddm_S) + (propnIDM * idm_S)) / intakeRecord.Biomass;
                intakeRecord.AshAlkalinity = ((propnDMD * ddm_AA) + (propnIDM * idm_AA)) / intakeRecord.Biomass;
                intakeRecord.HeightRatio = GrazType.REF_HERBAGE_BD / bulkDensity;
            }
        }

        /// <summary>
        /// Populate the seed record
        /// </summary>
        /// <param name="grazingInput">The grazing input</param>
        /// <param name="availPropn">Available proportion</param>
        /// <param name="idxDDM">Digestible index</param>
        /// <param name="idxIDM">Indigestible index</param>
        public void PopulateSeedRecord(ref GrazType.GrazingInputs grazingInput, double availPropn, int idxDDM, int idxIDM)
        {
            double totalDM;
            double meanDMD;

            totalDM = this.chemData[idxDDM].MassKgHa + this.chemData[idxIDM].MassKgHa;
            meanDMD = this.chemData[idxDDM].MassKgHa / totalDM;

            grazingInput.Seeds[1, this.seedType].Biomass = availPropn * totalDM;
            grazingInput.Seeds[1, this.seedType].Digestibility = meanDMD;
            grazingInput.Seeds[1, this.seedType].Degradability = Math.Min(0.90, meanDMD + 0.10);
            if (grazingInput.Seeds[1, this.seedType].Biomass > 0.0)
            {
                grazingInput.Seeds[1, this.seedType].CrudeProtein = (this.chemData[idxDDM].NitrogenKgHa + this.chemData[idxIDM].NitrogenKgHa) / totalDM * GrazType.N2Protein;
                grazingInput.Seeds[1, this.seedType].PhosContent = (this.chemData[idxDDM].PhosphorusKgHa + this.chemData[idxIDM].PhosphorusKgHa) / totalDM;
                grazingInput.Seeds[1, this.seedType].SulfContent = (this.chemData[idxDDM].SulphurKgHa + this.chemData[idxIDM].SulphurKgHa) / totalDM;
                grazingInput.Seeds[1, this.seedType].AshAlkalinity = (this.chemData[idxDDM].AshAlkMolHa + this.chemData[idxIDM].AshAlkMolHa) / totalDM;
            }
            grazingInput.Seeds[1, this.seedType].HeightRatio = 0.0;
        }

        /// <summary>
        /// Set the LegumePropn, SelectFactor and TropLegume fields of a GrazingInputs
        /// * Expects that the TotalGreen and TotalDead fields have already been computed
        /// </summary>
        /// <param name="grazingInput">The grazing inputs</param>
        public void PopulateHerbageType(ref GrazType.GrazingInputs grazingInput)
        {
            if (grazingInput.TotalGreen + grazingInput.TotalDead > 0.0)
            {
                grazingInput.LegumePropn = this.legumeMass / (grazingInput.TotalGreen + grazingInput.TotalDead);
                grazingInput.SelectFactor = 0.16 * this.C4GrassMass / (grazingInput.TotalGreen + grazingInput.TotalDead);
                grazingInput.LegumeTrop = 0.0; // FIX ME
            }
            else
            {
                grazingInput.LegumePropn = 0.0;
                grazingInput.SelectFactor = 0.0;
                grazingInput.LegumeTrop = 0.0;
            }
        }
        
        /// <summary>
        /// The the forage data
        /// </summary>
        /// <param name="forageInputs">Forage inputs</param>
        public void SetAvailForage(GrazType.GrazingInputs forageInputs)
        {
            this.useForageData = true;
            this.forageData.CopyFrom(forageInputs);
        }

        /// <summary>
        /// Calculates the GrazingInputs values from the values stored during addForageData() 
        /// </summary>
        /// <returns>The grazing inputs</returns>
        public GrazType.GrazingInputs AvailForage()
        {
            GrazType.GrazingInputs result = new GrazType.GrazingInputs();
                        
            result.CopyFrom(this.forageData);
            
            return result;
        }

        /// <summary>
        /// Returns True if something has been removed from herbage or seed pool.
        /// </summary>
        /// <returns>True if some forage amount has been removed by the animals</returns>
        public bool SomethingRemoved()
        {
            int i;

            bool result = false;

            // iterate through all the classes
            i = 1;
            while ((i <= GrazType.DigClassNo) && (!result))
            {
                if (this.RemovalKG.Herbage[i] > 0.0)
                    result = true;
                i++;
            }
            if (!result)
            {
                i = 1;
                while ((i <= GrazType.MaxPlantSpp) && (!result))
                {
                    if ((this.RemovalKG.Seed[i, GrazType.UNRIPE] > 0.0) || (this.RemovalKG.Seed[i, GrazType.RIPE] > 0.0))
                        result = true;
                    i++;
                }
            }
            return result;
        }
    }

    // ============================================================================
    
    /// <summary>
    /// List of ForageInfo forages 
    /// </summary>
    [Serializable]
    public class ForageList
    {
        /// <summary>
        /// The list of forage infos
        /// </summary>
        private ForageInfo[] items;

        /// <summary>
        /// This object manages the lifetime of the item list
        /// </summary>
        private bool ownsList;

        /// <summary>
        /// Construct the forage list
        /// </summary>
        /// <param name="ownsForages">The object will manage the lifetime of the forage list</param>
        public ForageList(bool ownsForages)
        {
            this.ownsList = ownsForages;
            Array.Resize(ref this.items, 0);
        }

        /// <summary>
        /// Count of forages
        /// </summary>
        /// <returns>The count of forages</returns>
        public int Count()
        {
            return this.items.Length;
        }

        /// <summary>
        /// Add a forage item
        /// </summary>
        /// <param name="forageInfo">The forage information</param>
        public void Add(ForageInfo forageInfo)
        {
            int idx = this.items.Length;
            Array.Resize(ref this.items, idx + 1);
            this.items[idx] = forageInfo;
        }

        /// <summary>
        /// Add a forage by name
        /// </summary>
        /// <param name="forageName">Forage name</param>
        /// <returns>The new forage item</returns>
        public ForageInfo Add(string forageName)
        {
            ForageInfo newInfo = new ForageInfo();
            newInfo.Name = forageName.ToLower();
            this.Add(newInfo);
            return newInfo;
        }

        /// <summary>
        /// Delete a forage by index
        /// </summary>
        /// <param name="indexValue">Index value</param>
        public void Delete(int indexValue)
        {
            int idx;

            if (this.ownsList)
                this.items[indexValue] = null;
            for (idx = indexValue + 1; idx <= this.items.Length - 1; idx++)
                this.items[idx - 1] = this.items[idx];
            Array.Resize(ref this.items, this.items.Length - 1);
        }

        /// <summary>
        /// Get a forage by index
        /// </summary>
        /// <param name="indexValue">The forage index</param>
        /// <returns>The forage. If not found then return null</returns>
        public ForageInfo ByIndex(int indexValue)
        {
            ForageInfo result = null;
            if ((indexValue >= 0) && (indexValue < this.items.Length))
                result = this.items[indexValue];

            return result;
        }

        /// <summary>
        /// Get a forage by name
        /// </summary>
        /// <param name="forageName">The forage name</param>
        /// <returns>The forage info. If not found then null.</returns>
        public ForageInfo ByName(string forageName)
        {
            int idx;

            idx = this.IndexOf(forageName);
            if (idx >= 0)
                return this.ByIndex(idx);
            else
                return null;
        }

        /// <summary>
        /// Get the index of a forage by name
        /// </summary>
        /// <param name="forageName">The forage name</param>
        /// <returns>Returns the forage index</returns>
        public int IndexOf(string forageName)
        {
            int result = this.Count() - 1;
            while ((result >= 0) && (this.ByIndex(result).Name.ToLower() != forageName.ToLower()))
            {
                result--;
            }

            return result;
        }
    }

    /// <summary>
    /// New forage interface for AvailableToAnimal support
    /// TForageProvider maps to a cmp component such as Plant or AgPasture.
    /// Each of these can contain 0..n forage. The forage will be named
    /// with the cohortid string.
    /// </summary>
    [Serializable]
    public class ForageProvider
    {
        /// <summary>
        /// The list of forages
        /// </summary>
        private ForageList forages;

        /// <summary>
        /// host crop, pasture component name
        /// </summary>
        private string forageHostName;

        /// <summary>
        /// plant/pasture comp
        /// </summary>
        private int hostID;

        /// <summary>
        /// owning paddock FQN
        /// </summary>
        private string paddockOwnerName;

        /// <summary>
        /// Ref to the paddock object in the model
        /// </summary>
        private PaddockInfo owningPaddock;     

        /// <summary>
        /// Construct the forage provider
        /// </summary>
        public ForageProvider()
        {
            this.forages = new ForageList(true);
            this.OwningPaddock = null;
        }

        /// <summary>
        /// Gets or sets the total calculated green dm for the paddock
        /// </summary>
        [Units("kg/ha")]
        public double PastureGreenDM { get; set; }

        /// <summary>
        /// Gets or sets the owning paddock
        /// </summary>
        [Units("-")]
        public PaddockInfo OwningPaddock
        {
            get { return this.owningPaddock; }
            set { this.owningPaddock = value; }
        }

        /// <summary>
        /// Gets or sets the paddock owner name
        /// </summary>
        [Units("-")]
        public string PaddockOwnerName
        {
            get { return this.paddockOwnerName; }
            set { this.paddockOwnerName = value; }
        }

        /// <summary>
        /// Gets or sets the forage host name
        /// </summary>
        [Units("-")]
        public string ForageHostName
        {
            get { return this.forageHostName; }
            set { this.forageHostName = value; }
        }

        /// <summary>
        /// Gets or sets the component id of the host
        /// </summary>
        [Units("-")]
        public int HostID
        {
            get { return this.hostID; }
            set { this.hostID = value; }
        }
               
        /// <summary>
        /// Gets or sets the crop, pasture component
        /// </summary>
        public IPlantDamage ForageObj { get; set; }

        /// <summary>
        /// Update the forage data for this crop/agpasture object
        /// </summary>
        /// <param name="forageObj">The crop/pasture object</param>
        public void UpdateForages(IPlantDamage forageObj)
        {
            // ensure this forage is in the list
            // the forage key in this case is component name
            ForageInfo forage = this.forages.ByName(this.ForageHostName);
            if (forage == null)  
            {
                // if this cohort doesn't exist in the forage list
                forage = new ForageInfo();
                forage.Name = this.ForageHostName.ToLower();
                this.owningPaddock.AssignForage(forage);               // the paddock in the model can access this forage
                this.forages.Add(forage);                               // create a new forage for this cohort
            }
            
            // TODO: just assuming one forage cohort in this component (expand here?)
            this.PassGrazingInputs(forage, this.Crop2GrazingInputs(forageObj), "g/m^2"); // then update it's value
        }

        /// <summary>
        /// The forage name is the name of the cohort.
        /// </summary>
        /// <param name="forageName">The forage name</param>
        /// <returns>The forage object</returns>
        public ForageInfo ForageByName(string forageName)
        {
            return this.forages.ByName(forageName);
        }

        /// <summary>
        /// Get the forage by index
        /// </summary>
        /// <param name="idx">The index. 0..n</param>
        /// <returns>The forage index</returns>
        public ForageInfo ForageByIndex(int idx)
        {
            return this.forages.ByIndex(idx);
        }

        /// <summary>
        /// Use the GrazingInputs to initialise the forage object
        /// </summary>
        /// <param name="forage">The forage object</param>
        /// <param name="grazingInput">The grazing inputs</param>
        /// <param name="units">The units</param>
        public void PassGrazingInputs(ForageInfo forage, GrazType.GrazingInputs grazingInput, string units)
        {
            double scaleInput;

            if (units == "kg/ha")                                                     // Convert to kg/ha                      
                scaleInput = 1.0;
            else if (units == "g/m^2")
                scaleInput = 10.0;
            else
                throw new Exception("Stock: Unit (" + units + ") not recognised");

            if (forage != null)
                forage.SetAvailForage(GrazType.ScaleGrazingInputs(grazingInput, scaleInput));
            else
                throw new Exception("Stock: Forage not recognised");
        }

        /// <summary>
        /// Copies a Plant/AgPasture object biomass organs into GrazingInputs object
        /// This object may then get scaled to kg/ha
        /// </summary>
        /// <param name="forageObj">The forage object - a Plant/AgPasture component</param>
        /// <returns>The grazing inputs</returns>
        private GrazType.GrazingInputs Crop2GrazingInputs(IPlantDamage forageObj)
        {
            GrazType.GrazingInputs result = new GrazType.GrazingInputs();
            GrazType.zeroGrazingInputs(ref result);
            
            result.TotalGreen = 0;
            result.TotalDead = 0;

            double totalDMD = 0;
            double totalN = 0;
            double nConc;
            double meanDMD;
            double dmd;

            // calculate the green available based on the total green in this paddock
            double greenPropn = 0;
            
            // ** should really take into account the height ratio here e.g. Params.HeightRatio
            if (this.PastureGreenDM > GrazType.Ungrazeable)
            {
                greenPropn = 1.0 - (GrazType.Ungrazeable / this.PastureGreenDM);
            }

            // calculate the total live and dead biomass
            foreach (IOrganDamage biomass in forageObj.Organs)
            {
                if (biomass.IsAboveGround)
                {
                    if (biomass.Live.Wt > 0 || biomass.Dead.Wt > 0)
                    {
                        result.TotalGreen += (greenPropn * biomass.Live.Wt);   // g/m^2
                        result.TotalDead += biomass.Dead.Wt;

                        // we can find the dmd of structural, assume storage and metabolic are 100% digestible
                        dmd = (biomass.Live.DMDOfStructural * greenPropn * biomass.Live.StructuralWt) + (1 * greenPropn * biomass.Live.StorageWt) + (1 * greenPropn * biomass.Live.MetabolicWt);    // storage and metab are 100% dmd
                        dmd += ((biomass.Dead.DMDOfStructural * biomass.Dead.StructuralWt) + (1 * biomass.Dead.StorageWt) + (1 * biomass.Dead.MetabolicWt));
                        totalDMD += dmd;
                        totalN += (greenPropn * biomass.Live.N) + (biomass.Dead.Wt > 0 ? biomass.Dead.N : 0);   // g/m^2
                    }
                }
            }

            // TODO: Improve this routine
            double availDM = result.TotalGreen + result.TotalDead;
            if (availDM > 0)
            {
                meanDMD = totalDMD / availDM; // calc the average dmd for the plant
                nConc = totalN / availDM;     // N conc 
                // get the dmd distribution
                double[] dmdPropns; // = new double[GrazType.DigClassNo + 1];

                // green 0.85-0.45, dead 0.70-0.30
                dmdPropns = ForageInfo.CalcDMDDistribution(meanDMD, 0.85, 0.45);    // FIX ME: the DMD ranges should be organ- and development-specific values

                for (int idx = 1; idx <= GrazType.DigClassNo; idx++)
                {
                    result.Herbage[idx].Biomass = dmdPropns[idx] * availDM;
                    result.Herbage[idx].CrudeProtein = nConc * GrazType.N2Protein;
                    result.Herbage[idx].Digestibility = GrazType.ClassDig[idx];
                    result.Herbage[idx].Degradability = Math.Min(0.90, result.Herbage[idx].Digestibility + 0.10);
                    result.Herbage[idx].HeightRatio = 1;
                    result.Herbage[idx].PhosContent = 0;    // N * 0.05?
                    result.Herbage[idx].SulfContent = 0;    // N * 0.07?
                    result.Herbage[idx].AshAlkalinity = 0.70;   // TODO: use a modelled value
                }

                if (forageObj is IPlant plant)
                {
                    switch (plant.PlantType)
                    {
                        case "AGPLucerne":
                        case "AGPRedClover":
                        case "AGPWhiteClover":
                            result.LegumePropn = 1;
                            break;
                        default:
                            result.LegumePropn = 0;
                            break;
                    }
                }
                    
                result.SelectFactor = 0;    // TODO: set from Plant model value

                // TODO: Store any seed pools
            }

            return result;
        }

        /// <summary>
        /// The herbage is removed from the plant/agpasture
        /// </summary>
        public void RemoveHerbageFromPlant()
        {
            string chemType = string.Empty;
            int forageIdx = 0;

            ForageInfo forage = this.ForageByIndex(forageIdx);
            while (forage != null)
            {
                double area = forage.InPaddock.Area;
                GrazType.GrazingOutputs removed = forage.RemovalKG;

                // total the amount removed kg/ha
                double totalRemoved = 0.0;
                for (int i = 0; i < removed.Herbage.Length; i++)
                    totalRemoved += removed.Herbage[i];
                double propnRemoved = Math.Min(1.0, (totalRemoved / area) / (forage.TotalLive + forage.TotalDead + GrazType.Ungrazeable * 10.0)); //  calculations in kg /ha, needs more checking, would be good to use a variable for the unit conversion on ungrazeable

                // calculations of proportions each organ of the total plant removed (in the native units)
                double totalDM = 0;
                foreach (IOrganDamage organ in ForageObj.Organs)
                {
                    if (organ.IsAboveGround && (organ.Live.Wt + organ.Dead.Wt) > 0)
                    {
                        totalDM += organ.Live.Wt + organ.Dead.Wt;
                    }
                }

                foreach (IOrganDamage organ in ForageObj.Organs)
                {
                    if (organ.IsAboveGround && (organ.Live.Wt + organ.Dead.Wt) > 0)
                    {
                        double propnOfPlantDM = (organ.Live.Wt + organ.Dead.Wt) / totalDM;
                        double prpnToRemove = propnRemoved * propnOfPlantDM;
                        prpnToRemove = Math.Min(prpnToRemove, 1.0);
                        PMF.OrganBiomassRemovalType removal = new PMF.OrganBiomassRemovalType();
                        removal.FractionDeadToRemove = prpnToRemove;
                        removal.FractionLiveToRemove = prpnToRemove;
                        ForageObj.RemoveBiomass(organ.Name, "Graze", removal);
                    }
                }
                
                forageIdx++;
                forage = this.ForageByIndex(forageIdx);
            }
        }

        /// <summary>
        /// Return the removal
        /// </summary>
        /// <param name="forage">The forage</param>
        /// <param name="units">The units</param>
        /// <returns>The grazing outputs/consumed</returns>
        public GrazType.GrazingOutputs ReturnRemoval(ForageInfo forage, string units)
        {
            double area;
            double scale;
            int idxClass;
            int idxSpecies;
            int idxRipe;

            GrazType.GrazingOutputs result = new GrazType.GrazingOutputs();

            if (forage != null)
            {
                result = forage.RemovalKG;
                area = forage.InPaddock.Area;
            }
            else
            {
                result = new GrazType.GrazingOutputs();
                area = 0.0;
            }

            if (area > 0.0)
            {
                if (units == "kg")
                    scale = 1.0;
                else if (units == "g/m^2")
                    scale = 0.10 / area;
                else if (units == "kg/ha")
                    scale = 1.0 / area;
                else
                    throw new Exception("Stock: Unit (" + units + ") not recognised");

                if (scale != 1.0)
                {
                    for (idxClass = 1; idxClass <= GrazType.DigClassNo; idxClass++)
                        result.Herbage[idxClass] = scale * result.Herbage[idxClass];
                    for (idxSpecies = 1; idxSpecies <= GrazType.MaxPlantSpp; idxSpecies++)
                        for (idxRipe = GrazType.UNRIPE; idxRipe <= GrazType.RIPE; idxRipe++)
                            result.Seed[idxSpecies, idxRipe] = scale * result.Seed[idxSpecies, idxRipe];
                }
            }
            return result;
        }

        /// <summary>
        /// Test the Removal to determine if there is any quantity of forage removed.
        /// </summary>
        /// <returns>True if some herbage has been removed</returns>
        public bool SomethingRemoved()
        {
            bool result = false;

            // get the removal for each forage
            int forageIdx = 0;
            ForageInfo forageInf = this.ForageByIndex(forageIdx);
            while ((forageInf != null) && (!result))
            {
                result = forageInf.SomethingRemoved();
                forageIdx++;
                forageInf = this.ForageByIndex(forageIdx);
            }
            return result;
        }
    }

    /// <summary>
    /// ForageProviders is a collection of forage/cmp components that each in turn
    /// supply 1..n forage plants/species
    /// </summary>
    [Serializable]
    public class ForageProviders
    {
        /// <summary>
        /// The list of forage providers
        /// </summary>
        private List<ForageProvider> forageProviderList;

        /// <summary>
        /// Construct a forage provider
        /// </summary>
        public ForageProviders()
        {
            this.forageProviderList = new List<ForageProvider>();
        }

        /// <summary>
        /// Count of forage providers
        /// </summary>
        /// <returns>The count of forage providers</returns>
        public int Count()
        {
            return this.forageProviderList.Count();
        }

        /// <summary>
        /// Add a forage provider component
        /// </summary>
        /// <param name="paddock">The paddock info</param>
        /// <param name="paddName">The paddock name</param>
        /// <param name="forageName">The forage name</param>
        /// <param name="hostID">Component ID</param>
        /// <param name="driverID">Driver ID</param>
        /// <param name="forageObj">The forage object</param>
        public void AddProvider(PaddockInfo paddock, string paddName, string forageName, int hostID, int driverID, IPlantDamage forageObj)
        {
            ForageProvider forageProvider;

            // this is a forage provider
            // this provider can host a number of forages/species
            forageProvider = new ForageProvider();
            forageProvider.PaddockOwnerName = paddName;    // owning paddock
            forageProvider.ForageHostName = forageName;    // host pasture/plant component name
            forageProvider.HostID = hostID;                // plant/pasture comp
            forageProvider.ForageObj = forageObj;          // setting property ID
            // keep a ptr to the paddock owned by the model so the forages can be assigned there as they become available
            forageProvider.OwningPaddock = paddock;

            this.forageProviderList.Add(forageProvider);
        }

        /// <summary>
        /// Find the forage provider for this forage/provider name
        /// </summary>
        /// <param name="providerName">The forage provider (component) name</param>
        /// <returns>The ForageProvider</returns>
        public ForageProvider FindProvider(string providerName)
        {
            ForageProvider provider = null;
            int i = 0;
            while ((provider == null) && (i <= this.forageProviderList.Count - 1))
            {
                // if the name matches
                if (this.forageProviderList[i].ForageHostName == providerName)
                    provider = this.forageProviderList[i];
                i++;
            }
            return provider;
        }

        /// <summary>
        /// Find the forage provider for this component ID.
        /// </summary>
        /// <param name="hostID">The host component ID</param>
        /// <returns>The forage provider</returns>
        public ForageProvider FindProvider(int hostID)
        {
            ForageProvider provider = null;
            int i = 0;
            while ((provider == null) && (i <= this.forageProviderList.Count - 1))
            {
                // if the host matches
                if (this.forageProviderList[i].HostID == hostID)
                    provider = this.forageProviderList[i];
                i++;
            }
            return provider;
        }

        /// <summary>
        /// Get a forage provider from the list. idx = 0..n
        /// </summary>
        /// <param name="idx">The index</param>
        /// <returns>The forage provider</returns>
        public ForageProvider ForageProvider(int idx)
        {
            if ((idx >= 0) && (idx < this.forageProviderList.Count))
                return this.forageProviderList[idx];
            else
                return null;
        }
    }
}