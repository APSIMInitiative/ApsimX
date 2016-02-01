!
module sinks
 use soildata, only:he,hofS
 use roots, only: getrex
 ! Example module for extraction of water and solutes using sink terms.
 ! Roots extract water but not solute. Drippers are sources (-ve sinks) of water
 ! and solute while drains are sinks. Here drippers and drains may be in the
 ! same layer, so there are 3 water extraction streams, one for roots, one for
 ! drippers and one for drains. Solute movement is calculated every few
 ! steps of water movement calculation. The water flow and solute transport
 ! routines accumulate separate water and solute extraction for each layer for
 ! each water stream.
 implicit none
 private
 save
 public nex,drip ! set true for drippers on
 public::setsinks,wsinks,ssinks ! routines
 integer,parameter::nex=3
 logical::drip
 integer::idrip,idrn,n,ns
 real::dcond,driprate
 integer,dimension(:),allocatable::jt
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
 ! dwdrip   - cumulative drip water since last solute calcs.
 ! dwdrn    - cumulative drain water since last solute calcs.
 ! qwdrip   - current dripper rate.
 ! qwdrn    - current drainage rate.
 ! qwdrnd   - derivative of drainage rate.
 ! wdrip    - cumulative drip water.
 ! wdrn     - cumulative drain water.
 ! jt       - layer type nos.
 ! dripsol  - solute concentrations in dripper water.
 !
contains
 !
 subroutine setsinks(n1,ns1,idrip1,idrn1,driprate1,dripsol1,dcond1,jt1)
  implicit none
  integer,intent(in)::n1,ns1,idrip1,idrn1,jt1(:)
  real,intent(in)::driprate1,dripsol1(:),dcond1
  ! Set sink parameters. Args match variables above.
  !
  n=n1; ns=ns1; idrip=idrip1; idrn=idrn1
  driprate=driprate1; dcond=dcond1
  if (allocated(dripsol)) deallocate(dripsol,jt)
  allocate(dripsol(ns),jt(n))
  dripsol=dripsol1(1:ns)
  jt=jt1(1:n)
  drip=.false. ! set drippers off
 end subroutine setsinks
 !
 subroutine wsinks(t,isat,var,qwexs,qwexsd)
  implicit none
  integer,intent(in)::isat(:)
  real,intent(in)::t,var(:)
  real,intent(out)::qwexs(:,:),qwexsd(:,:)
  ! Get layer water extraction rates (cm/h).
  ! Definitions of arguments:
  ! t             - current time.
  ! isat(n)       - layer sat (0 for unsat, 1 for sat).
  ! var(n)        - layer sat S or head diff h-he.
  ! qwexs(n,nex)  - layer extraction rates.
  ! qwexsd(n,nex) - partial derivs of qwex wrt S or h.
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
  if (drip) then
   qwdrip=-driprate ! current drip rate, -ve because drippers are a source
  else
   qwdrip=0.0
  end if
  qwexs(idrip,2)=qwdrip
  if (isat(idrn)/=0.and.var(idrn)+he(idrn)>0.0) then
   qwdrn=dcond*(var(idrn)+he(idrn)) ! current drainage rate
   qwdrnd=dcond ! current derivative
  else
   qwdrn=0.0
   qwdrnd=0.0
  end if
  qwexs(idrn,3)=qwdrn
  qwexsd(idrn,3)=qwdrnd
 end subroutine wsinks
 !
 subroutine ssinks(t,ti,tf,isol,dwexs,c,qsexs,qsexsd)
  implicit none
  integer,intent(in)::isol
  real,intent(in)::t,ti,tf,dwexs(:,:),c(:)
  real,intent(out)::qsexs(:,:),qsexsd(:,:)
  ! Get layer solute extraction rates (mass/h).
  ! Definitions of arguments:
  ! t,ti,tf       - current, initial and final times.
  ! isol          - solute no.
  ! dwexs(n,nex)  - water extraction from layers.
  ! c(n)          - layer concentrations.
  ! qsexs(n,nex)  - layer extraction rates.
  ! qsexsd(n,nex) - partial derivs of qsex wrt c.
  qsexs(idrip,2)=dripsol(isol)*dwexs(idrip,2)/(tf-ti) ! dripper solute
  qsexs(idrn,3)=c(idrn)*dwexs(idrn,3)/(tf-ti) ! solute in drainage
  qsexsd(idrn,3)=dwexs(idrn,3)/(tf-ti)
 end subroutine ssinks
 !
end module sinks

