using UnityEditor;

namespace Yiliang.Tools
{
    public class OptimizeMeshEditr : Editor
    {
        [MenuItem("GameObject/OptimizeMesh/Combine Mesh", false, 0)]
        public static void CombineMesh()
        {
            OptimizeMesh.CombineMesh();
        }
    }
}
