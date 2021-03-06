﻿using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

namespace VRStandardAssets.Utils
{
    // This class works similarly to the SelectionRadial class except
    // it has a physical manifestation in the scene.  This can be
    // either a UI slider or a mesh with the SlidingUV shader.  The
    // functions as a bar that fills up whilst the user looks at it
    // and holds down the Fire1 button.
	public class SelectionSlider : MonoBehaviour
    {
		public event Action OnBarFilled;                                    // This event is triggered when the bar finishes filling.
        [SerializeField] private GameObject FPSController;

		[SerializeField] private float m_Duration = 2f;                     // The length of time it takes for the bar to fill.
		[SerializeField] private AudioSource m_Audio;                       // Reference to the audio source that will play effects when the user looks at it and when it fills.
		[SerializeField] private AudioClip m_OnOverClip;                    // The clip to play when the user looks at the bar.
		[SerializeField] private AudioClip m_OnFilledClip;                  // The clip to play when the bar finishes filling.
		[SerializeField] private Slider m_Slider;                           // Optional reference to the UI slider (unnecessary if using a standard Renderer).
		[SerializeField] private VRInteractiveItem m_InteractiveItem;       // Reference to the VRInteractiveItem to determine when to fill the bar.
		[SerializeField] private VRInput m_VRInput;                         // Reference to the VRInput to detect button presses.
		[SerializeField] private GameObject m_BarCanvas;                    // Optional reference to the GameObject that holds the slider (only necessary if DisappearOnBarFill is true).
		[SerializeField] private Renderer m_Renderer;                       // Optional reference to a renderer (unnecessary if using a UI slider).
		[SerializeField] private SelectionRadial m_SelectionRadial;         // Optional reference to the SelectionRadial, if non-null the duration of the SelectionRadial will be used instead.
		[SerializeField] private UIFader m_UIFader;                         // Optional reference to a UIFader, used if the SelectionSlider needs to fade out.
		[SerializeField] private Collider m_Collider;                       // Optional reference to the Collider used to detect the user's gaze, turned off when the UIFader is not visible.
		[SerializeField] private bool m_DisableOnBarFill;                   // Whether the bar should stop reacting once it's been filled (for single use bars).
		[SerializeField] private bool m_DisappearOnBarFill;                 // Whether the bar should disappear instantly once it's been filled.
        [SerializeField] private bool m_DisableOnClick;                     // Whether the bar should disable after the first click.
        [SerializeField] private bool m_LockMovementOnClick;                // Stop movement after button is clicked and until bar is filled.
        [SerializeField] private GameObject text;                           // Any text on the panel/slider
        [SerializeField] private SelectionSlider m_PairedSlider;
        [SerializeField] private AudioClip m_OnClickedClip;
        [SerializeField] private GameObject[] m_PreItems;
        [SerializeField] private GameObject[] m_PostItems;
        [SerializeField] private bool isDishes;
        [SerializeField] private bool isLaundry;

        /* Fields used for money dispenser */

        //[SerializeField] private bool m_IsComparorObject;
        //[SerializeField] private MoneyDispenser money;
        //[SerializeField] private PanelDispenserHandler dispenserHandler;

        [SerializeField] private Autowalk walkingScript;

        FirstPersonController fps;
        private bool inProgress;                                            // Whether the fill is currently in progress.
		private bool m_BarFilled;                                           // Whether the bar is currently filled.
		private bool m_GazeOver;                                            // Whether the user is currently looking at the bar.
		private float m_Timer;                                              // Used to determine how much of the bar should be filled.
		private Coroutine m_FillBarRoutine;                                 // Reference to the coroutine that controls the bar filling up, used to stop it if required.
        private Coroutine m_FillOtherBarRoutine;                                 // Reference to the coroutine that controls the bar filling up, used to stop it if required.
        private const string k_SliderMaterialPropertyName = "_SliderValue"; // The name of the property on the SlidingUV shader that needs to be changed in order for it to fill.
        private int fontSize;
        private float walkSpeed;
        private float runSpeed;
        public bool handWashed;
        public bool hasRun;

        private void Start ()
        {
            inProgress = false;
            fps = FPSController.GetComponent<FirstPersonController>();
            walkSpeed = fps.m_WalkSpeed;
            runSpeed = fps.m_RunSpeed;
            hasRun = false;
            handWashed = true;

            for (var i = 0; i < m_PostItems.Length; i++)
                m_PostItems[i].SetActive(false);
        }

		private void OnEnable ()
		{
			m_VRInput.OnDown += HandleDown;
			m_VRInput.OnUp += HandleUp;

			m_InteractiveItem.OnOver += HandleOver;
			m_InteractiveItem.OnOut += HandleOut;
		}


		private void OnDisable ()
		{
			m_VRInput.OnDown -= HandleDown;
			m_VRInput.OnUp -= HandleUp;

			m_InteractiveItem.OnOver -= HandleOver;
			m_InteractiveItem.OnOut -= HandleOut;
		}


		private void Update ()
		{
			if(!m_UIFader)
				return;

			// If this bar is using a UIFader turn off the collider when it's invisible.
			m_Collider.enabled = m_UIFader.Visible;
		}


		public IEnumerator WaitForBarToFill ()
		{
            // Disable the bar so user cannot repeatedly press it
            // This has been done with the inProgress variable
//            if (m_DisableOnClick)
//                enabled = false;

            // If the bar should disappear when it's filled, it needs to be visible now.
            if (m_BarCanvas && m_DisappearOnBarFill)
				m_BarCanvas.SetActive(true);

			// Currently the bar is unfilled.
			m_BarFilled = false;

			// Reset the timer and set the slider value as such.
			m_Timer = 0f;
			SetSliderValue (0f);

			// Keep coming back each frame until the bar is filled.
			while (!m_BarFilled)
			{
                //fps.m_WalkSpeed = 0;
				yield return null;
			}

			// If the bar should disappear once it's filled, turn it off.
			if (m_BarCanvas && m_DisappearOnBarFill)
				m_BarCanvas.SetActive(false);
		}


		public IEnumerator FillBar ()
		{
            hasRun = true;

            if (m_Duration > m_PairedSlider.m_Duration)
                handWashed = true;
            else
                handWashed = false;

            if (isLaundry)
                fps.SetLaundryDoneTrue();
            else if (isDishes)
                fps.SetDishesDoneTrue();

            inProgress = true;
            // Disable the bar so user cannot repeatedly press it
            if (m_DisableOnClick) { 
                //enabled = false;
            }

            // Disable movement until bar has been filled
            if (m_LockMovementOnClick) {
                fps.m_WalkSpeed = fps.m_RunSpeed = 0;
                walkingScript.setCanWalk(false);
            }

            AudioSource audio = gameObject.GetComponent<AudioSource>();
            audio.loop = true;
            audio.clip = m_OnClickedClip;
            audio.Play();

            // When the bar starts to fill, reset the timer.
            m_Timer = 0f;
            var newText = "\n Activity \n Progress: ";

            // The amount of time it takes to fill is either the duration set in the inspector, or the duration of the radial.
            float fillTime = m_SelectionRadial != null ? m_SelectionRadial.SelectionDuration : m_Duration;

            //Will force the autowalk to pause for the duration
            walkingScript.waitTime = (int) m_Duration;

			// Until the timer is greater than the fill time...
			while (m_Timer < fillTime)
			{
                // ... add to the timer the difference between frames.
                m_Timer += Time.deltaTime;
 
                text.GetComponent<Text>().text = newText + (int) ((m_Timer / fillTime) * 100) + "%";

				// Set the value of the slider or the UV based on the normalised time.
				SetSliderValue(m_Timer / fillTime);

				// Wait until next frame.
				yield return null;

				// We want to continue the loop ever if the user looks away.
				if (true)
					continue;

				// If the user is no longer looking at the bar, reset the timer and bar and leave the function.
				m_Timer = 0f;
				SetSliderValue (0f);
				yield break;
			}

            walkingScript.waitTime = 0;
            inProgress = false;
            audio.Pause();
            audio.loop = false;

			// Play the clip for when the bar is filled.
			m_Audio.clip = m_OnFilledClip;
			m_Audio.Play();
            
            text.GetComponent<Text>().text = "\n Activity \n Complete";

            for (var i = 0; i < m_PreItems.Length; i++)
                m_PreItems[i].SetActive(false);

            for (var i = 0; i < m_PostItems.Length; i++)
                m_PostItems[i].SetActive(true);

            //Set speeds back to normal
            walkingScript.setCanWalk(true);
            fps.m_WalkSpeed = walkSpeed;
            fps.m_RunSpeed = runSpeed;      
        }


		private void SetSliderValue (float sliderValue)
		{
			// If there is a slider component set it's value to the given slider value.
			if (m_Slider)
				m_Slider.value = sliderValue;

			// If there is a renderer set the shader's property to the given slider value.
			if(m_Renderer)
				m_Renderer.sharedMaterial.SetFloat (k_SliderMaterialPropertyName, sliderValue);
		}

        //To be called only by the paired slider
        public void InstantFill ()
        {
            hasRun = inProgress = true;
            
            SetSliderValue(1);
            text.GetComponent<Text>().text = "\n Activity \n Complete";
            if (m_Duration > m_PairedSlider.m_Duration)
                handWashed = false;
            else
                handWashed = true;

            inProgress = false;
        }

        private void HandleDown ()
		{
			// If the user is looking at the bar start the FillBar coroutine and store a reference to it.
			if (m_GazeOver && !hasRun) {
				m_FillBarRoutine = StartCoroutine(FillBar());

                if (m_PairedSlider != null)
                    m_PairedSlider.InstantFill();
            }
		}


		private void HandleUp ()
		{ }


		private void HandleOver ()
		{
			// The user is now looking at the bar.
			m_GazeOver = true;
			if (walkingScript != null) {
				walkingScript.setCanWalk(false);
			}

            // Play the clip appropriate for when the user starts looking at the bar.
            if (!inProgress)
            {
                m_Audio.clip = m_OnOverClip;
                m_Audio.Play();
            }
		}


		private void HandleOut ()
		{
			m_GazeOver = false;
			if (walkingScript != null) {
                walkingScript.setCanWalk(true);
			}
		}

        public bool GetHandWashed()
        {
            return handWashed;
        }

        /* Below functions were built for toggle functionality, left for reference */
        /*
		private void ToggleSelection() {
			Debug.Log("Toggle Button");

			ToggleSliderValue();
			if (m_IsComparorObject) {
				Debug.Log("Toggle Comparater Dispenser");
				dispenserHandler.toggleComparatorDispensing(gameObject, money);
			}
			else {
				Debug.Log("Toggle Regular Dispenser");
				dispenserHandler.toggleDispensing(money);
			}

			// Play the clip for when the bar is filled.
			m_Audio.clip = m_OnFilledClip;
			m_Audio.Play();
		}

		public void ToggleSliderValue() {
			if (m_Slider.value == m_Slider.maxValue) {
				m_Slider.value = m_Slider.minValue;
			}
			else {
				m_Slider.value = m_Slider.maxValue;
			}
		}
        */
    }
}