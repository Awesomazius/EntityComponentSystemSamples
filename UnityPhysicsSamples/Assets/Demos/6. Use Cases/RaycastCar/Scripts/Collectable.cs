using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


//JimK - Component to manage collectables.
struct Collectable : IComponentData 
{
    //JimK - Do all collectables rotate?
    // public float Rotationspeed; 
    public uint FuelCollectablesSpawned;

    public uint CollectablesSpawned;

    public int numberOfSpawnLocations;
}