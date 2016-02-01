!
! Copyright P.J. Ross 2005-2007,2015
!
module roots
implicit none
private
save
public::Pt ! potl transpn rate
public::b1,b2,e1,e2,h1,h2,h31,h32,h4,lambda ! params
public::setroots,getrex ! routines
real,parameter::zero=0.0,half=0.5,one=1.0,two=2.0
real::Pt
real::b1=24.66,b2=1.59,e1=0.1/24.0,e2=0.5/24.0, &
  h31=-500.0,h32=-1000.0,h4=-30000.0,lambda=0.5
real,dimension(:),allocatable::h1,h2
! This module implements the simple plant water extraction model described in:
! Li, K.Y., De Jong, R., and J.B. Boisvert 2001. An exponential root-water-
! uptake model with water stress compensation. J. Hydrol. 252:189-204.
! The params above should be set by the user, but default values (from the paper
! above) are given for all except h1 and h2. These are arrays dependent on soil
! property types, and must be allocated and set by the user.
! Definitions:
! Pt       - potential transpiration rate (cm/h), to be set by user.
! b1, b2   - params used to get root distribn param b (see ref).
! e1, e2   - potl transpn rates for using params h31 or h32
! h1, h2   - as matric head decreases, transpn goes from zero at h1 to max at
!            h2 (cm). Will depend on aeration and plant type.
! h31, h32 - params to get head at which transpn decreases below max.
! h4       - head for zero transpn.
! lambda   - power in weighted stress index.
! setroots - subroutine to set current root distribn and potl transpn rate.
! getrex   - subroutine to get rate of water extraction from layers.
!
real,dimension(:),allocatable::Fs
contains
subroutine setroots(x,F10,Zr)
implicit none
real,intent(in)::x(:),F10,Zr
! Sets current weighted root length density distribn (Fs).
! Definitions of arguments:
! x(:) - depths to bottom of layers (cm).
! F10  - fraction of rld in top 10% of the root zone
! Zr   - rooting depth (cm).
INTEGER::i,n
real::b,e0,e1,er,Fi,xp
n=size(x)
if (allocated(Fs)) then
  if (size(Fs).ne.size(x)) then
    deallocate(Fs)
    allocate(Fs(n))
  end if
else
  allocate(Fs(n))
end if
b=b1*F10**b2/Zr ! root distribn param
er=exp(-b*Zr)
xp=zero; e0=one
do i=1,n
  if (xp<Zr) then
    e1=exp(-b*x(i))
    ! get fraction of rld in layer i
    Fi=(log((one+e0)/(one+e1))+half*(e0-e1))/(log(two/(one+er))+half*(one-er))
    Fs(i)=exp(lambda*log(Fi)) ! weighted Fi
    xp=x(i); e0=e1
  else
    Fs(i)=zero
  end if
end do
end subroutine setroots
subroutine getrex(h,jt,rex,rexh)
implicit none
integer,dimension(:),intent(in)::jt
real,dimension(:),intent(in)::h
real,dimension(:),intent(out)::rex,rexh
! Gets rate of water extraction and derivs wrt matric head h. Note that module
! transport assumes rex is zero for h>=he, where he is "air entry" head.
! Definitions of arguments:
! h(:)     - matric heads (cm).
! jt(1:n)  - layer soil type nos.
! rex(:)   - rate of water extraction by roots from layers (cm/h).
! rexh(:)  - derivs wrt h.
integer::i,j
real::a(size(h)),der(size(h)),h3,s,y
h3=min(h31,h32+(h31-h32)*(Pt-e1)/(e2-e1))
der=zero; a=zero ! a is availability factor (alpha in ref)
do i=1,size(h) ! 1 to no. of layers
  j=jt(i)
  if (h(i)>=h1(j)) then
    cycle
  else if (h(i)>=h2(j)) then
    der(i)=-one/(h1(j)-h2(j))
    a(i)=(h1(j)-h(i))*(-der(i))
  else if (h(i)>=h3) then
    a(i)=one
  else if (h(i)>=h4) then
    der(i)=one/(h3-h4)
    a(i)=(h(i)-h4)*der(i)
  end if
end do
s=sum(a*Fs) ! weighted stress index betai of ref is a(i)*Fs(i)/s
y=zero
if (s>zero) y=one/s ! s can be zero for sat profile
rex=a**2*Fs*Pt*y ! rex(i)=Si*dz(i) where Si is given by Eq.(8) of ref
rexh=two*a*der*Fs*Pt*y
end subroutine getrex
end module roots
