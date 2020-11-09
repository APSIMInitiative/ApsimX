namespace Models
{
    using System.Collections.Generic;

    /// <summary>A painter for setting the visual element of a simulation description to values of two visual elements.</summary>
    public class MultiDescriptorPainter : ISeriesDefinitionPainter
    {
        private List<string> values1 = new List<string>();
        private List<string> values2 = new List<string>();
        private List<string> values3 = new List<string>();

        private int maximumIndex1;
        private int maximumIndex2;
        private int maximumIndex3;
        private string descriptorName1;
        private string descriptorName2;
        private string descriptorName3;
        private SetFunction setter1;
        private SetFunction setter2;
        private SetFunction setter3;

        /// <summary>Constructor</summary>
        public MultiDescriptorPainter(string descriptorName, int maximumIndex, SetFunction setter)
        {
            descriptorName = descriptorName1;
            maximumIndex1 = maximumIndex;
            setter1 = setter;
        }

        /// <summary>Constructor</summary>
        public MultiDescriptorPainter(string descriptorName1, string descriptorName2, 
                                      int maximumIndex1, int maximumIndex2,
                                      SetFunction setter1, SetFunction setter2)
        {
            this.descriptorName1 = descriptorName1;
            this.descriptorName2 = descriptorName2;
            this.maximumIndex1 = maximumIndex1;
            this.maximumIndex2 = maximumIndex2;
            this.setter1 = setter1;
            this.setter2 = setter2;
        }

        /// <summary>Constructor</summary>
        public MultiDescriptorPainter(string descriptorName1, string descriptorName2, string descriptorName3,
                                      int maximumIndex1, int maximumIndex2, int maximumIndex3,
                                      SetFunction setter1, SetFunction setter2, SetFunction setter3)
        {
            this.descriptorName1 = descriptorName1;
            this.descriptorName2 = descriptorName2;
            this.descriptorName3 = descriptorName3;
            this.maximumIndex1 = maximumIndex1;
            this.maximumIndex2 = maximumIndex2;
            this.maximumIndex3 = maximumIndex3;
            this.setter1 = setter1;
            this.setter2 = setter2;
            this.setter3 = setter3;
        }

        /// <summary>Set visual aspects (colour, line type, marker type) of the series definition.</summary>
        /// <param name="seriesDefinition">The definition to paint.</param>
        public void Paint(SeriesDefinition seriesDefinition)
        {
            var descriptor1 = seriesDefinition.Descriptors.Find(d => d.Name == descriptorName1);
            if (descriptor1 != null)
            {
                string descriptorValue1 = descriptor1.Value;

                int index1 = values1.IndexOf(descriptorValue1);
                if (index1 == -1)
                {
                    values1.Add(descriptorValue1);
                    index1 = values1.Count - 1;
                }
                index1 = index1 % maximumIndex1;
                setter1(seriesDefinition, index1);
            }

            var descriptor2 = seriesDefinition.Descriptors.Find(d => d.Name == descriptorName2);
            if (descriptor2 != null)
            {
                string descriptorValue2 = descriptor2.Value;

                int index2 = values2.IndexOf(descriptorValue2);
                if (index2 == -1)
                {
                    values2.Add(descriptorValue2);
                    index2 = values2.Count - 1;
                }
                index2 = index2 % maximumIndex2;
                setter2(seriesDefinition, index2);
            }

            if (descriptorName3 != null)
            {
                var descriptor3 = seriesDefinition.Descriptors.Find(d => d.Name == descriptorName3);
                if(descriptor3 != null)
                {
                    var descriptorValue3 = descriptor3.Value;

                    var index3 = values3.IndexOf(descriptorValue3);
                    if (index3 == -1)
                    {
                        values3.Add(descriptorValue3);
                        index3 = values3.Count - 1;
                    }
                    index3 = index3 % maximumIndex3;
                    setter3(seriesDefinition, index3);
                }
            }
        }
    }

}
