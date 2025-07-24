namespace APSIM.Core;

public interface IDataProvider
{
    object Data { get; set; }
    Type Type { get; }
}