namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using System;

    /// <summary>
    /// For water contents below DUL, movement depends upon the water content gradient between adjacent layers and the diffusivity,
    /// which is a function of the average water contents of the two layers.
    ///
    /// Unsaturated flow may occur both towards the surface and downwards, but cannot move water out of the bottom of 
    /// the deepest layer in the profile. Flow between adjacent layers ceases at a soil water gradient (gravity_gradient) 
    /// specified in the SoilWater ini file.
    ///
    /// The diffusivity is defined by two parameters set by the user (diffus_const, diffus_slope) in the SoilWater 
    /// parameter set (Default values, from CERES, are 88 and 35.4, but 40 and 16 have been found to be more appropriate 
    /// for describing water movement in cracking clay soils). 
    ///
    /// Diffusivity = diffus_const x exp(diffus_slope x thet_av)
    ///
    /// where
    ///    thet_av is the average of SW - LL15 across the two layers.
    ///    Flow = Diffusivity x Volumetric Soil Water Gradient
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(WaterBalance))]
    public class UnsaturatedFlowModel : Model
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

 
        /// <summary>Calculate unsaturated flow below drained upper limit.</summary>
        public double[] Values
        {
            get
            {
                const double gravity_gradient = 0.00002;

                double[] Thickness = soil.Properties.Thickness;
                double[] SW = soil.Water;
                double[] LL15 = MathUtilities.Multiply(soil.Properties.LL15, soil.Properties.Thickness);
                double[] DUL = MathUtilities.Multiply(soil.Properties.DUL, soil.Properties.Thickness);

                int second_last_layer = Thickness.Length - 2;

                // w_out is water moving out of this layer (mm)
                // +ve = up to next layer
                // -ve = down into this layer
                double w_out = 0.0;

                double[] flow = new double[Thickness.Length];
                for (int i = 0; i <= second_last_layer; i++)
                {
                    double ave_dlayer = (Thickness[i] + Thickness[i+1]) * 0.5;

                    double esw_dep1 = Math.Max((SW[i] - w_out) - LL15[i], 0.0);
                    double esw_dep2 = Math.Max(SW[i+1] - LL15[i+1], 0.0);

                    // theta1 is excess of water content above lower limit,
                    // theta2 is the same but for next layer down.
                    double theta1 = MathUtilities.Divide(esw_dep1, Thickness[i], 0.0);
                    double theta2 = MathUtilities.Divide(esw_dep2, Thickness[i+1], 0.0);

                    // find diffusivity, a function of mean thet.
                    double dbar = soil.DiffusConst * Math.Exp(soil.DiffusSlope * (theta1 + theta2) * 0.5);

                    // testing found that a limit of 10000 (as used in ceres-maize)
                    // for dbar limits instability for flow direction for consecutive
                    // days in some situations.
                    dbar = MathUtilities.Bound(dbar, 0.0, 10000.0);

                    double sw1 = MathUtilities.Divide((SW[i] - w_out), Thickness[i], 0.0);
                    sw1 = Math.Max(sw1, 0.0);

                    double sw2 = MathUtilities.Divide(SW[i+1], Thickness[i+1], 0.0);
                    sw2 = Math.Max(sw2, 0.0);

                    // gradient is defined in terms of absolute sw content
                    // subtract gravity gradient to prevent gradient being +ve when flow_max is -ve, resulting in sw > sat.
                    double gradient = MathUtilities.Divide((sw2 - sw1), ave_dlayer, 0.0) - gravity_gradient;

                    // flow (positive up) = diffusivity * gradient in water content
                    flow[i] = dbar * gradient;

                    // flow will cease when the gradient, adjusted for gravitational
                    // effect, becomes zero.
                    double swg = gravity_gradient * ave_dlayer;

                    // calculate maximum flow
                    double sum_inverse_dlayer = MathUtilities.Divide(1.0, Thickness[i], 0.0) + 
                                                MathUtilities.Divide(1.0, Thickness[i+1], 0.0);
                    double flow_max = MathUtilities.Divide((sw2 - sw1 - swg), sum_inverse_dlayer, 0.0);

                    // this code will stop a saturated layer difusing water into a partially saturated
                    // layer above for Water_table height calculations
                    if (SW[i] >= DUL[i] && SW[i+1] >= DUL[i+1])
                        flow[i] = 0.0;

                    if (flow[i] < 0.0)
                    {
                        // flow is down to layer below.
                        // check capacity of layer below for holding water from this layer
                        // and the ability of this layer to supply the water.
                        // limit unsaturated downflow to a max of dul in next layer.

                        double next_layer_cap = Math.Max(DUL[i+1] - SW[i+1], 0.0);
                        flow_max = Math.Max(flow_max, -1 * next_layer_cap);
                        flow_max = Math.Max(flow_max, -1 * esw_dep1);
                        flow[i] = Math.Max(flow[i], flow_max);
                    }
                    else
                    {
                        if (flow[i] > 0.0)
                        {
                            // flow is up from layer below.
                            // check capacity of this layer for holding water from layer below
                            // and the ability of the layer below to supply the water.
                            // limit unsaturated upflow to a max of dul in this layer.
                            double this_layer_cap = Math.Max(DUL[i] - (SW[i] - w_out), 0.0);
                            flow_max = Math.Min(flow_max, this_layer_cap);
                            flow_max = Math.Min(flow_max, esw_dep2);
                            flow[i] = Math.Min(flow[i], flow_max);
                        }
                        else
                        {
                            // no flow
                        }
                    }
                    
                    // For conservation of water, store amount of water moving
                    // between adjacent layers to use for next pair of layers in profile
                    // when calculating theta1 and sw1.
                    w_out = flow[i];
                }

                return flow;
            }
        }

    }
}
