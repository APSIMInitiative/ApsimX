namespace UnitTests.Graph
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models;
    using Models.Storage;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using static UnitTests.Graph.MockSimulationDescriptionGenerator;
    using APSIM.Shared.Graphing;
    using Series = Models.Series;
    using Moq;
    using System.Data;
    using System;
    using UnitTests.Storage;

    [TestFixture]
    class SeriesTests
    {
        /// <summary>Create a single xy serie definition with no 'VaryBy' groupings.</summary>
        [Test]
        public void SeriesWithNoVaryBy()
        {
            var sim = new Simulation()
            {
                Name = "Sim1",
                Children = new List<IModel>()
                {
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2"
                            }
                        }
                    }
                }
            };
            sim.ParentAllDescendants();

            string data =
                "CheckpointName  SimulationID  Col1  Col2\r\n" +
                "            ()            ()    ()   (g)\r\n" +
                "       Current             1     1    10\r\n" +
                "       Current             1     1    10\r\n" +
                "       Current             1     2    20\r\n" +
                "       Current             1     2    20\r\n";

            var reader = new TextStorageReader(data);

            var graph = sim.Children[0] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions[0].XFieldName, Is.EqualTo("Col1"));
            Assert.That(definitions[0].SeriesDefinitions[0].YFieldName, Is.EqualTo("Col2"));
            Assert.That(definitions[0].SeriesDefinitions[0].YError, Is.Null);
            Assert.That(definitions[0].SeriesDefinitions[0].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[0].LineThickness, Is.EqualTo(LineThickness.Normal));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].MarkerSize, Is.EqualTo(MarkerSize.Normal));
            Assert.That(definitions[0].SeriesDefinitions[0].ShowInLegend, Is.False);
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Series"));
            Assert.That(definitions[0].SeriesDefinitions[0].Type, Is.EqualTo(SeriesType.Bar));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 1, 2, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 10, 20, 20 }));
            Assert.That(definitions[0].SeriesDefinitions[0].X2, Is.Null);
            Assert.That(definitions[0].SeriesDefinitions[0].Y2, Is.Null);
            Assert.That(definitions[0].SeriesDefinitions[0].XAxis, Is.EqualTo(AxisPosition.Bottom));
            Assert.That(definitions[0].SeriesDefinitions[0].YAxis, Is.EqualTo(AxisPosition.Left));
            Assert.That(definitions[0].SeriesDefinitions[0].XFieldUnits, Is.EqualTo("()"));
            Assert.That(definitions[0].SeriesDefinitions[0].YFieldUnits, Is.EqualTo("(g)"));
        }

        /// <summary>Create two series definitions due to a single 'VaryBy' grouping.</summary>
        [Test]
        public void SeriesWithOneVaryBy()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Exp"
                            }
                        }
                    },
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "Exp", "Exp2")
                    })
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID   Exp Col1  Col2\r\n" +
                "            ()              ()    ()   ()   (g)\r\n" +
                "       Current               1  Exp1    1    10\r\n" +
                "       Current               1  Exp1    1    10\r\n" +
                "       Current               2  Exp2    2    20\r\n" +
                "       Current               2  Exp2    2    20\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[0] as Graph;
            var series = graph.Children[0] as Series;

            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("Exp"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            foreach (var definition in definitions[0].SeriesDefinitions)
            {
                Assert.That(definition.XFieldName, Is.EqualTo("Col1"));
                Assert.That(definition.YFieldName, Is.EqualTo("Col2"));
                Assert.That(definition.YError, Is.Null);
                Assert.That(definition.Line, Is.EqualTo(LineType.Solid));
                Assert.That(definition.LineThickness, Is.EqualTo(LineThickness.Normal));
                Assert.That(definition.Marker, Is.EqualTo(MarkerType.FilledCircle));
                Assert.That(definition.MarkerSize, Is.EqualTo(MarkerSize.Normal));
                Assert.That(definition.ShowInLegend, Is.False);
                Assert.That(definition.Type, Is.EqualTo(SeriesType.Bar));
                Assert.That(definition.X2, Is.Null);
                Assert.That(definition.Y2, Is.Null);
                Assert.That(definition.XAxis, Is.EqualTo(AxisPosition.Bottom));
                Assert.That(definition.YAxis, Is.EqualTo(AxisPosition.Left));
                Assert.That(definition.XFieldUnits, Is.EqualTo("()"));
                Assert.That(definition.YFieldUnits, Is.EqualTo("(g)"));
            }

            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Exp1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 1 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 10 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Exp2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 2, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new int[] { 20, 20 }));
        }

        /// <summary>Create four series definitions due to a two 'VaryBy' groupings.</summary>
        [Test]
        public void SeriesWithTwoVaryBy()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Irr", "Dry", "Fert", "0"),
                        new Description("Sim2", "Irr", "Dry", "Fert", "10"),
                        new Description("Sim3", "Irr", "Wet", "Fert", "0"),
                        new Description("Sim4", "Irr", "Wet", "Fert", "10")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Irr",
                                FactorToVaryLines = "Fert"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID     Irr  Fert   Col1  Col2\r\n" +
                "            ()              ()      ()    ()   ()     (g)\r\n" +
                "       Current               1     Dry     0   1      10\r\n" +
                "       Current               1     Dry     0   2      20\r\n" +
                "       Current               2     Dry    10   1      30\r\n" +
                "       Current               2     Dry    10   2      40\r\n" +
                "       Current               3     Wet     0   1      50\r\n" +
                "       Current               3     Wet     0   2      60\r\n" +
                "       Current               4     Wet    10   1      70\r\n" +
                "       Current               4     Wet    10   2      80\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var series = graph.Children[0] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("Irr"));
            Assert.That(descriptors[1], Is.EqualTo("Fert"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(4));
            foreach (var definition in definitions[0].SeriesDefinitions)
            {
                Assert.That(definition.XFieldName, Is.EqualTo("Col1"));
                Assert.That(definition.YFieldName, Is.EqualTo("Col2"));
                Assert.That(definition.YError, Is.Null);
                Assert.That(definition.LineThickness, Is.EqualTo(LineThickness.Normal));
                Assert.That(definition.Marker, Is.EqualTo(MarkerType.FilledCircle));
                Assert.That(definition.MarkerSize, Is.EqualTo(MarkerSize.Normal));
                Assert.That(definition.ShowInLegend, Is.False);
                Assert.That(definition.Type, Is.EqualTo(SeriesType.Bar));
                Assert.That(definition.X2, Is.Null);
                Assert.That(definition.Y2, Is.Null);
                Assert.That(definition.XAxis, Is.EqualTo(AxisPosition.Bottom));
                Assert.That(definition.YAxis, Is.EqualTo(AxisPosition.Left));
                Assert.That(definition.XFieldUnits, Is.EqualTo("()"));
                Assert.That(definition.YFieldUnits, Is.EqualTo("(g)"));
            }

            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Dry0"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[1].Line, Is.EqualTo(LineType.Dash));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Dry10"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new int[] { 30, 40 }));

            Assert.That(definitions[0].SeriesDefinitions[2].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[2].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[2].Title, Is.EqualTo("Wet0"));
            Assert.That(definitions[0].SeriesDefinitions[2].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[2].Y as double[], Is.EqualTo(new int[] { 50, 60 }));

            Assert.That(definitions[0].SeriesDefinitions[3].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[3].Line, Is.EqualTo(LineType.Dash));
            Assert.That(definitions[0].SeriesDefinitions[3].Title, Is.EqualTo("Wet10"));
            Assert.That(definitions[0].SeriesDefinitions[3].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[3].Y as double[], Is.EqualTo(new int[] { 70, 80 }));   
        }

        /// <summary>Create six series definitions due to a three 'VaryBy' groupings.</summary>
        [Test]
        public void SeriesWithThreeVaryBy()
        {
            var data = "CheckpointName    SimulationID     Irr  Fert  Cultivar  Col1  Col2\r\n" +
                       "            ()              ()      ()    ()        ()    ()   (g)\r\n" +
                       "       Current               1     Dry     0     Early     1    10\r\n" +
                       "       Current               1     Dry     0     Early     2    20\r\n" +
                       "       Current               2     Dry    20     Early     1    30\r\n" +
                       "       Current               2     Dry    20     Early     2    40\r\n" +
                       "       Current               3     Wet     0     Early     1    50\r\n" +
                       "       Current               3     Wet     0     Early     2    60\r\n" +
                       "       Current               4     Wet    20     Early     1    70\r\n" +
                       "       Current               4     Wet    20     Early     2    80\r\n" +
                       "       Current               5     Dry     0      Late     1    90\r\n" +
                       "       Current               5     Dry     0      Late     2    100\r\n" +
                       "       Current               6     Dry    20      Late     1    110\r\n" +
                       "       Current               6     Dry    20      Late     2    120\r\n" +
                       "       Current               7     Wet     0      Late     1    130\r\n" +
                       "       Current               7     Wet     0      Late     2    140\r\n" +
                       "       Current               8     Wet    20      Late     1    150\r\n" +
                       "       Current               8     Wet    20      Late     2    160\r\n";
            var reader = new TextStorageReader(data);

            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Irr", "Dry", "Fert", "0", "Cultivar", "Early"),
                        new Description("Sim2", "Irr", "Dry", "Fert", "20", "Cultivar", "Early"),
                        new Description("Sim3", "Irr", "Wet", "Fert", "0", "Cultivar", "Early"),
                        new Description("Sim4", "Irr", "Wet", "Fert", "20", "Cultivar", "Early"),
                        new Description("Sim5", "Irr", "Dry", "Fert", "0", "Cultivar", "Late"),
                        new Description("Sim6", "Irr", "Dry", "Fert", "20", "Cultivar", "Late"),
                        new Description("Sim7", "Irr", "Wet", "Fert", "0", "Cultivar", "Late"),
                        new Description("Sim8", "Irr", "Wet", "Fert", "20", "Cultivar", "Late")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Irr",
                                FactorToVaryLines = "Fert",
                                FactorToVaryMarkers = "Cultivar"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            var graph = folder.Children[1] as Graph;
            var series = graph.Children[0] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("Irr"));
            Assert.That(descriptors[1], Is.EqualTo("Fert"));
            Assert.That(descriptors[2], Is.EqualTo("Cultivar"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(8));

            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Dry0Early"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[1].Line, Is.EqualTo(LineType.Dash));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Dry20Early"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new int[] { 30, 40 }));

            Assert.That(definitions[0].SeriesDefinitions[2].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[2].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[2].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[2].Title, Is.EqualTo("Wet0Early"));
            Assert.That(definitions[0].SeriesDefinitions[2].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[2].Y as double[], Is.EqualTo(new int[] { 50, 60 }));

            Assert.That(definitions[0].SeriesDefinitions[3].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[3].Line, Is.EqualTo(LineType.Dash));
            Assert.That(definitions[0].SeriesDefinitions[3].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[3].Title, Is.EqualTo("Wet20Early"));
            Assert.That(definitions[0].SeriesDefinitions[3].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[3].Y as double[], Is.EqualTo(new int[] { 70, 80 }));

            Assert.That(definitions[0].SeriesDefinitions[4].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[4].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[4].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[4].Title, Is.EqualTo("Dry0Late"));
            Assert.That(definitions[0].SeriesDefinitions[4].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[4].Y as double[], Is.EqualTo(new int[] { 90, 100 }));

            Assert.That(definitions[0].SeriesDefinitions[5].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[5].Line, Is.EqualTo(LineType.Dash));
            Assert.That(definitions[0].SeriesDefinitions[5].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[5].Title, Is.EqualTo("Dry20Late"));
            Assert.That(definitions[0].SeriesDefinitions[5].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[5].Y as double[], Is.EqualTo(new int[] { 110, 120 }));

            Assert.That(definitions[0].SeriesDefinitions[6].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[6].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[6].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[6].Title, Is.EqualTo("Wet0Late"));
            Assert.That(definitions[0].SeriesDefinitions[6].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[6].Y as double[], Is.EqualTo(new int[] { 130, 140 }));

            Assert.That(definitions[0].SeriesDefinitions[7].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[7].Line, Is.EqualTo(LineType.Dash));
            Assert.That(definitions[0].SeriesDefinitions[7].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[7].Title, Is.EqualTo("Wet20Late"));
            Assert.That(definitions[0].SeriesDefinitions[7].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[7].Y as double[], Is.EqualTo(new int[] { 150, 160 }));
        }

        /// <summary>
        /// Create series definitions where it works its way though the colours sequentially and 
        /// when it runs out of colours it works through the marker types. Useful when there
        /// are a lot of descriptor values.
        /// </summary>
        [Test]
        public void SeriesWithTwoIdenticalVaryBy()
        {
            var data = "CheckpointName    SimulationID   ABC   Col1  Col2\r\n" +
                       "            ()              ()    ()     ()   (g)\r\n" +
                       "       Current               1     A      1    10\r\n" +
                       "       Current               1     A      2    20\r\n" +
                       "       Current               2     B      1    30\r\n" +
                       "       Current               2     B      2    40\r\n" +
                       "       Current               3     C      1    50\r\n" +
                       "       Current               3     C      2    60\r\n" +
                       "       Current               4     D      1    70\r\n" +
                       "       Current               4     D      2    80\r\n" +
                       "       Current               5     E      1    90\r\n" +
                       "       Current               5     E      2    100\r\n" +
                       "       Current               6     F      1    110\r\n" +
                       "       Current               6     F      2    120\r\n" +
                       "       Current               7     G      1    130\r\n" +
                       "       Current               7     G      2    140\r\n" +
                       "       Current               8     H      1    150\r\n" +
                       "       Current               8     H      2    160\r\n" +
                       "       Current               9     I      1    170\r\n" +
                       "       Current               9     I      2    180\r\n" +
                       "       Current              10     J      1    190\r\n" +
                       "       Current              10     J      2    200\r\n" +
                       "       Current              11     K      1    210\r\n" +
                       "       Current              11     K      2    220\r\n" +
                       "       Current              12     L      1    230\r\n" +
                       "       Current              12     L      2    240\r\n";
            var reader = new TextStorageReader(data);

            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "ABC", "A"),
                        new Description("Sim2", "ABC", "B"),
                        new Description("Sim3", "ABC", "C"),
                        new Description("Sim4", "ABC", "D"),
                        new Description("Sim5", "ABC", "E"),
                        new Description("Sim6", "ABC", "F"),
                        new Description("Sim7", "ABC", "G"),
                        new Description("Sim8", "ABC", "H"),
                        new Description("Sim9", "ABC", "I"),
                        new Description("Sim10", "ABC", "J"),
                        new Description("Sim11", "ABC", "K"),
                        new Description("Sim12", "ABC", "L"),
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "ABC",
                                FactorToVaryLines = "ABC",
                                FactorToVaryMarkers = "ABC"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            var graph = folder.Children[1] as Graph;
            var series = graph.Children[0] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("ABC"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(12));

            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("A"));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[1].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("B"));

            Assert.That(definitions[0].SeriesDefinitions[2].Colour, Is.EqualTo(ColourUtilities.Colours[2]));
            Assert.That(definitions[0].SeriesDefinitions[2].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[2].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[2].Title, Is.EqualTo("C"));

            Assert.That(definitions[0].SeriesDefinitions[3].Colour, Is.EqualTo(ColourUtilities.Colours[3]));
            Assert.That(definitions[0].SeriesDefinitions[3].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[3].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[3].Title, Is.EqualTo("D"));

            Assert.That(definitions[0].SeriesDefinitions[4].Colour, Is.EqualTo(ColourUtilities.Colours[4]));
            Assert.That(definitions[0].SeriesDefinitions[4].Marker, Is.EqualTo(MarkerType.FilledCircle));   
            Assert.That(definitions[0].SeriesDefinitions[4].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[4].Title, Is.EqualTo("E"));

            Assert.That(definitions[0].SeriesDefinitions[5].Colour, Is.EqualTo(ColourUtilities.Colours[5]));
            Assert.That(definitions[0].SeriesDefinitions[5].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[5].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[5].Title, Is.EqualTo("F"));

            Assert.That(definitions[0].SeriesDefinitions[6].Colour, Is.EqualTo(ColourUtilities.Colours[6]));
            Assert.That(definitions[0].SeriesDefinitions[6].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[6].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[6].Title, Is.EqualTo("G"));

            Assert.That(definitions[0].SeriesDefinitions[7].Colour, Is.EqualTo(ColourUtilities.Colours[7]));
            Assert.That(definitions[0].SeriesDefinitions[7].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[7].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[7].Title, Is.EqualTo("H"));

            // Run out of colours, go back to first colour but increment markertype.

            Assert.That(definitions[0].SeriesDefinitions[8].Colour, Is.EqualTo(ColourUtilities.Colours[0])); 
            Assert.That(definitions[0].SeriesDefinitions[8].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[8].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[8].Title, Is.EqualTo("I"));

            Assert.That(definitions[0].SeriesDefinitions[9].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[9].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[9].Title, Is.EqualTo("J"));
            Assert.That(definitions[0].SeriesDefinitions[9].Line, Is.EqualTo(LineType.Solid));

            Assert.That(definitions[0].SeriesDefinitions[10].Colour, Is.EqualTo(ColourUtilities.Colours[2]));
            Assert.That(definitions[0].SeriesDefinitions[10].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[10].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[10].Title, Is.EqualTo("K"));

            Assert.That(definitions[0].SeriesDefinitions[11].Colour, Is.EqualTo(ColourUtilities.Colours[3]));
            Assert.That(definitions[0].SeriesDefinitions[11].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[11].Line, Is.EqualTo(LineType.Solid));
            Assert.That(definitions[0].SeriesDefinitions[11].Title, Is.EqualTo("L"));

        }

        /// <summary>Create a xy series definitions with a regression annotation.</summary>
        [Test]
        public void SeriesWithChildRegressionAnnotation()
        {
            var sim = new Simulation()
            {
                Name = "Sim1",
                Children = new List<IModel>()
                {
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",

                                Children = new List<IModel>()
                                {
                                    new Regression()
                                    {
                                        showEquation = true,
                                        showOneToOne = true
                                    }
                                }
                            }
                        }
                    }
                }
            };
            sim.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID  Col1  Col2\r\n" +
                "            ()              ()    ()   (g)\r\n" +
                "       Current               1     1    1.0\r\n" +
                "       Current               1     2    1.5\r\n" +
                "       Current               1     3    2.0\r\n" +
                "       Current               1     4    2.5\r\n";

            var reader = new TextStorageReader(data);

            var graph = sim.Children[0] as Graph;
            var series = graph.Children[0] as Series;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            // There should be 3 series - the regression line, the 1:1 line, and the
            // data series itself.
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(3));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Series"));
            Assert.That(definitions[0].SeriesDefinitions[0].Type, Is.EqualTo(SeriesType.Bar));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2, 3, 4 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 1.0, 1.5, 2.0, 2.5 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Regression line"));
            Assert.That(definitions[0].SeriesDefinitions[1].Type, Is.EqualTo(SeriesType.Scatter));
            Assert.That(definitions[0].SeriesDefinitions[1].LineThickness, Is.EqualTo(LineThickness.Normal));
            Assert.That(definitions[0].SeriesDefinitions[1].MarkerSize, Is.EqualTo(MarkerSize.Normal));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 4 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 1, 2.5 }));

            Assert.That(definitions[0].SeriesDefinitions[2].Title, Is.EqualTo("1:1 line"));
            Assert.That(definitions[0].SeriesDefinitions[2].Type, Is.EqualTo(SeriesType.Scatter));
            Assert.That(definitions[0].SeriesDefinitions[2].LineThickness, Is.EqualTo(LineThickness.Normal));
            Assert.That(definitions[0].SeriesDefinitions[2].MarkerSize, Is.EqualTo(MarkerSize.Normal));
            Assert.That(definitions[0].SeriesDefinitions[2].X as double[], Is.EqualTo(new double[] { 1, 4 }));
            Assert.That(definitions[0].SeriesDefinitions[2].Y as double[], Is.EqualTo(new double[] { 1, 4 }));

        }

        /// <summary>Create series definitions with 'Vary by graph series'.</summary>
        [Test]
        public void SeriesWithVaryByGraphSeries()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "Exp", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Graph series"
                            },
                            new Series()
                            {
                                Name = "Series2",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col3",
                                FactorToVaryColours = "Graph series"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID     Exp Col1  Col2  Col3\r\n" +
                "            ()              ()      ()   ()   (g)    ()\r\n" +
                "       Current               1    Exp1    1    10    50\r\n" +
                "       Current               1    Exp1    1    10    50\r\n" +
                "       Current               2    Exp2    2    20    60\r\n" +
                "       Current               2    Exp2    2    20    60\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Series1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 1, 2, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 10, 20, 20 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Series2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 1, 2, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 50, 50, 60, 60 }));
        }

        /// <summary>Create a single xy series definition with a 'Vary By Simulation' grouping.</summary>
        [Test]
        public void SeriesWithVaryBySimulation()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Exp", "Exp2"),
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "SimulationName"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID    Exp Col1  Col2\r\n" +
                "            ()              ()     ()   ()   (g)\r\n" +
                "       Current               1   Exp1    1    10\r\n" +
                "       Current               1   Exp1    2    20\r\n" +
                "       Current               2   Exp2    1    30\r\n" +
                "       Current               2   Exp2    2    40\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Sim1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Sim2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 30, 40 }));

        }

        /// <summary>Create xy series definitions with a 'Vary By Zone' grouping.</summary>
        [Test]
        public void SeriesWithVaryByZone()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Zone", "Zone1", "Zone", "Zone2"),
                        new Description("Sim2", "SimulationName", "Sim2", "Zone", "Zone1", "Zone", "Zone2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "SimulationName",
                                FactorToVaryMarkers = "Zone"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID   Zone Col1  Col2\r\n" +
                "            ()              ()     ()   ()   (g)\r\n" +
                "       Current               1  Zone1    1    10\r\n" +
                "       Current               1  Zone1    2    20\r\n" +
                "       Current               1  Zone2    1    30\r\n" +
                "       Current               1  Zone2    2    40\r\n" +
                "       Current               2  Zone1    1    50\r\n" +
                "       Current               2  Zone1    2    60\r\n" +
                "       Current               2  Zone2    1    70\r\n" +
                "       Current               2  Zone2    2    80\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(4));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Sim1Zone1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Sim1Zone2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 30, 40 }));

            Assert.That(definitions[0].SeriesDefinitions[2].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[2].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[2].Title, Is.EqualTo("Sim2Zone1"));
            Assert.That(definitions[0].SeriesDefinitions[2].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[2].Y as double[], Is.EqualTo(new double[] { 50, 60 }));

            Assert.That(definitions[0].SeriesDefinitions[3].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[3].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[3].Title, Is.EqualTo("Sim2Zone2"));
            Assert.That(definitions[0].SeriesDefinitions[3].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[3].Y as double[], Is.EqualTo(new double[] { 70, 80 }));
        }

        /// <summary>Create a xy series definition with a 'Vary By' that doesn't exist in the data table.</summary>
        [Test]
        public void SeriesWithVaryByThatDoesntExist()
        {
            // Observed files don't have all the descriptor columns. For them, series will need
            // to resort to using simulation names to find the data.
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Exp", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Observed",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Exp"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName   SimulationID Col1  Col2\r\n" +
                "            ()             ()   ()   (g)\r\n" +
                "       Current              1    1    10\r\n" +
                "       Current              1    2    20\r\n" +
                "       Current              2    1    30\r\n" +
                "       Current              2    2    40\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Exp1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Exp2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 30, 40 }));
        }

        /// <summary>Create a xy series definition with a 'Vary By' that is a text field of the data table.</summary>
        [Test]
        public void SeriesWithVaryByTextField()
        {
            // Observed files don't have all the descriptor columns. For them, series will need
            // to resort to using simulation names to find the data.
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1"),
                        new Description("Sim2", "SimulationName", "Sim2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Observed",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "ABC",
                                FactorToVaryMarkers = "DEF"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                " CheckpointName ABC  DEF Col1  Col2\r\n" +
                "             ()  ()   ()   ()   (g)\r\n" +
                "        Current   A    d    1    10\r\n" +
                "        Current   A    d    2    20\r\n" +
                "        Current   A    e    1    30\r\n" +
                "        Current   A    e    2    40\r\n" +
                "        Current   B    d    1    50\r\n" +
                "        Current   B    d    2    60\r\n" +
                "        Current   B    e    1    70\r\n" +
                "        Current   B    e    2    80\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var series1 = graph.Children[0] as Series;

            var descriptorNames = series1.GetDescriptorNames(reader).ToArray();
            //Assert.AreEqual(descriptorNames, new string[] { "SimulationName", "Graph series", "ABC" });

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(4));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Ad"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Ae"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 30, 40 }));

            Assert.That(definitions[0].SeriesDefinitions[2].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[2].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[2].Title, Is.EqualTo("Bd"));
            Assert.That(definitions[0].SeriesDefinitions[2].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[2].Y as double[], Is.EqualTo(new double[] { 50, 60 }));
                
            Assert.That(definitions[0].SeriesDefinitions[3].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[3].Marker, Is.EqualTo(MarkerType.FilledDiamond));
            Assert.That(definitions[0].SeriesDefinitions[3].Title, Is.EqualTo("Be"));
            Assert.That(definitions[0].SeriesDefinitions[3].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[3].Y as double[], Is.EqualTo(new double[] { 70, 80 }));
        }

        /// <summary>Create xy series definitions with a filter.</summary>
        [Test]
        public void SeriesWithFilter()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "Exp", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Exp",
                                Filter = "A='a'"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID     Exp   A  Col1  Col2\r\n" +
                "            ()              ()      ()  ()    ()   (g)\r\n" +
                "       Current               1    Exp1   a     1    10\r\n" +
                "       Current               1    Exp1   a     1    10\r\n" +
                "       Current               2    Exp2   b     2    20\r\n" +
                "       Current               2    Exp2   b     2    20\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var series = graph.Children[0] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("Exp"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));

            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 1 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 10 }));
        }

        /// <summary>Create xy series definitions with a filter.</summary>
        [Test]
        public void SeriesWithFilter2()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1"),
                        new Description("Sim2", "SimulationName", "Sim2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "SimulationName",
                                Filter = "A='a'"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID     Exp   A  Col1  Col2\r\n" +
                "            ()              ()      ()  ()    ()   (g)\r\n" +
                "       Current               1    Exp1   a     1    10\r\n" +
                "       Current               1    Exp1   a     1    10\r\n" +
                "       Current               2    Exp2   b     2    20\r\n" +
                "       Current               2    Exp2   b     2    20\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));

            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 1 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 10 }));
        }

        /// <summary>Create xy series definitions with a filter.</summary>
        [Test]
        public void SeriesWithFilterWildcards()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
            {
                new MockSimulationDescriptionGenerator(new List<Description>()
                {
                    new Description("Sim1", "Exp", "Exp1"),
                    new Description("Sim2", "Exp", "Exp2")
                }),
                new Graph()
                {
                    Children = new List<IModel>()
                    {
                        new Series()
                        {
                            Name = "Series",
                            TableName = "Report",
                            XFieldName = "Col1",
                            YFieldName = "Col2",
                            FactorToVaryColours = "Exp",
                            Filter = "A LIKE 'a%a'"
                        }
                    }
                }
            }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID     Exp   A  Col1  Col2\r\n" +
                "            ()              ()      ()  ()    ()   (g)\r\n" +
                "       Current               1    Exp1   a1a     1    10\r\n" +
                "       Current               1    Exp1   a1a     1    10\r\n" +
                "       Current               2    Exp2   b1b     2    20\r\n" +
                "       Current               2    Exp2   b1b     2    20\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var series = graph.Children[0] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("Exp"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));

            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 1 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new int[] { 10, 10 }));
        }


        /// <summary>
        /// Create a single xy series definition with a 'Vary By Simulation'.
        /// Ensure it only pulls in simulations in scope.
        /// </summary>
        [Test]
        public void SeriesWithVaryBySimulationUsingScope()
        {
            var simulations = new Simulations()
            {
                Name = "Simulations",
                Children = new List<IModel>()
                {
                    new Folder()
                    {
                        Name = "Folder1",
                        Children = new List<IModel>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim1", "SimulationName", "Sim1", "Exp", "Exp1"),
                                new Description("Sim2", "SimulationName", "Sim2", "Exp", "Exp2")
                            }),
                            new Graph()
                            {
                                Children = new List<IModel>()
                                {
                                    new Series()
                                    {
                                        Name = "Series1",
                                        TableName = "Report",
                                        XFieldName = "Col1",
                                        YFieldName = "Col2",
                                        FactorToVaryColours = "SimulationName"
                                    }
                                }
                            }
                        }
                    },
                    new Folder()
                    {
                        Name = "Folder2",
                        Children = new List<IModel>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim3", "SimulationName", "Sim3", "Exp", "Exp3"),
                                new Description("Sim4", "SimulationName", "Sim4", "Exp", "Exp4")
                            }),
                        }
                    }
                }
            };


            simulations.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID    Exp Col1  Col2\r\n" +
                "            ()              ()     ()   ()   (g)\r\n" +
                "       Current               1   Exp1    1    10\r\n" +
                "       Current               1   Exp1    2    20\r\n" +
                "       Current               2   Exp2    1    30\r\n" +
                "       Current               2   Exp2    2    40\r\n" +
                "       Current               3   Exp3    1    50\r\n" +
                "       Current               3   Exp3    2    60\r\n" +
                "       Current               4   Exp4    1    70\r\n" +
                "       Current               4   Exp4    2    80\r\n";

            var reader = new TextStorageReader(data);

            var graph = simulations.Children[0].Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Sim1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Sim2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 30, 40 }));

        }

        /// <summary>
        /// Create a single xy series definition with no vary by.
        /// Ensure it only pulls in experiments in scope.
        /// </summary>
        [Test]
        public void SeriesWithNoVaryByUsingScope()
        {
            var simulations = new Simulations()
            {
                Name = "Simulations",
                Children = new List<IModel>()
                {
                    new Folder()
                    {
                        Name = "Folder1",
                        Children = new List<IModel>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim1", "SimulationName", "Sim1"),
                            }),
                            new Graph()
                            {
                                Children = new List<IModel>()
                                {
                                    new Series()
                                    {
                                        Name = "Series1",
                                        TableName = "Report",
                                        XFieldName = "Col1",
                                        YFieldName = "Col2",
                                    }
                                }
                            }
                        }
                    },
                    new Folder()
                    {
                        Name = "Folder2",
                        Children = new List<IModel>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim2", "SimulationName", "Sim2"),
                            }),
                        }
                    }
                }
            };


            simulations.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID Col1  Col2\r\n" +
                "            ()              ()   ()   (g)\r\n" +
                "       Current               1    1    10\r\n" +
                "       Current               1    2    20\r\n" +
                "       Current               2    1    30\r\n" +
                "       Current               2    2    40\r\n";

            var reader = new TextStorageReader(data);

            var graph = simulations.Children[0].Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Series1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));
        }

        /// <summary>
        /// Create a single xy series definition with no vary by.
        /// Ensure it only pulls in experiments in scope.
        /// </summary>
        [Test]
        public void SeriesWithNoSimulationName()
        {
            var simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1"),
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "s0",
                                TableName = "Observed",
                                XFieldName = "x",
                                YFieldName = "y",
                            },
                            new Series()
                            {
                                Name = "s1",
                                TableName = "Report",
                                XFieldName = "x",
                                YFieldName = "y",
                            }
                        }
                    }
                }
            };

            simulations.ParentAllDescendants();

            List<string> checkpoints = new List<string>() { "Current" };

            DataTable report = new DataTable("Report");
            report.Columns.Add("SimulationName", typeof(string));
            report.Columns.Add("x", typeof(double));
            report.Columns.Add("y", typeof(double));
            report.Rows.Add("Sim1", 0, 1);
            report.Rows.Add("Sim1", 1, 3);

            DataTable obs = new DataTable("Observed");
            obs.Columns.Add("x", typeof(double));
            obs.Columns.Add("y", typeof(double));
            obs.Rows.Add(0, 1);
            obs.Rows.Add(1, 2);

            IStorageReader reader = new MockStorageReader(report, obs);

            Graph graph = simulations.FindDescendant<Graph>();
            GraphPage page = new GraphPage();
            page.Graphs.Add(graph);
            List<GraphPage.GraphDefinitionMap> definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Create a single xy series definition with a 'Vary By Experiment'.
        /// Ensure it only pulls in experiments in scope.
        /// </summary>
        [Test]
        public void SeriesWithVaryByFactorUsingScope()
        {
            var simulations = new Simulations()
            {
                Name = "Simulations",
                Children = new List<IModel>()
                {
                    new Folder()
                    {
                        Name = "Folder1",
                        Children = new List<IModel>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim1", "SimulationName", "Sim1", "A", "a"),
                            }),
                            new Graph()
                            {
                                Children = new List<IModel>()
                                {
                                    new Series()
                                    {
                                        Name = "Series1",
                                        TableName = "Report",
                                        XFieldName = "Col1",
                                        YFieldName = "Col2",
                                        FactorToVaryColours = "A"
                                    }
                                }
                            }
                        }
                    },
                    new Folder()
                    {
                        Name = "Folder2",
                        Children = new List<IModel>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim2", "SimulationName", "Sim2", "A", "b"),
                            }),
                        }
                    }
                }
            };

            simulations.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID   A  Col1  Col2\r\n" +
                "            ()              ()  ()    ()   (g)\r\n" +
                "       Current               1   a     1    10\r\n" +
                "       Current               1   a     2    20\r\n" +
                "       Current               2   a     1    30\r\n" +
                "       Current               2   a     2    40\r\n";

            var reader = new TextStorageReader(data);

            var graph = simulations.Children[0].Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("a"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 10, 20 }));
        }

        /// <summary>Create xy series definitions from predicted/observed table.</summary>
        [Test]
        public void SeriesFromPredictedObserved()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Experiment", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Experiment", "Exp1"),
                        new Description("Sim3", "SimulationName", "Sim3", "Experiment", "Exp2"),
                        new Description("Sim4", "SimulationName", "Sim4", "Experiment", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Predicted.Grain.Wt",
                                YFieldName = "Observed.Grain.Wt",
                                FactorToVaryColours = "Experiment",
                                FactorToVaryMarkers = "Experiment"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID Predicted.Grain.Wt  Observed.Grain.Wt\r\n" +
                "            ()              ()                 ()                 ()\r\n" +
                "       Current               1                  1                  1\r\n" +
                "       Current               2                  2                  5\r\n" +
                "       Current               3                  3                  8\r\n" +
                "       Current               4                  4                  6\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Exp1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 1, 5 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Exp2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 3, 4 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 8, 6 }));
        }

        /// <summary>Create xy series definitions from predicted/observed table.</summary>
        [Test]
        public void SeriesFromPredictedObservedAndMixOfSimulations()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Experiment", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Experiment", "Exp1"),
                        new Description("Sim3", "SimulationName", "Sim3")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Predicted.Grain.Wt",
                                YFieldName = "Observed.Grain.Wt",
                                FactorToVaryColours = "Experiment",
                                FactorToVaryMarkers = "Experiment"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID Predicted.Grain.Wt  Observed.Grain.Wt\r\n" +
                "            ()              ()                 ()                 ()\r\n" +
                "       Current               1                  1                  1\r\n" +
                "       Current               2                  2                  5\r\n" +
                "       Current               3                  3                  8\r\n" +
                "       Current               3                  4                  6\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph,reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Exp1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 1, 5 }));
        }

        /// <summary>Create xy series definitions from predicted/observed table.</summary>
        [Test]
        public void SeriesFromPredictedObservedWithMissingObserved()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Experiment", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Experiment", "Exp1"),
                        new Description("Sim3", "SimulationName", "Sim3", "Experiment", "Exp2"),
                        new Description("Sim4", "SimulationName", "Sim4", "Experiment", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Predicted.Grain.Wt",
                                YFieldName = "Observed.Grain.Wt",
                                FactorToVaryColours = "Experiment",
                                FactorToVaryMarkers = "Experiment"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName SimulationName SimulationID Predicted.Grain.Wt  Observed.Grain.Wt Experiment\r\n" +
                "            ()             ()           ()                 ()                 ()         ()\r\n" +
                "       Current           Sim1            1                  1                  1       Exp1\r\n" +
                "       Current           Sim2            2                  2                  5       null\r\n" +
                "       Current           Sim3            3                  3                  8       Exp2\r\n" +
                "       Current           Sim4            4                  4                  6       Exp2\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Exp1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 1, 5 }));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Exp2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 3, 4 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 8, 6 }));
        }

        /// <summary>Create xy series definitions from predicted/observed table with error bars.</summary>
        [Test]
        public void SeriesWithErrorBars()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Experiment", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Experiment", "Exp1"),
                        new Description("Sim3", "SimulationName", "Sim3", "Experiment", "Exp2"),
                        new Description("Sim4", "SimulationName", "Sim4", "Experiment", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series1",
                                TableName = "Report",
                                XFieldName = "Predicted.Grain.Wt",
                                YFieldName = "Observed.Grain.Wt",
                                FactorToVaryColours = "Experiment",
                                FactorToVaryMarkers = "Experiment"
                            }
                        }
                    }
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID Predicted.Grain.Wt  Observed.Grain.Wt  Observed.Grain.WtError\r\n" +
                "            ()              ()                 ()                 ()                      ()\r\n" +
                "       Current               1                  1                  1                     0.1\r\n" +
                "       Current               2                  2                  5                     0.5\r\n" +
                "       Current               3                  3                  8                     0.8\r\n" +
                "       Current               4                  4                  6                     0.6\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[1] as Graph;
            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);
            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].SeriesDefinitions[0].Colour, Is.EqualTo(ColourUtilities.Colours[0]));
            Assert.That(definitions[0].SeriesDefinitions[0].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[0].Title, Is.EqualTo("Exp1"));
            Assert.That(definitions[0].SeriesDefinitions[0].X as double[], Is.EqualTo(new double[] { 1, 2 }));
            Assert.That(definitions[0].SeriesDefinitions[0].Y as double[], Is.EqualTo(new double[] { 1, 5 }));
            Assert.That(definitions[0].SeriesDefinitions[0].YError.ToList()[0], Is.EqualTo(0.1).Within(0.000001));
            Assert.That(definitions[0].SeriesDefinitions[0].YError.ToList()[1], Is.EqualTo(0.5).Within(0.000001));

            Assert.That(definitions[0].SeriesDefinitions[1].Colour, Is.EqualTo(ColourUtilities.Colours[1]));
            Assert.That(definitions[0].SeriesDefinitions[1].Marker, Is.EqualTo(MarkerType.FilledCircle));
            Assert.That(definitions[0].SeriesDefinitions[1].Title, Is.EqualTo("Exp2"));
            Assert.That(definitions[0].SeriesDefinitions[1].X as double[], Is.EqualTo(new double[] { 3, 4 }));
            Assert.That(definitions[0].SeriesDefinitions[1].Y as double[], Is.EqualTo(new double[] { 8, 6 }));
            Assert.That(definitions[0].SeriesDefinitions[1].YError.ToList()[0], Is.EqualTo(0.8).Within(0.000001));
            Assert.That(definitions[0].SeriesDefinitions[1].YError.ToList()[1], Is.EqualTo(0.6).Within(0.000001));
        }

        /// <summary>Ensure a series definition that has no data doesn't throw exception.</summary>
        [Test]
        public void SeriesWithMissingData()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<IModel>()
                {
                    new Graph()
                    {
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "Series",
                                TableName = "Report",
                                XFieldName = "Col1",
                                YFieldName = "Col2",
                                FactorToVaryColours = "Exp"
                            }
                        }
                    },
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim3", "Exp", "Exp1")
                    })
                }
            };
            folder.ParentAllDescendants();

            string data =
                "CheckpointName    SimulationID   Exp Col1  Col2\r\n" +
                "            ()              ()    ()   ()   (g)\r\n" +
                "       Current               1  Exp1    1    10\r\n" +
                "       Current               1  Exp1    1    10\r\n" +
                "       Current               2  Exp2    2    20\r\n" +
                "       Current               2  Exp2    2    20\r\n";

            var reader = new TextStorageReader(data);

            var graph = folder.Children[0] as Graph;
            var series = graph.Children[0] as Series;

            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.That(descriptors[0], Is.EqualTo("Exp"));

            var page = new GraphPage();
            page.Graphs.Add(graph);
            var definitions = page.GetAllSeriesDefinitions(graph, reader, null);

            Assert.That(definitions.Count, Is.EqualTo(1));
            Assert.That(definitions[0].SeriesDefinitions.Count, Is.EqualTo(0));
        }


        /// <summary>Create some test data and return a storage reader. </summary>
        private static IStorageReader CreateTestData()
        {
            var data = "CheckpointName     Irr  Fert  Cultivar  Col1  Col2\r\n" +
                "            ()      ()    ()        ()    ()   (g)\r\n" +
                "       Current     Dry     0     Early     1    10\r\n" +
                "       Current     Dry     0     Early     2    20\r\n" +
                "       Current     Dry    20     Early     1    30\r\n" +
                "       Current     Dry    20     Early     2    40\r\n" +
                "       Current     Dry    40     Early     1    50\r\n" +
                "       Current     Dry    40     Early     2    60\r\n" +
                "       Current     Wet     0     Early     1    70\r\n" +
                "       Current     Wet     0     Early     2    80\r\n" +
                "       Current     Wet    20     Early     1    90\r\n" +
                "       Current     Wet    20     Early     2    100\r\n" +
                "       Current     Wet    40     Early     1    110\r\n" +
                "       Current     Wet    40     Early     2    120\r\n" +
                "       Current     Dry     0      Late     1    130\r\n" +
                "       Current     Dry     0      Late     2    140\r\n" +
                "       Current     Dry    20      Late     1    150\r\n" +
                "       Current     Dry    20      Late     2    160\r\n" +
                "       Current     Dry    40      Late     1    170\r\n" +
                "       Current     Dry    40      Late     2    180\r\n" +
                "       Current     Wet     0      Late     1    190\r\n" +
                "       Current     Wet     0      Late     2    200\r\n" +
                "       Current     Wet    20      Late     1    210\r\n" +
                "       Current     Wet    20      Late     2    220\r\n" +
                "       Current     Wet    40      Late     1    230\r\n" +
                "       Current     Wet    40      Late     2    240\r\n";
            return new TextStorageReader(data);
        }
    }
}
