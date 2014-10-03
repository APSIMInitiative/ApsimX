using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.OldPlant;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    [Serializable]
    public class Zadok : Model
    {
        [Link]
        Phenology Phenology = null;

        [Link]
        Leaf1 Leaf = null;

        
        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                double zadok_stage = 0.0;
                if (Phenology.InPhase("SowingToGermination"))
                    zadok_stage = 5.0f * fracInCurrent;
                else if (Phenology.InPhase("GerminationToEmergence"))
                    zadok_stage = 5.0f + 5 * fracInCurrent;
                else if (Phenology.InPhase("EmergenceToEndOfJuvenile"))
                    zadok_stage = 10.0f;
                else if (Phenology.InPhase("EndOfJuvenileToFloralInitiation") && fracInCurrent <= 0.9)
                {
                    double leaf_no_now = Math.Max(0.0, Leaf.LeafNumber - 2.0);

                    double[] tillerno_y = { 0, 0, 5 };  // tiller no.
                    double[] tillerno_x = { 0, 5, 8 };  // leaf no.
                    bool DidInterpolate;
                    double tiller_no_now = Utility.Math.LinearInterpReal(leaf_no_now,
                                                                        tillerno_x, tillerno_y,
                                                                        out DidInterpolate);
                    if (tiller_no_now <= 0.0)
                        zadok_stage = 10.0f + leaf_no_now;
                    else
                        zadok_stage = 20.0f + tiller_no_now;
                }
                else if (!Phenology.InPhase("out"))
                {
                    // from senthold's archive:
                    //1    2    3         4.5     5       6
                    //eme  ej   eveg(fl)  anth    sgf   mat
                    //10   30   43          65      70     9

                    // from CropMod
                    //                 sow ger eme  juv    fi   flag    fl st_gf end_gf mat hv_rpe end_crop
                    //stage_code       = 1   2   3    4      5     6     7    8    9    10   11   12
                    //Zadok_stage      = 0   5  10   10     15    40    60   71   87    90   93  100

                    // Plant:
                    //                 sow    ger   eme  juv    fi       fl   st_gf end_gf  mat hv_rpe  end
                    //stage_code      = 1      2    3     4     5   4.9 5.4   6     7      8     9    10     11  ()     ! numeric code for phenological stages
                    //                  na     na   na    na    na   30  43   65    71     87    90    100

                    double[] zadok_code_y = { 30.0, 40.0, 65.0, 71.0, 87.0, 90.0, 100.0 };
                    double[] zadok_code_x = { 4.9, 5.4, 6.0, 7.0, 8.0, 9.0, 10.0 };
                    bool DidInterpolate;
                    zadok_stage = Utility.Math.LinearInterpReal(Phenology.Stage,
                                                               zadok_code_x, zadok_code_y,
                                                               out DidInterpolate);
                }
                return zadok_stage;
            }
        }
    }
}