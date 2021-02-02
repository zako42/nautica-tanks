using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using TanksML;


namespace TanksML {
    /// <summary>
    /// NOTE: this is still WIP, we might not use this, and it's still unfinished.
    /// 
    /// This is another quick helper class to initialize a visual agent's cameras and rendertextures.
    /// Again needed because of the way tanks runs using prefabs and player ID.
    /// We need to wire up the agent to its correct render textures based on its own player number.
    /// </summary>
    public class AgentCameraInitializer : MonoBehaviour
    {
        public RenderTexture normalRenderTexture;
        public RenderTexture segmentedRenderTexture;

        [SerializeField] private RenderTextureSensor renderTextureSensor;
        [SerializeField] private CameraSensor cameraSensor;
        [SerializeField] private Camera normalCam;
        [SerializeField] private Camera segmentedCam;

        [SerializeField] private RenderTexture[] normalRTs;
        [SerializeField] private RenderTexture[] segmentedRTs;
        [SerializeField] private RawImage[] normalCamUIimages;
        [SerializeField] private RawImage[] segmentedCamUIimages;

        private ITankAgent agent;


        void Start()
        {
            agent = GetComponent<ITankAgent>();
            if (cameraSensor == null) cameraSensor = GetComponent<CameraSensor>();

            // wire up to UI images normal/segmented based on player 1 or 2
            // yeah this isn't pretty, but we just need something quick.
            int playerNumber = agent.GetPlayerNumber() - 1;

            // might not need to use this, might be better off manually wiring up for visual agents

            normalRenderTexture = normalRTs[playerNumber];
            segmentedRenderTexture = segmentedRTs[playerNumber];

            normalCam.targetTexture = normalRenderTexture;
            cameraSensor.Camera = normalCam;
        }
    }
}
