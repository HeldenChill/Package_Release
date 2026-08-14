namespace Hung.Base.Persistence
{
    public interface ICompatibilitySaveDefinitionFactory
    {
        SaveDefinition<T> Create<T>(string key) where T : new();
    }

    public interface IPersistenceService
    {
        SaveResult Save<T>(SaveDefinition<T> definition, T value) where T : new();
        LoadResult<T> Load<T>(SaveDefinition<T> definition) where T : new();
        void Register<T>(SaveDefinition<T> definition) where T : new();
        bool TryGetDefinition<T>(string requestedKey, out SaveDefinition<T> definition) where T : new();
    }
}
