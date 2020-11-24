using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TanksML {
    /// <summary>
    /// This component takes the place of the GameManager.
    /// Disable the existing GameManager and enable this for training our agents.
    /// We pretty much do the same stuff as GameManager, but cutting out all the stuff we don't need,
    /// like the coroutines, which could cause us issues with timing when we do our training.
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        public const int numPlayers = 2;

        public Complete.TankManager[] tankManagers = new Complete.TankManager[numPlayers];
        public GameObject[] tankPrefabs = new GameObject[numPlayers];
        private Complete.CameraControl cameraControl;

        private GameObject[] tankGameobjects = new GameObject[numPlayers];
        private ITankAgent[] tankAgents = new ITankAgent[numPlayers];
        private Complete.TankHealth[] tankHealth = new Complete.TankHealth[numPlayers];
        private bool resetgame = false;


        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            // need to do any reset book keeping?
            RunTraining();
        }

        /// <summary>
        /// One time initialization for everything.
        /// Further resets during training handled by ResetGame()
        /// </summary>
        public void Initialize()
        {
            SpawnTanks();
        }

        /// <summary>
        /// This is doing mostly the same thing the GameManager::SpawnAllTanks() does
        /// </summary>
        private void SpawnTanks()
        {
            for (int i=0; i < numPlayers; i++)
            {
                Debug.AssertFormat(tankPrefabs[i] != null, "TrainingManager: No tank prefab found");
                GameObject tank = Instantiate(tankPrefabs[i],
                    tankManagers[i].m_SpawnPoint.position,
                    tankManagers[i].m_SpawnPoint.rotation);
                tank.name = "Player" + i.ToString();
                // TODO: spawn in random point within region instead of at point

                tankManagers[i].m_Instance = tank;
                tankManagers[i].m_PlayerNumber = i+1;
                tankManagers[i].Setup();

                // cache references
                tankGameobjects[i] = tank;
                tankAgents[i] = tank.GetComponent<ITankAgent>();
                tankHealth[i] = tank.GetComponent<Complete.TankHealth>();
            }

            // not pretty, but can cleanup later if needed
            // set them to target each other -- for now assume only 2 tanks
            // if we want to set this up for more, will need to address later.
            // Note that we would also need to create agents which can handle more than 1 enemy as well 
            if (tankAgents[0] != null && tankAgents[1] != null)
            {
                tankAgents[0].SetTarget(tankGameobjects[1]);
                tankAgents[1].SetTarget(tankGameobjects[0]);
            }

            // setup the camera so it zooms out to fit all tanks in the scene
            cameraControl = GameObject.Find("CameraRig")?.GetComponent<Complete.CameraControl>();
            if (cameraControl)
            {
                Transform[] targets = new Transform[numPlayers];
                for (int i=0; i < numPlayers; i++)
                {
                    targets[i] = tankGameobjects[i].transform;
                }
                cameraControl.m_Targets = targets;
                cameraControl.SetStartPositionAndSize();
            }
        }

        private void ResetGame()
        {
            foreach (var tank in tankManagers)
            {
                tank.Reset();
                tank.EnableControl();
            }
            if (cameraControl) cameraControl.SetStartPositionAndSize();
        }

        private void EndGame()
        {
            foreach (var tank in tankManagers)
            {
                tank.DisableControl();
            }
        }
        
        public void RunTraining()
        {
            // if we need to reset the game, do it here.
            // record any metrics or whatnot prior to ResetGame()
            if (resetgame)
            {
                ResetGame();
                // does anything need to be done for Academy reset?
                resetgame = false;
            }

            int deadTanks = tankHealth.Count(t => t != null && t.Health <= 0f);
            if (deadTanks == numPlayers)  // DRAW GAME, everyone dead
            {
                EndGame();
                DestroyShells();
                resetgame = true;  // trigger reset next frame

                foreach (var tank in tankAgents)
                {
                    tank.OnDrawGame();
                }
            }
            else if (deadTanks == numPlayers - 1)  // only one tank alive, WIN GAME
            {
                EndGame();
                DestroyShells();
                resetgame = true;  // trigger reset next frame

                // winner OnWinGame(), losers OnLoseGame()
                for (int i=0; i < numPlayers; i++)
                {
                    if (tankHealth[i].Health > 0f)
                    {
                        tankAgents[i].OnWinGame();
                    }
                    else
                    {
                        tankAgents[i].OnLoseGame();
                    }
                }
            }

            // otherwise, multiple tanks alive, keep going
        }

        /// <summary>
        /// Destroy any shells in the air.
        /// This is needed when ending a training episode, because some shells might still be in the air,
        /// and they might hit a tank when we start the next episode (which is unfair).
        /// This wasn't needed by the normal Tanks game because when a player won,
        /// there were a couple seconds of UI ("you won", etc).
        /// For our training, we want to cut out these delays ane run as fast as possible,
        /// so we force clean everything out manually.
        /// </summary>
        public void DestroyShells()
        {
            GameObject[] shells = GameObject.FindGameObjectsWithTag("Shell");
            // Debug.unityLogger.Log(LOGTAG, "End game, found " + shells.Length.ToString() + " shells to destroy");

            foreach (var shellobject in shells)
            {
                shellobject.SetActive(false);
                Destroy(shellobject.gameObject);
            }
        }
    }
}
