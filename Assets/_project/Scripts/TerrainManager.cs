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
