using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SoilWaterBackend
    {


    /// <summary>
    /// Soil water constants
    /// </summary>
    [Serializable]
    public class Constants
        {


        #region Bounds checking and warning functions

        //TODO: Refactor this into the Utility namespace later.

            /// <summary>
            /// The summary
            /// </summary>
        public ISummary Summary;
        /// <summary>
        /// The thismodel
        /// </summary>
        public SoilWater thismodel;

        /// <summary>
        /// Issues the warning.
        /// </summary>
        /// <param name="warningText">The warning text.</param>
        public void IssueWarning(string warningText)
            {
            Summary.WriteWarning(thismodel, warningText);
            }

        /// <summary>
        /// Bounds the specified a.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="Lower">The lower.</param>
        /// <param name="Upper">The upper.</param>
        /// <returns></returns>
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
        /// <summary>
        /// Bound_check_real_vars the specified variable.
        /// </summary>
        /// <param name="Variable">The variable.</param>
        /// <param name="LowerBound">The lower bound.</param>
        /// <param name="UpperBound">The upper bound.</param>
        /// <param name="VariableName">Name of the variable.</param>
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

        /// <summary>
        /// Bound_check_real_arrays the specified a.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="LowerBound">The lower bound.</param>
        /// <param name="UpperBound">The upper bound.</param>
        /// <param name="ArrayName">Name of the array.</param>
        /// <param name="ElementToStopChecking">The element to stop checking.</param>
        public void bound_check_real_array(double[] A, double LowerBound, double UpperBound, string ArrayName, int ElementToStopChecking)
            {
            for (int i = 0; i < ElementToStopChecking; i++)
                {
                bound_check_real_var(A[i], LowerBound, UpperBound, ArrayName + "(" + i + 1 + ")");
                }
            }


        #endregion



        #region Constants

        /// <summary>
        /// The precision_sw_dep
        /// </summary>
        public const double precision_sw_dep = 1.0e-3; //!Precision for sw dep (mm)
        /// <summary>
        /// The ritchie_method
        /// </summary>
        public const int ritchie_method = 1;
        /// <summary>
        /// The MM2M
        /// </summary>
        public const double mm2m = 1.0 / 1000.0;      //! conversion of mm to m
        /// <summary>
        /// The SM2SMM
        /// </summary>
        public const double sm2smm = 1000000.0;       //! conversion of square metres to square mm
        /// <summary>
        /// The error_margin
        /// </summary>
        public const double error_margin = 0.0001;

        #endregion




        #region Module Constants (from SIM file but it gets from INI file)


        /// <summary>
        /// The a_to_evap_fact
        /// </summary>
        public double A_to_evap_fact;            //! factor to convert "A" to coefficient in Adam's type residue effect on Eos


        /// <summary>
        /// The canopy_eos_coef
        /// </summary>
        public double canopy_eos_coef;           //! coef in cover Eos reduction eqn



        /// <summary>
        /// The sw_top_crit
        /// </summary>
        public double sw_top_crit;               //! critical sw ratio in top layer below which stage 2 evaporation occurs



        /// <summary>
        /// The sumes1_max
        /// </summary>
        public double sumes1_max;                //! upper limit of sumes1



        /// <summary>
        /// The sumes2_max
        /// </summary>
        public double sumes2_max;                //! upper limit of sumes2


        /// <summary>
        /// The solute_flow_eff
        /// </summary>
        public double[] solute_flow_eff;          //sv- Unsaturated Flow   //! efficiency of moving solute with flow (0-1)


        /// <summary>
        /// The solute_flux_eff
        /// </summary>
        public double[] solute_flux_eff;         //sv- Drainage (Saturated Flow)   //! efficiency of moving solute with flux (0-1) 


        /// <summary>
        /// The gravity_gradient
        /// </summary>
        public double gravity_gradient;          //! gradient due to hydraulic differentials (0-1)


        /// <summary>
        /// The specific_bd
        /// </summary>
        public double specific_bd;               //! specific bulk density (g/cc)


        /// <summary>
        /// The hydrol_effective_depth
        /// </summary>
        public double hydrol_effective_depth;    //! hydrologically effective depth for runoff (mm)


        /// <summary>
        /// The mobile_solutes
        /// </summary>
        public string[] mobile_solutes;    //! names of all possible mobile solutes


        /// <summary>
        /// The immobile_solutes
        /// </summary>
        public string[] immobile_solutes;   //! names of all possible immobile solutes


        /// <summary>
        /// The canopy_fact
        /// </summary>
        public double[] canopy_fact;        //! canopy factors for cover runoff effect ()


        /// <summary>
        /// The canopy_fact_height
        /// </summary>
        public double[] canopy_fact_height; //! heights for canopy factors (mm)


        /// <summary>
        /// The canopy_fact_default
        /// </summary>
        public double canopy_fact_default;       //! default canopy factor in absence of height ()



        /// <summary>
        /// The act_evap_method
        /// </summary>
        public string act_evap_method;           //! actual soil evaporation model being used //sv- hard wired to "ritchie" in the init event handler. 


        #endregion



        }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CanopyData
        {

        /// <summary>
        /// This passes the potential infiltration into the SoilWaterSurface.  Not the best place for it but will get thinks moving
        /// </summary>
        public double PotentialInfiltration { get; set; }

        //GET CROP VARIABLES
            /// <summary>
            /// The cover_tot
            /// </summary>
        public double[] cover_tot = null;     //! total canopy cover of crops (0-1)
        /// <summary>
        /// The cover_green
        /// </summary>
        public double[] cover_green = null;   //! green canopy cover of crops (0-1)
        /// <summary>
        /// The canopy_height
        /// </summary>
        public double[] canopy_height = null; //! canopy heights of each crop (mm)
        /// <summary>
        /// The number of crops
        /// </summary>
        public int NumberOfCrops = 0;                //! number of crops ()



        /// <summary>
        /// The interception
        /// </summary>
        public double interception;  //Not actually from Canopy actually from Hamish Brown Manager Script


        //fraction of light intercepted by the green canopy
        //! sum of crop green covers (0-1)
        /// <summary>
        /// Gets or sets the cover_green_sum.
        /// </summary>
        /// <value>
        /// The cover_green_sum.
        /// </value>
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
        /// <summary>
        /// Gets or sets the cover_tot_sum.
        /// </summary>
        /// <value>
        /// The cover_tot_sum.
        /// </value>
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


        /// <summary>
        /// Zeroes the canopy data.
        /// </summary>
        public void ZeroCanopyData()
            {
            //GetTodaysCanopyData() will resize these arrays each day.
            cover_tot = new double[0];     
            cover_green = new double[0];
            canopy_height = new double[0];  
            NumberOfCrops = 0;    
            }

        }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SurfaceCoverData
        {
            /// <summary>
            /// The surfaceom_cover
            /// </summary>
        public double surfaceom_cover;

        /// <summary>
        /// The residueinterception
        /// </summary>
        public double residueinterception;  //Not actually from SurfaceOM actually from Hamish Brown Manager Script

        /// <summary>
        /// Zeroes the surface cover.
        /// </summary>
        public void ZeroSurfaceCover()
            {
            surfaceom_cover = 0.0;
            }

        }


    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class IrrigData
        {
        
        /// <summary>
        /// The amount
        /// </summary>
        public double amount;           //amount of irrigation (mm)  
        /// <summary>
        /// The is sub surface
        /// </summary>
        public bool isSubSurface;
        /// <summary>
        /// The layer
        /// </summary>
        public int layer;        //layer to which irrigation is applied. This is one based. eg. surface is layer 1.
        /// <summary>
        /// The will runoff
        /// </summary>
        public bool willRunoff; //will the irrigation runoff like rain. (0 means no runoff [default], 1 means it will runoff just like rainfall.)


        /// <summary>
        /// The n o3
        /// </summary>
        public double NO3;
        /// <summary>
        /// The n h4
        /// </summary>
        public double NH4;
        /// <summary>
        /// The cl
        /// </summary>
        public double CL;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrrigData"/> class.
        /// </summary>
        public IrrigData()
            {
            layer = 1;
            amount = 0.0;
            isSubSurface = false;
            willRunoff = false;
            layer = 1;

            NO3 = 0.0;
            NH4 = 0.0;
            CL = 0.0;

            }

        }



    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class MetData
        {

            //sv- These met variables get assigned by the OnNewMet Event Handler
            /// <summary>
            /// The rain
            /// </summary>
            public double rain;         //! precipitation (mm/d)
            /// <summary>
            /// The radn
            /// </summary>
            public double radn;         //! solar radiation (mj/m^2/day)
            /// <summary>
            /// The mint
            /// </summary>
            public double mint;         //! minimum air temperature (oC)
            /// <summary>
            /// The maxt
            /// </summary>
            public double maxt;         //! maximum air temperature (oC)

        }



    }
