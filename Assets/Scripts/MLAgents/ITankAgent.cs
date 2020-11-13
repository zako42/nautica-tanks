using UnityEngine;
using Unity.MLAgents;


namespace TanksML {
    public interface ITankAgent
    {
        void SetPlayerNumber(int number);
        void SetTarget(GameObject target);
        float GetTankMovementValue();
        float GetTankTurnValue();
        float GetTankFiredValue();
    }
}