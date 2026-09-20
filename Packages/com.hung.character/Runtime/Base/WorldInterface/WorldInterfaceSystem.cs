namespace Gameplay.Character.WorldInterface
{
    using Gameplay.Character;

    /// <summary>
    /// Responsible for updating knowledgement about the game world for DynamicObject
    /// </summary>
    public class WorldInterfaceSystem : System<WorldInterfaceModule, WorldInterfaceData, WorldInterfaceParameter>
    {
        #region Essential Functions
        protected WorldInterfaceSystem() { }
        public WorldInterfaceSystem(WorldInterfaceModule module, PerceptionData characterData)
        {
            this.module = module;
            data = new WorldInterfaceData();
            Parameter = new WorldInterfaceParameter();
            data.PerceptionData = characterData;
            Parameter.PerceptionData = characterData;
            module.Initialize(data, Parameter);
        }
        #endregion
    }
}