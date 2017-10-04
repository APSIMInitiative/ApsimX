
namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class PathwayParametersC4 : PathwayParameters
    {
        /// <summary></summary>
        public PathwayParametersC4()
            : base()
        {

            structuralN = 14;
            SLNRatioTop = 1.3;
            SLNAv = 1.3;

            // Kc μbar	
            Kc_P25 = 1210;
            Kc_c = 25.899;
            Kc_b = 7721.915;

            //Ko μbar	
            Ko_P25 = 292000;
            Ko_c = 4.236;
            Ko_b = 1262.93;

            //Kp μbar	
            Kp_P25 = 139;
            Kp_c = 14.644;
            Kp_b = 4366.129;

            //Vcmax/Vomax	-	
            VcMax_VoMax_P25 = 5.401;
            VcMax_VoMax_c = 9.126;
            VcMax_VoMax_b = 2719.478;

            //Vcmax μmol/m2/s*	
            VcMax_c = 31.467;
            VcMax_b = 9381.766;

            //Vpmax μmol/m2/s*	
            VpMax_c = 38.244;
            VpMax_b = 11402.450;

            //Rd μmol/m2/s*	
            Rd_c = 18.715;
            Rd_b = 5579.745;

            //Jmax(Barley, Farquhar 1980)    μmol/m2/s*	
            JMax_TOpt = 32.633;
            JMax_Omega = 15.270;

            //gm(Arabidopsis, Bernacchi 2002)    μmol/m2/s/bar	
            gm_P25 = 0.55;
            gm_TOpt = 34.309;
            gm_Omega = 20.791;

            psiVc = 0.5;
            psiJ = 2.4;
            psiRd = 0;
            psiVp = 1.0;

            F2 = 0.75;
            F1 = 0.95;

            fcyc = 0.136;
            CiCaRatio = 0.4;
            CiCaRatioIntercept = 0.84;
            CiCaRatioSlope = -0.19;
        }
    }
}
