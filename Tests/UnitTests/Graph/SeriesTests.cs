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
                Children = new List<Model>()
                {
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2"
                    },
                }
            };
            Apsim.ParentAllChildren(sim);

            string data =
                "CheckpointName  SimulationName  Col1  Col2\r\n" +
                "            ()              ()    ()   (g)\r\n" +
                "       Current            Sim1     1    10\r\n" +
                "       Current            Sim1     1    10\r\n" +
                "       Current            Sim1     2    20\r\n" +
                "       Current            Sim1     2    20\r\n";

            var reader = new TextStorageReader(data);

            var series = sim.Children[0] as Series;
            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 1);
            Assert.AreEqual(definitions[0].XFieldName, "Col1");
            Assert.AreEqual(definitions[0].YFieldName, "Col2");
            Assert.AreEqual(definitions[0].Colour, series.Colour);
            Assert.IsNull(definitions[0].Error);
            Assert.AreEqual(definitions[0].Line, LineType.Solid);
            Assert.AreEqual(definitions[0].LineThickness, LineThicknessType.Normal);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].MarkerSize, MarkerSizeType.Normal);
            Assert.AreEqual(definitions[0].ShowInLegend, false);
            Assert.AreEqual(definitions[0].Title, "Series");
            Assert.AreEqual(definitions[0].Type, SeriesType.Bar);
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 1, 2, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new int[] { 10, 10, 20, 20 });
            Assert.IsNull(definitions[0].X2);
            Assert.IsNull(definitions[0].Y2);
            Assert.AreEqual(definitions[0].XAxis, Axis.AxisType.Bottom);
            Assert.AreEqual(definitions[0].YAxis, Axis.AxisType.Left);
            Assert.AreEqual(definitions[0].XFieldUnits, "()");
            Assert.AreEqual(definitions[0].YFieldUnits, "(g)");
        }

        /// <summary>Create two series definitions due to a single 'VaryBy' grouping.</summary>
        [Test]
        public void SeriesWithOneVaryBy()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "Exp", "Exp2")
                    }),
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2",
                        FactorToVaryColours = "Exp"
                    },
                }
            };
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName   Exp Col1  Col2\r\n" +
                "            ()              ()    ()   ()   (g)\r\n" +
                "       Current            Sim1  Exp1    1    10\r\n" +
                "       Current            Sim1  Exp1    1    10\r\n" +
                "       Current            Sim2  Exp2    2    20\r\n" +
                "       Current            Sim2  Exp2    2    20\r\n";

            var reader = new TextStorageReader(data);

            var series = folder.Children[1] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.AreEqual(descriptors[0], "Exp");

            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 2);
            foreach (var definition in definitions)
            {
                Assert.AreEqual(definitions[0].XFieldName, "Col1");
                Assert.AreEqual(definitions[0].YFieldName, "Col2");
                Assert.IsNull(definitions[0].Error);
                Assert.AreEqual(definitions[0].Line, LineType.Solid);
                Assert.AreEqual(definitions[0].LineThickness, LineThicknessType.Normal);
                Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
                Assert.AreEqual(definitions[0].MarkerSize, MarkerSizeType.Normal);
                Assert.AreEqual(definitions[0].ShowInLegend, false);
                Assert.AreEqual(definitions[0].Type, SeriesType.Bar);
                Assert.IsNull(definitions[0].X2);
                Assert.IsNull(definitions[0].Y2);
                Assert.AreEqual(definitions[0].XAxis, Axis.AxisType.Bottom);
                Assert.AreEqual(definitions[0].YAxis, Axis.AxisType.Left);
                Assert.AreEqual(definitions[0].XFieldUnits, "()");
                Assert.AreEqual(definitions[0].YFieldUnits, "(g)");
            }

            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Title, "Exp1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 1 });
            Assert.AreEqual(definitions[0].Y as double[], new int[] { 10, 10 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Title, "Exp2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 2, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new int[] { 20, 20 });
        }

        /// <summary>Create four series definitions due to a two 'VaryBy' groupings.</summary>
        [Test]
        public void SeriesWithTwoVaryBy()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Irr", "Dry", "Fert", "0"),
                        new Description("Sim2", "Irr", "Dry", "Fert", "10"),
                        new Description("Sim3", "Irr", "Wet", "Fert", "0"),
                        new Description("Sim4", "Irr", "Wet", "Fert", "10")
                    }),
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2",
                        FactorToVaryColours = "Irr",
                        FactorToVaryLines = "Fert"
                    },
                }
            };
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName     Irr  Fert   Col1  Col2\r\n" +
                "            ()              ()      ()    ()   ()   (g)\r\n" +
                "       Current            Sim1     Dry     0   1    10\r\n" +
                "       Current            Sim1     Dry     0   2    20\r\n" +
                "       Current            Sim2     Dry    10   1    30\r\n" +
                "       Current            Sim2     Dry    10   2    40\r\n" +
                "       Current            Sim3     Wet     0   1    50\r\n" +
                "       Current            Sim3     Wet     0   2    60\r\n" +
                "       Current            Sim4     Wet    10   1    70\r\n" +
                "       Current            Sim4     Wet    10   2    80\r\n";

            var reader = new TextStorageReader(data);

            var series = folder.Children[1] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.AreEqual(descriptors[0], "Irr");
            Assert.AreEqual(descriptors[1], "Fert");

            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 4);
            foreach (var definition in definitions)
            {
                Assert.AreEqual(definitions[0].XFieldName, "Col1");
                Assert.AreEqual(definitions[0].YFieldName, "Col2");
                Assert.IsNull(definitions[0].Error);
                Assert.AreEqual(definitions[0].LineThickness, LineThicknessType.Normal);
                Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
                Assert.AreEqual(definitions[0].MarkerSize, MarkerSizeType.Normal);
                Assert.AreEqual(definitions[0].ShowInLegend, false);
                Assert.AreEqual(definitions[0].Type, SeriesType.Bar);
                Assert.IsNull(definitions[0].X2);
                Assert.IsNull(definitions[0].Y2);
                Assert.AreEqual(definitions[0].XAxis, Axis.AxisType.Bottom);
                Assert.AreEqual(definitions[0].YAxis, Axis.AxisType.Left);
                Assert.AreEqual(definitions[0].XFieldUnits, "()");
                Assert.AreEqual(definitions[0].YFieldUnits, "(g)");
            }

            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Line, LineType.Solid);
            Assert.AreEqual(definitions[0].Title, "Dry0");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new int[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[1].Line, LineType.Dash);
            Assert.AreEqual(definitions[1].Title, "Dry10");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new int[] { 30, 40 });

            Assert.AreEqual(definitions[2].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[2].Line, LineType.Solid);
            Assert.AreEqual(definitions[2].Title, "Wet0");
            Assert.AreEqual(definitions[2].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[2].Y as double[], new int[] { 50, 60 });

            Assert.AreEqual(definitions[3].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[3].Line, LineType.Dash);
            Assert.AreEqual(definitions[3].Title, "Wet10");
            Assert.AreEqual(definitions[3].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[3].Y as double[], new int[] { 70, 80 });

        }

        /// <summary>Create six series definitions due to a three 'VaryBy' groupings.</summary>
        [Test]
        public void SeriesWithThreeVaryBy()
        {
            var data = "CheckpointName  SimulationName     Irr  Fert  Cultivar  Col1  Col2\r\n" +
                       "            ()              ()      ()    ()        ()    ()   (g)\r\n" +
                       "       Current            Sim1     Dry     0     Early     1    10\r\n" +
                       "       Current            Sim1     Dry     0     Early     2    20\r\n" +
                       "       Current            Sim2     Dry    20     Early     1    30\r\n" +
                       "       Current            Sim2     Dry    20     Early     2    40\r\n" +
                       "       Current            Sim3     Wet     0     Early     1    50\r\n" +
                       "       Current            Sim3     Wet     0     Early     2    60\r\n" +
                       "       Current            Sim4     Wet    20     Early     1    70\r\n" +
                       "       Current            Sim4     Wet    20     Early     2    80\r\n" +
                       "       Current            Sim5     Dry     0      Late     1    90\r\n" +
                       "       Current            Sim5     Dry     0      Late     2    100\r\n" +
                       "       Current            Sim6     Dry    20      Late     1    110\r\n" +
                       "       Current            Sim6     Dry    20      Late     2    120\r\n" +
                       "       Current            Sim7     Wet     0      Late     1    130\r\n" +
                       "       Current            Sim7     Wet     0      Late     2    140\r\n" +
                       "       Current            Sim8     Wet    20      Late     1    150\r\n" +
                       "       Current            Sim8     Wet    20      Late     2    160\r\n";
            var reader = new TextStorageReader(data);

            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
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
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2",
                        FactorToVaryColours = "Irr",
                        FactorToVaryLines = "Fert",
                        FactorToVaryMarkers = "Cultivar"
                    },
                }
            };
            Apsim.ParentAllChildren(folder);

            var series = folder.Children[1] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.AreEqual(descriptors[0], "Irr");
            Assert.AreEqual(descriptors[1], "Fert");
            Assert.AreEqual(descriptors[2], "Cultivar");

            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 8);

            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Line, LineType.Solid);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Title, "Dry0Early");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new int[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[1].Line, LineType.Dash);
            Assert.AreEqual(definitions[1].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[1].Title, "Dry20Early");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new int[] { 30, 40 });

            Assert.AreEqual(definitions[2].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[2].Line, LineType.Solid);
            Assert.AreEqual(definitions[2].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[2].Title, "Wet0Early");
            Assert.AreEqual(definitions[2].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[2].Y as double[], new int[] { 50, 60 });

            Assert.AreEqual(definitions[3].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[3].Line, LineType.Dash);
            Assert.AreEqual(definitions[3].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[3].Title, "Wet20Early");
            Assert.AreEqual(definitions[3].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[3].Y as double[], new int[] { 70, 80 });

            Assert.AreEqual(definitions[4].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[4].Line, LineType.Solid);
            Assert.AreEqual(definitions[4].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[4].Title, "Dry0Late");
            Assert.AreEqual(definitions[4].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[4].Y as double[], new int[] { 90, 100 });

            Assert.AreEqual(definitions[5].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[5].Line, LineType.Dash);
            Assert.AreEqual(definitions[5].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[5].Title, "Dry20Late");
            Assert.AreEqual(definitions[5].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[5].Y as double[], new int[] { 110, 120 });

            Assert.AreEqual(definitions[6].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[6].Line, LineType.Solid);
            Assert.AreEqual(definitions[6].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[6].Title, "Wet0Late");
            Assert.AreEqual(definitions[6].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[6].Y as double[], new int[] { 130, 140 });

            Assert.AreEqual(definitions[7].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[7].Line, LineType.Dash);
            Assert.AreEqual(definitions[7].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[7].Title, "Wet20Late");
            Assert.AreEqual(definitions[7].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[7].Y as double[], new int[] { 150, 160 });
        }

        /// <summary>
        /// Create series definitions where it works its way though the colours sequentially and 
        /// when it runs out of colours it works through the marker types. Useful when there
        /// are a lot of descriptor values.
        /// </summary>
        [Test]
        public void SeriesWithTwoIdenticalVaryBy()
        {
            var data = "CheckpointName  SimulationName   ABC   Col1  Col2\r\n" +
                       "            ()              ()    ()     ()   (g)\r\n" +
                       "       Current            Sim1     A      1    10\r\n" +
                       "       Current            Sim1     A      2    20\r\n" +
                       "       Current            Sim2     B      1    30\r\n" +
                       "       Current            Sim2     B      2    40\r\n" +
                       "       Current            Sim3     C      1    50\r\n" +
                       "       Current            Sim3     C      2    60\r\n" +
                       "       Current            Sim4     D      1    70\r\n" +
                       "       Current            Sim4     D      2    80\r\n" +
                       "       Current            Sim5     E      1    90\r\n" +
                       "       Current            Sim5     E      2    100\r\n" +
                       "       Current            Sim6     F      1    110\r\n" +
                       "       Current            Sim6     F      2    120\r\n" +
                       "       Current            Sim7     G      1    130\r\n" +
                       "       Current            Sim7     G      2    140\r\n" +
                       "       Current            Sim8     H      1    150\r\n" +
                       "       Current            Sim8     H      2    160\r\n" +
                       "       Current            Sim9     I      1    170\r\n" +
                       "       Current            Sim9     I      2    180\r\n" +
                       "       Current           Sim10     J      1    190\r\n" +
                       "       Current           Sim10     J      2    200\r\n" +
                       "       Current           Sim11     K      1    210\r\n" +
                       "       Current           Sim11     K      2    220\r\n" +
                       "       Current           Sim12     L      1    230\r\n" +
                       "       Current           Sim12     L      2    240\r\n";
            var reader = new TextStorageReader(data);

            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
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
            };
            Apsim.ParentAllChildren(folder);

            var series = folder.Children[1] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.AreEqual(descriptors[0], "ABC");

            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 12);

            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Line, LineType.Solid);
            Assert.AreEqual(definitions[0].Title, "A");

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[1].Line, LineType.Solid);
            Assert.AreEqual(definitions[1].Title, "B");

            Assert.AreEqual(definitions[2].Colour, ColourUtilities.Colours[2]);
            Assert.AreEqual(definitions[2].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[2].Line, LineType.Solid);
            Assert.AreEqual(definitions[2].Title, "C");

            Assert.AreEqual(definitions[3].Colour, ColourUtilities.Colours[3]);
            Assert.AreEqual(definitions[3].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[3].Line, LineType.Solid);
            Assert.AreEqual(definitions[3].Title, "D");

            Assert.AreEqual(definitions[4].Colour, ColourUtilities.Colours[4]);
            Assert.AreEqual(definitions[4].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[4].Line, LineType.Solid);
            Assert.AreEqual(definitions[4].Title, "E");

            Assert.AreEqual(definitions[5].Colour, ColourUtilities.Colours[5]);
            Assert.AreEqual(definitions[5].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[5].Line, LineType.Solid);
            Assert.AreEqual(definitions[5].Title, "F");

            Assert.AreEqual(definitions[6].Colour, ColourUtilities.Colours[6]);
            Assert.AreEqual(definitions[6].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[6].Line, LineType.Solid);
            Assert.AreEqual(definitions[6].Title, "G");

            Assert.AreEqual(definitions[7].Colour, ColourUtilities.Colours[7]);
            Assert.AreEqual(definitions[7].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[7].Line, LineType.Solid);
            Assert.AreEqual(definitions[7].Title, "H");

            // Run out of colours, go back to first colour but increment markertype.

            Assert.AreEqual(definitions[8].Colour, ColourUtilities.Colours[0]); 
            Assert.AreEqual(definitions[8].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[8].Line, LineType.Solid);
            Assert.AreEqual(definitions[8].Title, "I");

            Assert.AreEqual(definitions[9].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[9].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[9].Title, "J");
            Assert.AreEqual(definitions[9].Line, LineType.Solid);

            Assert.AreEqual(definitions[10].Colour, ColourUtilities.Colours[2]);
            Assert.AreEqual(definitions[10].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[10].Line, LineType.Solid);
            Assert.AreEqual(definitions[10].Title, "K");

            Assert.AreEqual(definitions[11].Colour, ColourUtilities.Colours[3]);
            Assert.AreEqual(definitions[11].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[11].Line, LineType.Solid);
            Assert.AreEqual(definitions[11].Title, "L");

        }

        /// <summary>Create a xy series definitions with a regression annotation.</summary>
        [Test]
        public void SeriesWithChildRegressionAnnotation()
        {
            var sim = new Simulation()
            {
                Name = "Sim1",
                Children = new List<Model>()
                {
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2",

                        Children = new List<Model>()
                        {
                            new Regression()
                            {
                                showEquation = true,
                                showOneToOne = true
                            }
                        }
                    },
                }
            };
            Apsim.ParentAllChildren(sim);

            string data =
                "CheckpointName  SimulationName  Col1  Col2\r\n" +
                "            ()              ()    ()   (g)\r\n" +
                "       Current            Sim1     1    1.0\r\n" +
                "       Current            Sim1     2    1.5\r\n" +
                "       Current            Sim1     3    2.0\r\n" +
                "       Current            Sim1     4    2.5\r\n";

            var reader = new TextStorageReader(data);

            var series = sim.Children[0] as Series;
            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 3);
            Assert.AreEqual(definitions[0].Title, "Series");
            Assert.AreEqual(definitions[0].Type, SeriesType.Bar);
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2, 3, 4 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 1.0, 1.5, 2.0, 2.5 });

            Assert.AreEqual(definitions[1].Title, "Regression line");
            Assert.AreEqual(definitions[1].Type, SeriesType.Scatter);
            Assert.AreEqual(definitions[1].LineThickness, LineThicknessType.Normal);
            Assert.AreEqual(definitions[1].MarkerSize, MarkerSizeType.Normal);
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 4 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 1, 2.5 });

            Assert.AreEqual(definitions[2].Title, "1:1 line");
            Assert.AreEqual(definitions[2].Type, SeriesType.Scatter);
            Assert.AreEqual(definitions[2].LineThickness, LineThicknessType.Normal);
            Assert.AreEqual(definitions[2].MarkerSize, MarkerSizeType.Normal);
            Assert.AreEqual(definitions[2].X as double[], new double[] { 1, 4 });
            Assert.AreEqual(definitions[2].Y as double[], new double[] { 1, 4 });

        }

        /// <summary>Create series definitions with 'Vary by graph series'.</summary>
        [Test]
        public void SeriesWithVaryByGraphSeries()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "Exp", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName     Exp Col1  Col2  Col3\r\n" +
                "            ()              ()      ()   ()   (g)    ()\r\n" +
                "       Current            Sim1    Exp1    1    10    50\r\n" +
                "       Current            Sim1    Exp1    1    10    50\r\n" +
                "       Current            Sim2    Exp2    2    20    60\r\n" +
                "       Current            Sim2    Exp2    2    20    60\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Title, "Series1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 1, 2, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 10, 20, 20 });


            var series2 = folder.Children[1].Children[1] as Series;
            var definitions2 = new List<SeriesDefinition>();
            series2.GetSeriesToPutOnGraph(reader, definitions2);
            Assert.AreEqual(definitions2[0].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions2[0].Title, "Series2");
            Assert.AreEqual(definitions2[0].X as double[], new double[] { 1, 1, 2, 2 });
            Assert.AreEqual(definitions2[0].Y as double[], new double[] { 50, 50, 60, 60 });
        }

        /// <summary>Create a single xy series definition with a 'Vary By Simulation' grouping.</summary>
        [Test]
        public void SeriesWithVaryBySimulation()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Exp", "Exp2"),
                    }),
                    new Graph()
                    {
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName    Exp Col1  Col2\r\n" +
                "            ()              ()     ()   ()   (g)\r\n" +
                "       Current            Sim1   Exp1    1    10\r\n" +
                "       Current            Sim1   Exp1    2    20\r\n" +
                "       Current            Sim2   Exp2    1    30\r\n" +
                "       Current            Sim2   Exp2    2    40\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 2);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Title, "Sim1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Title, "Sim2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1,  2 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 30, 40 });

        }

        /// <summary>Create xy series definitions with a 'Vary By Zone' grouping.</summary>
        [Test]
        public void SeriesWithVaryByZone()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Zone", "Zone1", "Zone", "Zone2"),
                        new Description("Sim2", "SimulationName", "Sim2", "Zone", "Zone1", "Zone", "Zone2")
                    }),
                    new Graph()
                    {
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName   Zone Col1  Col2\r\n" +
                "            ()              ()     ()   ()   (g)\r\n" +
                "       Current            Sim1  Zone1    1    10\r\n" +
                "       Current            Sim1  Zone1    2    20\r\n" +
                "       Current            Sim1  Zone2    1    30\r\n" +
                "       Current            Sim1  Zone2    2    40\r\n" +
                "       Current            Sim2  Zone1    1    50\r\n" +
                "       Current            Sim2  Zone1    2    60\r\n" +
                "       Current            Sim2  Zone2    1    70\r\n" +
                "       Current            Sim2  Zone2    2    80\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 4);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Title, "Sim1Zone1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[1].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[1].Title, "Sim1Zone2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 30, 40 });

            Assert.AreEqual(definitions[2].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[2].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[2].Title, "Sim2Zone1");
            Assert.AreEqual(definitions[2].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[2].Y as double[], new double[] { 50, 60 });

            Assert.AreEqual(definitions[3].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[3].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[3].Title, "Sim2Zone2");
            Assert.AreEqual(definitions[3].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[3].Y as double[], new double[] { 70, 80 });
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
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Exp", "Exp2")
                    }),
                    new Graph()
                    {
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName SimulationName Col1  Col2\r\n" +
                "            ()             ()   ()   (g)\r\n" +
                "       Current           Sim1    1    10\r\n" +
                "       Current           Sim1    2    20\r\n" +
                "       Current           Sim2    1    30\r\n" +
                "       Current           Sim2    2    40\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 2);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Title, "Exp1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Title, "Exp2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 30, 40 });
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
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1"),
                        new Description("Sim2", "SimulationName", "Sim2")
                    }),
                    new Graph()
                    {
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

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

            var series1 = folder.Children[1].Children[0] as Series;

            var descriptorNames = series1.GetDescriptorNames(reader).ToArray();
            //Assert.AreEqual(descriptorNames, new string[] { "SimulationName", "Graph series", "ABC" });

            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 4);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Title, "Ad");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[1].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[1].Title, "Ae");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 30, 40 });

            Assert.AreEqual(definitions[2].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[2].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[2].Title, "Bd");
            Assert.AreEqual(definitions[2].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[2].Y as double[], new double[] { 50, 60 });

            Assert.AreEqual(definitions[3].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[3].Marker, MarkerType.FilledDiamond);
            Assert.AreEqual(definitions[3].Title, "Be");
            Assert.AreEqual(definitions[3].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[3].Y as double[], new double[] { 70, 80 });
        }

        /// <summary>Create xy series definitions with a filter.</summary>
        [Test]
        public void SeriesWithFilter()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "Exp", "Exp1"),
                        new Description("Sim2", "Exp", "Exp2")
                    }),
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2",
                        FactorToVaryColours = "Exp",
                        Filter = "A='a'"
                    },
                }
            };
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName     Exp   A  Col1  Col2\r\n" +
                "            ()              ()      ()  ()    ()   (g)\r\n" +
                "       Current            Sim1    Exp1   a     1    10\r\n" +
                "       Current            Sim1    Exp1   a     1    10\r\n" +
                "       Current            Sim2    Exp2   b     2    20\r\n" +
                "       Current            Sim2    Exp2   b     2    20\r\n";

            var reader = new TextStorageReader(data);

            var series = folder.Children[1] as Series;
            var descriptors = series.GetDescriptorNames(reader).ToList();
            Assert.AreEqual(descriptors[0], "Exp");

            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 1);

            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 1 });
            Assert.AreEqual(definitions[0].Y as double[], new int[] { 10, 10 });
        }

        /// <summary>Create xy series definitions with a filter.</summary>
        [Test]
        public void SeriesWithFilter2()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1"),
                        new Description("Sim2", "SimulationName", "Sim2")
                    }),
                    new Series()
                    {
                        Name = "Series",
                        TableName = "Report",
                        XFieldName = "Col1",
                        YFieldName = "Col2",
                        FactorToVaryColours = "SimulationName",
                        Filter = "A='a'"
                    },
                }
            };
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName    SimulationID     Exp   A  Col1  Col2\r\n" +
                "            ()              ()      ()  ()    ()   (g)\r\n" +
                "       Current               1    Exp1   a     1    10\r\n" +
                "       Current               1    Exp1   a     1    10\r\n" +
                "       Current               2    Exp2   b     2    20\r\n" +
                "       Current               2    Exp2   b     2    20\r\n";

            var reader = new TextStorageReader(data);

            var series = folder.Children[1] as Series;

            var definitions = new List<SeriesDefinition>();
            series.GetSeriesToPutOnGraph(reader, definitions);

            Assert.AreEqual(definitions.Count, 1);

            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 1 });
            Assert.AreEqual(definitions[0].Y as double[], new int[] { 10, 10 });
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
                Children = new List<Model>()
                {
                    new Folder()
                    {
                        Name = "Folder1",
                        Children = new List<Model>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim1", "SimulationName", "Sim1", "Exp", "Exp1"),
                                new Description("Sim2", "SimulationName", "Sim2", "Exp", "Exp2")
                            }),
                            new Graph()
                            {
                                Children = new List<Model>()
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
                        Children = new List<Model>()
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


            Apsim.ParentAllChildren(simulations);

            string data =
                "CheckpointName  SimulationName    Exp Col1  Col2\r\n" +
                "            ()              ()     ()   ()   (g)\r\n" +
                "       Current            Sim1   Exp1    1    10\r\n" +
                "       Current            Sim1   Exp1    2    20\r\n" +
                "       Current            Sim2   Exp2    1    30\r\n" +
                "       Current            Sim2   Exp2    2    40\r\n" +
                "       Current            Sim3   Exp3    1    50\r\n" +
                "       Current            Sim3   Exp3    2    60\r\n" +
                "       Current            Sim4   Exp4    1    70\r\n" +
                "       Current            Sim4   Exp4    2    80\r\n";

            var reader = new TextStorageReader(data);

            var series1 = simulations.Children[0].Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 2);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Title, "Sim1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Title, "Sim2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 30, 40 });

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
                Children = new List<Model>()
                {
                    new Folder()
                    {
                        Name = "Folder1",
                        Children = new List<Model>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim1", "SimulationName", "Sim1"),
                            }),
                            new Graph()
                            {
                                Children = new List<Model>()
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
                        Children = new List<Model>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim2", "SimulationName", "Sim2"),
                            }),
                        }
                    }
                }
            };


            Apsim.ParentAllChildren(simulations);

            string data =
                "CheckpointName  SimulationName Col1  Col2\r\n" +
                "            ()              ()   ()   (g)\r\n" +
                "       Current            Sim1    1    10\r\n" +
                "       Current            Sim1    2    20\r\n" +
                "       Current            Sim2    1    30\r\n" +
                "       Current            Sim2    2    40\r\n";

            var reader = new TextStorageReader(data);

            var series1 = simulations.Children[0].Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 1);
            Assert.AreEqual(definitions[0].Title, "Series1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });
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
                Children = new List<Model>()
                {
                    new Folder()
                    {
                        Name = "Folder1",
                        Children = new List<Model>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim1", "SimulationName", "Sim1", "A", "a"),
                            }),
                            new Graph()
                            {
                                Children = new List<Model>()
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
                        Children = new List<Model>()
                        {
                            new MockSimulationDescriptionGenerator(new List<Description>()
                            {
                                new Description("Sim2", "SimulationName", "Sim2", "A", "b"),
                            }),
                        }
                    }
                }
            };

            Apsim.ParentAllChildren(simulations);

            string data =
                "CheckpointName  SimulationName   A  Col1  Col2\r\n" +
                "            ()              ()  ()    ()   (g)\r\n" +
                "       Current            Sim1   a     1    10\r\n" +
                "       Current            Sim1   a     2    20\r\n" +
                "       Current            Sim2   a     1    30\r\n" +
                "       Current            Sim2   a     2    40\r\n";

            var reader = new TextStorageReader(data);

            var series1 = simulations.Children[0].Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 1);
            Assert.AreEqual(definitions[0].Title, "a");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 10, 20 });
        }

        /// <summary>Create xy series definitions from predicted/observed table.</summary>
        [Test]
        public void SeriesFromPredictedObserved()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
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
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName Predicted.Grain.Wt  Observed.Grain.Wt\r\n" +
                "            ()              ()                 ()                 ()\r\n" +
                "       Current            Sim1                  1                  1\r\n" +
                "       Current            Sim2                  2                  5\r\n" +
                "       Current            Sim3                  3                  8\r\n" +
                "       Current            Sim4                  4                  6\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 2);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Title, "Exp1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 1, 5 });

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[1].Title, "Exp2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 3, 4 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 8, 6 });
        }

        /// <summary>Create xy series definitions from predicted/observed table.</summary>
        [Test]
        public void SeriesFromPredictedObservedAndMixOfSimulations()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
                {
                    new MockSimulationDescriptionGenerator(new List<Description>()
                    {
                        new Description("Sim1", "SimulationName", "Sim1", "Experiment", "Exp1"),
                        new Description("Sim2", "SimulationName", "Sim2", "Experiment", "Exp1"),
                        new Description("Sim3", "SimulationName", "Sim3")
                    }),
                    new Graph()
                    {
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName Predicted.Grain.Wt  Observed.Grain.Wt\r\n" +
                "            ()              ()                 ()                 ()\r\n" +
                "       Current            Sim1                  1                  1\r\n" +
                "       Current            Sim2                  2                  5\r\n" +
                "       Current            Sim3                  3                  8\r\n" +
                "       Current            Sim3                  4                  6\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 1);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Title, "Exp1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 1, 5 });
        }

        /// <summary>Create xy series definitions from predicted/observed table with error bars.</summary>
        [Test]
        public void SeriesWithErrorBars()
        {
            var folder = new Folder()
            {
                Name = "Folder",
                Children = new List<Model>()
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
                        Children = new List<Model>()
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
            Apsim.ParentAllChildren(folder);

            string data =
                "CheckpointName  SimulationName Predicted.Grain.Wt  Observed.Grain.Wt  Observed.Grain.WtError\r\n" +
                "            ()              ()                 ()                 ()                      ()\r\n" +
                "       Current            Sim1                  1                  1                     0.1\r\n" +
                "       Current            Sim2                  2                  5                     0.5\r\n" +
                "       Current            Sim3                  3                  8                     0.8\r\n" +
                "       Current            Sim4                  4                  6                     0.6\r\n";

            var reader = new TextStorageReader(data);

            var series1 = folder.Children[1].Children[0] as Series;
            var definitions = new List<SeriesDefinition>();

            series1.GetSeriesToPutOnGraph(reader, definitions);
            Assert.AreEqual(definitions.Count, 2);
            Assert.AreEqual(definitions[0].Colour, ColourUtilities.Colours[0]);
            Assert.AreEqual(definitions[0].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[0].Title, "Exp1");
            Assert.AreEqual(definitions[0].X as double[], new double[] { 1, 2 });
            Assert.AreEqual(definitions[0].Y as double[], new double[] { 1, 5 });
            Assert.AreEqual(definitions[0].Error.ToList()[0], 0.1, 0.000001);
            Assert.AreEqual(definitions[0].Error.ToList()[1], 0.5, 0.000001);

            Assert.AreEqual(definitions[1].Colour, ColourUtilities.Colours[1]);
            Assert.AreEqual(definitions[1].Marker, MarkerType.FilledCircle);
            Assert.AreEqual(definitions[1].Title, "Exp2");
            Assert.AreEqual(definitions[1].X as double[], new double[] { 3, 4 });
            Assert.AreEqual(definitions[1].Y as double[], new double[] { 8, 6 });
            Assert.AreEqual(definitions[1].Error.ToList()[0], 0.8, 0.000001);
            Assert.AreEqual(definitions[1].Error.ToList()[1], 0.6, 0.000001);
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
