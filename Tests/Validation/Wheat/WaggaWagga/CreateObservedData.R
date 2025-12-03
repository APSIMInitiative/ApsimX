library(dplyr)
library(purrr)
library(openxlsx)
library(lubridate)
library(readxl)


# Path to the field measurements Excel file
file_path <- "C:/Users/Cfleit/Downloads/2024_WaggaWagga_PHDA24WARI2.xlsx"

wagga_path <- "C:/github/ApsimX/Tests/Validation/Wheat/WaggaWagga"

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
earYield_10_raw <- read_observed_func("PCDS 10.0 harvest", "Chaff dry matter (g/m²)", "Wheat.Ear.Wt",1)
grainYield_10_raw <- read_observed_func("PCDS 10.0 harvest", "Grain dry matter (g/m²)", "Wheat.Grain.Wt",1)

#Biomass
stemYield_6_raw <- read_observed_func("PCDS 6.0 harvest", "Stem dry matter (g/m²)", "Wheat.Stem.Wt",1)
stemYield_8_raw <- read_observed_func("PCDS 8.0 harvest", "Stem dry matter (g/m²)", "Wheat.Stem.Wt",1)
stemYield_10_raw <- read_observed_func("PCDS 10.0 harvest", "Stem dry matter (g/m²)", "Wheat.Stem.Wt",1)


spikeYield_6_raw <- read_observed_func("PCDS 6.0 harvest", "Spike dry matter (g/m²)", "Wheat.Spike.Wt",1)
spikeYield_8_raw <- read_observed_func("PCDS 8.0 harvest", "Spike dry matter (g/m²)", "Wheat.Spike.Wt",1)
spikeYield_10_raw <- read_observed_func("PCDS 10.0 harvest", "Spike dry matter (g/m²)", "Wheat.Spike.Wt",1)


senescLeafYield_6_raw <- read_observed_func("PCDS 6.0 harvest", "Dead leaf dry matter (g/m²)", "Wheat.Leaf.Dead.Wt",1)
senescLeafYield_8_raw <- read_observed_func("PCDS 8.0 harvest", "Dead leaf dry matter (g/m²)", "Wheat.Leaf.Dead.Wt",1)
senescLeafYield_10_raw <- read_observed_func("PCDS 10.0 harvest", "Dead leaf dry matter (g/m²)", "Wheat.Leaf.Dead.Wt",1)

greenLeaf_6_raw <- read_observed_func("PCDS 6.0 harvest","Grean leaf dry matter (g/m²)","Wheat.Leaf.Live.Wt",1)
greenLeaf_8_raw <- read_observed_func("PCDS 8.0 harvest","Grean leaf dry matter (g/m²)","Wheat.Leaf.Live.Wt",1)  


totalAboveGround_6_raw <- read_observed_func("PCDS 6.0 harvest", 
                                             "Estimated dry matter at PCDS 6.0 (g/m²)",
                                             "Wheat.AboveGround.Wt",1)

# BUG HERE FIXME
totalAboveGround_8_raw <- read_observed_func("PCDS 8.0 harvest", 
                                           #  "Test",
                                           "Estimated dry matter at PCDS 8.0 (g/m²)",
                                            "Wheat.AboveGround.Wt",1)

totalAboveGround_10_raw <- read_observed_func("PCDS 10.0 harvest", 
                                           "Estimated dry matter at PCDS 10.0 (g/m²)",
                                           "Wheat.AboveGround.Wt",1)



par_6_raw <- read_observed_func("PCDS 6.0 harvest", 
                                           "PARi at ground level","Wheat.Leaf.CoverTotal",1)

par_8_raw <- read_observed_func("PCDS 8.0 harvest", 
                              "PARi at ground level","Wheat.Leaf.CoverTotal",1)
#-----------------------------------------
# Hard coded corrections and calculations
#------------------------------------------

#ndvi (incorrect dates)
ndvi_raw <- ndvi_raw %>%
  mutate(
    Date = if_else(
      year(Date) == 2025,
      Date %m+% years(-1),   # subtract 1 year
      Date
    )
  )

summary(height_raw)

#Biomass (no dates available)
stemYield_6_raw$Date <- dmy("28/08/2024")
spikeYield_6_raw$Date <- dmy("28/08/2024")
senescLeafYield_6_raw$Date <- dmy("28/08/2024")
stemYield_8_raw$Date <- dmy("23/09/2024")
spikeYield_8_raw$Date <- dmy("23/09/2024")
senescLeafYield_8_raw$Date <- dmy("23/09/2024")
totalAboveGround_6_raw$Date <- dmy("28/08/2024")
totalAboveGround_8_raw$Date <- dmy("23/09/2024")


# par
par_6_raw$Date <- dmy("28/08/2024")
par_8_raw$Date <- dmy("23/09/2024")

# leaf
greenLeaf_6_raw$Date <- dmy("28/08/2024")
greenLeaf_8_raw$Date <- dmy("23/09/2024")


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

# Estimate APSIM Stage based on measured Haun
pcds_raw <- haun_raw %>%
  mutate(Wheat.Phenology.Stage = convert_haun_to_apsim(Wheat.Phenology.HaunStage)) %>%
  dplyr::select(-Wheat.Phenology.HaunStage)

# --------------------------------------------------------------
# --- Creates APSIM Observed File ------------------------------
# --------------------------------------------------------------


df_list <- list(
  # Pheno et al
  ndvi_raw,
  height_raw,
  haun_raw,
  pcds_raw,
  par_6_raw,
  par_8_raw,
  
  # Grain traits
  grainSize_raw,
  grainNumber_raw,
  grainYield_10_raw,
  
  # Total biomass
  totalAboveGround_6_raw,
  totalAboveGround_8_raw,
  totalAboveGround_10_raw,
  
  # Stem biomass
  stemYield_6_raw,
  stemYield_8_raw,
  stemYield_10_raw,
  
  # Spike biomass
  spikeYield_6_raw,
  spikeYield_8_raw,
  spikeYield_10_raw,
  
  # Senesced leaf biomass
  senescLeafYield_6_raw,
  senescLeafYield_8_raw,
  senescLeafYield_10_raw,
  
  # Green leaf biomass
  greenLeaf_6_raw,
  greenLeaf_8_raw,
  
  # Ear biomass
  earYield_10_raw
)


# merge

df_final <- bind_rows(df_list) %>%
  inner_join(df_sim_names, by = "Cultivar") %>%
  mutate(
    Clock.Today = format(
      as.POSIXct(Date),
      "%d/%m/%Y 00:00:00"
    )) %>%
  select(SimulationName, Clock.Today, everything(), -Cultivar, -Date)

summary(df_final %>% dplyr::select(Clock.Today, Wheat.AboveGround.Wt))

# save
wb <- createWorkbook()

addWorksheet(wb, "Observed")

writeData(wb, sheet = "Observed", x = df_final)

obs_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/WaggaWagga")
obs_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/Dookie2024")
obs_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/WaggaWagga2024")
saveWorkbook(wb, file.path(obs_path, "DookieWaggaWagga2024.xlsx"), overwrite = TRUE)








