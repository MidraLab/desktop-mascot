﻿using Kirurobo;
using Unity.Logging;
using UnityEngine;

namespace uDesktopMascot
{
    /// <summary>
    ///     システムマネージャー
    /// </summary>
    public class SystemManager : SingletonMonoBehaviour<SystemManager>
    {
        /// <summary>
        ///    ウィンドウコントローラー
        /// </summary>
        [SerializeField] private UniWindowController windowController;

        private protected override void Awake()
        {
            base.Awake();
            
            LoadSetting();

            // PCのスペック応じてQualitySettingsを変更
            SetQualityLevel();
        }

        /// <summary>
        ///    設定を読み込む
        /// </summary>
        private void LoadSetting()
        {
            var systemSettings = ApplicationSettings.Instance.Display;
            windowController.isTopmost = systemSettings.AlwaysOnTop;
            windowController.opacityThreshold = systemSettings.Opacity;
            
            Log.Info("System設定 : 常に最前面 = " + systemSettings.AlwaysOnTop + ", 不透明度 = " + systemSettings.Opacity);
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
                Log.Info("品質レベルをシステムスペックに基づき " + QualitySettings.names[qualityLevel] + " に設定しました。");

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
                Log.Info("品質レベルを設定ファイルの値 " + QualitySettings.names[qualityLevel] + " に設定しました。");
            }

            // TargetFrameRateの設定（同様に処理）
            if (performanceSettings.TargetFrameRate > 0)
            {
                Application.targetFrameRate = performanceSettings.TargetFrameRate;
                Log.Info("ターゲットフレームレートを " + Application.targetFrameRate + " に設定しました。");
            }
            else
            {
                // 無効な場合、デフォルト値を設定し、設定ファイルを更新
                Application.targetFrameRate = 60; // デフォルト値
                performanceSettings.TargetFrameRate = 60;
                Log.Warning("無効なターゲットフレームレートが設定されていたため、デフォルト値 " + Application.targetFrameRate + " に設定しました。");
                ApplicationSettings.Instance.SaveSettings();
                Log.Info("デフォルトのターゲットフレームレートを設定ファイルに保存しました。");
            }
        }
    }
}