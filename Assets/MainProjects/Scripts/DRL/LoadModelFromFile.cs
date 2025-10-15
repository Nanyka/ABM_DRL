using System.IO;
using System.Threading.Tasks;
using SimpleFileBrowser;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using Unity.Profiling;
#endif

namespace Sugarscape
{
    public class LoadModelFromFile : MonoBehaviour
    {
        [FormerlySerializedAs("modelStorage")] [SerializeField] private WorkerStorage workerStorage;
        public BackendType backend = BackendType.CPU;

        public void LoadModel()
        {
            FileBrowserLoad();
        }

        // private void EditorLoad()
        // {
        //     var onnx = EditorUtility.OpenFilePanel("Pick ONNX", "", "onnx");
        //     if (string.IsNullOrEmpty(onnx)) return;
        //     
        //     var rel = "Assets/TempModels/" + Path.GetFileName(onnx);
        //     Directory.CreateDirectory("Assets/TempModels");
        //     File.Copy(onnx, rel, true);
        //     AssetDatabase.ImportAsset(rel);
        //     
        //     // modelStorage.SetValue(AssetDatabase.LoadAssetAtPath<ModelAsset>(rel));
        // }

        private void FileBrowserLoad()
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
                var model = ModelLoader.Load(path);
                
                // Debug.Log($"Loaded model input: {model.inputs[0].name} - {model.inputs[0].shape}, {model.inputs[1].name},{model.inputs[2].name}");
                
                workerStorage.SetValue(new Worker(model, backend));
                // Debug.Log($"Loaded worker: {workerStorage.GetValue().GetType()}");
                // modelStorage.SetValue(model);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                // Optional fallback:
                try
                {
                    Debug.Log($"Loading model from file: {path}");
                    Debug.Log("GPU failed — fell back to CPU backend.");
                }
                catch (System.Exception e2)
                {
                    Debug.LogException(e2);
                }
            }
        }

        private void OnDestroy() => workerStorage.GetValue()?.Dispose();
    }
}