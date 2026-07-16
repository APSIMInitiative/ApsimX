### Converted the .kml file from Google earth pro into a GeoJson file

import json
import fiona
from shapely.geometry import mapping, shape

input_file = r"C:\Verson control in Git\TropicalAgronomyTechnicalMemorandum\Satellite imaging NDVI\Narayen paddock boundary.kml"
output_file = r"C:\Verson control in Git\TropicalAgronomyTechnicalMemorandum\Satellite imaging NDVI\Narayen paddock boundary.geojson"

fiona.drvsupport.supported_drivers["KML"] = "r"

features = []

with fiona.open(input_file, driver="KML") as src:
    for feature in src:

        geom = mapping(shape(feature["geometry"]))

        features.append({
            "type": "Feature",
            "properties": dict(feature["properties"]),
            "geometry": geom
        })

geojson = {
    "type": "FeatureCollection",
    "features": features
}

with open(output_file, "w", encoding="utf-8") as f:
    json.dump(geojson, f, indent=2)

print(f"Created: {output_file}")
print(f"Features: {len(features)}")

