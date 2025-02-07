﻿using System.IO;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.Logging;

namespace uDesktopMascot
{
    /// <summary>
    /// モデルの追加と選択ダイアログ
    /// </summary>
    public class SelectModelDialog : DialogBase
    {
        /// <summary>
        /// ModelInfoのPrefab
        /// </summary>
        [SerializeField] private ModelInfo modelInfoPrefab;

        /// <summary>
        /// ScrollViewのContent
        /// </summary>
        [SerializeField] private Transform contentTransform;

        /// <summary>
        /// 現在ロード中または表示中のモデル
        /// </summary>
        private ModelInfo _currentModel;

        private CancellationTokenSource _cancellationTokenSource;

        private protected override void Awake()
        {
            base.Awake();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async void Start()
        {
            await AddDefaultModelList();
            // モデルリストをロード
            LoadModelListAsync().Forget();
        }

        /// <summary>
        /// モデルリストを非同期でロードし、表示する
        /// </summary>
        private async UniTaskVoid LoadModelListAsync()
        {
            // StreamingAssetsフォルダ内のVRMファイルを取得
            string streamingAssetsPath = Application.streamingAssetsPath;
            string[] vrmFiles = Directory.GetFiles(streamingAssetsPath, "*.vrm", SearchOption.AllDirectories);

            foreach (string vrmFile in vrmFiles)
            {
                // ファイル名のみを取得
                string fileName = Path.GetFileName(vrmFile);

                // メインスレッドでUIを更新
                await UniTask.SwitchToMainThread();

                // ModelInfoアイテムを生成
                var item = Instantiate(modelInfoPrefab, contentTransform);

                // モデル情報を初期化
                item.Initialize(fileName, () => OnModelSelected(item,vrmFile).Forget());
            }
        }

        /// <summary>
        /// デフォルトのモデルリストを追加
        /// </summary>
        private async UniTask AddDefaultModelList()
        {
            // デフォルトのモデルリストを追加
            await UniTask.SwitchToMainThread();
            var item = Instantiate(modelInfoPrefab, contentTransform);
            var defaultModelPath = Path.Combine(Application.dataPath, Constant.DefaultVrmFileName);
            item.Initialize("Default", () => OnModelSelected(item, defaultModelPath).Forget());
            _currentModel = item;
            _currentModel.SetSelected(true);
        }

        /// <summary>
        /// モデルが選択されたときの処理
        /// </summary>
        /// <param name="modelInfo"></param>
        /// <param name="path">選択されたモデルのパス</param>
        private async UniTaskVoid OnModelSelected(ModelInfo modelInfo,string path)
        {
            modelInfo.SetSelected(true);
            _currentModel?.SetSelected(false);
            _currentModel = modelInfo;
            return;
            // 既存のモデルがある場合は削除
            if (_currentModel != null)
            {
                Destroy(_currentModel);
            }

            // 指定されたモデルをロード
            GameObject model = await LoadVRM.LoadModelAsync(path, _cancellationTokenSource.Token);

            if (model != null)
            {
                // モデルをシーンに配置
                model.transform.position = Vector3.zero;
            } else
            {
                Log.Error($"Failed to load Model:{path}");
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}