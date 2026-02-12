using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 엔딩 연출 컨트롤러.
/// 시네머신을 사용하지 않고 메인 카메라를 직접 제어합니다.
/// </summary>
public class EndingSequenceController : MonoBehaviour
{
    [Header("Falling Object")]
    [Tooltip("낙하 오브젝트 프리팹 (씨앗 등)")]
    public GameObject fallingObjectPrefab;

    [Tooltip("낙하 시작 Y 좌표 (월드 기준)")]
    public float startHeight = 2000f;

    [Tooltip("낙하 속도 (초당 미터)")]
    public float dropSpeed = 50f;

    [Tooltip("낙하 중 좌우 회전 속도 - Y축 (초당 도)")]
    public float rotationSpeedY = 90f;

    [Tooltip("낙하 중 위아래 회전 속도 - X축 (초당 도)")]
    public float rotationSpeedX = 45f;
    [Tooltip("카메라와 오브젝트 사이 거리")]
    public float cameraDistance = 5.0f;

    [Tooltip("오브젝트를 화면 왼쪽에 배치하기 위한 X 오프셋")]
    public float cameraXOffset = 2.0f;

    [Header("Slideshow Settings")]
    [Tooltip("보여줄 엔딩 이미지 리스트 (스프라이트)")]
    public List<Sprite> endingImages;

    [Tooltip("이미지를 보여줄 UI Image 컴포넌트")]
    public Image slideshowImage;

    [Tooltip("각 이미지가 보여질 시간 (초)")]
    public float slideDuration = 5.0f;

    [Tooltip("페이드 인/아웃 시간 (초)")]
    public float fadeDuration = 1.0f;

    [Header("Scene Transition")]
    [Tooltip("엔딩 후 이동할 타이틀 씬 이름")]
    public string titleSceneName = "Title";

    private Coroutine endingCoroutine;
    private GameObject spawnedObject;
    private bool isFollowing = false;
    private float cameraYOffset = 0f; // 카메라가 오브젝트보다 얼마나 위에 있는지 (점점 증가)

    private void Awake()
    {
        if (slideshowImage != null)
        {
            Color c = slideshowImage.color;
            c.a = 0f;
            slideshowImage.color = c;
            slideshowImage.gameObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        // 매 프레임 카메라가 낙하 오브젝트를 따라감 (약간 느리게)
        if (isFollowing && spawnedObject != null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // 카메라는 오브젝트보다 cameraYOffset만큼 위에 위치
            // → 오브젝트가 화면 아래쪽으로 점점 내려감
            Vector3 targetPos = spawnedObject.transform.position
                + new Vector3(cameraXOffset, cameraYOffset, -cameraDistance);

            cam.transform.position = targetPos;
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        }
    }

    [ContextMenu("Test Play Ending")]
    public void TestPlayEnding()
    {
        PlayEnding();
    }

    public void PlayEnding()
    {
        if (endingCoroutine != null) StopCoroutine(endingCoroutine);
        endingCoroutine = StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        Debug.Log("[Ending] Sequence Started.");

        // ── 1. 낙하 오브젝트 생성 ──
        if (fallingObjectPrefab == null)
        {
            Debug.LogError("[Ending] Falling Object Prefab is NOT assigned!");
            yield break;
        }

        Vector3 spawnPos = new Vector3(0f, startHeight, 0f);
        spawnedObject = Instantiate(fallingObjectPrefab, spawnPos, Quaternion.identity);
        spawnedObject.SetActive(true);
        Debug.Log($"[Ending] Object spawned at {spawnPos}");

        // ── 2. 카메라 전환 ──
        Camera cam = Camera.main;
        if (cam != null)
        {
            // 시네머신 브레인 비활성화 (있다면) → 카메라 직접 제어 가능
            var brain = cam.GetComponent<MonoBehaviour>();
            // CinemachineBrain을 이름으로 찾아서 비활성화 (버전 무관)
            var allComponents = cam.GetComponents<MonoBehaviour>();
            foreach (var comp in allComponents)
            {
                if (comp.GetType().Name.Contains("CinemachineBrain"))
                {
                    comp.enabled = false;
                    Debug.Log("[Ending] CinemachineBrain disabled.");
                    break;
                }
            }

            // Far Clip Plane 확장
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, startHeight + 1000f);

            // 카메라 초기 위치 즉시 설정 (오브젝트와 같은 높이, 정면)
            Vector3 camPos = spawnedObject.transform.position
                + new Vector3(cameraXOffset, 0f, -cameraDistance);
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward);

            Debug.Log($"[Ending] Camera set. Distance: {cameraDistance}, XOffset: {cameraXOffset}");
        }

        // LateUpdate에서 매 프레임 따라가기 시작
        cameraYOffset = 0f;
        isFollowing = true;

        // 1프레임 대기
        yield return null;

        // ── 3. 낙하 + 슬라이드쇼 동시 시작 ──
        int count = endingImages != null ? endingImages.Count : 0;
        float totalTime = count * (fadeDuration * 2f + slideDuration);
        if (totalTime <= 0f) totalTime = 10f;

        StartCoroutine(FallRoutine(totalTime));
        yield return StartCoroutine(SlideshowRoutine());

        // ── 4. 정리 및 씬 전환 ──
        isFollowing = false;
        Debug.Log("[Ending] Finished. Loading Title.");
        SceneManager.LoadScene(titleSceneName);
    }

    private IEnumerator FallRoutine(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration); // 0 → 1

            if (spawnedObject != null)
            {
                // 오브젝트는 계속 같은 속도로 떨어짐
                spawnedObject.transform.position += Vector3.down * dropSpeed * Time.deltaTime;

                // 회전 (X축 + Y축)
                spawnedObject.transform.Rotate(
                    rotationSpeedX * Time.deltaTime, 
                    rotationSpeedY * Time.deltaTime, 
                    0f, Space.World);

                // 카메라 지연: 시간이 지날수록 카메라가 점점 덜 따라감
                // progress가 0일 때 lag=0 (완벽 추적), 1일 때 lag=최대
                // 오브젝트 크기 * 거리 기반으로 적절한 오프셋 계산
                float lagAmount = progress * progress; // 가속 곡선 (천천히 시작, 점점 빨라짐)
                cameraYOffset = lagAmount * cameraDistance * 1.5f; // 최대 오프셋 = 거리의 1.5배
            }
            yield return null;
        }
        Debug.Log("[Ending] Fall Routine Finished.");
    }

    private IEnumerator SlideshowRoutine()
    {
        if (slideshowImage == null)
        {
            Debug.LogWarning("[Ending] Slideshow Image not assigned. Skipping.");
            yield return new WaitForSeconds(5f);
            yield break;
        }

        SetAlpha(0f);
        slideshowImage.gameObject.SetActive(true);

        if (endingImages != null)
        {
            foreach (var sprite in endingImages)
            {
                if (sprite == null) continue;
                slideshowImage.sprite = sprite;
                yield return StartCoroutine(Fade(0f, 1f));
                yield return new WaitForSeconds(slideDuration);
                yield return StartCoroutine(Fade(1f, 0f));
            }
        }

        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, timer / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (slideshowImage != null)
        {
            Color c = slideshowImage.color;
            c.a = alpha;
            slideshowImage.color = c;
        }
    }
}
