using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Logging;
using UniGLTF;
using Unity.VisualScripting;
using UniVRM10;
using Object = UnityEngine.Object;

namespace uDesktopMascot
{
    /// <summary>
    /// VRMファイルを読み込む
    /// </summary>
    public class LoadVrm : IDisposable
    {
        /// <summary>
        /// デフォルトのVRMファイル名
        /// </summary>
        private const string DefaultVrmFileName = "DefaultModel/DefaultModel";

        /// <summary>
        ///    runtime gltf instance
        /// </summary>
        private RuntimeGltfInstance _runtimeGltfInstance;

        /// <summary>
        ///   VRMモデルのインスタンス
        /// </summary>
        private Vrm10Instance _vrm10Instance;

        /// <summary>
        ///   VRMモデルのインスタンス
        /// </summary>
        public Vrm10Instance Instance => _vrm10Instance;

        /// <summary>
        ///  VRMモデルのコントロールリグ
        /// </summary>
        public Vrm10RuntimeControlRig ControlRig => _vrm10Instance.Runtime.ControlRig;

        /// <summary>
        /// VRMモデルのランタイム
        /// </summary>
        public Vrm10Runtime Runtime => _vrm10Instance.Runtime;
        
        private IVrm10Animation m_src = default;

        public IVrm10Animation TPose;

        public IVrm10Animation Motion
        {
            get => m_src;
            set
            {
                if (m_src != null)
                {
                    m_src.Dispose();
                }

                m_src = value;

                TPose = new Vrm10TPose(m_src.ControlRig.Item1.GetRawHipsPosition());
            }
        }

        /// <summary>
        ///     デフォルトのVRMモデルをロードして表示する
        /// </summary>
        public async UniTask LoadDefaultModel(CancellationToken cancellationToken)
        {
            Vrm10Instance loadPrefab = null;
            try
            {
                // ResourcesフォルダからPrefabをロード
                loadPrefab = await Resources.LoadAsync<Vrm10Instance>(DefaultVrmFileName).WithCancellation(cancellationToken) as Vrm10Instance;

            } catch (Exception e)
            {
                Log.Error($"デフォルトのPrefabのロードに失敗しました: {DefaultVrmFileName}.prefab");
            }
            
            if (loadPrefab == null)
            {
                Log.Error($"デフォルトのPrefabがResourcesフォルダに見つかりません: {DefaultVrmFileName}.prefab");
                return;
            }

            // Prefabをインスタンス化
            _vrm10Instance = GameObject.Instantiate(loadPrefab);

            if (_vrm10Instance != null)
            {
                _vrm10Instance.UpdateType = Vrm10Instance.UpdateTypes.LateUpdate;
                _vrm10Instance.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.YawPitchValue;
            }

            _vrm10Instance.AddComponent<Animation>();

            Log.Debug("デフォルトモデルのロードと表示が完了しました: " + DefaultVrmFileName);
        }

        /// <summary>
        ///    VRMファイルをロードして表示する
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        public async UniTask LoadVrmModel(string path,CancellationToken cancellationToken)
        {
            var vrm10Instance = await Vrm10.LoadPathAsync(path,
                canLoadVrm0X: true,
                ct: cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                UnityObjectDestroyer.DestroyRuntimeOrEditor(vrm10Instance.gameObject);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (vrm10Instance == null)
            {
                Log.Error("LoadPathAsync is null");
                return;
            }
            _vrm10Instance = vrm10Instance.GetComponent<Vrm10Instance>();
            if (_vrm10Instance != null)
            {
                _vrm10Instance.UpdateType = Vrm10Instance.UpdateTypes.LateUpdate;
                _vrm10Instance.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.YawPitchValue;
            }
        }
        
        /// <summary>
        /// StreamingAssets/motion フォルダからモーションを読み込む
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async UniTask LoadMotionsAsync(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            if (ext == ".bvh")
            {
                Motion = BvhMotionUtility.LoadBvhFromPath(path);
                return;
            }

            using var data = new AutoGltfFileParser(path).Parse();
            using var loader = new VrmAnimationImporter(data);
            var instance = await loader.LoadAsync(new ImmediateCaller());
            Motion = instance.GetComponent<Vrm10AnimationInstance>();
            // Motion.ShowBoxMan(false);
            var vrmAnimation = _vrm10Instance.GetComponent<Animation>();
            vrmAnimation.AddClip(instance.GetComponent<Animation>().clip,"motion");
            vrmAnimation.clip = instance.GetComponent<Animation>().clip;
            vrmAnimation.Play();

            // _vrm10Instance.Runtime.VrmAnimation.
            // _vrm10Instance.Runtime.VrmAnimation = Motion;
            // _vrm10Instance.Runtime.SpringBone.SetModelLevel(vrm.transform, new BlittableModelLevel
            // {
            //     ExternalForce = new Vector3(m_springboneExternalX.value, m_springboneExternalY.value, m_springboneExternalZ.value),
            //     StopSpringBoneWriteback = m_springbonePause.isOn,
            //     SupportsScalingAtRuntime = m_springboneScaling.isOn,
            // });
            // instance.GetComponent<Animation>().Play();
        }

        /// <summary>
        ///    リソースを解放する
        /// </summary>
        public void Dispose()
        {
            if (_runtimeGltfInstance != null)
            {
                Object.Destroy(_runtimeGltfInstance.gameObject);
            }
        }
    }
}