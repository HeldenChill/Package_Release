namespace Hung.UI
{
    public interface IUIPrefabProvider
    {
        T GetPrefab<T>() where T : UICanvas;
    }
}
