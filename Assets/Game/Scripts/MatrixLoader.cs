using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class Matrix
{
    public float m00, m01, m02, m03;
    public float m10, m11, m12, m13;
    public float m20, m21, m22, m23;
    public float m30, m31, m32, m33;
}

public class MatrixLoader : MonoBehaviour
{
    public List<Matrix> modelData { get; private set; }
    public List<Matrix> spaceData { get; private set; }

    public void LoadMatrices()
    {
        TextAsset modelJsonAsset = Resources.Load<TextAsset>("Configs/JSON/model");
        TextAsset spaceJsonAsset = Resources.Load<TextAsset>("Configs/JSON/space");

        if (modelJsonAsset == null || spaceJsonAsset == null)
        {
            Debug.LogError("Ошибка загрузки JSON! Проверьте путь: Resources/Configs/JSON/");
            return;
        }

        try
        {
            modelData = JsonConvert.DeserializeObject<List<Matrix>>(modelJsonAsset.text);
            spaceData = JsonConvert.DeserializeObject<List<Matrix>>(spaceJsonAsset.text);
        }
        catch (JsonException e)
        {
            Debug.LogError($"Ошибка при парсинге JSON: {e.Message}");
            return;
        }

        if (modelData == null || spaceData == null)
        {
            Debug.LogError("JSON файлы пустые или некорректные!");
        }
        else
        {
            Debug.Log($"Загружено {modelData.Count} матриц модели и {spaceData.Count} матриц пространства.");
        }
    }
}

