using UnityEditor;
using UnityEngine;
using System.IO;

public class PrefabPrepUtility : EditorWindow
{
    [MenuItem("Tools/Prep Medical Equipment Prefabs")]
    public static void PrepPrefabs()
    {
        string rootFolder = "Assets/Assets/Medical equipment/Equipment";

        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            Debug.LogError($"Folder not found: {rootFolder}");
            return;
        }

        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { rootFolder });
        Debug.Log($"Found {modelGuids.Length} models. Generating prefabs...");

        int created = 0;
        int skipped = 0;

        foreach (string guid in modelGuids)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) continue;

            // Save the prefab next to the FBX, with the same name + .prefab extension.
            string prefabPath = Path.ChangeExtension(modelPath, ".prefab");

            // Skip if a prefab already exists (so re-running is safe).
            if (File.Exists(prefabPath))
            {
                skipped++;
                continue;
            }

            // Instantiate the model in the editor (not in a scene), configure it, save it as a prefab.
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);

            instance.tag = "Selectable";

            if (instance.GetComponent<Rigidbody>() == null)
            {
                var rb = instance.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            if (instance.GetComponentInChildren<Collider>() == null)
            {
                var renderer = instance.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var box = instance.AddComponent<BoxCollider>();
                    // Convert world-space bounds to local space relative to the instance root.
                    Vector3 localCenter = instance.transform.InverseTransformPoint(renderer.bounds.center);
                    box.center = localCenter;
                    box.size = renderer.bounds.size;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created {created} new prefabs. Skipped {skipped} (already existed).");
    }
}