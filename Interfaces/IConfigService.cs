namespace CooperativaApp.Interfaces
{
    public interface IConfigService
    {
        Task<string> GetValue(string key);
        Task<T> GetValueAs<T>(string key);
        Task RefreshCache();
    }
}
