using System;
using System.Collections.Generic;
using System.Text;


class MicroMet
   {
   public event CanopyWaterBalanceDelegate Canopy_Water_Balance;
   [EventHandler] public void OnProcess()
      {
      CanopyWaterBalanceType CanopyWB = new CanopyWaterBalanceType();
      CanopyWB.Canopy = new CanopyWaterBalanceCanopyType[1];
      CanopyWB.Canopy[0] = new CanopyWaterBalanceCanopyType();
      CanopyWB.Canopy[0].name = "Plant";
      CanopyWB.Canopy[0].PotentialEp = 2.0F;

       if (Canopy_Water_Balance != null)
          Canopy_Water_Balance.Invoke(CanopyWB);
      }

   }
