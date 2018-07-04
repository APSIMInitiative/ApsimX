library('sensitivity')
Params<-c(%PARAMNAMES%)
    APSIMMorris<-morris(model=NULL
        ,Params #string vector of parameter names
        ,%NUMPATHS% #no of paths within the total parameter space
        ,design=list(type="oat",levels=20,grid.jump=10) #design type for parameter space - grid jump should be half levels
        ,binf=c(%PARAMLOWERS%) #min for each parameter
        ,bsup=c(%PARAMUPPERS%) #max for each parameter
        ,scale=T #scale before computing elementary effects (SHOULD BE TRUE)
        )

write.table(APSIMMorris$X, row.names=F, col.names=T, sep=",")