library(readxl)
library(lubridate)
library(glue)
library(dplyr)
library(tidyr)
library(purrr)

# Path to your Excel file
file_path <- "C:/Users/Cfleit/Downloads/2024_WaggaWagga_PHDA24WARI2.xlsx"
script_dir <- dirname(getActiveDocumentContext()$path)
file_path <- file.path(script_dir, "InputFilesFromCloud","2024_WaggaWagga_PHDA24WARI2.xlsx")
  
  
# Read sheet "Weather"
haun_raw <- read_excel(
  path = file_path,
  sheet = "Haun stage ", # note space in name
  col_types = "text"   # read everything as text first to control date parsing
) %>%
  dplyr::select(`Measurement date`,`Plant Haun stage`, `!Block`, `!Cultivar`, Plant, `!Habit`)

head(haun_raw)

# Prepare/Clean data
haun_worked <- haun_raw %>%
  mutate(
    Date = as.Date(ymd("1899-12-30") + as.numeric(`Measurement date`)),
    Haun = as.numeric(`Plant Haun stage`),
    Block= as.factor(`!Block`),
    Cultivar=as.factor(`!Cultivar`),
    Plant = as.factor(Plant),
    Habit = as.factor(`!Habit`)
  ) %>%
  dplyr::select(Date,Cultivar,Block,Plant,Haun, Habit)

#--------------------------
# INPUT:
# df with columns: Date, Block, Plant, Haun
# Date must be class Date
#--------------------------

# 1. Set base date
base_date <- as.Date(ymd("2024/04/22"))   # Haun = 0 for all blocks/plants

# 2. Get maximum Date
Date_mx <- max(haun_worked$Date, na.rm = TRUE)

# 3. Create daily skeleton for all Blocks & Plants
blocks <- unique(haun_worked$Block)
plants <- unique(haun_worked$Plant)
cultivars<-unique(haun_worked$Cultivar)

df_cv_habit <- haun_worked %>% dplyr::select(Cultivar, Habit) %>% unique()

date_grid <- expand_grid(
  Date = seq(base_date, Date_mx, by = "1 day"),
  Cultivar=cultivars,
  Block = blocks,
  Plant = plants
) %>%
  inner_join(df_cv_habit, by="Cultivar")

# 4. Add base date entry Haun = 0 for all blocks/plants
df_base <- expand_grid(
  Date = base_date,
  Cultivar=cultivars,
  Block = blocks,
  Plant = plants
) %>% 
  mutate(Haun = 0) %>%
  inner_join(df_cv_habit, by="Cultivar")  %>%
  mutate(Date = as.Date(ifelse(Habit == "Winter", ymd("2024/04/22"), ymd("2024/05/13")))) %>%
  dplyr::select(-Habit)

# ------------------------------------
# 5. Combine base row + original data
# ------------------------------------

df_all <- bind_rows(df_base,haun_worked)%>%
  dplyr::select(-Habit)

convert_haun_to_apsim <- function(Haun){
    
    # Anchor points
    haun_pts  <- c(0,   8,    10.5, 16)
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


# 6. Interpolate Haun per Block × Plant
#    using complete() to ensure full date sequence
#    and approx() for linear interpolation
df_interp <- df_all %>%
  arrange(Block, Plant, Cultivar, Date) %>%
  group_by(Block, Plant, Cultivar) %>%
  complete(Date = seq(min(Date), Date_mx, by = "1 day")) %>%
  arrange(Date) %>%
  mutate(
    Haun_interpolated = approx(
      x = Date[!is.na(Haun)],
      y = Haun[!is.na(Haun)],
      xout = Date,
      rule = 2   # extrapolate flat if needed
    )$y
  ) %>%
  ungroup() %>%
  group_by(Date,Cultivar,Block) %>%
  summarise(Haun_mean=mean(Haun_interpolated))%>%
  mutate(APSIM_Stage = convert_haun_to_apsim(Haun_mean))

#--------------------------
# OUTPUT:
# df_interp has:
#   Date, Block, Plant, Haun, Haun_interpolated
#--------------------------

df_interp

#input_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/inputs")
#input_path <- file.path(script_dir, "..", "..", "inputs")|> normalizePath()

# Save input parameter dates with obsvations and synthetic values
write.csv(df_interp,
          file.path(script_dir,
                    "Wagga2024PhenoStagesInterpolated.csv"),
          row.names = FALSE, quote = FALSE)
