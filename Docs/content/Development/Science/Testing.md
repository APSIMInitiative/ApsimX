---
title: "Testing"
draft: false
---

## Validation tests

Model submissions will provide evidence that the model works. This is normally done via validation tests that show predicted with observed data, along with validation statistics. The validation tests need to be accompanied by [memo](/usage/memo) text that describe the experiment and treatments. The validation .apsimx file is also converted to a PDF via auto documentation. 

The auto documentation code will walk through all nodes in the .apsimx file, writing any ‘Memo’ and ‘Graph’ models that it finds. For graphs, the menu option ‘Include in auto-documentation?’ (right click on graph) needs to be checked. This allows the model developer to optionally include validation graphs in the PDF and exclude others.

## Sensibility tests

Sensibility tests will be provided to broaden the validation tests into other GxExM scenarios. This is particularly important when the validation is limited in its scope, in particular for GxExM situations that are thought to be important, but where there is no data. Sensibility tests need to be accompanied by [memo](/usage/memo) text that describes what the sensibility plots show and why the results ‘make sense’.

