using System;
using System.Linq;
using Models.Core;

namespace Models.Functions;

/// <summary>
/// A class that returns 0 until a child function value reaches a threshold value at which point
/// this function returns 1 from that point on.
/// </summary>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
public class WaitForThresholdFunction : Model, IFunction
{
    private bool waiting = true;

    /// <summary>Link to an event service.</summary>
    [Link]
    private readonly IEvent events = null;

    /// <summary>The child function</summary>
    private IFunction childFunction = null;

    /// <summary>The value of the child function after which the function returns 1.</summary>
    [Description("The value of the child function after which the function returns 1.")]
    public double ThresholdValue { get; set; }

    /// <summary>The name of an event that resets this function (can be blank).</summary>
    [Description("The name of an event that resets this function (can be blank).")]
    public string ResetEventName { get; set; }


    /// <summary>Called when [simulation commencing].</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    [EventSubscribe("StartOfSimulation")]
    private void OnStartOfSimulation(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(ResetEventName))
            events.Subscribe(ResetEventName, (o, e) => waiting = true);
    }

    /// <summary>Gets the value.</summary>
    public double Value(int arrayIndex = -1)
    {
        if (childFunction == null)
            childFunction = Children.First() as IFunction;
        if (waiting)
            waiting = childFunction.Value(arrayIndex) < ThresholdValue;

        return waiting ? 0 : 1;
    }
}