using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 표준 파티클 시스템을 UI Canvas(Overlay 모드 포함) 내부에서 렌더링되도록 변환해주는 스크립트입니다.
/// 파티클 오브젝트에 이 컴포넌트를 추가하면, 파티클이 UI 요소처럼 취급되어 순서대로 그려집니다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(CanvasRenderer))]
public class UIParticleSystem : MaskableGraphic
{
    [Tooltip("파티클 텍스처 (Material의 텍스처와 동일하게 설정하세요)")]
    public Texture particleTexture;

    [Tooltip("3D 회전 무시 여부")]
    public bool ignore3DRotation = true;

    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _renderer;
    private ParticleSystem.Particle[] _particles;
    private UIVertex[] _quad = new UIVertex[4];
    private Vector4 _uv = new Vector4(0, 0, 1, 1);

    public override Texture mainTexture
    {
        get
        {
            if (particleTexture) return particleTexture;
            return material && material.mainTexture ? material.mainTexture : base.mainTexture;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _particleSystem = GetComponent<ParticleSystem>();
        _renderer = GetComponent<ParticleSystemRenderer>();
    }

    private void Update()
    {
        // 파티클이 변경될 때마다 UI 다시 그리기 요청
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (!_particleSystem) return;
        if (Application.isPlaying && !_particleSystem.isPlaying) return;

        int count = _particleSystem.particleCount;
        if (count == 0) return;

        if (_particles == null || _particles.Length < _particleSystem.main.maxParticles)
        {
            _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
        }

        _particleSystem.GetParticles(_particles);

        for (int i = 0; i < count; i++)
        {
            DrawParticle(vh, _particles[i]);
        }
    }

    private void DrawParticle(VertexHelper vh, ParticleSystem.Particle particle)
    {
        var center = particle.position;
        var rotation = Quaternion.Euler(particle.rotation3D);
        
        if (ignore3DRotation) rotation = Quaternion.Euler(0, 0, particle.rotation3D.z);
        
        // 파티클 크기
        var size = particle.GetCurrentSize3D(_particleSystem);
        
        // 색상 (UI Alpha 등 적용)
        var color = particle.GetCurrentColor(_particleSystem) * this.color;

        // Quad 생성
        var leftBottom = new Vector3(-size.x * 0.5f, -size.y * 0.5f);
        var leftTop = new Vector3(-size.x * 0.5f, size.y * 0.5f);
        var rightTop = new Vector3(size.x * 0.5f, size.y * 0.5f);
        var rightBottom = new Vector3(size.x * 0.5f, -size.y * 0.5f);

        if (!ignore3DRotation)
        {
            leftBottom = rotation * leftBottom;
            leftTop = rotation * leftTop;
            rightTop = rotation * rightTop;
            rightBottom = rotation * rightBottom;
        }
        else
        {
            // 2D 회전 (Z축)
            float angle = particle.rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            leftBottom = Rotate2D(leftBottom, cos, sin);
            leftTop = Rotate2D(leftTop, cos, sin);
            rightTop = Rotate2D(rightTop, cos, sin);
            rightBottom = Rotate2D(rightBottom, cos, sin);
        }

        // 로컬 좌표로 변환 + 중심 이동
        leftBottom += center;
        leftTop += center;
        rightTop += center;
        rightBottom += center;

        // UV 설정
        _quad[0] = SetUIVertex(leftBottom, new Vector2(_uv.x, _uv.y), color);
        _quad[1] = SetUIVertex(leftTop, new Vector2(_uv.x, _uv.w), color);
        _quad[2] = SetUIVertex(rightTop, new Vector2(_uv.z, _uv.w), color);
        _quad[3] = SetUIVertex(rightBottom, new Vector2(_uv.z, _uv.y), color);

        vh.AddUIVertexQuad(_quad);
    }

    private Vector3 Rotate2D(Vector3 v, float cos, float sin)
    {
        return new Vector3(v.x * cos - v.y * sin, v.x * sin + v.y * cos, 0);
    }

    private UIVertex SetUIVertex(Vector3 position, Vector2 uv, Color32 color)
    {
        UIVertex vertex = new UIVertex();
        vertex.position = position;
        vertex.uv0 = uv;
        vertex.color = color;
        return vertex;
    }
}
