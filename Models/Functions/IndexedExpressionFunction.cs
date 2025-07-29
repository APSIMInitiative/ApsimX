using System;
using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Utilities;
using System.Globalization;
using APSIM.Core;

namespace Models.Functions;
/// <summary>
/// An expression to use as part of subdaily interpolation.
/// </summary>
[Serializable]
[ViewName("UserInterface.Views.PropertyView")]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ValidParent(typeof(SubDailyInterpolation))]
public class IndexedExpressionFunction : Model, IIndexedFunction, ILocatorDependency
{
    [NonSerialized] private ILocator locator;

    /// <summary>
    /// The ExpressionEvaluator instance.
    /// </summary>
    private readonly ExpressionEvaluator _ee = new();

    /// <summary>
    /// Index of the index variable within ExpressionEvaluator variable list.
    /// </summary>
    private int _idx = -1;

    /// <summary>
    /// Index variable. Necessary to separate this if setter does additional work.
    /// </summary>
    private string _idxVar;

    /// <summary>
    /// Expression string. Necessary to separate this if setter does additional work.
    /// </summary>
    private string _exprString;

    // Repeated calls to ValueIndexed really slow the model down. We only need
    // to fill once per clock event tick.
    /// <summary>
    /// A list of symbols to be stored between calls to ValueIndexed within the same clock tick.
    /// </summary>
    private List<Symbol> _filled = null;

    /// <summary>
    /// Expression string.
    /// </summary>
    [Description("The expresison.")]
    public string Expression
    {
        get => _exprString;
        set
        {
            _exprString = value;
            _ee.Parse(value.Trim());
            _ee.Infix2Postfix();
            _idx = -1;
        }
    }

    /// <summary>
    /// The index variable.
    /// </summary>
    [Description("Index variable.")]
    public string IndexVariable
    {
        get => _idxVar;
        set
        {
            _idxVar = value;
            _idx = -1;
        }
    }

    /// <summary>Locator supplied by APSIM kernel.</summary>
    public void SetLocator(ILocator locator) => this.locator = locator;

    /// <summary>
    /// Evaluate the expression, with the value of IndexVariable passed as argument.
    /// </summary>
    public double ValueIndexed(double dX)
    {
        if (_filled == null || _idx < 0)
        {
            var varsToFill = _ee.Variables;
            if (_idx < 0)
                _idx = varsToFill.FindIndex((Symbol sym) => sym.m_name.Trim() == IndexVariable.Trim());
            if (_idx < 0)
                throw new Exception($"Cannot find index of {IndexVariable} in {FullPath}. This must appear exactly the same, brackets and all.");
            _filled = [];
            for (int i = 0; i < varsToFill.Count; i++)
            {
                var sym = varsToFill[i];
                if (i == _idx)
                    sym.m_value = dX;
                else
                    sym = FillValue(sym, this, locator);
                _filled.Add(sym);
            }
        }
        else
        {
            var sym = _filled[_idx];
            sym.m_value = dX;
            _filled[_idx] = sym;
        }
        Evaluate(_ee, _filled);
        return _ee.Result;
    }

    [EventSubscribe("StartOfDay")]
    private void OnStartOfDay(object sender, EventArgs e)
    {
        _filled = null;
    }

    /// <summary>
    /// Fills the given symbol with a value (or array of values) found from the model.
    /// </summary>
    /// <param name="sym">The symbol (name will be used to search).</param>
    /// <param name="relativeTo">The model from which to perofm the search relative to.</param>
    /// <param name="locator">Locator instance</param>
    /// <returns>Symbol with the value filled in.</returns>
    /// <exception cref="Exception">If the value cannot be found.</exception>
    private static Symbol FillValue(Symbol sym, Model relativeTo, ILocator locator)
    {
        var something = locator.Get(sym.m_name.Trim());
        if (something == null)
            throw new Exception($"Cannot find variable {sym.m_name} in {relativeTo.FullPath}");
        if (something is Array arr)
        {
            sym.m_values = new double[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var val = Convert.ToDouble(arr.GetValue(i), CultureInfo.InvariantCulture);
                if (double.IsNaN(val))
                    throw new Exception($"Value at index {i} in {sym.m_name} is NaN in function {relativeTo.FullPath}!");
                sym.m_values[i] = val;
            }
        }
        else if (something is IFunction fn)
            sym.m_value = fn.Value();
        else
        {
            sym.m_value = Convert.ToDouble(something, CultureInfo.InvariantCulture);
            if (double.IsNaN(sym.m_value))
                throw new Exception($"Value at {sym.m_name} is NaN in function {relativeTo.FullPath}!");
        }
        return sym;
    }

    /// <summary>
    /// Evaluates the given (assumed parsed and initialized) expression evaluator with the given variables.
    /// </summary>
    /// <param name="fn">The expression.</param>
    /// <param name="variables">The variables to use.</param>
    /// <exception cref="Exception">Something went wrong in the expression.</exception>
    private static void Evaluate(ExpressionEvaluator fn, List<Symbol> variables)
    {
        fn.Variables = variables;
        fn.EvaluatePostfix();
        if (fn.Error)
            throw new Exception(fn.ErrorDescription);
    }
}