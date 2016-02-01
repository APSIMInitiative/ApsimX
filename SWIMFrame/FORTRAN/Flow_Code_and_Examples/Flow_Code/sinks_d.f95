!
module sinks
 use soildata, only:he
 ! Example module for extraction of water and solutes using sink terms.
 ! Drippers are sources (-ve sinks) of water and solute while drains are sinks.
 ! A layer may not have both drippers and drains. Solute movement is calculated
 ! every few steps of water movement calculation. The water flow routine
 ! accumulates total water extraction for each layer while the solute transport
 ! routine calculates solute extraction.
 implicit none
 private
 save
 public nex,drip ! set true for drippers on
 public::setsinks,wsinks,ssinks ! routines
 integer,parameter::nex=1
 logical::drip
 integer::idrip,idrn,n,ns
 real::dcond,driprate
 real,dimension(:),allocatable::dripsol
 ! Module for sinks of water and solute.
 ! nex      - no. of extraction streams.
 ! drip     - true for dipper on.
 ! idrip    - layer no. of dripper.
 ! idrn     - layer no. of drain.
 ! n        - no. of layers.
 ! ns       - no. of solutes.
 ! dcond    - conductance for drain.
 ! driprate - dripper rate.
 ! dripsol  - solute concentrations in dripper water.
 !
contains
 !
 subroutine setsinks(n1,ns1,idrip1,idrn1,driprate1,dripsol1,dcond1)
  implicit none
  integer,intent(in)::n1,ns1,idrip1,idrn1
  real,intent(in)::driprate1,dripsol1(:),dcond1
  ! Set sink parameters. Args match variables above.
  !
  n=n1; ns=ns1; idrip=idrip1; idrn=idrn1
  if (idrip==idrn) then
   write (*,*) "setsinks: can't have dripper and drain in same layer"
   stop
  end if
  driprate=driprate1; dcond=dcond1
  if (allocated(dripsol)) deallocate(dripsol)
  allocate(dripsol(ns))
  dripsol=dripsol1(1:ns)
  drip=.false. ! set drippers off
 end subroutine setsinks
 !
 subroutine wsinks(t,isat,var,qwex,qwexd)
  implicit none
  integer,intent(in)::isat(:)
  real,intent(in)::t,var(:)
  real,intent(out)::qwex(:,:),qwexd(:,:)
  ! Get layer water extraction rates (cm/h).
  ! Definitions of arguments:
  ! t          - current time.
  ! isat(1:n)  - layer sat (0 for unsat, 1 for sat).
  ! var(1:n)   - layer sat S or head diff h-he.
  ! qwex(1:n)  - layer extraction rates.
  ! qwexd(1:n) - partial derivs of qwex wrt S or h.
  if (drip) then
   qwex(idrip,1)=-driprate ! -ve because source
  else
   qwex(idrip,1)=0.0
  end if
  qwexd(idrip,1)=0.0
  if (isat(idrn)/=0.and.var(idrn)+he(idrn)>0.0) then
   qwex(idrn,1)=dcond*(var(idrn)+he(idrn)) ! current drainage rate
   qwexd(idrn,1)=dcond ! current derivative
  else
   qwex(idrn,1)=0.0
   qwexd(idrn,1)=0.0
  end if
 end subroutine wsinks
 !
 subroutine ssinks(t,ti,tf,isol,dwex,c,qsex,qsexd)
  implicit none
  integer,intent(in)::isol
  real,intent(in)::t,ti,tf,dwex(:,:),c(:)
  real,intent(out)::qsex(:,:),qsexd(:,:)
  ! Get layer solute extraction rates (mass/h).
  ! Definitions of arguments:
  ! t,ti,tf - current, initial and final times.
  ! isol - solute no.
  ! dwex(1:n) - water extraction from layers.
  ! c(1:n) - layer concentrations.
  ! qsex(1:n) - layer extraction rates.
  ! qsexd(1:n) - partial derivs of qsex wrt c.
  qsex(idrip,1)=dripsol(isol)*dwex(idrip,1)/(tf-ti) ! dripper solute
  qsex(idrn,1)=c(idrn)*dwex(idrn,1)/(tf-ti) ! solute in drainage
  qsexd(idrn,1)=dwex(idrn,1)/(tf-ti)
 end subroutine ssinks
 !
end module sinks

