---
title: "Rotation Manager"
draft: false
---

# Rotation Manager

The rotation manager visualizes and helps to implement the logic in a crop rotation. By itself, the rotation manager understands very little of the components with which it is interacting. Instead, it relies on other components (usually manager scripts) for their specific knowledge. An example crop rotation is provided in the RotationManager.apsimx example file.

## States

The general idea is that the rotation will progress through states, usually in a repeating cycle. The sequencer's daily operation is simple:

- Can I do anything?
- If so, do it

The rules/conditions for moving from one state to another are defined in the "Rules" textbox which appears in the lower panel after clicking on an arc.

The actions to be performed are specified in the "Actions" textbox which appears in the lower panel after clicking on an arc.

In the following example, the rotation manager starts in the "initial" state and asks the MaizeManager (a manager script) each day if it is ready to sow a crop. If the manager is ready to sow, then the rotation manager will tell MaizeManager to sow a crop. Note that `CanSow` is a property in the manager script, and as such there are not parentheses required. However `SowCrop()` is a method (function) in the manager script, so parentheses are required for the invocation. Arguments can be passed if required by the method, but they must be primitive types (string, int, double, bool, etc).

![State transition](/images/RotationManager.Transition.png)
![State transition script](/images/RotationManager.Transition.Script.png)

States/nodes can be added by right-clicking an empty area of the rotation manager and clicking "Add Node". Arcs can be added by selecting (clicking on) a node and then right clicking on another node and selecting "Add node from x to y".

## State Transition Algorithm

Each rule/condition must be a boolean or numeric value, and an arc can have multiple rules (one per line). A weight is calculated for the arc as the product of all rules defined on the arc (boolean values are treated as true = 1, false = 0). If the weight is greater than 0, then transition along the arc is possible. If multiple arcs are connected to a state, then the rotation will transition along the arc with the highest weight.

When transitioning to a new stage, each action in the "Actions" textbox (one per line) will be executed. If the action contains parentheses, it is assumed to be a method. If no parentheses exist, the action is assumed to be an event name, and the event will be published.

Additionally, three extra events are published during a state transition, in this order:

- `TransitionFromX`
- `Transition`
- `TransitionToY`

Where `x` is the previous state and `y` is the next state.
