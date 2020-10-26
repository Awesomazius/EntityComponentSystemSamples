    using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;




[UpdateInGroup(typeof(SimulationSystemGroup))]
public class CollectableSpawnerSystem : SystemBase
{
    EntityCommandBufferSystem m_commandBufferSystem;
    EntityCommandBuffer ecb;

    protected override void OnCreate(){}

    
    protected override void OnUpdate()
    {
            var fuelCollectablesQuery = GetSingleton<FuelCollectable>();
            uint desiredFuelCollectables = fuelCollectablesQuery.DesiredCount;

            var spawnedFuelCollectables = GetSingleton<Collectable>();
            uint fuelCollectables = spawnedFuelCollectables.FuelCollectablesSpawned;

            int FuelCollectablesToSpawn = (int)(desiredFuelCollectables - fuelCollectables);

            //JimK - Pre-made objects for assignment.
            var trueOccupancy = new OccupancyBuffer{occupied = true};
            var spawnLocation = new SpawnedCollectableCoordinateBuffer{coordinateX= 0.0f, coordinateY=0.0f, coordinateZ= 0.0f };

            if(FuelCollectablesToSpawn>0)
            {
                m_commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
                var ecb =  m_commandBufferSystem.CreateCommandBuffer();
            
                Entities        
                .ForEach((Entity entity, ref FuelCollectable fuelCollectable, ref DynamicBuffer<OccupancyBuffer> occupancies, ref DynamicBuffer<SpawnedCollectables> entityBuffer, ref DynamicBuffer<SpawnedCollectableCoordinateBuffer> spawned, in GridCompleteTag doneTag, in DynamicBuffer<CoordBufferElement> buffer, in DynamicBuffer<CoordinateIndexBuffer> randex) =>
                {
                    for(int i=0; i<FuelCollectablesToSpawn; i++)
                    {
                        int randIndex = randex[i].spawnIndex;
                        var collectableEntity = ecb.Instantiate(fuelCollectable.Prefab);
                        entityBuffer.Add(new SpawnedCollectables{entity = collectableEntity});
                        var position = new float3(buffer[randIndex].coordinateX,buffer[randIndex].coordinateY, buffer[randIndex].coordinateZ);
                        spawnLocation.coordinateX = position.x;
                        spawnLocation.coordinateY = position.y;
                        spawnLocation.coordinateZ = position.z;
                        spawned.Add(spawnLocation);
                        ecb.SetComponent(collectableEntity, new Translation{Value = position});
                        occupancies[i] = trueOccupancy;
                        spawnedFuelCollectables.FuelCollectablesSpawned++;
                    }
                }).Run();

                var m_Collectable_query = GetEntityQuery(typeof(Collectable));
                m_Collectable_query.SetSingleton(spawnedFuelCollectables);
            }
    }
}
