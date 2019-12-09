namespace UnitTests.Graph
{
    using Models.Core;
    using Models.Core.Run;
    using System.Collections.Generic;

    class MockSimulationDescriptionGenerator : Model, ISimulationDescriptionGenerator
    {
        private IEnumerable<Description> descriptions;

        /// <summary>Constructor.</summary>
        public MockSimulationDescriptionGenerator(IEnumerable<Description> descriptions)
        {
            this.descriptions = descriptions;
        }

        /// <summary>Generate and return simulation descriptions.</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions()
        {
            var returnList = new List<SimulationDescription>();
            foreach (var description in descriptions)
                returnList.Add(description.ToSimulationDescription());
            return returnList;
        }


        public class Description
        {
            private string name;
            private string descriptorName1;
            private string descriptorName2;
            private string descriptorName3;
            private string descriptorValue1;
            private string descriptorValue2;
            private string descriptorValue3;

            /// <summary>Constructor.</summary>
            public Description(string name, string descriptorName1, string descriptorValue1)
            {
                this.name = name;
                this.descriptorName1 = descriptorName1;
                this.descriptorValue1 = descriptorValue1;
            }

            /// <summary>Constructor.</summary>
            public Description(string name,
                   string descriptorName1, string descriptorValue1,
                   string descriptorName2, string descriptorValue2)
            {
                this.name = name;
                this.descriptorName1 = descriptorName1;
                this.descriptorValue1 = descriptorValue1;
                this.descriptorName2 = descriptorName2;
                this.descriptorValue2 = descriptorValue2;
            }

            /// <summary>Constructor.</summary>
            public Description(string name,
                               string descriptorName1, string descriptorValue1,
                               string descriptorName2, string descriptorValue2,
                               string descriptorName3, string descriptorValue3)
            {
                this.name = name;
                this.descriptorName1 = descriptorName1;
                this.descriptorValue1 = descriptorValue1;
                this.descriptorName2 = descriptorName2;
                this.descriptorValue2 = descriptorValue2;
                this.descriptorName3 = descriptorName3;
                this.descriptorValue3 = descriptorValue3;
            }

            public SimulationDescription ToSimulationDescription()
            {
                var description = new SimulationDescription(null, name);
                description.Descriptors.Add(new SimulationDescription.Descriptor(descriptorName1, descriptorValue1));
                if (descriptorName2 != null)
                    description.Descriptors.Add(new SimulationDescription.Descriptor(descriptorName2, descriptorValue2));
                if (descriptorName3 != null)
                    description.Descriptors.Add(new SimulationDescription.Descriptor(descriptorName3, descriptorValue3));
                return description;
            }

        }
    }
}
