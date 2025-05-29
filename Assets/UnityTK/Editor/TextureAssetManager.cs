using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTK.Editor
{
    public class TextureAssetManager : EditorWindow
    {
        [Flags]
        private enum GroupOption
        {
            TextureType   = 1 << 0,
            IsReadable    = 1 << 1,
            FilterMode    = 1 << 2,
            MaxSize       = 1 << 3,
            WrapMode      = 1 << 4,
            AnisoLevel    = 1 << 5,
            Compression   = 1 << 6,
            MipMaps       = 1 << 7,
        }

        // 동적 키: 선택된 옵션만 비교·해시·출력
        private struct DynamicKey : IEquatable<DynamicKey>
        {
            public readonly GroupOption Options;
            public readonly TextureImporterType TextureType;
            public readonly bool IsReadable;
            public readonly FilterMode FilterMode;
            public readonly int MaxSize;
            public readonly TextureWrapMode WrapMode;
            public readonly int AnisoLevel;
            public readonly TextureImporterCompression Compression;
            public readonly bool MipMapsEnabled;

            public DynamicKey(TextureImporter imp, GroupOption opts)
            {
                Options        = opts;
                TextureType    = imp.textureType;
                IsReadable     = imp.isReadable;
                FilterMode     = imp.filterMode;
                MaxSize        = imp.maxTextureSize / 32 * 32;
                WrapMode       = imp.wrapMode;
                AnisoLevel     = imp.anisoLevel;
                Compression    = imp.textureCompression;
                MipMapsEnabled = imp.mipmapEnabled;
            }

            public bool Equals(DynamicKey o)
            {
                if (Options != o.Options) return false;
                if (Options.HasFlag(GroupOption.TextureType) &&
                    TextureType != o.TextureType) return false;
                if (Options.HasFlag(GroupOption.IsReadable) &&
                    IsReadable != o.IsReadable) return false;
                if (Options.HasFlag(GroupOption.FilterMode) &&
                    FilterMode != o.FilterMode) return false;
                if (Options.HasFlag(GroupOption.MaxSize) &&
                    MaxSize != o.MaxSize) return false;
                if (Options.HasFlag(GroupOption.WrapMode) &&
                    WrapMode != o.WrapMode) return false;
                if (Options.HasFlag(GroupOption.AnisoLevel) &&
                    AnisoLevel != o.AnisoLevel) return false;
                if (Options.HasFlag(GroupOption.Compression) &&
                    Compression != o.Compression) return false;
                if (Options.HasFlag(GroupOption.MipMaps) &&
                    MipMapsEnabled != o.MipMapsEnabled) return false;  
                
                return true;
            }

            public override bool Equals(object obj) =>
                obj is DynamicKey dk && Equals(dk);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = (int)Options;
                    if (Options.HasFlag(GroupOption.TextureType))
                        h = h * 31 + (int)TextureType;
                    if (Options.HasFlag(GroupOption.IsReadable))
                        h = h * 31 + IsReadable.GetHashCode();
                    if (Options.HasFlag(GroupOption.FilterMode))
                        h = h * 31 + (int)FilterMode;
                    if (Options.HasFlag(GroupOption.MaxSize))
                        h = h * 31 + MaxSize;
                    if (Options.HasFlag(GroupOption.WrapMode))
                        h = h * 31 + (int)WrapMode;
                    if (Options.HasFlag(GroupOption.AnisoLevel))
                        h = h * 31 + AnisoLevel;
                    if (Options.HasFlag(GroupOption.Compression))
                        h = h * 31 + (int)Compression;
                    if (Options.HasFlag(GroupOption.MipMaps))
                        h = h * 31 + MipMapsEnabled.GetHashCode();  

                    return h;
                }
            }

            public override string ToString()
            {
                var parts = new List<string>();
                if (Options.HasFlag(GroupOption.TextureType))
                    parts.Add(TextureType.ToString());
                if (Options.HasFlag(GroupOption.IsReadable))
                    parts.Add(IsReadable ? "Readable" : "Non-Readable");
                if (Options.HasFlag(GroupOption.FilterMode))
                    parts.Add(FilterMode.ToString());
                if (Options.HasFlag(GroupOption.MaxSize))
                    parts.Add($"{MaxSize}px");
                if (Options.HasFlag(GroupOption.WrapMode))
                    parts.Add(WrapMode.ToString());
                if (Options.HasFlag(GroupOption.AnisoLevel))
                    parts.Add($"Aniso:{AnisoLevel}");
                if (Options.HasFlag(GroupOption.Compression))
                    parts.Add(Compression.ToString());
                if (Options.HasFlag(GroupOption.MipMaps))
                    parts.Add(MipMapsEnabled ? "MipMaps On" : "MipMaps Off");

                return parts.Count > 0 ? string.Join(", ", parts) : "<No Criteria>";
            }
        }

        //---- 인스턴스 데이터 ----
        private Dictionary<DynamicKey, List<string>> _categories;
        private Dictionary<string, bool> _directoryFoldouts = new Dictionary<string, bool>();
        private DynamicKey? _currentKey;
        private Vector2 _groupScroll, _itemScroll, _inspectorScroll;
        private float _listPaneWidth = 300f;
        private bool _isResizing;
        private float _groupPaneHeight = 100f;
        private bool _isGroupResizing;
        private bool _showGroupOptions = true;
        private GroupOption _groupOptions =
            GroupOption.TextureType |
            GroupOption.IsReadable |
            GroupOption.FilterMode |
            GroupOption.MaxSize |
            GroupOption.WrapMode |
            GroupOption.AnisoLevel |
            GroupOption.Compression |
            GroupOption.MipMaps;

        private string _selectedPath;
        private TextureImporter _selectedImporter;
        private UnityEditor.Editor _importerEditor;

        [MenuItem("DD/Texture Asset Manager")]
        private static void ShowWindow()
        {
            var w = GetWindow<TextureAssetManager>();
            w.titleContent = new GUIContent("Texture Asset Manager");
            w.RefreshTextures();
        }

        private void RefreshTextures()
        {
            _categories = BuildCategories();
            ClearSelection();
        }

        private Dictionary<DynamicKey, List<string>> BuildCategories()
        {
            return AssetDatabase
                .FindAssets("t:Texture", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => new {
                    path,
                    imp = AssetImporter.GetAtPath(path) as TextureImporter
                })
                .Where(x => x.imp != null)
                .GroupBy(x => new DynamicKey(x.imp, _groupOptions))
                .ToDictionary(g => g.Key, g => g.Select(x => x.path).ToList());
        }

        private void ClearSelection()
        {
            _selectedPath = null;
            _selectedImporter = null;
            if (_importerEditor != null) DestroyImmediate(_importerEditor);
            _importerEditor = null;
            _currentKey = null;
            _groupScroll = _itemScroll = _inspectorScroll = Vector2.zero;
        }

        private void OnGUI()
        {
            HandleSplitter();
            GUILayout.BeginHorizontal();
            DrawLeftPane();
            GUILayout.BeginVertical();
            DrawImporterSettings();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void HandleSplitter()
        {
            var rect = new Rect(_listPaneWidth, 0, 4f, position.height);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill,
                false, 0, new Color(0, 0, 0, 0.2f), 0, 0);

            var e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                _isResizing = true; e.Use();
            }
            if (e.type == EventType.MouseDrag && _isResizing)
            {
                _listPaneWidth = Mathf.Clamp(e.mousePosition.x, 100f, 800f);
                Repaint(); e.Use();
            }
            if (e.type == EventType.MouseUp && _isResizing)
            {
                _isResizing = false; e.Use();
            }
        }

        private void DrawLeftPane()
        {
            GUILayout.BeginVertical(GUILayout.Width(_listPaneWidth));

            // 그룹화 옵션
            EditorGUI.BeginChangeCheck();
            _showGroupOptions = EditorGUILayout.Foldout(_showGroupOptions, "Group By");
            if (_showGroupOptions)
            {
                EditorGUILayout.BeginVertical("box");
                var newOpts = (GroupOption)EditorGUILayout.EnumFlagsField(
                    new GUIContent("Select Criteria", "Properties used to group textures"),
                    _groupOptions,
                    GUILayout.ExpandWidth(true));
                if (newOpts != _groupOptions)
                {
                    _groupOptions = newOpts;
                    RefreshTextures();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndChangeCheck();

            // 그룹 리스트
            EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);
            _groupScroll = EditorGUILayout.BeginScrollView(
                _groupScroll, GUILayout.Height(_groupPaneHeight));
            if (_categories != null)
            {
                foreach (var key in _categories.Keys)
                {
                    bool isSelected = _currentKey.HasValue && key.Equals(_currentKey.Value);
                    var groupCount = _categories[key].Count;

                    // 버튼 스타일 커스텀
                    var btnStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        fontSize = 13,
                        fixedHeight = 32,
                        normal = { textColor = isSelected ? Color.white : Color.black }
                    };

                    // 선택된 그룹은 파란색 배경
                    if (isSelected)
                    {
                        var selectedTex = new Texture2D(1, 1);
                        selectedTex.SetPixel(0, 0, new Color(0.24f, 0.49f, 0.90f)); // Unity 기본 선택 파랑
                        selectedTex.Apply();
                        btnStyle.normal.background = selectedTex;
                        btnStyle.active.background = selectedTex;
                        btnStyle.focused.background = selectedTex;
                    }

                    EditorGUILayout.BeginVertical("box");
                    if (GUILayout.Button($"{key}  ({groupCount})", btnStyle))
                        _currentKey = key;
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndScrollView();

            // 그룹/텍스처 스플리터
            var splitter = GUILayoutUtility.GetRect(0, 4f, GUILayout.ExpandWidth(true));
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeVertical);
            GUI.DrawTexture(splitter, Texture2D.whiteTexture, ScaleMode.StretchToFill,
                false, 0, new Color(0, 0, 0, 0.2f), 0, 0);

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && splitter.Contains(evt.mousePosition))
            {
                _isGroupResizing = true; evt.Use();
            }
            if (evt.type == EventType.MouseDrag && _isGroupResizing)
            {
                _groupPaneHeight = Mathf.Clamp(
                    _groupPaneHeight + evt.delta.y,
                    50f, position.height - 200);
                Repaint(); evt.Use();
            }
            if (evt.type == EventType.MouseUp && _isGroupResizing)
            {
                _isGroupResizing = false; evt.Use();
            }

            DrawTextureList();
            GUILayout.EndVertical();
        }

        private GroupOption ToggleOption(string label, GroupOption flag, GroupOption mask)
        {
            bool on = mask.HasFlag(flag);
            on = EditorGUILayout.ToggleLeft(label, on);
            if (on) mask |= flag; else mask &= ~flag;
            return mask;
        }

        private void DrawTextureList()
        {
            if (_currentKey == null) return;
            if (!_categories.TryGetValue(_currentKey.Value, out var list) || list.Count == 0)
            {
                EditorGUILayout.HelpBox("No textures in this group.", MessageType.Info);
                return;
            }

            // 디렉토리별로 그룹화
            var dirGroups = list
                .GroupBy(path => Path.GetDirectoryName(path))
                .OrderBy(g => g.Key);

            _itemScroll = EditorGUILayout.BeginScrollView(_itemScroll);
            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 40,
                imagePosition = ImagePosition.ImageLeft
            };

            foreach (var dirGroup in dirGroups)
            {
                string dir = dirGroup.Key;
                if (!_directoryFoldouts.ContainsKey(dir))
                    _directoryFoldouts[dir] = true;

                _directoryFoldouts[dir] = EditorGUILayout.Foldout(_directoryFoldouts[dir], dir, true);

                if (_directoryFoldouts[dir])
                {
                    EditorGUI.indentLevel++;
                    foreach (var path in dirGroup)
                    {
                        var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
                        var content = new GUIContent(" " + Path.GetFileName(path), tex);
                        if (GUILayout.Button(content, btnStyle))
                            SelectTexture(path);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawImporterSettings()
        {
            EditorGUILayout.LabelField(
                "Selected Texture",
                Path.GetFileName(_selectedPath),
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);
            if (_importerEditor == null)
            {
                EditorGUILayout.HelpBox("Select a texture on the left.", MessageType.Info);
                return;
            }

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            _importerEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply & Reimport"))
            {
                _importerEditor.serializedObject.ApplyModifiedProperties();
                if (_selectedImporter != null)
                    _selectedImporter.SaveAndReimport();
                
                // 리프레시 후 이전 선택 복원
                var prevKey  = _currentKey;
                var prevPath = _selectedPath;
                RefreshTextures();
                if (prevKey.HasValue && _categories.ContainsKey(prevKey.Value))
                    _currentKey = prevKey;
                if (!string.IsNullOrEmpty(prevPath))
                    SelectTexture(prevPath);
            }
            if (GUILayout.Button("Revert"))
                _importerEditor.serializedObject.Update();
            GUILayout.EndHorizontal();
        }

        private void SelectTexture(string path)
        {
            _selectedPath = path;
            _selectedImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (_importerEditor != null) DestroyImmediate(_importerEditor);
            if (_selectedImporter != null)
                _importerEditor = UnityEditor.Editor.CreateEditor(_selectedImporter);
        }
    }
}