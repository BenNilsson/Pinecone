using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Pinecone
{
    [CustomEditor(typeof(NetworkManager), true)]
    public class NetworkManagerCustomDraw : Editor
    {
        private static NetworkManager manager;

        public override void OnInspectorGUI()
        {
            manager ??= FindObjectOfType<NetworkManager>();

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                GenerateNetworkObjectIDs();
            }
        }

        private void GenerateNetworkObjectIDs()
        {
            if (manager.spawnableObjects == null)
                return;

            foreach (var go in manager.spawnableObjects)
            {
                if (go == null || !go.TryGetComponent<NetworkObject>(out var networkObject))
                    continue;

                if (networkObject.NetworkObjectID == Guid.Empty.ToString() && PrefabUtility.IsPartOfPrefabAsset(networkObject.gameObject))
                {
                    networkObject.NetworkObjectID = Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(go);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(networkObject);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
