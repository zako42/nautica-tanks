using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using TanksML;


/// <summary>
/// Agent to control Tank using ML Agents
/// </summary>
public class SimpleTankAgent : Agent, ITankAgent
{
    public int playerNumber = 1;
    public GameObject target;
    [SerializeField] private float movementSpeed = 12f;
    [SerializeField] private float turnSpeed = 180f;

    private bool initialized = false;
    private string vertAxis;
    private string horzAxis;
    private string fireButton;

    private float moveValue;
    private float turnValue;
    private float fireValue;


    // ITankAgent interface methods
    public void SetPlayerNumber(int number) => playerNumber = number;
    public void SetTarget(GameObject target) => this.target = target;
    public float GetTankMovementValue() => moveValue;
    public float GetTankTurnValue() => turnValue;
    public float GetTankFiredValue() => fireValue;


    void Awake()
    {
    }

    void Start()
    {
        vertAxis = "Vertical" + playerNumber.ToString();
        horzAxis = "Horizontal" + playerNumber.ToString();
        fireButton = "Fire" + playerNumber.ToString();

        Debug.AssertFormat(target, "Warning: Target not set");
        initialized = true;
    }

    public override void OnEpisodeBegin()
    {
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
            return;
        }

        // 4 observations:
        // relative angle to enemy, where 0 is dead-on, -1 is negative 180, +1 is positive 180
        // distance to opponent, normalized
        // relative angle the enemy is pointing, where 0 is dead-on, -1 is negative 180, +1 is positive 180
        // whether we have line-of-sight to the enemy (true/false)

        // calc relative angle between forward vector and vector from target to self, rotating about Y axis
        float relativeAngle = Vector3.SignedAngle(transform.forward,
            (target.transform.position - transform.position),
            Vector3.up);
        float relativeAngleObs = Mathf.Clamp(relativeAngle / 180f, -1f, 1f);
        sensor.AddObservation(relativeAngleObs);

        // calc relative angle of opponent the same way
        float enemyRelativeAngle = Vector3.SignedAngle(target.transform.forward,
            (transform.position - target.transform.position),
            Vector3.up);
        float enemyRelativeAngleObs = Mathf.Clamp(enemyRelativeAngle / 180f, -1f, 1f);
        sensor.AddObservation(enemyRelativeAngleObs);

        // calc distance
        float distance = Vector3.Distance(target.transform.position, transform.position);
        float normalizedDistance = distance / 70f;  // map hyponteneuse is roughly 70, use for normalizing
        float distanceObs = Mathf.Clamp(normalizedDistance, 0f, 1.0f);
        sensor.AddObservation(distanceObs);

        // determine LOS
        bool lineOfSight = false;

        // to determine LOS, we'll use a spherecast from tank to the enemy and see if it hits anything
        // spherecast is a raycast but with a sphere instead of a ray.

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

    private void FixedUpdate()
    {
    }
}
