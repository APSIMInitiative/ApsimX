
using Models.Core;
namespace Models.Graph
{
    public class Series : Model
    {
        public enum SeriesType { Line, Bar };
        public string Title { get; set; }
        public SeriesType Type { get; set; }
        public string SimulationName { get; set; }
        public string TableName { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
    }
}
