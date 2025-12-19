---
title: "Playlist"
draft: false
weight: 100
---

## Playlist (Running specified simulations in a file)

The playlist model can be used to only run specific simulation models in a file.

To do this you can add a playlist model to the `Simulations` model and add the names of the simulations or by using an expression in the provided text field.

### Simple Playlist Usage

To run a single file use a command like:

`Models.exe my_sim_name.apsimx --playlist playlist_name`

### Playlist usage with --apply switch

The Playlist's text property can be changed dynamically using the `--apply` switch. This allows users to selectively run specific `Simulation`'s in an apsimx file.

Here is an example config file (commands.txt) that will run just Simulation named Simulation1 in an apsimx file (example.apsimx) that has two Simulation one named Simulation and another named Simulation1:

```
load example.apsimx
add [Simulations] Playlist
[Playlist].Text="Simulation1"
save sim1_example.apsimx
load sim1_example.apsimx
run
```

You'd run this with the command:

```Models.exe --apply commands.txt --playlist playlist```


Additional details are included below on its usage:

Enter a list of names of simulations that you want to run. Case insensitive.

>A wildcard * can be used to represent any number of characters.
>A wildcard # can be used to represent any single character.
>
>Simulations and Experiments can also be added to this playlist by right-clicking on them in the GUI.
>
>Examples:
>
>**Sim1, Sim2, Sim3**   - *Runs simulations with exactly these names*
>
>**[Sim1, Sim2, Sim3]** - *Also allows [ ] around the entry*
>
>**Sim1**  - *Entries can be entered over multiple lines*
>**Sim2**
>
>**Sim#**  - *Runs simulations like Sim1, SimA, Simm, but will not run Sim or Sim11*
>
>**Sim\***  - *Runs simulations that start with Sim*
>
>**\*Sim**  - *Runs simulations that end with Sim*
>
>**\*Sim\*** - *Runs simulations with Sim anywhere in the name*
