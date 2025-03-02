using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts;
using Game.Scripts.Spawn.Entities;
using Game.Scripts.Utils;
using UnityEngine;

public class MatrixVisualizer : MonoBehaviour
{
    [SerializeField] private MatrixLoader _matrixLoader;
    [SerializeField] private int _maxOffset = 10;
    [SerializeField] private float _tolerance = 0.2f;
    [SerializeField] private int _neighborsCount = 10;

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
        StartCoroutine(CompareMatricesWithOffsetsCoroutine());
        VisualizeMatches();
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
        var renderer = cube.cubeRenderer;
        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(_propertyBlock);
    }

    private void BuildKdTree()
    {
        if (_matrixLoader.spaceData == null || _matrixLoader.spaceData.Count == 0)
        {
            Debug.LogError("Ошибка: spaceData не загружены, построение KD-дерева невозможно.");
            return;
        }

        _spacePositions = _matrixLoader.spaceData.Select(m => new Vector3(m.m03, m.m13, m.m23)).ToList();
        _spaceKdTree = new KDTree(_spacePositions);
        Debug.Log($"K-D дерево построено для {_spacePositions.Count} пространственных матриц.");
    }

    private IEnumerator CompareMatricesWithOffsetsCoroutine()
    {
        float startTime = Time.realtimeSinceStartup;
        int total = _matrixLoader.modelData.Count;
        int batchSize = 100; // Количество элементов за один проход
        int processed = 0;

        for (int i = 0; i < total; i += batchSize)
        {
            int batchEnd = Mathf.Min(i + batchSize, total);

            for (int j = i; j < batchEnd; j++)
            {
                var modelMatrix = _matrixLoader.modelData[j];
                Vector3 originalPosition = new Vector3(modelMatrix.m03, modelMatrix.m13, modelMatrix.m23);
                var nearestNeighbors = _spaceKdTree.GetNearestNeighbors(originalPosition, _neighborsCount);

                foreach (var neighbor in nearestNeighbors)
                {
                    Vector3 offset = neighbor - originalPosition;

                    if (offset.sqrMagnitude <= _maxOffset * _maxOffset && MatricesAreEqual(originalPosition + offset, neighbor))
                    {
                        _matches.Add(new MatrixMatch
                        {
                            ModelPosition = originalPosition,
                            SpacePosition = neighbor,
                            Offset = offset
                        });
                        _matchedPositions.Add(originalPosition);

                        if (_positionToEntityMap.TryGetValue(neighbor, out var cube))
                        {
                            ChangeColor(cube, Color.green);
                        }
                    }
                }
            }

            processed += batchSize;
            Debug.Log($"Обработано {processed}/{total} матриц...");
        
            yield return null; // Разрешаем кадру обновиться
        }

        Debug.Log($"Поиск завершён за {Time.realtimeSinceStartup - startTime:F2} сек. Найдено {_matches.Count} совпадений.");
        CheckUnmatchedMatrices();
        _matrixLoader.SaveMatchesToJson(_matches);
    }

    private bool MatricesAreEqual(Vector3 pos1, Vector3 pos2)
    {
        return (pos1 - pos2).sqrMagnitude < (_tolerance * _tolerance);
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
