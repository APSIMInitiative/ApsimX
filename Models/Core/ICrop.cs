namespace Models.Core
{
    public interface ICrop
    {
        NewCanopyType CanopyData {get;}

        string CropType { get; }
        double FRGR { get; }

    }
}