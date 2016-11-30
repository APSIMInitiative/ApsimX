!
module fluxes
 use soil, only:rktbl,soilprops,sp
 ! Calculates flux tables given soil properties and path lengths.
 implicit none
 private
 save
 public fluxend,fluxtable,fluxtbl,ft
 type fluxend
  ! sid - soil ident
  ! nfu, nft - no. of fluxes unsat and total
  ! dz - path length
  ! phif(1:nft) - phi values
  integer sid,nfu,nft
  real(rktbl)::dz
  real(rktbl),dimension(:),allocatable::phif
 end type fluxend
 type fluxtable
  ! fend(2) - flux end data
  ! qf(1:fend(1)%nft,1:fend(2)%nft) - flux table
  type(fluxend),dimension(2)::fend
  real(rktbl),dimension(:,:),allocatable::ftable
 end type fluxtable
 type(fluxtable),target::ft
 !
contains
 !
 subroutine fluxtbl(dz)
  implicit none
  real,intent(in)::dz
  ! Generates a flux table for use by other programs.
  ! Assumes soil props available in sp of module soil.
  ! dz - path length.
  integer,parameter::mx=100 ! max no. of phi values
  integer i,j,ni,ns,nt,nu,nit,nfu,nphif,ip,nfs,ii,ie
  integer,dimension(mx)::iphif,ifs
  real,parameter::qsmall=1.0e-5,rerr=1.0e-2,cfac=1.2
  real dh,q1,x,he,Ks,q
  real,dimension(mx)::ah,aK,aphi,hpK,aS
  real,dimension(mx)::phif,re,phii,phii5
  real,dimension(3,mx)::aKco,aphico
  real,dimension(mx,mx)::aq,qf,qi1,qi2,qi3,qi5
  type(fluxend),pointer::pe
!
! timing insert ------------------------
integer itime,ntime
integer row, col
real start,now
call cpu_time(start)
ntime=1000
write (*,*) "dz:", dz
! Write starting state
!write (*,*) "fluxtbl"
!write (*,*) "dz: ", dz
!write (*,*) "nc: ", sp%nc
!write (*,*) "hc: ", sp%hc
!write (*,*) "Kc: ", sp%Kc
!write (*,*) "phic: ", sp%phic
!write (*,*) "Sc: ", sp%Sc
!write (*,*) "Kco: ", sp%Kco
!write (*,*) "phico: ", sp%phico
!write (*,*) "he: ", sp%he
!write (*,*) "Ks: ", sp%Ks
do itime=1,ntime
! --------------------------------------
!

  ! Copy soil prop data required.
  nu=sp%nc
  ah(1:nu)=sp%hc; aK(1:nu)=sp%Kc; aphi(1:nu)=sp%phic; aS(1:nu)=sp%Sc(1:nu)
  aKco(:,1:nu-1)=sp%Kco; aphico(:,1:nu-1)=sp%phico
  he=sp%he; Ks=sp%Ks
  ! Get K values for Simpson's integration rule in subroutine odef.
  do i=1,nu-1
   x=0.5*(aphi(i+1)-aphi(i))
   hpK(i)=aK(i)+x*(aKco(1,i)+x*(aKco(2,i)+x*aKco(3,i)))
  end do
  !write(*,*) "hpk:", hpk
  ! Get fluxes aq(1,:) for values aphi(i) at bottom (wet), aphi(1) at top (dry).
  ! These used to select suitable phi values for flux table.
  nit=0
  aq(1,1)=aK(1) ! q=K here because dphi/dz=0
  dh=2.0 ! for getting phi in saturated region
  q1=(aphi(1)-aphi(2))/dz ! q1 is initial estimate
    write(*,*), "aphi:", q
  aq(1,2)=ssflux(1,2,dz,q1,0.1*rerr) ! get accurate flux
  do j=3,nu+20 ! 20*dh should be far enough for small curvature in (phi,q)
   if (j>nu) then ! part satn - set h, K and phi
    ah(j)=ah(j-1)+dh*(j-nu)
!	write(*,*) "ah", ah(1:nu)
!    write(*,*) "ah(j-1)", ah(j-1)
!	write(*,*) "ah(j)", ah(j)
    aK(j)=Ks
!	write(*,*) "aK(j)", aK(j)
    aphi(j)=aphi(j-1)+Ks*dh*(j-nu)
   end if
   ! get approx q from linear extrapolation
   q1=aq(1,j-1)+(aphi(j)-aphi(j-1))*(aq(1,j-1)-aq(1,j-2))/(aphi(j-1)-aphi(j-2))
   aq(1,j)=ssflux(1,j,dz,q1,0.1*rerr) ! get accurate q
   nt=j; ns=nt-nu
   if (j>nu) then
 !  write(*,*) "aphi:", aphi
 !  write(*,*) "aq:", aq(1,:)  
!	write(*,*) "aphi(j)", aphi(j)
	!write(*,*) "aphi(j-1)", aphi(j-1)
	!write(*,*) "aq(1,j)", aq(1,j)
	!write(*,*) "aq(1,j-1)", aq(1,j-1)
   !write(*,*) "if stement:", -(aphi(j)-aphi(j-1))/(aq(1,j)-aq(1,j-1))
   !write(*,*) "compare:", (1+rerr)*dz
    if (-(aphi(j)-aphi(j-1))/(aq(1,j)-aq(1,j-1))<(1+rerr)*dz) exit
   end if
  end do
  ! Get phi values phif for flux table using curvature of q vs phi.
  ! rerr and cfac determine spacings of phif.
  i=nonlin(nu,aphi(1:nu),aq(1,1:nu),rerr)
!  write(*,*) "aq(1)", aq(1,:)
  write(*,*) "i:", i
  write(*,*) "aphi(1:nu)", aphi(1:nu)
  write(*,*) "aq(1,1:nu)", aq(1,1:nu)
  write(*,*) "check this one-----------------"
  call curv(nu,aphi(1:nu),aq(1,1:nu),re) ! for unsat phi
  write(*,*) "re:", re
  call indices(nu-2,re(nu-2:1:-1),1+nu-i,cfac,nphif,iphif)
  write(*,*) "first iphif", iphif
  write(*,*) "nu", nu
  write(*,*) "re(nu-2:1:-1)", re(nu-2:1:-1)
  iphif(1:nphif)=1+nu-iphif(nphif:1:-1) ! locations of phif in aphi
  call curv(1+ns,aphi(nu:nt),aq(1,nu:nt),re) ! for sat phi 
  call indices(ns-1,re,ns,cfac,nfs,ifs)
  iphif(nphif+1:nphif+nfs-1)=nu-1+ifs(2:nfs)
  write(*,*) "nfs", nfs
  write(*,*) "ifs", ifs
  write(*,*) "iphif", iphif
  write(*,*) "nphif", nphif
  call EXIT(0)
  nfu=nphif ! no. of unsat phif
  nphif=nphif+nfs-1
  phif(1:nphif)=aphi(iphif(1:nphif))
  qf(1,1:nphif)=aq(1,iphif(1:nphif))
  ! Get rest of fluxes
  ! First for lower end wetter
  do j=2,nphif
   do i=2,j
    q1=qf(i-1,j)
    if (ah(iphif(j))-dz<ah(iphif(i))) q1=0.0 ! improve?
    qf(i,j)=ssflux(iphif(i),iphif(j),dz,q1,0.1*rerr)
   end do
  end do
  ! Then for upper end wetter
  do i=2,nphif
   do j=i-1,1,-1
    q1=qf(i,j+1)
    if (j+1==i) q1=q1+(aphi(iphif(i))-aphi(iphif(j)))/dz
!	write(*,*) "i,j", i, j
!	write(*,*) "iphif(i)", iphif(i)
!	write(*,*) "iphif(j)", iphif(j)
!	write(*,*) "dz", dz
!	write(*,*) "q1", q1
    qf(i,j)=ssflux(iphif(i),iphif(j),dz,q1,0.1*rerr)
   end do
  end do
  write(*,*) "qf(1,:)", qf(1,:)
  ! Use of flux table involves only linear interpolation, so gain accuracy
  ! by providing fluxes in between using quadratic interpolation.
  ni=nphif-1
  phii(1:ni)=0.5*(phif(1:ni)+phif(2:nphif))
  do i=1,nphif
   call quadinterp(phif,qf(i,:),nphif,phii,qi1(i,:))
  end do
  do j=1,nphif
   call quadinterp(phif,qf(:,j),nphif,phii,qi2(:,j))
  end do
  do j=1,ni
   call quadinterp(phif,qi1(:,j),nphif,phii,qi3(:,j))
  end do
  ! Put all the fluxes together.
  i=nphif+ni
  write(*,*) "i:", i
  qi5(1:i:2,1:i:2)=qf(1:nphif,1:nphif)
  qi5(1:i:2,2:i:2)=qi1(1:nphif,1:ni)
  qi5(2:i:2,1:i:2)=qi2(1:ni,1:nphif)
  qi5(2:i:2,2:i:2)=qi3(1:ni,1:ni)
  ! Get accurate qi5(j,j)=Kofphi(phii(ip))
  ip=0
  do j=2,i,2
   ip=ip+1
   ii=iphif(ip+1)-1
   do ! Search down to locate phii position for cubic.
    if (aphi(ii)<=phii(ip)) exit
    ii=ii-1
   end do
   x=phii(ip)-aphi(ii)
!   write(*,*) "x:", x
   qi5(j,j)=aK(ii)+x*(aKco(1,ii)+x*(aKco(2,ii)+x*aKco(3,ii)))
  end do
  phii5(1:i:2)=phif(1:nphif)
  phii5(2:i:2)=phii(1:ni)
  write(*,*) "nphif:", nphif
  write(*,*) "ni:", ni
!
! timing insert ------------------------
end do
!write (*,*) nfu,nphif
call cpu_time(now)
!write (*,*) "time ",(now-start)/ntime
! --------------------------------------
!
  ! Assemble flux table
  j=2*nfu-1
  if (allocated(ft%ftable)) then
   deallocate(ft%ftable)
   do ie=1,2
    pe=>ft%fend(ie)
    deallocate(pe%phif)
   end do
  end if
  do ie=1,2
   pe=>ft%fend(ie)
   pe%sid=sp%sid; pe%nfu=j; pe%nft=i; pe%dz=dz
   allocate(pe%phif(i))
   pe%phif=phii5(1:i)
  end do
  allocate(ft%ftable(i,i))
  ft%ftable=qi5(1:i,1:i)
  write (*,*) "flux table:"
do row=1,SIZE(ft%ftable,1)
  write (*,*) (ft%ftable(row, col), col=1,SIZE(ft%ftable,2))
end do
  !
 contains
  !
 subroutine odef(n1,n2,u)
  implicit none
  integer,intent(in)::n1,n2
  real,intent(out)::u(2)
  ! Get z and dz/dq for flux q and phi from aphi(n1) to aphi(n2).
  ! q is global to subroutine fluxtbl.
  integer,parameter::m=4
  integer::np
  real::da(n2-n1+1),db(n2-n1)
  np=n2-n1+1
!  write(*,*), "q:", q
!  write(*,*), "n1:", n1
!  write(*,*), "n2:", n2
!  write(*,*), "hpK:", hpK
!  write(*,*), "aphi:", aphi
1  write(*,*), "aK(n1:n2):", aK(n1:n2)
  da=1.0/(aK(n1:n2)-q)
  write(*,*), "da:", da
  db=1.0/(hpK(n1:n2-1)-q)
  write(*,*), "db:", db
  write(*,*), "hpK:", hpK
  ! apply Simpson's rule
  u(1)=sum((aphi(n1+1:n2)-aphi(n1:n2-1))*(da(1:np-1)+4*db+da(2:np))/6)
  da=da**2
  db=db**2
  u(2)=sum((aphi(n1+1:n2)-aphi(n1:n2-1))*(da(1:np-1)+4*db+da(2:np))/6)
 end subroutine odef
 !
 function ssflux(ia,ib,dz,qin,rerr)
  implicit none
  integer,intent(in)::ia,ib
  real,intent(in)::dz,qin,rerr
  ! Get steady-state flux
  ! ia,ib,iz,dz - table entry (ia,ib,iz) and path length dz
  integer,parameter::maxit=50
  integer::i,it,j,n,n1,n2
  real::dh,dq,ha,hb,Ka,Kb,Ks,q1,q2,qp,ssflux,u(2),u0(2),v1
  !write (*,*) "hab:", ah
  !write (*,*) "Kab:", aK
  !write (*,*) "ia:", ia
  !write (*,*) "ib:", ib
  !write (*,*) "dz:", dz
  !write (*,*) "qin:", qin
  !write (*,*) "rerr:", rerr
  i=ia; j=ib; n=nu
!write (*,*) i,j
  if (i==j) then ! free drainage
   ssflux=aK(i)
write (*,*) "aK(i) ",aK(i),i
   return
  end if
  ha=ah(i); hb=ah(j); Ka=aK(i); Kb=aK(j);
	if (i>=n.and.j>=n) then ! saturated flow
write (*,*) "sat flow ", Ka, ha - hb, dz, Ka*((ha-hb)/dz+1.0)
   ssflux=Ka*((ha-hb)/dz+1.0);
   return
  end if
  ! get bounds q1 and q2
  ! q is global in module
  if (i>j) then
   q1=Ka; q2=1.0e20; q=1.1*Ka;
  else
   if (ha>hb-dz) then
    q1=0.0; q2=Ka; q=0.1*Ka
   else
    q1=-1.0e20; q2=0.0; q=-0.1*Ka
	!write(*,*) "q12:", q1, q2, q
   end if
  end if
  !write (*,*) "Qin:", qin, "q1", q1, "q2", q2
  if (qin<q1 .or. qin>q2) then
   write (*,*) "ssflux: qin ",qin," out of range ",q1,q2
   write (*,*) "at ia, ib = ",ia,ib
  else
   q=qin
  end if
  ! integrate from dry to wet - up to satn
  if (i>j) then
   v1=-dz
   if (i>n) then
    Ks=Ka
    dh=ha-he
    n1=ib; n2=n
   else
    n1=ib; n2=ia
   end if
  else
   v1=dz
   if (j>n) then
    dh=hb-he
    n1=ia; n2=n
   else
    n1=ia; n2=ib
   end if
  end if
  u0=(/0.0,0.0/) ! u(1) is z, u(2) is dz/dq (partial deriv)
  do it=1,maxit ! bounded Newton iterations to get q that gives correct dz
   u=u0
   call odef(n1,n2,u)
   write (*,*) "odef call:", n1, n2 ,q,u(1),u(2)
   if (i>n.or.j>n) then ! add sat solns
    Ks=max(Ka,Kb)
    u(1)=u(1)+Ks*dh/(Ks-q)
    u(2)=u(2)+Ks*dh/(Ks-q)**2
   end if
   dq=(v1-u(1))/u(2) ! delta z / dz/dq
   write(*,*) "v1:", v1
   write(*,*) "u:", u
   write(*,*) "dq:",dq
   qp=q ! save q before updating
   if (dq>0.0) then
    q1=q
    q=q+dq
    if (q>=q2) then
     q=0.5*(q1+q2)
    end if
   else
    q2=q
    q=q+dq
    if (q<=q1) then
     q=0.5*(q1+q2)
    end if
   end if
   ! convergence test - q can be at or near zero
   if (abs(q-qp)<rerr*max(abs(q),Ka).and.abs(u(1)-v1)<rerr*dz.or.&
    abs(q1-q2)<0.01*qsmall) exit
  end do
  if (it>maxit) then
   write (*,*) "ssflux: too many its",ia,ib
  end if
  ssflux=q
 nit=nit+it
 write(*,*) "ssflux:", ssflux
 stop
 end function ssflux
 !
 subroutine curv(n,x,y,c)
  ! get curvature at interior points of (x,y)
  implicit none
  integer,intent(in)::n
  real,intent(in)::x(n),y(n)
  real,intent(out)::c(n-2)
  real s(n-2),yl(n-2)
  s=(y(3:n)-y(1:n-2))/(x(3:n)-x(1:n-2))
  write(*,*) "ySub:", (y(3:n)-y(1:n-2))
  write(*,*) "xSub:", (x(3:n)-x(1:n-2))
  yl=y(1:n-2)+s*(x(2:n-1)-x(1:n-2))
  write(*,*) "div:", y(2:n-1)/yl
  write(*,*) "divsub:", y(2:n-1)/yl-1.0
  c=y(2:n-1)/yl-1.0
  write(*,*) "y(2:n-1)", y(2:n-1)
  write(*,*) "s:", s
  write(*,*) "yl:", yl
  write(*,*) "c:", c 
 end subroutine curv
 !
 function nonlin(n,x,y,re)
  ! get last point where (x,y) deviates from linearity by < re
  implicit none
  integer,intent(in)::n
  real,intent(in)::x(n),y(n),re
  integer nonlin,i
  real s,yl(n-2),are
  nonlin=n
  do i=3,n
   s=(y(i)-y(1))/(x(i)-x(1))
   yl(1:i-2)=y(1)+s*(x(2:i-1)-x(1))
   are=maxval(abs(y(2:i-1)/yl(1:i-2)-1))
   if (are>re) then
    nonlin=i-1;
    exit
   endif
  end do
 end function nonlin
 !
 subroutine indices(n,c,iend,fac,nsel,isel)
  ! get indices of elements selected using curvature
  implicit none
  integer,intent(in)::n,iend
  real,intent(in)::c(n),fac
  integer,intent(out)::nsel,isel(n+2)
  integer i,j,di(n)
  real ac(n)
  ac=abs(c);
  di=nint(fac*maxval(ac)/ac); ! min spacings
write(*,*) "c:", c
write(*,*) "di:", di  
write(*,*) "fac:", fac
  isel(1)=1; i=1; j=1
  do
   if (i>=iend) exit
   i=i+1
   if (i>n) exit
   if (di(i-1)>2.and.di(i)>1) then
    i=i+2 ! don't want points to be any further apart
   elseif (di(i-1)>1) then
    i=i+1
   endif
   j=j+1
   isel(j)=i
  end do
  if (isel(j)<n+2) then
   j=j+1
   isel(j)=n+2
  endif
  nsel=j
 end subroutine indices
 !
 subroutine quadco(x,y,co)
  implicit none
  real,intent(in)::x(:),y(:)
  real, intent(out)::co(:)
  ! Return quadratic interpolation coeffs co.
  real s,x1,y2,x12,c1,c2
  s=1.0/(x(3)-x(1))
  x1=s*(x(2)-x(1))
  y2=y(3)-y(1)
  x12=x1*x1
  c1=(y(2)-y(1)-x12*y2)/(x1-x12)
  c2=y2-c1
  co(1)=y(1)
  co(2)=s*c1
  co(3)=s*s*c2
 end subroutine quadco
 !
 subroutine quadinterp(x,y,n,u,v)
  implicit none
  integer,intent(in)::n
  real,intent(in)::x(:),y(:),u(:)
  real, intent(out)::v(:)
  ! Return v(1:n-1) corresponding to u(1:n-1) using quadratic interpolation.
  integer i,j,k
  real z,co(3)
!  write(*,*) "x:", x
!  write(*,*) "y:", y
!  write(*,*) "n:", n
!  write(*,*) "u:", u
  do k=1,n,2
   i=k
   if (k+2>n) then
    i=n-2
   end if
   call quadco(x(i:i+2),y(i:i+2),co)
   do j=k,i+1
    z=u(j)-x(i)
    v(j)=co(1)+z*(co(2)+z*co(3))
   end do
  end do
!  write(*,*) "v:", v
 end subroutine quadinterp
 !
 end subroutine fluxtbl
 !
end module fluxes

