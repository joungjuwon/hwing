using UnityEngine;

[RequireComponent(typeof(AudioReactor))]
public class AudioWindVisualizer : MonoBehaviour
{
    [Header("Settings")]
    public float minWindStrength = 0.5f;
    public float maxWindStrength = 2.0f;

    private AudioReactor audioReactor;

    private void Awake()
    {
        audioReactor = GetComponent<AudioReactor>();
    }

    private void OnEnable()
    {
        if (audioReactor != null)
        {
            audioReactor.OnUpdateVolume.AddListener(OnAudioReact);
        }
    }

    private void OnDisable()
    {
        if (audioReactor != null)
        {
            audioReactor.OnUpdateVolume.RemoveListener(OnAudioReact);
        }
    }

    private void OnAudioReact(float volume)
    {
        if (GlobalWindManager.Instance != null)
        {
            float targetStrength = Mathf.Lerp(minWindStrength, maxWindStrength, volume);
            GlobalWindManager.Instance.SetWindStrength(targetStrength);
        }
    }
}
