using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Phen;
using APSIM.Shared.Utilities;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// Population model for old plant
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant15))]
    public class Population1 : Model
    {
        /// <summary>The plant</summary>
        [Link]
        Plant15 Plant = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>The _ plants</summary>
        double _Plants;

        /// <summary>Gets or sets the density.</summary>
        /// <value>The density.</value>
        public double Density
        {
            get
            {
                return _Plants;
            }
            set
            {
                _Plants = value;
            }
        }

        /// <summary>Called when [sow].</summary>
        /// <param name="Sow">The sow.</param>
        public void OnSow(SowPlant2Type Sow)
        {
            _Plants = Sow.Population;
        }

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The sw stress</summary>
        [Link]
        SWStress SWStress = null;

        /// <summary>The leaf</summary>
        [Link]
        Leaf1 Leaf = null;

        /// <summary>The crop failure stress period</summary>
        [Link]
        IFunction CropFailureStressPeriod = null;
        /// <summary>The death high temperature during emergence</summary>
        [Link]
        IFunction DeathHighTemperatureDuringEmergence = null;

        /// <summary>Gets or sets the leaf number critical.</summary>
        /// <value>The leaf number critical.</value>
        public double LeafNumberCritical { get; set; }

        /// <summary>Gets or sets the tt emergence limit.</summary>
        /// <value>The tt emergence limit.</value>
        public double TTEmergenceLimit { get; set; }

        /// <summary>Gets or sets the days to germination limit.</summary>
        /// <value>The days to germination limit.</value>
        public double DaysToGerminationLimit { get; set; }

        /// <summary>Gets or sets the sw stress pheno limit.</summary>
        /// <value>The sw stress pheno limit.</value>
        public double SWStressPhenoLimit { get; set; }

        /// <summary>Gets or sets the sw stress photo limit.</summary>
        /// <value>The sw stress photo limit.</value>
        public double SWStressPhotoLimit { get; set; }

        /// <summary>Gets or sets the sw stress photo rate.</summary>
        /// <value>The sw stress photo rate.</value>
        public double SWStressPhotoRate { get; set; }

        /// <summary>The das</summary>
        private int das;
        /// <summary>The dlt_plants_failure_germ</summary>
        private double dlt_plants_failure_germ;
        /// <summary>The dlt_plants_failure_emergence</summary>
        private double dlt_plants_failure_emergence;
        /// <summary>The dlt_plants_death_seedling</summary>
        private double dlt_plants_death_seedling;
        /// <summary>The dlt_plants_failure_leaf_sen</summary>
        private double dlt_plants_failure_leaf_sen;
        /// <summary>The dlt_plants_failure_phen_delay</summary>
        private double dlt_plants_failure_phen_delay;
        /// <summary>The dlt_plants_death_drought</summary>
        private double dlt_plants_death_drought;
        /// <summary>The dlt_plants</summary>
        private double dlt_plants;
        /// <summary>The cum sw stress pheno</summary>
        private double CumSWStressPheno = 0;
        /// <summary>The cum sw stress photo</summary>
        private double CumSWStressPhoto = 0;
        /// <summary>The dlt_plants_death_external</summary>
        private double dlt_plants_death_external;

        /// <summary>This is only called from a Plant1 process method - not used in Plant2.</summary>
        /// <returns></returns>
        internal bool PlantDeath()
        {
            das++;
            if (Phenology.InPhase("sowing"))
                dlt_plants_failure_germ = CropFailureGermination();

            else
                dlt_plants_failure_germ = 0.0;

            if (Phenology.InPhase("germination"))
                dlt_plants_failure_emergence = CropFailureEmergence();
            else
                dlt_plants_failure_emergence = 0.0;

            dlt_plants_death_seedling = 0.0;
            if (Phenology.OnDayOf("emergence"))
                dlt_plants_death_seedling = DeathSeedling();

            /*XXXX this needs to be coupled with dlt_leaf_area_sen, c_sen_start_stage  FIXME*/
            if (!Phenology.InPhase("SowingToGermination") && !Phenology.InPhase("GerminationToEmergence"))
                dlt_plants_failure_leaf_sen = CropFailureLeafSen();
            else
                dlt_plants_failure_leaf_sen = 0.0;

            if (CropFailureStressPeriod.Value == 1)
                dlt_plants_failure_phen_delay = CropFailurePhenDelay();
            else
                dlt_plants_failure_phen_delay = 0.0;

            if (CropFailureStressPeriod.Value == 1)
                dlt_plants_death_drought = DeathDrought();
            else
                dlt_plants_death_drought = 0.0;

            DeathActual();
            Util.Debug("Population.dlt_plants=%f", dlt_plants);
            if (MathUtilities.FloatsAreEqual(dlt_plants + Density, 0.0))
            {
                //!!!!! fix problem with deltas in update when change from alive to dead ?zero deltas

                double biomass = 0;
                foreach (Organ1 Organ in Plant.Tops)
                    biomass += (Organ.Live.Wt + Organ.Dead.Wt) * Conversions.gm2kg / Conversions.sm2ha;

                // report
                string msg = "Plant death. standing above-ground dm = " + biomass.ToString("f2") + " (kg/ha)";
                Summary.WriteWarning(this, msg);
                return true;
                // XX Needs to signal a need to call zero_variables here...
                // Present method is to rely on calling zero_xx at tomorrow's prepare() event.. :(
            }
            return false;
        }

        /// <summary>Updates this instance.</summary>
        internal void Update()
        {
            if (CropFailureStressPeriod.Value == 1)
            {
                CumSWStressPheno += (1 - SWStress.Pheno);
                CumSWStressPhoto += (1 - SWStress.Photo);
            }
            _Plants += dlt_plants;
            Util.Debug("Population.Density=%f", Density);
        }

        /// <summary>Crop failure from lack of germination within a specific maximum number of days.</summary>
        /// <returns></returns>
        private double CropFailureGermination()
        {
            if (das >= DaysToGerminationLimit)
            {
                Summary.WriteWarning(this, "      crop failure because of lack of\r\n" +
                                  "         germination within " + DaysToGerminationLimit.ToString() +
                                  " days of sowing");
                return -1.0 * Density;
            }
            return 0.0;
        }

        /// <summary>
        /// Crop failure from lack of emergence within a specific maximum
        /// thermal time sum from germination.
        /// </summary>
        /// <returns></returns>
        private double CropFailureEmergence()
        {
            if (Phenology.CurrentPhase is GerminatingPhase && Phenology.CurrentPhase.TTinPhase > TTEmergenceLimit)
            {
                Summary.WriteWarning(this, " failed emergence due to deep planting");
                return -1.0 * Density;
            }
            return 0.0;
        }

        /// <summary>Determine plant death from prolonged phenology delay.</summary>
        /// <returns></returns>
        private double CropFailurePhenDelay()
        {
            if (CumSWStressPheno >= SWStressPhenoLimit)
            {
                Summary.WriteWarning(this, "Crop failure because of prolonged phenology delay through water stress.");
                return -1.0 * Density;
            }
            return 0.0;
        }

        /// <summary>Determine plant population death from leaf area senescing</summary>
        /// <returns></returns>
        private double CropFailureLeafSen()
        {
            double leaf_area = MathUtilities.Divide(Leaf.LAI, Density, 0.0); // leaf area per plant

            if (MathUtilities.FloatsAreEqual(leaf_area, 0.0, 1.0e-6))
            {
                Summary.WriteWarning(this, "Crop failure because of total leaf senescence.");
                return -1.0 * Density;
            }
            return 0.0;
        }

        /// <summary>Determine seedling death rate due to high soil temperatures during emergence.</summary>
        /// <returns></returns>
        private double DeathSeedling()
        {
            // Calculate fraction of plants killed by high temperature
            double killfr = DeathHighTemperatureDuringEmergence.Value;
            double dlt_plants = -Density * killfr;

            if (killfr > 0.0)
            {
                string msg = "Plant kill. ";
                msg = msg + (killfr * Conversions.fract2pcnt).ToString("f2");
                msg = msg + "% failure because of high soil surface temperatures.";
                Summary.WriteWarning(this, msg);
            }
            return dlt_plants;
        }

        /// <summary>Determine plant death rate due to drought</summary>
        /// <returns></returns>
        private double DeathDrought()
        {
            double killfr;                                 // fraction of crop population to kill
            double dlt_plants = 0.0;                       // population to kill

            if (Leaf.LeafNumber < LeafNumberCritical &&
                CumSWStressPhoto > SWStressPhotoLimit &&
                !MathUtilities.FloatsAreEqual(SWStress.Photo, 1.0))
            {
                killfr = SWStressPhotoRate * (CumSWStressPhoto - SWStressPhotoLimit);
                killfr = MathUtilities.Constrain(killfr, 0.0, 1.0);
                dlt_plants = -Density * killfr;

                string msg = "Plant kill. ";
                msg = msg + (killfr * Conversions.fract2pcnt).ToString("f2");
                msg = msg + "% failure because of water stress.";
                Summary.WriteWarning(this, msg);
            }
            return dlt_plants;
        }

        /// <summary>Determine plant death rate due to a range of given processes</summary>
        private void DeathActual()
        {
            // dlt's are negative so take minimum.
            double pmin = dlt_plants_failure_germ;             // Progressive minimum
            pmin = Math.Min(pmin, dlt_plants_failure_emergence);
            pmin = Math.Min(pmin, dlt_plants_failure_leaf_sen);
            pmin = Math.Min(pmin, dlt_plants_failure_phen_delay);
            pmin = Math.Min(pmin, dlt_plants_death_drought);
            pmin = Math.Min(pmin, dlt_plants_death_seedling);
            pmin = Math.Min(pmin, dlt_plants_death_external);
            dlt_plants = pmin;
            dlt_plants_death_external = 0.0;                //Ugly hack here??
        }


        /// <summary>Gets the dying fraction plants.</summary>
        /// <value>The dying fraction plants.</value>
        public double DyingFractionPlants
        {
            get
            {
                double dying_fract_plants = MathUtilities.Divide(-dlt_plants, Density, 0.0);
                return MathUtilities.Constrain(dying_fract_plants, 0.0, 1.0);
            }
        }

        /// <summary>Kills the crop.</summary>
        /// <param name="KillFraction">The kill fraction.</param>
public void KillCrop(double KillFraction)
//=======================================================================================
// Event Handler for Kill Crop Event
   {
   if (Plant.plant_status != "out")
      {
      //bound_check_real_var(scienceAPI, Kill.KillFraction, 0.0, 1.0, "KillFraction");
      dlt_plants_death_external = dlt_plants_death_external - Density * KillFraction;

      if (KillFraction > 0.0)
         {
         string msg= "Plant kill. ";
          double KillPC = KillFraction*100;
         msg = msg + KillPC.ToString("f2");
         msg = msg + "% crop killed because of external action.";
         Summary.WriteMessage(this,msg);
         }
      }
   else
      {
      string msg = Plant.Name + " is not in the ground - unable to kill crop.";
      Summary.WriteMessage(this,msg);
      }
   }





    }
}
