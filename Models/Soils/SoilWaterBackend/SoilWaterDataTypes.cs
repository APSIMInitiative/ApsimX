using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SoilWaterBackend
    {


    [Serializable]
    public class Constants
        {


        #region Bounds checking and warning functions

        //TODO: Refactor this into the Utility namespace later.

        public Summary Summary;
        public SoilWater thismodel;

        public void IssueWarning(string warningText)
            {
            Summary.WriteWarning(thismodel, warningText);
            }

        public double bound(double A, double Lower, double Upper)
            {
            //force A to stay between the Lower and the Upper. Set A to the Upper or Lower if it exceeds them.
            if (Lower > Upper)
                {
                IssueWarning("Lower bound " + Lower + " is > upper bound " + Upper + "\n"
                                   + "        Variable is not constrained");
                return A;
                }
            else
                return Math.Max(Math.Min(A, Upper), Lower);
            }


        // Unlike u_bound and l_bound, this does not force the variable to be between the bounds. It just warns the user in the summary file.
        public void bound_check_real_var(double Variable, double LowerBound, double UpperBound, string VariableName)
            {
            string warningMsg;
            if (Variable > UpperBound)
                {
                warningMsg = "The variable: /'" + VariableName + "/' is above the expected upper bound of: " + UpperBound;
                IssueWarning(warningMsg);
                }
            if (Variable < LowerBound)
                {
                warningMsg = "The variable: /'" + VariableName + "/' is below the expected lower bound of: " + LowerBound;
                IssueWarning(warningMsg);
                }
            }

        public void bound_check_real_array(double[] A, double LowerBound, double UpperBound, string ArrayName, int ElementToStopChecking)
            {
            for (int i = 0; i < ElementToStopChecking; i++)
                {
                bound_check_real_var(A[i], LowerBound, UpperBound, ArrayName + "(" + i + 1 + ")");
                }
            }


        #endregion



        #region Constants

        public const double precision_sw_dep = 1.0e-3; //!Precision for sw dep (mm)
        public const int ritchie_method = 1;
        public const double mm2m = 1.0 / 1000.0;      //! conversion of mm to m
        public const double sm2smm = 1000000.0;       //! conversion of square metres to square mm
        public const double error_margin = 0.0001;

        #endregion




        #region Module Constants (from SIM file but it gets from INI file)


        public double min_crit_temp;             //! temperature below which eeq decreases (oC)



        public double max_crit_temp;             //! temperature above which eeq increases (oC)



        public double max_albedo;                //! maximum bare ground soil albedo (0-1)



        public double A_to_evap_fact;            //! factor to convert "A" to coefficient in Adam's type residue effect on Eos


        public double canopy_eos_coef;           //! coef in cover Eos reduction eqn



        public double sw_top_crit;               //! critical sw ratio in top layer below which stage 2 evaporation occurs



        public double sumes1_max;                //! upper limit of sumes1



        public double sumes2_max;                //! upper limit of sumes2


        public double[] solute_flow_eff;          //sv- Unsaturated Flow   //! efficiency of moving solute with flow (0-1)


        public double[] solute_flux_eff;         //sv- Drainage (Saturated Flow)   //! efficiency of moving solute with flux (0-1) 


        public double gravity_gradient;          //! gradient due to hydraulic differentials (0-1)


        public double specific_bd;               //! specific bulk density (g/cc)


        public double hydrol_effective_depth;    //! hydrologically effective depth for runoff (mm)


        public string[] mobile_solutes;    //! names of all possible mobile solutes


        public string[] immobile_solutes;   //! names of all possible immobile solutes


        public double[] canopy_fact;        //! canopy factors for cover runoff effect ()


        public double[] canopy_fact_height; //! heights for canopy factors (mm)


        public double canopy_fact_default;       //! default canopy factor in absence of height ()



        public string act_evap_method;           //! actual soil evaporation model being used //sv- hard wired to "ritchie" in the init event handler. 


        #endregion



        }



    [Serializable]
    public class CanopyData
        {


        //GET CROP VARIABLES
        public double[] cover_tot = null;     //! total canopy cover of crops (0-1)
        public double[] cover_green = null;   //! green canopy cover of crops (0-1)
        public double[] canopy_height = null; //! canopy heights of each crop (mm)
        public int NumberOfCrops = 0;                //! number of crops ()



        public double interception;  //Not actually from Canopy actually from Hamish Brown Manager Script


        //fraction of light intercepted by the green canopy
        //! sum of crop green covers (0-1)
        public double cover_green_sum
            {
            get
                {
                double green_sum = 0.0;
                for (int crop = 0; crop < NumberOfCrops; ++crop)
                    green_sum = 1.0 - (1.0 - green_sum) * (1.0 - cover_green[crop]);

                return green_sum;
                }
            set { }
            }

        //fraction of light intercepted by the canopy (green leaves plus dead leaves still on the plant)
        //! sum of total crop covers (0-1)
        public double cover_tot_sum
            {
            get
                {
                double tot_sum = 0.0;
                for (int i = 0; i < NumberOfCrops; i++)
                    tot_sum = 1.0 - (1.0 - tot_sum) * (1.0 - cover_tot[i]);

                return tot_sum;
                }
            set { }
            }


        public void ZeroCanopyData()
            {
            //GetTodaysCanopyData() will resize these arrays each day.
            cover_tot = new double[0];     
            cover_green = new double[0];
            canopy_height = new double[0];  
            NumberOfCrops = 0;    
            }

        }



    [Serializable]
    public class SurfaceCoverData
        {
        public double surfaceom_cover;

        public double residueinterception;  //Not actually from SurfaceOM actually from Hamish Brown Manager Script

        public void ZeroSurfaceCover()
            {
            surfaceom_cover = 0.0;
            }

        }


    [Serializable]
    public class IrrigData
        {

        public double irrigation;           //amount of irrigation (mm)  
        public int irrigation_layer;        //layer to which irrigation is applied. This is one based. eg. surface is layer 1.
        public bool irrigation_will_runoff; //will the irrigation runoff like rain. (0 means no runoff [default], 1 means it will runoff just like rainfall.)
        

        public double NO3;
        public double NH4;
        public double CL;

        public IrrigData()
            {
            //all the other values should already be initialised to zero (or false) which is what you want them to be.
            irrigation_layer = 1;
            }


        public void ZeroIrrigation()
            {
            irrigation = 0.0;
            irrigation_will_runoff = false;
            irrigation_layer = 1;

            NO3 = 0.0;
            NH4 = 0.0;
            CL = 0.0;

            }

        }



    [Serializable]
    public class MetData
        {

            //sv- These met variables get assigned by the OnNewMet Event Handler
            public double rain;         //! precipitation (mm/d)
            public double radn;         //! solar radiation (mj/m^2/day)
            public double mint;         //! minimum air temperature (oC)
            public double maxt;         //! maximum air temperature (oC)

        }



    }
