#if UNITY_EDITOR
using System.Collections.Generic;
using Azathrix.UpmEditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using PackageInfo = Azathrix.UpmEditor.Editor.Core.PackageInfo;

namespace Azathrix.UpmEditor.Editor.UI.Views
{
    /// <summary>
    /// Dependency tree view showing package relationships
    /// </summary>
    public class DependencyTreeView
    {
        private Dictionary<string, bool> _packageFoldouts = new Dictionary<string, bool>();

        private static readonly Color ItemBgColor = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color ItemAltBgColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color DepBgColor = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color SelectedBgColor = new Color(0.24f, 0.37f, 0.59f);
        private static readonly Color HoverBgColor = new Color(0.28f, 0.28f, 0.28f);

        private int _rowIndex;

        public void Draw(PackageCache cache, ref PackageInfo selectedPackage)
        {
            if (cache == null) return;

            // Sort packages by name
            var packages = new List<PackageInfo>(cache.Packages);
            packages.Sort((a, b) => string.Compare(a.Name, b.Name));

            _rowIndex = 0;
            bool clickedOnItem = false;
            foreach (var pkg in packages)
            {
                if (DrawPackageNode(cache, pkg, ref selectedPackage, 0))
                    clickedOnItem = true;
            }

            // 点击空白区域清除选中
            if (Event.current.type == EventType.MouseDown && !clickedOnItem)
            {
                selectedPackage = null;
                GUI.changed = true;
            }
        }

        private bool DrawPackageNode(PackageCache cache, PackageInfo pkg, ref PackageInfo selectedPackage, int depth)
        {
            if (pkg?.Data == null) return false;

            var name = pkg.Name;
            var deps = cache.GetDependencies(name);
            var hasDeps = deps.Count > 0;

            // Ensure foldout state exists
            if (!_packageFoldouts.ContainsKey(name))
                _packageFoldouts[name] = false;

            var isSelected = pkg == selectedPackage;
            _rowIndex++;

            // Draw row
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

            // Hover检测
            var isHover = rect.Contains(Event.current.mousePosition);
            if (isHover && Event.current.type == EventType.Repaint)
                EditorWindow.focusedWindow?.Repaint();

            var bgColor = isSelected ? SelectedBgColor : (isHover ? HoverBgColor : (_rowIndex % 2 == 0 ? ItemBgColor : ItemAltBgColor));
            EditorGUI.DrawRect(rect, bgColor);

            bool clicked = false;
            // 整行点击检测（排除checkbox和foldout区域）
            var foldoutEnd = rect.x + 10 + depth * 20 + 18 + (hasDeps ? 18 : 0);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                clicked = true;
                if (Event.current.mousePosition.x > foldoutEnd)
                {
                    selectedPackage = pkg;
                    Event.current.Use();
                    GUI.changed = true;
                }
            }

            // Indent
            GUILayout.Space(10 + depth * 20);

            // Checkbox
            pkg.IsSelected = EditorGUILayout.Toggle(pkg.IsSelected, GUILayout.Width(18));

            // Foldout or space
            if (hasDeps)
            {
                var foldoutRect = GUILayoutUtility.GetRect(18, 20, GUILayout.Width(18));
                // 手动处理foldout点击
                if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
                {
                    _packageFoldouts[name] = !_packageFoldouts[name];
                    Event.current.Use();
                }
                EditorGUI.Foldout(foldoutRect, _packageFoldouts[name], GUIContent.none);
            }
            else
            {
                GUILayout.Space(18);
            }

            // Package name
            var displayText = pkg.DisplayName;
            if (pkg.IsDirty)
                displayText += " *";

            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            GUILayout.Label(displayText, style);

            // Version
            GUILayout.FlexibleSpace();
            GUILayout.Label($"({pkg.Version})", EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            // Draw dependencies if expanded
            if (hasDeps && _packageFoldouts[name])
            {
                EditorGUI.indentLevel++;
                foreach (var dep in deps)
                {
                    if (DrawDependencyNode(cache, pkg, dep, ref selectedPackage, depth + 1))
                        clicked = true;
                }
                EditorGUI.indentLevel--;
            }

            return clicked;
        }

        private bool DrawDependencyNode(PackageCache cache, PackageInfo parent, PackageInfo dep, ref PackageInfo selectedPackage, int depth)
        {
            if (dep?.Data == null) return false;

            var isSelected = dep == selectedPackage;

            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

            // Hover检测
            var isHover = rect.Contains(Event.current.mousePosition);
            if (isHover && Event.current.type == EventType.Repaint)
                EditorWindow.focusedWindow?.Repaint();

            var bgColor = isSelected ? SelectedBgColor : (isHover ? HoverBgColor : DepBgColor);
            EditorGUI.DrawRect(rect, bgColor);

            bool clicked = false;
            // 整行点击检测
            var clickStart = rect.x + 10 + depth * 20 + 20;
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                clicked = true;
                if (Event.current.mousePosition.x > clickStart)
                {
                    selectedPackage = dep;
                    Event.current.Use();
                    GUI.changed = true;
                }
            }

            // Indent
            GUILayout.Space(10 + depth * 20);

            // Tree line indicator
            GUILayout.Label("└─", EditorStyles.miniLabel, GUILayout.Width(20));

            // Package name
            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            GUILayout.Label(dep.DisplayName, style);

            // Version from parent's dependency
            GUILayout.FlexibleSpace();
            var depVersion = dep.Version;
            if (parent.Data.dependencies != null && parent.Data.dependencies.TryGetValue(dep.Name, out var v))
                depVersion = v;
            GUILayout.Label($"({depVersion})", EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
            return clicked;
        }
    }
}
#endif
