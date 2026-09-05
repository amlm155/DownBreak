#if UNITY_EDITOR
using System.Collections.Generic;
using GAS.StateSystem;
using UnityEditor;
using UnityEngine;

namespace PlayerControllerSpace.EditorTools
{
    /// <summary>
    /// 玩家属性 GM 面板 运行时调试加减接口
    /// </summary>
    public sealed class PlayerGmWindow : EditorWindow
    {
        /// <summary> 目标玩家 </summary>
        private PlayerController player;

        /// <summary> 默认增减步长 </summary>
        private float deltaStep = 10f;

        /// <summary> 滚动位置 </summary>
        private Vector2 scrollPos;

        /// <summary> 绝对值编辑缓存 </summary>
        private readonly Dictionary<string, float> absoluteValueDict = new Dictionary<string, float>();

        [MenuItem("Tools/DownBreak/玩家属性 GM")]
        private static void Open()
        {
            var window = GetWindow<PlayerGmWindow>("玩家 GM");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            TryBindPlayer();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                TryBindPlayer();
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                player = null;
                absoluteValueDict.Clear();
            }
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "仅 Play 模式可用\n通过真实 Change 接口改属性 用于验证 UI 与数值链路",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                player = (PlayerController)EditorGUILayout.ObjectField(
                    "玩家",
                    player,
                    typeof(PlayerController),
                    true);

                if (GUILayout.Button("自动查找", GUILayout.Width(80f)))
                    TryBindPlayer(true);
            }

            deltaStep = EditorGUILayout.FloatField("默认步长", deltaStep);
            deltaStep = Mathf.Max(0.01f, deltaStep);

            EditorGUILayout.Space(8f);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请进入 Play 模式后再操作", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (player == null)
            {
                EditorGUILayout.HelpBox("未找到 PlayerController", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            var statController = player.StatController;
            if (statController == null)
            {
                EditorGUILayout.HelpBox("玩家缺少 StatController", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawStatRow("体力 Power", PlayerStatIds.Power, statController, player.ChangePlayerPowerValue);
            DrawStatRow("水分 Water", PlayerStatIds.Water, statController, player.ChangePlayerWaterValue);
            DrawStatRow("饱食 Food", PlayerStatIds.Food, statController, player.ChangePlayerFoodValue);
            DrawStatRow("血量 Health", PlayerStatIds.Health, statController, player.ChangeHealthValue);
            DrawStatRow(
                "理智 San",
                PlayerStatIds.San,
                statController,
                delta => statController.ChangeValue(PlayerStatIds.San, delta, true));
            DrawStatRow(
                "精力 Energy",
                PlayerStatIds.Energy,
                statController,
                delta => statController.ChangeValue(PlayerStatIds.Energy, delta, true));
            DrawStatRow(
                "疼痛 Pain",
                PlayerStatIds.Pain,
                statController,
                delta => statController.ChangeValue(PlayerStatIds.Pain, delta, true));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("被动属性", EditorStyles.boldLabel);
            DrawPassiveStatRow("攻击力 Attack", PlayerStatIds.Attack, statController);
            DrawPassiveStatRow("防御力 Defence", PlayerStatIds.Defence, statController);

            EditorGUILayout.Space(12f);
            DrawBatchButtons();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单条属性调试行
        /// </summary>
        /// <param name="label">显示名</param>
        /// <param name="statName">属性名</param>
        /// <param name="statController">属性控制器</param>
        /// <param name="changeAction">加减方法</param>
        private void DrawStatRow(
            string label,
            string statName,
            StatController statController,
            System.Action<float> changeAction)
        {
            var imStat = statController.GetImStat(statName);
            if (imStat == null)
            {
                EditorGUILayout.HelpBox($"{statName} 未注册为即时属性", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            float current = imStat.CurrentValue;
            float max = imStat.MaxValue;
            float hardMax = imStat.HardMaxValue;
            float normalized = max > 0f ? current / max : 0f;

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 18f),
                Mathf.Clamp01(normalized),
                $"{current:0.##} / {max:0.##}   硬上限 {hardMax:0.##}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"-{deltaStep:0.##}"))
                    changeAction(-deltaStep);

                if (GUILayout.Button($"+{deltaStep:0.##}"))
                    changeAction(deltaStep);

                if (GUILayout.Button("清空"))
                    changeAction(-current);

                if (GUILayout.Button("拉满"))
                    changeAction(max - current);

                if (GUILayout.Button("硬上限"))
                    statController.SetValueForDebug(statName, hardMax);
            }

            if (!absoluteValueDict.ContainsKey(statName))
                absoluteValueDict[statName] = current;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("设为绝对值", GUILayout.Width(70f));
                absoluteValueDict[statName] = EditorGUILayout.FloatField(absoluteValueDict[statName]);
                if (GUILayout.Button("应用", GUILayout.Width(50f)))
                    statController.SetValueForDebug(
                        statName,
                        Mathf.Clamp(absoluteValueDict[statName], imStat.MinValue, hardMax));
                if (GUILayout.Button("同步", GUILayout.Width(50f)))
                    absoluteValueDict[statName] = current;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        /// <summary>
        /// 绘制被动属性调试行
        /// 被动属性没有当前值和加减操作 只显示基础值与最终值
        /// </summary>
        /// <param name="label">显示名</param>
        /// <param name="statName">属性名</param>
        /// <param name="statController">属性控制器</param>
        private void DrawPassiveStatRow(string label, string statName, StatController statController)
        {
            var passiveStat = statController.GetPassiveStat(statName);
            if (passiveStat == null)
            {
                EditorGUILayout.HelpBox($"{statName} 未注册为被动属性", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"基础值 {passiveStat.BaseValue:0.##}   最终值 {passiveStat.FinalValue:0.##}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        /// <summary>
        /// 绘制批量操作
        /// </summary>
        private void DrawBatchButtons()
        {
            EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全部拉满"))
                    FillAll();

                if (GUILayout.Button("全部清空"))
                    EmptyAll();
            }
        }

        /// <summary>
        /// 全部拉满
        /// </summary>
        private void FillAll()
        {
            ApplyTowardMax("Power", player.ChangePlayerPowerValue);
            ApplyTowardMax("Water", player.ChangePlayerWaterValue);
            ApplyTowardMax("Food", player.ChangePlayerFoodValue);
            ApplyTowardMax("Health", player.ChangeHealthValue);
            ApplyTowardMax("San", player.ChangeSanValue);
        }

        /// <summary>
        /// 全部清空
        /// </summary>
        private void EmptyAll()
        {
            ApplyTowardZero("Power", player.ChangePlayerPowerValue);
            ApplyTowardZero("Water", player.ChangePlayerWaterValue);
            ApplyTowardZero("Food", player.ChangePlayerFoodValue);
            ApplyTowardZero("Health", player.ChangeHealthValue);
            ApplyTowardZero("San", player.ChangeSanValue);
        }

        /// <summary>
        /// 将属性补到最大值
        /// </summary>
        /// <param name="statName">属性名</param>
        /// <param name="changeAction">加减方法</param>
        private void ApplyTowardMax(string statName, System.Action<float> changeAction)
        {
            var imStat = player.StatController.GetImStat(statName);
            if (imStat == null)
                return;
            changeAction(imStat.MaxValue - imStat.CurrentValue);
        }

        /// <summary>
        /// 将属性扣到 0
        /// </summary>
        /// <param name="statName">属性名</param>
        /// <param name="changeAction">加减方法</param>
        private void ApplyTowardZero(string statName, System.Action<float> changeAction)
        {
            var imStat = player.StatController.GetImStat(statName);
            if (imStat == null)
                return;
            changeAction(-imStat.CurrentValue);
        }

        /// <summary>
        /// 自动绑定场景玩家
        /// </summary>
        /// <param name="force">是否强制重新查找</param>
        private void TryBindPlayer(bool force = false)
        {
            if (!Application.isPlaying)
                return;

            if (!force && player != null)
                return;

            player = Object.FindFirstObjectByType<PlayerController>();
        }
    }
}
#endif
