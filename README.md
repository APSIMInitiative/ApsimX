# ApsimX

ApsimX is the next generation of [APSIM](https://www.apsim.info)

* APSIM is an agricultural modelling framework used extensively worldwide.
* It can simulate a wide range of agricultural systems.
* It begins its third decade evolving into an agro-ecosystem framework.

## Contributing

Please read our [guide](https://apsimnextgeneration.netlify.app/contribute/).

## Overall Software Processes

The below steps detail the full process of submitting an issue all the way through to having code submitting to resolve the issue.

### Step 1 Raising an Issue

* Describe the issue and functionality required
* Emphasis is on “Why” more than “What”  or “How”

### Step 2 Issue Review

* Review for Clarity, Accuracy and Priority
* Allows input from others first
* Avoid unnecessary effort if issue is already resolved or alternative solutions are preferred.

### Step 3 Pull Request

* Must link to a clearly described issue
* May require several iterations
* When development is complete, label as “Ready for Review”

### Step 4 High Level Review

* Label as under high level review
* Check for [style guideline](#style-guidelines) issues
* Check overall design
* Check for availability of tests
* Check against documented [PR Rules](#pull-request-requirements-and-best-practices)
* Depending on review and communication with developer, either
  * Move to next step (and label)
  * Remove “ready for review” label
  * Close pull request

### Step 5 Low Level Review

* Code review
* Review of Tests
* Review of changes in statistics
* Co-pilot review
* Check for inadvertent changes in statistics
* Check for inadvertent changes in code
* Depending on review and communication with developer, either
  * If possible science impacts identified in code review not evident from stats (raise with expert or @ReferencePanel)
  * If no changes in statistics (merge and close)
  * If changes in statistics are deemed minor or acceptable ( notify @ReferencePanel, merge and close)
  * If larger issue, move to step six
  * Close pull request

### Step 6 Reference Panel Review

This step only occurs if required

* Put on the agenda for the next meeting
* Label PR as “Reference Panel Review”
* All changes, code, issue description, discussions, tests, stats etc should all be ready for evaluation.

## Style Guidelines

APSIM followings many of the [Microsoft C Sharp code conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [C Sharp coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) with some variations taking into account some code has been previously ported from others languages such as Fortran. If you intend on contributing code we recommend following these guides and conventions.

## Definition of Science Issue

A science issue is any issue that changes the statistics within the test set.  This assumes that tests are maintained for all model science, requiring that any PR that changes code and not the stats is evaluated for a deficiency of test data.

## Publications

* [doi:10.1016/j.envsoft.2014.07.009](https://dx.doi.org/10.1016/j.envsoft.2014.07.009)
