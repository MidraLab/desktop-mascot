using System;
using System.Collections.Generic;
using System.IO;
using UniHumanoid;
using UnityEngine;
using UniVRM10;
using Object = UnityEngine.Object;

namespace uDesktopMascot
{
    /// <summary>
    /// BVH モーションを扱うクラス
    /// </summary>
    public class BvhMotionUtility : IVrm10Animation
    {
        /// <summary>
        /// BVH モーションのインポートコンテキスト
        /// </summary>
        private readonly BvhImporterContext _mContext;
        public Transform Root => _mContext?.Root.transform;
        
        /// <summary>
        /// ボーンの可視化用の SkinnedMeshRenderer
        /// </summary>
        public SkinnedMeshRenderer MBoxMan;
        
        /// <summary>
        /// ボーンの可視化用の SkinnedMeshRenderer
        /// </summary>
        public SkinnedMeshRenderer BoxMan => MBoxMan;
        
        /// <summary>
        /// モーションのRig
        /// </summary>
        private readonly (INormalizedPoseProvider, ITPoseProvider) _mControlRig;
        
        /// <summary>
        /// モーションのRig
        /// </summary>
        (INormalizedPoseProvider, ITPoseProvider) IVrm10Animation.ControlRig => _mControlRig;
        
        /// <summary>
        /// expression のマップ
        /// </summary>
        private readonly IDictionary<ExpressionKey, Func<float>> _expressionMap = new Dictionary<ExpressionKey, Func<float>>();
        
        /// <summary>
        /// expression のマップ
        /// </summary>
        public IReadOnlyDictionary<ExpressionKey, Func<float>> ExpressionMap => (IReadOnlyDictionary<ExpressionKey, Func<float>>)_expressionMap;

        /// <summary>
        /// LookAt の設定
        /// </summary>
        public LookAtInput? LookAt { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context"></param>
        public BvhMotionUtility(BvhImporterContext context)
        {
            _mContext = context;
            var provider = new AnimatorPoseProvider(_mContext.Root.transform, _mContext.Root.GetComponent<Animator>());
            _mControlRig = (provider, provider);

            // create SkinnedMesh for bone visualize
            var animator = _mContext.Root.GetComponent<Animator>();
            MBoxMan = SkeletonMeshUtility.CreateRenderer(animator);
            var tmpPrimitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var defaultMaterial = tmpPrimitive.GetComponent<Renderer>().sharedMaterial;
            var material = new Material(defaultMaterial);
            Object.Destroy(tmpPrimitive);
            BoxMan.sharedMaterial = material;
            var mesh = BoxMan.sharedMesh;
            mesh.name = "box-man";
        }

        /// <summary>
        /// BVH モーションをテキストから読み込む
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BvhMotionUtility LoadBvhFromText(string source, string path = "tmp.bvh")
        {
            var context = new BvhImporterContext();
            context.Parse(path, source);
            context.Load();
            return new BvhMotionUtility(context);
        }
        
        /// <summary>
        /// BVH モーションをファイルから読み込む
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BvhMotionUtility LoadBvhFromPath(string path)
        {
            return LoadBvhFromText(File.ReadAllText(path), path);
        }

        /// <summary>
        /// ボーンの可視化を表示する
        /// </summary>
        /// <param name="enable"></param>
        public void ShowBoxMan(bool enable)
        {
            MBoxMan.enabled = enable;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Object.Destroy(_mContext.Root);
        }
    }
}
