using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public enum ExpressionType 
    {
        /// <summary>The variable</summary>
        Variable,

        /// <summary>The value</summary>
        Value,

        /// <summary>The operator</summary>
        Operator,

        /// <summary>The eval function</summary>
        EvalFunction,

        /// <summary>The result</summary>
        Result,

        /// <summary>The bracket</summary>
        Bracket,

        /// <summary>The comma</summary>
        Comma,

        /// <summary>The error</summary>
        Error 
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct Symbol
    {
        /// <summary>The m_name</summary>
        public string m_name;
        /// <summary>The m_value</summary>
        public double m_value;
        /// <summary>The m_values</summary>
        public double[] m_values;
        /// <summary>The m_type</summary>
        public ExpressionType m_type;
        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return m_name;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="args">The arguments.</param>
    /// <returns></returns>
    public delegate Symbol EvaluateFunctionDelegate(string name, params Object[] args);
    ///<author>Emad Barsoum</author>
    ///<email>ebarsoum@msn.com</email>
    ///<date>March 23, 2002</date>
    ///<copyright>
    ///This code is Copyright to Emad Barsoum, it can be used or changed for free without removing the header
    ///information which is the author name, email and date or refer to this information if any change made. 
    ///</copyright>
    ///<summary>
    ///This class <c>EvalFunction</c> use the transformation from infix notation to postfix notation to evalute most
    ///Mathematic expression, it support most operators (+,-,*,/,%,^), functions from 0 to any number of parameters
    ///and also a user defined function by using delegate, also it support variables in the expression, it will
    ///generate a symbol table that can be updated at run time.
    ///</summary>
    [Serializable]
    public class ExpressionEvaluator
    {
        /// <summary>Gets the result.</summary>
        /// <value>The result.</value>
        public double Result
        {
            get
            {
                return m_result;
            }
        }

        /// <summary>Gets the results.</summary>
        /// <value>The results.</value>
        public double[] Results
        {
            get
            {
                return m_results;
            }
        }

        /// <summary>Gets the equation.</summary>
        /// <value>The equation.</value>
        public ArrayList Equation
        {
            get
            {
                return (ArrayList)m_equation.Clone();
            }
        }
        /// <summary>Gets the postfix.</summary>
        /// <value>The postfix.</value>
        public ArrayList Postfix
        {
            get
            {
                return (ArrayList)m_postfix.Clone();
            }
        }

        /// <summary>Sets the default function evaluation.</summary>
        /// <value>The default function evaluation.</value>
        public EvaluateFunctionDelegate DefaultFunctionEvaluation
        {
            set
            {
                m_defaultFunctionEvaluation = value;
            }
        }

        /// <summary>Gets a value indicating whether this <see cref="ExpressionEvaluator"/> is error.</summary>
        /// <value><c>true</c> if error; otherwise, <c>false</c>.</value>
        public bool Error
        {
            get
            {
                return m_bError;
            }
        }

        /// <summary>Gets the error description.</summary>
        /// <value>The error description.</value>
        public string ErrorDescription
        {
            get
            {
                return m_sErrorDescription;
            }
        }

        /// <summary>Gets or sets the variables.</summary>
        /// <value>The variables.</value>
        public ArrayList Variables
        {
            get
            {
                ArrayList var = new ArrayList();
                foreach (Symbol sym in m_equation)
                {
                    if ((sym.m_type == ExpressionType.Variable) && (!var.Contains(sym)))
                        var.Add(sym);
                }
                return var;
            }
            set
            {
                foreach (Symbol sym in value)
                {
                    for (int i = 0; i < m_postfix.Count; i++)
                    {
                        if ((sym.m_name == ((Symbol)m_postfix[i]).m_name) && (((Symbol)m_postfix[i]).m_type == ExpressionType.Variable))
                        {
                            Symbol sym1 = (Symbol)m_postfix[i];
                            sym1.m_value = sym.m_value;
                            sym1.m_values = sym.m_values;
                            m_postfix[i] = sym1;
                        }
                    }
                }
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.</summary>
        public ExpressionEvaluator()
        { }

        /// <summary>Parses the specified equation.</summary>
        /// <param name="equation">The equation.</param>
        public void Parse(string equation)
        {
            // state 3 - building up a keyword inside temp
            int state = 1;

            // We iterate over the expression character by character, but a phrase
            // or keyword such as [Clock] is one symbol. 
            // This string builder is used to build up a keyword from the expression,
            // which is eventually stored in a symbol.
            StringBuilder temp = new StringBuilder();
            
            Symbol ctSymbol = new Symbol();
            ctSymbol.m_values = null;

            m_bError = false;
            m_sErrorDescription = "None";

            m_equation.Clear();
            m_postfix.Clear();

            //-- Remove all white spaces from the equation string --
            equation = equation.Replace(" ", "");

            for (int i = 0; i < equation.Length; i++)
            {
                switch (state)
                {
                    case 1:
                        if (Char.IsNumber(equation[i]))
                        {
                            state = 2;
                            temp.Append(equation[i]);
                        }
                        else if (Char.IsLetter(equation[i]) || equation[i] == '[' || equation[i] == ']')
                        {
                            state = 3;
                            temp.Append(equation[i]);
                        }
                        else
                        {
                            ctSymbol.m_name = equation[i].ToString();
                            ctSymbol.m_value = 0;
                            switch (ctSymbol.m_name)
                            {
                                case ",":
                                    ctSymbol.m_type = ExpressionType.Comma;
                                    break;
                                case "(":
                                case ")":
                                case "{":
                                case "}":
                                    ctSymbol.m_type = ExpressionType.Bracket;
                                    break;
                                case "-":
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    if (m_equation.Count < 1 || ((Symbol)m_equation[m_equation.Count - 1]).m_type == ExpressionType.Operator || ((Symbol)m_equation[m_equation.Count - 1]).m_name == "(" || ((Symbol)m_equation[m_equation.Count - 1]).m_name == "{")
                                    {
                                        // A minus sign is always unary if it immediately follows another operator or left parenthesis.
                                        // We need to somehow differentiate between unary and binary minus operations.
                                        // I have arbitrarily chosen to use -- for unary minus.
                                        ctSymbol.m_name = "--";
                                    }
                                    break;
                                default:
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    break;
                            }
                            m_equation.Add(ctSymbol);
                        }
                        break;
                    case 2:
                        if (Char.IsNumber(equation[i]) || (equation[i] == '.'))
                            temp.Append(equation[i]);
                        else if (!Char.IsLetter(equation[i]))
                        {
                            state = 1;
                            ctSymbol.m_name = temp.ToString();
                            ctSymbol.m_value = Double.Parse(temp.ToString(), CultureInfo.InvariantCulture);
                            ctSymbol.m_type = ExpressionType.Value;
                            m_equation.Add(ctSymbol);
                            ctSymbol.m_name = equation[i].ToString();
                            ctSymbol.m_value = 0;
                            switch (ctSymbol.m_name)
                            {
                                case ",":
                                    ctSymbol.m_type = ExpressionType.Comma;
                                    break;
                                case "(":
                                case ")":
                                case "{":
                                case "}":
                                    ctSymbol.m_type = ExpressionType.Bracket;
                                    break;
                                case "-":
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    if (m_equation.Count < 1 || ((Symbol)m_equation[m_equation.Count - 1]).m_type == ExpressionType.Operator || ((Symbol)m_equation[m_equation.Count - 1]).m_name == "(" || ((Symbol)m_equation[m_equation.Count - 1]).m_name == "{")
                                    {
                                        // A minus sign is always unary if it immediately follows another operator or left parenthesis.
                                        // We need to somehow differentiate between unary and binary minus operations.
                                        // I have arbitrarily chosen to use -- for unary minus.
                                        ctSymbol.m_name = "--";
                                    }
                                    break;
                                default:
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    break;
                            }
                            m_equation.Add(ctSymbol);
                            temp.Clear();
                        }
                        break;
                    case 3:
                        if (Char.IsLetterOrDigit(equation[i]) || (equation[i] == '.') ||
                            (equation[i] == '[') || (equation[i] == ']') || (equation[i] == ':') || (equation[i] == '_') ||
                            (equation[i] == '(' && equation[i+1] == ')') ||
                            (equation[i] == ')' && equation[i-1] == '('))
                            temp.Append(equation[i]);
                        else
                        {
                            state = 1;
                            ctSymbol.m_name = temp.ToString();
                            ctSymbol.m_value = 0;
                            if (equation[i] == '(')
                                ctSymbol.m_type = ExpressionType.EvalFunction;
                            else
                            {
                                if (ctSymbol.m_name == "pi")
                                    ctSymbol.m_value = Math.PI;
                                else if (ctSymbol.m_name == "e")
                                    ctSymbol.m_value = Math.E;
                                ctSymbol.m_type = ExpressionType.Variable;
                            }
                            m_equation.Add(ctSymbol);
                            ctSymbol.m_name = equation[i].ToString();
                            ctSymbol.m_value = 0;
                            switch (ctSymbol.m_name)
                            {
                                case ",":
                                    ctSymbol.m_type = ExpressionType.Comma;
                                    break;
                                case "(":
                                case ")":
                                case "{":
                                case "}":
                                    ctSymbol.m_type = ExpressionType.Bracket;
                                    break;
                                case "-":
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    if (m_equation.Count < 1 || ((Symbol)m_equation[m_equation.Count - 1]).m_type == ExpressionType.Operator || ((Symbol)m_equation[m_equation.Count - 1]).m_name == "(" || ((Symbol)m_equation[m_equation.Count - 1]).m_name == "{")
                                    {
                                        // A minus sign is always unary if it immediately follows another operator or left parenthesis.
                                        // We need to somehow differentiate between unary and binary minus operations.
                                        // I have arbitrarily chosen to use -- for unary minus.
                                        ctSymbol.m_name = "--";
                                    }
                                    break;
                                default:
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    break;
                            }
                            m_equation.Add(ctSymbol);
                            temp.Clear();
                        }
                        break;
                }
            }
            if (temp.ToString() != "")
            {
                ctSymbol.m_name = temp.ToString();
                if (state == 2)
                {
                    ctSymbol.m_value = Double.Parse(temp.ToString(), CultureInfo.InvariantCulture);
                    ctSymbol.m_type = ExpressionType.Value;
                }
                else
                {
                    if (ctSymbol.m_name == "pi")
                        ctSymbol.m_value = Math.PI;
                    else if (ctSymbol.m_name == "e")
                        ctSymbol.m_value = Math.E;
                    else
                        ctSymbol.m_value = 0;
                    ctSymbol.m_type = ExpressionType.Variable;
                }
                m_equation.Add(ctSymbol);
            }
        }

        /// <summary>Infix2s the postfix.</summary>
        public void Infix2Postfix()
        {
            Symbol tpSym;
            Stack tpStack = new Stack();
            for (int i = 0; i < m_equation.Count; i++)
            {
                Symbol sym = (Symbol)m_equation[i];
                if ((sym.m_type == ExpressionType.Value) || (sym.m_type == ExpressionType.Variable))
                    m_postfix.Add(sym);
                else if ((sym.m_name == "(") || (sym.m_name == "{"))
                    tpStack.Push(sym);
                else if ((sym.m_name == ")") || (sym.m_name == "}"))
                {
                    if (tpStack.Count > 0)
                    {
                        tpSym = (Symbol)tpStack.Pop();
                        while ((tpSym.m_name != "(") && (tpSym.m_name != "{"))
                        {
                            m_postfix.Add(tpSym);
                            tpSym = (Symbol)tpStack.Pop();
                        }
                    }
                }
                else
                {
                    if (tpStack.Count > 0)
                    {
                        tpSym = (Symbol)tpStack.Pop();
                        while ((tpStack.Count != 0) && ((tpSym.m_type == ExpressionType.Operator) || (tpSym.m_type == ExpressionType.EvalFunction) || (tpSym.m_type == ExpressionType.Comma)) && (Precedence(tpSym) >= Precedence(sym)))
                        {
                            m_postfix.Add(tpSym);
                            tpSym = (Symbol)tpStack.Pop();
                        }
                        if (((tpSym.m_type == ExpressionType.Operator) || (tpSym.m_type == ExpressionType.EvalFunction) || (tpSym.m_type == ExpressionType.Comma)) && (Precedence(tpSym) >= Precedence(sym)))
                            m_postfix.Add(tpSym);
                        else
                            tpStack.Push(tpSym);
                    }
                    tpStack.Push(sym);
                }
            }
            while (tpStack.Count > 0)
            {
                tpSym = (Symbol)tpStack.Pop();
                m_postfix.Add(tpSym);
            }
        }

        /// <summary>Evaluates the postfix.</summary>
        public void EvaluatePostfix()
        {
            Symbol tpSym1, tpSym2, tpResult;
            Stack tpStack = new Stack();
            ArrayList fnParam = new ArrayList();
            m_bError = false;
            foreach (Symbol sym in m_postfix)
            {
                if ((sym.m_type == ExpressionType.Value) || (sym.m_type == ExpressionType.Variable) || (sym.m_type == ExpressionType.Result))
                    tpStack.Push(sym);
                else if (sym.m_type == ExpressionType.Operator)
                {
                    tpSym1 = (Symbol)tpStack.Pop();
                    if (tpStack.Count > 0 && sym.m_name != "--")
                        tpSym2 = (Symbol)tpStack.Pop();
                    else
                        tpSym2 = new Symbol();
                    tpResult = Evaluate(tpSym2, sym, tpSym1);
                    if (tpResult.m_type == ExpressionType.Error)
                    {
                        m_bError = true;
                        m_sErrorDescription = tpResult.m_name;
                        return;
                    }
                    tpStack.Push(tpResult);
                }
                else if (sym.m_type == ExpressionType.EvalFunction)
                {
                    fnParam.Clear();
                    tpSym1 = (Symbol)tpStack.Pop();
                    if ((tpSym1.m_type == ExpressionType.Value) || (tpSym1.m_type == ExpressionType.Variable) || (tpSym1.m_type == ExpressionType.Result))
                    {
                        tpResult = EvaluateFunction(sym.m_name, tpSym1);
                        if (tpResult.m_type == ExpressionType.Error)
                        {
                            m_bError = true;
                            m_sErrorDescription = tpResult.m_name;
                            return;
                        }
                        tpStack.Push(tpResult);
                    }
                    else if (tpSym1.m_type == ExpressionType.Comma)
                    {
                        while (tpSym1.m_type == ExpressionType.Comma)
                        {
                            tpSym1 = (Symbol)tpStack.Pop();
                            fnParam.Add(tpSym1);
                            tpSym1 = (Symbol)tpStack.Pop();
                        }
                        fnParam.Add(tpSym1);
                        tpResult = EvaluateFunction(sym.m_name, fnParam.ToArray());
                        if (tpResult.m_type == ExpressionType.Error)
                        {
                            m_bError = true;
                            m_sErrorDescription = tpResult.m_name;
                            return;
                        }
                        tpStack.Push(tpResult);
                    }
                    else
                    {
                        tpStack.Push(tpSym1);
                        tpResult = EvaluateFunction(sym.m_name);
                        if (tpResult.m_type == ExpressionType.Error)
                        {
                            m_bError = true;
                            m_sErrorDescription = tpResult.m_name;
                            return;
                        }
                        tpStack.Push(tpResult);
                    }
                }
            }
            if (tpStack.Count == 1)
            {
                tpResult = (Symbol)tpStack.Pop();
                m_result = tpResult.m_value;
                if (tpResult.m_values != null)
                {
                    m_results = tpResult.m_values;
                }
            }
        }

        /// <summary>Precedences the specified sym.</summary>
        /// <param name="sym">The sym.</param>
        /// <returns></returns>
        /// <remarks>
        /// I give unary minus a higher precedence than multiplication, division,
        /// and exponentiation. e.g.
        /// 
        /// -2^4 = 16, not -16
        /// </remarks>
        protected int Precedence(Symbol sym)
        {
            switch (sym.m_type)
            {
                case ExpressionType.Bracket:
                    return 6;
                case ExpressionType.EvalFunction:
                    return 5;
                case ExpressionType.Comma:
                    return 0;
            }
            switch (sym.m_name)
            {
                case "^":
                    return 3;
                case "--":
                    return 4;
                case "/":
                case "*":
                case "%":
                    return 2;
                case "+":
                case "-":
                    return 1;
            }
            return -1;
        }

        /// <summary>Evaluates the specified sym1.</summary>
        /// <param name="sym1">The sym1.</param>
        /// <param name="opr">The opr.</param>
        /// <param name="sym2">The sym2.</param>
        /// <returns></returns>
        protected Symbol Evaluate(Symbol sym1, Symbol opr, Symbol sym2)
        {
            Symbol result;
            if (opr.m_name == "--")
                result.m_name = "-" + sym2.m_name;
            else
                result.m_name = sym1.m_name + opr.m_name + sym2.m_name;
            result.m_type = ExpressionType.Result;
            result.m_value = 0;
            result.m_values = null;
            switch (opr.m_name)
            {
                case "^":
                    if (sym1.m_values != null)
                    {
                        result.m_values = new double[sym1.m_values.Length];
                        for (int i = 0; i < sym1.m_values.Length; i++)
                            result.m_values[i] = Math.Pow(sym1.m_values[i], sym2.m_value);
                    }
                    else
                        result.m_value = System.Math.Pow(sym1.m_value, sym2.m_value);
                    break;
                case "/":
                    {
                        if (sym1.m_values != null && sym2.m_values != null)
                            result.m_values = MathUtilities.Divide(sym1.m_values, sym2.m_values);
                        else if (sym1.m_values != null)
                            result.m_values = MathUtilities.Divide_Value(sym1.m_values, sym2.m_value);
                        else if (sym2.m_values != null)
                        {
                            result.m_values = new double[sym2.m_values.Length];
                            for (int i = 0; i < result.m_values.Length; i++)
                                result.m_values[i] = MathUtilities.Divide(sym1.m_value, sym2.m_values[i], 0);
                        }
                        else
                        {
                            if (!MathUtilities.FloatsAreEqual(sym2.m_value, 0, 1E-12))
                                result.m_value = sym1.m_value / sym2.m_value;
                            else
                            {
                                result.m_name = "Divide by Zero.";
                                result.m_type = ExpressionType.Error;
                            }
                        }
                        break;
                    }
                case "*":
                    if (sym1.m_values != null && sym2.m_values != null)
                        result.m_values = MathUtilities.Multiply(sym1.m_values, sym2.m_values);
                    else if (sym1.m_values != null)
                        result.m_values = MathUtilities.Multiply_Value(sym1.m_values, sym2.m_value);
                    else if (sym2.m_values != null)
                        result.m_values = MathUtilities.Multiply_Value(sym2.m_values, sym1.m_value);
                    else
                        result.m_value = sym1.m_value * sym2.m_value;
                    break;
                case "%":
                    result.m_value = sym1.m_value % sym2.m_value;
                    break;
                case "+":
                    if (sym1.m_values != null && sym2.m_values != null)
                        result.m_values = MathUtilities.Add(sym1.m_values, sym2.m_values);
                    else if (sym1.m_values != null)
                        result.m_values = MathUtilities.AddValue(sym1.m_values, sym2.m_value);
                    else if (sym2.m_values != null)
                        result.m_values = MathUtilities.AddValue(sym2.m_values, sym1.m_value);
                    else
                        result.m_value = sym1.m_value + sym2.m_value;
                    break;
                case "-":
                    if (sym1.m_values != null && sym2.m_values != null)
                        result.m_values = MathUtilities.Subtract(sym1.m_values, sym2.m_values);
                    else if (sym1.m_values != null)
                        result.m_values = MathUtilities.Subtract_Value(sym1.m_values, sym2.m_value);
                    else if (sym2.m_values != null)
                    {
                        result.m_values = new double[sym2.m_values.Length];
                        for (int i = 0; i < result.m_values.Length; i++)
                            result.m_values[i] = sym1.m_value - sym2.m_values[i];
                    }
                    else
                        result.m_value = sym1.m_value - sym2.m_value;
                    break;
                case "--":
                    if (sym2.m_values != null && sym2.m_values.Length > 0)
                        result.m_values = MathUtilities.Multiply_Value(sym2.m_values, -1);
                    else
                        result.m_value = sym2.m_value * -1;
                    break;
                default:
                    result.m_type = ExpressionType.Error;
                    result.m_name = "Undefine operator: " + opr.m_name + ".";
                    break;
            }
            return result;
        }

        /// <summary>Evaluates the function.</summary>
        /// <param name="name">The name.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        protected Symbol EvaluateFunction(string name, params Object[] args)
        {
            Symbol result;
            result.m_name = "";
            result.m_type = ExpressionType.Result;
            result.m_value = 0;
            result.m_values = null;
            switch (name.ToLower())
            {
                case "value":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values;
                        result.m_value = MathUtilities.Sum(Values);
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "cos":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Cos(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "sin":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Sin(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "tan":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Tan(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "cosh":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Cosh(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "sinh":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Sinh(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "tanh":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Tanh(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "log":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Log10(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "ln":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Log(((Symbol)args[0]).m_value, 2);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "logn":
                    if (args.Length == 2)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + "'" + ((Symbol)args[1]).m_value.ToString() + ")";
                        result.m_value = System.Math.Log(((Symbol)args[0]).m_value, ((Symbol)args[1]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "sqrt":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Sqrt(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "abs":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Abs(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "acos":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Acos(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "asin":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Asin(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "atan":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";
                        result.m_value = System.Math.Atan(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "exp":
                    if (args.Length == 1)
                    {
                        result.m_name = name + "(" + ((Symbol)args[0]).m_value.ToString() + ")";

                        double[] values = ((Symbol)args[0]).m_values;
                        if (values != null && values.Length > 0)
                        {
                            result.m_values = new double[values.Length];
                            for (int i = 0; i < values.Length; i++)
                                result.m_values[i] = System.Math.Exp(values[i]);
                        }
                        else
                            result.m_value = System.Math.Exp(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "mean":
                    if (args.Length == 1)
                    {
                        double[] values = ((Symbol)args[0]).m_values;
                        result.m_value = MathUtilities.Average(values);
                        result.m_name = name;
                        result.m_values = null;
                    }
                    break;
                case "sum":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values;
                        result.m_value = MathUtilities.Sum(Values);
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "subtract":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values; 
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Values[i];
                            else
                                result.m_value -= Values[i];
                        }
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "multiply":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values;
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Values[i];
                            else
                                result.m_value *= Values[i];
                        }
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "divide":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values;
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Values[i];
                            else
                                result.m_value /= Values[i];
                        }
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "min":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values;
                        result.m_value = MathUtilities.Min(Values);
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "max":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        double[] Values = ((Symbol)args[0]).m_values;
                        result.m_value = MathUtilities.Max(Values);
                        result.m_name = name;
                        result.m_values = null;
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                default:
                    if (m_defaultFunctionEvaluation != null)
                        result = m_defaultFunctionEvaluation(name, args);
                    else
                    {
                        result.m_name = "EvalFunction: " + name + ", not found.";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
            }
            return result;
        }

        /// <summary>The M_B error</summary>
        protected bool m_bError = false;
        /// <summary>The M_S error description</summary>
        protected string m_sErrorDescription = "None";
        /// <summary>The m_result</summary>
        protected double m_result = 0;
        /// <summary>The m_results</summary>
        protected double[] m_results = null;
        /// <summary>The m_equation</summary>
        protected ArrayList m_equation = new ArrayList();
        /// <summary>The m_postfix</summary>
        protected ArrayList m_postfix = new ArrayList();
        /// <summary>The m_default function evaluation</summary>
        protected EvaluateFunctionDelegate m_defaultFunctionEvaluation;
    }
}
