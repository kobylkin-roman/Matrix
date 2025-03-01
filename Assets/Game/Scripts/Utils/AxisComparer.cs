using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Utils
{
    public class AxisComparer : IComparer<Vector3>
    {
        private readonly int _axis;
        public AxisComparer(int axis) => this._axis = axis;

        public int Compare(Vector3 a, Vector3 b)
        {
            return a[_axis].CompareTo(b[_axis]);
        }
    }
}