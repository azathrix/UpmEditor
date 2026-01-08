#if UNITY_EDITOR
using System.IO;
using Azathrix.UpmEditor.Editor.Core;
using Azathrix.UpmEditor.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace  Azathrix.UpmEditor.Editor.UI
{
    /// <summary>
    /// UPM 包发布窗口
    /// </summary>
    public class UPMPublishWindow : EditorWindow
    {
        private string _packagePath = "";
        private string _registry;
        private PublishService.RegistryType _registryType;
        private UPMPackageData _packageData;
        private Vector2 _scrollPosition;

        // npm login status
        private bool _npmLoggedIn;
        private string _npmUsername;
        private double _lastLoginCheckTime;

        // Unity Signing
        private string _unityUsername;
        private string _unityPassword;
        private string _cloudOrgId;
        private bool _foldUnitySigning = true;
        private bool _saveCredentials;

        [MenuItem(UPMConstants.ToolsMenuRoot + "发布 UPM")]
        public static void ShowWindow()
        {
            var window = GetWindow<UPMPublishWindow>("发布 UPM");
            window.minSize = new Vector2(400, 350);
            window.AutoDetectPackage();
        }

        /// <summary>
        /// 打开发布窗口并设置包路径
        /// </summary>
        public static void ShowWindow(string packagePath)
        {
            var window = GetWindow<UPMPublishWindow>("发布 UPM");
            window.minSize = new Vector2(400, 350);
            window._packagePath = packagePath;
            window.LoadPackageData();
        }

        private void OnEnable()
        {
            _registry = PublishService.GetRegistry();
            _registryType = PublishService.GetRegistryType();
            _saveCredentials = PublishService.GetSaveCredentials();
            if (_saveCredentials)
            {
                _unityUsername = PublishService.GetUnityUsername();
                _unityPassword = PublishService.GetUnityPassword();
                _cloudOrgId = PublishService.GetCloudOrgId();
            }
            AutoDetectPackage();
            CheckNpmLoginAsync();
        }

        private void CheckNpmLoginAsync()
        {
            if (EditorApplication.timeSinceStartup - _lastLoginCheckTime < 30) return;
            _lastLoginCheckTime = EditorApplication.timeSinceStartup;

            var (loggedIn, username) = PublishService.CheckNpmLogin(_registry);
            _npmLoggedIn = loggedIn;
            _npmUsername = username;
        }

        /// <summary>
        /// 自动检测当前选中的目录是否是UPM目录
        /// </summary>
        private void AutoDetectPackage()
        {
            // 如果已有路径且有效，不重新检测
            if (!string.IsNullOrEmpty(_packagePath) && _packageData != null) return;

            // 检测当前选中的目录
            if (Selection.activeObject != null)
            {
                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path))
                {
                    // 检查是否是文件夹
                    if (AssetDatabase.IsValidFolder(path) || System.IO.Directory.Exists(System.IO.Path.GetFullPath(path)))
                    {
                        if (UPMPackageValidator.HasValidPackageJson(path))
                        {
                            _packagePath = path;
                            LoadPackageData();
                            return;
                        }
                    }
                }
            }
        }

        private void OnSelectionChange()
        {
            // 选择变化时自动检测
            if (Selection.activeObject != null)
            {
                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path))
                {
                    if (AssetDatabase.IsValidFolder(path) || System.IO.Directory.Exists(System.IO.Path.GetFullPath(path)))
                    {
                        if (UPMPackageValidator.HasValidPackageJson(path))
                        {
                            _packagePath = path;
                            LoadPackageData();
                            Repaint();
                        }
                    }
                }
            }
        }

        private void LoadPackageData()
        {
            if (string.IsNullOrEmpty(_packagePath)) return;
            _packageData = PackageJsonService.ReadPackageJson(_packagePath);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UPM 包发布", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawPackageSelection();
            EditorGUILayout.Space(10);

            if (_packageData != null)
            {
                DrawPackageInfo();
                EditorGUILayout.Space(10);
                DrawUnitySigning();
                EditorGUILayout.Space(10);
                DrawRegistrySettings();
                EditorGUILayout.Space(10);
                DrawActions();
            }

            EditorGUILayout.Space(10);
            DrawHelp();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPackageSelection()
        {
            EditorGUILayout.LabelField("包路径", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _packagePath = EditorGUILayout.TextField(
                new GUIContent("路径", "选择要发布的 UPM 包目录"),
                _packagePath);

            if (GUILayout.Button(new GUIContent("...", "浏览选择包目录"), GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("选择 UPM 包", "Packages", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = ConvertToRelativePath(path);
                    _packagePath = path;
                    LoadPackageData();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_packagePath) && _packageData == null)
            {
                EditorGUILayout.HelpBox("未找到 package.json 文件", MessageType.Warning);
            }
        }

        private void DrawPackageInfo()
        {
            EditorGUILayout.LabelField("包信息", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("名称", _packageData.name);
            EditorGUILayout.LabelField("显示名称", _packageData.displayName);
            EditorGUILayout.LabelField("版本", _packageData.version);
            EditorGUILayout.LabelField("Unity 版本", _packageData.unity);
            EditorGUILayout.EndVertical();
        }

        private void DrawUnitySigning()
        {
            _foldUnitySigning = EditorGUILayout.BeginFoldoutHeaderGroup(_foldUnitySigning, "Unity 签名配置 (Unity 6.3+)");
            if (_foldUnitySigning)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _unityUsername = EditorGUILayout.TextField(
                    new GUIContent("Unity ID 邮箱", "Unity 账号邮箱"),
                    _unityUsername);

                _unityPassword = EditorGUILayout.PasswordField(
                    new GUIContent("密码", "Unity 账号密码"),
                    _unityPassword);

                _cloudOrgId = EditorGUILayout.TextField(
                    new GUIContent("Organization ID", "Unity Cloud Organization ID"),
                    _cloudOrgId);

                EditorGUI.BeginChangeCheck();
                _saveCredentials = EditorGUILayout.Toggle(
                    new GUIContent("记住凭据", "加密保存账号密码到本地"),
                    _saveCredentials);
                if (EditorGUI.EndChangeCheck())
                {
                    PublishService.SetSaveCredentials(_saveCredentials);
                    if (!_saveCredentials)
                    {
                        PublishService.ClearCredentials();
                    }
                }

                EditorGUILayout.HelpBox(
                    "Unity 6.3+ 要求包必须有签名才能安装。\n" +
                    "Organization ID 可在 Unity Cloud Dashboard 获取。",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                var canSign = _packageData != null &&
                              !string.IsNullOrEmpty(_unityUsername) &&
                              !string.IsNullOrEmpty(_unityPassword) &&
                              !string.IsNullOrEmpty(_cloudOrgId);

                GUI.enabled = canSign;
                if (GUILayout.Button(new GUIContent("签名打包 (.tgz)", "使用 Unity 打包带签名的 tgz 文件"), GUILayout.Height(25)))
                {
                    SaveCredentialsIfEnabled();
                    PackWithSignature();
                }

                GUI.enabled = canSign && PublishService.IsNpmAvailable();
                if (GUILayout.Button(new GUIContent("签名打包并发布", "打包签名后发布到 Registry"), GUILayout.Height(25)))
                {
                    SaveCredentialsIfEnabled();
                    PackAndPublishWithSignature();
                }
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void SaveCredentialsIfEnabled()
        {
            if (_saveCredentials)
            {
                PublishService.SetUnityUsername(_unityUsername);
                PublishService.SetUnityPassword(_unityPassword);
                PublishService.SetCloudOrgId(_cloudOrgId);
            }
        }

        private void DrawRegistrySettings()
        {
            EditorGUILayout.LabelField("Registry 设置", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Registry type selection
            EditorGUI.BeginChangeCheck();
            _registryType = (PublishService.RegistryType)EditorGUILayout.EnumPopup(
                new GUIContent("Registry 类型", "选择发布目标"),
                _registryType);
            if (EditorGUI.EndChangeCheck())
            {
                PublishService.SetRegistryType(_registryType);
                switch (_registryType)
                {
                    case PublishService.RegistryType.NpmOfficial:
                        _registry = PublishService.NpmOfficialRegistry;
                        break;
                    case PublishService.RegistryType.Verdaccio:
                        _registry = "http://localhost:4873";
                        break;
                }
                PublishService.SetRegistry(_registry);
                _lastLoginCheckTime = 0;
                CheckNpmLoginAsync();
            }

            // Custom URL input
            GUI.enabled = _registryType == PublishService.RegistryType.Custom;
            EditorGUI.BeginChangeCheck();
            _registry = EditorGUILayout.TextField(
                new GUIContent("Registry URL", "npm registry 地址"),
                _registry);
            if (EditorGUI.EndChangeCheck())
            {
                PublishService.SetRegistry(_registry);
                _lastLoginCheckTime = 0;
                CheckNpmLoginAsync();
            }
            GUI.enabled = true;

            // npm login status
            EditorGUILayout.Space(5);
            if (!PublishService.IsNpmAvailable())
            {
                EditorGUILayout.HelpBox("未找到 npm，请安装 Node.js 并重启 Unity", MessageType.Error);
            }
            else if (_npmLoggedIn)
            {
                EditorGUILayout.HelpBox($"已登录: {_npmUsername}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("未登录 npm，发布前请先登录", MessageType.Warning);
                if (GUILayout.Button("刷新登录状态"))
                {
                    _lastLoginCheckTime = 0;
                    CheckNpmLoginAsync();
                }
                EditorGUILayout.HelpBox($"登录命令: npm login --registry {_registry}", MessageType.None);
            }

            // npm official registry tips
            if (PublishService.IsNpmOfficialRegistry(_registry))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(
                    "npm 官方 registry:\n" +
                    "• 发布时会自动添加 --access public\n" +
                    "• 首次发布需要先 npm login",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Pack without signature
            GUI.enabled = _packageData != null && PublishService.IsNpmAvailable();
            if (GUILayout.Button(new GUIContent("打包 (.tgz)", "使用 npm pack 打包（无签名）"), GUILayout.Height(25)))
            {
                PackWithoutSignature();
            }

            // Pack and publish without signature
            if (GUILayout.Button(new GUIContent("打包并发布", "打包后发布到 Registry（无签名）"), GUILayout.Height(25)))
            {
                PackAndPublishWithoutSignature();
            }

            EditorGUILayout.Space(5);

            // Publish existing tgz
            GUI.enabled = PublishService.IsNpmAvailable();
            if (GUILayout.Button(new GUIContent("发布 .tgz 文件", "选择已打包的 tgz 文件发布到 Registry"), GUILayout.Height(25)))
            {
                PublishExistingTgz();
            }

            // Unpublish
            GUI.enabled = PublishService.IsNpmAvailable() && _packageData != null;
            if (GUILayout.Button(new GUIContent("撤销发布此版本", "从 registry 删除此版本"), GUILayout.Height(25)))
            {
                UnpublishPackage();
            }

            GUI.enabled = true;

            EditorGUILayout.Space(5);

            // Copy config
            if (GUILayout.Button(new GUIContent("复制 Registry 配置", "复制 scopedRegistries 配置到剪贴板"), GUILayout.Height(25)))
            {
                CopyRegistryConfig();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHelp()
        {
            EditorGUILayout.HelpBox(
                "使用说明:\n" +
                "• npm 发布: npm adduser --registry <url>\n" +
                "• Unity 签名: 需要 Unity 6.3+ 和 Organization ID\n" +
                "• 发布前请确保 package.json 中的版本号已更新",
                MessageType.Info);
        }

        private void PackWithoutSignature()
        {
            var outputPath = EditorUtility.SaveFolderPanel("选择输出目录", _packagePath, "");
            if (string.IsNullOrEmpty(outputPath)) return;

            EditorUtility.DisplayProgressBar("打包", "正在打包...", 0.5f);
            var result = PublishService.PackWithoutSignature(_packagePath, outputPath);
            EditorUtility.ClearProgressBar();

            if (result.Success)
            {
                EditorUtility.DisplayDialog("成功", $"包已生成:\n{result.TgzPath}", "确定");
                EditorUtility.RevealInFinder(result.TgzPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", result.ErrorMessage, "确定");
            }
        }

        private void PackAndPublishWithoutSignature()
        {
            if (!EditorUtility.DisplayDialog("发布确认",
                $"打包并发布 {_packageData.name}@{_packageData.version} 到:\n{_registry}\n\n注意: 无签名包在 Unity 6.3+ 可能无法安装",
                "发布", "取消"))
            {
                return;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "UPMEditor_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tempDir);

            EditorUtility.DisplayProgressBar("打包", "正在打包...", 0.3f);

            try
            {
                var packResult = PublishService.PackWithoutSignature(_packagePath, tempDir);
                if (!packResult.Success)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("打包失败", packResult.ErrorMessage, "确定");
                    return;
                }

                EditorUtility.DisplayProgressBar("发布", "正在发布到 Registry...", 0.7f);
                var publishResult = PublishService.PublishTgz(packResult.TgzPath, _registry);
                EditorUtility.ClearProgressBar();

                if (publishResult.Success)
                {
                    EditorUtility.DisplayDialog("成功", $"包已发布到 {_registry}", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("发布失败", publishResult.ErrorMessage, "确定");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        private void PackWithSignature()
        {
            var outputPath = EditorUtility.SaveFolderPanel("选择输出目录", _packagePath, "");
            if (string.IsNullOrEmpty(outputPath)) return;

            EditorUtility.DisplayProgressBar("打包签名包", "正在调用 Unity 打包...", 0.5f);

            try
            {
                var result = PublishService.PackWithSignature(_packagePath, outputPath, _unityUsername, _unityPassword, _cloudOrgId);
                EditorUtility.ClearProgressBar();

                if (result.Success)
                {
                    EditorUtility.DisplayDialog("成功", $"签名包已生成:\n{result.TgzPath}", "确定");
                    EditorUtility.RevealInFinder(result.TgzPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", result.ErrorMessage, "确定");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void PackAndPublishWithSignature()
        {
            if (!EditorUtility.DisplayDialog("发布确认",
                $"打包签名并发布 {_packageData.name}@{_packageData.version} 到:\n{_registry}",
                "发布", "取消"))
            {
                return;
            }

            // 使用临时目录
            var tempDir = Path.Combine(Path.GetTempPath(), "UPMEditor_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tempDir);

            EditorUtility.DisplayProgressBar("打包签名包", "正在调用 Unity 打包...", 0.3f);

            try
            {
                var packResult = PublishService.PackWithSignature(_packagePath, tempDir, _unityUsername, _unityPassword, _cloudOrgId);

                if (!packResult.Success)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("打包失败", packResult.ErrorMessage, "确定");
                    return;
                }

                EditorUtility.DisplayProgressBar("发布", "正在发布到 Registry...", 0.7f);

                var publishResult = PublishService.PublishTgz(packResult.TgzPath, _registry);

                EditorUtility.ClearProgressBar();

                if (publishResult.Success)
                {
                    EditorUtility.DisplayDialog("成功", $"签名包已发布到 {_registry}", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("发布失败", publishResult.ErrorMessage, "确定");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // 清理临时目录
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        private void PublishExistingTgz()
        {
            var tgzPath = EditorUtility.OpenFilePanel("选择 .tgz 文件", "", "tgz");
            if (string.IsNullOrEmpty(tgzPath)) return;

            if (!EditorUtility.DisplayDialog("发布确认",
                $"发布 {Path.GetFileName(tgzPath)} 到:\n{_registry}",
                "发布", "取消"))
            {
                return;
            }

            EditorUtility.DisplayProgressBar("发布", "正在发布到 Registry...", 0.5f);
            var result = PublishService.PublishTgz(tgzPath, _registry);
            EditorUtility.ClearProgressBar();

            if (result.Success)
            {
                EditorUtility.DisplayDialog("成功", $"包已发布到 {_registry}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("发布失败", result.ErrorMessage, "确定");
            }
        }

        private void UnpublishPackage()
        {
            if (!EditorUtility.DisplayDialog("撤销发布确认",
                $"从 {_registry} 删除:\n{_packageData.name}@{_packageData.version}\n\n此操作不可撤销!",
                "撤销发布", "取消"))
            {
                return;
            }

            var result = PublishService.Unpublish(_packageData.name, _packageData.version, _registry);
            if (result.Success)
            {
                EditorUtility.DisplayDialog("成功", "包已撤销发布", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", result.ErrorMessage, "确定");
            }
        }

        private void CopyRegistryConfig()
        {
            var config = PublishService.GenerateScopedRegistryConfig(_packageData.name, _registry);
            GUIUtility.systemCopyBuffer = config;
            Debug.Log("Registry 配置已复制:\n" + config);
            EditorUtility.DisplayDialog("已复制", "scopedRegistries 配置已复制到剪贴板\n请添加到 Packages/manifest.json", "确定");
        }

        private string ConvertToRelativePath(string absolutePath)
        {
            var dataPath = Application.dataPath.Replace("\\", "/");
            var projectPath = Path.GetDirectoryName(dataPath).Replace("\\", "/");
            absolutePath = absolutePath.Replace("\\", "/");
            if (absolutePath.StartsWith(projectPath))
            {
                return absolutePath.Substring(projectPath.Length + 1);
            }
            return absolutePath;
        }
    }
}
#endif
