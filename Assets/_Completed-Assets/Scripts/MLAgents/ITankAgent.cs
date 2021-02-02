using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;


namespace TanksML {
    /// <summary>
    /// This interface has all the methods we will expect Tank Agents to implement.
    /// This way we can swap out agents for easier experimentation and testing.
    /// </summary>
    public interface ITankAgent
    {
        float GetTankMovementValue();
        float GetTankTurnValue();
        float GetTankFiredValue();

        int GetPlayerNumber();
        void SetPlayerNumber(int number);
        string GetPlayerTag();

        GameObject GetTarget();
        void SetTarget(GameObject target);

        // implement these methods to handle rewards in your agent however you want
        void OnTankShellHit(Complete.ShellExplosion explosion, Dictionary<int, float> damages);
        void OnTakeDamage(float damage);
        void OnWinGame();
        void OnLoseGame();
        void OnDrawGame();
    }
}
