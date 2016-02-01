!
module solprops
 ! Dummy module for solute parameters.
 implicit none
 private
 save
 public::bd,dis,isotype,isopar ! soil solute parameters
 public::isosub ! routine
 character(len=2),dimension(:,:),allocatable::isotype
 real,allocatable::bd(:),dis(:)
 type rapointer
   real,dimension(:),pointer::p
 end type rapointer
 type(rapointer),allocatable::isopar(:,:)
 ! Module for solute parameters
 ! bd(:)       - bulk densities for soil types.
 ! dis(:)      - dispersivities for soil types.
 ! isotype(:)  - adsorption isotherm code for soil types.
 ! isopar(:,:) - adsorption isotherm params for soil types.
 !
contains
 subroutine isosub(iso,c,dsmmax,p,f,fd)
  implicit none
  character(len=2),intent(in)::iso
  real,intent(in)::c,dsmmax
  real,dimension(:),intent(inout)::p
  real,intent(out)::f,fd
  ! Subroutine to get adsorbed solute (units/g soil) from concn in soil water
  ! according to chosen isotherm code ("Fr" for Freundlich, "La" for Langmuir
  ! and "Ll" for Langmuir-linear).
  ! Definitions of arguments:
  ! iso    - 2 character code.
  ! c      - concn in soil water.
  ! dsmmax - max solute change per time step.
  ! p(:)   - isotherm parameters.
  ! f      - adsorbed mass/g soil.
  ! fc     - deriv of f wrt c (slope of isotherm curve).
 end subroutine isosub
 !
end module solprops

