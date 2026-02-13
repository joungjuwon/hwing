using UnityEngine;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance { get; private set; }

    // 터레인별, 레이어별 원본 데이터 저장
    private Dictionary<Terrain, Dictionary<int, int[,]>> originalDetailBackups = new Dictionary<Terrain, Dictionary<int, int[,]>>();
    private Dictionary<Terrain, float[,,]> originalAlphamapBackups = new Dictionary<Terrain, float[,,]>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 특정 터레인의 특정 디테일 레이어를 백업합니다. (이미 백업되어 있으면 무시)
    /// </summary>
    public void BackupDetailLayer(Terrain terrain, int layerIndex)
    {
        if (terrain == null) return;

        if (!originalDetailBackups.ContainsKey(terrain))
        {
            originalDetailBackups[terrain] = new Dictionary<int, int[,]>();
        }

        if (!originalDetailBackups[terrain].ContainsKey(layerIndex))
        {
            TerrainData td = terrain.terrainData;
            // 전체 맵 데이터를 백업합니다.
            int[,] data = td.GetDetailLayer(0, 0, td.detailWidth, td.detailHeight, layerIndex);
            originalDetailBackups[terrain][layerIndex] = data;
            Debug.Log($"[TerrainManager] Backed up detail layer {layerIndex} for {terrain.name}");
        }
    }

    /// <summary>
    /// 특정 터레인의 알파맵(텍스처) 전체를 백업합니다. (이미 백업되어 있으면 무시)
    /// </summary>
    public void BackupAlphamaps(Terrain terrain)
    {
        if (terrain == null) return;

        if (!originalAlphamapBackups.ContainsKey(terrain))
        {
            float[,,] maps = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
            originalAlphamapBackups[terrain] = maps;
            Debug.Log($"[TerrainManager] Backed up alphamaps for {terrain.name}");
        }
    }

    /// <summary>
    /// 저장된 모든 터레인 데이터를 원본 상태로 복구합니다.
    /// </summary>
    public void RestoreAllTerrains()
    {
        foreach (var terrainEntry in originalDetailBackups)
        {
            Terrain terrain = terrainEntry.Key;
            // 터레인이 파괴되지 않았고 데이터가 유효한 경우 복구
            if (terrain != null && terrain.terrainData != null)
            {
                foreach (var layerEntry in terrainEntry.Value)
                {
                    int layerIndex = layerEntry.Key;
                    int[,] originalData = layerEntry.Value;
                    terrain.terrainData.SetDetailLayer(0, 0, layerIndex, originalData);
                }
            }
        }

        foreach (var entry in originalAlphamapBackups)
        {
            if (entry.Key != null && entry.Key.terrainData != null)
            {
                entry.Key.terrainData.SetAlphamaps(0, 0, entry.Value);
            }
        }
        
        originalDetailBackups.Clear();
        originalAlphamapBackups.Clear();
        Debug.Log("[TerrainManager] All terrains restored to original state.");
    }

    /// <summary>
    /// 타이틀 화면 진입 시 적용할 터레인 스타일 설정
    /// 1. 잔디(Detail) 제거
    /// 2. 텍스쳐(Alphamap)를 1번 레이어(인덱스 1)로 통일
    /// </summary>
    public void ApplyTitleStyle()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0) return;

        foreach (var terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;

            TerrainData td = terrain.terrainData;

            // 1. 원본 데이터 백업 (나중에 복구하기 위해)
            // 모든 디테일 레이어 백업
            for (int i = 0; i < td.detailPrototypes.Length; i++)
            {
                BackupDetailLayer(terrain, i);
            }
            // 알파맵 백업
            BackupAlphamaps(terrain);

            // 2. 잔디(Detail) 모두 제거
            for (int i = 0; i < td.detailPrototypes.Length; i++)
            {
                int[,] emptyDetails = new int[td.detailWidth, td.detailHeight]; // 0으로 초기화됨
                td.SetDetailLayer(0, 0, i, emptyDetails);
            }

            // 3. 텍스쳐(Alphamap)를 1번 레이어(인덱스 1)로 전체 교체
            // 레이어가 2개 이상이어야 1번 인덱스 사용 가능
            if (td.alphamapLayers > 1)
            {
                float[,,] newMaps = new float[td.alphamapWidth, td.alphamapHeight, td.alphamapLayers];
                
                // 모든 픽셀에 대해
                for (int y = 0; y < td.alphamapHeight; y++)
                {
                    for (int x = 0; x < td.alphamapWidth; x++)
                    {
                        // 인덱스 1을 1.0f로, 나머지는 0.0f
                        newMaps[y, x, 1] = 1.0f; 
                    }
                }
                
                td.SetAlphamaps(0, 0, newMaps);
            }
            else
            {
                Debug.LogWarning($"[TerrainManager] Cannot set Texture Layer 1 for {terrain.name}: Not enough layers.");
            }
        }
        
        Debug.Log("[TerrainManager] Applied Title Style (No Grass, Processed Texture).");
    }

    private void OnDestroy()
    {
        // 매니저가 파괴될 때(씬 전환, 게임 종료 등) 복구 수행
        RestoreAllTerrains();
    }

    private void OnApplicationQuit()
    {
        RestoreAllTerrains();
    }
}
