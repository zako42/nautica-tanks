using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


namespace TanksML {
    /// <summary>
    /// This is a quick helper class to initialize the agent's perception sensors to track enemy shell and enemy tank.
    /// Needed because of the way the tanks (built from prefab) need to set their player ID at start.
    /// The enemy and their shells are setup to be based on their player number, i.e. Tank1 and Tank1Shell
    /// This sets the RayPerceptionSensor to use the enemy's tags (so we detect the enemy, not ourselves or our own shells)
    /// </summary>
    public class PerceptionInitializer : MonoBehaviour
    {
        [SerializeField] private RayPerceptionSensorComponent3D tankPerception;
        [SerializeField] private RayPerceptionSensorComponent3D shellPerception;
        private ITankAgent enemy;
        private const string LOGTAG = "PerceptionInitializer";


        void Start()
        {
            var agent = GetComponent<ITankAgent>();
            Debug.AssertFormat(agent != null, LOGTAG, "Could not find agent");
            if (agent != null)
            {
                GameObject enemy = agent.GetTarget();
                if (!enemy) return;
                var enemyAgent = enemy.GetComponent<ITankAgent>();

                if (tankPerception)
                {
                    string enemyTankTag = enemyAgent?.GetPlayerTag();
                    tankPerception.DetectableTags.Clear();
                    tankPerception.DetectableTags.Add(enemyTankTag);
                }

                if (shellPerception)
                {
                    string enemyShellTag = enemyAgent?.GetPlayerTag() + "Shell";
                    shellPerception.DetectableTags.Clear();
                    shellPerception.DetectableTags.Add(enemyShellTag);
                }
            }
        }
    }
}
