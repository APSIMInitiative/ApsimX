
using Models.Core;
using Models.PMF;

namespace Models.Soils;


/// <summary>
/// 
/// # APSIM Waterlogging Functions Documentation
///
/// Sotirios Archontoulis, Isaiah Huber, Ke Liu, Matthew Harrison
///
/// Excess soil moisture could affect several processes within the 
/// soil-plant-atmosphere system. Here, we describe new waterlogging functions 
/// added to APSIM and tested in maize, soybean, canola, wheat, and barley. 
/// The new functions affect root depth, radiation use efficiency, phenology, 
/// leaf senescence, and grain components, as reported in the literature. 
/// With the new additions, APSIM crop models can simulate both types of 
/// water stress: drought and excess water. We used SWIM as the 
/// primary soil water model to parameterize the new routines; 
/// however, users can use either SWIM or SoilWat; the new functions work with 
/// both soil water models.
///
/// ## Summary table of processes affected by waterlogging
///
/// |Process|Driver|Model Approach| Crop-specific?|
/// |-------|------|--------------|---------------|
/// |Root growth| AFPS| XY Modifier on root front velocity|Yes|
/// |RUE| Wet root fraction|Min(stress today, stress from legacy effect)|Crop and Stage Specific|
/// |Phenology|Cumulative Excess Water Stress|Phyllochron Modifier|Optional|
/// |Senesence|Cumulative Excess Water Stress|Leaf model modifier|Yes|
/// |Grain components|Cumulative Excess Water Stress| Penalty Functions| Yes|
///
/// ## Roots
///
/// We incorporated into the model the approach we already had in APSIM Classic 
/// (Ebrahimi-Mollabashi et al., 2019; Archontoulis et al., 2020, Liu et al. 2021, 2023).
/// Excess moisture affects the root front.  More specifically, when the 
/// air-filled pore space exceeds 97% of saturation, the root front velocity 
/// decreases for the period of excess stress. Model simulations showed 
/// good agreement with experimental findings (see graph). The users can 
/// alter the root parameters by altering the XY pairs: 
/// “[Root].RootFrontVelocity.AFPSFactor.XYPairs”
///
/// ![Soybean root depth](waterlogging-soybean-root-depth.png)
///
/// ## Radiation Use Efficiency
///
/// Excess moisture stress affects RUE like drought stress. Hence, we updated 
/// the model to calculate water stress on RUE by considering both “Deficient”
/// and “Excess” moisture stress via the “Minimum Function” (i.e. RUE FW = min(F_deficient, F_excess), see model structure).
/// The driver for excess moisture stress is the wet root fraction, which is 
/// calculated as (sw-dul)/(sat-dul)). A 0 to 1 daily value is computed as the 
/// average of it weighted by root length density. Then we use this 
/// information (x-axis) to develop a modifier (y-axis), which was added in the 
/// RUE module (name = Excess).
///
/// ![RUE](waterlogging-rue.png)
///
/// Different crop species could have different sensitivities to excess stress. 
/// Also, different crop growth stages have different sensitivities to excess 
/// stress; for example, maize is more sensitive early in the season, while 
/// soybeans are more sensitive later in the season. To address this, we made 
/// the XY function phase-specific (3 pairs of XY functions; early, middle, 
/// and late phase, user-defined, see below). This addition proved very 
/// important during model calibration.  
///
/// ![FW](waterlogging-water-stress-xy-pairs.png)
///
/// The last aspect we implemented in the model was a “legacy” factor to 
/// reflect the time required for crops to recover after a period of excess 
/// moisture stress (as shown by the persistent reduction). The legacy effect 
/// is modeled as an exponential decay function.
///
/// ![FW legacy effect](waterlogging-fw-legacy-effect.png)
///
/// While measured RUE data were not available, biomass data were used as a 
/// proxy to evaluate model performance, which was judged to be good. An 
/// example for maize is provided below (measured data by Lizaso and Ritchie).  
///
/// ![Maize Biomass](waterlogging-maize-biomass.png)
///
/// ![Maize LAI](waterlogging-maize-lai.png)
///
/// ## Crop Phenology
///
/// Excess moisture could delay phenology (Liu et al., 2020). We model this phenomenon by
/// adjusting the phyllochron parameter via an XY modifier. In the model, the 
/// driver for this delay (x-axis) is the cumulative excess water stress 
/// (“CumulativeExcessWaterStress”). This is off by default but remains 
/// open as a pathway the user could utilize via a custom cultivar.
///
/// ## Leaf Senescence
///
/// Excess moisture could accelerate canopy senescence. We capture this by 
/// adjusting leaf senescence via an XY modifier using 
/// “CumulativeExcessWaterStress” as the driver, similar to phyllochron; 
/// see diagrams below. Currently, there are two distinct leaf models in 
/// APSIM: “Leaf”, which is used by maize, and “SimpleLeaf”, which is used by 
/// soybean and canola. While implementation required different approaches 
/// for different crop models, the concept is similar.  The conceptual 
/// diagram with the driver and new functions are presented below:
///
/// ![Leaf senescence](waterlogging-leaf-senescence.png)
///
/// ## Grain components or harvest index
///
/// While it was expected that changes in senescence rate or RUE would capture 
/// reduced grain number or weight, or harvest index, this was not the case, 
/// indicating that modeling waterlogging is quite challenging. Therefore, we 
/// added functions to capture the reduction in grain components due to 
/// excess moisture.  In maize, we model this as a cumulative penalty for 
/// water stress-driven excess (implemented as a 0-1 multiplier) on maximum 
/// grains per cob. In soybeans, there is a similar penalty on the potential 
/// harvest index. In canola, we increase the maximum potential grain size as 
/// cumulative excess water stress days increase beyond 5.  Please see 
/// model structure for the XY modifiers.
///
/// ![Gain components](waterlogging-grain-components.png)
///
/// ## Sensibility test
///
/// We run three soybean simulations reflecting 3 weather 
/// scenarios (normal, drought, and excess moisture; see below).
///
/// ![Sensibility SW](waterlogging-sensibility-sw.png)
///
/// Both water stress simulations reduced biomass production and grain yield. 
/// The effects were evident in all plant organs, including N-fixation. 
/// We present some figures below:
///
/// ![Sensibility soybean organs](waterlogging-soybean-sensibility-organs.png)
///
/// ## Validation
///
/// We refer users to the APSIM sims to view the validation plots. 
/// Some examples are presented below.
///
/// ![canola grain wt validation graph](waterlogging-canola-grain-wt.png)
///
/// ![maize grain wt validation graph](waterlogging-maize-grain-wt.png)
///
/// ![soybean grain wt validation graph](waterlogging-soybean-grain-wt.png)
///
/// ## References
///
/// Ebrahimi-Mollabashi E, Huth NI, Holzwoth DP, Ordonez RS, Hatfield JL, Huber I, Castellano MJ, Archontoulis SV, 2019. Enhancing APSIM to simulate excessive moisture effects on root growth. Field Crops Research 236: 58–67.
///
/// Pasley HR, Huber I, Castellano MJ, Archontoulis SV, 2020. Modeling flood-induced stress in soybeans. Frontiers Plant Science 11:62, doi:10.3389/fpls.2020.00062.
///
/// Archontoulis SV, Castellano MJ, Licht MA, Nichols V, Baum M, Huber I, Martinez-Feria R, Puntel L, Ordónez RA, Iqbal J, Wright EE, Dietzel RN, Helmers M, Vanloocke A, Liebman M, Hatfield JL, Herzmann D, Cordova SC, Edmonds P, Togliatti K, Kessler A, Danalatos G, Pasley H, Pederson C, Lamkey KR, 2020. Predicting Crop Yields and Soil-Plant Nitrogen Dynamics in the US Corn Belt. Crop Science, 60: 721–738.
///
/// Garcia-Vila M, M dos Santos Vianna, MT Harrison, K Liu, R de S. Nóia-Júnior, T Weber, J Zhao, M Acutis, SV Archontoulis, S Asseng, P Aubry, J Balkovic, B Basso, X Chen, Y Chen, Q de Jong van Lier, M Delandmeter, A de Wit, B Dumont, R Ferrise, C Folberth, M Gabbrielli, T Gaiser, A Gorooei, G Hoogenboom, KC Kersebaum, YU Kim, D Kraus, B Liu, L Martin, K Metselaar, C Nendel, G Padovan, A Perego, DM Seserman, C Scheer, V Shelia, V Stocca, F Tao, E Wang, H Webber, Z Zhao, Y Zhu, T Palosuo (2025) Gaps and strategies for accurate simulation of waterlogging impacts on crop productivity. Nat Food, 6: 553-562. doi:10.1038/s43016-025-01179-y
///
/// Liu K, Harrison MT, Yan H, Liu DL, Meinke H, Hoogenboom G, Wang B, Peng B, Guan K, Jaegermeyr J, Wang E, Zhang F, Yin X, Archontoulis S, Nie L, Badea A, Man J, Wallach D, Zhao J, Benjumea AB, Fahad S, Tian X, Wang W, Tao F, Zhang Z, Rötter R, Yuan Y, Zhu M, Dai P, Nie J, Yang Y, Zhang Y, Zhou M, 2023. Silver lining to a climate crisis in multiple prospects for alleviating crop waterlogging under future climates. Nature Communications 14, 765.
///
/// Liu, K., Harrison, M. T., Archontoulis, S. V., Huth, N., Yang, R., Liu, D. L., Yan, H.L., Meinke, H., Huber, I., Ibrahim, A., Zhang, Y.B. Tian, X.H &amp; Zhou, M. (2021). Climate change shifts forward flowering and reduces crop waterlogging stress. Environmental Research Letters, 16(9), 094017
///
/// Liu, K., Harrison, M. T., Ibrahim, A., Manik, S. N., Johnson, P., Tian, X., Meinke, H.,  Zhou, M. (2020). Genetic factors increasing barley grain yields under soil waterlogging. Food and Energy Security, 9(4), e238
/// </summary>
[ValidParent(ParentType = typeof(Plant))]
public class Waterlogging : Model
{
     
}