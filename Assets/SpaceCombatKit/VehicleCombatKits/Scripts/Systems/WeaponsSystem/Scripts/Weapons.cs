﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VSX.UniversalVehicleCombat.Radar;
using UnityEngine.Events;

namespace VSX.UniversalVehicleCombat
{
    /// <summary>
    /// Manages the weapons loaded on a vehicle.
    /// </summary>
    public class Weapons : ModuleManager, ILeadTargetInfo
    {

        [SerializeField]
        protected AimController aimController;


        protected List<GunWeapon> gunWeapons = new List<GunWeapon>();
        public List<GunWeapon> GunWeapons
        {
            get { return gunWeapons; }
        }

        public float GunWeaponRange
        {
            get
            {
                float maxRange = 0;
                for(int i = 0; i < gunWeapons.Count; ++i)
                {
                    maxRange = Mathf.Max(maxRange, gunWeapons[i].Range);
                }

                return maxRange;
            }
        }

        protected List<MissileWeapon> missileWeapons = new List<MissileWeapon>();
        public List<MissileWeapon> MissileWeapons
        {
            get { return missileWeapons; }
        }

        [Tooltip("The target selector that the weapons use for lead target calculation and missile locking.")]
        [SerializeField]
        protected TargetSelector weaponsTargetSelector;
        public TargetSelector WeaponsTargetSelector { get { return weaponsTargetSelector; } }


        [Header("Turrets")]

        [SerializeField]
        protected bool overrideTurretsMode = true;

        [Tooltip("Targeting behavior for turrets.")]
        [SerializeField]
        protected TurretMode turretsMode;

        [Tooltip("The target for turrets.")]
        [SerializeField]
        protected Transform turretTargetTransform;

        [Tooltip("Whether turrets should snap to the target (rather that smoothly rotating toward it).")]
        [SerializeField]
        protected bool snapTurretsToTarget = false;

        protected List<Turret> turrets = new List<Turret>();
        public List<Turret> Turrets { get { return turrets; } }

        public virtual Transform Target
        {
            get
            {
                if (weaponsTargetSelector != null && weaponsTargetSelector.SelectedTarget != null)
                {
                    return weaponsTargetSelector.SelectedTarget.transform;
                }
                else
                {
                    return null;
                }
            }
        }

        protected Vector3[] leadTargetPositions;
        public Vector3[] LeadTargetPositions 
        { 
            get 
            { 
                if (weaponsTargetSelector != null && weaponsTargetSelector.SelectedTarget != null)
                {
                    return leadTargetPositions;
                }
                else
                {
                    return null;
                }
            } 
        }


        protected int leadTargetAimedIndex = -1;
        public virtual int LeadTargetAimedIndex
        {
            get { return leadTargetAimedIndex; }
        }


        protected float aimStateStartTime;
        public virtual float AimStateStartTime
        {
            get { return aimStateStartTime; }
        }


        protected override void Awake()
        {
            base.Awake();
            leadTargetPositions = new Vector3[0];

            if (aimController == null) aimController = GetComponentInChildren<AimController>();

            if (weaponsTargetSelector != null)
            {
                weaponsTargetSelector.onSelectedTargetChanged.AddListener((selectedTarget) => { UpdateLeadTargetPositions(true); });
            }
        }


        // Called when a module is mounted on one of the vehicle's module mounts.
        protected override void OnModuleMounted(Module module)
        { 
            // Look for gun weapons
            GunWeapon gunWeapon = module.GetComponentInChildren<GunWeapon>();
            if (gunWeapon != null)
            {
                // Store gun weapon reference
                if (!gunWeapons.Contains(gunWeapon))
                {
                    gunWeapons.Add(gunWeapon);
                }
            }

            MissileWeapon missileWeapon = module.GetComponentInChildren<MissileWeapon>();
            if (missileWeapon != null)
            {
                if (!missileWeapons.Contains(missileWeapon))
                {
                    missileWeapons.Add(missileWeapon);
                    if (weaponsTargetSelector != null) weaponsTargetSelector.onSelectedTargetChanged.AddListener(missileWeapon.TargetLocker.SetTarget);
                }
            }

            // Store gimbal controller reference
            Turret turret = module.GetComponentInChildren<Turret>();
            if (turret != null)
            {
                if (!turrets.Contains(turret))
                {
                    turrets.Add(turret);
                }
            }

            // Update lead target positions array
            if (leadTargetPositions.Length != gunWeapons.Count)
            {
                System.Array.Resize(ref leadTargetPositions, gunWeapons.Count);
            }
        }


        // Called when a module is unmounted from one of the vehicle's module mounts.
        protected override void OnModuleUnmounted(Module module)
        {
            
            // Unlink gimbaled weapons
            Turret turret = module.GetComponentInChildren<Turret>();
            if (turret != null)
            {
                if (turrets.Contains(turret))
                {
                    turrets.Remove(turret);
                }
            }

            // Unlink gun weapons
            GunWeapon gunWeapon = module.GetComponentInChildren<GunWeapon>();
            if (gunWeapon != null)
            {
                // Remove gun weapon reference
                if (gunWeapons.Contains(gunWeapon))
                {
                    gunWeapons.Remove(gunWeapon);
                }
            }

            // Unlink missile weapons
            MissileWeapon missileWeapon = module.GetComponentInChildren<MissileWeapon>();
            if (missileWeapon != null)
            {
                // Remove gun weapon reference
                if (missileWeapons.Contains(missileWeapon))
                {
                    missileWeapons.Remove(missileWeapon);
                    if (weaponsTargetSelector != null) weaponsTargetSelector.onSelectedTargetChanged.RemoveListener(missileWeapon.TargetLocker.SetTarget);
                }
            }
            
            // Update lead target positions array
            if (leadTargetPositions.Length != gunWeapons.Count)
            {
                System.Array.Resize(ref leadTargetPositions, gunWeapons.Count);
            }
        }


        public virtual Vector3 GetAverageLeadTargetPosition(Vector3 targetPosition, Vector3 targetVelocity)
        {

            if (gunWeapons.Count == 0) 
            {
                return targetPosition;
            }

            Vector3 leadTargetPosition = Vector3.zero;

            // Get the average lead target position
            for(int i = 0; i < gunWeapons.Count; ++i)
            {
                leadTargetPosition += gunWeapons[i].GetLeadTargetPosition(targetPosition, targetVelocity);
            }

            leadTargetPosition /= gunWeapons.Count;

            return leadTargetPosition;
        }


        protected virtual void UpdateLeadTargetPositions(bool reset)
        {
            if (weaponsTargetSelector == null || weaponsTargetSelector.SelectedTarget == null) return;

            if (reset)
            {
                leadTargetAimedIndex = -1;
            }

            int newLeadTargetAimedIndex = -1;
            Vector3 targetPos = weaponsTargetSelector.SelectedTarget.transform.TransformPoint(weaponsTargetSelector.SelectedTarget.TrackingBounds.center);
            for (int i = 0; i < gunWeapons.Count; ++i)
            {
                
                leadTargetPositions[i] = gunWeapons[i].GetLeadTargetPosition(targetPos, weaponsTargetSelector.SelectedTarget.Velocity);

                if (aimController != null && aimController.Aimed(leadTargetPositions[i]))
                {
                    if (newLeadTargetAimedIndex == -1 || newLeadTargetAimedIndex != leadTargetAimedIndex)   // Defer to the currently aimed index if there are multiple
                    {
                        newLeadTargetAimedIndex = i;
                    }
                }
            }

            if (newLeadTargetAimedIndex != leadTargetAimedIndex)
            {
                leadTargetAimedIndex = newLeadTargetAimedIndex;
                aimStateStartTime = Time.time;
            }
        }


        // Called every frame
        protected virtual void Update()
        {
            if (overrideTurretsMode)
            {
                for (int i = 0; i < turrets.Count; ++i)
                {
                    turrets[i].TurretMode = turretsMode;
                }

                switch (turretsMode)
                {
                    case TurretMode.Manual:

                        for (int i = 0; i < turrets.Count; ++i)
                        {
                            if (turretTargetTransform != null)
                            {
                                float angleToTarget;

                                turrets[i].GimbalController.TrackPosition(turretTargetTransform.position, out angleToTarget, snapTurretsToTarget);
                            }
                        }

                        break;

                    case TurretMode.Auto:

                        for (int i = 0; i < turrets.Count; ++i)
                        {
                            if (weaponsTargetSelector != null && weaponsTargetSelector.SelectedTarget != null)
                            {
                                turrets[i].SetTarget(weaponsTargetSelector.SelectedTarget);
                            }
                        }

                        break;
                }
            }

            UpdateLeadTargetPositions(false);
            
        }
    }
}