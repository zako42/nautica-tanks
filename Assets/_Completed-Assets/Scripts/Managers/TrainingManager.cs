using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;


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
        private Agent[] agents = new Agent[numPlayers];
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
                Vector3 spawnPosition = SpawnRandomizer.GetRandomSpawnPosition(tankManagers[i].m_SpawnPoint.gameObject);
                Vector3 spawnRotation = new Vector3(0f, SpawnRandomizer.GetRandomSpawnRotation(tankManagers[i].m_SpawnPoint.gameObject), 0f);
                GameObject tank = Instantiate(tankPrefabs[i], spawnPosition, Quaternion.Euler(spawnRotation));
                tank.name = "Player" + i.ToString();

                tankManagers[i].m_Instance = tank;
                tankManagers[i].m_PlayerNumber = i+1;
                tankManagers[i].Setup();

                // cache references
                tankGameobjects[i] = tank;
                tankAgents[i] = tank.GetComponent<ITankAgent>();
                agents[i] = tank.GetComponent<Agent>();
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
            for (int i=0; i < numPlayers; i++)
            {
                agents[i].EndEpisode();
                tankManagers[i].Reset();
                tankManagers[i].EnableControl();
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
            // this isn't the prettiest, but we need to reset the game if an agent gets to max steps
            // agent expects that it will reset and get a fresh episode when it reaches max steps so we need to reset the game
            // however, different agents may have different max_steps settings... so we need to just reset the whole game based on the agent with the least max_steps
            // this is not totally necessary, but in our case, the agent's target might have unlimited max_steps and it might have hidden far away on the map.
            // if we start a new episode and the target is hidden, it makes it harder for our agent to learn
            for (int i=0; i < numPlayers; i++)
            {
                if (agents[i].StepCount >= agents[i].MaxStep-1)
                {
                    resetgame = true;
                    break;
                }
            }

            // If we need to reset the game, do it here. Record any metrics or whatnot prior to ResetGame()
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
        /// For our training, we want to cut out these delays and run as fast as possible,
        /// so we force clean everything out manually.
        /// </summary>
        public void DestroyShells()
        {
            GameObject[] tank1shells = GameObject.FindGameObjectsWithTag("Tank1Shell");
            GameObject[] tank2shells = GameObject.FindGameObjectsWithTag("Tank2Shell");
            GameObject[] explosions = GameObject.FindGameObjectsWithTag("Explosion");

            foreach (var shellobject in tank1shells)
            {
                shellobject.SetActive(false);
                Destroy(shellobject.gameObject);
            }

            foreach (var shellobject in tank2shells)
            {
                shellobject.SetActive(false);
                Destroy(shellobject.gameObject);
            }

            foreach (var explosion in explosions)
            {
                ParticleSystem particles = explosion.GetComponent<ParticleSystem>();
                if (particles) particles.Stop();
                Destroy(explosion);
            }
        }
    }
}
