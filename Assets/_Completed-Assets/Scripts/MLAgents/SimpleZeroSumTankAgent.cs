using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


namespace TanksML {
    /// <summary>
    /// Agent to control Tank using ML Agents.
    /// Implements our ITankAgent interface.
    /// </summary>
    public class SimpleZeroSumTankAgent : Agent, ITankAgent
    {
        public int playerNumber = 1;
        public GameObject target;
        private const string LOGTAG = "SimpleZeroSumTankAgent";

        private bool initialized = false;
        private string vertAxis;
        private string horzAxis;
        private string fireButton;

        private float moveValue;
        private float turnValue;
        private float fireValue;

        private Complete.TankHealth health;
        private Complete.TankHealth targetHealth;
        private const float rewardPerDamage = 0.005f;  // max damage is 100, so max reward would be 0.5


        // ITankAgent interface methods
        public float GetTankMovementValue() => moveValue;
        public float GetTankTurnValue() => turnValue;
        public float GetTankFiredValue() => fireValue;
        public int GetPlayerNumber() => playerNumber;
        public void SetPlayerNumber(int number) => playerNumber = number;
        public string GetPlayerTag() => "Tank" + GetPlayerNumber().ToString();
        public GameObject GetTarget() => this.target;
        public void SetTarget(GameObject target) => this.target = target;


        /// <summary>
        /// Part of UnityEngine::Monobehavior (which is Base class of Agent and Unity's primary component class).
        /// Start() is called immediately before the first frame of the game is run and used for initialization.
        /// </summary>
        void Start()
        {
            // this stuff is to set up the input for heuristic controls (keyboard/gamepad control) you shouldn't need it for agent control
            vertAxis = "Vertical" + playerNumber.ToString();
            horzAxis = "Horizontal" + playerNumber.ToString();
            fireButton = "Fire" + playerNumber.ToString();

            // register ourself as a listener to the health OnTakeDamage event trigger
            health = GetComponent<Complete.TankHealth>();
            Debug.Assert(health, "Warning: could not get agent's Health component");
            if (health) health.OnTakeDamage += OnTakeDamage;

            targetHealth = target.GetComponent<Complete.TankHealth>();
            Debug.Assert(targetHealth, "Warning: could not get target's Health component");

            initialized = true;
        }

        /// <summary>
        /// This is an ML-agents framework method called when the agent starts a new training episode.
        /// Use this to do any housekeeping when agent is reset.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            Debug.unityLogger.Log(LOGTAG, gameObject.name.ToString() + " OnEpisodeBegin");
        }


        /// <summary>
        /// This is an ML-agents framework method called when the agent observes its environment.
        /// These observations are passed to the model as inputs.
        /// They make up the agent's STATE (S) at the current step, or time (t).
        /// The model will use them to decide action to perform.
        /// 
        /// NOTE: Aside from these manually created observations, we are also using
        ///     some others built-in to ML-agents, such as Ray Perception Sensor,
        ///     and stacking previous observations
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target)
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                return;
            }

            // Observations used by SimpleTankAgent (you can use whatever you want for your own agents)
            // 1. Relative angle to enemy, where 0 is dead-on, -1 is negative 180, +1 is positive 180
            // 2. Distance to opponent, normalized
            // 3. Relative angle the enemy is pointing, where 0 is dead-on, -1 is negative 180, +1 is positive 180
            // 4. Whether we have line-of-sight to the enemy (true/false)
            // 5. Our current Health, normalized from 0 to 1
            // 6. Enemy current Health, normalized from 0 to 1

            // calc relative angle between forward vector and vector from target to self, rotating about Y axis
            float relativeAngle = Vector3.SignedAngle(transform.forward,
                (target.transform.position - transform.position),
                Vector3.up);
            float relativeAngleObs = Mathf.Clamp(relativeAngle / 180f, -1f, 1f);
            sensor.AddObservation(relativeAngleObs);

            // calc distance
            float distance = Vector3.Distance(target.transform.position, transform.position);
            float normalizedDistance = distance / 70f;  // map hyponteneuse is roughly 70, use for normalizing
            float distanceObs = Mathf.Clamp(normalizedDistance, 0f, 1.0f);
            sensor.AddObservation(distanceObs);

            // calc relative angle of opponent the same way
            float enemyRelativeAngle = Vector3.SignedAngle(target.transform.forward,
                (transform.position - target.transform.position),
                Vector3.up);
            float enemyRelativeAngleObs = Mathf.Clamp(enemyRelativeAngle / 180f, -1f, 1f);
            sensor.AddObservation(enemyRelativeAngleObs);

            // to determine LOS, we'll use a spherecast from tank to the enemy and see if it hits anything
            // spherecast is a raycast but with a sphere instead of a ray.
            bool lineOfSight = false;

            // parameters for spherecast
            RaycastHit hit;
            const float spherecastRadius = 0.75f;
            Vector3 spherecastStart = transform.position;
            Vector3 spherecastDirection = target.transform.position - transform.position;
            float spherecastDistance = distance + 10f;

            // perform the spherecast with the parameters above
            if (Physics.SphereCast(spherecastStart, spherecastRadius, spherecastDirection, out hit, spherecastDistance))
            {
                // Returns the first collision found -- if we hit an obstacle before hitting the target, we have no LOS
                // if we did hit the target, there are no obstacles between us and them, so LOS is true
                if (hit.transform.Equals(target.transform))
                {
                    lineOfSight = true;
                }
            }
            sensor.AddObservation(lineOfSight);

            // our own health, normalized from 0-1
            if (health) sensor.AddObservation(health.NormalizedHealth);
            else sensor.AddObservation(0f);

            // target's health, normalized from 0-1
            if (targetHealth) sensor.AddObservation(targetHealth.NormalizedHealth);
            else sensor.AddObservation(0f);

            Unity.MLAgents.Monitor.Log(gameObject.name, GetCumulativeReward());
        }

        /// <summary>
        /// Event handler to process when our fired shell hit something.
        /// Note that in TankShooting.cs our agents is setup to listen whenever a new shell is fired,
        /// so we don't have to worry about that, this method will automagically be called.
        /// (just need to implement your rewards here, however you want)
        /// </summary>
        /// <param name="explosion">The ShellExplosion component sending the event</param>
        /// <param name="damages">Dictionary with { tank id : damage } damages dealt by explosion</param>
        public void OnTankShellHit(Complete.ShellExplosion explosion, Dictionary<int, float> damages)
        {

            foreach (var entry in damages)
            {
                if (entry.Key != this.playerNumber)
                {
                    AddReward(entry.Value * rewardPerDamage);
                    // Debug.unityLogger.Log(LOGTAG, gameObject.name + " HIT TARGET!  Reward = " + (entry.Value * rewardPerDamage).ToString());
                }
            }

            // housekeeping, remove listener since explosion will be destroyed
            if (explosion)
            {
                explosion.OnExplosion -= OnTankShellHit;  
            }
        }

        /// <summary>
        /// Event handler when agent observes a TankHealth::OnTakeDamage event
        /// (Note that this is wired up in Start() above)
        /// We use this to assess a reward/penalty when taking damage.
        /// </summary>
        /// <param name="damage">the amount of damage taken</param>
        public void OnTakeDamage(float damage)
        {
            AddReward(damage * -rewardPerDamage);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " TOOK DAMAGE!  Reward = " + (damage * penaltyPerDamage).ToString());
        }

        /// <summary>
        /// Called by TrainingManager::RunTraining() when it determines a winner/loser
        /// </summary>
        public void OnWinGame()
        {
            AddReward(1.0f);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " WON GAME! Total episode reward: " + GetCumulativeReward().ToString());
            EndEpisode();
        }

        /// <summary>
        /// Called by TrainingManager::RunTraining() when it determines a winner/loser
        /// </summary>
        public void OnLoseGame()
        {
            AddReward(-1.0f);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " LOST GAME! Total episode reward: " + GetCumulativeReward().ToString());
            EndEpisode();
        }

        /// <summary>
        /// Called by TrainingManager::RunTraining() when it determines a winner/loser
        /// </summary>
        public void OnDrawGame()
        {
            AddReward(0f);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " DRAW GAME! Total episode reward: " + GetCumulativeReward().ToString());
            EndEpisode();
        }

        /// <summary>
        /// This is an ML-agents framework method called when the agent receives decisions.
        /// The decisions can be from a model or from the Heuristic method below.
        /// We take these decisions and use them here to control the agent.
        /// In our case we take values for forward/back movement, left/right turning, and firing.
        /// Note that we store the values, and the TankMovement and TankShooting scripts use them
        /// </summary>
        /// <param name="vectorAction">array of actions, which were decided on</param>
        public override void OnActionReceived(float[] vectorAction)
        {
            if (!initialized) return;

            moveValue = vectorAction[0];
            turnValue = vectorAction[1];
            fireValue = vectorAction[2];
        }

        /// <summary>
        /// This is an ML-agents framework method called in the absence of a trained model.
        /// We are using it to allow human controls for testing.
        /// The output array would normally be populated by the model, in OnActionReceived() above
        /// In this case, we are manually setting them based on our human input controls.
        /// </summary>
        /// <param name="actionsOut">array of decided actions</param>
        public override void Heuristic(float[] actionsOut)
        {
            if (!initialized) return;

            // Gets inputs from Unity InputManager
            // In the unity InputManager, we have it set up to use keyboard keys like arrows or WASD
            // These inputs are called "Vertical1", "Horizontal1", "Fire1", etc.
            actionsOut[0] = Input.GetAxis(vertAxis);
            actionsOut[1] = Input.GetAxis(horzAxis);

            if (Input.GetButton(fireButton))
            {
                actionsOut[2] = 1f;
            }
            else
            {
                actionsOut[2] = 0f;
            }

            // The actionsOut[] values are sent to OnActionReceived above when there is no model available.
            // the values "fill in" the actions values that would have been computed by the model
        }
    }
}
