!
 module hyfuns
 ! Uses van Genuchten S(h) modified to be 1 at he and zero at hd:
 ! S(h)=(f(h)-f(hd))/(f(he)-f(hd)), where f(h)=(1+(h/hg**n)**(-m).
 ! K can either be of the Brooks-Corey form, K=S**eta, or it can be
 ! calculated from the Mualem model using numerical integration.
 ! Refer to these as the modified van Genuchten Brooks-Corey (MVGBC)
 ! model and the modified van Genuchten Mualem (MVGM) model.
 ! Work in cm and hours.
 implicit none
 private
 save
 !he air entry, hd
 public sid,ths,Ks,he,hd,p,Kgiven,params,Sofh,Sdofh,Kofh,KofhS
 logical,parameter::Kgiven=.true. ! set true if K to be calculated
! logical,parameter::Kgiven=.false.
 integer sid ! soil ident
 real ths,Ks,he,hd,p,hg,m,n,eta
 real fhe,fhd
 !
contains
 !
 subroutine params(sid1,ths1,Ks1,he1,hd1,p1,hg1,m1,n1)
  implicit none
  integer,intent(in)::sid1
  real,intent(in)::ths1,Ks1,he1,hd1,p1,hg1,m1,n1
  write (*,*) "MVG.params in"
  write (*,*) "sid1: ", sid1
  write (*,*) "ths1: ", ths1
  write (*,*) "Ks1: ", Ks1
  write (*,*) "he1: ", he1
  write (*,*) "hd1: ", hd1
  write (*,*) "p1: ", p1
  write (*,*) "hg1: ", hg1
  write (*,*) "m1: ", m1
  write (*,*) "n1: ", n1
  ! Set hydraulic params.
  ! sid1      - soil ident (arbitrary identifier).
  ! ths1 etc. - MVGBC or MVGM params.
  sid=sid1; ths=ths1; Ks=Ks1; he=he1; hd=hd1; p=p1; hg=hg1; m=m1; n=n1
  eta=2.0/(m*n)+2.0+p
  fhe=(1.0+(he/hg)**n)**(-m)
  fhd=(1.0+(hd/hg)**n)**(-m)
  write(*,*)
  write (*,*) "MVG.params out"
  write (*,*) "sid: ", sid
  write (*,*) "ths: ", ths
  write (*,*) "Ks: ", Ks
  write (*,*) "he: ", he
  write (*,*) "hd: ", hd
  write (*,*) "p: ", p
  write (*,*) "hg: ", hg
  write (*,*) "m: ", m
  write (*,*) "n: ", n  
  write (*,*) "eta: ", eta
  write (*,*) "fhe: ", fhe
  write (*,*) "fhd: ", fhd
 end subroutine params
 !
 function Sofh(h)
  implicit none
  real Sofh
  real,intent(in)::h
  real f
  if (h<he) then
   f=(1.0+(h/hg)**n)**(-m);
   Sofh=(f-fhd)/(fhe-fhd)
  else
   Sofh=1.0;
  endif
 end function Sofh
 !
 subroutine Sdofh(h,S,dSdh)
  ! Used instead of Sofh when K to be calculated.
  implicit none
  real,intent(in)::h
  real,intent(out)::S,dSdh
  real dfdh,f,v,vn
  if (h<he) then
   v=h/hg;
   vn=v**n;
   f=(1.0+vn)**(-m);
   dfdh=m*n*vn*f/(-hg*v*(1.0+vn));
   S=(f-fhd)/(fhe-fhd)
   dSdh=dfdh/(fhe-fhd)
  else
   S=1.0;
   dSdh=0.0;
  endif
 end subroutine Sdofh
 !
 function KofhS(h,S)
  implicit none
  real KofhS
  real,intent(in)::h,S
  KofhS=Ks*S**eta
 end function KofhS
 !
 function Kofh(h)
  implicit none
  real Kofh
  real,intent(in)::h
  Kofh=Ks*Sofh(h)**eta
 end function Kofh
 !
end module hyfuns

