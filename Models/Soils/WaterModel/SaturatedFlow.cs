
namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// When water content in any layer is below SAT but above DUL, a fraction of the water drains to the next 
    /// deepest layer each day. 
    /// 
    /// Flux = SWCON x (SW - DUL)
    /// 
    /// Infiltration or water movement into any layer that exceeds the saturation capacity of the layer automatically 
    /// cascades to the next layer.
    /// </summary>
    [Serializable]
    public class SaturatedFlowModel : Model
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

        /// <summary>Gets or sets the swcon.</summary>
        [UnitsAttribute("0-1")]
        public double[] SWCON { get; set; }

        /// <summary>Amount of water (mm) backed up.</summary>
        [XmlIgnore]
        public double backedUpSurface { get; private set; }

        /// <summary>Perform the movement of water.</summary>
        public double[] Values
        {
            get
            {
                backedUpSurface = 0.0;

                double[] SW = soil.Water;
                double[] DUL = MathUtilities.Multiply(soil.Properties.Water.DUL, soil.Properties.Water.Thickness);
                double[] SAT = MathUtilities.Multiply(soil.Properties.Water.SAT, soil.Properties.Water.Thickness);

                double w_in = 0.0;   // water coming into layer (mm)
                double w_out;        // water going out of layer (mm)
                double[] flux = new double[soil.Properties.Water.Thickness.Length];
                for (int i = 0; i < soil.Properties.Water.Thickness.Length; i++)
                {
                    double w_tot = SW[i] + w_in;

                    // Calculate EXCESS Amount (above SAT)

                    // get excess water above saturation & then water left
                    // to drain between sat and dul.  Only this water is
                    // subject to swcon. The excess is not - treated as a
                    // bucket model. (mm)

                    double w_excess;               // amount above saturation(overflow)(mm)
                    if (w_tot > SAT[i])
                    {
                        w_excess = w_tot - SAT[i];
                        w_tot = SAT[i];
                    }
                    else
                        w_excess = 0.0;

                    // Calculate water draining by gravity (mm) (between SAT and DUL)
                    double w_drain;               
                    if (w_tot > DUL[i])
                        w_drain = (w_tot - DUL[i]) * SWCON[i];
                    else
                        w_drain = 0.0;

                    // Calculate EXCESS Flow and DRAIN Flow (combined into Flux)
                    // if there is EXCESS Amount, 
                    if (w_excess > 0.0)
                    {
                        if (soil.Properties.Water.KS == null)
                        {
                            //! all this excess goes on down 
                            w_out = w_excess + w_drain;
                            flux[i] = w_out;
                        }
                        else
                        {
                            // Calculate amount of water to backup and push down
                            // Firstly top up this layer (to saturation)
                            double add = Math.Min(w_excess, w_drain);
                            w_excess = w_excess - add;
                            double new_sw_dep = SAT[i] - w_drain + add;

                            // partition between flow back up and flow down
                            // 'excessDown' is the amount above saturation(overflow) that moves down (mm)
                            double excess_down = Math.Min(soil.Properties.Water.KS[i] - w_drain, w_excess);
                            double backup = w_excess - excess_down;

                            w_out = excess_down + w_drain;
                            flux[i] = w_out;

                            // Starting from the layer above the current layer,
                            // Move up to the surface, layer by layer and use the
                            // backup to fill the space still remaining between
                            // the new sw_dep (that you calculated on the way down) 
                            // and sat for that layer. Once the backup runs out
                            // it will keep going but you will be adding 0.
                            if (i > 0)
                            {
                                for (int j = i - 1; j >= 0; j--)
                                {
                                    flux[j] = flux[j] - backup;
                                    add = Math.Min(SAT[i] - new_sw_dep, backup);
                                    new_sw_dep = new_sw_dep + add;
                                    backup = backup - add;
                                }
                            }

                            backedUpSurface = backedUpSurface + backup;
                        }
                    }
                    else
                    {
                        // there is no EXCESS Amount so only do DRAIN Flow   
                        w_out = w_drain;
                        flux[i] = w_drain;
                    }

                    // drainage out of this layer goes into next layer down
                    w_in = w_out;
                }

                return flux;
            }
        }

    }
}
