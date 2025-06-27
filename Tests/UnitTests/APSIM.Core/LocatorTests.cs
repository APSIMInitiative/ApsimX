﻿using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Soils.Nutrients;
using Models.Functions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace APSIM.Core.Tests
{
    [TestFixture]
    public class LocatorTests
    {
        class ModelA : Model
        {
            public int A1 { get { return 1; } }
            public int A2 { get { return 2; } }
        }

        class ModelB : Model
        {
            public int B1 { get { return 3; } }
            public int B2 { get { return 4; } }
        }

        class ModelC : Model
        {
            public int C1 { get { return 5; } }
            public double[] C2 { get { return new double[] { 6.0, 6.1, 6.2 }; } }
        }

        class ModelD : Model
        {
            private Clock clock = new Clock() { Start = new DateTime(2000, 1, 1) };
            public int D1 { get { return 7; } }
            public DateTime D2 { get { return new DateTime(2000, 1, 1); } }
            public Clock D3 { get { return clock; } set { clock = value; } }
        }

        class ModelE : Model
        {
            public ModelE(int f1, int f2)
            {
                models[0].F = f1;
                models[1].F = f2;
            }
            public ModelF[] models = new ModelF[] { new ModelF(), new ModelF() };
            public ModelF[] E1 { get { return models; } }
        }

        class ModelF : Model
        {
            public ModelF(string name = "ModelF", int value = 0)
            {
                Name = name;
                F = value;
            }
            public int F { get; set; }
        }

        class ModelG : Model
        {
            private ModelF model = null;
            public ModelF G1 { get { return model; } }
        }

        class ModelH : Model
        {
            public int H { get; } = 2;
            public int HFunction() { return 3; }
        }


        class ModelI : Model
        {
            public List<ModelA> I { get; } = [new ModelA(), new ModelA()];
        }

        public interface IInterface
        {
            int X { get; }
        }

        public class Container : Model
        {
            public InterfaceBase Current { get; set; }
            public IInterface Value { get; set; }
        }

        public abstract class InterfaceBase : IInterface
        {
            public abstract int X { get; }
        }

        public class Concrete2 : InterfaceBase
        {
            public override int X => 2;
        }

        public class Concrete3 : InterfaceBase
        {
            public override int X => 3;
        }

        [Test]
        public void TestAbstractProperty()
        {
            Concrete2 c2 = new Concrete2();
            Concrete3 c3 = new Concrete3();
            Container container = new Container();

            Simulation sim = new Simulation();
            sim.Children.Add(container);
            Simulations sims = new Simulations();
            sims.Children.Add(sim);
            Node.Create(sims);

            container.Current = c2;
            Assert.That(sim.Node.Get("[Container].Current.X"), Is.EqualTo(2));
            container.Current = c3;
            Assert.That(sim.Node.Get("[Container].Current.X"), Is.EqualTo(3));
        }

        [Test]
        public void TestPropertyOfInterface()
        {
            Concrete2 c2 = new Concrete2();
            Concrete3 c3 = new Concrete3();
            Container container = new Container();

            Simulation sim = new Simulation();
            sim.Children.Add(container);
            Simulations sims = new Simulations();
            sims.Children.Add(sim);
            Node.Create(sims);

            container.Value = c2;
            Assert.That(sim.Node.Get("[Container].Value.X"), Is.EqualTo(2));
            container.Value = c3;
            Assert.That(sim.Node.Get("[Container].Value.X"), Is.EqualTo(3));
        }

        [Test]
        public void LocatorGetVariable()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelD()
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for modelC
            ILocator locatorForC = sim.Children[2].Children[0].Node;
            Assert.That(locatorForC.Get("[ModelA].A1"), Is.EqualTo(1));

            // locator for modelD
            ILocator locatorForD = sim.Children[2].Children[1].Node;
            Assert.That(locatorForD.Get("[ModelD].D2.Year"), Is.EqualTo(2000));
        }

        [Test]
        public void LocatorGetVariableWithArrayIndex()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelD()
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for modelD
            ILocator locatorForD = sim.Children[2].Children[1].Node;
            Assert.That(locatorForD.Get("[ModelC].C2[1]"), Is.EqualTo(6.0));
            Assert.That(locatorForD.Get("[ModelC].C2[2]"), Is.EqualTo(6.1));
            Assert.That(locatorForD.Get("[ModelC].C2[3]"), Is.EqualTo(6.2));
        }

        [Test]
        public void LocatorGetVariableWithAbsoluteAddress()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelD()
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for modelC
            ILocator locatorForC = sim.Children[2].Children[0].Node;
            Assert.That(locatorForC.Get(".Simulations.Simulation.ModelA.A1"), Is.EqualTo(1));

            // locator for modelD
            ILocator locatorForD = sim.Children[2].Children[1].Node;
            Assert.That(locatorForD.Get(".Simulations.Simulation.Zone.ModelD.D2.Year"), Is.EqualTo(2000));
        }

        [Test]
        public void LocatorGetVariableWithRelativeAddress()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelD()
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for zone
            ILocator locatorForZone = sim.Children[2].Node;
            Assert.That(locatorForZone.Get("ModelC.C1"), Is.EqualTo(5));
        }

        [Test]
        public void LocatorGetExpression()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelD()
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for modelC
            ILocator locatorForC = sim.Children[2].Children[0].Node;
            Assert.That(locatorForC.Get("[ModelA].A1+[ModelD].D2.Year"), Is.EqualTo(2001));
        }

        [Test]
        public void LocatorGetModel()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelD()
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for modelC
            ILocator locatorForC = sim.Children[2].Children[0].Node;
            Assert.That(locatorForC.Get("[ModelA]"), Is.EqualTo(sim.Children[0]));
        }

        [Test]
        public void LocatorGetPropertyOfModelAtSpecificArrayElement()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelF(),
                            new ModelB(),
                            new Zone()
                            {
                                Children =
                                [
                                    new ModelC(),
                                    new ModelE(20, 21)
                                ]
                            }
                        ]
                    }
                ]
            };

            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // locator for modelC
            ILocator locatorForC = sim.Children[2].Children[0].Node;
            Assert.That(locatorForC.Get("[ModelE].E1[1].F"), Is.EqualTo(20));
            Assert.That(locatorForC.Get("[ModelE].E1[2].F"), Is.EqualTo(21));
        }

        [Test]
        public void LocatorGetPropertyOfModelThatHasChildWithSameName()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA()
                            {
                                Children =
                                [
                                    new Constant("A1", 10, "a1units")
                                ]
                            },
                            new ModelD()
                            {
                                Children =
                                [
                                    new Constant("D2", 10)
                                ]
                            },
                            new ModelG()
                            {
                                Children =
                                [
                                    new ModelF("G1", 27),
                                ]
                            }
                        ]
                    }
                ]
            };
            Simulations sims = Node.Create(simulations).Model as Simulations;
            Simulation sim = simulations.Children.First() as Simulation;

            // Check that the A1 property is referenced and not the child constant
            ILocator locator = sim.Node;
            Assert.That(locator.Get("[ModelA].A1"), Is.EqualTo((sim.Children[0] as ModelA).A1));

            //Check that if given the modelsOnly flag, that the child is returned
            Constant c = locator.Get("[ModelA].A1", LocatorFlags.ModelsOnly) as Constant;
            Assert.That(c.FixedValue, Is.EqualTo(10));

            //Check that we get the child if an additional name is provided and the property is a primitive type
            string units = locator.Get("[ModelA].A1.Units") as string;
            Assert.That(units, Is.EqualTo("a1units"));

            //check that we get the property if the property is not a primitive type
            DateTime date = (DateTime)locator.Get("[ModelD].D3.StartDate");
            Assert.That((sim.Children[1] as ModelD).D2, Is.EqualTo(date));

            //Check that if a property has a getter that will throw an exception, but there is a child with that name, that the child is returned
            int g = (int)locator.Get("[ModelG].G1.F");
            Assert.That((sim.Children[2] as ModelG).G1, Is.Null);
            Assert.That(g, Is.EqualTo(27));
        }

        [Test]
        public void LocatorGetCNRFPropertyOfNuterient()
        {
            //This is a special case that the locator needs to handle, as it previously returned the incorrect child instead of the property
            Assembly models = typeof(IModel).Assembly;
            string[] names = models.GetManifestResourceNames();
            string nut = ReflectionUtilities.GetResourceAsString(models, "Models.Resources.Nutrient.json");

            Simulations sims = FileFormat.ReadFromString<Simulations>(nut).Model as Simulations;

            // Check that the CNRF property is referenced and not the child model
            Nutrient nutrient = sims.Children[0] as Nutrient;
            ILocator locator = sims.Node;
            Assert.That(nutrient.CNRF, Is.EqualTo(locator.Get("[Nutrient].CNRF")));

            //check that the child still exists as well
            Model cnrfChild = null;
            foreach(Model child in nutrient.Children)
                if (child.Name == "CNRF")
                    cnrfChild = child;
            if (cnrfChild == null)
                Assert.Fail();
        }

        /// <summary>
        /// Make a TestSimulation we can use for function tests
        /// </summary>
        private Simulations MakeTestSimulation()
        {
            Simulations simulations = new()
            {
                Children =
                [
                    new Simulation()
                    {
                        Children =
                        [
                            new ModelA(),
                            new ModelB(),
                            new ModelH(),
                            new ModelD(),
                            new ModelF(),
                            new ModelC(),
                            new ModelI()
                        ]
                    }
                ]
            };
            return Node.Create(simulations).Model as Simulations;
        }

        /// <summary>
        /// Specific test for Clear
        /// </summary>
        [Test]
        public void ClearTests()
        {
            var sims = MakeTestSimulation();
            //fill up the cache with some gets
            sims.Node.Get("[ModelA]");
            sims.Node.Get("[ModelA].A1");
            sims.Node.Get("[ModelB].B1");
            FieldInfo cacheField = typeof(Locator).GetField("cache", BindingFlags.NonPublic | BindingFlags.Instance);
            var cache = cacheField.GetValue(sims.Node.Locator) as Dictionary<(object relativeTo, string path), VariableComposite>;
            Assert.That(cache.Count, Is.EqualTo(3));
            sims.Node.ClearLocator();
            Assert.That(cache.Count, Is.EqualTo(0));

            //should not error if cache empty
            Assert.DoesNotThrow(() => sims.Node.ClearLocator());
        }

        /// <summary>
        /// Specific test for ClearEntry
        /// Check that the cache is filling and clearing correctly.
        /// </summary>
        [Test]
        public void ClearEntryTests()
        {
            var sims = MakeTestSimulation();

            //fill up the cache with some gets
            sims.Node.Get("[ModelA]");
            sims.Node.Get("[ModelA].A1");
            sims.Node.Get("[ModelB].B1");
            FieldInfo cacheField = typeof(Locator).GetField("cache", BindingFlags.NonPublic | BindingFlags.Instance);
            var cache = cacheField.GetValue(sims.Node.Locator) as Dictionary<(object relativeTo, string path), VariableComposite>;

            Assert.That(cache.Count, Is.EqualTo(3));
            //clear only 1 cache entry
            sims.Node.ClearEntry("[ModelA].A1");
            Assert.That(cache.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Specific test for Get
        /// A Lot of the checks for this function are covered by other units tests here,
        /// so this just checks the state of the cache for some edge cases.
        /// </summary>
        [Test]
        public void GetTests()
        {
            var sims = MakeTestSimulation();
            FieldInfo cacheField = typeof(Locator).GetField("cache", BindingFlags.NonPublic | BindingFlags.Instance);
            var cache = cacheField.GetValue(sims.Node.Locator) as Dictionary<(object relativeTo, string path), VariableComposite>;

            //A failed Get should not put anything into the cache
            sims.Node.Get("[ModelA].Error");
            Assert.That(cache.Count, Is.EqualTo(0));

            //A Get should not put anything into the cache with the ModelsOnly flag
            sims.Node.Get("[ModelA].Error", LocatorFlags.ModelsOnly);
            Assert.That(cache.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Specific test for GetObject
        /// This is used by Get to get the variable it returns.
        /// </summary>
        [Test]
        public void GetObjectTests()
        {
            ILocator loc = MakeTestSimulation().Node;
            //should return the IVaraible instead of the value of a property
            int val = (int)loc.Get("[ModelA].A1");
            var v = loc.GetObject("[ModelA].A1");
            Assert.That(v.Value, Is.EqualTo(val));

            //should return null if object not found
            v = loc.GetObject("[ModelA].Error");
            Assert.That(v, Is.Null);
        }

        /// <summary>
        /// Specific test for GetObjectProperties
        /// Is used by report to get only property variables
        /// </summary>
        [Test]
        public void GetObjectProperties()
        {
            Simulations sims = MakeTestSimulation();
            ILocator loc = sims.Node;
            //should only be able to get the property, not the function
            int val = (int)loc.Get("[ModelH].H");
            Assert.That((sims.Children[0].Children[2] as ModelH).H, Is.EqualTo(val));

            object val2 = loc.Get("[ModelH].HFunction");
            Assert.That(val2, Is.Null);
            Assert.That((sims.Children[0].Children[2] as ModelH).HFunction(), Is.EqualTo(3));
        }


        /// <summary>
        /// Get property of a list of instances.
        /// </summary>
        [Test]
        public void GetPropertyOfListOFInstances()
        {
            Simulations sims = MakeTestSimulation();
            ILocator loc = sims.Node;
            // should be return an array of A's.
            int[] val = (int[])loc.Get("[ModelI].I.A1");

            Assert.That(val, Is.EqualTo(new int[] { 1, 1 }));
        }

        /// <summary>
        /// Specific test for Set
        /// Sets a value at the given path. Need to check all cases, property, method and model.
        /// Uses Get as the way to find the variable
        /// </summary>
        [Test]
        public void SetTests()
        {
            Simulations sims = MakeTestSimulation();
            ILocator loc = sims.Node;

            //set a read only property
            Assert.Throws<Exception>(() => loc.Set("[ModelA].A1", 10));
            Assert.That((sims.Children[0].Children[0] as ModelA).A1, Is.EqualTo(1));

            //set a editable property
            loc.Set("[ModelF].F", 10);
            Assert.That((sims.Children[0].Children[4] as ModelF).F, Is.EqualTo(10));

            //set a method (should fail)
            Assert.Throws<Exception>(() => loc.Set("[ModelH].HFunction", 10));

            //set a model
            DateTime dt = new DateTime(2020, 1, 1);
            loc.Set("[ModelD].D3", new Clock() { StartDate = dt });
            Assert.That((sims.Children[0].Children[3] as ModelD).D3.StartDate, Is.EqualTo(dt));
        }

        /// <summary>
        /// Specific test for IsExpression
        /// This function determines if the line is an expression that needs to be calculated or not
        /// I don't think this function is very good at doing that though.
        /// </summary>
        [Test]
        public void IsExpressionTests()
        {
            Locator loc = MakeTestSimulation().Node.Locator;
            MethodInfo isExpression = typeof(Locator).GetMethod("IsExpression", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            string input = "";

            //Non relative reference - return false
            input = ".Simulations.Simulation.ModelA.A1";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //Non relative reference that looks like an expression - return false
            input = ".Simulations.Simulation.ModelA.A1 + .Simulations.Simulation.ModelA.A2";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //empty - return false
            input = "";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //spaces - return false
            input = "     ";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //tabs - return false
            input = "\t";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //tabs and spaces - return false
            input = " \t \t ";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //should return false
            input = "()";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.False);

            //Plus - return true
            input = "[ModelA].A1 + [ModelA].A2";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.True);

            //Multiply - return true
            input = "[ModelA].A1 * [ModelA].A2";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.True);

            //divide - return true
            input = "[ModelA].A1 / [ModelA].A2";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.True);

            //power - return true
            input = "[ModelA].A1 ^ [ModelA].A2";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.True);

            //This shouldn't be an expression, but according to the code, should still return true
            input = "([ModelA].A1  [ModelA].A2) as A3";
            Assert.That((bool)isExpression.Invoke(loc, new object[] { input }), Is.True);
        }

        /// <summary>
        /// Specific test for GetInternalRelativeTo
        /// Takes the path given and should return the first model it finds from that path.
        /// Doesn't care about the rest of the path after that
        /// </summary>
        [Test]
        public void GetInternalRelativeToTests()
        {
            Simulations sims = MakeTestSimulation();
            Locator loc = sims.Node.Locator;
            MethodInfo getInternalRelativeTo = typeof(Locator).GetMethod("GetInternalRelativeTo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

            List<string> inputs = new List<string>();
            List<object> results = new List<object>();
            List<string> outputs = new List<string>();
            List<object[]> argss = new List<object[]>();

            //add the variations of ignore case and throw error arguements
            //IModel relativeTo, string namePath, StringComparison compareType, bool throwOnError, out string namePathFiltered
            argss.Add(new object[] { sims, "", StringComparison.Ordinal, false, new string("") });
            argss.Add(new object[] { sims, "", StringComparison.Ordinal, true, new string("") });
            argss.Add(new object[] { sims, "", StringComparison.OrdinalIgnoreCase, false, new string("") });
            argss.Add(new object[] { sims, "", StringComparison.OrdinalIgnoreCase, true, new string("") });

            //Should find ModelA
            inputs.Add("[ModelA].A1");
            results.Add(sims.Children[0].Children[0]);
            outputs.Add(".A1");

            //Should not find Model A because missing [] or .
            inputs.Add("ModelA");
            results.Add(null);
            outputs.Add("");

            //Should not find anything because empty
            inputs.Add("");
            results.Add(null);
            outputs.Add("");

            //Should not find Model A because missing ]
            inputs.Add("[ModelA");
            results.Add(null);
            outputs.Add("");

            //Should not find Model A because missing [
            inputs.Add("ModelA]");
            results.Add(null);
            outputs.Add("");

            //Should not find Model A because .
            inputs.Add(".[ModelA]");
            results.Add(null);
            outputs.Add("");

            //Should find Model A and have empty output string
            inputs.Add("[ModelA]");
            results.Add(sims.Children[0].Children[0]);
            outputs.Add("");

            //Absolute path should return Simulations
            inputs.Add(".Simulations.Simulation.ModelA.A1");
            results.Add(sims);
            outputs.Add("Simulation.ModelA.A1");

            //A relative path with an array reference
            inputs.Add("[ModelC].C2[1]");
            results.Add(sims.Children[0].Children[5]);
            outputs.Add(".C2[1]");

            //A model that doesn't exist and should return null
            inputs.Add("[ModelDoesNotExist]");
            results.Add(null);
            outputs.Add("");

            //Wrong root simulations
            inputs.Add(".NotSimulations.Simulation.ModelA");
            results.Add(null);
            outputs.Add("");

            //An incorrect path, but correct starting pint should still work for this
            inputs.Add(".Simulations..ModelA");
            results.Add(sims);
            outputs.Add(".ModelA");

            for (int i = 0; i < inputs.Count; i++)
            {
                for (int j = 0; j < argss.Count; j++)
                {
                    object[] args = argss[j];
                    args[1] = inputs[i];

                    if (results[i] == null && (bool)args[3] == true)
                    {
                        Assert.Throws<TargetInvocationException>(() => getInternalRelativeTo.Invoke(loc, args));
                    }
                    else
                    {
                        Node n = getInternalRelativeTo.Invoke(loc, args) as Node;
                        Assert.That(results[i], Is.EqualTo(n?.Model));
                        Assert.That(outputs[i], Is.EqualTo(args[4]));
                    }
                }
            }
        }

        /// <summary>
        /// Specific test for GetInternalNameBits
        /// Just chops up the path into words based on .
        /// </summary>
        [Test]
        public void GetInternalNameBitsTests()
        {
            Simulations sims = MakeTestSimulation();
            Locator loc = sims.Node.Locator;
            MethodInfo getInternalNameBits = typeof(Locator).GetMethod("GetInternalNameBits", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

            List<string> inputs = new List<string>();
            List<string[]> results = new List<string[]>();
            List<object[]> argss = new List<object[]>();

            //add the variations of ignore case and throw error arguements
            //string namePath, string cacheKey, bool throwOnError
            argss.Add(new object[] { "", false });
            argss.Add(new object[] { "", true });

            //Empty path should give back no bits or error
            inputs.Add("");
            results.Add(new string[] {});

            //a single space path should give an error
            inputs.Add(" ");
            results.Add(null);

            //Should give back A B C
            inputs.Add(".A.B.C.");
            results.Add(new string[] { "A", "B", "C" });

            //Should give back A B C
            inputs.Add("A.B.C");
            results.Add(new string[] { "A", "B", "C" });

            //Should give back ABC
            inputs.Add("ABC");
            results.Add(new string[] { "ABC" });

            //Should give back ABC
            inputs.Add("ABC.");
            results.Add(new string[] { "ABC" });

            //Should give back ABC
            inputs.Add(".......ABC.");
            results.Add(new string[] { "ABC" });

            //Should give an error for no bits
            inputs.Add(".");
            results.Add(null);

            for (int i = 0; i < inputs.Count; i++)
            {
                for (int j = 0; j < argss.Count; j++)
                {
                    object[] args = argss[j];
                    args[0] = inputs[i];
                    //args[1] = inputs[i];

                    if (results[i] == null && (bool)args[1] == true)
                    {
                        Assert.Throws<TargetInvocationException>(() => getInternalNameBits.Invoke(loc, args));
                    }
                    else
                    {
                        string[] m = getInternalNameBits.Invoke(loc, args) as string[];
                        Assert.That(results[i], Is.EqualTo(m));
                    }
                }
            }
        }

        /// <summary>
        /// Specific test for GetInternalObjectInfo
        /// This function is still to complex to test correctly, it needs further breaking down in a future refactor
        /// It doesn't make sense to test it independantly as it's functionality is tied directly into GetInternal and doesn't work without it
        /// </summary>
        [Test]
        public void GetInternalObjectInfoTests()
        {
        }

        /// <summary>
        /// A test to check that all functions in Locater have been tested by a unit test.
        /// </summary>
        [Test]
        public void MethodsHaveUnitTests()
        {
            BindingFlags reflectionFlagsMethods = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
            BindingFlags reflectionFlagsProperties = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;
            Type testsType = typeof(LocatorTests);
            Type modelType = typeof(Locator);

            //Get list of methods in this test file
            List<MethodInfo> testMethods = ReflectionUtilities.GetAllMethods(testsType, reflectionFlagsMethods, false);
            string names = "";
            foreach (MethodInfo method in testMethods)
                names += method.Name + "\n";

            //Get lists of methods and properties from Manager
            List<MethodInfo> methods = ReflectionUtilities.GetAllMethodsWithoutProperties(modelType);
            List<PropertyInfo> properties = ReflectionUtilities.GetAllProperties(modelType, reflectionFlagsProperties, false);

            //Check that at least one of the methods is named for the method or property
            foreach (MethodInfo method in methods)
                if (names.Contains(method.Name) == false)
                    Assert.Fail($"{method.Name} is not tested by an individual unit test.");

            foreach (PropertyInfo prop in properties)
                if (names.Contains(prop.Name) == false)
                    Assert.Fail($"{prop.Name} is not tested by an individual unit test.");
        }
    }
}