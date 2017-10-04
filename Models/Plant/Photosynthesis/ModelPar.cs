
namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class ModelPar : System.Attribute
    {
        /// <summary></summary>
        public string symbol;
        /// <summary></summary>
        public string description;
        /// <summary></summary>
        public string units;
        /// <summary></summary>
        public string subscript;
        /// <summary></summary>
        public string scope;
        /// <summary></summary>
        public string unitsTip;
        /// <summary></summary>
        public string id;

        /// <summary></summary>
        public bool C4Only;

        /// <summary></summary>
        public object value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="_desc"></param>
        /// <param name="_sym"></param>
        /// <param name="_subscript"></param>
        /// <param name="_units"></param>
        /// <param name="_scope"></param>
        /// <param name="_unitsTip"></param>
        /// <param name="_C4Only"></param>
        public ModelPar(string id, string _desc, string _sym, string _subscript, string _units, string _scope="", string _unitsTip="", bool _C4Only = false)
        {
            this.id = id;
            this.symbol = _sym;
            this.description = _desc;
            this.units = _units;
            this.subscript = _subscript;
            this.scope = _scope;
            this.unitsTip = _unitsTip;
            this.C4Only = _C4Only;
            value = null;
        }
    }
}
