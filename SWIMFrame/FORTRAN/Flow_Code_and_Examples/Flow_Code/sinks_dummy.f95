!
module sinks
 ! Dummy module for extraction of water and solutes using sink terms.
 implicit none
 private
 save
 public nex
 public::wsinks,ssinks ! routines
 integer,parameter::nex=0
 !
contains
 !
 subroutine wsinks(t,isat,var,qwexs,qwexsd)
  implicit none
  integer,intent(in)::isat(:)
  real,intent(in)::t,var(:)
  real,intent(out)::qwexs(:,:),qwexsd(:,:)
  ! Get layer water extraction rates (cm/h).
  ! Definitions of arguments:
  ! t           - current time.
  ! isat(1:n)   - layer sat (0 for unsat, 1 for sat).
  ! var(1:n)    - layer sat S or head diff h-he.
  ! qwexs(1:n)  - layer extraction rates.
  ! qwexsd(1:n) - partial derivs of qwex wrt S or h.
 end subroutine wsinks
 !
 subroutine ssinks(t,ti,tf,isol,dwexs,c,qsexs,qsexsd)
  implicit none
  integer,intent(in)::isol
  real,intent(in)::t,ti,tf,dwexs(:,:),c(:)
  real,intent(out)::qsexs(:,:),qsexsd(:,:)
  ! Get layer solute extraction rates (mass/h).
  ! Definitions of arguments:
  ! t,ti,tf     - current, initial and final times.
  ! isol        - solute no.
  ! dwexs(1:n)  - water extraction from layers.
  ! c(1:n)      - layer concentrations.
  ! qsexs(1:n)  - layer extraction rates.
  ! qsexsd(1:n) - partial derivs of qsexs wrt c.
 end subroutine ssinks
 !
end module sinks

