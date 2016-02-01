!
program example6
 ! This program solves for water flow in a duplex soil. Solve is called daily
 ! for 20 days with precipitation of 1 cm/h on day 1. There is vapour flow, but
 ! no solute transport. However, there is continuous water extraction by roots
 ! using the sink term, calculated in module "sinks" using module "roots".
 use soildata, only:dx,ths,gettbls,Sofh,hofS,he
 use flow, only:dSmax,solve
 use roots, only: Pt,hroots1=>h1,hroots2=>h2,setroots
 use sinks, only:nex,setsinks
 implicit none
 integer,parameter::n=10,nt=2,ns=0 ! 10 layers, 2 soil types, 0 solutes
 integer::j,jt(n),sidx(n),nsteps
 real::drn,evap,F10,growth,h0,h1,h2,infil,qevap,qprec,runoff
 real::S1,S2,ti,tf,ts,win,wp,wpi,Zr
 real::now,start
 real,dimension(n)::h,S,trans,x
 open (unit=2,iostat=j,file="example6.out",status="replace",position="rewind")
 ! set a list of soil layers (cm).
 x=(/10.0,20.0,30.0,40.0,60.0,80.0,100.0,120.0,160.0,200.0/)
 sidx(1:4)=103; sidx(5:n)=109 ! soil ident of layers
 ! set required soil hydraulic params
 call gettbls(n,x,sidx)
 ! set root params
 allocate(hroots1(nt),hroots2(nt))
 hroots1=(/he(1),he(5)/) ! zero uptake here
 call hofS(0.9,1,hroots2(1)) ! max uptake here
 call hofS(0.9,5,hroots2(2))
 ! Set sink parameters
 ! initialise for run
 ts=0.0 ! start time
 ! dSmax controls time step. Use 0.05 for a fast but fairly accurate solution.
 ! Use 0.001 to get many steps to test execution time per step.
 dSmax=0.01 ! 0.01 ensures very good accuracy
 jt(1:4)=1;jt(5:n)=2 ! 4 layers of type 1, rest of type2
 call setsinks(n,jt) ! to be passed to roots
 h0=0.0 ! pond depth initially zero
 h1=-1000.0; h2=-400.0 ! initial matric heads
 call Sofh(h1,1,S1) ! solve uses degree of satn
 call Sofh(h2,5,S2)
 S(1:4)=S1; S(5:n)=S2
 wpi=sum(ths*S*dx) ! water in profile initially
 nsteps=0 ! no. of time steps for water soln (cumulative)
 win=0.0 ! water input (total precip)
 evap=0.0; runoff=0.0; infil=0.0; drn=0.0
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
  call solve(ti,tf,qprec,qevap,ns,nex,h0,S,evap,runoff,infil,drn,nsteps,wex=trans)
  win=win+qprec*(tf-ti)
  wp=sum(ths*S*dx) ! water in profile
  write (2,"(f5.0,i5,11f10.4)") tf,nsteps,growth,S(1:2),wp,evap,infil,drn,sum(trans)
  ti=tf
  qprec=0.0
 end do
 call cpu_time(now)
 do j=1,n
  call hofS(S(j),j,h(j))
 end do
 write (2,"(f10.1,i10,10f10.4)") tf,nsteps,h0 ! max depth of pond
 write (2,"(10f8.3)") trans
 write (2,"(10f8.4)") S
 write (2,"(5e16.4)") h
 write (2,"(5g16.6)") wp,evap,infil,drn,sum(trans)
 write (2,"(2f16.6,e16.2)") wp-wpi,infil-drn,win-(wp-wpi+h0+evap+drn+sum(trans))
 write (2,"(a,f8.3)") "execution time (s) : ",now-start
end program example6

