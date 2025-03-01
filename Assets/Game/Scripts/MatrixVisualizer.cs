using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Scripts;
using Game.Scripts.Spawn.Entities;
using Game.Scripts.Utils;
using UnityEngine;

public class MatrixVisualizer : MonoBehaviour
{
    [SerializeField] private MatrixLoader _matrixLoader;
    [SerializeField] private int _maxOffset = 10;
    [SerializeField] private float _tolerance = 0.2f;

    private readonly List<DefaultEntity> _spawnedCubes = new();
    private readonly List<MatrixMatch> _matches = new();
    private Dictionary<Vector3, DefaultEntity> _positionToEntityMap = new();
    private HashSet<Vector3> _matchedPositions = new();
    private KDTree _spaceKdTree;
    private List<Vector3> _spacePositions;
    private EntitySpawnFactory _spawnFactory;
    private MaterialPropertyBlock _propertyBlock;

    public void Init(EntitySpawnFactory spawnFabric)
    {
        _spawnFactory = spawnFabric;
        _matrixLoader.LoadMatrices();
        BuildKdTree();
        VisualizeMatrices();
        CompareMatricesWithOffsetsUsingKdTree();
        VisualizeMatches();
        CheckUnmatchedMatrices();
    }

    private void VisualizeMatrices()
    {
        if (_matrixLoader.modelData == null || _matrixLoader.spaceData == null)
        {
            Debug.LogError("Матрицы не загружены! Проверьте JSON-файлы.");
            return;
        }

        ClearPreviousCubes();
        Visualize(_matrixLoader.modelData, Color.blue);
        Visualize(_matrixLoader.spaceData, Color.red);
        Debug.Log($"Визуализировано {_matrixLoader.modelData.Count} моделей и {_matrixLoader.spaceData.Count} пространственных матриц.");
    }

    private void Visualize(List<Matrix> matrices, Color color)
    {
        foreach (var matrix in matrices)
        {
            Vector3 position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            DefaultEntity cube = _spawnFactory.SpawnDefaultEntity(position);
            ChangeColor(cube, color);
            _spawnedCubes.Add(cube);
            _positionToEntityMap[position] = cube;
        }
    }

    private void VisualizeMatches()
    {
        foreach (var match in _matches)
        {
            if (_positionToEntityMap.TryGetValue(match.SpacePosition, out var cube))
            {
                ChangeColor(cube, Color.green);
            }
        }
    }

    private void ChangeColor(DefaultEntity cube, Color color)
    {
        if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
        var renderer = cube.GetComponent<Renderer>();
        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(_propertyBlock);
    }

    private void BuildKdTree()
    {
        _spacePositions = _matrixLoader.spaceData.Select(m => new Vector3(m.m03, m.m13, m.m23)).ToList();
        _spaceKdTree = new KDTree(_spacePositions);
        Debug.Log($"K-D дерево построено для {_spacePositions.Count} пространственных матриц.");
    }

    private void CompareMatricesWithOffsetsUsingKdTree()
    {
        if (_matrixLoader.modelData == null || _matrixLoader.spaceData == null)
        {
            Debug.LogError("Матрицы не загружены! Проверьте JSON-файлы.");
            return;
        }

        Parallel.ForEach(_matrixLoader.modelData, modelMatrix =>
        {
            Vector3 originalPosition = new Vector3(modelMatrix.m03, modelMatrix.m13, modelMatrix.m23);
            for (int dx = -_maxOffset; dx <= _maxOffset; dx++)
            for (int dy = -_maxOffset; dy <= _maxOffset; dy++)
            for (int dz = -_maxOffset; dz <= _maxOffset; dz++)
            {
                Vector3 shiftedPosition = ApplyOffset(originalPosition, dx, dy, dz);
                var nearestNeighbor = _spaceKdTree.GetNearestNeighbor(shiftedPosition);
                if (nearestNeighbor != Vector3.zero && MatricesAreEqual(shiftedPosition, nearestNeighbor))
                {
                    lock (_matches)
                    {
                        _matches.Add(new MatrixMatch
                        {
                            ModelPosition = originalPosition,
                            SpacePosition = nearestNeighbor,
                            Offset = new Vector3(dx, dy, dz)
                        });
                        _matchedPositions.Add(originalPosition);
                    }
                }
            }
        });

        Debug.Log($"Найдено {_matches.Count} совпадений с учетом смещений.");
    }

    private Vector3 ApplyOffset(Vector3 position, int dx, int dy, int dz)
    {
        return new Vector3(position.x + dx, position.y + dy, position.z + dz);
    }

    private bool MatricesAreEqual(Vector3 pos1, Vector3 pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) < _tolerance &&
               Mathf.Abs(pos1.y - pos2.y) < _tolerance &&
               Mathf.Abs(pos1.z - pos2.z) < _tolerance;
    }

    private void CheckUnmatchedMatrices()
    {
        int unmatchedCount = _matrixLoader.modelData.Count(model => !_matchedPositions.Contains(new Vector3(model.m03, model.m13, model.m23)));
        Debug.Log($"Матриц модели без совпадений: {unmatchedCount}");
    }

    private void ClearPreviousCubes()
    {
        foreach (var cube in _spawnedCubes)
        {
            Destroy(cube);
        }
        _spawnedCubes.Clear();
        _positionToEntityMap.Clear();
    }
}
