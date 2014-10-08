using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections; //needed for IEnumerable interface.

namespace Models.Soils.SoilWaterBackend
    {




    //SOLUTE IN A LAYER
    //*****************

    #region Solute in a layer


    /// <summary>
    /// This is an individual solute in an individual layer of the Soil
    /// </summary>
    [Serializable]
    public class SoluteInLayer
        {

        public string name = "";        // Name of the solute
        public string ownerName = "";    // FQN of the component handling this solute
        public bool mobility = false;      // Is the solute mobile?
        public double amount;    // amount of solute in each layer (kg/ha)
        public double delta;     // change in solute in each layer (kg/ha)
        public double up;        // amount "upped" from each layer (kg/ha)
        public double leach;     // amount leached from each layer (kg/ha)


        public SoluteInLayer(string Name, string OwnerName, bool Mobility)
            {

            //new solute event sets these 3 variables
            name = Name;
            ownerName = OwnerName;
            mobility = Mobility;

            //get this value each day using this module changes this value
            amount = 0.0;

            //this module calculates these 3 deltas as outputs (but only for mobile solutes)
            delta = 0.0;
            up = 0.0;
            leach = 0.0;
            }
        }



    #endregion






    //LAYER OF THE SOIL
    //*****************

    #region Layer of the Soil

    /// <summary>
    /// This is an individual Layer of the Soil
    /// </summary>
    [Serializable]
    public class Layer : IComparable
        {


        //Default Comparer for a Layer (used by the Soil's List of Layers to do a List.Sort()) 
        public int CompareTo(Object ObjectToCompareThisTo)
            {
            Layer LayerToCompareThisTo = ObjectToCompareThisTo as Layer;
            if (LayerToCompareThisTo == null)
                return 1;
            else
                return this.number.CompareTo(LayerToCompareThisTo.number);
            }


        //CONSTRUCTOR

        public Layer()
            {
            solutes = new List<SoluteInLayer>();
            }


        //PROPERTIES OF A LAYER


        public int number;

        public double dlayer;    //! thickness of soil layer (mm)

        public double bd;      //! moist bulk density of soil (g/cm^3) // ??? Is this "moist" or "dry"; how moist?



        public double sat
            {
            get { return MM2Frac(sat_dep); }
            set { sat_dep = Frac2MM(value); }
            }
        public double dul
            {
            get { return MM2Frac(dul_dep); }
            set { dul_dep = Frac2MM(value); }
            }
        public double ll15
            {
            get { return MM2Frac(ll15_dep); }
            set { ll15_dep = Frac2MM(value); }
            }
        public double air_dry
            {
            get { return MM2Frac(air_dry_dep); }
            set { air_dry_dep = Frac2MM(value); }
            }


        public double Frac2MM(double Fraction)
            {
            return Fraction * dlayer;
            }

        public double MM2Frac(double MM)
            {
            return Utility.Math.Divide(MM, dlayer, 0.0);
            }




        public double sat_dep;      //! saturated water content for this layer (mm water)


        public double dul_dep;      //! drained upper limit soil water content for this layer (mm water)


        public double ll15_dep;     //! 15 bar lower limit of extractable soil water for this layer (mm water)


        public double air_dry_dep;  //! air dry soil water content for this layer (mm water)



        //VARIABLES


        public double sw_dep;    // sw * dlayer //see soilwat2_init() for initialisation

        public double sw        //! soil water content of layer
            {
            get { return MM2Frac(sw_dep); }
            set { sw_dep = Frac2MM(value); }
            }

        public double esw                    //! potential extractable sw in profile 
            { 
            get { 
                double result =  sw_dep - ll15_dep;
                if (result > 0.0)
                    return result;
                else
                    return 0.0;
                } 
            }  


        public double amnt_to_sat           //change this to sw_to_sat
        { get { return sat_dep - sw_dep; } }

        public double amnt_to_dul           //change this to sw_to_dul
        { get { return dul_dep - sw_dep; } }

        public bool isDrainable             //change this to sw_above_dul
        { get { return (sw_dep > dul_dep) ? true : false; } }

        public double drainable_capacity    //change this to dul_to_sat
        { get { return sat_dep - dul_dep; } }

        public double drainable             //change this to amnt_drainable
            {
            get
                {
                if (isDrainable)
                    return sw_dep - dul_dep;
                else
                    return 0.0;
                }
            }
        public double saturated_fraction
        { get { return Utility.Math.Divide(drainable, drainable_capacity, 0.0); } }



        public bool layer_is_fully_saturated //shorten to is_fully_saturated
            {
            get {
                //change this to sw_dep >= sat_dep
                if (saturated_fraction >= 0.999999)
                    return true;
                else
                    return false;
                }
            }

        public bool layer_is_saturated  //shorten to is_saturated or is_above_dul (Actually this is the same as isDrainable)
            {
            get {
                //change this to sw_dep > dul_dep
                if (saturated_fraction > 0.0)
                    return true;
                else
                    return false;

                }

            }





        //SW MOVEMENT PROPERTIES 

        public double ks;        //! saturated conductivity (mm/d)

        public double SWCON;     //! soil water conductivity constant (1/d) //! ie day**-1 for each soil layer

        public double KLAT;





        //DELTA_SW VARIABLES


        public double flow;        //sv- Unsaturated Flow //! depth of water moving from layer i+1 into layer i because of unsaturated flow; (positive value indicates upward movement into layer i) (negative value indicates downward movement (mm) out of layer i)

        public double flux;       //sv- Drainage (Saturated Flow) //! initially, water moving downward into layer i (mm), then water moving downward out of layer i (mm)

        //public double flow_u;   //sv- unsaturated flow
        //public double flow_s;   //sv- saturated flow
        //public double flow_a;   //sv- excess water flow  (above saturfation flow).
        //public double flow      //sv- actual flow (aka. flow_water)

        public double flow_water         //flow_water[layer] = flux[layer] - flow[layer] 
        { get { return flux - flow; } }


        public double flow2;

        public double flux2;


        public double outflow_lat;   //! outflowing lateral water   //lateral outflow


        //temporary variables. Refactor and remove these later.
        public double new_sw_dep;
        public double solute_up;
        public double remain;
        public double solute_down;





        //SOLUTES IN THIS LAYER

        private List<SoluteInLayer> solutes;  //The List of solutes in this layer
        
        public int num_solutes { get { return solutes.Count; } }


        public void AddSolute(SoluteInLayer NewSolute)
            {
            solutes.Add(NewSolute);
            }


        public void RemoveSolute(string SoluteName)
            {
            SoluteInLayer solute;
            solute = GetASolute(SoluteName);
            solutes.Remove(solute);
            }


        public SoluteInLayer GetASolute(string SoluteName)
            {
            SoluteInLayer foundSolute;
            foundSolute = solutes.Find(delegate(SoluteInLayer sol)
            {
                return sol.name == SoluteName;
            }
                                        );
            return foundSolute;
            }


        public List<SoluteInLayer> GetAllSolutes()
            {
            return solutes;
            }


        public List<SoluteInLayer> GetAllMobileSolutes()
            {
            List<SoluteInLayer> mobiles = new List<SoluteInLayer>();
            foreach (SoluteInLayer sol in solutes)
                {
                if (sol.mobility)
                    mobiles.Add(sol);
                }

            return mobiles;
            }

        public List<string> GetMobileSoluteNames()
            {
            List<string> mobileNames = new List<string>();
            foreach (SoluteInLayer sol in solutes)
                {
                if (sol.mobility)
                    mobileNames.Add(sol.name);
                }
            return mobileNames;
            }


        public void ZeroMobileSolutesDeltas()
            {
            foreach (SoluteInLayer sol in GetAllMobileSolutes())
                {
                sol.delta = 0.0;
                sol.up = 0.0;
                sol.leach = 0.0;
                }
            }


        }



    #endregion






    //THE SOIL
    //********

    #region The Soil

    /// <summary>
    /// The Soil. 
    /// (Also contains methods for modifying the soil
    /// as well as the natural movement of water/solutes within the soil)
    /// </summary>
    [Serializable]
    public class SoilWaterSoil : IEnumerable
        {


        public Constants Constants;

        private bool using_ks;       //! flag to determine if Ks has been chosen for use. //sv- set in soilwat2_init() by checking if mwcon exists




        //SOIL PROPERTIES (NOT LAYERED)


        //different evap for summer and winter
        //summer

        public string SummerDate;
        public double SummerU;
        public double SummerCona;

        //winter
        public string WinterDate;
        public double WinterU;
        public double WinterCona;


        public double DiffusConst;      
        public double DiffusSlope;         


        public double Salb;           


        public double cn2_bare;
        public double cn_red;
        public double cn_cov;



        //Lateral flow properties  

        public double slope;
        public double discharge_width; 
        public double catchment_area;   



        public double max_pond;






        //OUTPUTS


        #region Single Values

        public double DepthToWaterTable;
        public double Drainage;         //Drainage out of the bottom layer.   

        public double esw
            {
            get
                {
                //sum esw over the profile and give single total value
                double result = 0.0;
                foreach (Layer lyr in layers)
                    {
                    result =  result + lyr.esw;
                    }
                return result;
                }
            }

        #endregion


        #region Output as an Array



        public double[] dlayer
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.dlayer;
                    }
                return result;
                }
            }



        //ARRAYS IN MILLIMETERS

        public double[] sat_dep
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.sat_dep;
                    }
                return result;
                }
            }

        public double[] dul_dep
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.dul_dep;
                    }
                return result;
                }
            }


        public double[] sw_dep
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.sw_dep;
                    }
                return result;
                }
            }



        public double[] ll15_dep
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.ll15_dep;
                    }
                return result;
                }

            }

        public double[] air_dry_dep
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.air_dry_dep;
                    }
                return result;
                }
            }




        //ARRAYS AS FRACTIONS


        public double[] sat
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.sat;
                    }
                return result;
                }
            }

        public double[] dul
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.dul;
                    }
                return result;
                }
            }


        public double[] sw
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.sw;
                    }
                return result;
                }
            }



        public double[] ll15
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.ll15;
                    }
                return result;
                }

            }

        public double[] air_dry
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.air_dry;
                    }
                return result;
                }
            }







        public double[] flow
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.flow;
                    }
                return result;
                }
            }


        public double[] flux
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.flux;
                    }
                return result;
                }
            }


        public double[] outflow_lat
            {
            get
                {
                double[] result = new double[num_layers];
                foreach (Layer lyr in layers)
                    {
                    result[lyr.number - 1] = lyr.outflow_lat;
                    }
                return result;
                }
            }




        #endregion








        //SOIL LAYERS


        private List<Layer> layers;


        #region Enumerators

        //http://msdn.microsoft.com/en-us/library/vstudio/65zzykke(v=vs.100).aspx

        private int top { get { return 1; } }  //one based
        private int bottom { get { return layers.Count; } }  //one based 


        /// <summary>
        /// Default Iterator from top layer to bottom layer.
        /// </summary>
        public IEnumerator GetEnumerator()
            {
            layers.Sort();

            for (int i = top; i <= bottom; i++)
                {
                yield return layers[i - 1];
                }

            }

        /// <summary>
        /// Iterator from bottom layer to top layer.
        /// </summary>
        public IEnumerable BottomToTop()
            {
            layers.Sort();

            for (int i = bottom; i <= top; i--)
                {
                yield return layers[i - 1];
                }
            }


        /// <summary>
        /// Iterator from top layer to X layer.
        /// Including layer X.
        /// </summary>
        /// <param name="X">X is 1 based</param>
        public IEnumerable TopToX(int X)
            {
            layers.Sort();
            for (int i = top; i <= X; i++)
                {
                yield return layers[i - 1];
                }
            }


        /// <summary>
        /// Iterator from X layer to top layer.
        /// Including layer X.
        /// </summary>
        /// <param name="X">X is 1 based</param>
        public IEnumerable XToTop(int X)
            {
            layers.Sort();
            for (int i = X; i <= top; i--)
                {
                yield return layers[i - 1];
                }
            }


        /// <summary>
        /// Iterator from X layer to bottom layer.
        /// Including layer X.
        /// </summary>
        /// <param name="X">X is 1 based</param>
        public IEnumerable XToBottom(int X)
            {
            layers.Sort();
            for (int i = X; i <= bottom; i++)
                {
                yield return layers[i - 1];
                }
            }


        /// <summary>
        /// Iterator from bottom layer to layer X.
        /// Including layer X.
        /// </summary>
        /// <param name="X">X is 1 based</param>
        public IEnumerable BottomToX(int X)
            {
            layers.Sort();
            for (int i = X; i <= bottom; i++)
                {
                yield return layers[i - 1];
                }
            }



        #endregion




        #region Get Layer Functions


        public Layer GetTopLayer()
            {
            if (layers != null)
                {
                layers.Sort();
                return layers.First();
                }
            else
                return null;
            }


        /// <summary>
        /// Returns the specified layer 
        /// </summary>
        /// <param name="LayerNumber">LayerNumber is 1 based</param>
        public Layer GetLayer(int LayerNumber)
            {
            if (layers != null)
                {
                layers.Sort();
                return layers[LayerNumber - 1];
                }
            else
                return null;
            }


        public Layer GetBottomLayer()
            {
            if (layers != null)
                {
                layers.Sort();
                return layers.Last();
                }
            else
                return null;
            }



        #endregion





        //SOIL HELPER METHODS



        #region Layer Helper Functions


        public int num_layers  //one based.
        { get { return layers.Count; } }



        public double DepthTotal
            {
            get
                {
                double depth = 0.0;
                foreach (Layer lyr in layers)
                    {
                    depth = depth + lyr.dlayer;
                    }
                return depth;
                }
            }


        public double DepthDownToBottomOfLayer(int LayerNumber)
            {
            double depth = 0.0;
            foreach (Layer lyr in TopToX(LayerNumber))
                {
                depth = depth + lyr.dlayer;
                }
            return depth;

            }



        /// <summary>
        /// Find the layer number of the specified depth.
        /// </summary>
        /// <param name="Depth"></param>
        /// <returns>1 based Layer Number</returns>
        public int FindLayerNo(double Depth)
            {
            // Find the soil layer in which the indicated depth is located
            // If the depth is not reached, the last element is used

            layers.Sort();

            double depth_cum = 0.0;
            foreach (Layer lyr in layers)
                {
                depth_cum = depth_cum + lyr.dlayer;
                if (depth_cum >= Depth)
                    return lyr.number;
                }
            return num_layers;
            }


        #endregion



        #region Solute Helper Functions



        public List<string> GetMobileSoluteNames()
            {
            Layer top = GetTopLayer();
            List<string> mobileNames = top.GetMobileSoluteNames();
            return mobileNames;
            }



        public List<SoluteInLayer> GetAllSolutesInALayer()
            {
            Layer top = GetTopLayer();
            List<SoluteInLayer> all = top.GetAllSolutes();
            return all;
            }



        public void UpdateSoluteAmounts(string SoluteName, double[] Amounts)
            {
            SoluteInLayer sol;
            foreach (Layer lyr in layers)
                {
                if (lyr.number <= Amounts.Length)
                    {
                    sol = lyr.GetASolute(SoluteName);
                    sol.amount = Amounts[lyr.number - 1];
                    }
                }
            }



        public double[] GetASolutesDeltas(string SoluteName)
            {
            double[] result = new double[num_layers];
            SoluteInLayer solute;

            foreach (Layer lyr in layers)
                {
                solute = lyr.GetASolute(SoluteName);
                result[lyr.number - 1] = solute.delta;
                }

            return result;
            }



        #endregion



        #region Add/Remove Water



        public void AddSubSurfaceIrrig(IrrigData Irrig)
            {

            Layer lyr = GetLayer(Irrig.irrigation_layer);
            lyr.sw_dep = lyr.sw_dep + Irrig.irrigation;

            //what about solutes?

            }


        public void SetWater_mm(double[] New_SW_dep)
            {
            foreach (Layer lyr in layers)
                {
                if (New_SW_dep.Length >= lyr.number)
                    lyr.sw_dep = New_SW_dep[lyr.number - 1];
                }
            }


        public void SetWater_frac(double[] New_SW)
            {
            foreach (Layer lyr in layers)
                {
                if (New_SW.Length >= lyr.number)
                    lyr.sw_dep = New_SW[lyr.number - 1] * lyr.dlayer;
                }
            }


        public void DeltaWater_mm(double[] Delta_mm)
            {
            foreach (Layer lyr in layers)
                {
                if (Delta_mm.Length >= lyr.number)
                    lyr.sw_dep = lyr.sw_dep + Delta_mm[lyr.number - 1];
                }
            }


        public void DeltaWater_frac(double[] Delta_frac)
            {
            foreach (Layer lyr in layers)
                {
                if (Delta_frac.Length >= lyr.number)
                    lyr.sw_dep = lyr.sw_dep + (Delta_frac[lyr.number - 1] * lyr.dlayer);
                }
            }


        #endregion



        #region Check for Errors in the Soil



        public void CheckSoilForErrors()
            {
            ////can't use an iterator because throwing an apsim warning inside the iterator causes an iterator error.
            //foreach (Layer lyr in layers)
            //    {
            //    CheckLayerForErrors(lyr.number);
            //    }
            for (int i = 1; i <= num_layers; i++)
                {
                CheckLayerForErrors(i);
                }

            }



        /// <summary>
        /// Checks the specified layer for any Errors
        /// </summary>
        /// <param name="LayerNumber">LayerNumber is One Based</param>
        public void CheckLayerForErrors(int LayerNumber)
            {

            //private void soilwat2_check_profile(int layer)
            //    {
            //*+  Purpose
            //*       checks validity of soil water parameters for a soil profile layer

            //*+  Notes
            //*           reports an error if
            //*           - g%ll15_dep, _dul_dep, and _sat_dep are not in ascending order
            //*           - ll15 is below min_sw
            //*           - sat is above max_sw
            //*           - sw > sat or sw < min_sw      


            //Constant Values

            string err_messg;           //! error message

            double dul_errmargin;       //! rounding error margin for dulc
            double ll15_errmargin;      //! rounding error margin for ll15c
            double air_dry_errmargin;   //! rounding error margin for air_dryc
            double sat_errmargin;       //! rounding error margin for satc
            double sw_errmargin;        //! rounding error margin for swc


            double min_sw = 0.0;
            double max_sw;       //! largest acceptable value for sat (mm water/mm soil) //sv-Largest possible amount of sw the soil can hold.
            double max_sw_errmargin = 0.01;


            Layer lyr = GetLayer(LayerNumber);


            max_sw = 1.0 - Utility.Math.Divide(lyr.bd, Constants.specific_bd, 0.0);  //ie. Total Porosity


            sw_errmargin = Constants.error_margin;
            sat_errmargin = Constants.error_margin;
            dul_errmargin = Constants.error_margin;
            ll15_errmargin = Constants.error_margin;
            air_dry_errmargin = Constants.error_margin;



            if ((lyr.air_dry + air_dry_errmargin) < min_sw)
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " Air dry lower limit of ",
                                           lyr.air_dry,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is below acceptable value of ",
                                           min_sw);
                Constants.IssueWarning(err_messg);
                }


            if ((lyr.ll15 + ll15_errmargin) < (lyr.air_dry - air_dry_errmargin))
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " 15 bar lower limit of ",
                                           lyr.ll15,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is below air dry value of ",
                                           lyr.air_dry);
                Constants.IssueWarning(err_messg);
                }



            if ((lyr.dul + dul_errmargin) <= (lyr.ll15 - ll15_errmargin))
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " drained upper limit of ",
                                           lyr.dul,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is at or below lower limit of ",
                                           lyr.ll15);
                Constants.IssueWarning(err_messg);
                }

            if ((lyr.sat + sat_errmargin) <= (lyr.dul - dul_errmargin))
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " saturation of ",
                                           lyr.sat,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is at or below drained upper limit of ",
                                           lyr.dul);
                Constants.IssueWarning(err_messg);
                }

            if ((lyr.sat - sat_errmargin) > (max_sw + max_sw_errmargin))
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G} {7} {8} {9:G} {10} {11} {12:G})",
                                           " saturation of ",
                                           lyr.sat,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is above acceptable value of ",
                                           max_sw,
                                           "\n",
                                           "You must adjust bulk density (bd) to below ",
                                           (1.0 - lyr.sat) * Constants.specific_bd,
                                           "\n",
                                           "OR saturation (sat) to below ",
                                           max_sw);
                Constants.IssueWarning(err_messg);
                }


            if (lyr.sw - sw_errmargin > lyr.sat + sat_errmargin)
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G}",
                                           " soil water of ",
                                           lyr.sw,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is above saturation of ",
                                           lyr.sat);
                Constants.IssueWarning(err_messg);
                }

            if (lyr.sw + sw_errmargin < lyr.air_dry - air_dry_errmargin)
                {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G}",
                                           " soil water of ",
                                           lyr.sw,
                                           " in layer ",
                                           LayerNumber,
                                           "\n",
                                           "         is below air-dry value of ",
                                           lyr.air_dry);
                Constants.IssueWarning(err_messg);
                }

            }


        #endregion





        //CONSTRUCTOR / RESET THE SOIL



        #region Constructor/Reset the Soil



        //Constructor
        public SoilWaterSoil(Constants Consts, Soil Soil)
            {

            //add all the layers
            if (Soil.Thickness != null)
                {
                layers = new List<Layer>();
                for (int i = 0; i < Soil.Thickness.Count(); i++)
                    {
                    Layer lyr = new Layer();
                    layers.Add(lyr);
                    }
                }
            else
                {
                throw new Exception("Constructor for SoilWaterSoil failed because there are no layers");
                }

            Constants = Consts;
            UseStartingValuesToInitialise(Soil);
            }


        //Reset
        public void ResetSoil(Constants Consts, Soil Soil)
            {
            Constants = Consts;
            UseStartingValuesToInitialise(Soil);
            }


        private void UseStartingValuesToInitialise(Soil Soil)
            {


            //Soil Properties (NOT LAYERED)
            //*****************************


            //! flag to determine if Ks has been chosen for use. 
            if (Soil.KS == null)
                using_ks = false;
            else
                using_ks = true;



            //From "SoilWater" node in the GUI 
            //-------------------------------

            //summer

            SummerCona = Soil.SoilWater.SummerCona;
            SummerU = Soil.SoilWater.SummerU;
            SummerDate = Soil.SoilWater.SummerDate;

            //winter
            WinterCona = Soil.SoilWater.WinterCona;
            WinterU = Soil.SoilWater.WinterU;
            WinterDate = Soil.SoilWater.WinterDate;


            DiffusConst = Soil.SoilWater.DiffusConst;
            DiffusSlope = Soil.SoilWater.DiffusSlope;


            Salb = Soil.SoilWater.Salb;


            cn2_bare = Soil.SoilWater.CN2Bare;
            cn_red = Soil.SoilWater.CNRed;
            cn_cov = Soil.SoilWater.CNCov;


            slope = Soil.SoilWater.slope;
            discharge_width = Soil.SoilWater.discharge_width;
            catchment_area = Soil.SoilWater.catchment_area;

            if (Double.IsNaN(slope))
                slope = 0.0;

            if (Double.IsNaN(discharge_width))
                discharge_width = 0.0;

            if (Double.IsNaN(catchment_area))
                catchment_area = 0.0;



            max_pond = Soil.SoilWater.max_pond;




            //Soil "Profile" (LAYERED) 
            //************************

            int i = 0;
            foreach (Layer lyr in layers)
                {

                lyr.number = i + 1;


                //From "Water" node in GUI
                //-----------------------

                lyr.dlayer = Soil.Thickness[i]; //need to set dlayer first so the fractions below can be converted to their mm _dep versions
                lyr.bd = Soil.BD[i];
                lyr.sat = Soil.SAT[i];
                lyr.dul = Soil.DUL[i];
                lyr.ll15 = Soil.LL15[i];
                lyr.air_dry = Soil.AirDry[i];
                if (Soil.KS == null)      //can be blank in the node
                    lyr.ks = 0.0;               //turn off but using_ks flag stops this from mattering
                else
                    lyr.ks = Soil.KS[i];



                //From "SoilWater" node in the GUI 
                //-------------------------------

                //(Use top level Soil instead of Soil.SoilWater so they are in standard layer thicknesses)

                if (Soil.SWCON == null)  //can be blank in the node
                    lyr.SWCON = 0.3;
                else
                    lyr.SWCON = Soil.SWCON[i];  //in standard thickness


                if (Soil.KLAT == null)   //can be blank in the node
                    lyr.KLAT = 0.0;             //turn off the lateral flow
                else
                    lyr.KLAT = Soil.KLAT[i];    //in standard thickness




                //From "InitWater" node or "SoilSample" node
                //------------------------------------------
                    
                lyr.sw = Soil.SW[i];  //Soil.SW returns the initial water not the current water.


                i++;
                }

 



            }





        #endregion



        #region Zeroing Functions



        public void ZeroOutputs()
            {
            ZeroSoilOutputs();
            ZeroLayerDeltas();
            }


        private void ZeroSoilOutputs()
            {
            //Soil Properties

            Drainage = 0.0;
            DepthToWaterTable = 0.0;
            }


        private void ZeroLayerDeltas()
            {

            //Soil Layers

            //deltas
            foreach (Layer lyr in layers)
                {

                lyr.flow = 0.0;
                lyr.flux = 0.0;
                //lyr.flow_water = 0.0; //its a property so don't need to zero it.
                lyr.flux2 = 0.0;
                lyr.flow2 = 0.0;

                lyr.outflow_lat = 0.0;

                ZeroLayerTemporaryVars();

                lyr.ZeroMobileSolutesDeltas();

                }
            }



        private void ZeroLayerTemporaryVars()
            {
            foreach (Layer lyr in layers)
                {
                lyr.new_sw_dep = 0.0;
                lyr.solute_up = 0.0;
                lyr.remain = 0.0;
                lyr.solute_down = 0.0;
                }

            }



        #endregion






        //SOIL PROCESSES




        #region Saturated Flow (Flux is both (dul to sat) flow and (above sat) flow combined)

        //TODO: Replace w_in, w_out with Layer above.flux and Layer below.flux variables.


        public double Calc_Saturated_Flow()
            {
            //private void soilwat2_drainage(ref double ExtraRunoff)
            //    {

            //*     ===========================================================
            //subroutine soilwat2_drainage (flux,extra_runoff)
            //*     ===========================================================


            //*+  Function Arguments
            //flux              //! (output) water moving out of
            //extra_runoff      //! (output) water to add to runoff layer (mm)

            //*+  Purpose       
            //calculate flux - drainage from each layer. 
            //sv- above dul flow. Both (dul to sat) -> drain flow AND (above sat) -> excess flow.
            //sv- it just calculates. It does not change anything.


            //*+  Local Variables

            double add;                   //! water to add to layer
            double backup;                //! water to backup
            double w_excess;               //! amount above saturation(overflow)(mm)
            double excess_down;           //! amount above saturation(overflow) that moves on down (mm)

            double w_drain;               //! water draining by gravity (mm)
            double w_in;                  //! water coming into layer (mm)
            double w_out;                 //! water going out of layer (mm)
            double w_tot;                 //! total water in layer at start (mm)

            //*- Implementation Section ----------------------------------

            //! flux into layer 1 = infiltration (mm).


            double backedUpSurface = 0.0;


            w_in = 0.0;
            foreach (Layer lyr in layers)
                {

                w_tot = lyr.sw_dep + w_in;


                //Calculate EXCESS Amount (above SAT)

                //! get excess water above saturation & then water left
                //! to drain between sat and dul.  Only this water is
                //! subject to swcon. The excess is not - treated as a
                //! bucket model. (mm)

                if (w_tot > lyr.sat_dep)
                    {
                    w_excess = w_tot - lyr.sat_dep;
                    w_tot = lyr.sat_dep;
                    }
                else
                    {
                    w_excess = 0.0;
                    }


                //Calculate DRAIN Amount (between SAT and DUL)

                if (w_tot > lyr.dul_dep)
                    w_drain = (w_tot - lyr.dul_dep) * lyr.SWCON;
                else
                    w_drain = 0.0;




                //Caclculate EXCESS Flow and DRAIN Flow (combined into Flux)

                //if there is EXCESS Amount, 
                if (w_excess > 0.0)
                    {
                    if (!using_ks)
                        {
                        //! all this excess goes on down 
                        w_out = w_excess + w_drain;
                        lyr.new_sw_dep = lyr.sw_dep + w_in - w_out;
                        lyr.flux = w_out;

                        }
                    else
                        {
                        //! Calculate amount of water to backup and push down
                        //! Firstly top up this layer (to saturation)
                        add = Math.Min(w_excess, w_drain);
                        w_excess = w_excess - add;
                        lyr.new_sw_dep = lyr.sat_dep - w_drain + add;

                        //! partition between flow back up and flow down
                        excess_down = Math.Min(lyr.ks - w_drain, w_excess);
                        backup = w_excess - excess_down;

                        w_out = excess_down + w_drain;
                        lyr.flux = w_out;


                        //Starting from the layer above the current layer,
                        //Move up to the surface, layer by layer and use the
                        //backup to fill the space still remaining between
                        //the new sw_dep (that you calculated on the way down) 
                        //and sat for that layer. Once the backup runs out
                        //it will keep going but you will be adding 0.

                        if (lyr.number >= 2)
                            {
                            foreach (Layer lyrUp in XToTop(lyr.number - 1))
                                {
                                lyrUp.flux = lyrUp.flux - backup;
                                add = Math.Min(lyrUp.sat_dep - lyrUp.new_sw_dep, backup);
                                lyrUp.new_sw_dep = lyrUp.new_sw_dep + add;
                                backup = backup - add;
                                }
                            }

                        backedUpSurface = backedUpSurface + backup;
                            
                        }
                    }
                else
                    {
                    //there is no EXCESS Amount so only do DRAIN Flow   
                    w_out = w_drain;
                    lyr.flux = w_drain;
                    lyr.new_sw_dep = lyr.sw_dep + w_in - w_out;
                    }



                //! drainage out of this layer goes into next layer down
                w_in = w_out;
                }

            return backedUpSurface;

            }



        public void Do_Saturated_Flow()
            {

            //private void MoveDownReal(double[] DownAmount, ref double[] A)
            //     {

            double win;    //! amount moving from layer above to current layer
            double wout;   //! amount moving from current layer to the one below

            //!- Implementation Section ----------------------------------

            win = 0.0;

            foreach (Layer lyr in layers)
                {
                wout = lyr.flux;
                lyr.sw_dep = lyr.sw_dep + win - wout;
                win = wout;
                }


            //! drainage out of bottom layer
            Layer bottom = GetBottomLayer();
            this.Drainage = bottom.flux;

            }



        #endregion



        #region Unsaturated Flow (flow is (below dul) flow)

        //TODO: Replace w_in, w_out with Layer above.flow and Layer below.flow variables.


        public void Calc_Unsaturated_Flow()
            {
            //private void soilwat2_unsat_flow()
            //    {

            //*+  Purpose
            //*       calculate unsaturated flow below drained upper limit

            //*+  Mission Statement
            //*     Calculate Unsaturated Solute and Water Flow


            double esw_dep1;            //! extractable soil water in current layer (mm)
            double esw_dep2;            //! extractable soil water in next layer below (mm)
            double dbar;                //! average diffusivity used to calc unsaturated flow between layers

            int second_last_layer;   //! last layer for flow

            double flow_max;            //! maximum flow to make gradient between layers equal zero
            double theta1;              //! sw content above ll15 for current layer (cm/cm)
            double theta2;              //! sw content above ll15 for next lower layer (cm/cm)
            double w_out;               //! water moving up out of this layer (mm)
            //! +ve = up to next layer
            //! -ve = down into this layer
            double this_layer_cap;      //! capacity of this layer to accept water from layer below (mm)
            double next_layer_cap;      //! capacity of nxt layer to accept water from layer above (mm)
            double sw1;                 //! sw for current layer (mm/mm)
            double sw2;                 //! sw for next lower layer (mm/mm)
            double gradient;            //! driving force for flow
            double sum_inverse_dlayer;

            double ave_dlayer;          //! average depth of current and next layers (mm)

            double swg;                 //! sw differential due to gravitational pressure head (mm)


            //! *** calculate unsaturated flow below drained upper limit (flow)***   


            //! second_last_layer is bottom layer but 1.
            second_last_layer = num_layers - 1;

            Layer below;

            w_out = 0.0;
            foreach (Layer lyr in TopToX(second_last_layer))
                {
                //for (layer = 0; layer < second_last_layer; layer++)
                //    {

                below = GetLayer(lyr.number + 1);

                ave_dlayer = (lyr.dlayer + below.dlayer) * 0.5;

                esw_dep1 = Math.Max((lyr.sw_dep - w_out) - lyr.ll15_dep, 0.0);
                esw_dep2 = Math.Max(below.sw_dep - below.ll15_dep, 0.0);

                //! theta1 is excess of water content above lower limit,
                //! theta2 is the same but for next layer down.
                theta1 = Utility.Math.Divide(esw_dep1, lyr.dlayer, 0.0);
                theta2 = Utility.Math.Divide(esw_dep2, below.dlayer, 0.0);

                //! find diffusivity, a function of mean thet.
                dbar = DiffusConst * Math.Exp(DiffusSlope * (theta1 + theta2) * 0.5);

                //! testing found that a limit of 10000 (as used in ceres-maize)
                //! for dbar limits instability for flow direction for consecutive
                //! days in some situations.

                dbar = Constants.bound(dbar, 0.0, 10000.0);

                sw1 = Utility.Math.Divide((lyr.sw_dep - w_out), lyr.dlayer, 0.0);
                sw1 = Math.Max(sw1, 0.0);

                sw2 = Utility.Math.Divide(below.sw_dep, below.dlayer, 0.0);
                sw2 = Math.Max(sw2, 0.0);

                //    ! gradient is defined in terms of absolute sw content
                //cjh          subtract gravity gradient to prevent gradient being +ve when flow_max is -ve, resulting in sw > sat.
                gradient = Utility.Math.Divide((sw2 - sw1), ave_dlayer, 0.0) - Constants.gravity_gradient;


                //!  flow (positive up) = diffusivity * gradient in water content
                lyr.flow = dbar * gradient;

                //! flow will cease when the gradient, adjusted for gravitational
                //! effect, becomes zero.
                swg = Constants.gravity_gradient * ave_dlayer;

                //! calculate maximum flow
                sum_inverse_dlayer = Utility.Math.Divide(1.0, lyr.dlayer, 0.0) + Utility.Math.Divide(1.0, below.dlayer, 0.0);
                flow_max = Utility.Math.Divide((sw2 - sw1 - swg), sum_inverse_dlayer, 0.0);


                //c dsg 260202
                //c dsg    this code will stop a saturated layer difusing water into a partially saturated
                //c        layer above for Water_table height calculations
                if ((lyr.sw_dep >= lyr.dul_dep) && (below.sw_dep >= below.dul_dep))
                    {
                    lyr.flow = 0.0;
                    }

                if (lyr.flow < 0.0)
                    {
                    //! flow is down to layer below
                    //! check capacity of layer below for holding water from this layer
                    //! and the ability of this layer to supply the water

                    //!    next_layer_cap = l_bound (sat_dep2 - sw_dep2, 0.0)
                    //!    dsg 150302   limit unsaturated downflow to a max of dul in next layer

                    next_layer_cap = Math.Max(below.dul_dep - below.sw_dep, 0.0);
                    flow_max = Math.Max(flow_max, -1 * next_layer_cap);
                    flow_max = Math.Max(flow_max, -1 * esw_dep1);
                    lyr.flow = Math.Max(lyr.flow, flow_max);
                    }
                else
                    {
                    if (lyr.flow > 0.0)
                        {
                        //! flow is up from layer below
                        //! check capacity of this layer for holding water from layer below
                        //! and the ability of the layer below to supply the water

                        //!            this_layer_cap = l_bound (sat_dep1 - (sw_dep1 - w_out), 0.0)
                        //!    dsg 150302   limit unsaturated upflow to a max of dul in this layer
                        this_layer_cap = Math.Max(lyr.dul_dep - (lyr.sw_dep - w_out), 0.0);
                        flow_max = Math.Min(flow_max, this_layer_cap);
                        flow_max = Math.Min(flow_max, esw_dep2);
                        lyr.flow = Math.Min(lyr.flow, flow_max);
                        }
                    else
                        {
                        // no flow
                        }
                    }


                //! For conservation of water, store amount of water moving
                //! between adjacent layers to use for next pair of layers in profile
                //! when calculating theta1 and sw1.
                w_out = lyr.flow;
                }

            }



        public void Do_Unsaturated_Flow()
            {

            //private void MoveUpReal(double[] UpAmount, ref double[] A)
            //    {
            //move_up_real(leach, temp_solute, num_layers);

            //!+ Local Variables

            double win;                   //! amount moving from layer below to current layer
            double wout;                  //! amount moving from current layer to the one above

            //!- Implementation Section ----------------------------------

            wout = 0.0;
            foreach (Layer lyr in layers)
                {
                win = lyr.flow;
                lyr.sw_dep = lyr.sw_dep + win - wout;
                wout = win;
                }
            }




        #endregion




        #region Solute



        public void AddSolutesDueToIrrigation(IrrigData Irrig)
            {

            //private void soilwat2_irrig_solute()
            //    {

            //*+  Mission Statement
            //*      Add solutes with irrigation


            //sv- 11 Dec 2012. 
            //Since I have allowed irrigations to runoff just like rain (using argument "will_runoff = 1" in apply command)
            //I should really remove a proportion of the solutes that are lost due to some of the irrigation running off.
            //Perhaps something like (irrigation / (rain + irrigation)) * runoff 
            //to work out how much of the runoff is caused by irrigation and remove this proportion of solutes from the surface layer.
            //HOWEVER, when rain causes runoff we don't remove solutes from the surface layer of the soil. 
            //So why when irrigation causes runoff should we remove solutes.  

            Layer lyr = GetLayer(Irrig.irrigation_layer);


            SoluteInLayer no3 = lyr.GetASolute("no3");
            SoluteInLayer nh4 = lyr.GetASolute("nh4");
            //SoluteInLayer cl = lyr.GetASolute("cl");


            no3.amount += Irrig.NO3;
            no3.delta += Irrig.NO3;

            nh4.amount += Irrig.NH4;
            nh4.delta += Irrig.NH4;

            //cl.amount += Irrig.CL;
            //cl.delta += Irrig.CL;

            }

        /*

           private void soilwat2_rainfall_solute()
              {
              //*+  Mission Statement
              //*      Add solutes from rainfall

              int      solnum;        //! solute number counter variable
              double   mass_rain;     //! mass of rainfall on this day (kg/ha)
              double   mass_solute;   //! mass of solute in this rainfall (kg/ha)

              //! 1mm of rain = 10000 kg/ha, therefore total mass of rainfall = g%rain * 10000 kg/ha
              mass_rain = rain * 10000.0;

              for(solnum=0; solnum<num_solutes; solnum++)
                 {
                 //!assume all rainfall goes into layer 1
                 //! therefore mass_solute = mass_rain * g%solute_conc_rain (in ppm) / 10^6
                 mass_solute = Utility.Math.Divide(mass_rain * solute_conc_rain[solnum], 1000000.0, 0.0);
                 solute[solnum,0]   = solute[solnum,0] + mass_solute;
                 dlt_solute[solnum,0] = dlt_solute[solnum,0] + mass_solute;
                 }

              }

        */







        //sv- solute movement during Drainage (Saturated Flow)


        public void Do_Solute_Leach_SatFlow(string SoluteName)
            {
            //private void soilwat2_solute_flux(ref double[] solute_out, double[] solute_kg)
            //    {

            //solute_out   ->   ! (output) solute leaching out of each layer (kg/ha) 
            //solute_kg    ->   ! (input) solute in each layer (kg/ha)

            //*+  Purpose
            //*         calculate the downward movement of solute with percolating water

            //*+  Mission Statement
            //*     Calculate the Solute Movement with Saturated Water Flux

            double in_solute;        //! solute leaching into layer from above (kg/ha)
            double out_max;          //! max. solute allowed to leach out of layer (kg/ha)
            double out_solute;       //! solute leaching out of layer (kg/ha)
            double out_w;            //! water draining out of layer (mm)
            double solute_kg_layer;  //! quantity of solute in layer (kg/ha)
            double water;            //! quantity of water in layer (mm)
            double solute_flux_eff_local;


            in_solute = 0.0;

            SoluteInLayer solute;

            foreach (Layer lyr in layers)
                {
                solute = lyr.GetASolute(SoluteName);
                solute.leach = 0.0;

                //! get water draining out of layer and n content of layer includes that leaching down         
                out_w = lyr.flux;
                lyr.flux2 = lyr.flux; //Fixme.  HEB This is a nasty cludge to get APSIMX reporting Flux without needing to do major refactoring
                solute_kg_layer = solute.amount + in_solute;

                //! n leaching out of layer is proportional to the water draining out.
                if (Constants.solute_flow_eff.Length == 1)
                    {
                    //single value was specified in ini file (still gets put in an array with just one element)
                    solute_flux_eff_local = Constants.solute_flux_eff[0];
                    }
                else
                    {
                    //array was specified in ini file
                    solute_flux_eff_local = Constants.solute_flux_eff[lyr.number - 1];
                    }
                water = lyr.sw_dep + out_w;
                out_solute = solute_kg_layer * Utility.Math.Divide(out_w, water, 0.0) * solute_flux_eff_local;

                //! don't allow the n to be reduced below a minimum level
                out_max = Math.Max(solute_kg_layer, 0.0);
                out_solute = Constants.bound(out_solute, 0.0, out_max);

                //save the solute leach, amount and delta 
                solute.leach = out_solute;
                solute.amount = solute.amount + in_solute - out_solute;
                solute.delta = solute.delta + in_solute - out_solute;   //add (in_solute - out_solute) to existing delta incase another form of solute movement eg.flow has also delta'ed the solute.

                //set the input for the next layer
                in_solute = out_solute;
                }


            }

        //sv- solute movement during Unsaturated Flow

        public void Do_Solute_Up_UnsatFlow(string SoluteName)
            {

            //private void soilwat2_solute_flow(ref double[] solute_up, double[] solute_kg)
            //    {

            //solute_up -> ! (output) solute moving upwards into each layer (kg/ha)
            //solute_kg -> ! (input/output) solute in each layer (kg/ha)

            //*+  Purpose
            //*       movement of solute in response to differences in
            //*       water content of adjacent soil layers when the soil water
            //*       content is < the drained upper limit (unsaturated flow)

            //*+  Notes
            //*       170895 nih The variable names and comments need to be cleaned
            //*                  up.  When this is done some references to no3 or
            //*                  nitrogen need to be changed to 'solute'

            //*+  Mission Statement
            //*     Calculate the Solute Movement with Unsaturated Water Flow

            double bottomw;             //! water movement to/from next layer (kg/ha)
            double in_solute;           //! solute moving into layer from above (kg/ha)
            double[] solute_down;         //! solute moving downwards out of each layer (kg/ha)
            int num_layers;          //! number of layers
            double out_solute;          //! solute moving out of layer (kg/ha)
            double out_w;               //! water draining out of layer (mm)
            double[] remain;              //! n remaining in each layer between movement up (kg/ha)
            double solute_kg_layer;     //! quantity of solute in layer (kg/ha)
            double top_w;               //! water movement to/from above layer (kg/ha)
            double water;               //! quantity of water in layer (mm)
            double solute_flow_eff_local;

            //sv- initialise the local arrays declared above.

            //! flow  up from lower layer:  + up, - down
            //******************************************
            //******************************************


            //! + ve flow : upward movement. go from bottom to top layer   
            //**********************************************************


            //solute_up = new double[num_layers];
            //solute_down = new double[num_layers];
            //remain = new double[num_layers];

            in_solute = 0.0;
            SoluteInLayer solute;
            Layer above;
            Layer top;

            foreach (Layer lyr in BottomToX(2))
                {
                above = GetLayer(lyr.number - 1);
                solute = lyr.GetASolute(SoluteName);

                //! keep the nflow upwards
                lyr.solute_up = in_solute;

                //! get water moving up and out of layer to the one above
                out_w = above.flow;
                lyr.flow2 = lyr.flow; //Fixme.  HEB This is a nasty cludge to get APSIMX reporting Flux without needing to do major refactoring
                if (out_w <= 0.0)
                    {
                    out_solute = 0.0;
                    }
                else
                    {
                    //! get water movement between this and next layer
                    bottomw = lyr.flow;

                    //! get n content of layer includes that moving from other layer
                    solute_kg_layer = solute.amount + in_solute;
                    water = lyr.sw_dep + out_w - bottomw;

                    //! n moving out of layer is proportional to the water moving out.
                    if (Constants.solute_flow_eff.Length == 1)
                        {
                        solute_flow_eff_local = Constants.solute_flow_eff[0];
                        }
                    else
                        {
                        solute_flow_eff_local = Constants.solute_flow_eff[lyr.number - 1];
                        }
                    out_solute = solute_kg_layer * Utility.Math.Divide(out_w, water, 0.0) * solute_flow_eff_local;

                    //! don't allow the n to be reduced below a minimum level
                    out_solute = Constants.bound(out_solute, 0.0, solute_kg_layer);
                    }
                //! set the input for the next layer
                in_solute = out_solute;
                }

            top = GetTopLayer();
            top.solute_up = in_solute;
            //! now get n remaining in each layer between movements
            //! this is needed to adjust the n in each layer before calculating
            //! downwards movement.  i think we shouldn't do this within a time
            //! step. i.e. there should be no movement within a time step. jngh
            top.remain = top.solute_up;
            foreach (Layer lyr in XToBottom(2))
                {
                above = GetLayer(lyr.number - 1);
                lyr.remain = lyr.solute_up - above.solute_up;
                }




            //! -ve flow - downward movement
            //******************************

            in_solute = 0.0;
            top_w = 0.0;

            foreach (Layer lyr in layers)
                {
                solute = lyr.GetASolute(SoluteName);

                //! get water moving out of layer
                out_w = -1 * lyr.flow;
                if (out_w <= 0.0)
                    {
                    out_solute = 0.0;
                    }
                else
                    {
                    //! get n content of layer includes that moving from other layer
                    solute_kg_layer = solute.amount + in_solute + lyr.remain;
                    water = lyr.sw_dep + out_w - top_w;

                    //! n moving out of layer is proportional to the water moving out.
                    if (Constants.solute_flow_eff.Length == 1)
                        {
                        solute_flow_eff_local = Constants.solute_flow_eff[0];
                        }
                    else
                        {
                        solute_flow_eff_local = Constants.solute_flow_eff[lyr.number - 1];
                        }

                    out_solute = solute_kg_layer * Utility.Math.Divide(out_w, water, 0.0) * solute_flow_eff_local;

                    //! don't allow the n to be reduced below a minimum level
                    out_solute = Utility.Math.RoundToZero(out_solute);
                    out_solute = Constants.bound(out_solute, 0.0, solute_kg_layer);
                    }
                lyr.solute_down = out_solute;
                in_solute = out_solute;
                top_w = out_w;
                }

            foreach (Layer lyr in layers)
                {
                lyr.solute_up = lyr.solute_up - lyr.solute_down;

                //save the solute up, amount and delta; 
                solute = lyr.GetASolute(SoluteName);
                solute.up = lyr.solute_up;
                solute.amount = solute.amount + solute.up;
                solute.delta = solute.delta + solute.up;   //add solute.up to existing delta incase another form of solute movement eg.flux has also delta'ed the solute.
                }

            }



        public void Do_Solutes_SatFlow()
            {
            //private void soilwat2_move_solute_down()
            //    {

            //*+  Mission Statement
            //*      Calculate downward movement of solutes


            List<string> soluteNames = GetMobileSoluteNames();

            foreach (string solName in soluteNames)
                {
                Do_Solute_Leach_SatFlow(solName);
                }

            }



        public void Do_Solutes_UnsatFlow()
            {
            //private void soilwat2_move_solute_up()
            //    {

            //*+  Mission Statement
            //*      Calculate upward movement of solutes

            List<string> soluteNames = GetMobileSoluteNames();

            foreach (string solName in soluteNames)
                {
                Do_Solute_Up_UnsatFlow(solName);
                }

            }


        #endregion




        #region Water Table


        public void Calc_DepthToWaterTable()
            {
            //private double soilwat_water_table()
            //    {
            //*+  Purpose
            //*     Calculate the water table
            // water table is just the depth (in mm) below the ground surface of the first layer which is above saturation.

            int sat_layer;
            double margin;      //! dsg 110302  allowable looseness in definition of sat
            //double saturated_fraction;
            //double saturated_fraction_above;
            //double drainable;
            //double drainable_capacity;
            double bottom_depth;
            double saturated;
            //bool layer_is_fully_saturated;
            //bool layer_is_saturated;
            //bool layer_above_is_saturated;


            ////sv- C# has a problem with these values being initialised inside of the final else clause of an if statement. You have to give them a default value.
            sat_layer = -1;
            //saturated_fraction_above = 0.0;
            //layer_is_saturated = false;

            Layer lyrSat;
            Layer lyrAboveSat;

            //Find Saturated Layer
            foreach (Layer lyr in layers)
                {
                margin = Constants.error_margin;

                //Find the first layer that is above saturation or really close to it. 
                //nb. sat_layer is a layer number not an index. Therefore it starts at 1 and not zero. 
                //So we need to convert it to a layer number from an index. "layer" variable is really an index not a layer number.
                if (lyr.amnt_to_sat <= margin)
                    {
                    sat_layer = lyr.number;
                    break;
                    }
                else
                    {
                    sat_layer = 0;   //if there is no saturated layer set it to 0
                    }
                }

            //no saturated layer means no water table
            if (sat_layer == 0)
                {
                //set the depth of watertable to the total depth of the soil profile
                DepthToWaterTable = DepthTotal;
                return;
                }


            lyrSat = GetLayer(sat_layer);


            //Do the calculation of the water_table 
     
            //if saturated layer is fully saturated and the layer above is saturated but not fully saturated
            //(if the layer above was fully saturatured then it would be the saturated layer)
            if ((lyrSat.layer_is_fully_saturated) && (lyrSat.number > 1))
                {
                lyrAboveSat = GetLayer(lyrSat.number - 1);

                if (lyrAboveSat.layer_is_saturated)
                    {
                    //layer above is over dul
                    bottom_depth = DepthDownToBottomOfLayer(lyrAboveSat.number);
                    saturated = lyrAboveSat.saturated_fraction * lyrAboveSat.dlayer;
                    DepthToWaterTable = (bottom_depth - saturated);
                    return;
                    }
                }

            //TODO: I think this maybe wrong, saturated_fration is the fraction of drainable and drainable_capacity
            //      which means that when you multiply it by dlayer you only get the depth of water going from dul to sw_dep.
            //      I think it should be multiplying sw x dlayer or just using sw_dep. This depth is only subtracted from
            //      bottom depth if sw_dep is above dul_dep however because otherwise it is not part of the watertable it
            //      is just water in the layer above the water table.

            bottom_depth = DepthDownToBottomOfLayer(sat_layer);
            saturated = lyrSat.saturated_fraction * lyrSat.dlayer;
            DepthToWaterTable = (bottom_depth - saturated);




            ////If you found a saturated layer in the profile, 
            //if (sat_layer > 0)
            //    {
            //    //! saturated fraction of saturated layer
            //    //calculate the saturation_fraction of current layer incase, there is no layer above
            //    //or incase mwcon was set to impermeable and sw was above dul (so there are layers above but no saturated layers, 
            //    //the impermeable layer is just above dul which is the watertable) 
            //    drainable = lyrSat.sw_dep - lyrSat.dul_dep;
            //    drainable_capacity = lyrSat.sat_dep - lyrSat.dul_dep;
            //    saturated_fraction = lyrSat.saturated_fraction;

            //    //if it is not the top layer that is saturated (ie. there is a layer above the saturated layer)
            //    //Then check to see if the layer above it is above DUL (not SAT), 
            //    //and if so, calculate the fraction so we can add this as extra millimeters to the water_table.
            //    if (sat_layer > 1)
            //        {
            //        //! saturated fraction of layer above saturated layer
            //        drainable = lyrAboveSat.sw_dep - lyrAboveSat.dul_dep;
            //        drainable_capacity = lyrAboveSat.sat_dep - lyrAboveSat.dul_dep;
            //        saturated_fraction_above = Utility.Math.Divide(drainable, drainable_capacity, 0.0);
            //        }
            //    else
            //        {
            //        //! top layer fully saturated - no layer above it
            //        saturated_fraction_above = 0.0;
            //        }
            //    }
            //else
            //    {
            //    //! profile not saturated
            //    saturated_fraction = 0.0;
            //    }



            ////set some boolean flags based on the saturated fraction calculated above.
            //if (saturated_fraction >= 0.999999)
            //    {
            //    layer_is_fully_saturated = true;
            //    //layer_above_is_saturated = true;
            //    }
            //else if (saturated_fraction > 0.0)
            //    {
            //    layer_is_fully_saturated = false;
            //    layer_is_saturated = true;
            //    }
            //else
            //    {
            //    layer_is_fully_saturated = false;
            //    layer_is_saturated = false;
            //    }


            //if (saturated_fraction_above > 0.0)
            //    {
            //    layer_above_is_saturated = true;
            //    }
            //else
            //    {
            //    layer_above_is_saturated = false;
            //    }



            ////Do the calculation of the water_table      
            //if (layer_is_fully_saturated && layer_above_is_saturated)
            //    {
            //    //layer above is over dul
            //    bottom_depth = DepthDownToBottomOfLayer(sat_layer - 1);
            //    saturated = saturated_fraction_above * lyrAboveSat.dlayer;
            //    DepthToWaterTable = (bottom_depth - saturated);
            //    }
            //else if (layer_is_saturated)
            //    {
            //    //layer above not over dul
            //    bottom_depth = DepthDownToBottomOfLayer(sat_layer);
            //    saturated = saturated_fraction * lyrSat.dlayer;
            //    DepthToWaterTable = (bottom_depth - saturated);
            //    }
            //else
            //    {
            //    //! profile is not saturated
            //    bottom_depth = DepthTotal;
            //    DepthToWaterTable = bottom_depth;
            //    }

            }


        public void SetWaterTable(double InitialDepth)
            {

            double top;
            double bottom;
            double fraction;

            top = 0.0;
            bottom = 0.0;


            foreach (Layer lyr in layers)
                {
                top = bottom;
                bottom = bottom + lyr.dlayer;
                if (InitialDepth >= bottom)
                    {
                    //do nothing;
                    }
                else if (InitialDepth > top)
                    {
                    //! top of water table is in this layer
                    fraction = (bottom - InitialDepth) / (bottom - top);
                    lyr.sw_dep = lyr.dul_dep + fraction * lyr.drainable_capacity;
                    }
                else
                    {
                    lyr.sw_dep = lyr.sat_dep;
                    }
                }

            DepthToWaterTable = InitialDepth;

            }



        #endregion




        #region Lateral Flow

        public void Do_Lateral_Flow(double[] Inflow_lat)
            {
            //private void Lateral_process()
            //    {

            double d;  //depth of water table in a layer (mm)
            double max_flow;



            foreach (Layer lyr in layers)
                {

                //! dsg 150302   add the inflowing lateral water
                if (Inflow_lat.Length >= lyr.number)
                    lyr.sw_dep = lyr.sw_dep + Inflow_lat[lyr.number - 1];


                d = lyr.dlayer * Utility.Math.Divide((lyr.sw_dep - lyr.dul_dep), (lyr.sat_dep - lyr.dul_dep), 0.0);
                d = Math.Max(0.0, d);  //! water table depth in layer must be +ve

                double i, j;
                i = lyr.KLAT * d * (discharge_width / Constants.mm2m) * slope;
                j = (catchment_area * Constants.sm2smm) * (Math.Pow((1.0 + Math.Pow(slope, 2)), 0.5));
                lyr.outflow_lat = Utility.Math.Divide(i, j, 0.0);

                //! Cannot drop sw below dul
                max_flow = Math.Max(0.0, (lyr.sw_dep - lyr.dul_dep));

                lyr.outflow_lat = Constants.bound(lyr.outflow_lat, 0.0, max_flow);

                lyr.sw_dep = lyr.sw_dep - lyr.outflow_lat;
                }


            }


        #endregion




        }





    #endregion





    }


