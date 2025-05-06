using System.Collections.Generic;
using APSIM.Shared.Documentation;
using ModelsMap = Models.Map;
using Models.Core;
using DocumentationCoordinate = APSIM.Shared.Documentation.Mapping.Coordinate;
using ModelsCoordinate = Models.Mapping.Coordinate;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation for Map
    /// </summary>
    public class DocMap : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocMap" /> class.
        /// </summary>
        public DocMap(IModel model): base(model) {}

        /// <summary>
        /// Document the model
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSectionTitle(model);
            ModelsMap map = model as ModelsMap;
            Map newMap = new(
                new DocumentationCoordinate(map.Center.Latitude, map.Center.Longitude),
                map.Zoom,
                GetConvertedMarkers(map.GetCoordinates())
            );
            section.Add(newMap);
            return new List<ITag>() {section};
        }        

        /// <summary>
        /// Converts a list of Models assembly coordinates to the documentation version of coordinates.
        /// Reason: Mapsui is an external nuget package that requires a MapTag, which requires a specific type of Models.Mapping.Coordinates.
        /// The MapView specifically uses MapTag to show maps in ApsimNG GUI.
        /// </summary>
        /// <param name="markers"></param>
        /// <returns></returns>
        public static List<DocumentationCoordinate> GetConvertedMarkers(IEnumerable<ModelsCoordinate> markers)
        {
            List<DocumentationCoordinate> convertedMarkerList = new();
            foreach( ModelsCoordinate modelCoord in markers)
            {
                convertedMarkerList.Add(new DocumentationCoordinate(modelCoord.Latitude,modelCoord.Longitude));
            }
            return convertedMarkerList;
        }
    }
}
