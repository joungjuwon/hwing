using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// SFX Emitter 에디터 도구
/// 선택한 오브젝트에 SoundEmitter 컴포넌트를
/// 원클릭으로 추가/제거/설정할 수 있는 에디터 윈도우입니다.
/// Tools > Audio > SFX Emitter Tool 에서 열 수 있습니다.
/// </summary>
public class SFXLooperEditorWindow : EditorWindow
{
    // 설정 값
    private AudioClip selectedClip;
    private List<AudioClip> clipList = new List<AudioClip>();
    private bool showClipList = false;

    private float volume = 1f;

    private float pitch = 1f;
    private bool useRandomPitch = false;
    private float minPitch = 0.9f;
    private float maxPitch = 1.1f;
    private bool loop = false;
    private bool playOnEnable = true;
    private bool useRandomDelay = false;
    private float minDelay = 1f;
    private float maxDelay = 5f;
    private float spatialBlend = 1f;
    private string emitterId = "";

    private Vector2 scrollPos;

    [MenuItem("Tools/Audio/SFX Emitter Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<SFXLooperEditorWindow>("SFX Emitter Tool");
        window.minSize = new Vector2(340, 500);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ── Title ──
        EditorGUILayout.Space(8);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("SFX Emitter Tool", titleStyle);
        EditorGUILayout.Space(4);
        DrawLine();

        // ── Clip Settings ──
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Clip Settings", EditorStyles.boldLabel);

        selectedClip = (AudioClip)EditorGUILayout.ObjectField("Main Clip", selectedClip, typeof(AudioClip), false);

        showClipList = EditorGUILayout.Foldout(showClipList, $"Random Clips ({clipList.Count})");
        if (showClipList)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < clipList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                clipList[i] = (AudioClip)EditorGUILayout.ObjectField(clipList[i], typeof(AudioClip), false);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    clipList.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Clip"))
                clipList.Add(null);
            EditorGUI.indentLevel--;
        }

        // ── Playback Settings ──
        EditorGUILayout.Space(10);
        DrawLine();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Playback Settings", EditorStyles.boldLabel);

        volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);

        // Pitch 설정 (Random 지원)
        EditorGUILayout.BeginHorizontal();
        pitch = EditorGUILayout.Slider("Pitch", pitch, 0.1f, 3f);
        useRandomPitch = EditorGUILayout.ToggleLeft("Random", useRandomPitch, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        if (useRandomPitch)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            minPitch = EditorGUILayout.FloatField(minPitch, GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            maxPitch = EditorGUILayout.FloatField(maxPitch, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.MinMaxSlider("Pitch Range", ref minPitch, ref maxPitch, 0.1f, 3f);
            if (minPitch < 0.1f) minPitch = 0.1f;
            if (maxPitch < minPitch) maxPitch = minPitch;
            EditorGUI.indentLevel--;
        }

        loop = EditorGUILayout.Toggle("Loop", loop);
        playOnEnable = EditorGUILayout.Toggle("Play On Enable", playOnEnable);
        spatialBlend = EditorGUILayout.Slider("Spatial Blend (0=2D, 1=3D)", spatialBlend, 0f, 1f);

        // ── Random Delay (Loop Only) ──
        EditorGUILayout.Space(6);
        EditorGUI.BeginDisabledGroup(!loop);
        useRandomDelay = EditorGUILayout.Toggle("Use Random Delay", useRandomDelay && loop);

        if (useRandomDelay && loop)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            minDelay = EditorGUILayout.FloatField(minDelay, GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            maxDelay = EditorGUILayout.FloatField(maxDelay, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.MinMaxSlider("Delay Range", ref minDelay, ref maxDelay, 0f, 30f);
            EditorGUILayout.LabelField($"  {minDelay:F1}s ~ {maxDelay:F1}s", EditorStyles.miniLabel);
            if (minDelay < 0f) minDelay = 0f;
            if (maxDelay < minDelay) maxDelay = minDelay;
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndDisabledGroup();

        // ── Emitter ID ──
        EditorGUILayout.Space(6);
        emitterId = EditorGUILayout.TextField("Emitter ID (optional)", emitterId);
        EditorGUILayout.LabelField("  (empty = use object name)", EditorStyles.miniLabel);

        // ── Selected Objects Info ──
        EditorGUILayout.Space(10);
        DrawLine();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Selected Objects", EditorStyles.boldLabel);

        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorGUILayout.HelpBox("Hierarchy에서 오브젝트를 선택하세요.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Selected: {selected.Length}");
            EditorGUILayout.Space(2);

            foreach (var go in selected)
            {
                var emitter = go.GetComponent<SoundEmitter>();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(go.name, GUILayout.Width(150));

                if (emitter != null)
                {
                    string clipName = emitter.clip != null ? emitter.clip.name : "(none)";
                    string loopStr = emitter.loop ? "Loop" : "Once";
                    EditorGUILayout.LabelField($"[{loopStr}] {clipName}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("(no emitter)", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ── Action Buttons ──
        EditorGUILayout.Space(10);
        DrawLine();
        EditorGUILayout.Space(6);

        bool hasClip = selectedClip != null || clipList.Count > 0;
        GUI.enabled = selected.Length > 0 && hasClip;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fixedHeight = 32 };
        if (GUILayout.Button("Apply to Selected", btnStyle))
        {
            ApplyToSelected(selected);
        }

        GUI.enabled = selected.Length > 0;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Update Existing Emitters"))
        {
            UpdateExisting(selected);
        }

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Remove from Selected"))
        {
            RemoveFromSelected(selected);
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void ApplyToSelected(GameObject[] objects)
    {
        int count = 0;
        foreach (var go in objects)
        {
            Undo.RecordObject(go, "Apply SoundEmitter");

            var emitter = go.GetComponent<SoundEmitter>();
            if (emitter == null)
                emitter = Undo.AddComponent<SoundEmitter>(go);

            ConfigureEmitter(emitter);
            EditorUtility.SetDirty(go);
            count++;
        }
        Debug.Log($"[SFX Emitter Tool] {count}개 오브젝트에 SoundEmitter 적용.");
    }

    private void UpdateExisting(GameObject[] objects)
    {
        int count = 0;
        foreach (var go in objects)
        {
            var emitter = go.GetComponent<SoundEmitter>();
            if (emitter != null)
            {
                Undo.RecordObject(emitter, "Update SoundEmitter");
                ConfigureEmitter(emitter);
                EditorUtility.SetDirty(emitter);
                count++;
            }
        }
        Debug.Log($"[SFX Emitter Tool] {count}개 이미터 설정 업데이트.");
    }

    private void RemoveFromSelected(GameObject[] objects)
    {
        int count = 0;
        foreach (var go in objects)
        {
            var emitter = go.GetComponent<SoundEmitter>();
            if (emitter != null)
            {
                // AudioSource도 같이 제거 (SoundEmitter가 추가한 것이므로)
                var audioSrc = go.GetComponent<AudioSource>();
                Undo.DestroyObjectImmediate(emitter);
                if (audioSrc != null) Undo.DestroyObjectImmediate(audioSrc);
                count++;
            }
        }
        Debug.Log($"[SFX Emitter Tool] {count}개 오브젝트에서 SoundEmitter 제거.");
    }

    private void ConfigureEmitter(SoundEmitter emitter)
    {
        emitter.clip = selectedClip;
        emitter.clips = clipList.Count > 0 ? clipList.ToArray() : null;
        emitter.volume = volume;
        emitter.pitch = pitch;
        emitter.useRandomPitch = useRandomPitch;
        emitter.minPitch = minPitch;
        emitter.maxPitch = maxPitch;
        emitter.loop = loop;
        emitter.playOnEnable = playOnEnable;
        emitter.useRandomDelay = useRandomDelay;
        emitter.minDelay = minDelay;
        emitter.maxDelay = maxDelay;
        emitter.spatialBlend = spatialBlend;
        if (!string.IsNullOrEmpty(emitterId))
            emitter.emitterId = emitterId;
    }

    private void DrawLine()
    {
        Rect r = EditorGUILayout.GetControlRect(false, 1);
        r.height = 1;
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private void OnSelectionChange()
    {
        Repaint();
    }
}
