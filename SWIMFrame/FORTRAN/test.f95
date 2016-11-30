!
program test
 use hyfuns, only:params
 use soil, only:soilprops,gensptbl,sp
 use fluxes, only:fluxend,fluxtable,fluxtbl,ft
 use twofluxes, only:twotbls,ftwo
 ! Generate soil properties and fluxes for a soil profile and write to files
 ! params - subroutine to set soil parameter values.
 ! soilprops - soil prop type definition.
 ! gensptbl - subroutine to generate soil prop table sp
 ! fluxend, fluxtable - type definitions.
 ! fluxtbl - subroutine to generate flux table ft.
 ! twotbls - subroutine to generate composite flux table ftwo from two single ones.
 implicit none
 integer,parameter::n=10 ! no. of layers
 integer,parameter,dimension(2)::sid=(/103,109/) ! arbitrary soil idents
 ! Define parameters for the two soils.
 real,parameter::hd=-1e7 ! recommended value
 real,parameter,dimension(2)::ths=(/0.4,0.6/),Ks=(/2.0,0.2/),he=(/-2.0,-2.0/)
 real,parameter,dimension(2)::hg=(/-10.0,-40.0/),mn=1.0/(/3,9/),en=2.0+mn,em=mn/en
 real,parameter,dimension(2)::p=(/1.0,1.0/)
 !
 character(4) id,mm
 character(14) ftname(2)
 character(80) sfile
 integer sidx(n)
 integer i,j,ndz(2)
 real dzmin,x(n)
 real,dimension(2,10)::dz
 type(soilprops)::sp1,sp2
 type(fluxtable),target::ft1,ft2
 ! Define soil profile.
 x=10.0*(/1,2,3,4,6,8,10,12,16,20/) ! depths to bottom of layers
 sidx(1:4)=103; sidx(5:n)=109 ! soil ident of layers
 dzmin=1.0 ! smallest likely path length
 ndz=(/2,4/) ! for the two soil types - gives six flux tables
 dz(1,1:2)=(/5.0,10.0/)
 dz(2,1:4)=(/10.0,20.0,30.0,40.0/)
 do i=1,2 ! for the two soils
 write (*,*) "Params setup"
 write (*,*) "soil id: ", sid(i)
 write (*,*) "ths: ", ths(i)
 write (*,*) "Ks: ", Ks(i)
 write (*,*) "he: ", he(i)
 write (*,*) "hd: ", hd
 write (*,*) "p: ", p(i)
 write (*,*) "hg: ", hg(i)
 write (*,*) "em: ", em(i)
 write (*,*) "en: ", en(i)
  call params(sid(i),ths(i),Ks(i),he(i),hd,p(i),hg(i),em(i),en(i)) ! set MVG params
  call gensptbl(dzmin) ! generate soil props
  write (id,'(i4.4)') sid(i) ! write them to a file
  sfile='soil' // id // '.dat'
  open (unit=11,file=sfile,form="unformatted",position="rewind",action="write")
  call writeprops(11,sp)
  close(11)
  do j=1,ndz(i) ! generate flux tables
   call fluxtbl(dz(i,j))
   write (mm,'(i4.4)') nint(10.0*dz(i,j)) ! write each to a file
   sfile='soil' // id // 'dz' // mm // '.dat'
   open (unit=11,file=sfile,form="unformatted",position="rewind",action="write")
   call writefluxes(11,ft)
   close(11)
  end do
 end do
 ! generate and write composite flux table for path with two soil types
 open (unit=11,file="soil0103.dat",form="unformatted",position="rewind",action="read")
 call readprops(11,sp1)
 close(11)
 open (unit=11,file="soil0109.dat",form="unformatted",position="rewind",action="read")
 call readprops(11,sp2)
 close(11)
 open (unit=11,file="soil0103dz0050.dat",form="unformatted",position="rewind",action="read")
 call readfluxes(11,ft1)
 close(11)
 open (unit=11,file="soil0109dz0100.dat",form="unformatted",position="rewind",action="read")
 call readfluxes(11,ft2)
 close(11)
 call twotbls(ft1,sp1,ft2,sp2)
 sfile="soil0103dz0050_soil0109dz0100.dat"
 open (unit=11,file=sfile,form="unformatted",position="rewind",action="write")
 call writefluxes(11,ftwo)
 close(11)
 !
contains
 !
 subroutine readprops(lun,sp)
  implicit none
  integer,intent(in)::lun
  type(soilprops),intent(out)::sp
  ! Read soil props from file and store in soilprops variable.
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
  ! Read soil fluxes from file and store in fluxtable variable.
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
 subroutine writeprops(lun,sp)
  implicit none
  integer,intent(in)::lun
  type(soilprops),intent(in)::sp
  ! Write soilprops variable to file.
  write (lun) sp%sid,sp%nld,sp%n,sp%nc
  write (lun) sp%ths,sp%Ks,sp%he,sp%phie
  write (lun) sp%Sd,sp%lnh,sp%S,sp%h,sp%K,sp%phi,sp%Sc,sp%hc,sp%Kc,sp%phic
  write (lun) sp%Kco,sp%phico
 end subroutine writeprops
 !
 subroutine writefluxes(lun,ft)
  implicit none
  integer,intent(in)::lun
  type(fluxtable),target,intent(in)::ft
  integer ie
  type(fluxend),pointer::pe
  ! Write fluxtable variable to file.
  do ie=1,2
   pe=>ft%fend(ie)
   write (lun) pe%sid,pe%nfu,pe%nft,pe%dz
   write (lun) pe%phif
  end do
  write (lun) ft%ftable
 end subroutine writefluxes
 !
end program test

