using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Game.Scripts
{
    public class ExportResults : MonoBehaviour
    {
        public void ExportMatrixOffsets(List<Vector3> offsets)
        {
            string json = JsonConvert.SerializeObject(offsets, Formatting.Indented);
            File.WriteAllText("Assets/Game/Resources/Configs/JSON/matrix_offsets.json", json);
        }
    }
}