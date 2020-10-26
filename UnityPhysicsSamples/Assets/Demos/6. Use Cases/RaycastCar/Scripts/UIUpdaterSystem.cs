using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Physics.Extensions;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UI;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System;
using System.Collections.Generic;
using Unity.Physics.Systems;
using RaycastHit = Unity.Physics.RaycastHit;



[UpdateInGroup(typeof(SimulationSystemGroup))]
public class UIUpdaterSystem : SystemBase
{
    EntityCommandBufferSystem m_commandBufferSystem;
    EntityCommandBuffer ecb;


    //JimK - OOP references
    private GameObject gameObject;
    private Text textObj;

    //JimK - cached amount of fuel.
    float cachedFuel;

    protected override void OnStartRunning()
    {
        SetupUIobj();
        base.OnStartRunning();
    }

    protected override void OnUpdate()
    {
        var m_query = GetEntityQuery(typeof(VehicleFuel), typeof(ActiveVehicle));
        var fuelQuery = m_query.GetSingleton<VehicleFuel>();
        float fuelQueryValue = fuelQuery.CurrentFuel;
        float maxfuelQueryValue = fuelQuery.MaxFuel;

        //JimK - query for changed values within a threshold.
        if(cachedFuel - fuelQueryValue > 0.01f || cachedFuel - fuelQueryValue< -0.01f)
        {
                ChangeText(fuelQueryValue, maxfuelQueryValue);  
                cachedFuel = fuelQueryValue;
        }
    }

    public void SetupUIobj()
    {
        gameObject = GameObject.Find("RemainingFuelText");
        textObj = gameObject.GetComponent<Text>();
    }

    public void ChangeText(float fuel, float maxFuel)
    {
        textObj.text = "Fuel: " + fuel.ToString("F1") +" / " + maxFuel.ToString("F1") + " litres";
    }    
};