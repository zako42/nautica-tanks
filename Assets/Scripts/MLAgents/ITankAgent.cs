using UnityEngine;
using Unity.MLAgents;


namespace TanksML {
    public interface ITankAgent
    {
        void SetPlayerNumber(int number);
        float GetTankMovementValue();
        float GetTankTurnValue();
        float GetTankFiredValue();
    }
}