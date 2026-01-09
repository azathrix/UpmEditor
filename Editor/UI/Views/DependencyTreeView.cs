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

        public void Draw(PackageCache cache, ref PackageInfo selectedPackage)
        {
            if (cache == null) return;

            // Sort packages by name
            var packages = new List<PackageInfo>(cache.Packages);
            packages.Sort((a, b) => string.Compare(a.Name, b.Name));

            foreach (var pkg in packages)
            {
                DrawPackageNode(cache, pkg, ref selectedPackage, 0);
            }
        }

        private void DrawPackageNode(PackageCache cache, PackageInfo pkg, ref PackageInfo selectedPackage, int depth)
        {
            if (pkg?.Data == null) return;

            var name = pkg.Name;
            var deps = cache.GetDependencies(name);
            var hasDeps = deps.Count > 0;

            // Ensure foldout state exists
            if (!_packageFoldouts.ContainsKey(name))
                _packageFoldouts[name] = false;

            var isSelected = pkg == selectedPackage;
            var bgColor = isSelected ? new Color(0.24f, 0.37f, 0.59f) : Color.clear;

            // Draw row
            var rect = EditorGUILayout.BeginHorizontal();
            if (isSelected)
                EditorGUI.DrawRect(rect, bgColor);

            // Indent
            GUILayout.Space(depth * 20);

            // Checkbox
            pkg.IsSelected = EditorGUILayout.Toggle(pkg.IsSelected, GUILayout.Width(18));

            // Foldout or bullet
            if (hasDeps)
            {
                _packageFoldouts[name] = EditorGUILayout.Foldout(_packageFoldouts[name], "", true);
            }
            else
            {
                GUILayout.Space(18);
            }

            // Package name (clickable)
            var displayText = pkg.DisplayName;
            if (pkg.IsDirty)
                displayText += " *";

            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            if (GUILayout.Button(displayText, style))
            {
                selectedPackage = pkg;
            }

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
                    DrawDependencyNode(cache, pkg, dep, ref selectedPackage, depth + 1);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawDependencyNode(PackageCache cache, PackageInfo parent, PackageInfo dep, ref PackageInfo selectedPackage, int depth)
        {
            if (dep?.Data == null) return;

            var isSelected = dep == selectedPackage;
            var bgColor = isSelected ? new Color(0.24f, 0.37f, 0.59f) : Color.clear;

            var rect = EditorGUILayout.BeginHorizontal();
            if (isSelected)
                EditorGUI.DrawRect(rect, bgColor);

            // Indent
            GUILayout.Space(depth * 20);

            // Tree line indicator
            GUILayout.Label("└─", EditorStyles.miniLabel, GUILayout.Width(20));

            // Package name (clickable)
            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            if (GUILayout.Button(dep.DisplayName, style))
            {
                selectedPackage = dep;
            }

            // Version from parent's dependency
            GUILayout.FlexibleSpace();
            var depVersion = dep.Version;
            if (parent.Data.dependencies != null && parent.Data.dependencies.TryGetValue(dep.Name, out var v))
                depVersion = v;
            GUILayout.Label($"({depVersion})", EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
