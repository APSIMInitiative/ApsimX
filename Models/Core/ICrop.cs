namespace Models.Core
{
    public interface ICrop
    {
        NewCanopyType CanopyData {get;}

        // Required for MicroClimate 
        string CropType { get; }
        double FRGR { get; }

        double PotentialEP { get; set; }
        CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        }
}