!
! Disclaimer
! This software is supplied ‘as is’ and on the understanding that CSIRO will:
! translate it and contribute it to the APSIM Initiative; and (although it has
! undergone limited testing) further develop and thoroughly test it before it
! becomes part of APSIM. Such development and testing is not possible outside of
! the APSIM environment.
!
module flow
 use soildata, only:n,dx,ths,he,getq,getK
 use sinks, only:wsinks,ssinks
 use solprops, only:bd,dis,isotype,isopar,isosub
 ! Module flow
 ! This module allows solution of the equation of continuity for water transport
 ! in a soil profile for a specified period using fluxes read from tables rather
 ! than calculated. The basic references for the methods are:
 ! Parts of Ross, P.J. 2003. Modeling soil water and solute transport - fast,
 ! simplified numerical solutions. Agron. J. 95:1352-1361.
 ! Ross, P.J. 2010. Numerical solution of the equation of continuity for soil
 ! water flow using pre-computed steady-state fluxes.
 ! The user calls sub solve as many times as required for the solution.
 ! Fluxes are provided by subroutine getq in module soildata.
 ! n      - no. of soil layers.
 ! dx     - layer thicknesses.
 ! ths    - saturated water contents of layers.
 ! he     - matric heads of layers at air entry.
 ! getq   - subroutine to find fluxes and derivatives given S and/or matric head.
 ! getK   - subroutine to get conductivity and derivs given S or matric head.
 ! wsinks - subroutine to get layer water extraction rates (cm/h).
 ! ssinks - subroutine to get layer solute extraction rates (mass/h).
 implicit none
 private
 save
 public::botbc ! bottom boundary conditions
 public::h0max,qprecmax,hbot,Sbot ! boundary parameters
 public::dSmax,dSmaxr,dtmax,dtmin,dsmmax,nwsteps ! solution parameters
 public::solve ! solution routine
 real,parameter::zero=0.0,half=0.5,one=1.0,two=2.0
 real,parameter::dSfac=1.25,h0min=-0.02,Smax=1.001,dh0max=0.01
 character(len=20)::botbc="free drainage"
 real::h0max=1.0e10,qprecmax=1.0e10,hbot=0.0,Sbot=1.0
 real::dSmax=0.05,dSmaxr=0.5,dtmax=1.0e10,dtmin=0.0,dsmmax=1.0
 integer::nwsteps=10
 logical::debug=.false.
 integer nless
 !
 ! DEFINITIONS
 ! For default values, see above. For dimensions, see subroutine readtbls.
 ! Boundary conditions:
 ! botbc    - bottom boundary condn for water; "constant head", "free drainage",
 !            "seepage", or "zero flux". Constant head means that matric head h
 !            is specified. Free drainage means zero gradient of matric head,
 !            i.e. unit hydraulic gradient. Seepage means zero flux when the
 !            matric head is below zero and an upper limit of zero for the head.
 ! h0max    - max pond depth allowed before runoff.
 ! hbot     - matric head at bottom of profile when botbc is "constant head".
 ! Sbot     - degree of satn at bottom (needed when hbot<he).
 ! qprecmax - max precipitation (or water input) rate (cm/h) for use with ponded
 !            constant head infiltration. If qprec > qprecmax then actual input
 !            rate is taken to be equal to infiltration plus evaporation rates.
 ! Solution parameters:
 ! dSmax    - max change in S (the "effective saturation") of any unsaturated
 !            layer to aim for each time step; controls time step size.
 ! dSmaxr   - maximum negative relative change in S each time step. This
 !            parameter helps avoid very small or negative S.
 ! dSfac    - a change in S of up to dSfac*dSmax is accepted.
 ! Smax     - max value for layer saturation to allow some overshoot.
 ! dh0max   - allowable overshoot when pond reaches max allowed depth.
 ! h0min    - min (negative) value for surface pond when it empties.
 ! dtmax    - max time step allowed.
 ! dtmin    - min time step allowed; program stops if smaller step required.
 ! dsmmax   - max solute change per time step (see dSmax); user should set this
 !            according to solute units used. Units for different solutes can be
 !            scaled by the user (e.g. to an expected max of around 1.0).
 ! nwsteps  - the solute routine is called every nwsteps of the RE solution.
 ! Other entities:
 ! solve    - sub to call to solve eq. of con.
 ! n        - no. of soil layers.
 ! dx(:)    - layer thicknesses (top to bottom).
 ! ths(:)   - saturated water content.
 ! he(:)    - air entry matric head.
 ! debug    - flag for debugging, or for conditionally printing info.
 ! nless    - no. of step size reductions.
 !
contains
 subroutine solve(ts,tfin,qprec,qevap,nsol,nex,h0,S,evap,runoff,infil,drn,nsteps, &
   jt,cin,c0,sm,soff,sinfil,sdrn,nssteps,wex,sex)
 implicit none
 integer,intent(in)::nsol,nex
 real,intent(in)::ts,tfin,qprec,qevap
 integer,intent(inout)::nsteps
 real,intent(inout)::h0,S(:),evap,runoff,infil,drn
 integer,intent(in),optional::jt(:)
 real,intent(in),optional::cin(nsol)
 integer,intent(inout),optional::nssteps(nsol)
 real,intent(inout),optional::wex(n,nex),c0(nsol),sm(n,nsol),soff(nsol), &
   sinfil(nsol),sdrn(nsol),sex(n,nex,nsol)
 ! Solves the equation of continuity from time ts to tfin.
 ! Definitions of arguments:
 ! ts     - start time (h).
 ! tfin   - finish time.
 ! qprec  - precipitation (or water input) rate (fluxes are in cm/h).
 ! qevap  - potl evaporation rate from soil surface.
 ! nsol   - no. of solutes.
 ! nex    - no. of water extraction streams.
 ! h0     - surface head, equal to depth of surface pond.
 ! S(n)   - degree of saturation ("effective satn") of layers.
 ! evap   - cumulative evaporation from soil surface (cm, not initialised).
 ! runoff - cumulative runoff.
 ! infil  - cumulative net infiltration (time integral of flux across surface).
 ! drn    - cumulative net drainage (time integral of flux across bottom).
 ! nsteps - cumulative no. of time steps for RE soln.
 ! Optional args:
 ! jt(n)           - layer soil type numbers for solute.
 ! cin(nsol)       - solute concns in water input (user's units/cc).
 ! c0(nsol)        - solute concns in surface pond.
 ! sm(n,nsol)      - solute (mass) concns in layers.
 ! soff(nsol)      - cumulative solute runoff (user's units).
 ! sinfil(nsol)    - cumulative solute infiltration.
 ! sdrn(nsol)      - cumulative solute drainage.
 ! nssteps(nsol)   - cumulative no. of time steps for ADE soln.
 ! wex(n,nex)      - cumulative water extractions from layers.
 ! sex(n,nex,nsol) - cumulative solute extractions from layers.
 logical again,extraction,initpond,maxpond
 integer i,iflux,ih0,iok,isatbot,itmp,j,ns,nsat,nsatlast,nsteps0
 integer,dimension(size(S))::isat
 real accel,dmax,dt,dwinfil,dwoff,fac,infili,qpme,qprec1,rsig,rsigdt,sig, &
   t,ti,win,xtblbot
 real,dimension(size(S))::dSdt,h,xtbl
 real,dimension(size(S))::thi,thf,qwex,qwexd
 real,dimension(size(S),nex)::dwexs,qwexs,qwexsd
 real,dimension(0:size(S))::aa,bb,cc,dd,dy,ee,q,qya,qyb
 real,dimension(nsol)::cav,sinfili
 real,dimension(n,nsol)::c
 ! The saturation status of a layer is stored as 0 or 1 in isat since S may be
 ! >1 (because of previous overshoot) when a layer desaturates. Fluxes at the
 ! beginning of a time step and their partial derivs wrt S or h of upper and
 ! lower layers or boundaries are stored in q, qya and qyb.
 extraction=.false.
 if (nex>0) then
  extraction=.true.
  qwexs=0.0 ! so unused elements won't need to be set
  qwexsd=0.0
 end if
 if (size(S).ne.n) then
   write (*,*) "solve: size of S differs from table data"
   stop
 end if
 !----- set up for boundary conditions
   if (botbc=="constant head") then ! h at bottom bdry specified
     if (hbot<he(n)) then
       isatbot=0; xtblbot=Sbot
     else
       isatbot=1; xtblbot=hbot-he(n)
     end if
   end if
 !----- end set up for boundary conditions
 !----- initialise
   t=ts; nsteps0=nsteps; nsat=0
   ! initialise saturated regions
   where (S>=one)
     isat=1; h=he
   elsewhere
     isat=0; h=he-one
   end where
   if (nsol>0) then
     ! set solute info
     thi=ths*S ! initial th
     dwexs=0 ! initial water extracted from layers
     ti=t; infili=infil; sinfili=sinfil
     if (h0>zero.and.count(c0/=cin)>0) then
       initpond=.true. ! initial pond with different solute concn
     else
       initpond=.false.
     end if
     c=zero ! temp storage for soln concns
   end if
 !----- end initialise
 !----- solve until tfin
   do while (t<tfin)
     !----- take next time step
       do iflux=1,2 ! sometimes need twice to adjust h at satn
         nsatlast=nsat ! for detecting onset of profile saturation
         nsat=sum(isat) ! no. of sat layers
         sig=half; if (nsat/=0) sig=one ! time weighting sigma
         rsig=one/sig
         !----- get fluxes and derivs
           ! get table entries
           where (isat==0)
             xtbl=S
           elsewhere
             xtbl=h-he
           end where
           ! get surface flux
           qpme=qprec-qevap ! input rate at saturation
           qprec1=qprec ! may change qprec1 to maintain pond if required
           if (h(1)<=zero.and.h0<=zero.and.nsat<n) then ! no ponding
             ns=1 ! start index for eqns
             call getq(0,(/0,isat(1)/),(/zero,xtbl(1)/),q(0),qya(0),qyb(0))
             if (q(0)<qpme) then
               q(0)=qpme; qyb(0)=zero
             end if
             maxpond=.false.
          else ! ponding
             ns=0
             call getq(0,(/1,isat(1)/),(/h0-he(1),xtbl(1)/),q(0),qya(0),qyb(0))
             if (h0>=h0max.and.qpme>q(0)) then
               maxpond=.true.
               ns=1
             else
               maxpond=.false.
             end if
           end if
           ! get profile fluxes
           do i=1,n-1
             call getq(i,(/isat(i),isat(i+1)/),(/xtbl(i),xtbl(i+1)/),q(i),qya(i),qyb(i))
           end do
           ! get bottom flux
           select case (botbc)
             case ("constant head")
               call getq(n,(/isat(n),isatbot/),(/xtbl(n),xtblbot/),q(n),qya(n),qyb(n))
             case ("zero flux")
               q(n)=zero
               qya(n)=zero
             case ("free drainage")
               call getK(n,isat(n),xtbl(n),q(n),qya(n))
             case ("seepage")
               if (h(n)<=-half*dx(n)) then
                 q(n)=zero
                 qya(n)=zero
               else
                 call getq(n,(/isat(n),1/),(/xtbl(n),-he(n)/),q(n),qya(n),qyb(n))
               end if
             case default
               write (*,*) "solve: illegal bottom boundary condn"
               stop
           end select
           if (extraction) then ! get rate of extraction
             call wsinks(t,isat,xtbl,qwexs,qwexsd)
             qwex=sum(qwexs,2)
             qwexd=sum(qwexsd,2)
           end if
           again=.false. ! flag for recalcn of fluxes
         !----- end get fluxes and derivs
         !----- estimate time step dt
           dmax=zero
           dSdt=zero
           if (extraction) then
            where (isat==0) dSdt=abs(q(1:n)-q(0:n-1)+qwex)/(ths*dx)
           else
            where (isat==0) dSdt=abs(q(1:n)-q(0:n-1))/(ths*dx)
           end if
           dmax=maxval(dSdt) ! max derivative |dS/dt|
           if (dmax>zero) then
             dt=dSmax/dmax
             ! if pond going adjust dt
             if (h0>zero.and.(q(0)-qpme)*dt>h0) dt=(h0-half*h0min)/(q(0)-qpme)
           else ! steady state flow
             if (qpme>=q(n)) then
               ! step to finish - but what if extraction varies with time???
               dt=tfin-t
             else
               dt=-(h0-half*h0min)/(qpme-q(n)) ! pond going so adjust dt
             end if
           end if
           if (dt>dtmax) dt=dtmax ! user's limit
           ! if initial step, improve h where S>=1
           if (nsteps==nsteps0.and.nsat>0.and.iflux==1) then
             again=.true.
             dt=1.0e-20*(tfin-ts)
           end if
           if (nsat==n.and.nsatlast<n.and.iflux==1) then
             ! profile has just become saturated so adjust h values
             again=.true.
             dt=1.0e-20*(tfin-ts)
           end if
           if (t+1.1*dt>tfin) then ! step to finish
             dt=tfin-t
             t=tfin
           else
             t=t+dt ! tentative update
           end if
         !----- end estimate time step dt
         !----- get and solve eqns
           rsigdt=one/(sig*dt)
           ! aa, bb, cc and dd hold coeffs and rhs of tridiag eqn set
           aa(ns+1:n)=qya(ns:n-1); cc(ns:n-1)=-qyb(ns:n-1)
           if (extraction) then
            dd(1:n)=-(q(0:n-1)-q(1:n)-qwex)*rsig
           else
            dd(1:n)=-(q(0:n-1)-q(1:n))*rsig
           end if
           iok=0 ! flag for time step test
           itmp=0 ! counter to abort if not getting solution
           do while (iok==0) ! keep reducing time step until all ok
             itmp=itmp+1
             accel=one-0.05*min(10,max(0,itmp-4)) ! acceleration
             if (itmp>20) then
               write (*,*) "solve: too many iterations of equation solution"
               stop
             end if
             if (ns<1) then
               bb(0)=-qya(0)-rsigdt
               dd(0)=-(qpme-q(0))*rsig
             end if
             if (extraction) then
              where (isat==0) bb(1:n)=qyb(0:n-1)-qya(1:n)-qwexd-ths*dx*rsigdt
              where (isat/=0) bb(1:n)=qyb(0:n-1)-qya(1:n)-qwexd
             else
              where (isat==0) bb(1:n)=qyb(0:n-1)-qya(1:n)-ths*dx*rsigdt
              where (isat/=0) bb(1:n)=qyb(0:n-1)-qya(1:n)
             end if
             call tri(ns,n,aa,bb,cc,dd,ee,dy)
             ! dy contains dS or, for sat layers, h values
             iok=1
             if (.not.again) then
               ! check if time step ok, if not then set fac to make it less
               iok=1
               do i=1,n
                 if (isat(i)==0) then ! check change in S
                   if (abs(dy(i))>dSfac*dSmax) then
                     fac=max(half,accel*abs(dSmax/dy(i))); iok=0; exit
                   end if
                   if (-dy(i)>dSmaxr*S(i)) then
                     fac=max(half,accel*dSmaxr*S(i)/(-dSfac*dy(i))); iok=0; exit
                   end if
                   if (S(i)<one.and.S(i)+dy(i)>Smax) then
                     fac=accel*(half*(one+Smax)-S(i))/dy(i); iok=0; exit
                   end if
                   if (S(i)>=one.and.dy(i)>half*(Smax-one)) then
                     fac=0.25*(Smax-one)/dy(i); iok=0; exit
                   end if
                 end if
               end do
               if (iok==1.and.ns<1.and.h0<h0max.and.h0+dy(0)>h0max+dh0max) then
                 ! start of runoff
                 fac=(h0max+half*dh0max-h0)/dy(0); iok=0
               end if
               if (iok==1.and.ns<1.and.h0>zero.and.h0+dy(0)<h0min) then
                 ! pond going
                 fac=-(h0-half*h0min)/dy(0); iok=0
               end if
               if (iok==0) then ! reduce time step
                 t=t-dt; dt=fac*dt; t=t+dt; rsigdt=1./(sig*dt)
                 nless=nless+1 ! count step size reductions
               end if
               if (isat(1)/=0.and.iflux==1.and.h(1)<zero.and. &
                 h(1)+dy(1)>zero) then
                 ! incipient ponding - adjust state of saturated regions
                 t=t-dt; dt=1.0e-20*(tfin-ts); rsigdt=1./(sig*dt)
                 again=.true.; iok=0
               end if
             end if
           end do
         !----- end get and solve eqns
         !----- update unknowns
           ih0=0
           if (.not.again) then
             dwoff=zero
             if (ns<1) then
               h0=h0+dy(0)
               if (h0<zero.and.dy(0)<zero) ih0=1 ! pond gone
               evap=evap+qevap*dt
               ! note that fluxes required are q at sigma of time step
               dwinfil=(q(0)+sig*(qya(0)*dy(0)+qyb(0)*dy(1)))*dt
             else
               dwinfil=(q(0)+sig*qyb(0)*dy(1))*dt
               if (maxpond) then
                 evap=evap+qevap*dt
                 if (qprec>qprecmax) then ! set input to maintain pond
                   qpme=(q(0)+sig*qyb(0)*dy(1))
                   qprec1=qpme+qevap
                   dwoff=zero
                 else
                   dwoff=qpme*dt-dwinfil
                 end if
                 runoff=runoff+dwoff
               else
                 evap=evap+qprec1*dt-dwinfil
               end if
             end if
             infil=infil+dwinfil
             if (nsol>0) then ! get surface solute balance
               if (initpond) then ! pond concn /= cin
                 if (h0>zero) then
                   if (ns==1) dy(0)=zero ! if max pond depth
                   cav=((two*h0-dy(0))*c0+qprec1*dt*cin)/(two*h0+dwoff+dwinfil)
                   c0=two*cav-c0
                 else
                   cav=((h0-dy(0))*c0+qprec1*dt*cin)/(dwoff+dwinfil)
                   initpond=.false. ! pond gone
                   c0=cin ! for output if any pond at end
                 end if
                 soff=soff+dwoff*cav
                 sinfil=sinfil+dwinfil*cav
               else
                 soff=soff+dwoff*cin
                 sinfil=sinfil+(qprec1*dt-dwoff)*cin
               end if
             end if
             if (botbc=="constant head") then
               drn=drn+(q(n)+sig*qya(n)*dy(n))*dt
             else
               drn=drn+(q(n)+sig*qya(n)*dy(n))*dt
             end if
             if (extraction) then
               if (nsol>0) then
!                 dwexs=dwexs+(qwexs+sig*qwexsd*spread(dy(1:n),2,nex))*dt
                 do i=1,nex
                  dwexs(:,i)=dwexs(:,i)+(qwexs(:,i)+sig*qwexsd(:,i)*dy(1:n))*dt
                 end do
               end if
               if (present(wex)) then
!                 wex=wex+(qwexs+sig*qwexsd*spread(dy(1:n),2,nex))*dt
                 do i=1,nex
                  wex(:,i)=wex(:,i)+(qwexs(:,i)+sig*qwexsd(:,i)*dy(1:n))*dt
                 end do
               end if
             end if
           end if
           do i=1,n
             if (isat(i)==0) then
               if (.not.again) then
                 S(i)=S(i)+dy(i)
                 if (S(i)>one.and.dy(i)>zero) then ! saturation of layer
                   isat(i)=1; h(i)=he(i)
                 end if
               end if
             else
               h(i)=h(i)+dy(i)
               if (i==1.and.ih0/=0.and.h(i)>=he(i)) h(i)=he(i)-one ! pond gone
               if (h(i)<he(i)) then ! desaturation of layer
                 isat(i)=0; h(i)=he(i)
               end if
             end if
           end do
         !----- end update unknowns
         if (.not.again) exit
       end do
       if (dt<=dtmin) then
         write (*,*) "solve: time step = ",dt
         stop
       end if
     !----- end take next time step
     ! remove negative h0 (optional)
     if (h0<zero.and.isat(1)==0) then
       infil=infil+h0
       S(1)=S(1)+h0/(ths(1)*dx(1)); h0=zero
     end if
     nsteps=nsteps+1
     ! solve for solute transport if required
     if (nwsteps*(nsteps/nwsteps)==nsteps) then
       call getsolute()
     end if
   end do
 !----- end solve until tfin
 ! finalise solute transport if required
 call getsolute()
 contains
   subroutine getsolute()
   ! Provides an interface to subroutine solute
   if (nsol>0.and.t>ti) then
     thf=ths*S ! final th before call
     win=infil-infili ! water in at top over time interval
     cav=(sinfil-sinfili)/win ! average concn in win
     if (extraction) then
       call solute(ti,t,thi,thf,dwexs,win,cav,n,nsol,nex,dx,jt,dsmmax,sm, &
         sdrn,nssteps,c,sex=sex)
     else
       call solute(ti,t,thi,thf,dwexs,win,cav,n,nsol,nex,dx,jt,dsmmax,sm, &
         sdrn,nssteps,c)
     end if
     ti=t; thi=thf; dwexs=0; infili=infil; sinfili=sinfil ! for next interval
   end if
   end subroutine getsolute
 end subroutine solve
 !
 subroutine solute(ti,tf,thi,thf,dwexs,win,cin,n,ns,nex,dx,jt,dsmmax,sm,sdrn, &
   nssteps,c,sex)
 implicit none
 integer,intent(in)::n,ns,nex,jt(n)
 real,intent(in)::ti,tf,thi(n),thf(n),dwexs(n,nex),win,cin(ns),dx(n),dsmmax
 integer,intent(inout)::nssteps(ns)
 real,intent(inout)::sm(n,ns),sdrn(ns),c(n,ns)
 real,intent(inout),optional::sex(n,nex,ns)
 ! Solves the ADE from time ti to tf. Diffusion of solute ignored - dispersion
 ! coeff = dispersivity * abs(pore water velocity).
 ! Definitions of arguments:
 ! Required args:
 ! ti           - start time (h).
 ! tf           - finish time.
 ! thi(n)       - initial layer water contents.
 ! thf(n)       - final layer water contents.
 ! dwexs(n,nex) - water extracted from layers over period ti to tf.
 ! win          - water in at top of profile.
 ! cin(ns)      - solute concn in win.
 ! n            - no. of soil layers.
 ! ns           - no. of solutes.
 ! nex          - no. of water extraction streams.
 ! dx(n)        - layer thicknesses.
 ! jt(n)        - layer soil type numbers for solute.
 ! dsmmax(ns)   - max change in sm of any layer to aim for each time step;
 !                 controls time step size.
 ! sm(n,ns)     - layer masses of solute per cc.
 ! sdrn(ns)     - cumulative solute drainage.
 ! nssteps(ns)  - cumulative no. of time steps for ADE soln.
 ! Optional arg:
 ! sex(n,nex,ns) - cumulative solute extractions in water extraction streams.
 !
 integer,parameter::itmax=20 ! max iterations for finding c from sm
 real,parameter::eps=0.00001 ! for stopping
 integer::i,it,j,k
 real::dc,dm,dmax,dt,dz(n-1),f,fc,r,rsig,rsigdt,sig,sigdt,t,tfin,th,v1,v2
 real,dimension(n-1)::coef1,coef2
 real,dimension(n)::csm,tht,dwex,qsex,qsexd
 real,dimension(n,nex)::qsexs,qsexsd
 real,dimension(0:n)::aa,bb,cc,dd,dy,ee,q,qw,qya,qyb
 qsexs=0.0; qsexsd=0.0 ! so unused elements won't need to be set
 sig=half; rsig=one/sig
 tfin=tf
 dz=half*(dx(1:n-1)+dx(2:n))
 !get average water fluxes
 dwex=sum(dwexs,2) ! total changes in sink water extraction since last call
 r=one/(tf-ti); qw(0)=r*win; tht=r*(thf-thi)
 do i=1,n
   qw(i)=qw(i-1)-dx(i)*tht(i)-r*dwex(i)
 end do
 !get constant coefficients
 do i=1,n-1
   v1=half*qw(i)
   v2=half*(dis(jt(i))+dis(jt(i+1)))*abs(qw(i))/dz(i)
   coef1(i)=v1+v2; coef2(i)=v1-v2
 end do
 do j=1,ns
   t=ti
   if (qw(0)>zero) then
     q(0)=qw(0)*cin(j)
   else
     q(0)=zero
   end if
   qyb(0)=zero
   do while (t<tfin)
     ! get fluxes
     do i=1,n
       ! get c and csm=dc/dsm (with theta constant)
       k=jt(i)
       th=thi(i)+(t-ti)*tht(i)
       if (isotype(k,j)=="no".or.sm(i,j)<zero) then ! handle sm<0 here
         csm(i)=one/th
         c(i,j)=csm(i)*sm(i,j)
       else if (isotype(k,j)=="li") then
         csm(i)=one/(th+bd(k)*isopar(k,j)%p(1))
         c(i,j)=csm(i)*sm(i,j)
       else
         do it=1,itmax ! get c from sm using Newton's method and bisection
           if (c(i,j)<zero) c(i,j)=zero ! c and sm are >=0
           call isosub(isotype(k,j),c(i,j),dsmmax,isopar(k,j)%p(:),f,fc)
           csm(i)=one/(th+bd(k)*fc)
           dm=sm(i,j)-(bd(k)*f+th*c(i,j))
           dc=dm*csm(i)
           if (sm(i,j)>=zero.and.c(i,j)+dc<zero) then
             c(i,j)=half*c(i,j)
           else
             c(i,j)=c(i,j)+dc
           end if
           if (abs(dm)<eps*(sm(i,j)+10.0*dsmmax)) exit
           if (it==itmax) then
             write (*,*) "solute: too many iterations getting c"
             stop
           end if
         end do
       end if
     end do
     q(1:n-1)=coef1*c(1:n-1,j)+coef2*c(2:n,j)
     qya(1:n-1)=coef1*csm(1:n-1)
     qyb(1:n-1)=coef2*csm(2:n)
     q(n)=qw(n)*c(n,j)
     qya(n)=qw(n)*csm(n)
     ! get time step
     dmax=maxval(abs(q(1:n)-q(0:n-1))/dx)
     if (dmax==zero) then
       dt=tfin-t
     elseif (dmax<zero) then
       write (*,*) "solute: errors in fluxes prevent continuation"
       stop
     else
       dt=dsmmax/dmax
     end if
     if (t+1.1*dt>tfin) then
       dt=tfin-t; t=tfin
     else
       t=t+dt
     end if
     sigdt=sig*dt; rsigdt=one/sigdt
     ! adjust q for change in theta
     q(1:n-1)=q(1:n-1)-sigdt*(qya(1:n-1)*tht(1:n-1)*c(1:n-1,j)+ &
       qyb(1:n-1)*tht(2:n)*c(2:n,j))
     q(n)=q(n)-sigdt*qya(n)*tht(n)*c(n,j)
     ! get and solve eqns
     aa(2:n)=qya(1:n-1); cc(1:n-1)=-qyb(1:n-1)
     if (present(sex)) then ! get extraction
       call ssinks(t,ti,tf,j,dwexs,c(1:n,j),qsexs,qsexsd)
       qsex=sum(qsexs,2)
       qsexd=sum(qsexsd,2)
       bb(1:n)=qyb(0:n-1)-qya(1:n)-qsexd*csm-dx*rsigdt
       dd(1:n)=-(q(0:n-1)-q(1:n)-qsex)*rsig
     else
       bb(1:n)=qyb(0:n-1)-qya(1:n)-dx*rsigdt
       dd(1:n)=-(q(0:n-1)-q(1:n))*rsig
     end if
     call tri(1,n,aa,bb,cc,dd,ee,dy)
     ! update unknowns
     sdrn(j)=sdrn(j)+(q(n)+sig*qya(n)*dy(n))*dt
     sm(:,j)=sm(:,j)+dy(1:n)
     if (present(sex)) then
!       sex(:,:,j)=sex(:,:,j)+(qsexs+sig*qsexsd*spread(csm*dy(1:n),2,nex))*dt
       do i=1,nex
        sex(:,i,j)=sex(:,i,j)+(qsexs(:,i)+sig*qsexsd(:,i)*csm*dy(1:n))*dt
       end do
     end if
     nssteps(j)=nssteps(j)+1
   end do
 end do
 end subroutine solute
 subroutine tri(ns,n,aa,bb,cc,dd,ee,dy)
 implicit none
 integer,intent(in)::ns,n
 real,dimension(0:n),intent(in)::aa,cc,dd
 real,dimension(0:n),intent(inout)::bb,ee,dy
 ! Solves tridiag set of linear eqns. Coeff arrays aa and cc left intact.
 ! Definitions of arguments:
 ! ns      - start index for eqns.
 ! n       - end index.
 ! aa(0:n) - coeffs below diagonal; ns+1:n used.
 ! bb(0:n) - coeffs on diagonal; ns:n used.
 ! cc(0:n) - coeffs above diagonal; ns:n-1 used.
 ! dd(0:n) - rhs coeffs; ns:n used.
 ! ee(0:n) - work space.
 ! dy(0:n) - solution in ns:n.
 integer i
 dy(ns)=dd(ns) ! decomposition and forward substitution
 do i=ns,n-1
   ee(i)=cc(i)/bb(i)
   dy(i)=dy(i)/bb(i)
   bb(i+1)=bb(i+1)-aa(i+1)*ee(i)
   dy(i+1)=dd(i+1)-aa(i+1)*dy(i)
 end do
 dy(n)=dy(n)/bb(n) ! back substitution
 do i=n-1,ns,-1
   dy(i)=dy(i)-ee(i)*dy(i+1)
 end do
 end subroutine tri
end module flow
