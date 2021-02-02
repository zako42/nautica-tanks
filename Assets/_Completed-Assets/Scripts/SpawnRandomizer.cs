using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TanksML {
    /// <summary>
    /// Helper class to get a random spawnpoint position and rotation within a given region.
    /// We'll define the region by using a gameobject's position as a centerpoint, its rotation,
    /// and use the local scale X and Z to define a rectangular region.
    /// </summary>
    public class SpawnRandomizer
    {
        private const float rotationMaxRange = 90.0f;

        /// <summary>
        /// Get a random point within the given spawn region.
        /// </summary>
        /// <param name="spawnRegion">game object whose transform we will use</param>
        /// <returns>Vector3 with x,y,z position randomized within the given region</returns>
        public static Vector3 GetRandomSpawnPosition(GameObject spawnRegion)
        {
            Vector3 randomPosition = new Vector3();
            // get randomized values (all using local space coordinates)
            randomPosition.x = Random.Range(-spawnRegion.transform.localScale.x / 2, spawnRegion.transform.localScale.x / 2);
            randomPosition.y = 0f;
            randomPosition.z = Random.Range(-spawnRegion.transform.localScale.z / 2, spawnRegion.transform.localScale.z / 2);

            // Take the local coordinates and transform to world space
            // NOTE: we need to make sure not to use scale vector, or it could end up outside the region
            var transformMatrix = Matrix4x4.TRS(spawnRegion.transform.position, spawnRegion.transform.rotation, Vector3.one);
            return transformMatrix.MultiplyPoint3x4(randomPosition);
        }

        /// <summary>
        /// Get a random angle based on the given spawn region's current angle,
        /// and the constant rotationMaxRange.
        /// </summary>
        /// <param name="spawnRegion"></param>
        /// <returns></returns>
        public static float GetRandomSpawnRotation(GameObject spawnRegion)
        {
            int minAngle = Mathf.RoundToInt(spawnRegion.transform.rotation.eulerAngles.y - rotationMaxRange / 2);
            int maxAngle = Mathf.RoundToInt(spawnRegion.transform.rotation.eulerAngles.y + rotationMaxRange / 2);
            return (float)(Random.Range(minAngle, maxAngle) % 360);
        }
    }
}
