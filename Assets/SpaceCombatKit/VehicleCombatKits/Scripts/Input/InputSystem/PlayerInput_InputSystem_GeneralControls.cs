﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VSX.GameStates;

namespace VSX.UniversalVehicleCombat
{
    /// <summary>
    /// Input script for controlling the steering and movement of a space fighter vehicle.
    /// </summary>
    public class PlayerInput_InputSystem_GeneralControls : MonoBehaviour
    {
        [SerializeField]
        protected GameState pauseGameState;

        protected GeneralInputAsset input;

  
        protected void Awake()
        {
           
            input = new GeneralInputAsset();


            input.GeneralControls.Menu.performed += ctx => PauseGame();
      
        }

        protected void PauseGame()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EnterGameState(pauseGameState);
            }
        }

        private void OnEnable()
        {
            input.Enable();
        }


        private void OnDisable()
        {
            input.Disable();
        }

    }
}