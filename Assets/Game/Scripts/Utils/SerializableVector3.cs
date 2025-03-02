using System;
using UnityEngine;

namespace Game.Scripts.Utils
{
    [Serializable]
    public class SerializableVector3
    {
        public float x { get; private set; }
        public float y { get; private set; }
        public float z { get; private set; }

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

}