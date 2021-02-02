using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


namespace TanksML {
    public abstract class TankAgentBase : Agent, ITankAgent
    {
        public int playerNumber = 1;
        public GameObject target;
        protected AgentTextDisplayer textOutput;
        private const string LOGTAG = "TankAgentBase";

        protected bool initialized = false;
        protected string vertAxis;
        protected string horzAxis;
        protected string fireButton;
        protected string strongFireButton;

        protected float moveValue;
        protected float turnValue;
        protected float fireValue;

        // ITankAgent interface methods
        public float GetTankMovementValue() => moveValue;
        public float GetTankTurnValue() => turnValue;
        public float GetTankFiredValue() => fireValue;
        public int GetPlayerNumber() => playerNumber;
        public void SetPlayerNumber(int number) => playerNumber = number;
        public string GetPlayerTag() => "Tank" + GetPlayerNumber().ToString();
        public GameObject GetTarget() => this.target;
        public void SetTarget(GameObject target) => this.target = target;

        // more interface methods below, these are abstract so your subclass needs to implement them
        public abstract void OnTankShellHit(Complete.ShellExplosion explosion, Dictionary<int, float> damages);
        public abstract void OnTakeDamage(float damage);
        public abstract void OnWinGame();
        public abstract void OnLoseGame();
        public abstract void OnDrawGame();


        protected virtual void Start()
        {
            // this stuff is to set up the input for heuristic controls (keyboard/gamepad control) you shouldn't need it for agent control
            vertAxis = "Vertical" + playerNumber.ToString();
            horzAxis = "Horizontal" + playerNumber.ToString();
            fireButton = "Fire" + playerNumber.ToString();
            strongFireButton = "StrongFire" + playerNumber.ToString();
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
            // These inputs are called "Vertical1", "Horizontal1", "Fire1", etc. and saved in member variables in Start()
            float move = Input.GetAxis(vertAxis);
            float turn = Input.GetAxis(horzAxis);

            // next we need to translate raw input into our expected DISCRETE action values

            // forward actions [0,1,2] where 0 = do nothing, 1 = move forward, 2 = move backward
            if (move < 0) actionsOut[0] = 1;  // any negative value gets set to discrete action 1 (move forward)
            else if (move > 0) actionsOut[0] = 2;  // any positive value gets set to discrete action 2 (move back)
            else actionsOut[0] = 0;  // otherwise, set discrete action 0 (do nothing)

            // turn actions [0,1,2] where 0 = nothing, 1 = turn left, 2 = turn right
            if (turn < 0) actionsOut[1] = 1;  // negative value is turn left
            else if (turn > 0) actionsOut[1] = 2;  // positive values are turn right
            else actionsOut[1] = 0;  // do nothing

            // for fire action, we have [0,1,2] where 0 = nothing, 1 = fire cannon, 2 = strong fire cannon (fire farther)
            // we could add more granularity to fire strengths, but not going to worry about this for our purposes
            // since player might press both buttons simultaneously, we prioritize strong fire button
            actionsOut[2] = 0;
            if (Input.GetButton(fireButton)) actionsOut[2] = 1;
            if (Input.GetButton(strongFireButton)) actionsOut[2] = 2;

            // The actionsOut[] values are sent to OnActionReceived above when there is no model available.
            // the values "fill in" the actions values that would have been computed by the model
        }
    }
}
