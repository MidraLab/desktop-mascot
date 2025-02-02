using Cysharp.Threading.Tasks;

#if !UNITY_WSA
using Kirurobo;
#endif
using Unity.Logging;
using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Threading;

namespace uDesktopMascot
{
    /// <summary>
    ///     システムマネージャー
    /// </summary>
    public class SystemManager : SingletonMonoBehaviour<SystemManager>
    {
#if !UNITY_WSA
        
        /// <summary>
        ///    ウィンドウコントローラー
        /// </summary>
        [SerializeField] private UniWindowController windowController;
#endif

        /// <summary>
        ///   アップグレードダイアログを表示する
        /// </summary>
        [SerializeField] private ShowUpdateDialog showUpdateDialog;
        
        /// <summary>
        /// キャンセルトークンソース
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// バージョンチェッカー
        /// </summary>
        private CheckVersion _checkVersion;

        private protected override void Awake()
        {
            base.Awake();

            _cancellationTokenSource = new CancellationTokenSource();
            _checkVersion = new CheckVersion();

            LoadSetting();

            // ローカライゼーションを設定
            SetLocalizationAsync().Forget();

            // PCのスペック応じてQualitySettingsを変更
            SetQualityLevel();

        }

        private void Start()
        {
            SetEvent();
            
            // アップデートチェックを非同期に開始
            CheckUpdateAsync().Forget();
        }

        /// <summary>
        ///   イベントを設定
        /// </summary>
        private void SetEvent()
        {
            showUpdateDialog.OnClose = SaveSkipUpdateDialog;
        }

        /// <summary>
        ///    設定を読み込む
        /// </summary>
        private void LoadSetting()
        {
            var systemSettings = ApplicationSettings.Instance.Display;

#if !UNITY_WSA
            
            windowController.isTopmost = systemSettings.AlwaysOnTop;
            windowController.opacityThreshold = systemSettings.Opacity;
#endif

            Log.Info($"System設定 : 常に最前面 = {systemSettings.AlwaysOnTop}, 不透明度 = {systemSettings.Opacity}");
        }

        /// <summary>
        /// 新しいバージョンがあるかどうかをチェック
        /// </summary>
        private async UniTask CheckUpdateAsync()
        {
            // アップデートチェック
            var isUpdateAvailable = await _checkVersion.IsUpdateAvailable(_cancellationTokenSource.Token);
            
            if (isUpdateAvailable)
            {
                var displaySettings = ApplicationSettings.Instance.Display;

                // スキップしたバージョンを取得
                var skippedVersion = displaySettings.SkippedVersion;

                // スキップしたバージョンが設定されている場合
                if (!string.IsNullOrEmpty(skippedVersion))
                {
                    // 最新バージョンがスキップしたバージョンより新しいかを確認
                    if (_checkVersion.IsNewerVersion(_checkVersion.LatestVersion, skippedVersion))
                    {
                        // 新しいバージョンがある場合、ダイアログを表示
                        showUpdateDialog.Show(_checkVersion.LatestVersion);
                    }
                    else
                    {
                        // スキップしたバージョンと同じかそれより古い場合、ダイアログを表示しない
                        Log.Info("ユーザーがスキップしたバージョンのため、アップデートダイアログを表示しません。");
                    }
                }
                else
                {
                    // スキップしたバージョンがない場合、ダイアログを表示
                    showUpdateDialog.Show(_checkVersion.LatestVersion);
                }
            }
        }

        /// <summary>
        ///    アップグレードダイアログをスキップするかどうかを保存
        /// </summary>
        private void SaveSkipUpdateDialog()
        {
            var displaySettings = ApplicationSettings.Instance.Display;

            // ユーザーがスキップを選択した場合、現在の最新バージョンを保存
            displaySettings.SkippedVersion = showUpdateDialog.SkipShowUpgradeDialog ? _checkVersion.LatestVersion :
                string.Empty;

            ApplicationSettings.Instance.SaveSettings();
        }

        /// <summary>
        ///     品質レベルを設定
        /// </summary>
        private void SetQualityLevel()
        {
            var performanceSettings = ApplicationSettings.Instance.Performance;

            int qualityLevel = performanceSettings.QualityLevel;
            bool isQualityLevelValid = qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length;

            if (!isQualityLevelValid)
            {
                // 無効な場合、品質レベルを動的に調整
                qualityLevel = QualityLevelAdjuster.AdjustQualityLevel();
                QualitySettings.SetQualityLevel(qualityLevel, true);
                Log.Info($"品質レベルをシステムスペックに基づき {QualitySettings.names[qualityLevel]} に設定しました。");

                // 動的に調整した値を設定に反映
                performanceSettings.QualityLevel = qualityLevel;

                // 設定ファイルを更新
                ApplicationSettings.Instance.SaveSettings();
                Log.Info("動的に調整した品質レベルを設定ファイルに保存しました。");
            }
            else
            {
                // 有効な場合、設定ファイルの値を使用
                QualitySettings.SetQualityLevel(qualityLevel, true);
                Log.Info($"品質レベルを設定ファイルの値 {QualitySettings.names[qualityLevel]} に設定しました。");
            }

            // TargetFrameRateの設定（同様に処理）
            if (performanceSettings.TargetFrameRate > 0)
            {
                Application.targetFrameRate = performanceSettings.TargetFrameRate;
                Log.Info($"ターゲットフレームレートを {Application.targetFrameRate} に設定しました。");
            }
            else
            {
                // 無効な場合、デフォルト値を設定し、設定ファイルを更新
                Application.targetFrameRate = 60; // デフォルト値
                performanceSettings.TargetFrameRate = 60;
                Log.Warning($"無効なターゲットフレームレートが設定されていたため、デフォルト値 {Application.targetFrameRate} に設定しました。");
                ApplicationSettings.Instance.SaveSettings();
                Log.Info("デフォルトのターゲットフレームレートを設定ファイルに保存しました。");
            }
        }

        /// <summary>
        ///   ローカライゼーションを設定
        /// </summary>
        private async UniTask SetLocalizationAsync()
        {
            await LocalizationSettings.InitializationOperation;

            // システムの言語設定を取得
            var systemLanguage = Application.systemLanguage;

            // 対応するロケールを取得
            var selectedLocale = LocalizeUtility.GetLocale(systemLanguage);

            if (selectedLocale != null)
            {
                // 選択したロケールを設定
                LocalizationSettings.SelectedLocale = selectedLocale;
                Log.Info($"ロケールを '{selectedLocale.LocaleName}' に設定しました。");
            }
            else
            {
                // 対応するロケールがない場合はデフォルトロケール（英語）を設定
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                Log.Warning($"システム言語 '{systemLanguage}' に対応するロケールが見つかりませんでした。デフォルトのロケールを '{LocalizationSettings.SelectedLocale.LocaleName}' に設定します。");
            }
        }

        /// <summary>
        ///   破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}