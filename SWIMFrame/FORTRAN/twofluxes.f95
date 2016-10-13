!
module twofluxes
 use soil, only:rktbl,soilprops
 use fluxes, only:fluxtable,fluxend
 ! Calculates flux table for path through two soils given tables for each path.
 ! rktbl - real kind for flux tables; 4 bytes is more than sufficient.
 ! soilprops - derived type definition for properties.
 ! fluxtable,fluxend - type definitions.
 implicit none
 private
 save
 public twotbls,ftwo
 ! twotbls - subroutine to generate composite flux table ftwo.
 type(fluxtable),target::ftwo
contains
 subroutine twotbls(ft1,sp1,ft2,sp2)
  implicit none
  type(fluxtable),target,intent(in)::ft1,ft2
  type(soilprops),target,intent(in)::sp1,sp2
  ! Generates a composite flux table from two uniform ones.
  ! Sets up quadratic interpolation table to get phi at interface for lower path
  ! from phi at interface for upper path.
  ! Sets up cubic interpolation tables to get fluxes from phi at interface for
  ! upper and lower paths.
  ! Solves for phi at interface in upper path that gives same fluxes in upper
  ! and lower paths, for all phi at upper and lower ends of composite path.
  ! Increases no. of fluxes in table by quadratic interpolation.
  ! ft1,ft2 - flux tables for upper and lower paths.
  ! sp1,sp2 - soil prop tables for upper and lower paths.
  integer,parameter::mx=100,maxit=20
  integer i,j,k,m,ne,ni,iarray(1),id,ip,nco1,nco2,nit,ie,ii,jj
  integer,dimension(2)::nft,nfu,n,nfi
  real,parameter::rerr=1e-3
  real phi1max,dhe,v,vlast,dx,e,f,df,q1,phialast,v1,v2,f1,f2
  real,dimension(2)::he,phie,Ks
  real,dimension(4)::co
  real,dimension(mx)::xval,phico1,phico2
  real,dimension(3*mx)::y2,hi
  real,dimension(2,mx)::phif,phifi,phii5
  real,dimension(3,3*mx)::coq
  real,dimension(4,mx)::co1
  real,dimension(mx,mx)::qp,y22,qi1,qi2,qi3,qi5
  real,dimension(2,3*mx)::h,phi,phii
  real,dimension(2,mx,mx)::qf,y2q
  real,dimension(4,mx,mx)::co2
  type ftp
   type(fluxtable),pointer::ft
  end type ftp
  type(ftp) ft(2)
  type spp
   type(soilprops),pointer::sp
  end type spp
  type(spp) sp(2)
  type(fluxend),pointer::pe,qe
!
! timing insert ------------------------
integer itime,ntime
real start,now
! --------------------------------------
!
  ! Set up required pointers and data.
  if (ft1%fend(1)%sid/=ft1%fend(2)%sid.or.ft2%fend(1)%sid/=ft2%fend(2)%sid) then
   write (*,*) "flux table not for uniform soil"
   stop
  end if
  ft(1)%ft=>ft1; ft(2)%ft=>ft2
  sp(1)%sp=>sp1; sp(2)%sp=>sp2
  do i=1,2
   n(i)=sp(i)%sp%n
   he(i)=sp(i)%sp%he
   phie(i)=sp(i)%sp%phie
   Ks(i)=sp(i)%sp%Ks
   h(i,1:n(i))=sp(i)%sp%h
   phi(i,1:n(i))=sp(i)%sp%phi
  end do
  ! Discard unwanted input - use original uninterpolated values only.
  do i=1,2
   m=ft(i)%ft%fend(1)%nft ! should be odd
   j=1+m/2
   phif(i,1:j)=ft(i)%ft%fend(1)%phif(1:m:2) ! discard every second
   qf(i,1:j,1:j)=ft(i)%ft%ftable(1:m:2,1:m:2)
   nft(i)=j
   nfu(i)=1+ft(i)%ft%fend(1)%nfu/2 ! ft(i)%ft%fend(1)%nfu should be odd
  end do
  ! Extend phi2 and h2 if he1>he2, or vice-versa.
  dhe=abs(he(1)-he(2))
  if (dhe>0.001) then
   if (he(1)>he(2)) then
    i=1; j=2
   else
    i=2; j=1
   end if
   ii=find(he(j),h(i,1:n(i)),n(i))
   do k=1,n(i)-ii
    h(j,n(j)+k)=h(i,ii+k)
    phi(j,n(j)+k)=phie(j)+Ks(j)*(h(i,ii+k)-he(j))
   end do
   n(j)=n(j)+n(i)-ii
  end if
  phi1max=phi(1,n(1))
!
! timing insert ------------------------
call cpu_time(start)
ntime=1000
do itime=1,ntime
! --------------------------------------
!
  ! Get phi for same h.
  if (h(1,1)>h(2,1)) then
   i=1; j=2
  else
   i=2; j=1
  end if
  iarray=minloc(abs(h(j,1:n(j))-h(i,1)))
  id=iarray(1)
  if (h(j,id)>=h(i,1)) id=id-1
  ! phii(j,:) for soil j will match h(i,:) from soil i and h(j,1:id) from soil j.
  ! phii(i,:) for soil i will match h(i,:) and fill in for h(j,1:id).
  phii(j,1:id)=phi(j,1:id) ! keep these values
  ! But interpolate to match values that start at greater h.
  jj=id+1 ! h(j,id+1) to be checked first
  phii(j,id+n(i))=phi(j,n(j)) ! last h values match
  do ii=1,n(i)-1
   do ! get place of h(i,ii) in h array for soil j
    if (jj>n(j)) then
     write (*,*) "twotbls: h(j,n(j))<=h(i,ii); i,j,ii,n(j) = ",i,j,ii,n(j)
     stop
    end if
    if (h(j,jj)>h(i,ii)) exit
    jj=jj+1
   end do
   k=jj-1 ! first point for cubic interp
   if (jj+2>n(j)) k=n(j)-3 ! off end of table so set k lower
   call cuco(h(j,k:k+3),phi(j,k:k+3),co) ! get cubic coeffs
   v=h(i,ii)-h(j,k)
   phii(j,id+ii)=co(1)+v*(co(2)+v*(co(3)+v*co(4)))
  end do
  ni=id+n(i)
  ! Generate sensible missing values using quadratic extrapolation.
  call quadco(phii(j,(/1,id+1,id+2/)),(/0.0,phi(i,1:2)/),co(:))
  if (co(2)>0.0) then ! +ve slope at zero - ok
   xval(1:id)=phii(j,1:id)-phii(j,1)
   phii(i,1:id)=co(1)+xval(1:id)*(co(2)+xval(1:id)*co(3))
  else ! -ve slope at zero, use quadratic with zero slope at zero
   co(3)=phi(i,1)/phii(j,id+1)**2
   phii(i,1:id)=co(3)*phii(j,1:id)**2
  end if
  phii(i,id+1:ni)=phi(i,1:n(i))
  hi(1:id)=h(j,1:id)
  hi(id+1:ni)=h(i,1:n(i))
  ! hi(1:ni) are h values for the interface tables.
  ! phii(1,1:ni) are corresponding interface phi values for upper layer.
  ! phii(2,1:ni) are corresponding interface phi values for lower layer.
  ! Set up quadratic interpolation coeffs to get phii2 given phii1.
  do i=1,ni-2
   call quadco(phii(1,i:i+2),phii(2,i:i+2),coq(:,i))
  end do
  call linco(phii(1,ni-1:ni),phii(2,ni-1:ni),coq(:,ni-1)) ! at end
  coq(3,ni-1)=0.0
  ! Set up cubic coeffs to get fluxes q given phi.
  do j=1,nft(2)
   k=1; ip=1
   do
    phico2(k)=phif(2,ip)
    call cuco(phif(2,ip:ip+3),qf(2,ip:ip+3,j),co2(:,k,j))
    ip=ip+3
    if (ip==nft(2)) exit
    if (ip>nft(2)) ip=nft(2)-3
    k=k+1
   end do
  end do
  nco2=k
  ! Get fluxes.
  nit=0
  do i=1,nft(1) ! step through top phis
   vlast=phif(1,i)
   k=1; ip=1
   do
    phico1(k)=phif(1,ip)
    call cuco(phif(1,ip:ip+3),qf(1,i,ip:ip+3),co1(:,k))
    ip=ip+3
    if (ip==nft(1)) exit
    if (ip>nft(1)) ip=nft(1)-3
    k=k+1
   end do
   nco1=k
   do j=1,nft(2) ! bottom phis
    v=vlast
    do k=1,maxit ! solve for upper interface phi giving same fluxes
     call fd(v,f,df,q1)
     nit=nit+1
     dx=f/df ! Newton's method - almost always works
     v=min(10.0*phif(1,nft(1)),max(phii(1,1),v-dx))
     e=abs(f/q1)
     if (e<rerr) exit
     vlast=v
    end do
    if (k>maxit) then ! failed - bracket q and use bisection
     v1=phii(1,1)
     call fd(v1,f1,df,q1)
     if (f1<=0.0) then ! answer is off table - use end value
      qp(i,j)=q1
      cycle
     end if
     v2=phii(1,ni)
     call fd(v2,f2,df,q1)
     do k=1,maxit
      if (f1*f2<0.0) exit
      v1=v2
      f1=f2
      v2=2.0*v1
      call fd(v2,f2,df,q1)
     end do
     if (k>maxit) then
      write (*,*) v1,v2,f1,f2
      v1=phii(1,1)
      call fd(v1,f1,df,q1)
      write (*,*) v1,f1
      write (*,*) "twotbls: too many iterations at i, j = ",i,j
      stop
     end if
     do k=1,maxit
      v=0.5*(v1+v2)
      call fd(v,f,df,q1)
      e=abs(f/q1)
      if (e<rerr) exit
      if (f>0.0) then
       v1=v
       f1=f
      else
       v2=v
       f2=f
      end if
     end do
     vlast=v
     if (k>maxit) then
      write (*,*) "twotbls: too many iterations at i, j = ",i,j
      stop
     end if
    end if
    ! Solved.
    qp(i,j)=q1
   end do
  end do
  ! Interpolate extra fluxes.
  do i=1,2
  nfi(i)=nft(i)-1
  phifi(i,1:nfi(i))=0.5*(phif(i,1:nfi(i))+phif(i,2:nft(i)))
  end do
  do i=1,nft(1)
  call quadinterp(phif(2,:),qp(i,:),nft(2),phifi(2,:),qi1(i,:))
  end do
  do j=1,nft(2)
  call quadinterp(phif(1,:),qp(:,j),nft(1),phifi(1,:),qi2(:,j))
  end do
  do j=1,nfi(2)
  call quadinterp(phif(1,:),qi1(:,j),nft(1),phifi(1,:),qi3(:,j))
  end do
  ! Put all the fluxes together.
  i=nft(1)+nfi(1)
  j=nft(2)+nfi(2)
  qi5(1:i:2,1:j:2)=qp(1:nft(1),1:nft(2))
  qi5(1:i:2,2:j:2)=qi1(1:nft(1),1:nfi(2))
  qi5(2:i:2,1:j:2)=qi2(1:nfi(1),1:nft(2))
  qi5(2:i:2,2:j:2)=qi3(1:nfi(1),1:nfi(2))
  phii5(1,1:i:2)=phif(1,1:nft(1))
  phii5(1,2:i:2)=phifi(1,1:nfi(1))
  phii5(2,1:j:2)=phif(2,1:nft(2))
  phii5(2,2:j:2)=phifi(2,1:nfi(2))
!
! timing insert ------------------------
end do
call cpu_time(now)
write (*,*) "time ",(now-start)/ntime
! --------------------------------------
!
  ! Assemble flux table.
  if (allocated(ftwo%ftable)) then
   deallocate(ftwo%ftable)
   do ie=1,2
    pe=>ftwo%fend(ie)
    deallocate(pe%phif)
   end do
  end if
  do ie=1,2
   pe=>ftwo%fend(ie); qe=>ft(ie)%ft%fend(1)
   pe%sid=sp(ie)%sp%sid; pe%nfu=qe%nfu; pe%nft=qe%nft; pe%dz=qe%dz
   allocate(pe%phif(qe%nft))
   pe%phif=qe%phif
  end do
  allocate(ftwo%ftable(i,j))
  ftwo%ftable=qi5(1:i,1:j)
 !
contains
 !
 subroutine fd(phia,f,d,q)
  implicit none
  real,intent(in)::phia
  real,intent(out)::f,d,q
  ! Returns flux difference f, deriv d and upper flux q.
  ! phia - phi at interface in upper path.
  real h,phib,der,v,vm1,qv,qvm1,q1,q1d,q2,q2d
  save phib,q1,q1d ! #mod# 13/5/16
  if (phia/=phialast) then
   if (phia>phi1max) then ! both saturated - calc der and lower interface phi
    h=he(1)+(phia-phie(1))/Ks(1)
    phib=phie(2)+Ks(2)*(h-he(2))
    der=Ks(2)/Ks(1)
   else ! use quadratic interpolation to get them
    ii=find(phia,phii(1,1:ni),ni)
    v=phia-phii(1,ii)
    der=coq(2,ii)+v*2.0*coq(3,ii)
    phib=coq(1,ii)+v*(coq(2,ii)+v*coq(3,ii))
   end if
   ! Get upper flux and deriv.
   v=phif(1,nft(1))
   if (phia>v) then ! off table - extrapolate
    vm1=phif(1,nft(1)-1)
    qv=qf(1,i,nft(1))
    qvm1=qf(1,i,nft(1)-1)
    q1d=(qv-qvm1)/(v-vm1)
    q1=qv+q1d*(phia-v)
   else ! use cubic interpolation
    call ceval1(phia,q1,q1d)
   end if
   phialast=phia
  end if
  ! Get lower flux and deriv in same way.
  v=phif(2,nft(2))
  if (phib>v) then
   vm1=phif(2,nft(2)-1)
   qv=qf(2,nft(2),j)
   qvm1=qf(2,nft(2)-1,j)
   q2d=(qv-qvm1)/(v-vm1)
   q2=qv+q2d*(phib-v)
  else
   call ceval2(j,phib,q2,q2d)
  end if
  ! Set return values.
  f=q1-q2
  d=q1d-q2d*der
  q=q1
 end subroutine fd
 !
 subroutine ceval1(phi,q,qd)
  implicit none
  real,intent(in)::phi
  real,intent(out)::q,qd
  ! Return flux q and deriv qd given phi.
  ! Use cubic interpolation in table (phico1,co1).
  integer i1,i2,im
  real x
  i1=1; i2=nco1+1 ! allow for last interval in table
  do ! use bisection to find place
  if (i2-i1<=1) exit
   im=(i1+i2)/2
   if (phico1(im)>phi) then
    i2=im
   else
    i1=im
   end if
  end do
  ! Interpolate.
  x=phi-phico1(i1)
  q=co1(1,i1)+x*(co1(2,i1)+x*(co1(3,i1)+x*co1(4,i1)))
  qd=co1(2,i1)+x*(2.0*co1(3,i1)+x*3.0*co1(4,i1))
 end subroutine ceval1
 !
 subroutine ceval2(j,phi,q,qd)
  implicit none
  integer,intent(in)::j
  real,intent(in)::phi
  real,intent(out)::q,qd
  ! Return flux q and deriv qd given phi.
  ! Use cubic interpolation in table j of (phico2,co2).
  integer i1,i2,im
  real x
  i1=1; i2=nco2+1 ! allow for last interval in table
  do
  if (i2-i1<=1) exit
   im=(i1+i2)/2
   if (phico2(im)>phi) then
    i2=im
   else
    i1=im
   end if
  end do
  x=phi-phico2(i1)
  q=co2(1,i1,j)+x*(co2(2,i1,j)+x*(co2(3,i1,j)+x*co2(4,i1,j)))
  qd=co2(2,i1,j)+x*(2.0*co2(3,i1,j)+x*3.0*co2(4,i1,j))
 end subroutine ceval2
 !
 subroutine linco(x,y,co)
  implicit none
  real,intent(in)::x(:),y(:)
  real, intent(out)::co(:)
  ! Return linear interpolation coeffs co.
  co(1)=y(1)
  co(2)=(y(2)-y(1))/(x(2)-x(1))
 end subroutine linco
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
 subroutine cuco(x,y,co)
  implicit none
  real,intent(in)::x(:),y(:)
  real, intent(out)::co(:)
  ! Return cubic interpolation coeffs co.
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
 subroutine quadinterp(x,y,n,u,v)
  implicit none
  integer,intent(in)::n
  real,intent(in)::x(:),y(:),u(:)
  real, intent(out)::v(:)
  ! Return v(1:n-1) corresponding to u(1:n-1) using quadratic interpolation.
  integer i,j,k
  real z,co(3)
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
 end subroutine quadinterp
 !
 function find(x,xa,n)
  implicit none
  integer,intent(in)::n
  real,intent(in)::x
  real,intent(in)::xa(:)
  integer find
  ! Return i where xa(i)<=x<xa(i+1).
  integer i1,i2,im
  i1=1; i2=n-1
  do
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
 end subroutine twotbls
end module twofluxes

