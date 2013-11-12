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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility
{
    /// <summary>
    /// </summary>
    /// 
    public enum ExpressionType { Variable, Value, Operator, EvalFunction, Result, Bracket, Comma, Error }
    public struct Symbol
    {
        public string m_name;
        public double m_value;
        public string m_valueString;
        public ExpressionType m_type;
        public override string ToString()
        {
            return m_name;
        }
    }
    public delegate Symbol EvaluateFunctionDelegate(string name, params Object[] args);
    public class ExpressionEvaluator
    {
        public double Result
        {
            get
            {
                return m_result;
            }
        }

        public double[] Results
        {
            get
            {
                return m_results;
            }
        }

        public ArrayList Equation
        {
            get
            {
                return (ArrayList)m_equation.Clone();
            }
        }
        public ArrayList Postfix
        {
            get
            {
                return (ArrayList)m_postfix.Clone();
            }
        }

        public EvaluateFunctionDelegate DefaultFunctionEvaluation
        {
            set
            {
                m_defaultFunctionEvaluation = value;
            }
        }

        public bool Error
        {
            get
            {
                return m_bError;
            }
        }

        public string ErrorDescription
        {
            get
            {
                return m_sErrorDescription;
            }
        }

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
                            sym1.m_valueString = sym.m_valueString;
                            m_postfix[i] = sym1;
                        }
                    }
                }
            }
        }

        public ExpressionEvaluator()
        { }

        public void Parse(string equation)
        {
            int state = 1;
            string temp = "";
            Symbol ctSymbol;
            ctSymbol.m_valueString = "";

            m_bError = false;
            m_sErrorDescription = "None";

            m_equation.Clear();
            m_postfix.Clear();

            int nPos = 0;
            //-- Remove all white spaces from the equation string --
            equation = equation.Trim();
            while ((nPos = equation.IndexOf(' ')) != -1)
                equation = equation.Remove(nPos, 1);

            for (int i = 0; i < equation.Length; i++)
            {
                switch (state)
                {
                    case 1:
                        if (Char.IsNumber(equation[i]))
                        {
                            state = 2;
                            temp += equation[i];
                        }
                        else if (Char.IsLetter(equation[i]))
                        {
                            state = 3;
                            temp += equation[i];
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
                                default:
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    break;
                            }
                            m_equation.Add(ctSymbol);
                        }
                        break;
                    case 2:
                        if ((Char.IsNumber(equation[i])) || (equation[i] == '.'))
                            temp += equation[i];
                        else if (!Char.IsLetter(equation[i]))
                        {
                            state = 1;
                            ctSymbol.m_name = temp;
                            ctSymbol.m_value = Double.Parse(temp);
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
                                default:
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    break;
                            }
                            m_equation.Add(ctSymbol);
                            temp = "";
                        }
                        break;
                    case 3:
                        if (Char.IsLetterOrDigit(equation[i]) || (equation[i] == '.') || (equation[i] == '[') || (equation[i] == ']'))
                            temp += equation[i];
                        else
                        {
                            state = 1;
                            ctSymbol.m_name = temp;
                            ctSymbol.m_value = 0;
                            if (equation[i] == '(')
                                ctSymbol.m_type = ExpressionType.EvalFunction;
                            else
                            {
                                if (ctSymbol.m_name == "pi")
                                    ctSymbol.m_value = System.Math.PI;
                                else if (ctSymbol.m_name == "e")
                                    ctSymbol.m_value = System.Math.E;
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
                                default:
                                    ctSymbol.m_type = ExpressionType.Operator;
                                    break;
                            }
                            m_equation.Add(ctSymbol);
                            temp = "";
                        }
                        break;
                }
            }
            if (temp != "")
            {
                ctSymbol.m_name = temp;
                if (state == 2)
                {
                    ctSymbol.m_value = Double.Parse(temp);
                    ctSymbol.m_type = ExpressionType.Value;
                }
                else
                {
                    if (ctSymbol.m_name == "pi")
                        ctSymbol.m_value = System.Math.PI;
                    else if (ctSymbol.m_name == "e")
                        ctSymbol.m_value = System.Math.E;
                    else
                        ctSymbol.m_value = 0;
                    ctSymbol.m_type = ExpressionType.Variable;
                }
                m_equation.Add(ctSymbol);
            }
        }

        public void Infix2Postfix()
        {
            Symbol tpSym;
            Stack tpStack = new Stack();
            foreach (Symbol sym in m_equation)
            {
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
                    tpSym2 = (Symbol)tpStack.Pop();
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
                if (tpResult.m_valueString != "")
                {
                    List<double> Values = new List<double>();
                    foreach (string Value in tpResult.m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                        Values.Add(Convert.ToDouble(Value));
                    m_results = Values.ToArray();
                }
            }
        }

        protected int Precedence(Symbol sym)
        {
            switch (sym.m_type)
            {
                case ExpressionType.Bracket:
                    return 5;
                case ExpressionType.EvalFunction:
                    return 4;
                case ExpressionType.Comma:
                    return 0;
            }
            switch (sym.m_name)
            {
                case "^":
                    return 3;
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

        protected Symbol Evaluate(Symbol sym1, Symbol opr, Symbol sym2)
        {
            Symbol result;
            result.m_name = sym1.m_name + opr.m_name + sym2.m_name;
            result.m_type = ExpressionType.Result;
            result.m_value = 0;
            result.m_valueString = "";
            switch (opr.m_name)
            {
                case "^":
                    result.m_value = System.Math.Pow(sym1.m_value, sym2.m_value);
                    break;
                case "/":
                    {
                        if (sym2.m_value > 0)
                            result.m_value = sym1.m_value / sym2.m_value;
                        else
                        {
                            result.m_name = "Divide by Zero.";
                            result.m_type = ExpressionType.Error;
                        }
                        break;
                    }
                case "*":
                    result.m_value = sym1.m_value * sym2.m_value;
                    break;
                case "%":
                    result.m_value = sym1.m_value % sym2.m_value;
                    break;
                case "+":
                    result.m_value = sym1.m_value + sym2.m_value;
                    break;
                case "-":
                    result.m_value = sym1.m_value - sym2.m_value;
                    break;
                default:
                    result.m_type = ExpressionType.Error;
                    result.m_name = "Undefine operator: " + opr.m_name + ".";
                    break;
            }
            return result;
        }

        protected Symbol EvaluateFunction(string name, params Object[] args)
        {
            Symbol result;
            result.m_name = "";
            result.m_type = ExpressionType.Result;
            result.m_value = 0;
            result.m_valueString = "";
            switch (name)
            {
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
                        result.m_value = System.Math.Exp(((Symbol)args[0]).m_value);
                    }
                    else
                    {
                        result.m_name = "Invalid number of parameters in: " + name + ".";
                        result.m_type = ExpressionType.Error;
                    }
                    break;
                case "sum":
                    if (args.Length == 1)
                    {
                        result.m_value = ((Symbol)args[0]).m_value;
                        string[] Values = ((Symbol)args[0]).m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string Arg in Values)
                            result.m_value += Convert.ToDouble(Arg);
                        result.m_name = name + "(" + result.m_valueString + ")";
                        result.m_valueString = "";
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
                        string[] Values = ((Symbol)args[0]).m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Convert.ToDouble(Values[i]);
                            else
                                result.m_value -= Convert.ToDouble(Values[i]);
                        }
                        result.m_name = name + "(" + result.m_valueString + ")";
                        result.m_valueString = "";
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
                        string[] Values = ((Symbol)args[0]).m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Convert.ToDouble(Values[i]);
                            else
                                result.m_value *= Convert.ToDouble(Values[i]);
                        }
                        result.m_name = name + "(" + result.m_valueString + ")";
                        result.m_valueString = "";
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
                        string[] Values = ((Symbol)args[0]).m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Convert.ToDouble(Values[i]);
                            else
                                result.m_value /= Convert.ToDouble(Values[i]);
                        }
                        result.m_name = name + "(" + result.m_valueString + ")";
                        result.m_valueString = "";
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
                        string[] Values = ((Symbol)args[0]).m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Convert.ToDouble(Values[i]);
                            else
                                result.m_value = System.Math.Min(result.m_value, Convert.ToDouble(Values[i]));
                        }
                        result.m_name = name + "(" + result.m_valueString + ")";
                        result.m_valueString = "";
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
                        string[] Values = ((Symbol)args[0]).m_valueString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < Values.Length; i++)
                        {
                            if (i == 0)
                                result.m_value = Convert.ToDouble(Values[i]);
                            else
                                result.m_value = System.Math.Max(result.m_value, Convert.ToDouble(Values[i]));
                        }
                        result.m_name = name + "(" + result.m_valueString + ")";
                        result.m_valueString = "";
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

        protected bool m_bError = false;
        protected string m_sErrorDescription = "None";
        protected double m_result = 0;
        protected double[] m_results = null;
        protected ArrayList m_equation = new ArrayList();
        protected ArrayList m_postfix = new ArrayList();
        protected EvaluateFunctionDelegate m_defaultFunctionEvaluation;
    }
}
