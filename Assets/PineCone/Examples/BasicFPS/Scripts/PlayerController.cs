using UnityEngine;

namespace Pinecone.Examples.BasicFPS
{

    [RequireComponent(typeof(CharacterController))]
    public partial class PlayerController : NetworkBehaviour
    {
        private CharacterController controller;

        public float baseSpeed = 15f;
        public float gravity = -9.81f;
        public float mass = 70f;
        public float jumpHeight = 70f;
        public float CurrentSpeedMultiplier = 1.0f;

        public Transform groundCheck;
        public Transform cameraPos;
        public Renderer[] gfxRenderer;
        public float groundDistance = 0.4f;
        public LayerMask groundMask;

        public Animator animator;

        private float curSpeed;

        public Vector3 Velocity;
        private bool isGrounded;

        private Camera playerCamera;
        public Camera PlayerCamera => playerCamera;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public override void OnStart()
        {
            curSpeed = baseSpeed;

            if (HasAuthority)
            {
                playerCamera = Camera.main;
                playerCamera.gameObject.transform.SetParent(cameraPos);
                playerCamera.transform.position = cameraPos.position;
                playerCamera.transform.rotation = cameraPos.rotation;
                foreach (var r in gfxRenderer)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        public void SetRenderer(bool enable)
        {
            foreach (var r in gfxRenderer)
            {
                r.enabled = enable;
            }
        }

        // Update is called once per frame
        public void Update()
        {
            if (!HasAuthority)
                return;

            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && Velocity.y < 0)
            {
                Velocity.y = 0.0f;
                CurrentSpeedMultiplier = 1.0f;
            }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            if (CurrentSpeedMultiplier > 1.0f)
            {
                CurrentSpeedMultiplier -= Time.deltaTime;
                CurrentSpeedMultiplier = Mathf.Clamp(CurrentSpeedMultiplier, 1.0f, 100.0f);
            }

            curSpeed = baseSpeed * CurrentSpeedMultiplier;

            Vector3 move = transform.right * x + transform.forward * z;

            controller.Move(Vector3.ClampMagnitude(move, 1.0f) * curSpeed * Time.deltaTime);
            animator.SetBool("IsMoving", controller.velocity.magnitude > 0.05f);
            animator.SetBool("IsInAir", !isGrounded && curSpeed > 15f && controller.velocity.magnitude > 10f);

            if (Input.GetButtonDown("Jump") && isGrounded)
                Velocity.y = mass / Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (!isGrounded)
                Velocity.y += gravity * Time.deltaTime;

            controller.Move(Velocity * Time.deltaTime);
        }

        public void AddJumpHeight(float jumpToAdd)
        {
            Velocity.y = mass / Mathf.Sqrt(jumpToAdd * -2f * gravity);
        }

        public void SetSpeedMultiplier(float speedMultiplierAmount)
        {
            CurrentSpeedMultiplier = speedMultiplierAmount;
        }
    }
}
