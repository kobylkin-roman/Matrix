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
    public float _scale = 1.0f;
    public int _maxOffset = 10;

    private readonly List<DefaultEntity> _spawnedCubes = new();
    private readonly List<MatrixMatch> _matches = new();
    private KDTree _spaceKdTree;

    private EntitySpawnFactory _spawnFactory;

    public void Init(EntitySpawnFactory spawnFabric)
    {
        _spawnFactory = spawnFabric;

        _matrixLoader.LoadMatrices();
        VisualizeMatrices();
        BuildKdTree();
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
            Vector3 position = new Vector3(matrix.m03, matrix.m13, matrix.m23) * _scale;
            DefaultEntity cube = _spawnFactory.SpawnDefaultEntity(position);
            // GameObject cube = Instantiate(_cubePrefab, position, Quaternion.identity);
            cube.GetComponent<Renderer>().material.color = color;
            _spawnedCubes.Add(cube);
        }
    }
    
    private void VisualizeMatches()
    {
        foreach (var match in _matches)
        {
            // Ищем уже существующий куб с такой же позицией
            DefaultEntity matchingCube = _spawnedCubes.Find(cube => cube.transform.position == match.SpacePosition * _scale);
        
            if (matchingCube != null)
            {
                matchingCube.GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }
    
    private void BuildKdTree()
    {
        List<Vector3> spacePositions = new List<Vector3>();
        foreach (var matrix in _matrixLoader.spaceData)
        {
            Vector3 position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            spacePositions.Add(position);
        }

        _spaceKdTree = new KDTree(spacePositions);

        Debug.Log($"K-D дерево построено для {spacePositions.Count} пространственных матриц.");
    }

    private void CompareMatricesWithOffsetsUsingKdTree()
    {
        if (_matrixLoader.modelData == null || _matrixLoader.spaceData == null)
        {
            Debug.LogError("Матрицы не загружены! Проверьте JSON-файлы.");
            return;
        }

        int matchCount = 0;

        Parallel.ForEach(_matrixLoader.modelData, modelMatrix =>
        {
            for (int dx = -_maxOffset; dx <= _maxOffset; dx++)
            {
                for (int dy = -_maxOffset; dy <= _maxOffset; dy++)
                {
                    for (int dz = -_maxOffset; dz <= _maxOffset; dz++)
                    {
                        Matrix shiftedMatrix = ApplyOffset(modelMatrix, dx, dy, dz);
                        Vector3 shiftedPosition = new Vector3(shiftedMatrix.m03, shiftedMatrix.m13, shiftedMatrix.m23);

                        var nearestNeighbor = _spaceKdTree.GetNearestNeighbor(shiftedPosition);

                        if (nearestNeighbor != Vector3.zero && MatricesAreEqual(shiftedMatrix, nearestNeighbor))
                        {
                            lock (_matches)
                            {
                                _matches.Add(new MatrixMatch
                                {
                                    ModelPosition = new Vector3(modelMatrix.m03, modelMatrix.m13, modelMatrix.m23),
                                    SpacePosition = nearestNeighbor,
                                    Offset = new Vector3(dx, dy, dz)
                                });
                            }

                            matchCount++;
                        }
                    }
                }
            }
        });

        Debug.Log($"Найдено совпадений с учетом смещений: {matchCount}");
    }

    private Matrix ApplyOffset(Matrix matrix, int dx, int dy, int dz)
    {
        return new Matrix
        {
            m00 = matrix.m00, m01 = matrix.m01, m02 = matrix.m02, m03 = matrix.m03 + dx,
            m10 = matrix.m10, m11 = matrix.m11, m12 = matrix.m12, m13 = matrix.m13 + dy,
            m20 = matrix.m20, m21 = matrix.m21, m22 = matrix.m22, m23 = matrix.m23 + dz,
            m30 = matrix.m30, m31 = matrix.m31, m32 = matrix.m32, m33 = matrix.m33
        };
    }

    private bool MatricesAreEqual(Matrix matrix1, Vector3 matrix2Position)
    {
        float tolerance = 0.2f;
        return Mathf.Abs(matrix1.m03 - matrix2Position.x) < tolerance &&
               Mathf.Abs(matrix1.m13 - matrix2Position.y) < tolerance &&
               Mathf.Abs(matrix1.m23 - matrix2Position.z) < tolerance;
    }
    
    private void CheckUnmatchedMatrices()
    {
        int unmatchedCount = 0;
        foreach (var model in _matrixLoader.modelData)
        {
            bool found = _matches.Any(m => m.ModelPosition == new Vector3(model.m03, model.m13, model.m23));
            if (!found) unmatchedCount++;
        }
        Debug.Log($"Матриц модели без совпадений: {unmatchedCount}");
    }
    
    private void ClearPreviousCubes()
    {
        foreach (var cube in _spawnedCubes)
        {
            Destroy(cube);
        }
        _spawnedCubes.Clear();
    }
}

public class MatrixMatch
{
    public Vector3 ModelPosition;
    public Vector3 SpacePosition;
    public Vector3 Offset;
}