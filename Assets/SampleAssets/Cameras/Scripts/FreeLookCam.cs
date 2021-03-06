using System;
using UnityEngine;
using UnitySampleAssets.CrossPlatformInput;
#if UNITY_EDITOR

#endif

namespace UnitySampleAssets.Cameras
{
    public class FreeLookCam : PivotBasedCameraRig
    {
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 	Camera Rig
        // 		Pivot
        // 			Camera

        [SerializeField] private float m_MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
        [Range(0f, 10f)] [SerializeField] private float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
        [SerializeField] private float m_TurnSmoothing = 0.1f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
        [SerializeField] private float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
        [SerializeField] private float m_TiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
        [SerializeField] private bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
        [SerializeField] private bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return

        private float m_LookAngle;                    // The rig's y axis rotation.
        private float m_TiltAngle;                    // The pivot's x axis rotation.
        private const float k_LookDistance = 100f;    // How far in front of the pivot the character's look target is.
        private float m_SmoothX = 0;
        private float m_SmoothY = 0;
        private float m_SmoothXvelocity = 0;
        private float m_SmoothYvelocity = 0;
		private Vector3 m_PivotEulers;

        protected override void Awake()
        {
            base.Awake();
            // Lock or unlock the cursor.
            Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !m_LockCursor;
			m_PivotEulers = m_Pivot.rotation.eulerAngles;
        }


        protected void Update()
        {
            HandleRotationMovement();
            if (m_LockCursor && Input.GetMouseButtonUp(0))
            {
                Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !m_LockCursor;
            }
        }


        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        protected override void FollowTarget(float deltaTime)
        {
            if (m_Target == null) return;
            // Move the rig towards target position.
            transform.position = Vector3.Lerp(transform.position, m_Target.position, deltaTime*m_MoveSpeed);
        }


        private void HandleRotationMovement()
        {
			if(Time.timeScale < float.Epsilon)
				return;

            // Read the user input
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");

            // smooth the user input
            if (m_TurnSmoothing > 0)
            {
                m_SmoothX = Mathf.SmoothDamp(m_SmoothX, x, ref m_SmoothXvelocity, m_TurnSmoothing);
                m_SmoothY = Mathf.SmoothDamp(m_SmoothY, y, ref m_SmoothYvelocity, m_TurnSmoothing);
            }
            else
            {
                m_SmoothX = x;
                m_SmoothY = y;
            }

            // Adjust the look angle by an amount proportional to the turn speed and horizontal input.
            m_LookAngle += m_SmoothX*m_TurnSpeed;

            // Rotate the rig (the root object) around Y axis only:
            transform.rotation = Quaternion.Euler(0f, m_LookAngle, 0f);

            if (m_VerticalAutoReturn)
            {
                // For tilt input, we need to behave differently depending on whether we're using mouse or touch input:
                // on mobile, vertical input is directly mapped to tilt value, so it springs back automatically when the look input is released
                // we have to test whether above or below zero because we want to auto-return to zero even if min and max are not symmetrical.
                m_TiltAngle = y > 0 ? Mathf.Lerp(0, -m_TiltMin, m_SmoothY) : Mathf.Lerp(0, m_TiltMax, -m_SmoothY);
            }
            else
            {
                // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
                m_TiltAngle -= m_SmoothY*m_TurnSpeed;
                // and make sure the new value is within the tilt range
                m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);
            }

            // Tilt input around X is applied to the pivot (the child of this object)
			m_Pivot.localRotation = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y , m_PivotEulers.z);
        }
    }
}
