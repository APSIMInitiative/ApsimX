namespace Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>A painter for setting the visual element of a simulation description using consecutive values of up to three visual elements.</summary>
    public class SequentialPainter : ISeriesDefinitionPainter
    {
        private Series series;
        private List<string> values = new List<string>();
        private List<Tuple<int, int, int>> indexMatrix = new List<Tuple<int, int, int>>();
        private string descriptorName;
        private SetFunction setter1 { get; set; }
        private SetFunction setter2 { get; set; }
        private SetFunction setter3 { get; set; }

        /// <summary>Constructor</summary>
        public SequentialPainter(Series series,
                                 string descriptorName,
                                 int maximumIndex1,
                                 SetFunction set1)
        {
            this.series = series;
            this.descriptorName = descriptorName;
            for (int i = 0; i < maximumIndex1; i++)
                indexMatrix.Add(new Tuple<int, int, int>(i, -1, -1));
            setter1 = set1;
        }

        /// <summary>Constructor</summary>
        public SequentialPainter(Series series,
                                 string descriptorName,
                                 int maximumIndex1, int maximumIndex2,
                                 SetFunction set1, SetFunction set2)
        {
            this.series = series;
            this.descriptorName = descriptorName;
            for (int j = 0; j < maximumIndex2; j++)
                for (int i = 0; i < maximumIndex1; i++)
                    indexMatrix.Add(new Tuple<int, int, int>(i, j, -1));
            setter1 = set1;
            setter2 = set2;
        }

        /// <summary>Constructor</summary>
        public SequentialPainter(Series series,
                                 string descriptorName,
                                 int maximumIndex1, int maximumIndex2, int maximumIndex3,
                                 SetFunction set1, SetFunction set2, SetFunction set3)
        {
            this.series = series;
            this.descriptorName = descriptorName;
            for (int k = 0; k < maximumIndex3; k++)
                for (int j = 0; j < maximumIndex2; j++)
                    for (int i = 0; i < maximumIndex1; i++)
                        indexMatrix.Add(new Tuple<int, int, int>(i, j, k));
            setter1 = set1;
            setter2 = set2;
            setter3 = set3;
        }

        /// <summary>Set visual aspects (colour, line type, marker type) of the series definition.</summary>
        /// <param name="seriesDefinition">The definition to paint.</param>
        public void Paint(SeriesDefinition seriesDefinition)
        {
            int index;
            if (descriptorName == "Graph series")
            {
                index = series.Parent.Children.IndexOf(series);
            }
            else
            {
                var descriptor = seriesDefinition.Descriptors.Find(d => d.Name == descriptorName);

                index = values.IndexOf(descriptor.Value);
                if (index == -1)
                {
                    values.Add(descriptor.Value);
                    index = values.Count - 1;
                }
            }
            if (index >= indexMatrix.Count)
                index = index % indexMatrix.Count;
            setter1(seriesDefinition, indexMatrix[index].Item1);
            setter2?.Invoke(seriesDefinition, indexMatrix[index].Item2);
            setter3?.Invoke(seriesDefinition, indexMatrix[index].Item3);
        }
    }
}
