using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Models.GrazPlan
{
    /// <summary>
    /// Environment interface
    /// </summary>
    public static class GrazEnv
    {
        // Unit conversion constants     
        /// <summary>
        /// Convert day-of-year to radians
        /// </summary>
        public const double DAY2RAD = 2 * Math.PI / 365;                                                       
        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        public const double DEG2RAD = 2 * Math.PI / 360;                                                                   
        /// <summary>
        /// Convert km/d to m/s
        /// </summary>
        public const double KMD_2_MS = 1.0E3 / (24 * 60 * 60);                                                                  
        /// <summary>
        /// Convert W/m^2 to MJ/m^2/d
        /// </summary>
        public const double WM2_2_MJM2 = 1.0E6 / (24 * 60 * 60);                                                            
        /// <summary>
        /// Convert degrees C to K
        /// </summary>
        public const double C_2_K = 273.15;                                                                         
        /// <summary>
        /// 
        /// </summary>
        public const double HERBAGE_ALBEDO = 0.23;
        /// <summary>
        /// Reference [CO2] in ppm
        /// </summary>
        public const double REFERENCE_CO2 = 350.0;                                                                       
    }
}



/*


{==============================================================================}
{                          TWeatherHandler class                               }
{==============================================================================}

type                                                                           { Daily weather data, used as inputs:   }
  TWeatherData = (wdtMaxT,                                                     {   Maximum air temperature     deg C   }
                  wdtMinT,                                                     {   Minimum air temperature     deg C   }
                  wdtRain,                                                     {   Rainfall                    mm      }
                  wdtSnow,                                                     {   Snow (rain equivalents)     mm      }
                  wdtRadn,                                                     {   Solar radiation             MJ/m^2/d}
                  wdtVP,                                                       {   Actual vapour pressure      kPa     }
                  wdtWind,                                                     {   Average windspeed           m/s     }
                  wdtEpan,                                                     {   Pan evaporation             mm      }
                  wdtRelH,                                                     {   Relative humidity           0-1     }
                  wdtSunH,                                                     {   Hours of bright sunshine    hr      }
                  wdtTrMT);                                                    {   Terrestrial min. temperature deg C  }
  TWeatherSet  = set of TWeatherData;

const
  NO_ELEMENTS  = 1 + Ord( High(TWeatherData) );

type
  TEvapMethod  = (emPropnPan,
                  emPenman,
                  emCERES,
                  emPriestley,
                  emFAO);
  TCO2Array = array[0..5] of Single;

type
  TWeatherHandler =
  class
  private
    FLatitude   : Single;                                                      { Latitude,  in degrees                 }
    FLongitude  : Single;                                                      { Longitude, in degrees                 }
    FElevation  : Single;                                                      { Elevation, in m                       }
    FSlope      : Single;                                                      { Slope,     in degrees                 }
    FAspect     : Single;                                                      { Aspect,    in degrees                 }

    FData       : array[TWeatherData] of Single;
    FDataKnown  : array[TWeatherData] of Boolean;
    FDaylengths : array[Boolean] of Single;
    FDayLenIncr : Boolean;
    FET_Radn    : Single;

    FCO2_Order    : Integer;
    FCO2_Coeffs   : array[0..5] of Single;                                     { Coefficients of a polynomial for [CO2]}
    FCO2_Conc     : Single;

    function  fSatVPSlope(  fTemperature : Single )  : Single;                 { Slope of SVP-T curve         kPa/deg C}
    function  fPsychrometricConst                    : Single;
    function  fLatentHeat                            : Single;                 { Latent heat of vaporization  MJ/kg    }

    function  CeresPET(        fAlbedo        : Single ) : Single;             { Evaporation estimators                }
    function  PenmanPET(       fAlbedo,
                               fCropHeight,
                               fSurfaceResist : Single ) : Single;
    function  PriestleyPET(    fAlbedo,
                               fScalar        : Single ) : Single;
    function  FAO_ReferenceET( fAlbedo        : Single ) : Single;

    function  getData(  D : TWeatherData ) : Single;
    procedure setData(  D : TWeatherData; fValue : Single );
  protected
    FToday      : StdDATE.Date;
  public
    constructor Create;

    property  fLatDegrees    : Single       read FLatitude  write FLatitude;
    property  fLongDegrees   : Single       read FLongitude write FLongitude;
    property  fElevationM    : Single       read FElevation write FElevation;
    property  fSlopeDegrees  : Single       read FSlope     write FSlope;
    property  fAspectDegrees : Single       read FAspect    write FAspect;
    property  fCO2_PPM       : Single       read FCO2_Conc;

    procedure setCO2Trajectory( const Coeffs : TCO2Array );

    procedure setToday( iDay, iMonth, iYear : Integer );                   virtual;
    property  Data[D:TWeatherData] : Single read getData    write setData; default;
    procedure computeVP( const fTemperature, fRelHum : array of Single );  overload; { Daily average vapour pressure   }
    procedure computeVP;                                                   overload; {   computations                  }
    function  bInputKnown( Query : TWeatherData ) : Boolean;               overload;
    function  bInputKnown( Query : TWeatherSet  ) : Boolean;               overload;

    procedure computeDLandETR( iDOY : Integer; var fDL0, fDLC, fETR : Single );{ Compute daylengths and E-T radiation  }

    function  fDaylength( bCivil : Boolean = FALSE ) : Single;                 { Daylength                    hours    }
    function  bDaylengthIncreasing                   : Boolean;
    function  fMeanTemp                              : Single;                 { Daily average temperature    deg C    }
    function  fMeanDayTemp                           : Single;                 { Mean T in daylight hours     deg C    }
    function  fExtraT_Radiation                      : Single;                 { Extra-terrestrial radiation  MJ/m^2/d }
    function  fNetRadiation( fAlbedo     : Single )  : Single;                 { Estimated net radiation      MJ/m^2/d }
    function  fSaturatedVP( fTemperature : Single )  : Single;                 { Saturated vapour pressure    kPa      }
    function  fVP_Deficit                            : Single;                 { Daily average VPD            kPa      }
    function  fEvaporation( Method  : TEvapMethod;                             { Potential evapotranspiration mm       }
                            fParam1 : Single = 0.0;
                            fParam2 : Single = 0.0;
                            fParam3 : Single = 0.0 ) : Single;
  end;

function ET_Radn_Ratio( iDOY : Integer; fLat1, fLat2 : Single ) : Single;

const
  EVAP_REQUIREMENT : array[TEvapMethod] of TWeatherSet =
                     ( [wdtEpan],
                       [wdtMaxT,wdtMinT,wdtRadn,wdtVP,wdtWind],
                       [wdtMaxT,wdtMinT,wdtRadn],
                       [wdtMaxT,wdtMinT,wdtRadn,wdtVP],
                       [wdtMaxT,wdtMinT,wdtRadn,wdtVP,wdtWind] );

implementation

uses
  SysUtils, Math;

const
  MEASURE_HEIGHT = 2.0;                                                        { Assumed height of measurement (m)     }

{==============================================================================}
{ Constants for daylength & extraterrestrial radiation calculations            }
{==============================================================================}

const
  DAYANGLE : array[Boolean] of Single = (90.0*DEG2RAD,90.833*DEG2RAD);         { Horizon angles                        }
  MAXDECLIN    = 23.45 * DEG2RAD;                                              { Maximum declination of the sun (rad)  }
  DECLINBASE   = 172;                                                          { Day of year for maximum declination   }

//  SOLARCONST   = 1360;                                                         { Solar constant (W/m^2)                }
//  ECCENTRICITY = 0.035;                                                        { Effect of eccentricity of Earth's orbit}
  SOLARCONST   = 1367;                                                         { Solar constant (W/m^2)                }
  ECCENTRICITY = 0.033;                                                        { Effect of eccentricity of Earth's orbit}
  OMEGA        = 2*Pi/24;                                                      { Earth's angular velocity (rad/hr)     }

{==============================================================================}
{ Cosntants used in evapotranspiration calculations                            }
{==============================================================================}

  SIGMA        = 4.903E-9;                                                     { Stefan-Boltzmann constant, MJ/m^2/d/K^4 }
  KARMAN       = 0.41;                                                         { von Karman's constant                 }
  SEC2DAY      = 1.0/86400.0;                                                  { Convert seconds to days               }
  EPSILON      = 0.622;                                                        { Molecular weight ratio H20:air        }
  GAS_CONST    = 0.287;                                                        { Specific gas constant,     kJ/kg/K    }

  EVAPDEFAULTS : array[TEvapMethod,1..3] of Single =                           { Default parameters for the            }
                  ( (0.8,            0.0,   0.0),   // emPropnPan              {   fEvaporation method                 }
                    (HERBAGE_ALBEDO, 0.12, 70.0),   // emPenman
                    (HERBAGE_ALBEDO, 0.0,   0.0),   // emCERES
                    (HERBAGE_ALBEDO, 1.26,  0.0),   // emPriestley
                    (HERBAGE_ALBEDO, 1.0,   0.0) ); // emFAO

{==============================================================================}
{ Constants for the Parton & Logan model of diurnal T variation                }
{ Parton WJ & Logan JA (1981), Agric.Meteorol. 23:205-216                      }
{==============================================================================}

const
  PARTON_A =  1.86;
  PARTON_B =  2.20;
  PARTON_C = -0.17;

{==============================================================================}
{ Create                                                                       }
{==============================================================================}

constructor TWeatherHandler.Create;
begin
  FCO2_Order     := 0;
  FCO2_Coeffs[0] := REFERENCE_CO2;
  FCO2_Conc      := REFERENCE_CO2;
end;

{==============================================================================}
{ computeVP( Single[], Single[] )                                              }
{ Computes the daily average vapour pressure, in kPa, from one or more         }
{ temperature-relative humidity pairs and stores it in Data[wdtVP]             }
{                                                                              }
{ Parameters:                                                                  }
{   fTemperature     Temperatures         deg C                                }
{   fRelHum          Relative humidities  0-1                                  }
{ Assumptions:                                                                 }
{ * fTemperature and fRelHum have the same length, which is at least 1.        }
{==============================================================================}

procedure TWeatherHandler.computeVP( const fTemperature, fRelHum : array of Single );
var
  fVP : Single;
  Idx : Integer;
begin
  fVP := 0.0;
  for Idx := 0 to Length(fTemperature)-1 do
    fVP := fVP + fSaturatedVP(fTemperature[Idx]) * fRelHum[Idx];
  fVP := fVP / Length(fTemperature);

  Data[wdtVP] := fVP;
end;

{==============================================================================}
{ computeVP()                                                                  }
{ Computes the daily average vapour pressure, in kPa, and stores it in         }
{ Data[wdtVP]. Computation is done as follows:                                 }
{ 1. If relative humidity is available, it is computed from that.              }
{ 2. Otherwise, VP is computed by assuming that dew point = min. T.            }
{==============================================================================}

procedure TWeatherHandler.computeVP;
var
  fSVP      : Single;
  fDewPoint : Single;
begin
  if bInputKnown( wdtRelH ) then
  begin
    fSVP := 0.75 * fSaturatedVP(Data[wdtMaxT]) + 0.25 * fSaturatedVP(Data[wdtMinT]);
    Data[wdtVP] := Data[wdtRelH] * fSVP;
  end
  else
  begin
    fDewPoint   := Data[wdtMinT];
    Data[wdtVP] := fSaturatedVP( fDewPoint );
  end;
end;

{==============================================================================}
{ HASFunc                                                                      }
{ Horizontal angle of the sun.  All three input angles are in radians          }
{==============================================================================}

function HASFunc( fLat, fDeclin, fHorizAngle : Single ) : Single;
var
  fCosine : Single;
begin
  fCosine := (Cos(fHorizAngle) - Sin(fLat)*Sin(fDeclin)) / (Cos(fLat)*Cos(fDeclin));
  if (fCosine <= -1.0) then
    Result := 12.0 * OMEGA
  else if (fCosine >= 1.0) then
    Result := 0.0
  else
    Result := ArcCos( fCosine );
end;

{==============================================================================}
{ computeDayValues                                                             }
{ Computes daylengths and E-T radiation                                        }
{==============================================================================}

procedure TWeatherHandler.computeDLandETR( iDOY : Integer; var fDL0, fDLC, fETR : Single );
var
  fLatRad,
  fSlopeRad,
  fAspectRad,
  fDeclination,                                                                { Declination of the sun (rad)          }
  fSunRiseRad,                                                                 { These are in radians, not hours       }
  fSunSetRad,
  fEquivLat,                                                                   { Latitude "equivalent" to the slope/aspect}
  fHalfDay0,                                                                   { Half-daylength on flat surface (rad)  }
  fHalfDayE,                                                                   { Ditto  at "equivalent" latitude       }
  fAlpha,
  fDenom       : Single;
  bCivil       : Boolean;

BEGIN
  fLatRad      := DEG2RAD * fLatDegrees;                                       { Convert latitude, slope and aspect to }
  fSlopeRad    := DEG2RAD * fSlopeDegrees;                                     {   radians                             }
  fAspectRad   := DEG2RAD * fAspectDegrees;

  fDeclination := MAXDECLIN * Cos( DAY2RAD * (iDOY-DECLINBASE) );

  fDenom := Cos(fSlopeRad) * Cos(fLatRad)                                      { Trap e.g. flat surface at equator     }
            - Cos(fAspectRad) * Sin(fSlopeRad) * Sin(fLatRad);
  IF (Abs(fDenom) < 1.0E-8) THEN
    fAlpha := Sign( Sin(fSlopeRad) * Sin(fAspectRad) ) * Pi/2.0
  ELSE
    fAlpha := ArcTan( Sin(fSlopeRad) * Sin(fAspectRad) / fDenom );

  fEquivLat := ArcSin( Sin(fSlopeRad) * Cos(fAspectRad) * Cos(fLatRad)         { Determine the "equivalent latitude"   }
                       + Cos(fSlopeRad) * Sin(fLatRad) );

  for bCivil := TRUE downto FALSE do                                           { Do bCivil=FALSE last so that fSunRise }
  begin                                                                        {   and fSunSet are set for the E-T     }
    fHalfDay0   := HASFunc( fLatRad,   fDeclination, DAYANGLE[bCivil] );       {   radiation calculation               }
    fHalfDayE   := HASFunc( fEquivLat, fDeclination, DAYANGLE[bCivil] );
    fSunRiseRad := Max( -fHalfDay0, -fHalfDayE-fAlpha );
    fSunSetRad  := Min( +fHalfDay0, +fHalfDayE-fAlpha );
    if bCivil then
      fDLC :=(fSunSetRad - fSunRiseRad) / OMEGA                                { Convert daylength to hours here       }
    else
      fDL0 :=(fSunSetRad - fSunRiseRad) / OMEGA;
  end;

  fETR := SOLARCONST / WM2_2_MJM2 / 24.0                                       { Extra-terrestrial radiation           }
          * (1.0 + ECCENTRICITY * Cos(DAY2RAD*iDOY))                           {   calculation                         }
          * ( fDL0 * Sin(fDeclination) * Sin(fEquivLat)
              + (Sin(fSunSetRad+fAlpha) - Sin(fSunRiseRad+fAlpha)) / OMEGA
                * Cos(fDeclination) * Cos(fEquivLat) );
end;

{==============================================================================}
{ ET_Radn_Ratio                                                                }
{==============================================================================}

function ET_Radn_Ratio( iDOY : Integer; fLat1, fLat2 : Single ) : Single;
var
  fDeclination : Single;
  fHalfDay1,
  fHalfDay2    : Single;
begin
  fLat1        := DEG2RAD * fLat1;
  fLat2        := DEG2RAD * fLat2;
  fDeclination := MAXDECLIN * Cos( DAY2RAD * (iDOY-DECLINBASE) );
  fHalfDay1    := HASFunc( fLat1, fDeclination, DAYANGLE[FALSE] );
  fHalfDay2    := HASFunc( fLat2, fDeclination, DAYANGLE[FALSE] );
  Result       := ( fHalfDay1 * Sin(fDeclination) * Sin(fLat1) + Sin(fHalfDay1) * Cos(fDeclination) * Cos(fLat1) )
                / ( fHalfDay2 * Sin(fDeclination) * Sin(fLat2) + Sin(fHalfDay2) * Cos(fDeclination) * Cos(fLat2) );
end;

{==============================================================================}
{ fDaylength                                                                   }
{ Daylength, in hours. Values are pre-computed when Today is set.              }
{                                                                              }
{ Parameter:                                                                   }
{   bCivil   If TRUE, the daylength value includes civil twilight.             }
{==============================================================================}

function TWeatherHandler.fDaylength( bCivil : Boolean = FALSE ) : Single;
begin
  Result := FDaylengths[bCivil];
end;

{==============================================================================}
{ bDaylengthIncreasing                                                         }
{ TRUE i.f.f. the daylength is increasing. The value is pre-computed when      }
{ Today is set.                                                                }
{==============================================================================}

function TWeatherHandler.bDaylengthIncreasing : Boolean;
begin
  Result := FDayLenIncr;
end;

{==============================================================================}
{ fExtraT_Radiation                                                            }
{ Extra-terrestrial radiation, in MJ/m^2/d. The value is pre-computed when     }
{ Today is set.                                                                }
{==============================================================================}

function TWeatherHandler.fExtraT_Radiation : Single;
begin
  Result := FET_Radn;
end;

{==============================================================================}
{ fNetRadiation                                                                }
{ Net radiation estimate.                                                      }
{ This calculation follows Allen et al (1998)                                  }
{==============================================================================}

function TWeatherHandler.fNetRadiation( fAlbedo : Single ) : Single;
var
  fRadClearDay : Single;
  fRadFract    : Single;
  fNetLongWave : Single;
begin
  fRadClearDay := (0.75 + 2.0E-5 * fElevationM) * fExtraT_Radiation;
  fRadFract    := Min( 1.0, Data[wdtRadn] / fRadClearDay );
  fNetLongWave := SIGMA * (Power(Data[wdtMaxT]+C_2_K,4) + Power(Data[wdtMinT]+C_2_K,4)) / 2.0
                  * (0.34 - 0.14 * Sqrt( Data[wdtVP] ))
                  * (1.35 * fRadFract - 0.35);

  Result       := (1.0 - fAlbedo) * Data[wdtRadn] - fNetLongWave;
end;

{==============================================================================}
{ fMeanTemp                                                                    }
{ Mean daily temperature, taken as the average of maximum and minimum T        }
{==============================================================================}

function TWeatherHandler.fMeanTemp : Single;
begin
  if not bInputKnown( [wdtMaxT,wdtMinT] ) then
    raise Exception.Create( 'Weather handler: Mean temperature cannot be calculated' );

  Result := 0.5 * (Data[wdtMaxT] + Data[wdtMinT]);
end;

{==============================================================================}
{ fMeanDayTemp                                                                 }
{ Equation for the mean temperature during daylight hours.  Integrated from a  }
{ model of Parton & Logan (1981), Agric.Meteorol. 23:205                       }
{==============================================================================}

function TWeatherHandler.fMeanDayTemp : Single;
var
  fDayLen : Single;
begin
  if not bInputKnown( [wdtMaxT,wdtMinT] ) then
    raise Exception.Create( 'Weather handler: Mean daytime temperature cannot be calculated' );

  fDayLen := fDaylength( TRUE );
  Result  := Data[wdtMinT] + (Data[wdtMaxT] - Data[wdtMinT])
                             * (1.0 + 2.0*PARTON_A/fDayLen)
                             * (Cos(   -PARTON_C         /(fDayLen+2.0*PARTON_A) * Pi )
                                - Cos( (fDayLen-PARTON_C)/(fDayLen+2.0*PARTON_A) * Pi ))
                             / Pi;
end;

{==============================================================================}
{ fSaturatedVP                                                                 }
{ Saturated vapour pressure at a given temperature, in kPa.                    }
{ The equation is taken from Allen et al (1998), FAO Irrigation and Drainage   }
{ Paper 56.                                                                    }
{==============================================================================}

function TWeatherHandler.fSaturatedVP( fTemperature : Single )  : Single;
begin
  Result := 0.6108 * Exp( 17.27 * fTemperature / (fTemperature+237.3) );
end;

{==============================================================================}
{ fSaturatedVP                                                                 }
{ Derivative of the SVP-temperature curve above                                }
{==============================================================================}

function TWeatherHandler.fSatVPSlope( fTemperature : Single )  : Single;
begin
  Result := 17.27 * 237.3 / Sqr( fTemperature+237.3 ) * fSaturatedVP( fTemperature );
end;

{==============================================================================}
{ fVP_Deficit                                                                  }
{ Daily average vapour pressure deficit                                        }
{ * The weighting in the saturated VP calculation follows Jeffrey et al.       }
{   (2001), Env. Modelling & Software 16:309                                   }
{ * In the absence of VP or RH data, assumes that dew point can be approximated}
{   by the minimum temperature.                                                }
{==============================================================================}

function TWeatherHandler.fVP_Deficit : Single;
var
  fSVP : Single;
begin
  fSVP := 0.75 * fSaturatedVP(Data[wdtMaxT]) + 0.25 * fSaturatedVP(Data[wdtMinT]);

  if not bInputKnown( wdtVP ) then
    computeVP;
  Result := fSVP - Data[wdtVP];
end;

{==============================================================================}
{ fPsychrometricConst                                                          }
{ Psychrometric constant, following Allen et al (1998).                        }
{ * The equation for atmospheric pressure is also taken from Allen et al.      }
{==============================================================================}

function TWeatherHandler.fPsychrometricConst : Single;
var
  fPressure : Single;
begin
  fPressure := 101.3 * Power( 1.0 - 0.0065 * fElevationM/(20.0+C_2_K), 5.26 ); { Atmospheric pressure, in kPa          }
  Result    := 6.65E-4 * fPressure;
end;

{==============================================================================}
{ fLatentHeat                                                                  }
{ Latent heat of vaporization of water, in MJ/kg                               }
{==============================================================================}

function TWeatherHandler.fLatentHeat : Single;
begin
  Result := 2.501 - 0.002361 * fMeanTemp;
end;

{==============================================================================}
{ fEvaporation                                                                 }
{ Evapotranspiration calculation using various methods.                        }
{ * All methods return potential evaporation except pmPenman, which returns    }
{   the estimated evaporation rate for a nominated surface resistance.         }
{ * The meaning of fParam1, fParam2 and fParam3 depends upon the method:       }
{     Method      Param.  Meaning                          Unit  Default       }
{     emPropnPan    1     potential:pan evaporation ratio  -     0.8           }
{     emPenman      1     albedo                           -     0.23          }
{                   2     crop height                      m     0.12          }
{                   3     surface resistance               s/m   70.0          }
{     emCERES       1     albedo                           -     0.23          }
{     emPriestley   1     albedo                           -     0.23          }
{                   2     potential:equilibrium ratio      -     1.26          }
{     emFAO         1     albedo                           -     0.23          }
{                   2     crop coefficient                 -     1.0           }
{==============================================================================}

function TWeatherHandler.fEvaporation( Method  : TEvapMethod;
                                       fParam1 : Single = 0.0;
                                       fParam2 : Single = 0.0;
                                       fParam3 : Single = 0.0 ) : Single;
begin
  if (fParam1 = 0.0) then
    fParam1 := EVAPDEFAULTS[Method,1];
  if (fParam2 = 0.0) then
    fParam2 := EVAPDEFAULTS[Method,2];
  if (fParam3 = 0.0) then
    fParam3 := EVAPDEFAULTS[Method,3];

  case Method of
    emPropnPan  : begin
                    Result := fParam1 * Data[wdtEpan];
                    if (Result >= 12.0) then                                   { Large E(pan) values are untrustworthy }
                      Result := Min( Result, CeresPET(HERBAGE_ALBEDO) );
                  end;
    emPenman    : Result := PenmanPET(    fParam1, fParam2, fParam3 );
    emCERES     : Result := CeresPET(     fParam1 );
    emPriestley : Result := PriestleyPET( fParam1, fParam2 );
    emFAO       : Result := fParam2 * FAO_ReferenceET( fParam1 );
    else          Result := 0.0;
  end;
end;

{==============================================================================}
{ CeresPET                                                                     }
{ Potential evaporation estimator using the logic from the CERES suite of crop }
{ growth models. This estimator is a variant of the Priestley-Taylor estimator }
{==============================================================================}

function TWeatherHandler.CeresPET( fAlbedo : Single ) : Single;
var
  fMeanT   : Single;
  fEEQ     : Single;
  fEEQ_FAC : Single;
begin
  if not bInputKnown( EVAP_REQUIREMENT[emCERES] ) then
    raise Exception.Create( 'Weather handler: CERES evaporation cannot be calculated' );

  fMeanT  := 0.6 * Data[wdtMaxT] + 0.4 * Data[wdtMinT];
  fEEQ    := Data[wdtRadn] * 23.8846*(0.000204-0.000183*fAlbedo)*(29.0+fMeanT);
  if (Data[wdtMaxT] >= 5.0) then
    fEEQ_FAC := 1.1 + 0.05 * Max( 0.0, Data[wdtMaxT]-35.0 )
  else
    fEEQ_FAC := 0.01 * Exp( 0.18 * (Data[wdtMaxT] + 20.0) );
  Result := fEEQ_FAC * fEEQ;
end;

function TWeatherHandler.PenmanPET( fAlbedo, fCropHeight, fSurfaceResist : Single ) : Single;
var
  fDelta             : Single;
  fGamma             : Single;
  fVirtualT          : Single;                                                 { "Virtual temperature" for gas law, K  }
  fRho_Cp            : Single;                                                 { Density x specific heat of air, MJ/m^3/K }
  fSoilFlux          : Single;                                                 { Soil heat flux G, MJ/m^2/d            }
  fZeroPlaneDispl    : Single;                                                 { Zero plane displacement d, m          }
  fRoughLengthMom    : Single;                                                 { Roughness length for momentum z(om), m}
  fRoughLengthVapour : Single;                                                 { Roughness length for heat & water     }
                                                                               {   vapour z(oh), m                     }
  fAeroResist        : Single;                                                 { Aerodynamic resistance, s/m and d/m   }
begin
  if not bInputKnown( EVAP_REQUIREMENT[emPenman] ) then
    raise Exception.Create( 'Weather handler: Penman-Monteith evaporation cannot be calculated' );

  fDelta      := fSatVPSlope( fMeanTemp );
  fGamma      := fPsychrometricConst;

  fVirtualT   := 1.01 * (fMeanTemp + C_2_K);
  fRho_Cp     := fGamma * EPSILON * fLatentHeat / (GAS_CONST * fVirtualT);

  fSoilFlux   := 0.0;

  fZeroPlaneDispl    := 2/3   * Max( 0.005, fCropHeight );                     { Set a floor of 5mm on the height used }
  fRoughLengthMom    := 0.123 * Max( 0.005, fCropHeight );                     {   in calculating the aerodynamic      }
  fRoughLengthVapour := 0.1   * Max( 0.005, fCropHeight );                     {   resistance                          }
  fAeroResist        := Ln( (MEASURE_HEIGHT-fZeroPlaneDispl)/fRoughLengthMom   { Aerodynamic resistance in s/m         }
                            * (MEASURE_HEIGHT-fZeroPlaneDispl)/fRoughLengthVapour )
                        / (Sqr(KARMAN) * Data[wdtWind]);

  fAeroResist       := fAeroResist * SEC2DAY;                                  { Convert the resistances to d/m for    }
  fSurfaceResist    := fAeroResist * SEC2DAY;                                  {   consistency with other units        }

  Result := (fDelta * (fNetRadiation(fAlbedo) - fSoilFlux) + fRho_Cp * fVP_Deficit / fAeroResist)
            / (fDelta + fGamma * (1.0 + fSurfaceResist/fAeroResist))
            / fLatentHeat;
end;

{==============================================================================}
{ PriestleyPET                                                                 }
{ Priestley-Taylor potential evapotranspiration function                       }
{==============================================================================}

function TWeatherHandler.PriestleyPET( fAlbedo, fScalar : Single ) : Single;
var
  fDelta : Single;
  fGamma : Single;
begin
  if not bInputKnown( EVAP_REQUIREMENT[emPriestley] ) then
    raise Exception.Create( 'Weather handler: Priestley-Taylor evaporation cannot be calculated' );

  fDelta := fSatVPSlope( fMeanTemp );
  fGamma := fPsychrometricConst;
  Result := fScalar * fDelta/(fDelta+fGamma) * fNetRadiation(fAlbedo) / fLatentHeat;
end;

{==============================================================================}
{ FAO_ReferenceET                                                              }
{ Potential evaporation estimator using the logic from the CERES suite of crop }
{ growth models. This estimator is a variant of the Priestley-Taylor estimator }
{==============================================================================}

function TWeatherHandler.FAO_ReferenceET( fAlbedo : Single ) : Single;
var
  fDelta    : Single;
  fGamma    : Single;
  fSoilFlux : Single;
begin
  if not bInputKnown( EVAP_REQUIREMENT[emFAO] ) then
    raise Exception.Create( 'Weather handler: Reference evaporation cannot be calculated' );

  fDelta    := fSatVPSlope( fMeanTemp );
  fGamma    := fPsychrometricConst;
  fSoilFlux := 0.0;
  Result    := (0.408 * fDelta * (fNetRadiation(fAlbedo) - fSoilFlux)
                + fGamma * 900.0/(fMeanTemp+C_2_K) * Data[wdtWind] * fVP_Deficit)
               / (fDelta + fGamma * (1.0 + 0.34 * Data[wdtWind]));
end;


{==============================================================================}
{ bInputKnown( TWeatherData )                                                  }
{ Returns TRUE i.f.f. the data value for the weather element has been assigned }
{ since Today was last set.                                                    }
{==============================================================================}

function TWeatherHandler.bInputKnown( Query : TWeatherData ) : Boolean;
begin
  Result := FDataKnown[Query];
end;

{==============================================================================}
{ bInputKnown( TWeatherSet )                                                   }
{ Returns TRUE i.f.f. the data value for all members of Query has been assigned}
{ since Today was last set.                                                    }
{==============================================================================}

function TWeatherHandler.bInputKnown( Query : TWeatherSet ) : Boolean;
var
  D : TWeatherData;
begin
  Result := TRUE;
  for D := Low(TWeatherData) to High(TWeatherData) do
    Result := Result and (FDataKnown[D] or not (D in Query));
end;

{==============================================================================}
{ setToday                                                                     }
{==============================================================================}

procedure TWeatherHandler.setToday( iDay, iMonth, iYear : Integer );
var
  Dt         : StdDATE.Date;
  fOldDayLen : Single;
  D          : TWeatherData;
  X, XN      : Double;
  Idx        : Integer;
begin
  Dt := DateVal( iDay, iMonth, iYear );

  if (FToday = 0) or (DateShift(FToday,1,0,0) <> Dt) then                      { Compute yesterday's day length where  }
  begin                                                                        {   it isn't already known              }
    FToday := DateShift( Dt, -1, 0, 0 );
    computeDLandETR( StdDATE.DOY(FToday,TRUE), FDayLengths[FALSE], FDayLengths[TRUE], FET_Radn );
  end;

  FToday := Dt;
  for D := Low(TWeatherData) to High(TWeatherData) do
    FDataKnown[D] := FALSE;

  fOldDayLen := fDaylength;
  computeDLandETR( StdDATE.DOY(FToday,TRUE), FDayLengths[FALSE], FDayLengths[TRUE], FET_Radn );
  FDayLenIncr := fDayLength > fOldDayLen;

  FCO2_Conc := FCO2_Coeffs[0];
  if (FCO2_Order > 0) then                                                     { Polynomial time course for [CO2]      }
  begin
    X  := Interval( DateVal(1,1,2001), FToday ) / 365.25;                      { Years since reference date            }
    XN := 1;
    for Idx := 1 to FCO2_Order do
    begin
      XN        := XN * X;
      FCO2_Conc := FCO2_Conc + FCO2_Coeffs[Idx] * XN;
    end;
  end;
end;

{==============================================================================}
{ setCO2Trajectory                                                             }
{==============================================================================}

procedure TWeatherHandler.setCO2Trajectory( const Coeffs : TCO2Array );
var
  Idx : Integer;
begin
  FCO2_Order := Length(Coeffs)-1;
  while (FCO2_Order > 0) and (Coeffs[FCO2_Order] = 0.0) do
    Dec( FCO2_Order );

  if (FCO2_Order > 5) then
    raise Exception.Create( '[CO2] polynomial must have order <= 6' );

  for Idx := 0 to FCO2_Order do
    FCO2_Coeffs[Idx] := Coeffs[Idx];
  for Idx := FCO2_Order+1 to 5 do
    FCO2_Coeffs[Idx] := 0.0;
end;

{==============================================================================}
{ getData                                                                      }
{==============================================================================}

function TWeatherHandler.getData( D : TWeatherData ) : Single;
begin
  Result := FData[D];
end;

{==============================================================================}
{ setData                                                                      }
{==============================================================================}

procedure TWeatherHandler.setData( D : TWeatherData; fValue : Single );
begin
  FData[D]      := fValue;
  FDataKnown[D] := TRUE;
end;



*/