using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HW_BioTreeRuntime : MonoBehaviour
    {
        [Header("Growth Control")]
        [Range(0f, 1f)] public float growthCycle = 0f;
        public float growthSpeed = 0.1f;
        public bool autoGrow = false;

        [Header("Bio-Params")]
        public float maxTrunkHeight = 5f;
        public float maxTrunkThickness = 0.2f;
        
        [Header("Branching Rules")]
        [Range(10f, 90f)] public float branchingAngle = 25f;
        [Range(0.5f, 2.0f)] public float branchSpread = 1.0f;
        [Range(0.1f, 1f)] public float lengthDecay = 0.8f;
        [Range(0.1f, 1f)] public float radiusDecay = 0.6f;
        public int maxRecursion = 5;
        public float noiseIntensity = 0.1f; 

        [Header("Randomness & Variation")]
        [Range(0f, 1f)] public float lengthRandomness = 0.2f; // Varies branch length (intervals)
        [Range(0f, 1f)] public float angleRandomness = 0.2f; // Varies branching angle
        public int randomSeed = 0;

        [Header("Visuals")]
        public Material treeMaterial;

        // Internal State
        private MeshFilter meshFilter;
        private List<CombineInstance> meshes = new List<CombineInstance>();

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            if(randomSeed == 0) randomSeed = Random.Range(1, 10000);
        }

        private void Update()
        {
            if (autoGrow)
            {
                growthCycle += Time.deltaTime * growthSpeed;
                if (growthCycle > 1f) growthCycle = 1f;
                GenerateTree();
            }
        }

        private void OnValidate()
        {
            if(randomSeed == 0) randomSeed = Random.Range(1, 10000);
            GenerateTree();
        }

        [ContextMenu("Generate")]
        public void GenerateTree()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            meshes.Clear();
            Random.InitState(randomSeed);

            // [BIO-MIMETIC RULE 1] Height stops growing at 50% maturity.
            float heightGrowth = Mathf.Clamp01(growthCycle / 0.5f); 
            float currentHeight = Mathf.Lerp(0.1f, maxTrunkHeight, heightGrowth * (2f - heightGrowth)); 
            float currentThickness = Mathf.Lerp(0.01f, maxTrunkThickness, growthCycle);
            
            // Base Trunk
            GenerateBranch(Vector3.zero, Vector3.up, currentHeight, currentThickness, 0);

            // Apply to Mesh
            Mesh finalMesh = new Mesh();
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(meshes.ToArray(), true, true);
            finalMesh.RecalculateNormals();
            meshFilter.sharedMesh = finalMesh;
            
            if(GetComponent<MeshRenderer>().sharedMaterial == null && treeMaterial != null)
                 GetComponent<MeshRenderer>().sharedMaterial = treeMaterial;
        }

        private void GenerateBranch(Vector3 startPos, Vector3 direction, float length, float thickness, int depth)
        {
            if (length < 0.05f || thickness < 0.002f) return;

            // [BIO-MIMETIC RULE 3] "Squiggly is okay"
            int segments = 3;
            float segLength = length / segments;
            Vector3 currentPos = startPos;
            Vector3 currentDir = direction;

            for(int s=0; s<segments; s++)
            {
                if (depth > 0) 
                {
                    // [FIX] Reduce noise accumulation to prevent "curling"
                    // Instead of permanently modifying currentDir, we just wiggle the path slightly
                    Vector3 noise = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * noiseIntensity;
                    // Blend noise but keep general direction dominant
                    currentDir = Vector3.Lerp(currentDir, (currentDir + noise).normalized, 0.5f).normalized; 
                }
                
                // [FIX] Upward Tropism (Gravity/Sun) - Prevents falling over
                // As the tree grows, it naturally tries to correct itself upwards (0.1 strength)
                if (depth < maxRecursion / 2) // Stronger in lower trunk
                {
                    currentDir = Vector3.Lerp(currentDir, Vector3.up, 0.1f * lengthDecay).normalized;
                }

                Vector3 nextPos = currentPos + currentDir * segLength;
                float segStartThick = Mathf.Lerp(thickness, thickness * radiusDecay, (float)s/segments);
                float segEndThick = Mathf.Lerp(thickness, thickness * radiusDecay, (float)(s+1)/segments);

                AddCylinderToMesh(currentPos, nextPos, segStartThick, segEndThick);
                currentPos = nextPos;
            }
            Vector3 endPos = currentPos;
            
            // Recursive Branching Logic
            // [TUNING] Reduce delay for faster branching. Was 0.15f * (depth+1).
            float depthThreshold = 0.05f * (depth + 1); 
            float localGrowth = Mathf.Clamp01((growthCycle - depthThreshold) * 5f); // Faster maturity curve

            if (localGrowth > 0f && depth < maxRecursion)
            {
                float baseNewLength = length * lengthDecay * localGrowth; 
                float baseNewThickness = thickness * radiusDecay;

                // 1. Continuation (Main Leader)
                float mainLengthRnd = 1f + Random.Range(-lengthRandomness, lengthRandomness); 
                float finalMainLength = baseNewLength * mainLengthRnd;
                
                // [FIX] Stabilize Main Leader - Less random deviation, more "Upward" bias
                // This fixes the "Curling" issue where the main trunk spirals out of control
                Vector3 randomBendAxis = Vector3.Cross(currentDir, Random.onUnitSphere).normalized; 
                if(randomBendAxis == Vector3.zero) randomBendAxis = Vector3.right;
                
                // Reduced bend angle for main leader (was 15f * randomness)
                Quaternion mainBendRot = Quaternion.AngleAxis(angleRandomness * 10f, randomBendAxis); 
                Vector3 mainDir = (mainBendRot * currentDir).normalized;
                
                // Stronger Tropism for the split point
                mainDir = Vector3.Lerp(mainDir, Vector3.up, 0.2f).normalized;

                GenerateBranch(endPos, mainDir, finalMainLength, baseNewThickness, depth + 1);

                // 2. Side Branches
                // [TUNING] Lower threshold (was 0.2) so branches appear sooner as parent grows
                if (localGrowth > 0.1f) 
                {
                     // [FIX] Limit branch count to 1 or 2 (Removed 3)
                     int branchCount = Random.Range(1, 3); 
                     
                     // [PHYLLOTAXIS] Use Golden Angle (137.5) to guarantee 3D coverage
                     // This ensures each new branch points in a fundamentally different direction from the previous ones
                     // Start with a random roll offset for this node
                     float startRoll = Random.Range(0f, 360f); 

                     for(int i=0; i<branchCount; i++) {
                         float sideLengthRnd = 1f + Random.Range(-lengthRandomness, lengthRandomness);
                         float finalSideLength = baseNewLength * sideLengthRnd * 0.9f; 

                         // Golden Angle Distribution + Random Jitter
                         float currentRoll = startRoll + (i * 137.5f) + Random.Range(-angleRandomness * 45f, angleRandomness * 45f);
                         
                         // Robust 3D Rotation from 'Up' (Growth Direction)
                         // 1. Create a rotation looking at currentDir
                         Quaternion lookRot = (currentDir == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(currentDir);
                         
                         // 2. Create the divergent rotation (Roll + Spread)
                         // Roll around Z (local forward), Spread around X (local right)
                         // Note: LookRotation aligns Z with currentDir.
                         float rndAngleFactor = 1f + Random.Range(-angleRandomness, angleRandomness);
                         float finalAngle = branchingAngle * branchSpread * rndAngleFactor;
                         
                         Quaternion rollQ = Quaternion.AngleAxis(currentRoll, Vector3.forward);
                         Quaternion spreadQ = Quaternion.AngleAxis(finalAngle, Vector3.right); // Pitch out
                         
                         // Combine: Look * Roll * Spread * Forward
                         Vector3 sideDir = lookRot * rollQ * spreadQ * Vector3.forward; 
                         
                         // Ensure it's not identical to mainDir
                         if (Vector3.Dot(sideDir, mainDir) > 0.95f) {
                              sideDir = Quaternion.AngleAxis(45, Vector3.up) * sideDir; // Kick it away if too close
                         }

                         GenerateBranch(endPos, sideDir, finalSideLength, baseNewThickness * 0.8f, depth + 1);
                     }
                }
            }
        }

        private void AddCylinderToMesh(Vector3 start, Vector3 end, float startRadius, float endRadius)
        {
             Vector3 dir = (end - start);
             if(dir == Vector3.zero) return;
             Quaternion rot = Quaternion.LookRotation(dir);
             float height = dir.magnitude;
             
             Mesh tempMesh = CreateConeMesh(height, startRadius, endRadius);
             Matrix4x4 trs = Matrix4x4.TRS(start, rot * Quaternion.Euler(90, 0, 0), Vector3.one);
             meshes.Add(new CombineInstance() { mesh = tempMesh, transform = trs });
        }
        
        private Mesh CreateConeMesh(float height, float bottomRadius, float topRadius)
        {
            Mesh m = new Mesh();
            int segments = 8;
            Vector3[] verts = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segments * 6];
            
            for(int i=0; i<=segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);
                
                verts[i] = new Vector3(x * bottomRadius, 0, y * bottomRadius);
                verts[i + segments + 1] = new Vector3(x * topRadius, height, y * topRadius);
                
                uvs[i] = new Vector2((float)i / segments, 0);
                uvs[i + segments + 1] = new Vector2((float)i / segments, 1);
            }
            
            int t = 0;
            for(int i=0; i<segments; i++)
            {
                tris[t++] = i;
                tris[t++] = i + segments + 1;
                tris[t++] = i + 1;
                
                tris[t++] = i + 1;
                tris[t++] = i + segments + 1;
                tris[t++] = i + segments + 2;
            }
            
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            return m;
        }
    }
}
