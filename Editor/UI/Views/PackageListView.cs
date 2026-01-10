#if UNITY_EDITOR
using System.Collections.Generic;
using Azathrix.UpmEditor.Editor.Core;
using Azathrix.UpmEditor.Editor.Services;
using UnityEditor;
using UnityEngine;
using PackageInfo = Azathrix.UpmEditor.Editor.Core.PackageInfo;

namespace Azathrix.UpmEditor.Editor.UI.Views
{
    /// <summary>
    /// List view grouped by scope
    /// </summary>
    public class PackageListView
    {
        private Dictionary<string, bool> _scopeFoldouts = new Dictionary<string, bool>();

        private static readonly Color GroupBgColor = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color ItemBgColor = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color ItemAltBgColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color SelectedBgColor = new Color(0.24f, 0.37f, 0.59f);
        private static readonly Color HoverBgColor = new Color(0.28f, 0.28f, 0.28f);

        public void Draw(PackageCache cache, ref PackageInfo selectedPackage)
        {
            if (cache == null) return;

            var scopes = PackageScanService.GetSortedScopes(cache);
            var contentStartY = GUILayoutUtility.GetRect(0, 0).y;
            bool clickedOnItem = false;

            foreach (var scope in scopes)
            {
                if (!cache.ScopeGroups.TryGetValue(scope, out var packages))
                    continue;

                // Ensure foldout state exists
                if (!_scopeFoldouts.ContainsKey(scope))
                    _scopeFoldouts[scope] = true;

                // Draw scope header with checkbox for select all
                var headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
                EditorGUI.DrawRect(headerRect, GroupBgColor);

                // 整行点击检测（排除checkbox区域）
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.mousePosition.x > headerRect.x + 28)
                    {
                        _scopeFoldouts[scope] = !_scopeFoldouts[scope];
                        Event.current.Use();
                    }
                }

                // Select all checkbox
                GUILayout.Space(10);
                var allSelected = IsAllSelected(packages);
                var newAllSelected = EditorGUILayout.Toggle(allSelected, GUILayout.Width(18));
                if (newAllSelected != allSelected)
                {
                    foreach (var pkg in packages)
                        pkg.IsSelected = newAllSelected;
                }

                // Foldout
                _scopeFoldouts[scope] = EditorGUILayout.Foldout(_scopeFoldouts[scope], scope, true, EditorStyles.foldout);

                EditorGUILayout.EndHorizontal();

                // Draw packages if expanded
                if (_scopeFoldouts[scope])
                {
                    for (int i = 0; i < packages.Count; i++)
                    {
                        if (DrawPackageItem(packages[i], ref selectedPackage, i % 2 == 0))
                            clickedOnItem = true;
                    }
                }
            }

            // 点击空白区域清除选中
            if (Event.current.type == EventType.MouseDown && !clickedOnItem)
            {
                selectedPackage = null;
                GUI.changed = true;
            }
        }

        private bool DrawPackageItem(PackageInfo pkg, ref PackageInfo selectedPackage, bool altRow)
        {
            var isSelected = pkg == selectedPackage;
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

            // Hover检测
            var isHover = rect.Contains(Event.current.mousePosition);
            if (isHover && Event.current.type == EventType.Repaint)
                EditorWindow.focusedWindow?.Repaint();

            var bgColor = isSelected ? SelectedBgColor : (isHover ? HoverBgColor : (altRow ? ItemAltBgColor : ItemBgColor));
            EditorGUI.DrawRect(rect, bgColor);

            bool clicked = false;
            // 整行点击检测
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                clicked = true;
                // 排除 checkbox 区域 (前48像素: 30缩进 + 18宽度)
                if (Event.current.mousePosition.x > rect.x + 48)
                {
                    selectedPackage = pkg;
                    Event.current.Use();
                    GUI.changed = true;
                }
            }

            // Indent
            GUILayout.Space(30);

            // Checkbox
            pkg.IsSelected = EditorGUILayout.Toggle(pkg.IsSelected, GUILayout.Width(18));

            // Package name
            var displayName = pkg.DisplayName;
            if (pkg.IsDirty)
                displayName += " *";

            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            GUILayout.Label(displayName, style);

            // Version
            GUILayout.FlexibleSpace();
            GUILayout.Label(pkg.Version, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
            return clicked;
        }

        private bool IsAllSelected(List<PackageInfo> packages)
        {
            foreach (var pkg in packages)
            {
                if (!pkg.IsSelected)
                    return false;
            }
            return packages.Count > 0;
        }
    }
}
#endif
