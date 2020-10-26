using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Physics.Extensions;
using Unity.Physics;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System;
using System.Collections.Generic;
using Unity.Physics.Systems;
using RaycastHit = Unity.Physics.RaycastHit;






//JimK - Tag component for grid having been made.
public struct GridCompleteTag : IComponentData{}

//JimK - System makes the persistent grid for spawning.
[RequireComponent(typeof(SpawnGridMakerData))]

public class SpawnGridMakerSystem : SystemBase
{
    
    EntityCommandBufferSystem m_commandBufferSystem;
    EntityCommandBuffer commandBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
    }
    protected override void OnStartRunning()
    {
        //JimK - unity random.
        var random = new Random();
        //JimK - seeding random.
        random.InitState((uint)System.DateTime.Now.Ticks);

        m_commandBufferSystem = World.GetExistingSystem<EntityCommandBufferSystem>();
        commandBuffer = m_commandBufferSystem.CreateCommandBuffer();
        
        //JimK - lambda to set up XZ points on the grid. Could have scheduled the grid coordinate calculation in parallel once 
        //size of grid was known.
        JobHandle coordinateFiller = Entities
        .WithName("XZCoordinateFillerJob")
        .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
        .ForEach((Entity entity, ref Collectable collectable, ref DynamicBuffer<CoordBufferElement> buffer, ref SpawnGridMakerData sgmd) =>
            {
                //JimK - cast is faster than math floor.
                var xIntWidth = (int)((sgmd.xlen - sgmd.SpawnGridBorder*2.0f)/sgmd.SpawnGridSideLength); 
                var zIntWidth = (int)((sgmd.zlen - sgmd.SpawnGridBorder*2.0f)/sgmd.SpawnGridSideLength); 
                var indices = sgmd.numberOfIndices = collectable.numberOfSpawnLocations = xIntWidth * zIntWidth;


                float leftOverX = (sgmd.xlen-sgmd.SpawnGridBorder*2.0f)% sgmd.SpawnGridSideLength;
                float leftOverZ = (sgmd.zlen-sgmd.SpawnGridBorder*2.0f)% sgmd.SpawnGridSideLength;
                float startX = sgmd.centre.x - sgmd.xlen/2.0f + sgmd.SpawnGridBorder + leftOverX/2.0f;
                float startZ = sgmd.centre.z - sgmd.zlen/2.0f + sgmd.SpawnGridBorder + leftOverZ/2.0f;
                float squareLength = sgmd.SpawnGridSideLength;


            for(int i = 0; i < indices; i++)
            {
                int xIndex = i%xIntWidth;
                int zIndex = (i/xIntWidth);
                
                //JimK - staggering +/-0.4 from square centres.
                float X = startX + (((float)xIndex)+0.5f+random.NextFloat(-0.4f,+0.4f))*squareLength;
                float Z = startZ + (((float)zIndex)+0.5f+random.NextFloat(-0.4f,+0.4f))*squareLength;
                
                

                buffer.Add(new CoordBufferElement{
                    coordinateX = X,
                    coordinateY = +20.0f,
                    coordinateZ = Z
                }
                );
            }
        }).Schedule(this.Dependency);//JimK - dependant on monobehaviours that run before OnStartRunning
        coordinateFiller.Complete();


        
        var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;
        var query = GetEntityQuery(typeof(FuelCollectable));
        Entity bufferEntity = query.GetSingletonEntity();
        var coordsForRayCast = GetBuffer<CoordBufferElement>(bufferEntity);
        int len = coordsForRayCast.Length;

        var results = new NativeArray<UnityEngine.RaycastHit>(len, Allocator.Persistent);
        var commands = new NativeArray<RaycastCommand>(len, Allocator.TempJob);


        float3 rayStart = new float3(0.0f,20.0f, 0.0f);
        CollisionFilter collisionFilter = CollisionFilter.Default;

        for(int i=0; i<len; i++)
        {
            rayStart.x = coordsForRayCast[i].coordinateX;
            rayStart.z = coordsForRayCast[i].coordinateZ;
            commands[i] = new RaycastCommand(rayStart, Vector3.down);
        }

        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, coordinateFiller);
        handle.Complete();
        commands.Dispose();



        JobHandle fillRaycast = 
        Entities
        .WithName("RayCastResultsFiller")
        .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
        .ForEach((ref DynamicBuffer<CoordBufferElement> finalBuffer)=>
        {
            CoordBufferElement buf = new CoordBufferElement{coordinateX =0.0f, coordinateY=0.0f, coordinateZ=0.0f}; 
            var length = finalBuffer.Length;
            for(int i=0; i<len; i++)
            {
                buf.coordinateX = finalBuffer[i].coordinateX;
                buf.coordinateZ = finalBuffer[i].coordinateZ;
                buf.coordinateY = results[i].point.y + 1.5f; //JimK - fuelcube is of height 2 so half that and 0.5;
                finalBuffer[i] =buf;
            }
        }).Schedule(handle);

        fillRaycast.Complete();        
        results.Dispose();
        

        //JimK - lambda to shuffle indices for the grid locations. Dependant on number of coordinates.
        //JimK - Would have used Linq: int[] indexArray = Enumerable.Range(0, sg.numberOfIndices).ToArray();
        //JimK - There has to be a better way to do shuffle indices.
        Entities
        .WithName("CoordinateIndexShuffleJob")
       .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
        .ForEach((Entity entity, ref DynamicBuffer<CoordinateIndexBuffer> bufferElements,ref DynamicBuffer<OccupancyBuffer> occupancyBuffer, in SpawnGridMakerData sg)=>
        {
            var indices = sg.numberOfIndices;

            for(int i=0; i<indices;i++)
            {
                bufferElements.Add(new CoordinateIndexBuffer{spawnIndex = i});
            }

            for (int i = 0; i < indices; i++) {
                var temp = bufferElements[i];
                int randomIndex = random.NextInt(i, (indices-1));
                bufferElements[i] = bufferElements[randomIndex];//JimK - equating indices not values.
                bufferElements[randomIndex] = temp;
            }

            //JimK - awful loop.
            var falseOccupancy = new OccupancyBuffer{occupied = false};
            for(int i=0; i<indices; i++){occupancyBuffer.Add(falseOccupancy);}

        }).Schedule();//JimK - Dependant on array dimensions being known.
        
        //JimK - The dimensions of the grid are no longer useful.
        var entityQuery = GetEntityQuery(typeof(SpawnGridMakerData));

        entityQuery = GetEntityQuery(typeof(FuelCollectable));
        Entity thisEntity = entityQuery.GetSingletonEntity();
        commandBuffer.AddComponent<GridCompleteTag>(thisEntity);//JimK - deliberate sync point.
        commandBuffer.RemoveComponent<SpawnGridMakerData>(thisEntity);

        base.OnStartRunning();
    }

    protected override void OnUpdate(){}

    
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
