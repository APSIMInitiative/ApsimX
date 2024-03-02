# ApsimX

ApsimX is the next generation of [APSIM](https://www.apsim.info)

* APSIM is an agricultural modelling framework used extensively worldwide.
* It can simulate a wide range of agricultural systems.
* It begins its third decade evolving into an agro-ecosystem framework.

## Licencing Conditions

Use of APSIM source code is provided under the terms and conditions provided by either the General Use Licence or the Special Use Licence.  Use in any way is not permitted unless previously agreed to and currently bound by a licence agreement which can be reviewed on http://www.apsim.info/. The General Use licence can be found [here](https://www.apsim.info/wp-content/uploads/2023/09/APSIM_General_Use_Licence.pdf). The Special Use licence can be found [here](https://www.apsim.info/wp-content/uploads/2023/09/APSIM_Special_Use_Licence.pdf)
Any questions, please email apsim@csiro.au.

## Getting Started

**Hardware required**: 

Any recent PC with a minimum of 8Gb of RAM.

**Software required**:

64-bit version of Microsoft Windows 10, Windows 11, Linux or macOS.

### Installation

Binary releases are available via our [registration system](https://registration.apsim.info).

## OASIS Modifications

OASIS is a UCSC project, funded by a USDA NIFA seed grant, to extend APSIM to a voxelized 3D representation interfaced through a server/Python client pair. This step is preparatory work, before using APSIM as a physics engine for procedural generation of realistic test environments.


### World Representation

We use a ENU-oriented world coordinate system (x-East, y-North, z-Up) to index the voxels of our representation, locating the front, left, bottom voxel at origin. Columns are currently strictly of uniform depth, but taller columns will be implemented through variations of the number of layers in the column.
Boundary voxels, those which touch atmosphere or the edge of the simulated region, will be partitioned from the voxels with full neighbor sets, so that they can be treated as (literal) edge cases when implementing fluid dynamics models on the voxel set.

The generated fields are required to have a rectangular cross-section when viewed from a top-down perspective.

The size of a field and the height of each voxel column will be specifed by the Python client before the APSIM server is launched. Subsequent adjustments of the properties of each voxel, as well as specifications of weather patterns, can be executed via the Python client at runtime.

### Coupled Column Physics Implementation

Our physics model couples soil columns at the surface, only. Subsurface water transport is currently modeled as laterally independent of neighboring voxels (all transport is strictly vertical).
Surface water transport is implemented by passing runoff equally among neighboring surface voxels of equal or lower height (if column heights are nonuniform).

## Contributing

Any individual or organisation (a 3rd party outside of the AI) who uses APSIM must be licensed do so by the AI. On download of APSIM, the terms and conditions of a General Use Licence are agreed to and binds the user.

Intellectual property rights in APSIM are retained by the AI. If a licensee makes any improvements to APSIM, the intellectual property rights to those improvements belong to the AI. This means that the AI can choose to make the improvements - including source code - and these improvements would then be made available to all licensed users. As part of the submission process, you are complying with this term as well as making it available to all licensed users. Any Improvements to APSIM are required to be unencumbered and the contributing party warrants that the IP being contributed does not and will not infringe any third party IPR rights.

Please read our [guide](https://apsimnextgeneration.netlify.app/contribute/).

## Publications 

* [doi:10.1016/j.envsoft.2014.07.009](https://dx.doi.org/10.1016/j.envsoft.2014.07.009)
