using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Spawn.Entities
{
    public class DefaultEntity : MonoBehaviour, IEntity
    { 
        [field: SerializeField]
        public Renderer cubeRenderer;
        public Vector2 GetEntitySize()
        {
            return new Vector2(2, 2);
        }
    }
}