
namespace Models.Stock
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStockEvent
    {

    }

    /// <summary>
    /// The Add event
    /// </summary>
    public class TStockAdd: IStockEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public string genotype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int birth_day { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int min_years { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int max_years { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double mean_weight { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double cond_score { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double mean_fleece_wt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int shear_day { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mated_to { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int pregnant { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double foetuses { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int lactating { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double offspring { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double young_wt { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double young_cond_score { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double young_fleece_wt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int usetag { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockBuy: IStockEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public string genotype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double age { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double weight { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double fleece_wt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double cond_score { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mated_to { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int pregnant { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int lactating { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int no_young { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double young_wt { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double young_fleece_wt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int usetag { get; set; }

    }

    /// <summary>
    /// Sell event
    /// </summary>
    public class TStockSell
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Castrate event
    /// </summary>
    public class TStockCastrate
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Dryoff event
    /// </summary>
    public class TStockDryoff
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockSellTag
    {
        /// <summary>
        /// 
        /// </summary>
         public int tag { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockShear
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sub_group { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockMove
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string paddock { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockJoin
    {
        /// <summary>
        /// 
        /// </summary>
             public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mate_to { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int mate_days { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockWean
    {
        /// <summary>
        /// 
        /// </summary>
               public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockSplitAll
    {
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double value { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int othertag { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockSplit
    {
        /// <summary>
        /// 
        /// </summary>
                   public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double value { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int othertag { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockTag
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockPrioritise
    {
        /// <summary>
        /// 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TStockSort
    {

    }

    /// <summary>
    /// Draft event
    /// </summary>
    public class TStockDraft
    {
        /// <summary>
        /// Paddocks that are closed
        /// </summary>
        public string[] closed { get; set; }
    }

    /// <summary>
    /// Event types 
    /// </summary>
    public class TStockEventTypes
    {
        /// <summary>
        /// Tag by number event ddml type
        /// </summary>
        public const string typeTAG_NUMBER = "<type>"
             + "<field name=\"tag\"    kind=\"integer4\"/>"
             + "<field name=\"number\" kind=\"integer4\"/>"
             + "</type>";
        /// <summary>
        /// Group ddml type
        /// </summary>
        public const string typeST_GROUP = "<type>"
                   + "<field name=\"group\"     kind=\"integer4\"/>"
                   + "</type>";
        /// <summary>
        /// Dry ddml type
        /// </summary>
        public const string typeDRYM = "<type>"
                   + "<field name=\"dm\" unit=\"kg\"  kind=\"double\"/>"
                   + "<field name=\"n\"  unit=\"kg\"  kind=\"double\"/>"
                   + "<field name=\"p\"  unit=\"kg\"  kind=\"double\"/>"
                   + "<field name=\"s\"  unit=\"kg\"  kind=\"double\"/>"
                   + "</type>";
        /// <summary>
        /// Add faeces event ddml type
        /// </summary>
        public const string typeADDFAECES = "<type name=\"AddFaeces\">"
                      + "<field name=\"Defaecations\"         unit=\"-\"    kind=\"double\"/>"
                      + "<field name=\"VolumePerDefaecation\" unit=\"m^3\"  kind=\"double\"/>"
                      + "<field name=\"AreaPerDefaecation\"   unit=\"m^2\"  kind=\"double\"/>"
                      + "<field name=\"Eccentricity\"         unit=\"-\"    kind=\"double\"/>"
                      + "<field name=\"OMWeight\"   unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"OMN\"        unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"OMP\"        unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"OMS\"        unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"OMAshAlk\"   unit=\"mol/ha\" kind=\"double\"/>"
                      + "<field name=\"NO3N\"  unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"NH4N\"  unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"POXP\"  unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"SO4S\"  unit=\"kg/ha\"  kind=\"double\"/>"
                      + "</type>";
        /// <summary>
        /// Add urine event ddml type
        /// </summary>
        public const string typeADDURINE = "<type name=\"AddUrine\">"
                      + "<field name=\"Urinations\"         unit=\"-\"  kind=\"double\"/>"
                      + "<field name=\"VolumePerUrination\" unit=\"m^3\"  kind=\"double\"/>"
                      + "<field name=\"AreaPerUrination\"   unit=\"m^2\"  kind=\"double\"/>"
                      + "<field name=\"Eccentricity\"       unit=\"-\"  kind=\"double\"/>"
                      + "<field name=\"Urea\"     unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"POX\"      unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"SO4\"      unit=\"kg/ha\"  kind=\"double\"/>"
                      + "<field name=\"AshAlk\"   unit=\"mol/ha\" kind=\"double\"/>"
                      + "</type>";

    }
}
