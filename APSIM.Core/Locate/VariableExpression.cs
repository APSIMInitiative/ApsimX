using APSIM.Shared.Utilities;

namespace APSIM.Core;


/// <summary>
/// TODO: Update summary.
/// </summary>
internal class VariableExpression : IVariable
{
    /// <summary>
    /// The expression string.
    /// </summary>
    private string expression;

    /// <summary>
    /// An instance of the expression evaluator once the expression has been parsed.
    /// </summary>
    private ExpressionEvaluator fn = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableExpression" /> class.
    /// </summary>
    /// <param name="expression">The string expression</param>
    /// <param name="model">The model</param>
    public VariableExpression(string expression, object model)
    {
        this.expression = expression;
        this.Object = model;

        // Perform initial parsing.
        this.fn = new ExpressionEvaluator();
        this.fn.Parse(this.expression.Trim());
        this.fn.Infix2Postfix();
    }

    /// <summary>
    /// A reference to the variables class so that getting of variable values is possible.
    /// </summary>
    public Object Object { get; set; }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name
    {
        get
        {
            return expression;
        }
    }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    public object Value
    {
        get
        {
            this.FillVariableNames();
            fn.EvaluatePostfix();
            if (fn.Error)
            {
                throw new Exception(fn.ErrorDescription);
            }
            if (fn.Results != null)
            {
                return fn.Results;
            }
            else
            {
                return fn.Result;
            }
        }

        set
        {
            throw new Exception("Cannot set the value of an expression: " + expression);
        }
    }

    /// <summary>
    /// Fill the function variables with names.
    /// </summary>
    private void FillVariableNames()
    {
        List<Symbol> variablesToFill = fn.Variables;
        for (int i = 0; i < variablesToFill.Count; i++)
        {
            Symbol sym = (Symbol)variablesToFill[i];
            sym.m_values = null;
            sym.m_value = 0;
            Node node = (Object as INodeModel).Node;
            VariableComposite sometypeofobject = node.GetObject(sym.m_name.Trim(), LocatorFlags.PropertiesOnly | LocatorFlags.ThrowOnError);
            if (sometypeofobject == null)
                throw new Exception("Cannot find variable: " + sym.m_name + " while evaluating expression: " + expression);

            object objectValue = sometypeofobject.Value;
            if (objectValue == null)
                throw new Exception("Variable " + sym.m_name + " evaluated to NULL in expression: " + expression);

            if (objectValue is double)
                sym.m_value = (double)objectValue;
            else if (objectValue is int)
                sym.m_value = System.Convert.ToDouble(objectValue, System.Globalization.CultureInfo.InvariantCulture);
            else if (objectValue is double[])
            {
                sym.m_values = (double[])objectValue;
            }
            else if (objectValue is double[][])
            {
                double[][] allvalues = objectValue as double[][];
                List<double> singleArrayOfValues = new List<double>();
                foreach (double[] dimension in allvalues)
                    foreach (double value in dimension)
                        singleArrayOfValues.Add(value);
                sym.m_values = (double[])singleArrayOfValues.ToArray();
            }
            //else if (objectValue is IFunction fun)   IS THIS NEEDED? I THINK THE LOCATOR DOES THE .VALUE.
            //    sym.m_value = fun.Value();
            else
            {
                try
                {
                    sym.m_value = System.Convert.ToDouble(objectValue, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (InvalidCastException)
                {
                    throw new Exception($"Don't know how to use type {sometypeofobject.GetType()} of variable {sym.m_name} in an expression!");
                }
            }
            variablesToFill[i] = sym;
        }
        fn.Variables = variablesToFill;
    }

    /// <summary>
    /// Gets the data type of the property
    /// </summary>
    public Type DataType
    {
        get
        {
            return Object.GetType();
        }
    }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable { get { return false; } }
}
