!
module soil
 use hyfuns, only:sid,ths,Ks,he,hd,p,Kgiven,Sofh,Sdofh,Kofh,KofhS
 ! Generates soil property tables from functions in module hyfuns.
 ! If K functions not provided, calculates K from the Mualem model
 ! K=Ks*S**p*[(Integral(dS'/h,S'=0,S)/Integral(dS'/h,S'=0,1)]**2) using numerical
 ! integration, with dS=dS/dh*dh/dlnh*dlnh=dS/dh*h*dlnh, where lnh=ln(-h).
 ! See type soilprops.
 ! sid                - soil ident. no.
 ! ths, Ks, he        - soil params. (ths SAT in APSIM, Ks KS in APSIM, he air entry potential)
 ! hd                 - driest h (usually -1e7). (oven dry potential)
 ! p                  - pore interaction param giving S**p factor for K.
 ! Kgiven             - .true. if functions giving K provided, else .false. (false will generate a K curve using Mualem)
 ! Sofh(h)            - function giving S, used if K functions given. (S is relative saturation theta / sat)
 ! Sdofh(h,S,dSdh)    - subroutine giving S and dSdh, used if K calculated.
 ! Kofh(h),KofhS(h,S) - functions giving K.
 ! Work in cm and hours.
 implicit none
 private
 save
 public::rktbl,soilprops,gensptbl,sp
 ! rktbl     - real kind for flux tables; 4 bytes is more than sufficient.
 ! soilprops - derived type definition for properties.
 ! gensptbl  - subroutine to generate the property values.
 ! sp        - variable of type soilprops containing the properties.
 integer,parameter::rktbl=selected_real_kind(6,30) ! six sig figs and 10^30 (use 12, 300)
 integer,parameter::nliapprox=70 ! approx no. of log intervals in property table (possible optimisation in soil props table)
 real,parameter::qsmall=1.0e-5 ! smaller fluxes negligible
 real,parameter::vhmax=-10000 ! for vapour - rel humidity > 0.99 at vhmax (matric potential above which ignore vapour flow)
 integer,parameter::nhpd=5 ! no. of h per decade from hd to vhmax
 ! The above parameters can be varied, but the defaults should usually be ok.
 type soilprops
  ! Sd and lnh - 1:nld used for vapour
  ! S, h, K, phi - 1:n n is number of properties
  ! Sc, hc, Kc, phic - 1:nc c stands for cubic 
  ! Kco, phico - 1:3,1:nc-1 co stand for coefficient
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
 type(soilprops),target::sp
 !
contains
 !
 subroutine gensptbl(dzmin)
  implicit none
  real,intent(in)::dzmin
  ! Generates a soil property table for use by other programs.
  ! dzmin - smallest path length to be used; determines phi for smallest flux.
  integer,parameter::nlimax=220 ! max no. of log intervals
  integer i,j,ndry,nli,nli1,n,nc,nld
  real dSdh,hdry,hwet,dlhr,dlh,lhd,phidry,phie,x
  real,dimension(nlimax+3)::h,lhr,K,phi,S
  real,dimension(4)::cco
!
! timing insert ------------------------
integer itime,ntime
real start,now
integer row,col
call cpu_time(start)
ntime=1000
write (*,*) "gensptbl in"
write (*,*) "dzmin: ", dzmin
do itime=1,ntime
! --------------------------------------
!
  ! Find start of significant fluxes.
  hdry=hd ! normally -1e7 cm
  phidry=0.0
  hwet=min(he,-1.0) ! h/hwet used for log spacing
  if (Kgiven) then
   dlh=log(10.0)/3.0 ! three points per decade
   x=exp(-dlh) ! x*x*x=0.1
   h(1)=hdry
   K(1)=Kofh(h(1))
   phi(1)=0.0
   do i=2,nlimax ! should exit well before nlimax
    h(i)=x*h(i-1)
    K(i)=Kofh(h(i))
  ! Get approx. phi by integration using dln(-h).
    phi(i)=phi(i-1)-0.5*(K(i)*h(i)-K(i-1)*h(i-1))*dlh
    if (phi(i)>qsmall*dzmin) exit ! max flux is approx (phi-0)/dzmin
   end do
   if (i>nlimax) then
    write (*,*) "gensptbl: start of significant fluxes not found"
    stop
   end if
   hdry=h(i-1)
   phidry=phi(i-1)
  else
   ! Calculate K and find start of significant fluxes.
   call props(hdry,phidry)
   do i=2,n
    if (phi(i)>qsmall*dzmin) exit
   end do
   if (i>n) then
    write (*,*) "gensptbl: start of significant fluxes not found"
    stop
   end if
   i=i-1
   hdry=h(i)
   phidry=phi(i)
  end if
  ! hdry and phidry are values where significant fluxes start.
  ! Get props.
  call props(hdry,phidry)
  ! Get ln(-h) and S values from dryness to approx -10000 cm.
  ! These needed for vapour flux (rel humidity > 0.99 at -10000 cm).
  ! To have complete S(h) coverage, bridge any gap between -10000 and h(1).
  x=log(-max(vhmax,h(1)))
  lhd=log(-hd)
  dlh=log(10.0)/nhpd ! nhpd points per decade
  nli1=nint((lhd-x)/dlh)
  nld=nli1+1
  nc=1+n/3 ! n-1 has been made divisible by 3
  ! Store single items in sp and allocate storage for arrays.
  sp%sid=sid; sp%nld=nld; sp%n=n; sp%nc=nc
  sp%ths=ths; sp%Ks=ks; sp%he=he; sp%phie=phie
  if (allocated(sp%Sd)) deallocate(sp%Sd,sp%lnh,sp%S,sp%h,sp%K,sp%phi,sp%Sc,sp%hc,sp%Kc,sp%phic,sp%Kco,sp%phico,sp%Sco)
  allocate(sp%S(n),sp%h(n),sp%K(n),sp%phi(n))
  sp%S=S(1:n); sp%h=h(1:n); sp%K=K(1:n); sp%phi=phi(1:n)
  allocate(sp%Sd(nld),sp%lnh(nld))
  ! Store Sd and lnh in sp.
  sp%lnh(1)=lhd
  sp%lnh(2:nld)=lhd-dlh*(/(j,j=1,nli1)/)
  if (Kgiven) then
   sp%Sd(1)=Sofh(hd)
   do j=2,nld
    x=sp%lnh(j)
    sp%Sd(j)=Sofh(-exp(x))
!   write(*,*) "x:", x	
   end do
!   write(*,*) "Sd:", sp%Sd
!   write(*,*) "lnh:", sp%lnh
  else
   call Sdofh(hd,x,dSdh)
   sp%Sd(1)=x
   do j=2,nld
    x=sp%lnh(j)
    call Sdofh(-exp(x),x,dSdh)
    sp%Sd(j)=x
   end do
  end if
  ! Get polynomial coefficients.
  allocate(sp%Sc(nc),sp%hc(nc),sp%Kc(nc),sp%phic(nc),sp%Kco(3,nc-1),sp%phico(3,nc-1),sp%Sco(3,nc-1))
  j=0
  do i=1,n,3
   j=j+1
   sp%Sc(j)=S(i)
   sp%hc(j)=h(i)
   sp%Kc(j)=K(i)
   sp%phic(j)=phi(i)
   if (i==n) exit
   call cuco(phi(i:i+3),K(i:i+3),cco)
   sp%Kco(:,j)=cco(2:4)
   call cuco(S(i:i+3),phi(i:i+3),cco)
   sp%phico(:,j)=cco(2:4)
   call cuco(phi(i:i+3),S(i:i+3),cco)
   sp%Sco(:,j)=cco(2:4)
  end do
!
! timing insert ------------------------
end do
call cpu_time(now)
write (*,*) "---------------------------"
write (*,*) "gensptbl out"
write (*,*) "sid: ", sp%sid
write (*,*) "nld: ", sp%nld
write (*,*) "n: ", sp%n
write (*,*) "nc: ", sp%nc
write (*,*) "ths: ", sp%ths
write (*,*) "ks: ", sp%ks
write (*,*) "he: ", sp%he
write (*,*) "phie: ", sp%phie
write (*,*) "Sd: ", sp%Sd
write (*,*) "lnh: ", sp%lnh
write (*,*) "S: ", sp%S
write (*,*) "h: ", sp%h
write (*,*) "K: ", sp%K
write (*,*) "phi: ", sp%phi
write (*,*) "Sc: ", sp%Sc
write (*,*) "hc: ", sp%hc
write (*,*) "Kc: ", sp%Kc
write (*,*) "phic: ", sp%phic
write (*,*) "Kco: "
do row=1,SIZE(sp%Kco,1)
  write (*,*) (sp%Kco(row, col), col=1,SIZE(sp%Kco,2))
end do
write (*,*) "phico: "
do row=1,SIZE(sp%phico,1)
  write (*,*) (sp%phico(row, col), col=1,SIZE(sp%phico,2))
end do
write (*,*) "Sco: "
do row=1,SIZE(sp%Sco,1)
  write (*,*) (sp%Sco(row, col), col=1,SIZE(sp%Sco,2))
end do
write (*,*)
write (*,*) "hwet: ", hwet
write (*,*) "nlimax: ", nlimax
write (*,*) "lhd: ", lhd
write (*,*) "nli1: ", nli1
write (*,*) "x: ", x
write (*,*) "nld: ", nld
write (*,*) "dlh: ", dlh
write (*,*) "------------------------"

!write (*,*) "time ",(now-start)/ntime
! --------------------------------------
!
 !
 contains
 !
  subroutine props(hdry,phidry)
   implicit none
   real,intent(in)::hdry,phidry
   ! Get arrays of props.
   ! Calculate K if not given.
   integer i,j
   real,dimension(200)::g,dSdhg
   j=2*(nliapprox/6) ! an even no.
   nli=3*j ! nli divisible by 2 (for integrations) and 3 (for cubic coeffs)
   if (he>hwet) nli=3*(j+1)-1 ! to allow for extra points
   dlhr=-log(hdry/hwet)/nli ! even spacing in log(-h)
!   write(*,*) "dlhr:", dlhr
   lhr(1:nli+1)=(/(-i*dlhr,i=nli,0,-1)/)
!   write(*,*) "lhr:", lhr
   h(1:nli+1)=hwet*exp(lhr(1:nli+1))
   if (he>hwet) then ! add extra points
    n=nli+3;
    h(n-1)=0.5*(he+hwet);
    h(n)=he;
   else
    n=nli+1
   end if
   if (Kgiven) then
    do i=1,n
     S(i)=Sofh(h(i))
     K(i)=KofhS(h(i),S(i));
    end do
   else ! calculate relative K by integration using dln(-h)
    do i=1,n
     call Sdofh(h(i),S(i),dSdhg(i))
    end do
    g(1)=0;
    do i=2,nli,2 ! integrate using Simpson's rule
     g(i+1)=g(i-1)+dlhr*(dSdhg(i-1)+4.0*dSdhg(i)+dSdhg(i+1))/3.0;
    end do
    g(2)=0.5*(g(1)+g(3));
    do i=3,nli-1,2
     g(i+1)=g(i-1)+dlhr*(dSdhg(i-1)+4.0*dSdhg(i)+dSdhg(i+1))/3.0;
    end do
    if (he>hwet) then
     g(n)=g(n-2)+(h(n)-h(n-1))*(dSdhg(n-2)/h(n-2)+4.0*dSdhg(n-1)/h(n-1))/3.0;
     g(n-1)=g(n) ! not accurate, but K(n-1) will be discarded
    end if
    K(1:n)=Ks*S(1:n)**p*(g(1:n)/g(n))**2;
   end if
   ! Calculate phi by integration using dln(-h).
   phi(1)=phidry;
   do i=2,nli,2 ! integrate using Simpson's rule
    phi(i+1)=phi(i-1)+dlhr*(K(i-1)*h(i-1)+4.0*K(i)*h(i)+K(i+1)*h(i+1))/3.0;
   end do
   phi(2)=0.5*(phi(1)+phi(3));
   do i=3,nli-1,2
    phi(i+1)=phi(i-1)+dlhr*(K(i-1)*h(i-1)+4.0*K(i)*h(i)+K(i+1)*h(i+1))/3.0;
   end do
   if (he>hwet) then ! drop unwanted point
    phi(n-1)=phi(n-2)+(h(n)-h(n-1))*(K(n-2)+4.0*K(n-1)+K(n))/3.0;
    h(n-1)=h(n)
    S(n-1)=S(n)
    K(n-1)=K(n)
    n=n-1
   end if
   phie=phi(n)
!   write(*,*) "phi:", phi
  end subroutine props
  !
  subroutine cuco(x,y,co)
   implicit none
   real,intent(in)::x(4),y(4)
   real, intent(out)::co(4)
   ! Get coeffs of cubic through (x,y)
   real s,x1,x2,y3,x12,x13,x22,x23,a1,a2,a3,b1,b2,b3,c1,c2,c3
   s=1.0/(x(4)-x(1))
   x1=s*(x(2)-x(1))
   x2=s*(x(3)-x(1))
   y3=y(4)-y(1)
   x12=x1*x1
   x13=x1*x12
   x22=x2*x2
   x23=x2*x22
   a1=x1-x13
   a2=x12-x13
   a3=y(2)-y(1)-x13*y3
   b1=x2-x23
   b2=x22-x23
   b3=y(3)-y(1)-x23*y3
   c1=(a3*b2-a2*b3)/(a1*b2-a2*b1)
   c2=(a3-a1*c1)/a2
   c3=y3-c1-c2
   co(1)=y(1)
   co(2)=s*c1
   co(3)=s*s*c2
   co(4)=s*s*s*c3
  end subroutine cuco
  !
 end subroutine gensptbl
 !
end module soil

