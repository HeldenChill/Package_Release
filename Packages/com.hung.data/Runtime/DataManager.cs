using UnityEngine;

namespace Hung.Data
{
    using Hung.Base;
    using Hung.DesignPattern;
    using System;

    [DefaultExecutionOrder(-100)]
    public partial class DataManager : Singleton<DataManager>, IDataService
    {
        [SerializeField]
        private PoolData poolData;
        [SerializeField]
        private GameConfig gameConfig;
        private GameData _gameData;

        /// <summary>
        /// A game registers its own ScriptableObjects by declaring another <c>partial class DataManager</c>
        /// from its <c>Hung.Data.asmref</c> folder: add the <c>[SerializeField]</c> fields there and
        /// implement these hooks. All three are optional - a game that adds no data of its own
        /// implements none of them and the calls compile away.
        /// </summary>
        partial void TryGetGameSOData<T>(ref T result) where T : ScriptableObject;
        partial void TryGetGameData<T>(int index, ref T result) where T : class;
        partial void OnFirstInitData();

        /// <summary>
        /// The same seam, reserved for optional framework service packages that ship their own SO
        /// (com.hung.services.iap's <c>IAPData</c>). Separate from <see cref="TryGetGameSOData{T}"/>
        /// because a partial method takes exactly one implementation, and the game already owns that one.
        /// A project without those packages installed implements nothing and the call compiles away.
        /// </summary>
        partial void TryGetServiceSOData<T>(ref T result) where T : ScriptableObject;

        public GameData GameData
        {
            get
            {
                if (_gameData == null)
                {
                    Load();
                    ItemCatalog itemCatalog = GetSOData<ItemCatalog>();
                    bool isFirstInit = _gameData.InitData(itemCatalog != null ? itemCatalog.Ids : Array.Empty<ItemId>());
                    _gameData.user.playGameAdsCount = 0;
                    if (isFirstInit)
                    {
                        OnFirstInitData();
                        Debug.Log($"<color=#fffd74> {"[System]: First Init Data Value"}</color>");
                    }
                }
                return _gameData;
            }
        }

        private void Awake()
        {
            Locator.Data = this;
            DontDestroyOnLoad(this);
        }

        private GameData Load()
        {
            _gameData = Database.Load<GameData>(GameData.SaveKey);
            return _gameData;
        }

        public void Save()
        {
            Database.Save(_gameData, GameData.SaveKey);
        }

        public T GetSOData<T>() where T : ScriptableObject
        {
            switch (typeof(T))
            {
                case Type type when type == typeof(GameConfig):
                    return gameConfig as T;
                case Type type when type == typeof(PoolData):
                    return poolData as T;
            }
            T result = null;
            TryGetServiceSOData(ref result);
            if (result != null) return result;
            TryGetGameSOData(ref result);
            return result;
        }

        public T GetUnit<T>(int type) where T : class
        {
            PoolType t = (PoolType)type;
            return poolData.Units[t] as T;
        }

        public T GetData<T>(int index = 0) where T : class
        {
            switch (typeof(T))
            {
                case Type type when type == typeof(GameData):
                    return GameData as T;
                case Type type when type == typeof(PoolData):
                    return poolData as T;
            }
            T result = null;
            TryGetGameData(index, ref result);
            return result;
        }

        /// <summary>Loads <c>Resources/LevelData/Lvl_&lt;level&gt;</c> as raw JSON. Games use this from
        /// <see cref="TryGetGameData{T}"/> to deserialize their own level model.</summary>
        protected internal string LoadFromJson(int level)
        {
            TextAsset tmp = (TextAsset)Resources.Load("LevelData/" + "Lvl_" + level);
            if (tmp != null) return tmp.ToString();
            return null;
        }
    }
}
