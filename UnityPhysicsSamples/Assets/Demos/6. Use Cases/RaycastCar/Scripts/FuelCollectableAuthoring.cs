using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;



//JimK - Random indexes in the grid.
[BurstCompile]
public struct CoordinateIndexBuffer : IBufferElementData
{
    public int spawnIndex;
}

//JimK - Occupancy of spawn grid squares.
[BurstCompile]
public struct OccupancyBuffer : IBufferElementData
{
    public bool occupied;
}

//JimK - Coordinates in the sapwn grid.
[BurstCompile]
public struct CoordBufferElement : IBufferElementData
{
    public float coordinateX;
    public float coordinateY;
    public float coordinateZ;

}
[BurstCompile]
public struct SpawnedCollectableCoordinateBuffer : IBufferElementData
{
    public float coordinateX;
    public float coordinateY;
    public float coordinateZ;

}

//JimK - Entities spawned. Might use IDs here instead.
[BurstCompile]
public struct SpawnedCollectables : IBufferElementData
{
    public Entity entity;
}


//JimK - SpawnGrid holds locations to spawn to. [variables for calculation] 
[BurstCompile]
struct SpawnGridMakerData : IComponentData
{
    public float SpawnGridBorder;

    public float SpawnGridSideLength;

    //JimK - flaot4 preferred by burst.
    public float4 centre;

    public float xlen;

    public float zlen;
    
    public int numberOfIndices;
}

struct FuelCollectable : IComponentData
{
    //JimK - Fuel contained in each barrel.
    public float FuelContained;   

    //Collectable Entity spawned by spawner.
    public Entity Prefab; 

    public uint DesiredCount;    

}

[RequiresEntityConversion]
public class FuelCollectableAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    //JimK - Could have put these variables into an editor script.
    [Header("Fuel Collectable Prefab to spawn.")]
    public GameObject Prefab;

    [Header("Number of Fuel Collectables to spawn.")]
    [Range(0.0f,5000.0f)]
    public uint DesiredCount = 15;

    [Header("Fuel in each collectable.")]
    [Range(0,5000)]
    public float FuelContained = 100.0f;

    [Header("Size of squares to split terrain into for collectable spawning.")]
    [Range(1.0f,50.0f)]
    public float SpawnGridSideLength = 6.0f;

    [Header("Area at edges to avoid")]
    [Range(1.0f,50.0f)]
    public float SpawnGridBorder = 10.0f;



    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(Prefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        MeshRenderer meshRenderer = GameObject.Find("TerrainMesh").GetComponent<MeshRenderer>();
        var bounds = meshRenderer.bounds;
        Vector3 centre = bounds.center;
        var xlen = bounds.size.x;
        var zlen = bounds.size.z;

        var data = new SpawnGridMakerData
        {
            SpawnGridSideLength = SpawnGridSideLength,
            SpawnGridBorder = SpawnGridBorder,
            centre = new float4(centre.x, centre.y, centre.z, 0.0f),
            xlen =xlen,
            zlen =zlen
        };

        var fuelCollectable = new FuelCollectable
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            DesiredCount = DesiredCount,
            FuelContained = FuelContained
        };

        var collectableSpawner = new Collectable{FuelCollectablesSpawned = 0};
        dstManager.AddComponentData(entity, data);
        dstManager.AddBuffer<CoordBufferElement>(entity);
        dstManager.AddBuffer<CoordinateIndexBuffer>(entity);
        dstManager.AddBuffer<SpawnedCollectableCoordinateBuffer>(entity);
        dstManager.AddBuffer<OccupancyBuffer>(entity);
        dstManager.AddBuffer<SpawnedCollectables>(entity);
        dstManager.AddComponentData(entity, fuelCollectable);
        dstManager.AddComponentData(entity, collectableSpawner);
    }
}