!
program example7
 ! This program solves for water flow in a duplex soil. Solve is called daily
 ! for 20 days with precipitation of 1 cm/h on day 1. There is vapour flow,
 ! solute transport, a source/sink term with drippers and drains, continuous
 ! surface evaporation and instantaneous water extraction by roots at the end of
 ! every day, calculated in the main program using module "roots". Drippers and
 ! drains are in different layers, so solve can calculate addition and
 ! extraction of water and solute using single terms for each layer.
 use soildata, only:dx,ths,gettbls,Sofh,hofS,he
 use flow, only:dSmax,dsmmax,nwsteps,solve
 use solprops, only:allo,solpar,setiso
 use sinks, only:nex,drip,setsinks
 use roots, only: Pt,hroots1=>h1,hroots2=>h2,setroots,getrex
 implicit none
 integer,parameter::n=10,nt=2,ns=2 ! 10 layers, 2 soil types, 2 solutes
 integer::idrip,idrn,il,j,jt(n),sidx(n),nsteps,nssteps(ns)
 real::dcond,driprate,drn,evap,F10,growth,h0,h1,h2,infil,qevap,qprec,runoff
 real::S1,S2,ti,tf,ts,win,wp,wpi,Zr
 real::now,start
 real,dimension(nt)::bd,dis
 real,dimension(ns)::c0,cin,dripsol,sdrn,sinfil,soff,sp,spi
 real,dimension(n)::h,rex,rexh,S,trans,x,v1,v2
 real,dimension(n,nex)::wex
 real,dimension(n,ns)::sm
 real,dimension(n,nex,ns)::sex
 ! define isotype and isopar for solute 2
 character(len=2)::isotype(nt)
 real,dimension(nt)::isopar(nt,2) ! 2 params
 open (unit=2,iostat=j,file="example7.out",status="replace",position="rewind")
 ! set a list of soil layers (cm).
 x=(/10.0,20.0,30.0,40.0,60.0,80.0,100.0,120.0,160.0,200.0/)
 sidx(1:4)=103; sidx(5:n)=109 ! soil ident of layers
 ! set required soil hydraulic params
 call gettbls(n,x,sidx)
 call allo(nt,ns) ! allocate storage for water and solute params
 bd=(/1.3,1.3/)
 dis=(/20.0,20.0/)
 ! set isotherm type and params for solute 2 here
 isotype(1)="Fr"
 isotype(2)="La"
 isopar(1,1:2)=(/1.0,0.5/)
 isopar(2,1:2)=(/1.0,0.01/)
 do j=1,nt ! set params
   call solpar(j,bd(j),dis(j))
   ! set isotherm type and params
   call setiso(j,2,isotype(j),isopar(j,:))
 end do
 ! set root params
 allocate(hroots1(nt),hroots2(nt))
 hroots1=(/he(1),he(5)/) ! zero uptake here
 call hofS(0.9,1,hroots2(1)) ! max uptake here
 call hofS(0.9,5,hroots2(2))
 ! initialise for run
 ts=0.0 ! start time
 ! dSmax controls time step. Use 0.05 for a fast but fairly accurate solution.
 ! Use 0.001 to get many steps to test execution time per step.
 dSmax=0.01 ! 0.01 ensures very good accuracy
 jt(1:4)=1;jt(5:n)=2 ! 4 layers of type 1, rest of type2
 ! set drippers and drains
 idrip=1; idrn=4 ! drippers in layer 1, drain in layer 4
 driprate=0.2 ! 0.2 cm/h (24 h average) from drippers
 dripsol=(/10.0,10.0/) ! solute concns (mass units per cm water)
 dcond=0.1 ! drain conductance is 0.1 cm/h per cm head above 0
 call setsinks(n,ns,idrip,idrn,driprate,dripsol,dcond)
 h0=0.0 ! pond depth initially zero
 h1=-1000.0; h2=-400.0 ! initial matric heads
 call Sofh(h1,1,S1) ! solve uses degree of satn
 call Sofh(h2,5,S2)
 S(1:4)=S1; S(5:n)=S2
 wpi=sum(ths*S*dx) ! water in profile initially
 nsteps=0 ! no. of time steps for water soln (cumulative)
 win=0.0 ! water input (total precip)
 evap=0.0; runoff=0.0; infil=0.0; drn=0.0
 sm=0.0; sm(1,:)=1000.0/dx(1) ! initial solute concn (mass units per cm soil)
 spi=sum(sm*spread(dx,2,ns),1) ! solute in profile initially
 dsmmax=0.1*sm(1,1) ! solute stepsize control param
 nwsteps=10
 c0=0.0; cin=0.0 ! no solute input
 nssteps=0 ! no. of time steps for solute soln (cumulative)
 soff=0.0; sinfil=0.0; sdrn=0.0
 trans=0.0 ! cumulative transpiration
 qprec=1.0 ! precip at 1 cm/h for first 24 h
 ti=ts
 call cpu_time(start)
 do j=1,20
  tf=ti+24.0
  ! set plant growth and get qevap
  growth=1.0/(1.0+exp(-ti/240.0)) ! sigmoid growth curve, 0 to 1
  F10=0.8*growth ! fraction of root length density in top 10% of the root zone
  Zr=180.0*growth ! rooting depth (cm).
  call setroots(x,F10,Zr)
  Pt=0.05*growth ! potl transpn rate, cm/h, average for 24 h
  qevap=0.05-Pt ! potl evap rate from soil surface, average for 24 h
  call solve(ti,tf,qprec,qevap,ns,nex,h0,S,evap,runoff,infil,drn,nsteps, &
   jt,cin,c0,sm,soff,sinfil,sdrn,nssteps,wex,sex)
  ! extract water taken up by roots - average over S values before and after
  do il=1,n
   call hofS(min(S(il),1.0),il,h(il)) ! need heads for root extraction routine
  end do
  call getrex(h,jt,rex,rexh) ! extraction rates rex for 24 h
  v1=S
  v2=rex
  S=(ths*S*dx-rex*24.0)/(ths*dx) ! remove water extracted by roots
  do il=1,n
   call hofS(min(S(il),1.0),il,h(il))
  end do
  call getrex(h,jt,rex,rexh) ! extraction rates at new S
  rex=0.5*(v2+rex) ! average extraction rates
  S=(ths*v1*dx-rex*24.0)/(ths*dx) ! remove average water extracted by roots
  if (S(1)<S1) then ! switch drippers on
   drip=.true.
  else
   drip=.false.
  end if
  trans=trans+rex*24.0
  win=win+qprec*(tf-ti)
  wp=sum(ths*S*dx) ! water in profile
  sp=sum(sm*spread(dx,2,ns),1) ! solute in profile
  write (2,"(f5.0,i5,13f10.4)") tf,nsteps,growth,S(1:2),wp,evap,infil,drn, &
   sum(trans),sum(wex),sp,sum(sum(sex,1),1)
  ti=tf
  qprec=0.0
 end do
 call cpu_time(now)
 do j=1,n
  call hofS(S(j),j,h(j))
 end do
 write (2,"(f10.1,i10,10f10.4)") tf,nsteps,h0 ! max depth of pond
 write (2,"(10f8.3)") trans
! write (2,"(10f8.3)") wex
 write (2,"(10f8.4)") S
 write (2,"(5e16.4)") h
 write (2,"(5g16.6)") wp,evap,infil,drn,sum(trans),sum(wex)
 write (2,"(2f16.6,e16.2)") wp-wpi,infil-drn,win-(wp-wpi+h0+evap+drn+sum(trans)+sum(wex))
 write (2,"(g16.6,12i5)") tf,nssteps
 write (2,"(6g16.6)") sp,sdrn,sum(sex,1)
 write (2,"(6g16.6)") sp-spi,sp-spi+sdrn+sum(sum(sex,1),1) ! solute balance
 write (2,"(10f8.3)") sm
 write (2,"(10f8.3)") sex
 write (2,"(a,f8.3)") "execution time (s) : ",now-start
end program example7

