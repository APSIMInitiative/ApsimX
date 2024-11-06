using APSIM.Shared.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.APSIMShared
{
    /// <summary>
    /// Unit tests for the expression parser.
    /// <see cref="ExpressionEvaluator"/>.
    /// </summary>
    [TestFixture]
    public class ExpressionEvaluatorTests
    {
        /// <summary>
        /// Tolerance for floating point comparisons.
        /// </summary>
        private const double tolerance = 1e-12;

        /// <summary>
        /// Ensure that an expression can be parsed without error and
        /// that the result matches an expected value.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <param name="value">Expected value of the expression.</param>
        [TestCase("1 + 2", 3)]
        [TestCase("2 * 3", 6)]
        [TestCase("1 - 4", -3)]
        [TestCase("1 / 8", 0.125)]
        [TestCase("2 ^ 6", 64)]
        [TestCase("16 / 8 + 2 ^ 5 * 2", 66)]
        [TestCase("(1 + 2) * 3", 9)]
        [TestCase("sin(pi/2)", 1)]
        [TestCase("cos(pi)", -1)]
        [TestCase("tan(pi/4)", 1)]
        [TestCase("log10(10)", 1)]
        [TestCase("ln(e)", 1)]
        [TestCase("logn(2, 2)", 1)]
        [TestCase("sqrt(9)", 3)]
        [TestCase("abs(-1)", 1)]
        [TestCase("abs(1)", 1)]
        [TestCase("acos(-1)", Math.PI)]
        [TestCase("asin(1)", Math.PI / 2)]
        [TestCase("atan(1)", Math.PI / 4)]
        [TestCase("exp(1)", Math.E)]
        [TestCase("floor(2.99)", 2)]
        [TestCase("ceil(3.1)", 4)]
        [TestCase("ceiling(2.678)", 3)]
        public void TestExpression(string expression, double value)
        {
            ExpressionEvaluator parser = new ExpressionEvaluator();
            parser.Parse(expression);
            parser.Infix2Postfix();
            parser.EvaluatePostfix();
            Assert.That(parser.Error, Is.False);
            Assert.That(parser.Result, Is.EqualTo(value).Within(tolerance));
        }

        /// <summary>
        /// Test functions which take a vector and return a scalar.
        /// </summary>
        /// <param name="value">Expected result.</param>
        /// <param name="expression">Expression to be parsed.</param>
        /// <param name="inputs">Inputs to the function.</param>
        [TestCase(0.5, "mean(x)", 0, 1)]
        [TestCase(14, "sum(x)", 1, 2, 4, 7)]
        [TestCase(8, "sum(x)", 8)]
        [TestCase(3, "subtract(x)", 6, 1, 2)]
        [TestCase(15, "subtract(x)", 15)]
        [TestCase(120, "multiply(x)", 1, 2, 3, 4, 5)]
        [TestCase(4, "multiply(x)", 4)]
        [TestCase(0.125, "divide(x)", 1, 4, 2)]
        [TestCase(2, "divide(x)", 2)]
        [TestCase(2, "min(x)", 2, 2.01)]
        [TestCase(34, "min(x)", 34)]
        [TestCase(3, "max(x)", 2.99, 3)]
        [TestCase(44, "max(x)", 44)]
        [TestCase(0.5, "stddev(x)", 0, 1)]
        [TestCase(3, "median(x)", 0, 1, 3, 4.21, 6)]
        public void TestVectorToScalar(double value, string expression, params double[] inputs)
        {
            ExpressionEvaluator parser = new ExpressionEvaluator();
            parser.Parse(expression);
            parser.Infix2Postfix();

            // blech
            List<Symbol> variables = parser.Variables;
            Assert.That(variables.Count, Is.EqualTo(1), "Expression must have exactly 1 symbol");
            Symbol symbol = variables[0];
            symbol.m_values = inputs;
            variables[0] = symbol;
            parser.Variables = variables;

            parser.EvaluatePostfix();
            Assert.That(parser.Error, Is.False);

            Assert.That(parser.Result, Is.EqualTo(value).Within(tolerance));
        }

        [Test]
        public void TestVectorToVectorFunctions()
        {
            // Inputs which return known values for sin/cos.
            double[] inputs = new double[] { 0, 1, 2, 3, 4 };
            double[] trigInputs = new[] { 0, Math.PI / 2, Math.PI, 1.5 * Math.PI, 2 * Math.PI };
            double[] mixedInputs = new double[] { -2, -1, 0, 1, 2 };
            double[] invTrigs = new[] { -1d, 0, 1 };
            double[] floats = new[] { -0.75, -0.25, 0.25, 1.75 };
            Func<double, double[]> getLogNInputs = x => new double[] { 0, Math.Pow(x, 1), Math.Pow(x, 2), Math.Pow(x, 3) };
            TestElementwiseFunction("sin(x)", Math.Sin, trigInputs);
            TestElementwiseFunction("cos(x)", Math.Cos, trigInputs);
            TestElementwiseFunction("tan(x)", Math.Tan, trigInputs);
            TestElementwiseFunction("sinh(x)", Math.Sinh, inputs);
            TestElementwiseFunction("cosh(x)", Math.Cosh, inputs);
            TestElementwiseFunction("tanh(x)", Math.Tanh, inputs);
            TestElementwiseFunction("log10(x)", Math.Log10, getLogNInputs(10));
            TestElementwiseFunction("ln(x)", Math.Log, getLogNInputs(Math.E));
            TestElementwiseFunction("logn(x, 2)", Math.Log2, getLogNInputs(2));
            TestElementwiseFunction("sqrt(x)", Math.Sqrt, inputs);
            TestElementwiseFunction("abs(x)", Math.Abs, mixedInputs);
            TestElementwiseFunction("asin(x)", Math.Asin, invTrigs);
            TestElementwiseFunction("acos(x)", Math.Acos, invTrigs);
            TestElementwiseFunction("atan(x)", Math.Atan, invTrigs);
            TestElementwiseFunction("exp(x)", Math.Exp, inputs);
            TestElementwiseFunction("floor(x)", Math.Floor, floats);
            TestElementwiseFunction("ceil(x)", Math.Ceiling, floats);
            TestElementwiseFunction("ceiling(x)", Math.Ceiling, floats);
        }

        /// <summary>
        /// Ensure that the given expression may be parsed without errors, and that
        /// it returns an array with the given func applied to each input.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <param name="inputs">Inputs to the function.</param>
        /// <param name="func">Function which is expected to be performed element-wise on the inputs.</param>
        public void TestElementwiseFunction(string expression, Func<double, double> func, params double[] inputs)
        {
            ExpressionEvaluator parser = new ExpressionEvaluator();
            parser.Parse(expression);
            parser.Infix2Postfix();

            // blech
            List<Symbol> variables = parser.Variables;
            Symbol symbol;
            if (variables.Count == 1)
                symbol = variables[0];
            else
            {
                symbol = variables.FirstOrDefault(v => v.m_name == "x");
            }
            symbol.m_values = inputs;
            variables[0] = symbol;
            parser.Variables = variables;

            parser.EvaluatePostfix();
            Assert.That(parser.Error, Is.False);

            Assert.That(parser.Results, Is.Not.Null, $"ExpressionEvaluator returned a scalar for {expression}");
            Assert.That(parser.Results.Length, Is.EqualTo(inputs.Length));
            for (uint i = 0; i < inputs.Length; i++)
                Assert.That(parser.Results[i], Is.EqualTo(func(inputs[i])).Within(tolerance), $"{expression.Replace("(x)", $"({inputs[i]})")}: incorrect return value");
        }
    }
}
