using UnityEngine;

namespace Utils.CollidersExporter
{
    public enum ServerSurfaceType
    {
        Walkable = 0,     // 可站立
        NonWalkable = 1,  // 不可站立（比如屋顶）
    }
    
    [DisallowMultipleComponent]
    public class ServerCollisionMarker : MonoBehaviour
    {
        [Tooltip("用于服务器碰撞系统的表面类型标记")]
        public ServerSurfaceType surfaceType = ServerSurfaceType.Walkable;
    }
}

