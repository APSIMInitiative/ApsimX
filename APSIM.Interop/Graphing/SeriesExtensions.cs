using System;
using System.Diagnostics;
using Series = OxyPlot.Series.Series;
using APSIM.Services.Graphing;
using System.Collections.Generic;

namespace APSIM.Interop.Graphing
{
    public static class SeriesExtensions
    {
        private static readonly IEnumerable<ISeriesExporter> seriesExporters = GetSeriesExporters();

        private static IEnumerable<ISeriesExporter> GetSeriesExporters()
        {
            return new ISeriesExporter[5]
            {
                new BarSeriesExporter(),
                new ErrorSeriesExporter(),
                new LineSeriesExporter(),
                new RegionSeriesExporter(),
                new BoxWhiskerSeriesExporter()
            };
        }

        private static Dictionary<Type, ISeriesExporter> exportersLookup = new Dictionary<Type, ISeriesExporter>();

        public static Series ToOxyPlotSeries(this APSIM.Services.Graphing.Series series)
        {
            Type seriesType = series.GetType();
            if (exportersLookup.TryGetValue(seriesType, out ISeriesExporter exporter))
                return exporter.Export(series);
            foreach (ISeriesExporter seriesExporter in seriesExporters)
            {
                if (seriesExporter.CanExport(series))
                {
                    exportersLookup[seriesType] = seriesExporter;
                    return seriesExporter.Export(series);
                }
            }
            throw new NotImplementedException($"Unknown series type {series.GetType().Name}");
        }
    }
}
