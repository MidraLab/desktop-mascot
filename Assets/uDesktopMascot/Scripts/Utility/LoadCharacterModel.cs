using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Logging;
using UnityEngine;

namespace uDesktopMascot
{
    /// <summary>
    /// キャラクターモデルを読み込むクラス
    /// </summary>
    public class LoadCharacterModel : IDisposable
    {
        
        /// <summary>
        /// ロード済みのVRM
        /// </summary>
        private readonly LoadVrm _loadVrm;
        
        /// <summary>
        /// キャラクターモデルのGameObject
        /// </summary>
        public GameObject CharacterModelObject => _loadVrm.Instance.gameObject;
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LoadCharacterModel()
        {
            _loadVrm = new LoadVrm();
        }

        /// <summary>
        /// キャラクターモデルを読み込む
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask LoadModel(CancellationToken cancellationToken)
        {
            // 設定ファイルからキャラクター情報の取得
            var characterSettings = ApplicationSettings.Instance.Character;
            
            // パスにモデルが存在するかどうかを確認
            if (!File.Exists(characterSettings.ModelPath))
            {
                Log.Warning("指定されたモデルが見つかりません: {0}。デフォルトモデルをロードします", characterSettings.ModelPath);
                await _loadVrm.LoadDefaultModel(cancellationToken);
            } else
            {
                await _loadVrm.LoadVrmModel(characterSettings.ModelPath, cancellationToken);
                
                // シェーダーをlilToonに置き換える
                bool shaderReplaceSuccess = ReplaceShadersWithLilToon(_loadVrm.Instance.gameObject);
            
                if (!shaderReplaceSuccess)
                {
                    Log.Warning("シェーダーの置き換えに失敗したため、デフォルトのシェーダーを使用します。");
                }
            }
        }

        /// <summary>
        /// VRMAファイルをロードする
        /// </summary>
        /// <param name="path"></param>
        public async UniTask LoadVRMA(string path)
        {
           await _loadVrm.LoadMotionsAsync(path);
        }

        /// <summary>
        /// モデルのシェーダーをlilToonに置き換える
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static bool ReplaceShadersWithLilToon(GameObject model)
        {
            try
            {
                // lilToon のシェーダーを取得
                var lilToonCutoutShader = Shader.Find("Hidden/lilToonCutout");
                var lilToonTransparentShader = Shader.Find("Hidden/lilToonTransparent");

                if (lilToonCutoutShader == null || lilToonTransparentShader == null)
                {
                    Log.Warning("lilToon シェーダーの一部が見つかりません。プロジェクトに lilToon シェーダーが含まれており、正しくインストールされていることを確認してください。");
                    return false;
                }

                // すべての Renderer を取得
                var renderers = model.GetComponentsInChildren<Renderer>(true);

                foreach (var renderer in renderers)
                {
                    // 各 Renderer のマテリアルを取得
                    var materials = renderer.materials;

                    foreach (var material in materials)
                    {
                        if (material == null || material.shader == null)
                        {
                            continue;
                        }

                        // シェーダーを置き換え
                        material.shader = lilToonCutoutShader;

                        material.SetFloat("_TransparentMode", 2); // 0: Opaque, 1: Cutout, 2: Transparent, etc.
                        material.SetFloat("_OutlineEnable", 1);   // アウトラインを有効化
                    }
                }

                Log.Info("シェーダーの置き換えが完了しました。");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"シェーダーの置き換え中にエラーが発生しました: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _loadVrm?.Dispose();
        }
    }
}