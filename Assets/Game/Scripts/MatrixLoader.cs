using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts;
using Game.Scripts.Utils;
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
        string modelPath = Path.Combine(Application.streamingAssetsPath, "model.json");
        string spacePath = Path.Combine(Application.streamingAssetsPath, "space.json");

        if (!File.Exists(modelPath) || !File.Exists(spacePath))
        {
            Debug.LogError($"Файлы JSON не найдены! Ожидался путь: {modelPath} и {spacePath}");
            return;
        }

        try
        {
            string modelJson = File.ReadAllText(modelPath);
            string spaceJson = File.ReadAllText(spacePath);

            modelData = JsonConvert.DeserializeObject<List<Matrix>>(modelJson);
            spaceData = JsonConvert.DeserializeObject<List<Matrix>>(spaceJson);
        }
        catch (JsonException e)
        {
            Debug.LogError($"Ошибка при парсинге JSON: {e.Message}");
        }
    }
    
    public void SaveMatchesToJson(List<MatrixMatch> matches)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "matrix_matches.json");

        var serializableMatches = matches.Select(m => new
        {
            ModelPosition = new SerializableVector3(m.ModelPosition),
            SpacePosition = new SerializableVector3(m.SpacePosition),
            Offset = new SerializableVector3(m.Offset)
        }).ToList();

        try
        {
            string json = JsonConvert.SerializeObject(serializableMatches, Formatting.Indented);
            File.WriteAllText(path, json);
            Debug.Log($"Смещения сохранены в {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при сохранении JSON: {e.Message}");
        }
    }
}

