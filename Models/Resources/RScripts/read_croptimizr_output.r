
df <- NULL
for (i in 1:length(nlo)) {
  row <- nlo[[i]]
  initial <- row$x0
  solution <- row$solution
  msg <- row$message
  objective <- row$objective
  iterations <- row$iterations

  rowdata <- c(i, objective, iterations)
  for (j in 1:length(param_names)) {
    rowdata <- c(rowdata, initial[j], solution[j])
  }
  rowdata <- c(rowdata, msg)
  df <- rbind(df, rowdata)
}

cols <- c('Repetition', 'Objective Function Value', 'Number of Iterations')
for (param in param_names) {
  cols <- c(cols, paste(param, 'Initial'))
  cols <- c(cols, paste(param, 'Final'))
}
cols <- c(cols, 'Message')
colnames(df) <- cols
write.table(df, row.names = F, col.names = T, sep = ',')