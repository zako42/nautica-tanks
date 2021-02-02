using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


namespace TanksML {
    /// <summary>
    /// Agent to control Tank using ML Agents.
    /// Inherits from abstract class TankAgentBase.
    /// TankAgentBase does some housekeeping for us so we don't need to worry about some things,
    /// probably could be cleaner and better encapsulated, etc. but not enough time.
    /// If you want to create a brand new agent, derive it from TankAgentBase.
    /// Otherwise, feel free to take this agent and mess with it and tweak.
    /// </summary>
    public class SimpleTankAgent : TankAgentBase
    {
        public bool debug = true;
        private Complete.TankHealth health;
        private Complete.TankHealth targetHealth;
        private Complete.TankShooting shooting;

        private const string LOGTAG = "SimpleTankAgent";


        /// <summary>
        /// Part of UnityEngine::Monobehavior (which is Base class of Agent and Unity's primary component class).
        /// Start() is called immediately before the first frame of the game is run and used for initialization.
        /// Typically, you cache references here into member variables for easy reference later.
        /// Note that during training, Start() will only get called once, at beginning of training.
        /// If you want to initialize something for each episode, use OnEpisodeBegin()
        /// </summary>
        protected override void Start()
        {
            base.Start(); // TankAgentBase does some hosuekeeping for us

            // you can add code to do one-time initialization here
            // for this agent, we're adding health related stuff so we can reward based on damages done

            // register ourself as a listener to the health OnTakeDamage event trigger
            health = GetComponent<Complete.TankHealth>();
            Debug.Assert(health, "Warning: could not get agent's TankHealth component");
            if (health) health.OnTakeDamage += OnTakeDamage;

            shooting = GetComponent<Complete.TankShooting>();
            Debug.Assert(health, "Warning: could not get agent's TankShooting component");

            targetHealth = target.GetComponent<Complete.TankHealth>();
            Debug.Assert(targetHealth, "Warning: could not get target's Health component");

            textOutput = GetComponent<AgentTextDisplayer>();
            Debug.Assert(textOutput, "Warning: could not get AgentTextDisplayer component");

            initialized = true;
        }

        /// <summary>
        /// This is an ML-agents framework method called when the agent starts a new training episode.
        /// Use this to do any housekeeping when agent is reset between episodes
        /// </summary>
        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();
            // we're not doing anything here, but put stuff here if you want to mess around
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
        /// <param name="sensor">part of ML Agents API</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target)
            {
                // if no target, we're in a bad state, so just pass data with all 0
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
            // 4. Our current Health, normalized from 0 to 1
            // 5. Enemy current Health, normalized from 0 to 1
            // 6. If gun is cooling down, true/false

            /* 1 */
            // calc relative angle between forward vector and vector from target to self, rotating about Y axis
            float relativeAngle = Vector3.SignedAngle(transform.forward,
                (target.transform.position - transform.position),
                Vector3.up);
            float relativeAngleObs = Mathf.Clamp(relativeAngle / 180f, -1f, 1f);
            sensor.AddObservation(relativeAngleObs);

            // we're adding a reward penalty here for when we're facing away from enemy,
            // hoping agent learns to generally face enemy to move toward them or fire at them
            if (relativeAngleObs < -0.5f || relativeAngleObs > 0.5f)
            {
                AddReward(-0.001f);
            }

            /* 2 */
            // calc distance
            float distance = Vector3.Distance(target.transform.position, transform.position);
            float normalizedDistance = distance / 70f;  // map hyponteneuse is roughly 70, use for normalizing
            float distanceObs = Mathf.Clamp(normalizedDistance, 0f, 1.0f);
            sensor.AddObservation(distanceObs);

            /* 3 */
            // calc relative angle of opponent the same way
            float enemyRelativeAngle = Vector3.SignedAngle(target.transform.forward,
                (transform.position - target.transform.position),
                Vector3.up);
            float enemyRelativeAngleObs = Mathf.Clamp(enemyRelativeAngle / 180f, -1f, 1f);
            sensor.AddObservation(enemyRelativeAngleObs);

            /* 4 */
            // our own health, normalized from 0-1
            if (health) sensor.AddObservation(health.NormalizedHealth);
            else sensor.AddObservation(0f);

            /* 5 */
            // target's health, normalized from 0-1
            if (targetHealth) sensor.AddObservation(targetHealth.NormalizedHealth);
            else sensor.AddObservation(0f);

            /* 6 */
            // observe whether our gun is cooling down and can't fire
            // this might not be useful, but agent might learn difference between weak/strong shots,
            // and it might want to fire faster/slower depending on situation
            if (shooting) sensor.AddObservation(shooting.cooldown);
            else sensor.AddObservation(0f);

            AddReward(-0.0001f);  // tiny negative reward over time to incentivize agent to hurry up

            // do some debug outputting here
            if (debug && textOutput)
            {
                textOutput.output = "<b>Agent" + playerNumber.ToString() + "</b>\n";
                textOutput.output += "<b>Relative Angle: </b>" + relativeAngleObs.ToString() + "\n";
                textOutput.output += "<b>Distance: </b>" + distanceObs.ToString() + "\n";
                textOutput.output += "<b>Enemy Relative Heading: </b>" + enemyRelativeAngleObs.ToString() + "\n";
                textOutput.output += "<b>Health: </b>" + health.NormalizedHealth.ToString() + "\n";
                textOutput.output += "<b>Enemy Health: </b>" + targetHealth.NormalizedHealth.ToString() + "\n";
                textOutput.output += "<b>Cannon Cooldown: </b>" + shooting.cooldown.ToString() + "\n";
                textOutput.output += "<b>Total Reward: </b>" + GetCumulativeReward().ToString() + "\n";
            }

            // Unity.MLAgents.Monitor.Log(gameObject.name, GetCumulativeReward());
        }

        // when using discrete actions, we can MASK actions that are impossible,
        // so the agent doesn't try to perform them.
        // when the tank cannon is cooling down, it is not possible for the agent to fire,
        // so we are masking our discrete actions for branch 3, actions 1 and 2 (fire weak, fire strong)
        // this is not critical, but may help with learning
        public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
        {
            base.CollectDiscreteActionMasks(actionMasker);
            if (shooting && shooting.cooldown)
            {
                int[] firingActions = { 1, 2 };
                actionMasker.SetMask(2, firingActions);
            }
        }

        /// <summary>
        /// Event handler to process when our fired shell hit something.
        /// Note that in TankShooting.cs our agents is setup to listen whenever a new shell is fired,
        /// so we don't have to worry about that, this method will automagically be called.
        /// (just need to implement your rewards here, however you want)
        /// </summary>
        /// <param name="explosion">The ShellExplosion component sending the event</param>
        /// <param name="damages">Dictionary with { tank id : damage } damages dealt by explosion</param>
        public override void OnTankShellHit(Complete.ShellExplosion explosion, Dictionary<int, float> damages)
        {
            const float rewardPerDamage = 0.005f;  // max damage is 100, so max reward would be 0.5
            const float selfDamageFactor = -1.5f;  // anytime we damage ourself, assess large penalty

            foreach (var entry in damages)
            {
                if (entry.Key == this.playerNumber)
                {
                    // argh... we hit ourself!!
                    AddReward(entry.Value * rewardPerDamage * selfDamageFactor);
                    // Debug.unityLogger.Log(LOGTAG, gameObject.name + " HIT OURSELF!  Reward = " + (entry.Value * rewardPerDamage * selfDamageFactor).ToString());
                }
                else
                {
                    // yay... we hit someone else!!
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
        public override void OnTakeDamage(float damage)
        {
            const float penaltyPerDamage = -0.005f;  // max damage is 100, so max penalty is -0.5
            AddReward(damage * penaltyPerDamage);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " TOOK DAMAGE!  Reward = " + (damage * penaltyPerDamage).ToString());
        }

        /// <summary>
        /// Called by TrainingManager::RunTraining() when it determines a winner/loser
        /// </summary>
        public override void OnWinGame()
        {
            AddReward(1.0f);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " WON GAME! Total episode reward: " + GetCumulativeReward().ToString());
            EndEpisode();
        }

        /// <summary>
        /// Called by TrainingManager::RunTraining() when it determines a winner/loser
        /// </summary>
        public override void OnLoseGame()
        {
            AddReward(-1.0f);
            // Debug.unityLogger.Log(LOGTAG, gameObject.name + " LOST GAME! Total episode reward: " + GetCumulativeReward().ToString());
            EndEpisode();
        }

        /// <summary>
        /// Called by TrainingManager::RunTraining() when it determines a winner/loser
        /// </summary>
        public override void OnDrawGame()
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

            // note that the DISCRETE values we receive need to be translated back to input values that the other tank scripts are expecting
            // they were designed in the original tanks tutorial to be input axis values from -1 to +1, NOT 0,1,2, etc
            // sorry this is a bit confusing, but we're trying to avoid messing with the original tanks code too much which makes things a bit weird here
            // the values here are used in TankMovement.cs and TankShooting.cs scripts as mentioned above
            switch (Mathf.RoundToInt(vectorAction[0]))
            {
                case 0 : moveValue = 0f; break;
                case 1 : moveValue = -1f; break;
                case 2 : moveValue = 1f; break;
                default : moveValue = 0f; break;
            }

            switch (Mathf.RoundToInt(vectorAction[1]))
            {
                case 0 : turnValue = 0f; break;
                case 1 : turnValue = -1f; break;
                case 2 : turnValue = 1f; break;
                default : turnValue = 0f; break;
            }

            // for fire button, we had to alter the orignal tank code, so we can keep using our discrete value
            fireValue = vectorAction[2];
        }

        /// <summary>
        /// This is an ML-agents framework method called in the absence of a trained model.
        /// We are using it to allow human controls for testing.
        /// The output array would normally be populated by the model, in OnActionReceived() above
        /// In this case, we are manually setting them based on our human input controls.
        /// If you change Behavior Parameters -> Behavior Type to "Heuristic Only", this method is called.
        /// </summary>
        /// <param name="actionsOut">array of decided actions</param>

        public override void Heuristic(float[] actionsOut)
        {
            // this is already done in TankAgentBase, you don't need to do anything here
            // unless you want to mess with it
            base.Heuristic(actionsOut);
        }
    }
}
