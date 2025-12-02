# Path to your Excel file
file_path <- "C:/Users/Cfleit/Downloads/2024_WaggaWagga_PHDA24WARI2.xlsx"

df_sim_names <- read.csv2(file.path(wagga_path, "CultivarToSimName.csv"),
                          header = TRUE, stringsAsFactors = TRUE, sep = ",",
                          , check.names = FALSE)


read_observed_func <- function (SheetName, VarName, NewVarName, UnitCorrect) {
  
  df <- read_excel(
    path = file_path,
    sheet = SheetName,
    col_types = "text"
  )
  
  # Candidate date columns
  possible_cols <- c("Measurement date", "Harvest date", "Date")
  date_col <- intersect(possible_cols, names(df)) |> dplyr::first()
  
  if (is.null(date_col)) {
    stop("Cannot find Date column: none of 'Measurement date', 'Harvest date', or 'Date' found.")
  }
  
  parse_any_date <- function(x) {
    suppressWarnings({
      
      # 1. Try Excel serial number
      num <- suppressWarnings(as.numeric(x))
      if (!all(is.na(num))) {
        # keep only entries that are truly numeric
        out <- as.Date(num, origin = "1899-12-30")
        if (!all(is.na(out))) return(out)
      }
      
      # 2. Try dd/mm/yy
      # x <- lubridate::dmy(.data[[date_col]])
      out <- suppressWarnings(as.Date(x, format = "%d/%m/%y"))
      
      if (!all(is.na(out))) return(out)
      
      # 3. Try dd/mm/yyyy
      out <- suppressWarnings(as.Date(x, format = "%d/%m/%Y"))
      if (!all(is.na(out))) return(out)
      
      # 4. Try yyyy-mm-dd (ISO)
      out <- suppressWarnings(as.Date(x, format = "%Y-%m-%d"))
      if (!all(is.na(out))) return(out)
      
      # 5. Final fallback (may produce NA)
      suppressWarnings(as.Date(x))
    })
  }
  
  df <- df %>%
    mutate(
      Date = parse_any_date(.data[[date_col]]),
      Block = as.factor(`!Block`),
      !!NewVarName := as.numeric(.data[[VarName]]),
      Cultivar = as.factor(`!Cultivar`)
    ) %>%
    select(Date, Cultivar, Block, all_of(NewVarName)) %>%
  group_by(Date, Cultivar) %>%
    summarise(
      across(
        where(is.numeric),
        ~ replace(mean(.x, na.rm = TRUE)*UnitCorrect, 
                  is.nan(mean(.x, na.rm = TRUE)), NA)
      ),
      .groups = "drop"
    )
  
  return(df)
  
}


# --------------------------------------------------------------
# --- Get Observed Values of Selected Variables-----------------
# --------------------------------------------------------------

ndvi_raw <- read_observed_func("NDVI & height", "Plot NDVI", "NDVIModel.Script.NDVI",1)
height_raw <- read_observed_func("Canopy Height", "Canopy height (cm)", "Wheat.Leaf.Height",10)
haun_raw <- read_observed_func("Haun stage ", "Plant Haun stage", "Wheat.Phenology.HaunStage",1)
# Grain
grainSize_raw <- read_observed_func("PCDS 10.0 harvest", "Individual grain weight (mg)", "Wheat.Grain.Size",1)
grainNumber_raw <- read_observed_func("PCDS 10.0 harvest", "Grain number (grains/m²)", "Wheat.Grain.Number",1)
#Biomass
grainYield_raw <- read_observed_func("PCDS 10.0 harvest", "Grain dry matter (g/m²)", "Wheat.Grain.Wt",1)
stemYield_raw <- read_observed_func("PCDS 10.0 harvest", "Stem dry matter (g/m²)", "Wheat.Stem.Wt",1)
spikeYield_raw <- read_observed_func("PCDS 10.0 harvest", "Spike dry matter (g/m²)", "Wheat.Spike.Wt",1)
senescLeafYield_raw <- read_observed_func("PCDS 10.0 harvest", "Dead leaf dry matter (g/m²)", "Wheat.Leaf.Dead.Wt",1)
earYield_raw <- read_observed_func("PCDS 10.0 harvest", "Chaff dry matter (g/m²)", "Wheat.Ear.Wt",1)
totalAboveGround_raw <- read_observed_func("PCDS 10.0 harvest", 
                                           "Estimated dry matter at PCDS 10.0 (g/m²)","Wheat.AboveGround.Wt",1)

# Corrections and calculations
ndvi_raw <- ndvi_raw %>%
  mutate(
    Date = if_else(
      year(Date) == 2025,
      Date %m+% years(-1),   # subtract 1 year
      Date
    )
  )

summary(height_raw)


# Find PCDS
convert_haun_to_apsim <- function(Haun){
  
  # Anchor points
  haun_pts  <- c(0,   8,    10, 16) # These are not checked - mock values for test
  apsim_pts <- c(3,   6,     8, 11)
  
  # Linear interpolation with clipping to range
  APSIM_Stage <- approx(
    x = haun_pts,
    y = apsim_pts,
    xout = Haun,
    rule = 2   # clamp below min and above max instead of producing NAs
  )$y
  
  return(APSIM_Stage)
}
haun_raw$Wheat.Phenology.HaunStage
pcds_raw <- haun_raw %>%
  mutate(Wheat.Phenology.Stage = convert_haun_to_apsim(Wheat.Phenology.HaunStage)) %>%
  dplyr::select(-Wheat.Phenology.HaunStage)
# --------------------------------------------------------------
# --- Creates APSIM Observed File ------------------------------
# --------------------------------------------------------------

library(dplyr)
library(purrr)

df_list <- list(ndvi_raw,
                height_raw,
                haun_raw,
                grainSize_raw, 
                grainNumber_raw,
                stemYield_raw,
                senescLeafYield_raw,
                spikeYield_raw,
                totalAboveGround_raw,
                earYield_raw,
                pcds_raw,
                grainYield_raw)


df_final <- reduce(
  df_list,
  full_join,
  by = c("Date", "Cultivar")) %>%
  inner_join(df_sim_names, by="Cultivar") %>%
  mutate(
    Clock.Today = format(
      as.POSIXct(Date),
      "%d/%m/%Y 00:00:00"
    )) %>%
  dplyr::select(SimulationName, Clock.Today, everything(), -Cultivar, -Date)

obs_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/WaggaWagga")
obs_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/Dookie2024")


library(openxlsx)

wb <- createWorkbook()

addWorksheet(wb, "Observed")

writeData(wb, sheet = "Observed", x = df_final)

saveWorkbook(wb, file.path(obs_path, "DookieWagga2024.xlsx"), overwrite = TRUE)
















ndvi_raw <- read_excel(
  path = file_path,
  sheet = "NDVI & height",
  col_types = "text"   # read everything as text first to control date parsing
)





ndvi_worked <- ndvi_raw %>%
  mutate(Clock.Today = as.Date(ymd("1899-12-30") + as.numeric(`Measurement date`)), 
                               Block=as.factor(`!Block`),
                               NDVI=as.numeric(`Plot NDVI`),
                               Cultivar=as.factor(`!Cultivar`)
                               ) %>%
  dplyr::select(Clock.Today, Cultivar,Block, NDVI) %>%
  inner_join(df_sim_names, by="Cultivar")

# --------------------------------------------------------------
# --- Plant Height ---------------------------------------------
# --------------------------------------------------------------

ndvi_raw <- read_excel(
  path = file_path,
  sheet = "Canopy Height",
  col_types = "text"   # read everything as text first to control date parsing
)

