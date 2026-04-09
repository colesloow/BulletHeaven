using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Builds and manages the runtime NavMesh for a procedurally generated dungeon.
// Attach this component to the same GameObject as DungeonGenerator.
// DungeonGenerator calls Build() once generation is complete.
//
// Uses NavMeshBuilder.BuildNavMeshData rather than a baked NavMesh Surface
// so the mesh can be generated at runtime from procedural geometry.
[RequireComponent(typeof(DungeonGenerator))]
public class DungeonNavMeshBuilder : MonoBehaviour
{
    // Handle to the runtime NavMesh, kept so it can be removed when the scene unloads.
    private NavMeshDataInstance navMeshInstance;

    // Builds the NavMesh from all placed pieces and sealing walls.
    //
    // Each NavMeshBuildSource describes one walkable (area=0) or obstacle (area=1) shape.
    // The transform field is a TRS matrix (Translation * Rotation * Scale) that places
    // the source shape in world space. For mesh sources this transforms the mesh vertices;
    // for box sources it defines the center and orientation of the box.
    public void Build(IReadOnlyList<DungeonPiece> placedPieces, IReadOnlyList<GameObject> sealingWalls)
    {
        var sources = new List<NavMeshBuildSource>();

        // Compute the world-space bounding volume of the entire dungeon.
        // NavMeshBuilder ignores sources that fall outside this volume.
        bool first = true;
        Bounds totalBounds = default;

        foreach (DungeonPiece piece in placedPieces)
        {
            Bounds b = piece.GetBounds();
            if (first) { totalBounds = b; first = false; }
            else totalBounds.Encapsulate(b);
        }

        // Rooms: use the precise navmesh sub-mesh from the FBX if assigned,
        // otherwise fall back to a flat axis-aligned box covering the room footprint.
        foreach (DungeonPiece piece in placedPieces)
        {
            if (piece is not Room room) continue;

            Mesh floorMesh = room.NavFloorMesh;

            if (floorMesh == null)
            {
                Bounds floor = room.GetFloorBounds();
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = floor.size,
                    transform = Matrix4x4.TRS(floor.center, Quaternion.identity, Vector3.one),
                    area = 0
                });
                continue;
            }

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = floorMesh,
                transform = Matrix4x4.TRS(room.transform.position, room.transform.rotation, Vector3.one),
                area = 0
            });

            Bounds meshBounds = new(
                room.transform.TransformPoint(floorMesh.bounds.center),
                floorMesh.bounds.size
            );
            if (first) { totalBounds = meshBounds; first = false; }
            else totalBounds.Encapsulate(meshBounds);
        }

        // Corridors: same approach with their own navmesh sub-mesh.
        foreach (DungeonPiece piece in placedPieces)
        {
            if (piece is not Corridor corridor) continue;
            if (corridor.NavFloorMesh == null) continue;

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = corridor.NavFloorMesh,
                transform = Matrix4x4.TRS(corridor.transform.position, corridor.transform.rotation, Vector3.one),
                area = 0
            });
        }

        // Sealing walls: mark them as non-walkable (area=1) so the NavMesh agent
        // does not try to walk through sealed doorways.
        foreach (GameObject wall in sealingWalls)
        {
            foreach (Collider col in wall.GetComponentsInChildren<Collider>())
            {
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = col.bounds.size,
                    transform = Matrix4x4.TRS(col.bounds.center, Quaternion.identity, Vector3.one),
                    area = 1
                });
            }
        }

        if (sources.Count == 0) return;

        NavMeshData data = NavMeshBuilder.BuildNavMeshData(
            NavMesh.GetSettingsByID(0),
            sources,
            totalBounds,
            Vector3.zero,
            Quaternion.identity
        );

        if (data != null)
            navMeshInstance = NavMesh.AddNavMeshData(data);
    }

    private void OnDestroy()
    {
        NavMesh.RemoveNavMeshData(navMeshInstance);
    }
}
