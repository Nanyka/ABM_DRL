using System.IO;
using SimpleFileBrowser;
using Unity.InferenceEngine;
using UnityEngine;

namespace Sugarscape
{
    public class LoadModelFromFile : MonoBehaviour
    {
        [SerializeField] private ModelStorage modelStorage;

        public void LoadModel()
        {
            FileBrowser.ShowLoadDialog(
                onSuccess: paths =>
                {
                    var path = paths[0];
                    TryLoadSentis(path);
                },
                onCancel: () => { /* user canceled */ },
                pickMode: FileBrowser.PickMode.Files,
                allowMultiSelection: false,
                initialPath: Application.persistentDataPath,
                title: "Pick Sentis model",
                loadButtonText: "Load"
            );
            
            // var onnx = EditorUtility.OpenFilePanel("Pick ONNX", "", "onnx");
            // if (string.IsNullOrEmpty(onnx)) return;
            //
            // var rel = "Assets/TempModels/" + Path.GetFileName(onnx);
            // Directory.CreateDirectory("Assets/TempModels");
            // File.Copy(onnx, rel, true);
            // AssetDatabase.ImportAsset(rel);
            //
            // modelStorage.SetValue(AssetDatabase.LoadAssetAtPath<ModelAsset>(rel));
            
            // var onnx =StandaloneFileBrowser.OpenFilePanel(
            //     "Pick ONNX model", "", "onnx", false);
            
            // var asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(rel);
            // Debug.Log($"Model Asset type: {asset.GetType()}");

            // _runtimeModel = ModelLoader.Load(asset);
        }

        private void TryLoadSentis(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogError($"Invalid path: {path}");
                return;
            }

            try
            {
                Debug.Log($"Loading model from file: {path}");
                var readPath = File.ReadAllBytes(path);
                Debug.Log($"Loading model from file: {readPath}");
                
                // using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                
                // var model = ModelLoader.Load(fs);
                // Debug.Log($"Model type: {model.GetType()}");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                // Optional fallback:
                try
                {
                    Debug.Log($"Loading model from file: {path}");

                    // using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    // var model = ModelLoader.Load(fs);
                    Debug.Log("GPU failed — fell back to CPU backend.");
                }
                catch (System.Exception e2)
                {
                    Debug.LogException(e2);
                }
            }
        }
    }
}