using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TanksML;


namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        // public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        // public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        // public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        // public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.


        // private string m_FireButton;                // The input axis that is used for launching shells.
        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        // private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        // private bool m_Fired;                       // Whether or not the shell has been launched with this button press.

        // tanks-ml tutorial
        private const float fireNormalForce = 18f;  // note that this value is tuned so we can't move forward into our own shell explosion
        private const float fireStrongForce = 23f;
        private const float cooldownNormal = 0.4f;
        private const float cooldownStrong = 0.7f;
        public bool cooldown = false;
        private float cooldownTimer = 0f;
        private ITankAgent agent;
        [SerializeField] private bool agentControl = false;


        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
            m_CurrentLaunchForce = m_MinLaunchForce;
            // m_AimSlider.value = m_MinLaunchForce;
            cooldown = false;
            cooldownTimer = 0f;
        }


        public void Reset()
        {
            OnEnable();
        }

        
        private void Awake()
        {
            agent = GetComponent<ITankAgent>();
            if (agent == null)
            {
                agentControl = false;
                Debug.unityLogger.LogWarning("TankMovement", "No agent found");
            }
        }


        private void Start ()
        {
            // The fire axis is based on the player number.
            // m_FireButton = "Fire" + m_PlayerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            // m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }


        private void Update ()
        {
            // from original tanks tutorial, not using
            // The slider should have a default value of the minimum launch force.
            // m_AimSlider.value = m_MinLaunchForce;

            if (agentControl)
            {
                int fireaction = Mathf.RoundToInt(agent.GetTankFiredValue());

                if (cooldown)
                {
                    cooldownTimer -= Time.deltaTime;
                    // Debug.unityLogger.Log("cooldown: " + cooldownTimer.ToString());
                    if (cooldownTimer <= 0)
                    {
                        cooldown = false;
                        // Debug.unityLogger.Log("cooldown timer reset");
                    }
                }
                else
                {
                    if (fireaction == 1)
                    {
                        cooldown = true;
                        cooldownTimer = cooldownNormal;
                        m_CurrentLaunchForce = fireNormalForce;
                        Fire();
                        // Debug.unityLogger.Log("normal fire, set cooldown timer: " + cooldownTimer.ToString());
                    }
                    else if (fireaction == 2)
                    {
                        cooldown = true;
                        cooldownTimer = cooldownStrong;
                        m_CurrentLaunchForce = fireStrongForce;
                        Fire();
                        // Debug.unityLogger.Log("strong fire, set cooldown timer: " + cooldownTimer.ToString());
                    }
                }
            }
        }


        private void Fire ()
        {
            // Set the fired flag so only Fire is only called once.
            // m_Fired = true;

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance = Instantiate (m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            shellInstance.gameObject.tag = agent.GetPlayerTag() + "Shell";

            if (agent != null)
            {
                // For agents: register as a listener to the ShellExplosion::OnExplosion event.
                // This way we can use the damage data to set rewards.
                // Rewards set in agent's OnTankShellHit() method.
                var explosion = shellInstance.GetComponent<ShellExplosion>();
                if (explosion)
                {
                    explosion.OnExplosion += agent.OnTankShellHit;
                }
            }

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward; 

            // Change the clip to the firing clip and play it.
            // m_ShootingAudio.clip = m_FireClip;
            // m_ShootingAudio.Play ();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;
        }
    }
}