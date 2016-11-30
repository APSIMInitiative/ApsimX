!
module sinks
 use soildata, only:he,hofS
 use roots, only: getrex
 ! Example module for extraction of water by roots using sink terms.
 ! The water flow routine accumulates water extraction for each layer.
 implicit none
 private
 save
 public nex
 public::setsinks,wsinks,ssinks ! routines
 integer,parameter::nex=1
 integer::n
 integer,dimension(:),allocatable::jt
 ! Module for sinks of water and solute.
 ! n  - no. of soil layers.
 ! jt - layer type nos.
 !
contains
 !
 subroutine setsinks(n1,jt1)
  implicit none
  integer,intent(in)::n1,jt1(:)
  ! Set sink parameters. Args match variables above.
  !
  n=n1
  if (allocated(jt)) deallocate(jt)
  allocate(jt(n))
  jt=jt1(1:n)
 end subroutine setsinks
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
  integer::i
  real::h(n),hS(n),qwdrip,qwdrn,qwdrnd
  do i=1,n
   if (isat(i)==0) then
    call hofS(min(var(i),1.0),i,h(i),hS(i)) ! root extraction routine needs h
   else
    h(i)=he(i)+var(i)
    hS(i)=0.0
   end if
  end do
  call getrex(h,jt,qwexs(:,1),qwexsd(:,1)) ! extraction rates qwex
  qwexsd(:,1)=qwexsd(:,1)*hS ! derivative wrt S
 end subroutine wsinks
 !
 ! The following is a dummy routine.
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

