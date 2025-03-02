using System;
using System.Collections.Generic;
using System.Linq;
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

        public List<Vector3> GetNearestNeighbors(Vector3 originalPosition, int k)
        {
            var nearestNeighbors = new List<Vector3>();
            var maxHeap = new SortedList<float, Vector3>(new DuplicateKeyComparer<float>());

            FindKNearest(_root, originalPosition, 0, k, maxHeap);

            foreach (var entry in maxHeap.Values)
            {
                nearestNeighbors.Add(entry);
            }

            return nearestNeighbors;
        }

        private void FindKNearest(KDNode node, Vector3 target, int depth, int k, SortedList<float, Vector3> maxHeap)
        {
            if (node == null) return;

            float nodeDist = (node.point - target).sqrMagnitude;
            
            // Если не достигли предела k, просто добавляем
            if (maxHeap.Count < k)
            {
                maxHeap.Add(nodeDist, node.point);
            }
            // Если нашли точку ближе, чем самая дальняя в списке, заменяем её
            else if (nodeDist < maxHeap.Keys.Last())
            {
                maxHeap.RemoveAt(maxHeap.Count - 1);
                maxHeap.Add(nodeDist, node.point);
            }

            int axis = depth % 3;
            float delta = target[axis] - node.point[axis];

            KDNode first = delta < 0 ? node.left : node.right;
            KDNode second = delta < 0 ? node.right : node.left;

            FindKNearest(first, target, depth + 1, k, maxHeap);

            if (delta * delta < maxHeap.Keys.Last() || maxHeap.Count < k)
            {
                FindKNearest(second, target, depth + 1, k, maxHeap);
            }
        }

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

    public class DuplicateKeyComparer<T> : IComparer<T> where T : IComparable
    {
        public int Compare(T x, T y)
        {
            int result = x.CompareTo(y);
            return result == 0 ? 1 : result; // Чтобы не было ошибки с дублирующимися ключами
        }
    }
}
