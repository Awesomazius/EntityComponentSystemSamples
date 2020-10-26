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
public class FuelRemovalSystem : SystemBase
{
    //JimK - Variables to measure
    private float deadSwitch;
    private float currSpeed;
    private float currentFuel;
    private float rate;

    //JimK - Variable to set.
    private float newfuel;


    

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }


    protected override void OnUpdate()
    {
        var newFuelComponent = new VehicleFuel{};

        var m_fuelquery = GetEntityQuery(typeof(VehicleFuel), typeof(ActiveVehicle));
        var fuelComponent = m_fuelquery.GetSingleton<VehicleFuel>();
        currentFuel = fuelComponent.CurrentFuel;         
        rate = fuelComponent.ConsumptionRate;

        var m_speedquery = GetEntityQuery(typeof(VehicleSpeed), typeof(ActiveVehicle));
        var speedComponent = m_speedquery.GetSingleton<VehicleSpeed>();
        currSpeed = speedComponent.DesiredSpeed;

        var m_switchQuery = GetEntityQuery(typeof(VehicleOn), typeof(ActiveVehicle));
        var switchComponent = m_switchQuery.GetSingleton<VehicleOn>();
        deadSwitch = switchComponent.deadSwitch;

        //JimK - variable to swap.
        var onSwitch = new VehicleOn{deadSwitch = 1.0f};
        var offSwitch = new VehicleOn{deadSwitch = 0.0f};



        if(currentFuel>0 && deadSwitch==0.0f)
        {
            //reset top speed throttle will work.
            m_switchQuery.SetSingleton(onSwitch);
        }

        if(currentFuel>0 && deadSwitch==1.0f)
        {
            //JimK - Edit component data and then set entity component.
            newFuelComponent.CurrentFuel = currentFuel - (rate * Time.DeltaTime * currSpeed);
            m_fuelquery.SetSingleton(newFuelComponent);
        }

        if(currentFuel<0 && deadSwitch==1.0f)
        {
            currentFuel = 0.0f;
            m_switchQuery.SetSingleton(offSwitch);
        }
            
    }    
};