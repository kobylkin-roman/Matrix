using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Utils
{
    public class KDTree
    {
        private readonly KDNode _root;
        private readonly Vector3[] _pointsArray; 

        public KDTree(List<Vector3> points)
        {
            _pointsArray = points.ToArray();
            _root = BuildTree(0, _pointsArray.Length - 1, 0);
        }

        private KDNode BuildTree(int start, int end, int depth)
        {
            if (start > end) return null;

            int axis = depth % 3;
            int median = (start + end) / 2;

            QuickSelect(_pointsArray, start, end, median, axis); // Быстрая выборка медианы

            var node = new KDNode(_pointsArray[median]);
            
            // Используем Task.Run только при большом количестве точек
            if (end - start > 5000)
            {
                KDNode left = null, right = null;
                Parallel.Invoke(
                    () => left = BuildTree(start, median - 1, depth + 1),
                    () => right = BuildTree(median + 1, end, depth + 1)
                );
                node.SetChildren(left, right);
            }
            else
            {
                node.SetChildren(
                    BuildTree(start, median - 1, depth + 1),
                    BuildTree(median + 1, end, depth + 1)
                );
            }

            return node;
        }

        public Vector3 GetNearestNeighbor(Vector3 target)
        {
            return FindNearest(_root, target, 0, _root.point);
        }

        private Vector3 FindNearest(KDNode node, Vector3 target, int depth, Vector3 best)
        {
            if (node == null) return best;

            float bestDist = (best - target).sqrMagnitude;
            float nodeDist = (node.point - target).sqrMagnitude;

            if (nodeDist < bestDist)
            {
                best = node.point;
                bestDist = nodeDist;
            }

            int axis = depth % 3;
            float delta = target[axis] - node.point[axis];

            KDNode first = delta < 0 ? node.left : node.right;
            KDNode second = delta < 0 ? node.right : node.left;

            best = FindNearest(first, target, depth + 1, best);

            if (delta * delta < bestDist)
            {
                best = FindNearest(second, target, depth + 1, best);
            }

            return best;
        }

        // Быстрая выборка k-го элемента (аналог QuickSort, но сортируем только нужную часть)
        private void QuickSelect(Vector3[] points, int left, int right, int k, int axis)
        {
            while (left < right)
            {
                int pivotIndex = Partition(points, left, right, axis);
                if (pivotIndex == k) return;
                if (pivotIndex > k) right = pivotIndex - 1;
                else left = pivotIndex + 1;
            }
        }

        private int Partition(Vector3[] points, int left, int right, int axis)
        {
            Vector3 pivot = points[right];
            int i = left;
            for (int j = left; j < right; j++)
            {
                if (Compare(points[j], pivot, axis) < 0)
                {
                    (points[i], points[j]) = (points[j], points[i]);
                    i++;
                }
            }
            (points[i], points[right]) = (points[right], points[i]);
            return i;
        }

        private int Compare(Vector3 a, Vector3 b, int axis) => axis switch
        {
            0 => a.x.CompareTo(b.x),
            1 => a.y.CompareTo(b.y),
            2 => a.z.CompareTo(b.z),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}