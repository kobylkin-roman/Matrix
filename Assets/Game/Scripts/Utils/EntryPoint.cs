using Game.Scripts.Services;
using Game.Scripts.Spawn.Entities;
using UnityEngine;

namespace Game.Scripts.Utils
{
    public class EntryPoint : MonoBehaviour
    {
        private readonly int _poolCount = 5500;
        private const string ENTITY_PATH = "Prefabs/Entity";
        [SerializeField] private MatrixVisualizer _matrixVisualizer;

        private void Start()
        {
            var assetProvider = new AssetProvider();
            AllServices.Container.RegisterSingle<IAssetProvider>(new AssetProvider());
            
            var prefabEntity = assetProvider.LoadEntityPrefab<DefaultEntity>(ENTITY_PATH);
            var entityPool = new CustomPool<DefaultEntity>(prefabEntity, _poolCount, transform);
            var spawnFabric = new EntitySpawnFactory(entityPool);
            _matrixVisualizer.Init(spawnFabric);
        }
    }
}