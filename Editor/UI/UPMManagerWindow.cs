#if UNITY_EDITOR
using System.Collections.Generic;
using Azathrix.UpmEditor.Editor.Core;
using Azathrix.UpmEditor.Editor.Services;
using Azathrix.UpmEditor.Editor.UI.Views;
using UnityEditor;
using UnityEngine;
using PackageInfo = Azathrix.UpmEditor.Editor.Core.PackageInfo;

namespace Azathrix.UpmEditor.Editor.UI
{
    /// <summary>
    /// View mode for package list
    /// </summary>
    public enum ViewMode
    {
        List,
        DependencyTree,
        Table
    }

    /// <summary>
    /// UPM Package Manager Window
    /// </summary>
    public class UPMManagerWindow : EditorWindow
    {
        private PackageCache _cache;
        private ViewMode _viewMode = ViewMode.List;
        private PackageInfo _selectedPackage;
        private float _splitWidth = 300f;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private bool _isDraggingSplit;

        // Views
        private PackageListView _listView;
        private DependencyTreeView _treeView;
        private PackageTableView _tableView;
        private PackageDetailPanel _detailPanel;

        // Batch publish
        private bool _showBatchPublish;

        [MenuItem(UPMConstants.ManagerMenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<UPMManagerWindow>("包管理器");
            window.minSize = new Vector2(800, 500);
        }

        private void OnEnable()
        {
            _splitWidth = EditorPrefs.GetFloat(UPMConstants.PrefsManagerSplitWidth, 300f);
            _viewMode = (ViewMode)EditorPrefs.GetInt(UPMConstants.PrefsManagerViewMode, 0);
            RefreshPackages();
            InitViews();
        }

        private void OnDisable()
        {
            EditorPrefs.SetFloat(UPMConstants.PrefsManagerSplitWidth, _splitWidth);
            EditorPrefs.SetInt(UPMConstants.PrefsManagerViewMode, (int)_viewMode);
        }

        private void InitViews()
        {
            _listView = new PackageListView();
            _treeView = new DependencyTreeView();
            _tableView = new PackageTableView();
            _detailPanel = new PackageDetailPanel();
        }

        private void RefreshPackages()
        {
            _cache = PackageScanService.ScanPackages();
            _selectedPackage = null;
        }

        private void OnGUI()
        {
            DrawToolbar();

            var toolbarHeight = EditorStyles.toolbar.fixedHeight;
            var contentY = toolbarHeight;
            var contentHeight = position.height - toolbarHeight;

            // 左面板背景
            var leftRect = new Rect(0, contentY, _splitWidth, contentHeight);
            EditorGUI.DrawRect(leftRect, new Color(0.22f, 0.22f, 0.22f));

            // 分割线
            EditorGUI.DrawRect(new Rect(_splitWidth, contentY, 1, contentHeight), new Color(0.1f, 0.1f, 0.1f));

            // 右面板背景
            var rightRect = new Rect(_splitWidth + 1, contentY, position.width - _splitWidth - 1, contentHeight);
            EditorGUI.DrawRect(rightRect, new Color(0.18f, 0.18f, 0.18f));

            // 左面板内容
            GUILayout.BeginArea(leftRect);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
            switch (_viewMode)
            {
                case ViewMode.List:
                    _listView?.Draw(_cache, ref _selectedPackage);
                    break;
                case ViewMode.DependencyTree:
                    _treeView?.Draw(_cache, ref _selectedPackage);
                    break;
                case ViewMode.Table:
                    _tableView?.Draw(_cache, ref _selectedPackage);
                    break;
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // 右面板内容
            GUILayout.BeginArea(rightRect);
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            if (_showBatchPublish)
            {
                DrawBatchPublishPanel();
            }
            else if (_selectedPackage != null)
            {
                _detailPanel?.Draw(_cache, _selectedPackage);
            }
            else
            {
                EditorGUILayout.HelpBox("选择一个包查看详情", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // 分割线拖拽
            var splitterRect = new Rect(_splitWidth - 2, contentY, 5, contentHeight);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _isDraggingSplit = true;
                Event.current.Use();
            }

            HandleSplitterDrag();
        }

        private void HandleSplitterDrag()
        {
            if (_isDraggingSplit)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _splitWidth = Mathf.Clamp(Event.current.mousePosition.x, 200, position.width - 300);
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _isDraggingSplit = false;
                    EditorPrefs.SetFloat(UPMConstants.PrefsManagerSplitWidth, _splitWidth);
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshPackages();
            }

            if (GUILayout.Button("保存全部", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveAllDirty();
            }

            GUILayout.Space(20);

            GUILayout.Label("视图:", GUILayout.Width(35));

            EditorGUI.BeginChangeCheck();
            var newMode = (ViewMode)GUILayout.Toolbar((int)_viewMode, new[] { "列表", "依赖树", "表格" }, EditorStyles.toolbarButton, GUI.ToolbarButtonSize.Fixed, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                _viewMode = newMode;
            }

            GUILayout.FlexibleSpace();

            var selectedCount = _cache?.GetSelectedPackages().Count ?? 0;
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"批量发布 ({selectedCount})", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                _showBatchPublish = true;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBatchPublishPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("批量发布", EditorStyles.boldLabel);
            if (GUILayout.Button("关闭", GUILayout.Width(50)))
            {
                _showBatchPublish = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            var selected = _cache?.GetSelectedPackages() ?? new List<PackageInfo>();
            if (selected.Count == 0)
            {
                EditorGUILayout.HelpBox("没有选中的包", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"选中 {selected.Count} 个包:");
                EditorGUILayout.Space(5);

                foreach (var pkg in selected)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {pkg.DisplayName}", GUILayout.Width(200));
                    EditorGUILayout.LabelField(pkg.Version, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(10);

                if (GUILayout.Button("打开批量发布窗口", GUILayout.Height(30)))
                {
                    var paths = new List<string>();
                    foreach (var pkg in selected)
                    {
                        paths.Add(pkg.Path);
                    }
                    UPMPublishWindow.ShowBatchWindow(paths);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void SaveAllDirty()
        {
            var dirty = _cache?.GetDirtyPackages() ?? new List<PackageInfo>();
            if (dirty.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有需要保存的更改", "确定");
                return;
            }

            var saved = PackageScanService.SaveAllDirty(_cache);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("保存完成", $"已保存 {saved} 个包", "确定");
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
#endif
