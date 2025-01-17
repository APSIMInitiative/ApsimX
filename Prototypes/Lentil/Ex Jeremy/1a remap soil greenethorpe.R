rm(list=ls())
library(dplyr)
library(purrr)

library(sf)
library(sp)
library(ggplot2)
library(gstat)
library(geoR)
setwd("~/work/Projects/GRDC Pulse 2019/Lentils/Third Cut/")
df0 <- read.xlsx("SWGreenethorpe2019.xlsx", 1) %>%
  filter(MixedCropType == "Lentil" & EventName == "PreSowing"  ) %>% 
  mutate(Clock.Today = as.Date(SampleDate,  origin="1900-01-01"))

phys <- data.frame(
  profile = "apsim",
  midpoint = c(5, 15, 35, 65, 95, 125, 155, 185),
  LayerDepth_cm = c(10, 20, 50, 80, 110, 140, 170, 200),
  sat = c(0.43, 0.37, 0.37, 0.42, 0.41, 0.38, 0.38, 0.38),
  dul = c(0.23, 0.25, 0.3, 0.32, 0.32, 0.32, 0.33, 0.34),
  ll15 = c(0.1, 0.1, 0.13, 0.19, 0.21, 0.24, 0.25, 0.27),
  cll = c(0.102, 0.102, 0.156, 0.213, 0.241, 0.253, 0.33, 0.34)) 

# Map observed profile thicknesses onto apsim profile
df1 <- df0 %>% 
  mutate(Variety = gsub("_2", "2", gsub("PBA_", "", Variety)),
         TOS=paste0("TOS", TOS),
         layerName = paste0("Soil.Water.SW(", LayerNo, ")")) %>%
  select(-c(ExpNo, TOSDate, MixedCropType,  EventName)) %>%
  group_by(PlotNo, Clock.Today) %>%
  arrange(PlotNo, Clock.Today, LayerDepth_cm) %>%
  reframe(PlotNo = unique(PlotNo),
          Clock.Today = unique(Clock.Today),
          Variety = unique(Variety),
          TOS = unique(TOS),
          WaterTrt = unique(WaterTrt),
          SimulationName = paste0("GreeneThorpe19", TOS, "Cv", Variety, WaterTrt),
          LayerDepth2_cm = phys$LayerDepth_cm,
          Vol_mm.mm = approx(x=LayerDepth_cm, y=Vol_mm.mm, xout=LayerDepth2_cm, rule=2)$y) %>%
  mutate(VolBounded_mm.mm = pmax(Vol_mm.mm, phys$ll15)) %>%
  rename(LayerDepth_cm = "LayerDepth2_cm")
#nest() %>% # data=c(LayerDepth_cm, Vol_mm.mm)) %>%
#mutate(Vol2 = lapply(data, function(df) lm(Vol_mm.mm ~ LayerDepth_cm, method="loess", data = df)))

#pdf("remap.pdf", width=5, height=5)
plots <- unique(df0$PlotNo)
for (pane in seq(1, length(plots), 4)) {
  
    # raw data as points
    t1 <- df0 %>%
      filter(PlotNo %in% plots[pane:(pane+3)]) %>%
      select(PlotNo, LayerDepth_cm, Vol_mm.mm)
    
    # Points are the obseravtions
    # Vertices of the green line (and horizontal lines) are used by apsim
    g<- df1 %>% 
      filter(PlotNo %in% plots[pane:(pane+3)]) %>%
      ggplot( aes(x =  LayerDepth_cm) ) +
      geom_vline(data=phys, aes(xintercept=LayerDepth_cm), colour = "grey")  +
      geom_line(aes(y = Vol_mm.mm), colour = "green")  +
      geom_text(data= t1, aes(x=-Inf, y=first(Vol_mm.mm), hjust=0, vjust=1, label="Obs")) +
      geom_point(data= t1, aes( y = Vol_mm.mm), colour = "green")  +
      geom_line(aes( y = VolBounded_mm.mm ), colour = "purple")  +
      geom_line(data=phys, aes( y = ll15), colour = "blue")  +
      geom_text(data=phys, aes(x=Inf, y=last(ll15), hjust=0, vjust=0, label="LL15")) +
      geom_line(data=phys, aes( y = dul), colour = "orange")  +
      geom_text(data=phys, aes(x=Inf, y=last(dul), hjust=0, vjust=0, label="DUL")) +
      geom_line(data=phys, aes( y = sat), colour = "red")  +
      geom_text(data=phys, aes(x=Inf, y=last(sat), hjust=0, vjust=0, label="SAT")) +
      scale_x_reverse() + 
      labs(x="Depth (mm)", y= "SW (mm/mm)") +
      coord_flip() +
      theme_minimal() + 
      theme(panel.grid.minor.y =  element_line(colour="white", size=0.5),
            panel.grid.major.y =  element_line(colour="white", size=0.5)) +
      facet_wrap(~PlotNo)
   print(g)
}

# Map observed profile thicknesses onto apsim profile
df1 <- df0 %>% 
  mutate(Variety = gsub("_2", "2", gsub("PBA_", "", Variety)),
         TOS=paste0("TOS", TOS),
         Vol_mm.mm = ifelse(Vol_mm.mm < 0, -1, 1) * Vol_mm.mm) %>%
  select(-c(ExpNo, TOSDate, MixedCropType,  EventName)) %>%
  group_by(TOS, WaterTrt, Clock.Today, LayerDepth_cm) %>%
  summarise(Vol_mm.mm = mean(Vol_mm.mm))  %>%
  group_by(TOS, WaterTrt, Clock.Today) %>%
  arrange(TOS, WaterTrt, Clock.Today, LayerDepth_cm) %>%
  reframe(Clock.Today = unique(Clock.Today),
          TOS = unique(TOS),
          WaterTrt = unique(WaterTrt),
          LayerDepth2_cm = phys$LayerDepth_cm,
          Vol_mm.mm = approx(x=LayerDepth_cm, y=Vol_mm.mm, xout=LayerDepth2_cm, rule=2)$y) %>%
  mutate(VolBounded_mm.mm = pmax(Vol_mm.mm, phys$ll15)) %>%
  rename(LayerDepth_cm = "LayerDepth2_cm")

t1 <- df0 %>%
  mutate(TOS=paste0("TOS", TOS),
         Vol_mm.mm = ifelse(Vol_mm.mm < 0, -1, 1) * Vol_mm.mm) %>%
  select(TOS, WaterTrt, LayerDepth_cm, Vol_mm.mm)

g<- df1 %>% 
  ggplot( aes(x =  LayerDepth_cm) ) +
  geom_vline(data=phys, aes(xintercept=LayerDepth_cm), colour = "grey")  +
  geom_line(aes(y = Vol_mm.mm), colour = "green")  +
  geom_text(data= t1, aes(x=-Inf, y=first(Vol_mm.mm), hjust=0, vjust=1, label="Obs")) +
  geom_point(data= t1, aes( y = Vol_mm.mm), colour = "green")  +
  geom_line(aes( y = VolBounded_mm.mm ), colour = "purple")  +
  geom_line(data=phys, aes( y = ll15), colour = "blue")  +
  geom_text(data=phys, aes(x=Inf, y=last(ll15), hjust=0, vjust=0, label="LL15")) +
  geom_line(data=phys, aes( y = dul), colour = "orange")  +
  geom_text(data=phys, aes(x=Inf, y=last(dul), hjust=0, vjust=0, label="DUL")) +
  geom_line(data=phys, aes( y = sat), colour = "red")  +
  geom_text(data=phys, aes(x=Inf, y=last(sat), hjust=0, vjust=0, label="SAT")) +
  scale_x_reverse() + 
  labs(x="Depth (mm)", y= "SW (mm/mm)") +
  coord_flip() +
  theme_minimal() + 
  theme(panel.grid.minor.y =  element_line(colour="white", size=0.5),
        panel.grid.major.y =  element_line(colour="white", size=0.5)) +
  facet_wrap(~WaterTrt + TOS)
print(g)

write.csv(df1 %>%
            select(-c(Vol_mm.mm)) %>%
            rename(Vol_mm.mm = "VolBounded_mm.mm"),
          "ISWGreenethorpe2019.csv", row.names = F)

stop()

#dev.off()
# need to get this to treatment level somehow ...# 

df2 <- df1 %>%
  st_as_sf( coords=c("??Eastings_AA", "??Northings_AA")) 

for (layer_cm in unique(df1$LayerDepth_cm)) {
  thisLayer <- df2[df2$LayerDepth_cm == layer,]
  list4likfit <- list('coords' = st_coordinates(thisLayer$geometry) , 
                      'data' = thisLayer$Vol_mm.mm)
  reml <- likfit(list4likfit , ini=c(0.5, 0.5), fix.nug = TRUE, nugget=250, 
               limits = pars.limits(phi=c(1.5 * min(dist(list4likfit$coords)), 
                                          max(dist(list4likfit$coords)))),
               lik.met = "REML")
  summary(reml)
  plot(variog(list4likfit))
  lines(reml, lty = 2)

  loci <- expand.grid(seq(min(list4likfit$coords[,1]),max(list4likfit$coords[,1]),l=51),
                      seq(min(list4likfit$coords[,2]),max(list4likfit$coords[,2]),l=51))

  s.out <- output.control(n.predictive = 10)
  krPrd <- krige.conv(list4likfit,
                      locations = loci , 
                      krige = krige.control(cov.pars=reml$cov.pars) ,
                      output = s.out)
  # do something with this layer..
}

d<- rbind(
  cbind(n="Prediction", data.frame(loci, pawc= krPrd$predict)),
  cbind(n="Simulation 1", data.frame(loci, pawc=krPrd$simulations[,1])),
  cbind(n="Simulation 2", data.frame(loci, pawc=krPrd$simulations[,2])),
  cbind(n="Simulation 3", data.frame(loci, pawc=krPrd$simulations[,3])))

stop("the end")

##########################################
# JSON Soils fr apsim
##############################################
fixupSoil <- function (oldSoil) {
  newSoil <- jsonClone(oldSoil)
  setValue(newSoil, "$.Name", "Soil")

  # Physical 
  old <-list()
  for (v in c("Thickness", "BD", "AirDry", "LL15", "DUL", "SAT")) {
    old[[v]]<- unlist(getValue(newSoil, paste0("$..[?(@.Name=='Physical')].", v), result="object"))
    old[[paste0(v, ".m")]]<- unlist(getValue(newSoil, paste0("$..[?(@.Name=='Physical')].", v, "Metadata"), result="object"))
  }
  old$dlayer <- cumsum(old$Thickness)
  
  new <- list(Thickness = c(150, 150, rep(200, 7)))
  setValue(newSoil, paste0("$..[?(@.Name=='Physical')].Thickness"), new$Thickness)
  new$dlayer <- cumsum(new$Thickness)
  
  # check to be sure..
  stopifnot(! any(is.na(sc$BD_g.cc)))
  setValue(newSoil, paste0("$..[?(@.Name=='Physical')].BD"), sc$BD_g.cc)
  
  for (v in c("AirDry", "LL15", "DUL", "SAT", "CropLL")) {
    new[[v]] <- sc[[paste0(v,"_mm.mm")]]
    new[[paste0(v, ".m")]] <- rep("Field measured (NaPA)", length(new[[v]]))
    missing <- is.na(new[[v]])
    if (any(missing)) {
      fn <- approxfun(old$dlayer, old[[v]], rule=2)
      new[[v]][missing] <- fn(new$dlayer[missing])
      new[[paste0(v, ".m")]][missing] <- "Interpolated from apsoil"
    }
    if (v != "CropLL") {
      setValue(newSoil, paste0("$..[?(@.Name=='Physical')].", v), new[[v]])
      setValue(newSoil, paste0("$..[?(@.Name=='Physical')].", v, "Metadata"), new[[paste0(v, ".m")]])
    }
    if (v == "LL15") {
      # ISW is set on day before sowing, this will be overwritten
      setValue(newSoil, paste0("$..[?(@.Name=='Physical')].SW"), new[[v]]) 
    }
  }
  
  # soil/Crop 
  mySoil <- getValue(newSoil, paste0("$..[?(@.Name=='", tolower(thisCrop), "Soil')]"), result="object")
  mySoil$LL <- new[["CropLL"]]
  mySoil$LLMetadata <- rep("NaPA", length(new[["CropLL"]]))
  
  fn <- approxfun(old$dlayer, unlist(mySoil$KL), rule=2)
  mySoil$KL <- fn(new$dlayer)
  mySoil$KLMetadata <- NA 
  
  mySoil$XF <- rep(1, length(new[["CropLL"]]))
  mySoil$XFMetadata <- NA 
  mySoil$ResourceName  <- NA
  setValue(newSoil, "$..[?(@.Name=='Physical')].Children", list(mySoil))
  
  # Soilwat
  setValue(newSoil, "$..[?(@.Name=='SoilWater')].Thickness", new$Thickness)
  setValue(newSoil, "$..[?(@.Name=='SoilWater')].SWCON", rep(0.3, length(new$Thickness)))
  
  # Carbon/organic
  dlayer <- cumsum(unlist(getValue(newSoil, "$..[?(@.Name=='Organic')].Thickness", result="object")))
  if (any(is.na(sc$OC_pc))) {
    fn <- approxfun(dlayer, unlist(getValue(newSoil, "$..[?(@.Name=='Organic')].Carbon", result="object")), rule=2)
    setValue(newSoil, "$..[?(@.Name=='Organic')].Carbon", fn(new$dlayer))
  } else {
    setValue(newSoil, "$..[?(@.Name=='Organic')].Carbon", sc$OC_pc)
  }
  # Fixme: I'm not sure about remapping FOM. It should be a total weight and distribution curve?
  for(v in c("FOM", "FInert", "FBiom", "SoilCNRatio")) {
    fn <- approxfun(dlayer, unlist(getValue(newSoil, paste0("$..[?(@.Name=='Organic')].", v), result="object")), rule=2)
    setValue(newSoil, paste0("$..[?(@.Name=='Organic')].", v), fn(new$dlayer))
  }  
  setValue(newSoil, "$..[?(@.Name=='Organic')].Thickness", new$Thickness)
  
  # Chem
  if (any(is.na(sc$OC_pc))) {
    dlayer <- cumsum(unlist(getValue(newSoil, "$..[?(@.Name=='Chemical')].Thickness", result="object")))
    fn <- approxfun(dlayer, unlist(getValue(newSoil, "$..[?(@.Name=='Chemical')].PH", result="object")), rule=2)
    setValue(newSoil, "$..[?(@.Name=='Chemical')].PH", fn(new$dlayer))
  } else {
    setValue(newSoil, "$..[?(@.Name=='Chemical')].PH", sc$PH15_water)
  }
  setValue(newSoil, "$..[?(@.Name=='Chemical')].Thickness", new$Thickness)
  
  #writeLines(getValue(newSoil, "", result="string"), paste0(key$SiteName, " ", key$TrialType, ".soil.apsimx"))
}
