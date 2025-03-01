using Game.Scripts.Services;
using Game.Scripts.Spawn.Entities;
using UnityEngine;

namespace Game.Scripts
{
    public interface IEntitySpawnFactory : IService
    {
        DefaultEntity SpawnDefaultEntity(Vector3 position);        
        DefaultEntity SpawnDefaultEntity();


        void SpawnDefaultEntities(int entitiesCount);

    }
}