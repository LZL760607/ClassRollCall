namespace ClassRollCall.Services;

public interface IConfigurationService
{
    T? GetConfiguration<T>(string key);
    void SetConfiguration<T>(string key, T value);
    void Save();
}