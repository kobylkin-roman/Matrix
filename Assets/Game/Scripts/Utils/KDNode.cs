
using UnityEngine;

namespace Game.Scripts.Utils
{
    public class KDNode
    {
        public Vector3 point;
        public KDNode left;
        public KDNode right;

        public KDNode(Vector3 point) => this.point = point;
        public void SetChildren(KDNode left, KDNode right)
        {
            this.left = left;
            this.right = right;
        }
    }
}