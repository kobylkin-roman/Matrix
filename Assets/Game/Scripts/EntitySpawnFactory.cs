using Game.Scripts.Spawn.Entities;
using UnityEngine;

namespace Game.Scripts
{
    public class EntitySpawnFactory : IEntitySpawnFactory
    {
        private readonly CustomPool<DefaultEntity> _defaultEntityPool;

        private Vector2 _spawnPosition;

        public EntitySpawnFactory(CustomPool<DefaultEntity> defaultEntityPool)
        {
            _defaultEntityPool = defaultEntityPool;
        }

        public DefaultEntity SpawnDefaultEntity()
        {
            DefaultEntity defaultEntity = _defaultEntityPool.Get();
            SpawnInChessOrder(defaultEntity);
            defaultEntity.transform.position = _spawnPosition;
            return defaultEntity;
        }
        
        public DefaultEntity SpawnDefaultEntity(Vector3 position)
        {
            DefaultEntity defaultEntity = _defaultEntityPool.Get();
            defaultEntity.transform.position = position;
            return defaultEntity;
        }

        private int _counterForChessOrder = 0;
        private void SpawnInChessOrder(DefaultEntity defaultEntity)
        {
            if (_counterForChessOrder == 0)
            {
                _counterForChessOrder++;
                _spawnPosition += defaultEntity.GetEntitySize();
            }
            else
            {
                _counterForChessOrder--;
                _spawnPosition = new Vector2(_spawnPosition.x + defaultEntity.GetEntitySize().x,
                    _spawnPosition.y - defaultEntity.GetEntitySize().y);
            }
        }
        
        public void SpawnDefaultEntities(int entitiesCount)
        {
            for (int i = 0; i < entitiesCount; i++)
            {
                SpawnDefaultEntity();
            }
        }
    }
}