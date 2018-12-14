using UnityEngine;

namespace Elang.Tools
{
    /// <summary>
    /// 
    /// 使用优化Mesh的Material之前需要先合并Material的贴图.这一步是合并贴图之后替换Material使用的小图成图集，重写对于的UV操作
    /// 
    /// 1. 需要合并的MeshRenderer都使用了相同的Material，并且Material的属性相同
    /// 
    /// 2.需要合并的MeshRenderer使用了不同的Material，但是Shader相同并除了Shader所使用的Texture不同之外其他属性都相同。
    ///   这种情况先合并贴图，然后根据贴图再图集中的位置偏移重写MeshRenderer的UV，然后合并MeshRenderer
    /// </summary>
    public class OptimizeMeshMaterial
    {



        /// <summary>
        /// 重写Mesh的UV
        /// </summary>
        public void OverrideMeshUV()
        {

        }
        
    }
}