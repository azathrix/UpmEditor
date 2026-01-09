#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Azathrix.UpmEditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using PackageInfo = Azathrix.UpmEditor.Editor.Core.PackageInfo;

namespace Azathrix.UpmEditor.Editor.UI.Views
{
    /// <summary>
    /// Sort column for table view
    /// </summary>
    public enum TableSortColumn
    {
        Name,
        Version,
        DependencyCount,
        DependentCount
    }

    /// <summary>
    /// Table view with sortable columns
    /// </summary>
    public class PackageTableView
    {
        private TableSortColumn _sortColumn = TableSortColumn.Name;
        private bool _sortAscending = true;

        public void Draw(PackageCache cache, ref PackageInfo selectedPackage)
        {
            if (cache == null) return;

            // Header row
            DrawHeader();

            // Sort packages
            var packages = new List<PackageInfo>(cache.Packages);
            SortPackages(packages, cache);

            // Draw rows
            foreach (var pkg in packages)
            {
                DrawRow(cache, pkg, ref selectedPackage);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Select all checkbox - 与行的Toggle对齐
            GUILayout.Label("", GUILayout.Width(18));

            // Name column
            if (GUILayout.Button(GetHeaderText("包名", TableSortColumn.Name), EditorStyles.toolbarButton, GUILayout.Width(150)))
            {
                ToggleSort(TableSortColumn.Name);
            }

            // Version column
            if (GUILayout.Button(GetHeaderText("版本", TableSortColumn.Version), EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ToggleSort(TableSortColumn.Version);
            }

            // Dependency count column
            if (GUILayout.Button(GetHeaderText("依赖数", TableSortColumn.DependencyCount), EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ToggleSort(TableSortColumn.DependencyCount);
            }

            // Dependent count column
            if (GUILayout.Button(GetHeaderText("被依赖", TableSortColumn.DependentCount), EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ToggleSort(TableSortColumn.DependentCount);
            }

            // Status column
            GUILayout.Label("状态", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private string GetHeaderText(string text, TableSortColumn column)
        {
            if (_sortColumn == column)
            {
                return text + (_sortAscending ? " ▲" : " ▼");
            }
            return text;
        }

        private void ToggleSort(TableSortColumn column)
        {
            if (_sortColumn == column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = column;
                _sortAscending = true;
            }
        }

        private void SortPackages(List<PackageInfo> packages, PackageCache cache)
        {
            packages.Sort((a, b) =>
            {
                int result = _sortColumn switch
                {
                    TableSortColumn.Name => string.Compare(a.Name, b.Name, StringComparison.Ordinal),
                    TableSortColumn.Version => CompareVersions(a.Version, b.Version),
                    TableSortColumn.DependencyCount => GetDependencyCount(a).CompareTo(GetDependencyCount(b)),
                    TableSortColumn.DependentCount => GetDependentCount(cache, a).CompareTo(GetDependentCount(cache, b)),
                    _ => 0
                };
                return _sortAscending ? result : -result;
            });
        }

        private int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');
            var len = Math.Max(parts1.Length, parts2.Length);

            for (int i = 0; i < len; i++)
            {
                var p1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                var p2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;
                if (p1 != p2)
                    return p1.CompareTo(p2);
            }
            return 0;
        }

        private int GetDependencyCount(PackageInfo pkg)
        {
            return pkg.Data?.dependencies?.Count ?? 0;
        }

        private int GetDependentCount(PackageCache cache, PackageInfo pkg)
        {
            return cache.GetDependents(pkg.Name).Count;
        }

        private void DrawRow(PackageCache cache, PackageInfo pkg, ref PackageInfo selectedPackage)
        {
            var isSelected = pkg == selectedPackage;
            var bgColor = isSelected ? new Color(0.24f, 0.37f, 0.59f) : Color.clear;

            var rect = EditorGUILayout.BeginHorizontal();
            if (isSelected)
                EditorGUI.DrawRect(rect, bgColor);

            // Checkbox
            pkg.IsSelected = EditorGUILayout.Toggle(pkg.IsSelected, GUILayout.Width(18));

            // Name (clickable)
            var style = new GUIStyle(EditorStyles.label);
            if (isSelected)
                style.normal.textColor = Color.white;

            if (GUILayout.Button(pkg.DisplayName, style, GUILayout.Width(150)))
            {
                selectedPackage = pkg;
            }

            // Version
            GUILayout.Label(pkg.Version, GUILayout.Width(80));

            // Dependency count
            GUILayout.Label(GetDependencyCount(pkg).ToString(), GUILayout.Width(60));

            // Dependent count
            GUILayout.Label(GetDependentCount(cache, pkg).ToString(), GUILayout.Width(60));

            // Status
            var status = pkg.IsDirty ? "已修改" : "";
            GUILayout.Label(status, GUILayout.Width(50));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
