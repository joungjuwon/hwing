using UnityEngine;
using UnityEditor;

/// <summary>
/// SFX Looper 에디터 도구
/// 선택한 오브젝트에 SoundRandomLooper 컴포넌트를 
/// 원클릭으로 추가/제거/설정할 수 있는 에디터 윈도우입니다.
/// Window > Audio > SFX Looper Tool 에서 열 수 있습니다.
/// </summary>
public class SFXLooperEditorWindow : EditorWindow
{
    private SoundData selectedSoundData;
    private float minDelay = 1f;
    private float maxDelay = 5f;
    private bool playOnEnable = true;
    private Vector2 scrollPos;

    [MenuItem("Window/Audio/SFX Looper Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<SFXLooperEditorWindow>("SFX Looper Tool");
        window.minSize = new Vector2(320, 400);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ── 타이틀 ──
        EditorGUILayout.Space(8);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🔊 SFX Looper Tool", titleStyle);
        EditorGUILayout.Space(4);
        DrawHorizontalLine();

        // ── SoundData 선택 ──
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("사운드 데이터 설정", EditorStyles.boldLabel);
        selectedSoundData = (SoundData)EditorGUILayout.ObjectField(
            "Sound Data", selectedSoundData, typeof(SoundData), false);

        if (selectedSoundData != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("이름", selectedSoundData.soundName);
            
            int clipCount = 0;
            if (selectedSoundData.clips != null && selectedSoundData.clips.Length > 0)
                clipCount = selectedSoundData.clips.Length;
            else if (selectedSoundData.clip != null)
                clipCount = 1;
            EditorGUILayout.LabelField("클립 수", clipCount.ToString());
            EditorGUILayout.LabelField("볼륨", selectedSoundData.volume.ToString("F2"));
            EditorGUILayout.LabelField("피치", selectedSoundData.pitch.ToString("F2"));
            EditorGUI.indentLevel--;
        }

        // ── 딜레이 설정 ──
        EditorGUILayout.Space(10);
        DrawHorizontalLine();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("루프 딜레이 설정", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Min Delay (초)", GUILayout.Width(120));
        minDelay = EditorGUILayout.FloatField(minDelay);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Max Delay (초)", GUILayout.Width(120));
        maxDelay = EditorGUILayout.FloatField(maxDelay);
        EditorGUILayout.EndHorizontal();

        // min/max 자동 보정
        if (minDelay < 0f) minDelay = 0f;
        if (maxDelay < minDelay) maxDelay = minDelay;

        EditorGUILayout.MinMaxSlider("딜레이 범위", ref minDelay, ref maxDelay, 0f, 30f);
        EditorGUILayout.LabelField($"   → {minDelay:F1}초 ~ {maxDelay:F1}초 사이 랜덤", EditorStyles.miniLabel);

        // ── 옵션 ──
        EditorGUILayout.Space(6);
        playOnEnable = EditorGUILayout.Toggle("Play On Enable", playOnEnable);

        // ── SoundData → 설정 복사 ──
        if (selectedSoundData != null)
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("📥 SoundData에서 딜레이 가져오기"))
            {
                minDelay = selectedSoundData.minLoopDelay;
                maxDelay = selectedSoundData.maxLoopDelay;
            }
        }

        // ── 선택된 오브젝트 정보 ──
        EditorGUILayout.Space(10);
        DrawHorizontalLine();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("선택 오브젝트 상태", EditorStyles.boldLabel);

        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("Hierarchy에서 오브젝트를 선택하세요.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"선택된 오브젝트: {selectedObjects.Length}개");
            EditorGUILayout.Space(4);
            
            foreach (var go in selectedObjects)
            {
                var existing = go.GetComponent<SoundRandomLooper>();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(go.name, GUILayout.Width(160));
                if (existing != null)
                {
                    string status = existing.isLooping ? "🟢 루핑" : "🔴 정지";
                    EditorGUILayout.LabelField(status, GUILayout.Width(70));
                    
                    if (existing.soundData != null)
                        EditorGUILayout.LabelField(existing.soundData.soundName, EditorStyles.miniLabel);
                    else
                        EditorGUILayout.LabelField("(데이터 없음)", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("⬜ 미적용", GUILayout.Width(70));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ── 액션 버튼 ──
        EditorGUILayout.Space(10);
        DrawHorizontalLine();
        EditorGUILayout.Space(6);

        GUI.enabled = selectedObjects.Length > 0 && selectedSoundData != null;

        // Apply 버튼
        GUIStyle applyStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fixedHeight = 35 };
        if (GUILayout.Button("🔨 Apply — 선택 오브젝트에 적용", applyStyle))
        {
            ApplyToSelected(selectedObjects);
        }

        GUI.enabled = selectedObjects.Length > 0;

        EditorGUILayout.Space(4);

        // 설정 업데이트 버튼
        if (GUILayout.Button("🔄 Update — 기존 루퍼 설정 업데이트"))
        {
            UpdateExistingLoopers(selectedObjects);
        }

        EditorGUILayout.Space(4);

        // Remove 버튼
        GUIStyle removeStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 30 };
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("🗑 Remove — 선택 오브젝트에서 제거", removeStyle))
        {
            RemoveFromSelected(selectedObjects);
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void ApplyToSelected(GameObject[] objects)
    {
        int applied = 0;
        foreach (var go in objects)
        {
            Undo.RecordObject(go, "Apply SFX Looper");

            var looper = go.GetComponent<SoundRandomLooper>();
            if (looper == null)
            {
                looper = Undo.AddComponent<SoundRandomLooper>(go);
            }

            looper.soundData = selectedSoundData;
            looper.minDelay = minDelay;
            looper.maxDelay = maxDelay;
            looper.playOnEnable = playOnEnable;
            
            EditorUtility.SetDirty(go);
            applied++;
        }
        Debug.Log($"[SFX Looper Tool] {applied}개 오브젝트에 SoundRandomLooper 적용 완료.");
    }

    private void UpdateExistingLoopers(GameObject[] objects)
    {
        int updated = 0;
        foreach (var go in objects)
        {
            var looper = go.GetComponent<SoundRandomLooper>();
            if (looper != null)
            {
                Undo.RecordObject(looper, "Update SFX Looper");
                
                if (selectedSoundData != null)
                    looper.soundData = selectedSoundData;
                    
                looper.minDelay = minDelay;
                looper.maxDelay = maxDelay;
                looper.playOnEnable = playOnEnable;
                
                EditorUtility.SetDirty(looper);
                updated++;
            }
        }
        Debug.Log($"[SFX Looper Tool] {updated}개 루퍼 설정 업데이트 완료.");
    }

    private void RemoveFromSelected(GameObject[] objects)
    {
        int removed = 0;
        foreach (var go in objects)
        {
            var looper = go.GetComponent<SoundRandomLooper>();
            if (looper != null)
            {
                Undo.DestroyObjectImmediate(looper);
                removed++;
            }
        }
        Debug.Log($"[SFX Looper Tool] {removed}개 오브젝트에서 SoundRandomLooper 제거 완료.");
    }

    private void DrawHorizontalLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    // Hierarchy 선택 변경 시 자동 갱신
    private void OnSelectionChange()
    {
        Repaint();
    }
}
