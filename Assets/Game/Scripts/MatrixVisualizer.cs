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
        _propertyBlock ??= new MaterialPropertyBlock();
        var cubeRenderer = cube.cubeRenderer;
        cubeRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", color);
        cubeRenderer.SetPropertyBlock(_propertyBlock);
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
        int totalModels = _matrixLoader.modelData.Count;
        int processed = 0;

        foreach (var modelMatrix in _matrixLoader.modelData)
        {
            List<Vector3> modelPositions = ExtractPositionsFromMatrix(modelMatrix);

            if (modelPositions.Count == 0)
                continue;

            Vector3 firstModelPos = modelPositions[0];
            var candidateOffsets = _spaceKdTree.GetNearestNeighbors(firstModelPos, _neighborsCount)
                .Select(spacePos => spacePos - firstModelPos)
                .Where(offset => offset.sqrMagnitude <= _maxOffset * _maxOffset)
                .ToList();

            foreach (var offset in candidateOffsets)
            {
                if (modelPositions.All(pos => _spacePositions.Contains(pos + offset)))
                {
                    _matches.Add(new MatrixMatch
                    {
                        ModelPosition = firstModelPos,
                        SpacePosition = firstModelPos + offset,
                        Offset = offset
                    });

                    foreach (var pos in modelPositions)
                    {
                        _matchedPositions.Add(pos);
                    }

                    break;
                }
            }

            processed++;
            if (processed % 10 == 0)
            {
                Debug.Log($"Обработано {processed}/{totalModels} матриц...");
                yield return null;
            }
        }

        Debug.Log($"Поиск завершён за {Time.realtimeSinceStartup - startTime:F2} сек. Найдено {_matches.Count} совпадений.");
        _matrixLoader.SaveMatchesToJson(_matches);
    }

    private List<Vector3> ExtractPositionsFromMatrix(Matrix matrix)
    {
        return new List<Vector3>
        {
            new(matrix.m03, matrix.m13, matrix.m23)
        };
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
