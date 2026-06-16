namespace EasyPeasyFirstPersonController
{
    using UnityEngine;
    using FMODUnity;

    public partial class FirstPersonController : MonoBehaviour
    {
        [Header("Audio (FMOD)")]
        public EventReference footstepEvent;
        public EventReference clothEvent;
        public string surfaceParameterName = "fs_material";

        [Header("Settings")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 5f;
        public float crouchSpeed = 1.5f;
        public float jumpSpeed = 4f;
        public float gravity = 9.81f;
        public float slideDuration = 0.7f;
        public float slideSpeed = 6f;
        public float mouseSensitivity = 2f;
        public float strafeTiltAmount = 2f;

        [Header("References")]
        public Transform playerCamera;
        public Transform cameraParent;
        public Transform groundCheck;
        public LayerMask groundMask;

        [HideInInspector] public CharacterController characterController;
        [HideInInspector] public IInputManager input;
        [HideInInspector] public Vector3 moveDirection;
        [HideInInspector] public bool isGrounded;

        private int previousStepCount = 0;
        private int previousClothesCount = 0;

        private PlayerBaseState currentState;
        private PlayerStateFactory states;
        private float xRotation = 0f;
        private float currentTilt;
        private float tiltVelocity;

        public PlayerBaseState CurrentState { get => currentState; set => currentState = value; }

        [Header("Visual Settings")]
        public float normalFov = 60f;
        public float sprintFov = 75f;
        public float slideFovBoost = 5f;
        public float fovChangeSpeed = 8f;
        public float bobAmount = 0.001f;
        public float bobSpeed = 10f;
        public float recoilReturnSpeed = 5f;

        [HideInInspector] public Camera cam;
        [HideInInspector] public float targetFov;
        [HideInInspector] public float currentBobIntensity;
        [HideInInspector] public float currentBobSpeed;
        [HideInInspector] public float targetTilt;

        private float bobTimer;
        private float fovVelocity;
        private float originalCamY;

        [Header("Height Settings")]
        public float standingCameraHeight = 1.75f;
        public float crouchingCameraHeight = 1f;
        public float crouchingCharacterControllerHeight = 1f;
        [HideInInspector] public float standingCharacterControllerHeight = 1.8f;
        [HideInInspector] public Vector3 standingCharacterControllerCenter = new Vector3(0, 0.9f, 0);
        [HideInInspector] public float targetCameraY;

        [Header("Ledge Settings")]
        public LayerMask ledgeLayer;
        public float ledgeDetectionDistance = 1f;
        private float landingMomentum;

        [Header("Swimming Settings")]
        public float swimSpeed = 4f;
        public float swimSprintSpeed = 6f;
        public float waterDrag = 2f;
        public LayerMask waterMask;
        [HideInInspector] public bool isInWater;

        [Header("Visual Preferences")]
        public bool useFovKick = true;
        public bool useHeadBob = true;
        public bool useCameraTilt = true;
        public bool useClimbTilt = true;

        [Header("Debug")]
        public bool currentStateDebug = true;

        void OnGUI()
        {
            if (currentState != null && Application.isEditor && currentStateDebug)
                GUILayout.Label("Current State: " + currentState.GetType().Name);
        }

        private void Awake()
        {
            cam = playerCamera.GetComponent<Camera>();
            targetFov = normalFov;
            targetCameraY = standingCameraHeight;
            originalCamY = standingCameraHeight;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            characterController = GetComponent<CharacterController>();
            standingCharacterControllerHeight = characterController.height;
            standingCharacterControllerCenter = characterController.center;
            input = GetComponent<IInputManager>();
            states = new PlayerStateFactory(this);

            currentState = states.Grounded();
            currentState.EnterState();
        }

        private void Update()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask, QueryTriggerInteraction.Ignore);

            currentState.UpdateState();
            HandleRotation();
            UpdateVisuals();
        }

        private void HandleRotation()
        {
            float mouseX = input.lookInput.x * mouseSensitivity;
            float mouseY = input.lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            float strafeTilt = useCameraTilt ? (-input.moveInput.x * strafeTiltAmount) : 0;
            float combinedTargetTilt = (useCameraTilt ? targetTilt : 0) + strafeTilt;

            currentTilt = Mathf.SmoothDamp(currentTilt, combinedTargetTilt, ref tiltVelocity, 0.1f);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0, currentTilt);
        }

        private int DetermineSurfaceType()
        {
            // 1. Prioritize water - if we are swimming/wading, return the water value
            if (isInWater)
                return 4;

            // 2. Shoot a short raycast down from the groundCheck to see what we are standing on
            if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, 0.5f, groundMask))
            {
                // Check the Tag of the object we hit
                switch (hit.collider.tag)
                {
                    case "Mud":
                        Debug.Log("Mud");
                        return 1;
                    case "Grass":
                        Debug.Log("Grass");
                        return 2;
                    case "Dirt":
                        Debug.Log("Dirt");
                        return 3;
                    case "Stone":
                        Debug.Log("Stone");
                        return 4;
                    case "Wood_Hollow":
                        Debug.Log("Wood Hollow");
                        return 5;

                    default:
                        Debug.Log("Wood");
                        return 0; // Default surface - Wood
                }
            }

            return 0; // Fallback default
        }

        private void PlayFootstep()
        {
            if (!footstepEvent.IsNull)
            {
                // 1. Create an instance of the FMOD event
                FMOD.Studio.EventInstance footstepInstance = FMODUnity.RuntimeManager.CreateInstance(footstepEvent);

                // 2. Set the 3D position so it sounds like it's coming from the feet
                footstepInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(groundCheck.position));

                // 3. Determine the surface and pass it to FMOD
                int surfaceIndex = DetermineSurfaceType();
                footstepInstance.setParameterByName(surfaceParameterName, surfaceIndex);

                // 4. Play the sound
                footstepInstance.start();

                // 5. CRUCIAL: Release the instance so it gets destroyed from memory after it finishes playing
                footstepInstance.release();
            }
        }

        private void PlayCloth()
        {
            if (!clothEvent.IsNull)
            {
                // 1. Create an instance of the FMOD event
                FMOD.Studio.EventInstance clothInstance = FMODUnity.RuntimeManager.CreateInstance(clothEvent);

                // 2. Set the 3D position so it sounds like it's coming from the feet
                clothInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(groundCheck.position));

                // 4. Play the sound
                clothInstance.start();

                // 5. CRUCIAL: Release the instance so it gets destroyed from memory after it finishes playing
                clothInstance.release();
            }
        }

        public void UpdateVisuals()
        {
            if (!useFovKick)
            {
                targetFov = normalFov;
            }
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, 1f / fovChangeSpeed);

            landingMomentum = Mathf.Lerp(landingMomentum, 0, Time.deltaTime * 10f);
            float newY = Mathf.Lerp(cameraParent.localPosition.y, targetCameraY, Time.deltaTime * 8f);

            if (useHeadBob && characterController.velocity.magnitude > 0.1f && isGrounded)
            {
                bobTimer += Time.deltaTime * currentBobSpeed;

                int currentStepCount = Mathf.FloorToInt(bobTimer / (Mathf.PI * 2f));
                if (currentStepCount > previousStepCount)
                {
                    PlayFootstep();
                    previousStepCount = currentStepCount;
                }

                int currentClothesCount = Mathf.FloorToInt((bobTimer + Mathf.PI) / (Mathf.PI * 2f));
                if (currentClothesCount > previousClothesCount)
                {
                    PlayCloth();
                    previousClothesCount = currentClothesCount;
                }

                float bobOffset = Mathf.Sin(bobTimer) * currentBobIntensity;
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, newY + bobOffset, cameraParent.localPosition.z);
            }
            else
            {
                bobTimer = 0;
                previousStepCount = 0;
                previousClothesCount = 0; 
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, newY, cameraParent.localPosition.z);
            }
        }
        public bool HasCeiling()
        {
            float radius = characterController.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (characterController.height - radius);
            float checkDistance = standingCharacterControllerHeight - characterController.height + 0.1f;

            return Physics.SphereCast(origin, radius, Vector3.up, out _, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
        }
        public bool CheckLedge(out Vector3 climbPosition)
        {
            climbPosition = Vector3.zero;
            RaycastHit wallHit;
            Vector3 wallOrigin = transform.position + Vector3.up * 1.5f;

            if (Physics.Raycast(wallOrigin, transform.forward, out wallHit, ledgeDetectionDistance, ledgeLayer, QueryTriggerInteraction.Ignore))
            {
                Vector3 ledgeOrigin = wallOrigin + Vector3.up * 0.6f + transform.forward * 0.2f;
                RaycastHit ledgeHit;

                if (!Physics.Raycast(ledgeOrigin, transform.forward, 0.5f, groundMask))
                {
                    if (Physics.Raycast(ledgeOrigin + transform.forward * 0.4f, Vector3.down, out ledgeHit, 1f, groundMask))
                    {
                        climbPosition = ledgeHit.point + Vector3.up * 1f;
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterMask) != 0)
            {
                isInWater = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterMask) != 0)
            {
                isInWater = false;
            }
        }

    }
}