using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController2 : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("�ƶ��ٶ�")]
    public float MoveSpeed = 2.0f;
    [Tooltip("�����ƶ��ٶ�")]
    public float SprintSpeed = 5.335f;
    [Tooltip("��ת�ٶ�")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;
    [Tooltip("���ٶ�")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("��Ծ�߶�")]
    public float JumpHeight = 1.2f;
    [Tooltip("������Ĭ��Ϊ -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("��Ծ���ʱ��")]
    public float JumpTimeout = 0.50f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("��ǰ�Ƿ��ڵ���")]
    public bool Grounded = true;
    [Tooltip("�ֲڵ���ƫ����")]
    public float GroundedOffset = -0.14f;
    [Tooltip("������İ뾶��Ӧ�ú�CharacterController�İ뾶ƥ��")]
    public float GroundedRadius = 0.28f;
    [Tooltip("��������Щ��")]
    public LayerMask GroundLayers;

    [Header("CinemachineTarget")]
    [Tooltip("�������Ŀ��")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("�������")]
    public float TopClamp = 80;
    [Tooltip("��С����")]
    public float BottomClamp = -80;
    [Tooltip("����ĽǶ�����������������뵱�����ס��ʱ��")]
    public float CameraAngleOverride = 0.0f;
    [Tooltip("���������")]
    public float MouseSensitivity = 200;

    [Tooltip("�������")]
    public Cinemachine3rdPersonFollow CinemachineVirtualCamera;
    [Tooltip("�������")]
    public float CameraDistance = 3;
    [Tooltip("�������")]
    public float CameraDistanceRatio = 5;
    [Tooltip("���������С����")]
    public float CameraDistanceMin = 2;
    [Tooltip("�������������")]
    public float CameraDistanceMax = 8;

    [Tooltip("�����")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private float _cinemachineTargetDistance;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // state
    private bool isRun = false;
    private bool isJump = false;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private Animator _animator;
    private CharacterController _controller;
    private GameObject _mainCamera;

    private bool _hasAnimator;

    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        var cinemachine = FindObjectOfType<CinemachineVirtualCamera>();
        CinemachineVirtualCamera = cinemachine.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            isRun = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isRun = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJump = true;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LockCameraPosition = !LockCameraPosition;
        }

        _cinemachineTargetDistance -= Input.GetAxis("Mouse ScrollWheel") * CameraDistanceRatio;
        _cinemachineTargetDistance = Mathf.Clamp(_cinemachineTargetDistance, CameraDistanceMin, CameraDistanceMax);
    }

    private void FixedUpdate()
    {
        _hasAnimator = TryGetComponent(out _animator);

        JumpAndGravity();
        GroundedCheck();
        Move();
    }
    public float cameraDistanceLerpSpeed = 1;
    private void LateUpdate()
    {
        CameraPosition();
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    /// <summary>
    /// ������
    /// </summary>
    private void GroundedCheck()
    {
        // �õ�����λ��
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

        // �����
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        // ����Animator
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraPosition()
    {
        CameraDistance = Mathf.Lerp(CameraDistance, _cinemachineTargetDistance, Time.deltaTime * cameraDistanceLerpSpeed);
        CinemachineVirtualCamera.CameraDistance = CameraDistance;
    }

    private void CameraRotation()
    {
        var mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        var mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        _cinemachineTargetYaw += mouseX * Time.deltaTime;
        _cinemachineTargetPitch -= mouseY * Time.deltaTime;

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
        var targetRot = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0);
        CinemachineCameraTarget.transform.rotation = targetRot;
    }

    /// <summary>
    /// ��ɫ�ƶ�
    /// </summary>
    private void Move()
    {
        // ��ȡ����ǰ����������
        Vector3 curInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // �����Ƿ��¼��ټ�����������ٶ�ֵ
        float targetSpeed = isRun ? SprintSpeed : MoveSpeed;

        // ����������ֵ̫С�򲻼���
        if (curInput == Vector3.zero) targetSpeed = 0.0f;

        // ��ȡ��ҵ�ǰ��ˮƽ���ϵĵ�λ�ٶ�
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        //float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // ģ����ٹ���
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // ģ��һ�������Եļ��ٹ���
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * curInput.magnitude, Time.fixedDeltaTime * SpeedChangeRate);

            // ��ȷ��С�����3λ
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        // ���ö������ٶ�
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.fixedDeltaTime * SpeedChangeRate);

        // ��ȡ��ҵ����뵥λˮƽ����
        Vector3 inputDirection = new Vector3(curInput.x, 0.0f, curInput.z).normalized;

        // Vector2's != ����ʡ����
        if (curInput != Vector3.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

            // ��ת�������������ķ���
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // ��ȡ�ƶ���Ŀ�귽��
        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // �ƶ���ң�ˮƽ����ƶ� + ��ֱ������ƶ�
        _controller.Move(targetDirection.normalized * (_speed * Time.fixedDeltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // ����Animator
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, curInput.magnitude);
        }
    }

    /// <summary>
    /// ��ɫ��Ծ
    /// </summary>
    private void JumpAndGravity()
    {
        // �ڵ�����
        if (Grounded)
        {
            // ��������ʱ��
            _fallTimeoutDelta = FallTimeout;

            // ����Animator
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // ��������
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // ���������Ծ
            if (isJump && _jumpTimeoutDelta <= 0.0f)
            {
                // �����ֱ�ٶ�
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // ����Animator
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // ������Ծ��״̬��ֵ
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.fixedDeltaTime;
            }
        }
        //�ڰ����
        else
        {
            // ������Ծʱ��
            _jumpTimeoutDelta = JumpTimeout;

            // �����������
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.fixedDeltaTime;
            }
            else
            {
                // ����Animator
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // �ڰ���в�����Ծ
            isJump = false;
        }

        // �����ٶ�Ϊ Vt = V0 + a * t
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.fixedDeltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }

}
