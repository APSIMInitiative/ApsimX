df <- read.delim("vic.txt", sep = "")

df$sowDate <- as.Date(paste("2001", df$sow_day), format="%Y %j")
df$floweringDate <- as.Date(paste("2001", df$flowering_day), format="%Y %j")
df$floweringDAS <- as.integer(df$floweringDate - df$sowDate)
df$TOS <- case_when(df$sow_day == 123 ~ "TOS1",
                    df$sow_day == 154 ~ "TOS2",
                    df$sow_day == 165 ~ "TOS3",
                    df$sow_day == 186 ~ "TOS4",
                    df$sow_day == 130 ~ "TOS1",
                    df$sow_day == 168 ~ "TOS2",
                    df$sow_day == 193 ~ "TOS3")
write.csv(df[,c("location", "cultivar", "TOS", "floweringDAS")], "vic.csv")

df <- apsimx::read_apsimx("Lentil.db", simplify = F)$Harvests
