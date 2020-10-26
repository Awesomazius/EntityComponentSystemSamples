using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;



[UpdateInGroup(typeof(SimulationSystemGroup))]
public class FuelCollectionSystem : SystemBase
{
    EntityCommandBufferSystem m_commandBufferSystem;
    EntityCommandBuffer ecb;

    //JimK - Radial distance collectionDistance from collectable.
    public float collectionDistance = 12.0f;
        
    //JimK - Caching position. Will only check when changes nontrivially.
    float cachedX;
    float cachedZ;
        
    //JimK - Variables used.
    float Xdistsq;
    float Zdistsq;

    protected override void OnCreate()
    {
        m_commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        ecb =  m_commandBufferSystem.CreateCommandBuffer();
        base.OnCreate();
    }

    // protected override void OnStartRunning(){base.OnStartRunning();}

    protected override void OnUpdate()
    {
        float collectionDistanceSq = collectionDistance * collectionDistance;
        var m_entityQuery = GetEntityQuery(typeof(VehicleFuel),typeof(Translation), typeof(ActiveVehicle));

        var fuelComponent = m_entityQuery.GetSingleton<VehicleFuel>();        
        var transformComponent = m_entityQuery.GetSingleton<Translation>();
        

        var position = transformComponent.Value;
        var Xpos = position.x;
        var Zpos = position.z;

        //JimK - Premade objects for assignment.
        OccupancyBuffer falseVariable = new OccupancyBuffer{occupied=false};
        OccupancyBuffer trueVariable = new OccupancyBuffer{occupied=true};       
        
        //JimK - Insert check that XZ postion has changed from Cached?
        if(Xpos-cachedX> 0.01f || Xpos-cachedX< -0.01f || Zpos - cachedZ> 0.01f || Zpos - cachedZ< -0.01f )
        {
            cachedX = Xpos; cachedZ = Zpos;

            Entities
            .WithoutBurst()
            .ForEach((ref DynamicBuffer<OccupancyBuffer> occupancies, ref DynamicBuffer<SpawnedCollectables> entityBuffer,ref Collectable collectable, ref DynamicBuffer<SpawnedCollectableCoordinateBuffer> spawned, in  FuelCollectable fuelVariable, in DynamicBuffer<CoordinateIndexBuffer> randex) =>
            {
                int noOfCollectables = (int)collectable.CollectablesSpawned;

                for(int i = 0; i<noOfCollectables; i++)
                {
                    if(occupancies[i].occupied)
                    {
                        float Xdistsq = (spawned[i].coordinateX - Xpos)*(spawned[i].coordinateX - Xpos);
                        float Zdistsq = (spawned[i].coordinateZ - Zpos)*(spawned[i].coordinateZ - Zpos);

                        if(((Xdistsq + Zdistsq) < collectionDistanceSq))
                        {
                            fuelComponent.CurrentFuel = ((fuelComponent.CurrentFuel + fuelVariable.FuelContained) > fuelComponent.MaxFuel) ? fuelComponent.MaxFuel : fuelVariable.FuelContained + fuelComponent.CurrentFuel;
                            occupancies[i] = falseVariable;
                            collectable.CollectablesSpawned--;
                            ecb.DestroyEntity(entityBuffer[i].entity);

                            m_entityQuery.SetSingleton<VehicleFuel>(fuelComponent);//Update vehicle fuel.
                        }                    
                    }
                }                
            }).Run();
        }
    }     
}