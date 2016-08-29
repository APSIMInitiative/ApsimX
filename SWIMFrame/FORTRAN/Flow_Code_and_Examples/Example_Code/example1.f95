!
program example1
 ! This program solves for water flow in a duplex soil. Solve is called for
 ! 1 day with precipitation at 1 cm/h, then for a further uninterrupted 99 days
 ! with no precip. The potential evaporation rate is 0.05 cm/h throughout.
 ! There is vapour flow, but no sink term and no solute transport.
 use soildata, only:dx,ths,gettbls,Sofh,hofS
 use flow, only:dSmax,solve
 use sinks, only:nex ! no. of water extraction streams
 implicit none
 integer,parameter::n=10,nt=2,ns=0 ! 10 layers, 2 soil types, 0 solutes
 integer::j,jt(n),sidx(n),nsteps
 real::drn,evap,h0,h1,h2,infil,qevap,qprec,runoff
 real::S1,S2,ti,tf,ts,win,wp,wpi
 real::now,start
 real,dimension(n)::h,S,x
 open (unit=2,iostat=j,file="example1.out",status="replace",position="rewind")
 ! set a list of soil layers (cm).
 x=(/10.0,20.0,30.0,40.0,60.0,80.0,100.0,120.0,160.0,200.0/)
 sidx(1:4)=103; sidx(5:n)=109 ! soil ident of layers
 ! set required soil hydraulic params
 call gettbls(n,x,sidx)
 ! initialise for run
 ts=0.0 ! start time
 ! dSmax controls time step. Use 0.05 for a fast but fairly accurate solution.
 ! Use 0.001 or less to get many steps to test execution time per step.
 dSmax=0.01 ! 0.01 ensures very good accuracy
 jt(1:4)=1;jt(5:n)=2 ! 4 layers of type 1, rest of type2
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
 tf=ti+24.0
 qevap=0.05 ! potential evap rate from soil surface
 call cpu_time(start)
 call solve(ti,tf,qprec,qevap,ns,nex,h0,S,evap,runoff,infil,drn,nsteps)
 win=win+qprec*(tf-ti)
 write (2,"(f10.1,i10,10f10.4)") tf,nsteps,h0 ! max depth of pond
 write (2,"(10f8.4)") S
 ti=tf; tf=2400.0
 qprec=0.0
 call solve(ti,tf,qprec,qevap,ns,nex,h0,S,evap,runoff,infil,drn,nsteps)
 call cpu_time(now)
 win=win+qprec*(tf-ti)
 wp=sum(ths*S*dx) ! water in profile
 do j=1,n
  call hofS(S(j),j,h(j))
 end do
 write (2,"(f10.1,i10,10f10.4)") tf,nsteps,h0 ! max depth of pond
 write (2,"(10f8.4)") S
 write (2,"(5e16.4)") h
 write (2,"(5g16.6)") wp,evap,infil,drn
 write (2,"(2f16.6,e16.2)") wp-wpi,infil-drn,win-(wp-wpi+h0+evap+drn)
 write (2,"(a,f8.3)") "execution time (s) : ",now-start
end program example1

