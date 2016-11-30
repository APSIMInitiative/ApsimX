The main flow code is in flow.f95 while soildata.f95 gets soil data and flux
tables for a given soil profile. Solute properties are handled in solprops.f95.
When solutes are not needed, solprops_dummy.f95 provides a dummy module.
A simple root water extraction model is provided in roots.f95.
Code for various types of water sinks are provided, e.g sinks_r.f95, while
sinks_dummy.f95 is a dummy module if sinks are not needed. See the comments in
the examples for detailed information. The files example1.txt etc. give the
instructions needed to compile the examples, showing the files needed in each
case. Turning off the array bounds checking speeds up execution of the compiled
program substantially. Soil property data and flux tables for the examples are
included in the Example_Code folder.

Disclaimer
This software is supplied ‘as is’ and on the understanding that CSIRO will:
translate it and contribute it to the APSIM Initiative; and (although it has
undergone limited testing) further develop and thoroughly test it before it
becomes part of APSIM. Such development and testing is not possible outside of
the APSIM environment.



