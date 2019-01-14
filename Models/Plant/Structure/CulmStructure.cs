using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Phen;
using System.Xml.Serialization;
using Models.Interfaces;

namespace Models.PMF.Struct
{
    /// <summary>
    /// # Structure
    /// The structure model simulates morphological development of the plant to inform the Leaf class when 
    ///   and how many leaves appear and to provide a hight estimate for use in calculating potential transpiration.
    /// ## Plant and Main-Stem Population
    /// The *Plant.Population* is set at sowing with information sent from a manager script in the Sow method.    
    ///   The *PrimaryBudNumber* is also sent with the Sow method and the main-stem population (*MainStemPopn*) for the crop is calculated as:  
    ///   *MainStemPopn* = *Plant.Population* x *PrimaryBudNumber*
    ///   Primary bud number is > 1 for crops like potato and grape vine where there are more than one main-stem per plant
    ///  ## Main-Stem leaf appearance
    ///  Each day the number of main-stem leaf tips appeared (*LeafTipsAppeared*) is calculated as:  
    ///    *LeafTipsAppeared* += *DeltaTips*
    ///  Where *DeltaTips* is calculated as:  
    ///    *DeltaTips* = *ThermalTime*/*Phyllochron*  
    ///    Where *Phyllochron* is the thermal time duration between the appearance of leaf tipx given by: 
    /// [Document Phyllochron]
    ///   and *ThermalTime* is given by:
    /// [Document ThermalTime]
    /// *LeafTipsAppeared* continues to increase until *FinalLeafNumber* is reached where *FinalLeafNumber* is calculated as:  
    /// [Document FinalLeafNumber]
    /// ##Branching and Branch Mortality
    /// The total population of stems (*TotalStemPopn*) is calculated as:  
    ///   *TotalStemPopn* = *MainStemPopn* + *NewBranches* - *NewlyDeadBranches*   
    ///    Where *NewBranches* = *MainStemPopn* x *BranchingRate*  
    ///    and *BranchingRate* is given by:
    /// [Document BranchingRate]
    ///   *NewlyDeadBranches* is calcualted as:  
    ///   *NewlyDeadBranches* = (*TotalStemPopn* - *MainStemPopn*) x *BranchMortality*  
    ///   where *BranchMortality* is given by:  
    /// [Document BranchMortality]
    /// ##Height
    ///  The Height of the crop is calculated by the *HeightModel*:
    /// [Document HeightModel]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CulmStructure : Model
    {
        // 1. Links
        //-------------------------------------------------------------------------------------------
        [Link]
        private Plant plant = null;

        [Link]
        private ILeaf leaf = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>The thermal time</summary>
        [Link]
        public IFunction thermalTime = null;

        [Link]
        private IFunction phyllochron = null;

        /// <summary>The main stem final node number</summary>
        [Link]
        public IFunction finalLeafNumber = null;

        [Link]
        private IFunction heightModel = null;


    }
}
