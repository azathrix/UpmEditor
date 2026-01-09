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

        public void Draw(PackageCache cache, ref PackageInfo selectedPackage)
        {
            if (cache == null) return;

            var scopes = PackageScanService.GetSortedScopes(cache);

            foreach (var scope in scopes)
            {
                if (!cache.ScopeGroups.TryGetValue(scope, out var packages))
                    continue;

                // Ensure foldout state exists
                if (!_scopeFoldouts.ContainsKey(scope))
                    _scopeFoldouts[scope] = true;

                // Draw scope header with checkbox for select all
                EditorGUILayout.BeginHorizontal();

                // Select all checkbox
                var allSelected = IsAllSelected(packages);
                var newAllSelected = EditorGUILayout.Toggle(allSelected, GUILayout.Width(18));
                if (newAllSelected != allSelected)
                {
                    foreach (var pkg in packages)
                        pkg.IsSelected = newAllSelected;
                }

                // Foldout
                _scopeFoldouts[scope] = EditorGUILayout.Foldout(_scopeFoldouts[scope], scope, true, EditorStyles.foldoutHeader);

                EditorGUILayout.EndHorizontal();

                // Draw packages if expanded
                if (_scopeFoldouts[scope])
                {
                    foreach (var pkg in packages)
                    {
                        DrawPackageItem(pkg, ref selectedPackage);
                    }
                }
            }
        }

        private void DrawPackageItem(PackageInfo pkg, ref PackageInfo selectedPackage)
        {
            var isSelected = pkg == selectedPackage;
            var bgColor = isSelected ? new Color(0.24f, 0.37f, 0.59f) : Color.clear;

            var rect = EditorGUILayout.BeginHorizontal();
            if (isSelected)
                EditorGUI.DrawRect(rect, bgColor);

            // Indent
            GUILayout.Space(20);

            // Checkbox
            pkg.IsSelected = EditorGUILayout.Toggle(pkg.IsSelected, GUILayout.Width(18));

            // Package name (clickable)
            var displayName = pkg.DisplayName;
            if (pkg.IsDirty)
                displayName += " *";

            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            if (GUILayout.Button(displayName, style))
            {
                selectedPackage = pkg;
            }

            // Version
            GUILayout.FlexibleSpace();
            GUILayout.Label(pkg.Version, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
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
