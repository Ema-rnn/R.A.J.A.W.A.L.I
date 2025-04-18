﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VSX.UniversalVehicleCombat.Radar;
using VSX.CameraSystem;

namespace VSX.UniversalVehicleCombat
{
    /// <summary>
    /// This script adds targeting functionality for the player vehicle, using Unity's Input System.
    /// </summary>
    public class PlayerInput_InputSystem_RadarControls : VehicleInput
    {

        protected GeneralInputAsset input;

        protected TargetSelector targetSelector;

        protected Tracker tracker;
        protected HUDCursor cursor;
        protected CameraTarget cameraTarget;




        protected override void Awake()
        {
            base.Awake();

            input = new GeneralInputAsset();

            // Link input to functions
            input.TargetingControls.TargetNext.performed += ctx => TargetNext();
            input.TargetingControls.TargetPrevious.performed += ctx => TargetPrevious();
            input.TargetingControls.TargetNearest.performed += ctx => TargetNearest();
            input.TargetingControls.TargetFront.performed += ctx => TargetFront();
            input.TargetingControls.TargetUnderCursor.performed += ctx => TargetUnderCursor();
        }


        protected virtual void OnEnable()
        {
            input.Enable();
        }


        protected virtual void OnDisable()
        {
            input.Disable();
        }


        protected override bool Initialize(Vehicle vehicle)
        {

            // Update the dependencies
            cursor = vehicle.GetComponentInChildren<HUDCursor>();
            cameraTarget = vehicle.GetComponentInChildren<CameraTarget>();
            tracker = vehicle.GetComponentInChildren<Tracker>();

            Weapons weapons = vehicle.GetComponentInChildren<Weapons>();
            if (weapons != null)
            {
                if (weapons.WeaponsTargetSelector != null)
                {
                    targetSelector = weapons.WeaponsTargetSelector;

                    if (debugInitialization)
                    {
                        Debug.Log(GetType().Name + " successfully initialized.");
                    }

                    return true;
                }
            }

            if (debugInitialization)
            {
                Debug.Log(GetType().Name + " failed to initialize. Failed to find a weapons target selector on the vehicle.");
            }

            return false;

        }


        /// <summary>
        /// Select the next target.
        /// </summary>
        protected virtual void TargetUnderCursor()
        {
            if (CanRunInput())
            {
                if (cursor != null && cameraTarget != null && cameraTarget.CameraEntity != null)
                {
                    targetSelector.SelectFront(cameraTarget.CameraEntity.transform.position, cursor.AimDirection);
                }
            }
        }


        /// <summary>
        /// Select the next target.
        /// </summary>
        protected virtual void TargetNext()
        {
            if (!CanRunInput()) return;
            
            targetSelector.Cycle(true);
            
        }

        /// <summary>
        /// Select the previous target.
        /// </summary>
        protected virtual void TargetPrevious()
        {
            if (!CanRunInput()) return;

            targetSelector.Cycle(false);           
        }

        /// <summary>
        /// Select the nearest target.
        /// </summary>
        protected virtual void TargetNearest()
        {
            if (!CanRunInput()) return;
            targetSelector.SelectNearest();
        }

        /// <summary>
        /// Select the target in front.
        /// </summary>
        protected virtual void TargetFront()
        {
            if (!CanRunInput()) return;
            targetSelector.SelectFront();
        }
    }
}

