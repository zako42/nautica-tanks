using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace TanksML {
    /// <summary>
    /// quick helper class to visualize observation data for an agent.
    /// helps for debugging
    /// </summary>
    public class AgentTextDisplayer : MonoBehaviour
    {
        public string output;
        public bool debug = true;
        private Text[] outputUITexts = new Text[2];  // this is ugly but just for quick debugging
        private ITankAgent agent;
        private Text outputText;


        void Start()
        {
            outputUITexts[0] = GameObject.Find("AgentText1")?.GetComponent<Text>();
            outputUITexts[1] = GameObject.Find("AgentText2")?.GetComponent<Text>();
            agent = GetComponent<ITankAgent>();
            int textIndex = agent.GetPlayerNumber()-1;
            outputText = outputUITexts[textIndex];
        }

        void Update()
        {
            if (!debug || !outputText) return;
            outputText.text = output;
        }
    }
}
