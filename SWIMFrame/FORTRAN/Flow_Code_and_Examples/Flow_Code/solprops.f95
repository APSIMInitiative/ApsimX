module solprops
 implicit none
 private
 save
 public::bd,dis,isotype,isopar ! soil solute parameters
 public::allo,solpar,setiso,isosub ! routines
 character(len=2),dimension(:,:),allocatable::isotype
 real,allocatable::bd(:),dis(:)
 type rapointer
   real,dimension(:),pointer::p
 end type rapointer
 type(rapointer),allocatable::isopar(:,:)
 ! Module for solute parameters
 ! allo        - subroutine to allocate parameter storage.
 ! bd(:)       - bulk densities for soil types.
 ! dis(:)      - dispersivities for soil types.
 ! isotype(:)  - adsorption isotherm code for soil types.
 ! isopar(:,:) - adsorption isotherm params for soil types.
 ! solpar      - subroutine to set soil solute params.
 ! setiso      - subroutine to set soil solute isotherm type and params.
 ! isosub      - subroutine to get adsorbed solute (units/g soil) from concn
 !               in soil water
 !
 contains
 subroutine allo(nt,ns)
 implicit none
 integer,intent(in)::nt,ns
 ! Allocate storage for soil solute parameters. This cannot be
 ! deallocated but it can be reused.
 ! Definitions of arguments:
 ! nt - no. of soil hydraulic property types.
 ! ns - no. of solutes.
 integer::i,j
 allocate(isotype(nt,ns),bd(nt),dis(nt),isopar(nt,ns))
 do i=1,nt
   do j=1,ns
     nullify(isopar(i,j)%p) ! so association can be tested
   end do
 end do
 end subroutine allo
 subroutine solpar(j,bdj,disj)
 implicit none
 integer,intent(in)::j
 real,intent(in)::bdj,disj
 ! Set soil solute property parameters.
 ! Definitions of arguments:
 ! j    - soil type no.
 ! bdj  - soil bulk density.
 ! disj - dispersivity.
 bd(j)=bdj
 dis(j)=disj
 isotype(j,:)="no" ! will be changed if required in sub setiso
 end subroutine solpar
 subroutine setiso(j,isol,isotypeji,isoparji)
 implicit none
 integer,intent(in)::j,isol
 character(len=2),intent(in)::isotypeji
 real,intent(in)::isoparji(:)
 ! Set soil solute adsorption isotherm and parameters.
 ! Definitions of arguments:
 ! j           - soil type no.
 ! isol        - solute no.
 ! isotypeji   - isotherm code.
 ! isoparji(:) - isotherm params.
 integer::np
 isotype(j,isol)=isotypeji
 np=size(isoparji)
 if (associated(isopar(j,isol)%p)) deallocate(isopar(j,isol)%p)
 if (isotypeji=="Fr") then ! add params to avoid singularity at zero
   allocate(isopar(j,isol)%p(np+2))
   isopar(j,isol)%p=0.0
 else
   allocate(isopar(j,isol)%p(np))
 end if
 isopar(j,isol)%p(1:np)=isoparji
 end subroutine setiso
 !
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
  real,parameter::one=1.0
  real::x
  select case (iso)
    case ("Fr")
      if (p(3)==0.0) then ! linearise near zero
        p(3)=(0.01*dsmmax/p(1))**(one/p(2)) ! concn at 0.01*dsmmax
        p(4)=p(1)*p(3)**(p(2)-one) ! slope
      end if
      if (c<p(3)) then
        fd=p(4)
        f=fd*c
      else
        x=p(1)*exp((p(2)-one)*log(c))
        f=x*c
        fd=p(2)*x
      end if
    case ("La")
      x=one/(one+p(2)*c)
      f=p(1)*c*x
      fd=p(1)*(x-p(2)*c*x**2)
    case ("Ll")
      x=one/(one+p(2)*c)
      f=p(1)*c*x+p(3)*c
      fd=p(1)*(x-p(2)*c*x**2)+p(3)
    case default
      write (*,*) "isosub: illegal isotherm type"
      stop
  end select
 end subroutine isosub
 !
end module solprops

