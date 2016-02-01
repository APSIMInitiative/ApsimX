!
! Disclaimer
! This software is supplied ‘as is’ and on the understanding that CSIRO will:
! translate it and contribute it to the APSIM Initiative; and (although it has
! undergone limited testing) further develop and thoroughly test it before it
! becomes part of APSIM. Such development and testing is not possible outside of
! the APSIM environment.
!
module soildata
 ! Gets soil data and flux tables for a given soil profile.
 implicit none
 private
 save
 public n,x,dx,ths,he,gettbls,getq,getK,Sofh,hofS,KofS
 public sp,nsp,ft,nft,soilprops,soilloc,pathloc
 ! n            - no. of soil layers.
 ! x(1:n)       - depths to bottoms of layers.
 ! dx(1:n)      - thicknesses of layers.
 ! ths(1:n)     - water contents of layers at effective saturation (S=1).
 ! he(1:n)      - matric heads of layers at air entry.
 ! gettbls      - subroutine to read soil tables and set up data.
 ! getq         - subroutine to find fluxes and derivs given S and/or matric head.
 ! getK         - subroutine to get conductivity and deriv given S or matric head.
 ! Sofh(h,il)   - function giving saturation S for matric head h for layer il.
 ! hofS(S,il)   - h(S).
 ! KofS(S,il)   - K(S).
 ! sp(1:nsp)    - soilprops variables for no. of soils nsp.
 ! ft(1:nft)    - fluxtable variables for no. of flux tables nft.
 ! soilprops    - derived type definition for properties.
 ! soilloc(1:n) - position of layers 1 to n in soilprops array sp.
 ! pathloc(0:n) - position of paths 0 to n in fluxtable array ft.
 integer,parameter::rktbl=selected_real_kind(6,30)
 type soilprops
  ! Sd and lnh - 1:nld
  ! S, h, K, phi - 1:n
  ! Sc, hc, Kc, phic - 1:nc
  ! Kco, phico - 1:3,1:nc-1
  ! S(1:n:3) <=> Sc(1:nc) etc.
  ! Kco are cubic coeffs for K of phi, phico for phi of S, Sco for S of phi
  ! e.g. K=Kc(i)+x*(Kco(1,i)+x*(Kco(2,i)+x*Kco(3,i))) where x=phi-phic(i)
  ! S(n)=1, h(n)=he, K(n)=Ks, phi(n)=phie
  ! phi is matric flux potential (Kirchhoff transform), used for flux tables
  integer sid,nld,n,nc
  real(rktbl) ths,Ks,he,phie
  real(rktbl),dimension(:),allocatable::Sd,lnh,S,h,K,phi,Sc,hc,Kc,phic
  real(rktbl),dimension(:,:),allocatable::Kco,phico,Sco
 end type soilprops
 type fluxend
  ! sid - soil ident
  ! nfu, nfs, nft - no. of fluxes unsat, sat and total
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
 type pathend
  ! all needed refs to end data
  integer nfu,nft,nld
  real ths,rdS,Ks
  real,dimension(:),pointer::S
  real,dimension(:),pointer::phi
  real,dimension(:),pointer::dphidS
  real,dimension(:),pointer::K
  real,dimension(:),pointer::dKdS
  real(rktbl),dimension(:),pointer::Sd
  real(rktbl),dimension(:),pointer::lnh
  real(rktbl),dimension(:),pointer::phif
 end type pathend
 type fluxpath
  real dz ! total path length
  type(pathend),dimension(2)::pend
  real(rktbl),dimension(:,:),pointer::ftable
 end type fluxpath
 integer::n,nsp,nft
 integer,dimension(:),allocatable::soilloc,pathloc
 integer,dimension(:,:),allocatable::philoc
 real,dimension(:),allocatable::x,dx,ths,he
 type(soilprops),dimension(:),allocatable,target::sp
 type(fluxtable),dimension(:),allocatable,target::ft
 type(fluxpath),dimension(:),allocatable,target::fpath
 type(pathend) pe1,pe2
 integer,parameter::nphi=101
 real,dimension(:),allocatable::rdS
 real,dimension(:,:),allocatable,target::S,phi,dphidS,K,dKdS
 !
contains
 !
 subroutine gettbls(nin,xin,sid)
  implicit none
  integer,intent(in)::nin,sid(nin)
  real,intent(in)::xin(nin)
  ! Reads tables and sets up data.
  ! nin        - no. of layers.
  ! xin        - depths to bottoms of layers.
  ! sid(1:nin) - soil idents of layers.
  real,parameter::small=1.0e-5
  character(4) id,mm
  character(14) ftname(2)
  character(80) sfile
  integer i,j,ns,np,nc,i1
  integer isoil(nin),isid(nin+1,2),jt(0:nin+1),loc(1)
  real dz(nin+1,2),hdx(0:nin+1),x1
  ! Set profile info.
  if (allocated(x)) then
   deallocate(x,dx,soilloc,pathloc,philoc,sp,ths,he)
   deallocate(S,rdS,phi,dphidS,K,dKdS,ft,fpath)
  end if
  n=nin
  allocate(x(n),dx(n-1))
  x=xin; dx=x(2:n)-x(1:n-1)
  ! Set up locations in soil, path, S and phi arrays.
  allocate(soilloc(n),pathloc(0:n),philoc(0:n,2))
  philoc=1
  ! Get ns different soil idents in array isoil.
  ns=0
  do i=1,n
   if (count(isoil(1:ns)==sid(i))==0) then
    ns=ns+1
    isoil(ns)=sid(i)
   end if
   loc=minloc(abs(isoil(1:ns)-sid(i)))
   soilloc(i)=loc(1)
  end do
  ! Get soil idents in array isid and lengths in array dz for the np paths.
  dx=x-eoshift(x,-1)
  hdx=(/0.0,0.5*dx,0.0/) ! half delta x
  jt=(/sid(1),sid,sid(n)/)
  np=0 ! no. of different paths out of possible n+1
  do i=0,n
   isid(np+1,:)=(/jt(i),jt(i+1)/)
   if (jt(i)==jt(i+1)) then ! no interface between types
    dz(np+1,:)=(/hdx(i)+hdx(i+1),0.0/)
   else
    dz(np+1,:)=(/hdx(i),hdx(i+1)/)
   end if
   ! Increment np if this path is new.
   do j=1,np
    if (isid(j,1)/=isid(np+1,1).or.isid(j,2)/=isid(np+1,2)) cycle
    if (sum(abs(dz(j,:)-dz(np+1,:)))<small) exit
   end do
   if (j>np) then
    np=np+1
   end if
   pathloc(i)=j ! store path location
  end do
  ! Read soil props.
  allocate(sp(ns)); nsp=ns
  do i=1,ns
   write (id,'(i4.4)') isoil(i)
   sfile='soil' // id // '.dat'
   open (unit=11,file=sfile,form="unformatted",position="rewind",action="read")
   call readprops(11,sp(i))
   close(11)
  end do
  ! Set ths and he.
  allocate(ths(n),he(n))
  do i=1,n
   ths(i)=sp(soilloc(i))%ths
   he(i)=sp(soilloc(i))%he
  end do
! Set up S and phi arrays to get phi from S.
  allocate(S(nphi,ns),rdS(ns),phi(nphi,ns),dphidS(nphi-1,ns),K(nphi,ns),dKdS(nphi-1,ns))
  do i=1,ns
   nc=sp(i)%nc
   S(:,i)=sp(i)%Sc(1)+(/(j,j=0,nphi-1)/)*(sp(i)%Sc(nc)-sp(i)%Sc(1))/(nphi-1)
   phi(1,i)=sp(i)%phic(1)
   phi(nphi,i)=sp(i)%phic(nc)
   j=1
   do i1=2,nphi-1
    do
     if (S(i1,i)<sp(i)%Sc(j+1)) exit
     j=j+1
    end do
    x1=S(i1,i)-sp(i)%Sc(j)
    phi(i1,i)=sp(i)%phic(j)+x1*(sp(i)%phico(1,j)+x1*(sp(i)%phico(2,j)+x1*sp(i)%phico(3,j)))
    x1=phi(i1,i)-sp(i)%phic(j)
    K(i1,i)=sp(i)%Kc(j)+x1*(sp(i)%Kco(1,j)+x1*(sp(i)%Kco(2,j)+x1*sp(i)%Kco(3,j)))
   end do
   rdS(i)=1.0/(S(2,i)-S(1,i))
   dphidS(:,i)=rdS(i)*(phi(2:nphi,i)-phi(1:nphi-1,i))
   dKdS(:,i)=rdS(i)*(K(2:nphi,i)-K(1:nphi-1,i))
  end do
  ! Read flux tables and form flux paths.
  allocate(ft(np),fpath(np)); nft=np
  do i=1,np
   do j=1,2
    write (id,'(i4.4)') isid(i,j)
    write (mm,'(i4.4)') nint(10.0*dz(i,j))
    ftname(j)='soil' // id // 'dz' // mm
   end do
   if (isid(i,1)==isid(i,2)) then
    sfile=ftname(1) // '.dat'
   else
    sfile=ftname(1) // '_' // ftname(2) // '.dat'
   end if
   open (unit=11,file=sfile,form="unformatted",position="rewind",action="read")
   call readfluxes(11,ft(i))
   ! Set up flux path data.
   j=jsp(ft(i)%fend(1)%sid) ! get soil prop location in isoil list
   pe1%nfu=ft(i)%fend(1)%nfu; pe1%nft=ft(i)%fend(1)%nft; pe1%nld=sp(j)%nld
   pe1%ths=sp(j)%ths; pe1%rdS=rdS(j); pe1%Ks=sp(j)%Ks
   pe1%S=>S(:,j)
   pe1%phi=>phi(:,j)
   pe1%dphidS=>dphidS(:,j)
   pe1%K=>K(:,j)
   pe1%dKdS=>dKdS(:,j)
   pe1%Sd=>sp(j)%Sd
   pe1%lnh=>sp(j)%lnh
   pe1%phif=>ft(i)%fend(1)%phif
   fpath(i)%dz=ft(i)%fend(1)%dz
   if (ft(i)%fend(2)%sid==ft(i)%fend(1)%sid) then
    pe2=pe1
   else ! composite path
    j=jsp(ft(i)%fend(2)%sid)
    pe2%nfu=ft(i)%fend(2)%nfu; pe2%nft=ft(i)%fend(2)%nft; pe2%nld=sp(j)%nld
    pe2%rdS=rdS(j); pe2%Ks=sp(j)%Ks
    pe2%S=>S(:,j)
    pe2%phi=>phi(:,j)
    pe2%dphidS=>dphidS(:,j)
    pe2%K=>K(:,j)
    pe2%dKdS=>dKdS(:,j)
    pe2%Sd=>sp(j)%Sd
    pe2%lnh=>sp(j)%lnh
    pe2%phif=>ft(i)%fend(2)%phif
    fpath(i)%dz=fpath(i)%dz+ft(i)%fend(2)%dz
   end if
   fpath(i)%pend(1)=pe1
   fpath(i)%pend(2)=pe2
   fpath(i)%ftable=>ft(i)%ftable
  end do
  !
 contains
  !
  function jsp(sid)
   integer,intent(in)::sid
   integer jsp
   ! Get soil prop location in isoil list.
   do jsp=1,ns
    if (sid==isoil(jsp)) exit
   end do
   if (jsp>ns) then
    write (*,*) "data for soil ",sid," not found"
    stop
   end if
  end function jsp
 end subroutine gettbls
 !
 subroutine readprops(lun,sp)
  implicit none
  integer,intent(in)::lun
  type(soilprops),intent(out)::sp
  integer n,nld,nc
  !
  read (lun) sp%sid,sp%nld,sp%n,sp%nc
  nld=sp%nld; n=sp%n; nc=sp%nc
  allocate(sp%Sd(nld),sp%lnh(nld),sp%S(n),sp%h(n),sp%K(n),sp%phi(n),sp%Sc(nc),sp%hc(nc),sp%Kc(nc),sp%phic(nc))
  allocate(sp%Kco(3,nc-1),sp%phico(3,nc-1))
  read (lun) sp%ths,sp%Ks,sp%he,sp%phie
  read (lun) sp%Sd,sp%lnh,sp%S,sp%h,sp%K,sp%phi,sp%Sc,sp%hc,sp%Kc,sp%phic
  read (lun) sp%Kco,sp%phico
 end subroutine readprops
 !
 subroutine readfluxes(lun,ft)
  implicit none
  integer,intent(in)::lun
  type(fluxtable),target,intent(out)::ft
  integer ie
  type(fluxend),pointer::pe
  !
  do ie=1,2
   pe=>ft%fend(ie)
   read (lun) pe%sid,pe%nfu,pe%nft,pe%dz
   allocate(pe%phif(pe%nft))
   read (lun) pe%phif
  end do
  allocate(ft%ftable(ft%fend(1)%nft,ft%fend(2)%nft))
  read (lun) ft%ftable
 end subroutine readfluxes
 !
 subroutine getq(iq,is,x,q,qya,qyb)
  implicit none
  integer,intent(in)::iq,is(2)
  real,intent(in)::x(2)
  real,intent(out)::q,qya,qyb
  ! Gets fluxes q and partial derivs qya, qyb from stored tables.
  ! iq       - flux number (0 to n, with 0 and n top and bottom fluxes).
  ! is(1:2)  - satn status (0 if unsat, 1 if sat) of layers above and below.
  ! x(1:2)   - S or h-he of layers above and below.
  ! q        - flux.
  ! qya, qyb - partial derivs of q wrt x(1) and x(2).
  logical vapour
  integer i,j,k(2),i2
  real dlnhdS,lnh,h,hr(2),dhrdS(2),poros(2),cvs,rhow,dv,dv1(2),cv(2),dcvdS(2),vc
  real(rktbl) v,phii,f1,f2,f3,f4,Smin
  real(rktbl),dimension(2)::phix,rdphif,u,omu
  real(rktbl),dimension(:),pointer::phif
  real(rktbl),dimension(:,:),pointer::qf
  type(fluxpath),pointer::path
  type(pathend),pointer::pe
  !
  vapour=.false.
  path=>fpath(pathloc(iq))
  do j=1,2
   pe=>path%pend(j)
   phif=>pe%phif
   if (is(j)==0) then ! end unsaturated
    Smin=pe%S(1)
    v=min(x(j),0.99999)
    ! Set up for vapour flux if needed
    hr(j)=1.0
    dhrdS(j)=0.0
    poros(j)=pe%ths*(1.0-v) ! ############## could use (ths/0.93-th)?
    if (v<pe%Sd(pe%nld)) then ! get rel humidity etc
     vapour=.true.
     i=pe%nld-1
     do ! search down
      if (pe%Sd(i)<=v) exit
      i=i-1
     end do
     dlnhdS=(pe%lnh(i+1)-pe%lnh(i))/(pe%Sd(i+1)-pe%Sd(i))
     lnh=pe%lnh(i)+dlnhdS*(v-pe%Sd(i)) ! linear interp
     h=-exp(lnh)
     hr(j)=exp(7.25e-7*h) ! get rel humidity from h
     dhrdS(j)=7.25e-7*hr(j)*h*dlnhdS
    end if
    ! Get phi for flux table from S using linear interp.
    if (v<Smin) v=Smin
    i=1+int(pe%rdS*(v-Smin))
    phix(j)=pe%dphidS(i)
    phii=pe%phi(i)+phix(j)*(v-pe%S(i))
   else ! end saturated
    ! Set up for vapour flux if needed
    hr(j)=1.0
    dhrdS(j)=0.0
    poros(j)=0.0
    ! Get phi for flux table from h-he (x(j)).
    phix(j)=pe%Ks
    i=pe%nfu
    phii=phif(i)+x(j)*pe%Ks
   end if
   ! Get place in flux table.
   i=philoc(iq,j)
   i2=pe%nft
   if (phii>=phif(i)) then
    do ! search up
     if (phii<phif(i+1)) exit ! found
     if (i+1==i2) exit ! outside array
     i=i+1
    end do
   else
    do ! search down
     i=i-1
     if (i<1) then
      write (*,*) "getq: phi<phif(1) in table"
      stop
     end if
     if (phii>=phif(i)) exit ! found
    end do
   end if
   philoc(iq,j)=i ! save location in phif
   rdphif(j)=1.0_rktbl/(phif(i+1)-phif(i))
   u(j)=rdphif(j)*(phii-phif(i))
   k(j)=i
  end do
  ! Get flux from table.
  qf=>path%ftable
  f1=qf(k(1),k(2)) ! use bilinear interp
  f2=qf(k(1)+1,k(2))
  f3=qf(k(1),k(2)+1)
  f4=qf(k(1)+1,k(2)+1)
  omu=1.0_rktbl-u
  q=omu(1)*omu(2)*f1+u(1)*omu(2)*f2+omu(1)*u(2)*f3+u(1)*u(2)*f4
  qya=phix(1)*rdphif(1)*(omu(2)*(f2-f1)+u(2)*(f4-f3))
  qyb=phix(2)*rdphif(2)*(omu(1)*(f3-f1)+u(1)*(f4-f2))
  if (vapour) then ! add vapour flux
   do j=1,2
    cvs=0.0173e-6 ! to vary with temp
    rhow=0.9982e-3 ! ditto
    dv=864.0 ! ditto
    dv1(j)=0.66*poros(j)*dv/rhow
    cv(j)=hr(j)*cvs
    dcvdS(j)=dhrdS(j)*cvs
   end do
   vc=0.5*(dv1(1)+dv1(2))/path%dz
   q=q+vc*(cv(1)-cv(2))
   qya=qya+vc*dcvdS(1)
   qyb=qyb-vc*dcvdS(2)
  end if
 end subroutine getq
 !
 subroutine getK(iq,is,x,q,qya)
  implicit none
  integer,intent(in)::iq,is
  real,intent(in)::x
  real,intent(out)::q,qya
  ! Gets conductivity and deriv from stored tables.
  ! iq  - flux number (0 to n, with 0 and n top and bottom fluxes).
  ! is  - satn status (0 if unsat, 1 if sat) of layers above and below.
  ! x   - S or h-he of layers above and below.
  ! q   - K for x.
  ! qya - deriv of K wrt x.
  integer i
  real(rktbl) v,Smin
  type(fluxpath),pointer::path
  type(pathend),pointer::pe
  !
  path=>fpath(pathloc(iq))
  pe=>path%pend(1)
  if (is==0) then ! end unsaturated
   Smin=pe%S(1)
   v=min(x,0.99999)
   if (v<Smin) v=Smin
   i=1+int(pe%rdS*(v-Smin))
   qya=pe%dKdS(i)
   q=pe%K(i)+qya*(v-pe%S(i))
  else ! end saturated
   q=pe%Ks
   qya=0.0
  end if
 end subroutine getK
 !
! function Sofh(h,il)
!  implicit none
!  integer,intent(in)::il
!  real,intent(in)::h
!  real  Sofh
!  ! Returns S given h and soil layer no. il.
!  integer i,j
!  real lnh
!  i=soilloc(il)
!  if (h>=sp(i)%he) then
!   Sofh=1.0
!  else if (h>=sp(i)%h(1)) then ! use linear interp in (ln(h),S)
!   j=find(h,sp(i)%h)
!   Sofh=sp(i)%S(j)+(sp(i)%S(j+1)-sp(i)%S(j))*log(h/sp(i)%h(j))/log(sp(i)%h(j+1)/sp(i)%h(j))
!  else
!   lnh=log(-h)
!   if (-lnh>=-sp(i)%lnh(1)) then
!    j=find(-lnh,-sp(i)%lnh)
!    Sofh=sp(i)%Sd(j)+(sp(i)%Sd(j+1)-sp(i)%Sd(j))*(lnh-sp(i)%lnh(j))/(sp(i)%lnh(j+1)-sp(i)%lnh(j))
!   else
!    Sofh=sp(i)%Sd(1)
!   end if
!  end if
! end function Sofh
! !
! function hofS(S,il)
!  implicit none
!  integer,intent(in)::il
!  real,intent(in)::S
!  real  hofS
!  ! Returns h given S and soil layer no. il.
!  integer i,j
!  real lnh
!  i=soilloc(il)
!  if (S>=1.0) then
!   hofS=sp(i)%he
!  else if (S>=sp(i)%S(1)) then ! use linear interp in (S,ln(h))
!   j=find(S,sp(i)%S)
!   lnh=log(-sp(i)%h(j))+log(sp(i)%h(j+1)/sp(i)%h(j))*(S-sp(i)%S(j))/(sp(i)%S(j+1)-sp(i)%S(j))
!   hofS=-exp(lnh)
!  else if (S>=sp(i)%Sd(1)) then ! use linear interp in (S,ln(h))
!   j=find(S,sp(i)%Sd)
!   lnh=sp(i)%lnh(j)+(sp(i)%lnh(j+1)-sp(i)%lnh(j))*(S-sp(i)%Sd(j))/(sp(i)%Sd(j+1)-sp(i)%Sd(j))
!   hofS=-exp(lnh)
!  else
!   hofS=-exp(sp(i)%lnh(1))
!  end if
! end function hofS
! !
! function KofS(S,il)
!  implicit none
!  integer,intent(in)::il
!  real,intent(in)::S
!  real  KofS
!  ! Returns K given S and soil layer no. il.
!  integer i,j
!  real phi,x
!  i=soilloc(il)
!  if (S>=1.0) then
!   KofS=sp(i)%Ks
!  else if (S>=sp(i)%Sc(1)) then ! use cubic interp
!   j=find(S,sp(i)%Sc)
!   x=S-sp(i)%Sc(j)
!   phi=sp(i)%phic(j)+x*(sp(i)%phico(1,j)+x*(sp(i)%phico(2,j)+x*sp(i)%phico(3,j)))
!   x=phi-sp(i)%phic(j)
!   KofS=sp(i)%Kc(j)+x*(sp(i)%Kco(1,j)+x*(sp(i)%Kco(2,j)+x*sp(i)%Kco(3,j)))
!  else
!   KofS=sp(i)%Kc(1)
!  end if
! end function KofS
! !
 subroutine Sofh(h,il,S,Sh)
  implicit none
  integer,intent(in)::il
  real,intent(in)::h
  real,intent(out)::S
  real,intent(out),optional::Sh
  ! Returns S and, if required, Sh given h and soil layer no. il.
  integer i,j
  real d,lnh
  i=soilloc(il)
  if (h>sp(i)%he) then
   S=1.0
   if (present(Sh)) Sh=0.0
  else if (h>=sp(i)%h(1)) then ! use linear interp in (ln(-h),S)
   j=find(h,sp(i)%h)
   d=(sp(i)%S(j+1)-sp(i)%S(j))/log(sp(i)%h(j+1)/sp(i)%h(j))
   S=sp(i)%S(j)+d*log(h/sp(i)%h(j))
   if (present(Sh)) Sh=d/h
  else
   lnh=log(-h)
   if (-lnh>=-sp(i)%lnh(1)) then ! use linear interp in (ln(-h),S)
    j=find(-lnh,-sp(i)%lnh)
    d=(sp(i)%Sd(j+1)-sp(i)%Sd(j))/(sp(i)%lnh(j+1)-sp(i)%lnh(j))
    S=sp(i)%Sd(j)+d*(lnh-sp(i)%lnh(j))
    if (present(Sh)) Sh=d/h
   else
    S=sp(i)%Sd(1)
    if (present(Sh)) Sh=0.0
   end if
  end if
 end subroutine Sofh
 !
 subroutine hofS(S,il,h,hS)
  implicit none
  integer,intent(in)::il
  real,intent(in)::S
  real,intent(out)::h
  real,intent(out),optional::hS
  ! Returns h and, if required, hS given S and soil layer no. il.
  integer i,j
  real d,lnh
  i=soilloc(il)
  if (S>1.0.or.S<0.0) then
   write (*,*) "hofS: S out of range (0,1), S = ",S
   stop
  end if
  if (S>=sp(i)%S(1)) then ! use linear interp in (S,ln(-h))
   j=find(S,sp(i)%S)
   d=log(sp(i)%h(j+1)/sp(i)%h(j))/(sp(i)%S(j+1)-sp(i)%S(j))
   lnh=log(-sp(i)%h(j))+d*(S-sp(i)%S(j))
   h=-exp(lnh)
   if (present(hS)) hS=d*h
  else if (S>=sp(i)%Sd(1)) then ! use linear interp in (S,ln(-h))
   j=find(S,sp(i)%Sd)
   d=(sp(i)%lnh(j+1)-sp(i)%lnh(j))/(sp(i)%Sd(j+1)-sp(i)%Sd(j))
   lnh=sp(i)%lnh(j)+d*(S-sp(i)%Sd(j))
   h=-exp(lnh)
   if (present(hS)) hS=d*h
  end if
 end subroutine hofS
 !
 subroutine KofS(S,il,K,KS)
  implicit none
  integer,intent(in)::il
  real,intent(in)::S
  real,intent(out)::K
  real,intent(out),optional::KS
  ! Returns K and, if required, KS given S and soil layer no. il.
  integer i,j
  real d,Kphi,phi,phiS,x,x1
  i=soilloc(il)
  if (S>1.0.or.S<0.0) then
   write (*,*) "KofS: S out of range (0,1), S = ",S
   stop
  end if
  if (S>=sp(i)%Sc(1)) then ! use cubic interp
   j=find(S,sp(i)%Sc)
   x=S-sp(i)%Sc(j)
   phi=sp(i)%phic(j)+x*(sp(i)%phico(1,j)+x*(sp(i)%phico(2,j)+x*sp(i)%phico(3,j)))
   if (present(KS)) phiS=sp(i)%phico(1,j)+x*(2.0*sp(i)%phico(2,j)+x*3.0*sp(i)%phico(3,j))
   x=phi-sp(i)%phic(j)
   K=sp(i)%Kc(j)+x*(sp(i)%Kco(1,j)+x*(sp(i)%Kco(2,j)+x*sp(i)%Kco(3,j)))
   if (present(KS)) then
    Kphi=sp(i)%Kco(1,j)+x*(2.0*sp(i)%Kco(2,j)+x*3.0*sp(i)%Kco(3,j))
    KS=Kphi*phiS
   end if
  else
   K=sp(i)%Kc(1)
   if (present(KS)) KS=0.0
  end if
 end subroutine KofS
 !
 function find(x,xa)
  implicit none
  real,intent(in)::x
  real(rktbl),intent(in)::xa(:)
  integer find
  ! Returns i where xa(i)<=x<xa(i+1).
  integer i1,i2,im
  i1=1; i2=size(xa)-1
  do ! use bisection
   if (i2-i1<=1) exit
   im=(i1+i2)/2
   if (x>=xa(im)) then
    i1=im
   else
    i2=im
   end if
  end do
  find=i1
 end function find
 !
end module soildata

