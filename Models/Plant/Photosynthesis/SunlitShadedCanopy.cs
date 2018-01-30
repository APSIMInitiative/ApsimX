using System;
using System.Linq;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public enum SSType
    {     /// <summary></summary>
        AC1,
        /// <summary></summary>
        AC2,
        /// <summary></summary>
        AJ
    };

    /// <summary></summary>
    public enum TranspirationMode
    {     /// <summary></summary>
        limited,
        /// <summary></summary>
        unlimited
    };

    /// <summary></summary>
    public class SunlitShadedCanopy
    {
        /// <summary></summary>
        public SSType type;
        /// <summary></summary>
        public int _nLayers;

        /// <summary></summary>
        public double CmTolerance = 1;
        /// <summary></summary>
        public double leafTempTolerance = 0.5;

        /// <summary></summary>
        [ModelVar("yeYEA", "LAI of the sunlit and shade leaf fractions", "LAI", "", "m2/m2", "l,t", "m2 leaf/m2 ground")]
        public double[] LAIS { get; set; }
        /// <summary></summary>
        [ModelVar("aRNmW", "Irradiance incident on leaves", "I", "inc", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] incidentIrradiance { get; set; }
        /// <summary></summary>
        [ModelVar("wKFLV", "PAR absorbed by the sunlit and shade leaf fractions", "I", "abs", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedIrradiance { get; set; }
        /// <summary></summary>
        [ModelVar("0PWTP", "Direct-beam absorbed by leaves", "Integrate over LAIsun", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedRadiationDirect { get; set; }
        /// <summary></summary>
        [ModelVar("OsYZA", "Diffuse absorbed by leaves", "Integrate over LAIsun", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedRadiationDiffuse { get; set; }
        /// <summary></summary>
        [ModelVar("jRJiA", "Scattered-beam absorbed by leaves", "Integrate over LAIsun", "", "μmol m-2 ground s-1")]
        public double[] absorbedRadiationScattered { get; set; }
        /// <summary></summary>
        [ModelVar("wKFLV", "PAR absorbed by the sunlit and shade leaf fractions", "I", "abs", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedIrradianceNIR { get; set; }
        /// <summary></summary>
        [ModelVar("0PWTP", "Direct-beam absorbed by leaves", "Integrate over LAIsun", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedRadiationDirectNIR { get; set; }
        /// <summary></summary>
        [ModelVar("OsYZA", "Diffuse absorbed by leaves", "Integrate over LAIsun", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedRadiationDiffuseNIR { get; set; }
        /// <summary></summary>
        [ModelVar("jRJiA", "Scattered-beam absorbed by leaves", "Integrate over LAIsun", "", "μmol m-2 ground s-1")]
        public double[] absorbedRadiationScatteredNIR { get; set; }
        /// <summary></summary>
        [ModelVar("wKFLV", "PAR absorbed by the sunlit and shade leaf fractions", "I", "abs", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedIrradiancePAR { get; set; }
        /// <summary></summary>
        [ModelVar("0PWTP", "Direct-beam absorbed by leaves", "Integrate over LAIsun", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedRadiationDirectPAR { get; set; }
        /// <summary></summary>
        [ModelVar("OsYZA", "Diffuse absorbed by leaves", "Integrate over LAIsun", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] absorbedRadiationDiffusePAR { get; set; }
        /// <summary></summary>
        [ModelVar("jRJiA", "Scattered-beam absorbed by leaves", "Integrate over LAIsun", "", "μmol m-2 ground s-1")]
        public double[] absorbedRadiationScatteredPAR { get; set; }
        /// <summary></summary>
        [ModelVar("hwe6f", "Vcmax for the sunlit and shade leaf fractions  @ 25°", "V", "c_max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] VcMax25 { get; set; }
        /// <summary></summary>
        [ModelVar("nX8u7", "Vcmax for the sunlit and shade leaf fractions  @ T°", "V", "c_max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] VcMaxT { get; set; }
        /// <summary></summary>
        [ModelVar("y1rt7", "Maximum rate of P activity-limited carboxylation @ 25°", "V", "p_max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] VpMax25 { get; set; }
        /// <summary></summary>
        [ModelVar("Bl7oY", "Maximum rate of Rubisco carboxylation", "V", "p_max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] VpMaxT { get; set; }
        /// <summary></summary>
        [ModelVar("2cAQn", "J2max for the sunlit and shade leaf fractions @ 25°", "J2", "max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] J2Max25 { get; set; }
        /// <summary></summary>
        [ModelVar("UMOz0", "J2max for the sunlit and shade leaf fractions @ T°", "J2", "max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] J2MaxT { get; set; }
        /// <summary></summary>
        [ModelVar("lAx_5b", "Jmax for the sunlit and shade leaf fractions @ 25°", "J", "max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] JMax25 { get; set; }
        /// <summary></summary>
        [ModelVar("Gx6ir", "Maximum rate of electron transport", "J", "max", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] JMaxT { get; set; }
        /// <summary></summary>
        [ModelVar("xMC5L", "Temperature of sunlit and shade leaf fractions", "T", "l", "°C", "l,t")]
        public double[] leafTemp { get; set; }
        /// <summary></summary>
        [ModelVar("xMC5L", "Temperature of sunlit and shade leaf fractions", "T", "l", "°C", "l,t")]
        public double[] leafTemp_1 { get; set; }
        /// <summary></summary>
        [ModelVar("xMC5L", "Temperature of sunlit and shade leaf fractions", "T", "l", "°C", "l,t")]
        public double[] leafTemp_2 { get; set; }
        /// <summary></summary>
        [ModelVar("VOep5", "Leaf-to-air vapour pressure deficit for sunlit and shade leaf fractions", "VPD", "al", "kPa", "l,t")]
        public double[] VPD { get; set; }
        /// <summary></summary>
        [ModelVar("YC5yq", "", "", "", "")]
        public double[] fVPD { get; set; }
        /// <summary></summary>
        [ModelVar("N14in", "PEP regeneration rate at Temp", "V", "pr", "μmol/m2/s", "l,t")]
        public double[] Vpr { get; set; }
        /// <summary></summary>
        [ModelVar("opoaH", "Leaf boundary layer conductance for CO2 for the sunlit and shade fractions", "g", "b_CO2", "mol/m2/s", "l,t", "mol H20, m2 ground")]
        public double[] gb_CO2 { get; set; }
        /// <summary></summary>
        [ModelVar("L3FnM", "Leakiness", "ɸ", "", "", "l,t")]
        public double[] F { get; set; }
        /// <summary></summary>
        [ModelVar("VBqDL", "Half the reciprocal of Sc/o", "γ*", "", "", "l,t")]
        public double[] g_ { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] leafTemp__ { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] Cm__ { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] Cm_1 { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] Cm_2 { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gsw { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] rsw { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] es1 { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] VPD_la { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] Elambda_ { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] TDelta { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] Elambda { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] E_PC { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gbh { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gbw { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] rbw { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] rbh { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gbw_m { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gbCO2 { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gsCO2 { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double[] gsh { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double xx { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double x_4 { get; set; }
        /// <summary></summary>
        [ModelVar("", "", "", "", "", "")]
        public double x_5 { get; set; }
        /// <summary></summary>
        public double[] Cb { get; set; }

        #region Instaneous Photosynthesis
        /// <summary></summary>
        [ModelVar("0svKg", "Rate of electron transport of sunlit and shade leaf fractions", "J", "", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] J { get; set; }
        /// <summary></summary>
        [ModelVar("pmvS3", "Rate of e- transport through PSII", "J2", "", "μmol/m2/s", "l,t")]
        public double[] J2 { get; set; }
        /// <summary></summary>
        [ModelVar("Q7w4j", "CO2 compensation point in the absence of Rd for the sunlit and shade leaf fractions for layer l", "Γ*", "", "μbar", "l,t")]
        public double[] r_ { get; set; }
        /// <summary></summary>
        [ModelVar("LL1b6", "Relative CO2/O2 specificity factor for Rubisco", "S", "c/o", "bar/bar", "l,t")]
        public double[] ScO { get; set; }
        /// <summary></summary>
        [ModelVar("ZLENJ", "Kc for the sunlit and shade leaf fractions for layer l", "K", "c", "μbar", "l,t")]
        public double[] Kc { get; set; }
        /// <summary></summary>
        [ModelVar("TMWa9", "Ko for the sunlit and shade leaf fractions for layer l", "K", "o", "μbar", "l,t")]
        public double[] Ko { get; set; }
        /// <summary></summary>
        [ModelVar("P0HgR", "", "", "", "", "")]
        public double[] VcVo { get; set; }
        /// <summary></summary>
        [ModelVar("hv6R5", "Effective Michaelis-Menten constant", "K'", "", "μbar")]
        public double[] K_ { get; set; }
        /// <summary></summary>
        [ModelVar("AyR6r", "Rd for the sunlit and shade leaf fractions @ 25°", "Rd @ 25", "", "μmol/m2/s1", "l,t", "m2 ground")]
        public double[] Rd25 { get; set; }
        /// <summary></summary>
        [ModelVar("hJOu6", "Rd for the sunlit and shade leaf fractions @ T°", "R", "d", "μmol/m2/s1", "l,t", "m2 ground")]
        public double[] RdT { get; set; }
        /// <summary></summary>
        [ModelVar("CZFJx", "min(Aj, Ac) of sunlit and shade leaf fractions for layer l", "A", "", "μmol CO2/m2/s", "l,t", "m2 ground")]
        public double[] A { get; set; }
        /// <summary></summary>
        [ModelVar("YFblo", "Leaf stomatal conductance for CO2 for the sunlit and shade fractions", "g", "s_CO2", "mol/m2/s", "l,t", "mol H20, m2 ground")]
        public double[] gs_CO2 { get; set; }
        /// <summary></summary>
        [ModelVar("XrAH2", "Leaf mesophyll conductance for CO2 for the sunlit and shade fractions", "g", "m_CO2", "mol/m2/s", "l,t", "mol H20, m2 ground")]
        public double[] gm_CO2T { get; set; }
        /// <summary></summary>
        [ModelVar("cozpi", "Intercellular CO2 partial pressure for the sunlit and shade leaf fractions", "C", "i", "μbar", "l,t")]
        public double[] Ci { get; set; }
        /// <summary></summary>
        [ModelVar("8VmlI", "Chloroplastic CO2 partial pressure for the sunlit and shade leaf fractions", "C", "c", "μbar", "l,t")]
        public double[] Cc { get; set; }
        #endregion

        #region Conductance and Resistance parameters

        #endregion

        #region Leaf temperature from Penman-Monteith combination equation (isothermal form)
        /// <summary></summary>
        [ModelVar("B2QqH", "Laten heat of vaporizatin for water * evaporation rate", "lE", "", "J s-1 kg-1 * kg m-2")]
        public double[] lE { get; set; }
        /// <summary></summary>
        [ModelVar("G3rwq", "Slope of curve relating saturating water vapour pressure to temperature", "s", "", "kPa K-1")]
        public double[] s { get; set; }
        /// <summary></summary>
        [ModelVar("rJSil", "Net isothermal radiation absorbed by the leaf", "Rn", "", "J s-1 m-2")]
        public double[] Rn { get; set; }
        /// <summary></summary>
        [ModelVar("be7vB", "Absorbed short-wave radiation (PAR + NIR)", "Sn", "", "J s-1 m-2")]
        public double[] Sn { get; set; }
        /// <summary></summary>
        [ModelVar("KbcZq", "Outgoing long-wave radiation", "R↑", "", "J s-1m-2")]
        public double[] R_ { get; set; }
        /// <summary></summary>
        [ModelVar("EYrht", "Saturated water vapour pressure @ Tl", "es(Tl)", "", "")]
        public double[] es { get; set; }
        /// <summary></summary>
        [ModelVar("ii3Kr", "Leaf stomatal resistance for H2O", "rs_H2O", "", "s m-1")]
        public double[] rs_H2O { get; set; }
        /// <summary></summary>
        [ModelVar("a4Pbr", "", "gs", "", "m s-1")]
        public double[] gs { get; set; }
        /// <summary></summary>
        [ModelVar("XenBm", "Michealise-Menten constant of PEPc for CO2", "K", "p", "μbar", "l,t", "", true)]
        public double[] Kp { get; set; }
        /// <summary></summary>
        [ModelVar("09u65", "(Mesophyll oxygen partial pressure for sunlit and shade leaf fractions,", "O", "m", "μbar", "l,t")]
        public double[] Om { get; set; }
        /// <summary></summary>
        [ModelVar("85YYK", "Intercellular oxygen partial pressure for sunlit and shade leaf fractions", "O", "i", "μbar", "l,t")]
        public double[] Oi { get; set; }
        /// <summary></summary>
        [ModelVar("nIyeA", "Ci to Ca ratio", "Ci/Ca", "ratio", "", "l,t")]
        public double[] CiCaRatio { get; set; }
        #endregion

        /// <summary></summary>
        [ModelVar("s4Vg0", "Oxygen partial pressure at the oxygenating site of Rubisco in the chloroplast for sunlit and shade leaf fractions", "O", "c", "μbar", "l,t")]
        public double[] Oc { get; set; }
        /// <summary></summary>
        [ModelVar("mmxWN", "Mesophyll CO2 partial pressure for the sunlit and shade leaf fractions", "C", "m", "μbar", "l,t", "", true)]
        public double[] Cm { get; set; }
        /// <summary></summary>
        [ModelVar("7u3JF", "Chloroplast or BS CO2 partial pressuer", "Cbs", "", "μbar")]
        public double[] Cbs { get; set; }
        /// <summary></summary>
        [ModelVar("JiUIt", "Rate of PEPc", "V", "p", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] Vp { get; set; }
        /// <summary></summary>
        [ModelVar("FCzCY", "Gross canopy CO2 assimilation per second", "Ag", "", "μmol CO2 m-2 ground s-1")]
        public double[] Ag { get; set; }
        /// <summary></summary>
        [ModelVar("a2eBP", "", "", "", "")]
        public double[] L { get; set; }
        /// <summary></summary>
        [ModelVar("iuUvg", "Mitochondrial respiration occurring in the mesophyll", "R", "m", "μmol/m2/s", "l,t", "m2 ground")]
        public double[] Rm { get; set; }
        /// <summary></summary>
        [ModelVar("oem3o", "Conductance to CO2 leakage from the bundle sheath to mesophyll", "g", "bsCO2", "mol/m2/s", "l,t", "mol of H20, m2 leaf", true)]
        public double[] gbs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        //---------------------------------------------------------------------------------------------------------
        public SunlitShadedCanopy() { }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nLayers"></param>
        /// <param name="type"></param>
        public SunlitShadedCanopy(int nLayers, SSType type)
        {
            //_nLayers = nLayers;
            this.type = type;
            initArrays(nLayers);
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nLayers"></param>
        public void initArrays(int nLayers)
        {
            _nLayers = nLayers;

            incidentIrradiance = new double[nLayers];
            absorbedRadiationDirect = new double[nLayers];
            absorbedRadiationDiffuse = new double[nLayers];
            absorbedRadiationScattered = new double[nLayers];
            absorbedIrradiance = new double[nLayers];

            absorbedIrradianceNIR = new double[nLayers];
            absorbedRadiationDirectNIR = new double[nLayers];
            absorbedRadiationDiffuseNIR = new double[nLayers];
            absorbedRadiationScatteredNIR = new double[nLayers];

            absorbedIrradiancePAR = new double[nLayers];
            absorbedRadiationDirectPAR = new double[nLayers];
            absorbedRadiationDiffusePAR = new double[nLayers];
            absorbedRadiationScatteredPAR = new double[nLayers];

            VcMax25 = new double[nLayers];
            VcMaxT = new double[nLayers];
            J2Max25 = new double[nLayers];
            J2MaxT = new double[nLayers];
            JMaxT = new double[nLayers];
            JMax25 = new double[nLayers];
            VpMax25 = new double[nLayers];
            VpMaxT = new double[nLayers];

            J2 = new double[nLayers];
            J = new double[nLayers];
            r_ = new double[nLayers];
            ScO = new double[nLayers];
            Kc = new double[nLayers];
            Ko = new double[nLayers];
            VcVo = new double[nLayers];
            K_ = new double[nLayers];
            Rd25 = new double[nLayers];
            RdT = new double[nLayers];
            A = new double[nLayers];
            gs_CO2 = new double[nLayers];
            gm_CO2T = new double[nLayers];
            Ci = new double[nLayers];
            Cc = new double[nLayers];

            lE = new double[nLayers];
            s = new double[nLayers];
            Rn = new double[nLayers];
            Sn = new double[nLayers];
            es = new double[nLayers];
            rs_H2O = new double[nLayers];
            gs = new double[nLayers];

            leafTemp = new double[nLayers];
            es = new double[nLayers];
            s = new double[nLayers];
            gs = new double[nLayers];
            rs_H2O = new double[nLayers];
            lE = new double[nLayers];
            Sn = new double[nLayers];
            R_ = new double[nLayers];
            g_ = new double[nLayers];

            Rn = new double[nLayers];
            VPD = new double[nLayers];
            fVPD = new double[nLayers];
            gb_CO2 = new double[nLayers];

            Vpr = new double[nLayers];

            //C4 variables 
            Oc = new double[nLayers];
            Cm = new double[nLayers];
            Cc = new double[nLayers];
            Vp = new double[nLayers];
            Ag = new double[nLayers];
            Rm = new double[nLayers];
            L = new double[nLayers];
            gbs = new double[nLayers];
            F = new double[nLayers];

            Kp = new double[nLayers];
            Om = new double[nLayers];
            Oi = new double[nLayers];
            CiCaRatio = new double[nLayers];

            leafTemp__ = new double[nLayers];
            leafTemp_1 = new double[nLayers];
            leafTemp_2 = new double[nLayers];
            Cm__ = new double[nLayers];
            Cm_1 = new double[nLayers];
            Cm_2 = new double[nLayers];

            gsw = new double[nLayers];
            rsw = new double[nLayers];
            es1 = new double[nLayers];
            VPD_la = new double[nLayers];
            Elambda_ = new double[nLayers];
            TDelta = new double[nLayers];
            Elambda = new double[nLayers];
            E_PC = new double[nLayers];

            gbh = new double[nLayers];
            gbw = new double[nLayers];

            rbw = new double[nLayers];
            rbh = new double[nLayers];
            gbw_m = new double[nLayers];
            gbCO2 = new double[nLayers];

            gsCO2 = new double[nLayers];
            gsw = new double[nLayers];
            gsh = new double[nLayers];

            Cb = new double[nLayers];
        }

        /// <summary></summary>
        public virtual void run(int nlayers, PhotosynthesisModel PM, SunlitShadedCanopy counterpart)
        {
            _nLayers = nlayers;
            initArrays(_nLayers);
            calcIncidentRadiation(PM.envModel, PM.canopy, counterpart);
            calcAbsorbedRadiation(PM.envModel, PM.canopy, counterpart);
            calcMaxRates(PM.canopy, counterpart, PM);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="counterpart"></param>
        public virtual void calcLAI(LeafCanopy canopy, SunlitShadedCanopy counterpart) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="EM"></param>
        /// <param name="canopy"></param>
        /// <param name="counterpart"></param>
        public virtual void calcIncidentRadiation(EnvironmentModel EM, LeafCanopy canopy, SunlitShadedCanopy counterpart) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="EM"></param>
        /// <param name="canopy"></param>
        /// <param name="counterpart"></param>
        public virtual void calcAbsorbedRadiation(EnvironmentModel EM, LeafCanopy canopy, SunlitShadedCanopy counterpart) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="counterpart"></param>
        /// <param name="EM"></param>
        public virtual void calcMaxRates(LeafCanopy canopy, SunlitShadedCanopy counterpart, PhotosynthesisModel EM) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public double solveQuadratic(double a, double b, double c, double d)
        {
            return (-1 * Math.Pow((Math.Pow(a, 2) - 4 * b), 0.5) + c) / (2 * d);
        }
        ////---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PM"></param>
        /// <param name="canopy"></param>
        public virtual void calcConductanceResistance(PhotosynthesisModel PM, LeafCanopy canopy)
        {
            for (int i = 0; i < canopy.nLayers; i++)
            {
                //gbh[i] = 0.01 * Math.Pow((canopy.us[i] / canopy.leafWidth), 0.5) *
                //    (1 - Math.Exp(-1 * (0.5 * canopy.ku + canopy.kb) * canopy.LAI)) / (0.5 * canopy.ku + canopy.kb);

                gbw[i] = gbh[i] / 0.93;

                rbh[i] = 1 / gbh[i];

                rbw[i] = 1 / gbw[i];

                gbw_m[i] = PM.envModel.ATM * PM.canopy.rair * gbw[i];

                gbCO2[i] = gbw_m[i] / 1.37;

            }
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PM"></param>
        /// <param name="canopy"></param>
        public virtual void calcWaterUse(PhotosynthesisModel PM, LeafCanopy canopy)
        {
            for (int i = 0; i < canopy.nLayers; i++)
            {
                gsCO2[i] = A[i] / (((1 - canopy.CPath.CiCaRatio) - A[i] * Math.Pow(gbCO2[i] * canopy.Ca, -1)) * canopy.Ca);

                gsw[i] = gsCO2[i] * 1.6;

                rsw[i] = canopy.rair / gsw[i] * PM.envModel.ATM;

                VPD_la[i] = PM.envModel.calcSVP(leafTemp__[i]) - PM.envModel.calcSVP(PM.envModel.minT);

                double totalAbsorbed = absorbedIrradiancePAR[i] + absorbedIrradianceNIR[i];

                Rn[i] = totalAbsorbed - 2 * (canopy.sigma * Math.Pow(273 + leafTemp__[i], 4) - canopy.sigma * Math.Pow(273 + PM.envModel.getTemp(PM.time), 4));

                Elambda_[i] = (canopy.s * Rn[i] + VPD_la[i] * canopy.rcp / rbh[i]) /
                    (canopy.s + canopy.g * (rsw[i] + rbw[i]) / rbh[i]);

            }
        }
        //---------------------------------------------------------------------------------------------------------
    }
}