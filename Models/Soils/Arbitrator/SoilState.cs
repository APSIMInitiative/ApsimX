// -----------------------------------------------------------------------
// <copyright file="SoilState.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Soils.Arbitrator
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Encapsulates the state of water and N in multiple zones.
    /// </summary>
    public class SoilState
    {
        /// <summary>The parent model.</summary>
        private IModel Parent;

        /// <summary>Initializes a new instance of the <see cref="SoilState"/> class.</summary>
        /// <param name="parent">The parent model.</param>
        public SoilState(IModel parent)
        {
            Parent = parent;
            Zones = new List<ZoneUptakes>();
        }

        /// <summary>Initialises this instance.</summary>
        public void Initialise(List<IModel> zones)
        {
            foreach (Zone Z in zones)
            {
                Soil soil = Apsim.Child(Z, typeof(Soil)) as Soil;
                if (soil != null)
                {
                    ZoneUptakes NewZ = new ZoneUptakes(Z);
                    NewZ.Water = soil.Water;
                    NewZ.NO3N = soil.NO3N;
                    NewZ.NH4N = soil.NH4N;
                    Zones.Add(NewZ);
                }
            }
        }

        /// <summary>Gets all zones in this soil state.</summary>
        public List<ZoneUptakes> Zones { get; private set; }

        /// <summary>Implements the operator -.</summary>
        /// <param name="state">The soil state.</param>
        /// <param name="estimate">The estimate to subtract from the soil state.</param>
        /// <returns>The result of the operator.</returns>
        public static SoilState operator -(SoilState state, Estimate estimate)
        {
            SoilState NewState = new SoilState(state.Parent);
            foreach (ZoneUptakes Z in state.Zones)
            {
                ZoneUptakes NewZ = new ZoneUptakes(Z.Zone);
                NewZ.Water = Z.Water;
                NewZ.NO3N = Z.NO3N;
                NewZ.NH4N = Z.NH4N;
                NewState.Zones.Add(NewZ);
            }

            foreach (CropUptakes C in estimate.Values)
                foreach (ZoneUptakes Z in C.Zones)
                    foreach (ZoneUptakes NewZ in NewState.Zones)
                        if (Z.Zone.Name == NewZ.Zone.Name)
                        {
                            NewZ.Water = MathUtilities.Subtract(NewZ.Water, Z.Water);
                            NewZ.NO3N = MathUtilities.Subtract(NewZ.NO3N, Z.NO3N);
                            NewZ.NH4N = MathUtilities.Subtract(NewZ.NH4N, Z.NH4N);

                            for (int i = 0; i < NewZ.Water.Length; i++)
                            {
                                if (NewZ.Water[i] < 0)
                                    NewZ.Water[i] = 0;
                                if (NewZ.NO3N[i] < 0)
                                    NewZ.NO3N[i] = 0;
                                if (NewZ.NH4N[i] < 0)
                                    NewZ.NH4N[i] = 0;
                            }
                        }
            return NewState;
        }

    }
}
