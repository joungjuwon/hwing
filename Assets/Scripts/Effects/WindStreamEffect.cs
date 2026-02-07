using UnityEngine;

/// <summary>
/// 낙하하는 물체(씨앗)에 역동적인 바람 효과(스피드 라인, 난기류)를 추가합니다.
/// 컴포넌트를 추가하기만 하면 자동으로 파티클 시스템을 설정해줍니다.
/// [ExecuteAlways]를 추가하여 에디터에서도 실시간으로 변화를 볼 수 있습니다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
[ExecuteAlways] 
public class WindStreamEffect : MonoBehaviour
{
    [Header("Wind Settings")]
    [Tooltip("파티클 생성 색상")]
    public Color streamColor = new Color(1f, 1f, 1f, 0.5f);
    [Tooltip("파티클 크기")]
    public float particleSize = 0.1f;
    [Tooltip("난기류 강도 (바람에 흔들리는 느낌)")]
    public float turbulenceStrength = 0.5f;

    private ParticleSystem ps;

    private void Start()
    {
        Initialize();
    }

    private void OnValidate()
    {
        // 인스펙터 값이 바뀌면 즉시 적용
        ApplySettings();
    }

    private void Update()
    {
        // 런타임 중에도 값이 바뀌면 적용 (테스트 편의성)
        if (Application.isPlaying)
        {
            ApplySettings();
        }
    }

    private void Initialize()
    {
        ps = GetComponent<ParticleSystem>();
        SetupParticleSystem();
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        main.startColor = streamColor;
        main.startSize = particleSize;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = turbulenceStrength;
    }

    private void SetupParticleSystem()
    {
        if (ps == null) return;

        // 1. 기본 설정 (Main Module)
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World; 
        main.startLifetime = 0.5f; 
        main.startSpeed = 0f; 
        main.maxParticles = 1000;
        main.playOnAwake = true;

        // 2. 배출 설정 (Emission)
        var emission = ps.emission;
        emission.rateOverTime = 50f; 
        emission.rateOverDistance = 10f; 

        // 3. 모양 설정 (Shape)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // 4. 크기 변화
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // 5. 노이즈 설정 (Noise)
        var noise = ps.noise;
        noise.enabled = true;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 1.0f;

        // 6. 렌더러 (Renderer)
        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.cameraVelocityScale = 0f;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 2.0f; 
        
        // 중요: 색상 변경이 안 된다면 머티리얼 문제일 수 있음.
        // Legacy Alpha Blended는 Vertex Color를 확실하게 지원함.
        if (renderer.material == null || renderer.material.name.Contains("Default-Particle"))
        {
            // 쉐이더 교체 시도 (없으면 기본값 사용)
            Shader legacyShader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (legacyShader != null)
            {
                renderer.material = new Material(legacyShader);
            }
            else
            {
                // Unlit Standard Fallback
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            }
        }
    }
}
