using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utils.CollidersExporter
{
#if UNITY_EDITOR

    /// <summary>
    /// 导出服务器使用的场景碰撞数据：
    /// 只导出 Layer == "ServerCollision" 的 BoxCollider，
    /// 并写成自定义二进制格式：
    ///
    /// uint32 magic   = 'SCOL' 0x4C4F4353
    /// uint32 version = 1
    /// float  worldScale (目前写 1.0f)
    /// uint32 boxCount
    /// repeated:
    ///   float3 center (world)
    ///   float4 rotation (world, quaternion x,y,z,w)
    ///   float3 halfExtents (world)
    ///   uint32 flags (bit0 = Walkable)
    /// </summary>
    public static class ServerCollisionExporter
    {
        private const string LayerName = "ServerCollision";
        private const uint MAGIC = 0x4C4F4353u; // 'SCOL'
        private const uint VERSION = 1u;

        private const uint FLAG_WALKABLE = 1u << 0;

        [MenuItem("Tools/Export Server Collision...")]
        private static void ExportServerCollision()
        {
            // 1. 选择输出路径（在工程内）
            string defaultName =
                $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_server_collision.scol";
            string path = EditorUtility.SaveFilePanelInProject(
                "Export Server Collision",
                defaultName,
                "scol",
                "选择导出服务器碰撞数据的保存位置");

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("导出取消。");
                return;
            }

            // 2. 收集 BoxCollider
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                Debug.LogError($"未找到名为 \"{LayerName}\" 的 Layer，请先在 Project Settings -> Tags and Layers 中创建。");
                return;
            }

            BoxCollider[] allBoxes = UnityEngine.Object.FindObjectsOfType<BoxCollider>();
            var entries = new List<Entry>();

            foreach (var box in allBoxes)
            {
                if (!box.enabled) continue;
                if (!box.gameObject.activeInHierarchy) continue;
                if (box.gameObject.layer != layer) continue;

                var tf = box.transform;

                // 世界中心
                Vector3 worldCenter = tf.TransformPoint(box.center);

                // 世界旋转（BoxCollider 自身无额外旋转）
                Quaternion worldRotation = tf.rotation;

                // 世界半尺寸：size * |lossyScale| * 0.5
                Vector3 scale = tf.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

                Vector3 scaledSize = Vector3.Scale(box.size, scale);
                Vector3 halfExtents = scaledSize * 0.5f;

                // flags：目前只用 bit0 = Walkable
                uint flags = 0;

                var marker = box.GetComponent<ServerCollisionMarker>();
                ServerSurfaceType surfaceType = marker != null ? marker.surfaceType : ServerSurfaceType.Walkable;

                if (surfaceType == ServerSurfaceType.Walkable)
                {
                    flags |= FLAG_WALKABLE;
                }
                // NonWalkable 时，walkable 位不置位即可，将来可以继续扩展其它 bit

                entries.Add(new Entry
                {
                    center = worldCenter,
                    rotation = worldRotation,
                    halfExtents = halfExtents,
                    flags = flags,
                    sourceObject = box.gameObject,
                });
            }

            // 3. 写入文件
            try
            {
                WriteToFile(path, entries);
                AssetDatabase.Refresh();

                Debug.Log($"[ServerCollisionExporter] 导出完成，共 {entries.Count} 个 BoxCollider。\n路径: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerCollisionExporter] 导出失败: {ex}");
            }
        }

        private struct Entry
        {
            public Vector3 center;
            public Quaternion rotation;
            public Vector3 halfExtents;
            public uint flags;

            // 仅用于调试
            public GameObject sourceObject;
        }

        private static void WriteToFile(string assetPath, List<Entry> entries)
        {
            string fullPath = Path.GetFullPath(assetPath);

            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                // 头部
                bw.Write(MAGIC);
                bw.Write(VERSION);

                float worldScale = 1.0f;
                bw.Write(worldScale);

                uint boxCount = (uint)entries.Count;
                bw.Write(boxCount);

                // 每个 Box
                foreach (var e in entries)
                {
                    // center
                    bw.Write(e.center.x);
                    bw.Write(e.center.y);
                    bw.Write(e.center.z);

                    // rotation (quaternion x,y,z,w)
                    bw.Write(e.rotation.x);
                    bw.Write(e.rotation.y);
                    bw.Write(e.rotation.z);
                    bw.Write(e.rotation.w);

                    // halfExtents
                    bw.Write(e.halfExtents.x);
                    bw.Write(e.halfExtents.y);
                    bw.Write(e.halfExtents.z);

                    // flags
                    bw.Write(e.flags);
                }
            }
        }
    }
#endif
}