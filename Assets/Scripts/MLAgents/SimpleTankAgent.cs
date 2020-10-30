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
    private bool initialized = false;
    private string vertAxis;
    private string horzAxis;
    private string fireButton;

    private float moveValue;
    private float turnValue;
    private float fireValue;


    void Awake()
    {
    }

    void Start()
    {
        vertAxis = "Vertical" + playerNumber.ToString();
        horzAxis = "Horizontal" + playerNumber.ToString();
        fireButton = "Fire" + playerNumber.ToString();
        initialized = true;
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        base.OnActionReceived(vectorAction);
        if (!initialized) return;
    }

    /// <summary>
    /// This method is called in the absence of a trained model.
    /// We are using it to allow human controls for testing.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(float[] actionsOut)
    {
        if (!initialized) return;

        moveValue = Input.GetAxis(vertAxis);
        turnValue = Input.GetAxis(horzAxis);
        actionsOut[0] = moveValue;
        actionsOut[1] = turnValue;

        if (Input.GetButton(fireButton))
        {
            fireValue = 1f;
        }
        else
        {
            fireValue = 0f;
        }

        actionsOut[2] = fireValue;
    }

    public float GetTankMovementValue()
    {
        return moveValue;
    }

    public float GetTankTurnValue()
    {
        return turnValue;
    }

    public float GetTankFiredValue()
    {
        return fireValue;
    }

    public void SetPlayerNumber(int number)
    {
        playerNumber = number;
    }

    private void FixedUpdate()
    {
    }
}
