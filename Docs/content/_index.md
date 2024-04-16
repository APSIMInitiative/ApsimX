---
title: "The next generation of APSIM"
draft: false
---

# The next generation of APSIM
 
Since 1991, the Agricultural Production Systems sIMulator (APSIM) has grown from a farming systems framework used by a small number of people, into a large collection of models used by many thousands of modellers internationally. The software consists of many hundreds of thousands of lines of code in 6 different programming languages. The models are connected to each other using a ‘common modelling protocol’. This infrastructure has successfully integrated a diverse range of models but isn’t capable of meeting new computing challenges. For these reasons, the APSIM Initiative has begun developing a next generation of APSIM (dubbed APSIM Next Gen.) that is written from scratch and designed to run on Windows, Linux and OSX.

The new framework incorporates the best of the APSIM 7.x framework. C# was chosen as the programming language and together with MONO and GTK#, the models and user interface run on Windows, LINUX and OSX. The Plant Modelling Framework (PMF), a generic collection of plant building blocks, was ported from the existing APSIM to bring a rapid development pathway for plant models. The user interface look and feel has been retained, but completely rewritten to support new application domains and the PMF. The ability to describe experiments has been added which can also be used for rapidly building factorials of simulations. The ability to write C# and VB.NET scripts to control farm and paddock management has been retained. Finally, all simulation outputs are written to an SQLite database to make it easier and quicker to query, filter and graph outputs.

The software engineering process has also been significantly improved. We have adopted GitHub to host the repository and have built a workflow around it involving feature branches, pull requests for peer-review of code and science reviews for major tasks. We have improved the testing regime and are building validation data sets for all models. These datasets are used to automatically revalidate each model every time there is a change and regression statistics are compared with previously accepted values. This improves the likelihood of detecting unexpected changes to model performance when a developer commits new changes. 

We have also enhanced the way we document all models by auto-generating all documentation from the validation tests and from using reflection to examine comments in the source code. The result is a nicely formatted PDF that describes a model and presents its validation, with regression statistics, graphically.
