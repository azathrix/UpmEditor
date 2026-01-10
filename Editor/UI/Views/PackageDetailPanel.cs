#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Azathrix.UpmEditor.Editor.Core;
using Azathrix.UpmEditor.Editor.Services;
using UnityEditor;
using UnityEngine;
using PackageInfo = Azathrix.UpmEditor.Editor.Core.PackageInfo;

namespace Azathrix.UpmEditor.Editor.UI.Views
{
    /// <summary>
    /// Detail panel for editing selected package
    /// </summary>
    public class PackageDetailPanel
    {
        private bool _foldBasicInfo = true;
        private bool _foldAuthor = true;
        private bool _foldKeywords = true;
        private bool _foldDependencies = true;
        private bool _foldReverseDeps = true;
        private bool _foldChangelog = true;

        private string _newDepName = "";
        private string _newDepVersion = "1.0.0";
        private string _newKeyword = "";

        // Version update dialog
        private bool _showVersionUpdateDialog;
        private string _oldVersion;
        private string _newVersion;
        private List<PackageInfo> _dependentsToUpdate = new List<PackageInfo>();
        private Dictionary<PackageInfo, bool> _dependentSelection = new Dictionary<PackageInfo, bool>();

        // Track original version for save comparison
        private string _lastSelectedPackage;
        private string _originalVersion;

        public void Draw(PackageCache cache, PackageInfo pkg)
        {
            if (pkg?.Data == null) return;

            // Track package selection change to store original version
            if (_lastSelectedPackage != pkg.Name)
            {
                _lastSelectedPackage = pkg.Name;
                _originalVersion = pkg.Data.version;
                _showVersionUpdateDialog = false;
            }

            // Version update dialog
            if (_showVersionUpdateDialog)
            {
                DrawVersionUpdateDialog(cache, pkg);
                return;
            }

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(pkg.DisplayName, EditorStyles.boldLabel);
            if (pkg.IsDirty)
            {
                GUILayout.Label("*", GUILayout.Width(15));
            }
            if (GUILayout.Button("发布", GUILayout.Width(50)))
            {
                UPMPublishWindow.ShowWindow(pkg.Path);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            DrawBasicInfo(cache, pkg);
            DrawAuthor(pkg);
            DrawKeywords(pkg);
            DrawDependencies(pkg);
            DrawReverseDependencies(cache, pkg);
            DrawChangelog(pkg);

            EditorGUILayout.Space(10);

            // Save button
            GUI.enabled = pkg.IsDirty;
            if (GUILayout.Button("保存", GUILayout.Height(25)))
            {
                SavePackage(cache, pkg);
            }
            GUI.enabled = true;
        }

        private void DrawBasicInfo(PackageCache cache, PackageInfo pkg)
        {
            _foldBasicInfo = EditorGUILayout.BeginFoldoutHeaderGroup(_foldBasicInfo, "基本信息");
            if (_foldBasicInfo)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Name (read-only)
                EditorGUILayout.LabelField("包名", pkg.Name);

                // Display name
                EditorGUI.BeginChangeCheck();
                pkg.Data.displayName = EditorGUILayout.TextField("显示名称", pkg.Data.displayName);
                if (EditorGUI.EndChangeCheck())
                    pkg.MarkDirty();

                // Version with +/- buttons
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("版本");
                EditorGUI.BeginChangeCheck();
                var newVersion = EditorGUILayout.TextField(pkg.Data.version);
                if (EditorGUI.EndChangeCheck() && newVersion != pkg.Data.version)
                {
                    OnVersionChanged(cache, pkg, pkg.Data.version, newVersion);
                }

                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    var incremented = IncrementVersion(pkg.Data.version, 2); // patch
                    OnVersionChanged(cache, pkg, pkg.Data.version, incremented);
                }
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    var decremented = DecrementVersion(pkg.Data.version, 2); // patch
                    if (decremented != pkg.Data.version)
                    {
                        OnVersionChanged(cache, pkg, pkg.Data.version, decremented);
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Unity version
                EditorGUI.BeginChangeCheck();
                pkg.Data.unity = EditorGUILayout.TextField("Unity 版本", pkg.Data.unity);
                if (EditorGUI.EndChangeCheck())
                    pkg.MarkDirty();

                // License
                EditorGUI.BeginChangeCheck();
                pkg.Data.license = EditorGUILayout.TextField("许可证", pkg.Data.license);
                if (EditorGUI.EndChangeCheck())
                    pkg.MarkDirty();

                // Visibility
                EditorGUI.BeginChangeCheck();
                var visibility = pkg.Data.hideInEditor ? DefaultVisibility.Hidden : DefaultVisibility.Visible;
                visibility = (DefaultVisibility)EditorGUILayout.EnumPopup("默认显示", visibility);
                if (EditorGUI.EndChangeCheck())
                {
                    pkg.Data.hideInEditor = visibility == DefaultVisibility.Hidden;
                    pkg.MarkDirty();
                }

                // Description
                EditorGUILayout.LabelField("描述");
                EditorGUI.BeginChangeCheck();
                pkg.Data.description = EditorGUILayout.TextArea(pkg.Data.description, GUILayout.MinHeight(40));
                if (EditorGUI.EndChangeCheck())
                    pkg.MarkDirty();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAuthor(PackageInfo pkg)
        {
            _foldAuthor = EditorGUILayout.BeginFoldoutHeaderGroup(_foldAuthor, "作者信息");
            if (_foldAuthor)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (pkg.Data.author == null)
                    pkg.Data.author = new UPMPackageData.AuthorInfo();

                EditorGUI.BeginChangeCheck();
                pkg.Data.author.name = EditorGUILayout.TextField("姓名", pkg.Data.author.name);
                pkg.Data.author.email = EditorGUILayout.TextField("邮箱", pkg.Data.author.email);
                pkg.Data.author.url = EditorGUILayout.TextField("网址", pkg.Data.author.url);
                if (EditorGUI.EndChangeCheck())
                    pkg.MarkDirty();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawKeywords(PackageInfo pkg)
        {
            if (pkg.Data.keywords == null)
                pkg.Data.keywords = new System.Collections.Generic.List<string>();

            _foldKeywords = EditorGUILayout.BeginFoldoutHeaderGroup(_foldKeywords,
                $"关键词 ({pkg.Data.keywords.Count})");
            if (_foldKeywords)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Show keywords as removable buttons
                EditorGUILayout.BeginHorizontal();
                var toRemoveIdx = -1;
                for (int i = 0; i < pkg.Data.keywords.Count; i++)
                {
                    if (GUILayout.Button($"{pkg.Data.keywords[i]} ×", EditorStyles.miniButton))
                        toRemoveIdx = i;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                if (toRemoveIdx >= 0)
                {
                    pkg.Data.keywords.RemoveAt(toRemoveIdx);
                    pkg.MarkDirty();
                }

                // Add new keyword
                EditorGUILayout.BeginHorizontal();
                _newKeyword = EditorGUILayout.TextField(_newKeyword);
                GUI.enabled = !string.IsNullOrEmpty(_newKeyword) && !pkg.Data.keywords.Contains(_newKeyword);
                if (GUILayout.Button("添加", GUILayout.Width(40)))
                {
                    pkg.Data.keywords.Add(_newKeyword);
                    _newKeyword = "";
                    pkg.MarkDirty();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawDependencies(PackageInfo pkg)
        {
            _foldDependencies = EditorGUILayout.BeginFoldoutHeaderGroup(_foldDependencies,
                $"依赖项 ({pkg.Data.dependencies?.Count ?? 0})");
            if (_foldDependencies)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var toRemove = new List<string>();
                var toUpdate = new List<(string oldKey, string newKey, string newVersion)>();

                if (pkg.Data.dependencies != null)
                {
                    foreach (var dep in pkg.Data.dependencies)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var newKey = EditorGUILayout.TextField(dep.Key);
                        var newVer = EditorGUILayout.TextField(dep.Value, GUILayout.Width(80));
                        if (newKey != dep.Key || newVer != dep.Value)
                            toUpdate.Add((dep.Key, newKey, newVer));
                        if (GUILayout.Button("×", GUILayout.Width(20)))
                            toRemove.Add(dep.Key);
                        EditorGUILayout.EndHorizontal();
                    }
                }

                // Apply changes
                foreach (var key in toRemove)
                {
                    pkg.Data.dependencies.Remove(key);
                    pkg.MarkDirty();
                }
                foreach (var (oldKey, newKey, newVer) in toUpdate)
                {
                    pkg.Data.dependencies.Remove(oldKey);
                    if (!string.IsNullOrEmpty(newKey))
                        pkg.Data.dependencies[newKey] = newVer;
                    pkg.MarkDirty();
                }

                // Add new dependency
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                _newDepName = EditorGUILayout.TextField(_newDepName);
                _newDepVersion = EditorGUILayout.TextField(_newDepVersion, GUILayout.Width(80));
                GUI.enabled = !string.IsNullOrEmpty(_newDepName) &&
                              (pkg.Data.dependencies == null || !pkg.Data.dependencies.ContainsKey(_newDepName));
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    if (pkg.Data.dependencies == null)
                        pkg.Data.dependencies = new Dictionary<string, string>();
                    pkg.Data.dependencies[_newDepName] = _newDepVersion;
                    _newDepName = "";
                    pkg.MarkDirty();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawReverseDependencies(PackageCache cache, PackageInfo pkg)
        {
            var dependents = cache.GetDependents(pkg.Name);

            _foldReverseDeps = EditorGUILayout.BeginFoldoutHeaderGroup(_foldReverseDeps,
                $"反向依赖 ({dependents.Count})");
            if (_foldReverseDeps)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (dependents.Count == 0)
                {
                    EditorGUILayout.LabelField("没有其他包依赖此包", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var dep in dependents)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {dep.DisplayName}");
                        var depVersion = "";
                        if (dep.Data.dependencies != null && dep.Data.dependencies.TryGetValue(pkg.Name, out var v))
                            depVersion = v;
                        EditorGUILayout.LabelField(depVersion, GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawChangelog(PackageInfo pkg)
        {
            _foldChangelog = EditorGUILayout.BeginFoldoutHeaderGroup(_foldChangelog, "更新日志");
            if (_foldChangelog)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (pkg.Changelog == null)
                {
                    EditorGUILayout.LabelField("没有 CHANGELOG.md 文件", EditorStyles.miniLabel);
                    if (GUILayout.Button("创建 CHANGELOG.md"))
                    {
                        pkg.Changelog = new ChangelogData();
                        pkg.Changelog.AddVersion(pkg.Version);
                        ChangelogService.SaveChangelog(pkg);
                    }
                }
                else
                {
                    // Add version button
                    if (GUILayout.Button("+ 添加版本"))
                    {
                        pkg.Changelog.AddVersion(pkg.Version);
                    }

                    EditorGUILayout.Space(5);

                    // Show versions
                    foreach (var version in pkg.Changelog.Versions)
                    {
                        DrawChangelogVersion(pkg, version);
                    }

                    EditorGUILayout.Space(5);
                    if (GUILayout.Button("保存更新日志"))
                    {
                        ChangelogService.SaveChangelog(pkg);
                        EditorUtility.DisplayDialog("成功", "更新日志已保存", "确定");
                    }
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawChangelogVersion(PackageInfo pkg, ChangelogVersion version)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Version header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{version.Version}]", EditorStyles.boldLabel, GUILayout.Width(100));
            version.Date = EditorGUILayout.TextField(version.Date, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                pkg.Changelog.Versions.Remove(version);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            // Entries
            var toRemove = new List<ChangelogEntry>();
            foreach (var entry in version.Entries)
            {
                EditorGUILayout.BeginHorizontal();
                entry.Category = (ChangelogCategory)EditorGUILayout.EnumPopup(entry.Category, GUILayout.Width(80));
                entry.Description = EditorGUILayout.TextField(entry.Description);
                if (GUILayout.Button("×", GUILayout.Width(20)))
                    toRemove.Add(entry);
                EditorGUILayout.EndHorizontal();
            }
            foreach (var e in toRemove)
                version.Entries.Remove(e);

            // Add entry button
            if (GUILayout.Button("+ 添加条目", EditorStyles.miniButton))
            {
                version.Entries.Add(new ChangelogEntry(ChangelogCategory.Added, ""));
            }

            EditorGUILayout.EndVertical();
        }

        private void OnVersionChanged(PackageCache cache, PackageInfo pkg, string oldVer, string newVer)
        {
            pkg.Data.version = newVer;
            pkg.MarkDirty();
            // Dialog will be shown after save if version actually changed from original
        }

        private void DrawVersionUpdateDialog(PackageCache cache, PackageInfo pkg)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("版本更新影响", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"{pkg.Name} 版本从 {_oldVersion} 更新到 {_newVersion}");
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("以下包依赖此包，是否更新其依赖版本？");
            EditorGUILayout.Space(5);

            foreach (var dep in _dependentsToUpdate)
            {
                EditorGUILayout.BeginHorizontal();
                _dependentSelection[dep] = EditorGUILayout.Toggle(_dependentSelection[dep], GUILayout.Width(20));
                EditorGUILayout.LabelField(dep.DisplayName);
                var currentVer = "";
                if (dep.Data.dependencies != null && dep.Data.dependencies.TryGetValue(pkg.Name, out var cv))
                    currentVer = cv;
                EditorGUILayout.LabelField($"{currentVer} → {_newVersion}", GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("更新选中"))
            {
                var toUpdate = new List<PackageInfo>();
                foreach (var kvp in _dependentSelection)
                {
                    if (kvp.Value)
                        toUpdate.Add(kvp.Key);
                }
                PackageScanService.UpdateDependencyVersion(cache, pkg.Name, _newVersion, toUpdate);
                _showVersionUpdateDialog = false;
            }
            if (GUILayout.Button("跳过"))
            {
                _showVersionUpdateDialog = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void SavePackage(PackageCache cache, PackageInfo pkg)
        {
            var versionChanged = _originalVersion != pkg.Data.version;
            var newVersion = pkg.Data.version;

            if (PackageScanService.SavePackage(pkg))
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("成功", "保存成功", "确定");

                // Check if version changed and has dependents
                if (versionChanged)
                {
                    var dependents = cache.GetDependents(pkg.Name);
                    if (dependents.Count > 0)
                    {
                        _showVersionUpdateDialog = true;
                        _oldVersion = _originalVersion;
                        _newVersion = newVersion;
                        _dependentsToUpdate = dependents;
                        _dependentSelection.Clear();
                        foreach (var dep in dependents)
                            _dependentSelection[dep] = true;
                    }
                }

                // Update original version after save
                _originalVersion = newVersion;
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "保存失败", "确定");
            }
        }

        private string IncrementVersion(string version, int part)
        {
            var parts = version.Split('.');
            if (parts.Length < 3) return version;

            if (int.TryParse(parts[part], out var num))
            {
                parts[part] = (num + 1).ToString();
                // Reset lower parts
                for (int i = part + 1; i < parts.Length; i++)
                    parts[i] = "0";
            }
            return string.Join(".", parts);
        }

        private string DecrementVersion(string version, int part)
        {
            var parts = version.Split('.');
            if (parts.Length < 3) return version;

            if (int.TryParse(parts[part], out var num) && num > 0)
            {
                parts[part] = (num - 1).ToString();
            }
            return string.Join(".", parts);
        }
    }
}
#endif
