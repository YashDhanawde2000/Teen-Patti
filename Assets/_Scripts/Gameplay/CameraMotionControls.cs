using UnityEngine;
using System.Collections;
using Photon.Pun;
using UnityEngine.InputSystem;

public enum ZoomMode
{
    CameraFieldOfView,
    ZAxisDistance
}

public class CameraMotionControls : MonoBehaviour
{
    public float rotationSpeed = 0.1f;

    public float mouseSensitivity = 100f;
    public float speed = 12f;
    

    [Header("Manual Rotation")]
    [Tooltip("The smoothness coming to a stop of the camera after the user pans the camera and releases. Lower values result in significantly smoother results. This means the camera will take longer to stop rotating")]
    public float rotationSmoothing = 2f;
    [Tooltip("The object the camera will focus on.")]
    public Transform target;
    [Tooltip("How sensitive the camera-panning is when the user pans -- the speed of panning.")]
    public float rotationSensitivity = 1f;
    [Tooltip("The min and max distance along the Y-axis the camera is allowed to move when panning.")]
    public Vector2 rotationLimit = new Vector2(5, 80);
    [Tooltip("The position along the X-axis the camera game object is.")]
    public float xAxisDistance = 0f;
    [Tooltip("The position along the Y-axis the camera game object is.")]
    public float yAxisDistance = 1f;
    [Tooltip("The position along the Z-axis the camera game object is.")]
    public float zAxisDistance = 0.45f;

    [Header("Zooming")]
    [Tooltip("Whether the camera should zoom by adjusting it's FOV or by moving it closer/further along the z-axis")]
    public ZoomMode zoomMode = ZoomMode.CameraFieldOfView;
    [Tooltip("The minimum and maximum range the camera can zoon using the camera's FOV.")]
    public Vector2 cameraZoomRangeFOV = new Vector2(10, 60);
    [Tooltip("The minimum and maximum range the camera can zoon using the camera's z-axis position.")]
    public Vector2 cameraZoomRangeZAxis = new Vector2(10, 60);
    public float zoomSoothness = 10f;
    [Tooltip("How sensitive the camera zooming is -- the speed of the zooming.")]
    public float zoomSensitivity = 2;

    [Header("FPP Camera")]
    public int cameraIndex;
    public Camera fppCamera;
    public Camera mainCamera;
    public GameObject playerHead;

    private PhotonView pv;
    private float cameraFieldOfView;
    new private Transform transform;
    private float xVelocity;
    private float yVelocity;
    private float xRotationAxis;
    private float yRotationAxis;
    private float zoomVelocity;
    private float zoomVelocityZAxis;

    // For FPP Camera
    private float xRotation = 0f;
    private float yRotation = 0f;
    private float xMinRotation = -85f;
    private float xMaxRotation = 85f;
    private float yMinRotation = -75f;
    private float yMaxRotation = 75f;
    private Vector3 lastMousePosition;

    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        //mainCamera = GetComponent<Camera>();
        playerInputActions = new PlayerInputActions();
        pv = GetComponent<PhotonView>();
        transform = GetComponent<Transform>();
    }
    private void OnEnable()
    {
        playerInputActions.Player.SwitchCam.performed += OnSwitchCamPerformed;
        playerInputActions.Enable();
    }
    private void OnDisable()
    {
        playerInputActions.Player.SwitchCam.performed -= OnSwitchCamPerformed;
        playerInputActions.Disable();
    }

    private void Start()
    {
        if (!pv.IsMine)
        {
            // Disable camera for non-local players
            gameObject.SetActive(false);
            return;
        }
        if (pv.IsMine)
        {
            if (cameraIndex == 1) 
            { 
                gameObject.SetActive(false);
            }
        }

        cameraFieldOfView = mainCamera.fieldOfView;

        target = GameObject.FindGameObjectWithTag("CameraTarget").transform;
        // Sets the camera's rotation along the y axis.
        // The reason we're dividing by rotationSpeed is because we'll be multiplying by rotationSpeed in LateUpdate.
        // So we're just accounting for that at start.
        // xRotationAxis = startRotation / rotationSpeed;

        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        if (pv.IsMine && target && cameraIndex == 0)
        {
            Zoom();

        }

        if (pv.IsMine && cameraIndex == 1)
        {
            // Mouse input
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;

            xRotation = Mathf.Clamp(xRotation, xMinRotation, xMaxRotation);
            yRotation = Mathf.Clamp(yRotation, yMinRotation, yMaxRotation);

            transform.position = playerHead.transform.position; 
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            //playerBody.Rotate(Vector3.up * mouseX);

            // Keyboard input
            //float x = Input.GetAxis("Horizontal");
            //float z = Input.GetAxis("Vertical");
            //Vector3 move = transform.right * x + transform.forward * z;
            //playerBody.GetComponent<CharacterController>().Move(move * speed * Time.deltaTime);

            

                

        }

    }

    private void LateUpdate()
    {
        if (pv.IsMine && target && cameraIndex == 0)
        {
            Quaternion rotation;
            Vector3 position;
            float deltaTime = Time.deltaTime;

            // We only really want to capture the position of the cursor when the screen when the user is holding down left click/touching the screen
            // That's why we're checking for that before capturing the mouse/finger position.
            // Otherwise, on a computer, the camera would move whenever the cursor moves. 
            if (Input.GetMouseButton(0))
            {
                xVelocity += Input.GetAxis("Mouse X") * rotationSensitivity;
                yVelocity -= Input.GetAxis("Mouse Y") * rotationSensitivity;
            }

            xRotationAxis += xVelocity;
            yRotationAxis += yVelocity;

            // Clamp the rotation along the y-axis between the limits we set. 
            // Limits of 360 or -360 on any axis will allow the camera to rotate unrestricted
            yRotationAxis = ClampAngleBetweenMinAndMax(yRotationAxis, rotationLimit.x, rotationLimit.y);

            rotation = Quaternion.Euler(yRotationAxis, xRotationAxis * rotationSpeed, 0);
            position = rotation * new Vector3(xAxisDistance, yAxisDistance, -zAxisDistance) + target.position;

            transform.rotation = rotation;
            transform.position = position;

            xVelocity = Mathf.Lerp(xVelocity, 0, deltaTime * rotationSmoothing);
            yVelocity = Mathf.Lerp(yVelocity, 0, deltaTime * rotationSmoothing);

        }

    }

    private void Zoom()
    {
        float deltaTime = Time.deltaTime;

        /*If the user's on a touch screen device like:
        an Android iOS or Windows phone/tablet, we'll detect if there are two fingers touching the screen.
        If the touches are moving closer together from where they began, we zoom out.
        If the touches are moving further apart, then we zoom in.*/
#if UNITY_ANDROID || UNITY_IOS || UNITY_WSA
            if (Input.touchCount == 2) {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                float touchDeltaMag = (touch0.position - touch1.position).magnitude;
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                camera.fieldOfView = cameraFieldOfView;
                cameraFieldOfView += deltaMagnitudeDiff * zoomSensitivity;
                cameraFieldOfView = Mathf.Clamp(cameraFieldOfView, cameraZoomRangeFOV.x, cameraZoomRangeFOV.y);
            }
#endif

        //Zooms the camera in using the mouse scroll wheel
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            if (zoomMode == ZoomMode.CameraFieldOfView)
            {
                cameraFieldOfView = Mathf.SmoothDamp(cameraFieldOfView, cameraZoomRangeFOV.x, ref zoomVelocity, deltaTime * zoomSoothness);

                //prevents the field of view from going below the minimum value
                if (cameraFieldOfView <= cameraZoomRangeFOV.x)
                {
                    cameraFieldOfView = cameraZoomRangeFOV.x;
                }
            }
            else
            {
                if (zoomMode == ZoomMode.ZAxisDistance)
                {
                    zAxisDistance = Mathf.SmoothDamp(zAxisDistance, cameraZoomRangeZAxis.x, ref zoomVelocityZAxis, deltaTime * zoomSoothness);

                    //prevents the z axis distance from going below the minimum value
                    if (zAxisDistance <= cameraZoomRangeZAxis.x)
                    {
                        zAxisDistance = cameraZoomRangeZAxis.x;
                    }
                }
            }
        }
        else
        {
            //Zooms the camera out using the mouse scroll wheel
            if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                if (zoomMode == ZoomMode.CameraFieldOfView)
                {
                    cameraFieldOfView = Mathf.SmoothDamp(cameraFieldOfView, cameraZoomRangeFOV.y, ref zoomVelocity, deltaTime * zoomSoothness);

                    //prevents the field of view from exceeding the max value
                    if (cameraFieldOfView >= cameraZoomRangeFOV.y)
                    {
                        cameraFieldOfView = cameraZoomRangeFOV.y;
                    }
                }
                else
                {
                    if (zoomMode == ZoomMode.ZAxisDistance)
                    {
                        zAxisDistance = Mathf.SmoothDamp(zAxisDistance, cameraZoomRangeZAxis.y, ref zoomVelocityZAxis, deltaTime * zoomSoothness);

                        //prevents the z axis distance from exceeding the max value
                        if (zAxisDistance >= cameraZoomRangeZAxis.y)
                        {
                            zAxisDistance = cameraZoomRangeZAxis.y;
                        }
                    }
                }

            }
        }

        //We're just ensuring that when we're zooming using the camera's FOV, that the FOV will be updated to match the value we got when we scrolled.
        if (Input.GetAxis("Mouse ScrollWheel") > 0 || Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            mainCamera.fieldOfView = cameraFieldOfView;
        }
    }

    // Method to switch to the next camera
    public void SwitchToNextCamera()
    {
        if (cameraIndex == 0)
        {
            fppCamera.gameObject.SetActive(true);
            mainCamera.gameObject.SetActive(false);
        }
        else if (cameraIndex == 1)
        {
            mainCamera.gameObject.SetActive(true);
            fppCamera.gameObject.SetActive(false);
        }
    }

    private void OnSwitchCamPerformed(InputAction.CallbackContext context)
    {
        if (pv.IsMine && target && cameraIndex == 0)
        {
            // Check for V key press to switch to the next camera
            SwitchToNextCamera();

        }

        if (pv.IsMine && cameraIndex == 1)
        {
            // Check for V key press to switch to the next camera
            SwitchToNextCamera();
        }
    }

    // Prevents the camera from locking after rotating a certain amount if the rotation limits are set to 360 degrees.
    private float ClampAngleBetweenMinAndMax(float angle, float min, float max)
    {
        if (angle < -360)
        {
            angle += 360;
        }
        if (angle > 360)
        {
            angle -= 360;
        }
        return Mathf.Clamp(angle, min, max);
    }
}
