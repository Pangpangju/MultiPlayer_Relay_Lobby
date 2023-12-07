using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] CharacterController _CC;
    [SerializeField] Animator _AM;

    [SerializeField] private CinemachineVirtualCamera _VC;
    [SerializeField] private AudioListener listener;

    [SerializeField] private Transform spawnedObjectPrefab;
    private Vector3 moveDirection;
    private Vector3 lookPos;

    private Transform cam;
    private Vector3 camForward;
    private Vector3 move;
    private Vector3 moveInput;

    private float forwardAmount;
    private float turnAmount;

    public float movementSpeed;
    public float rotationSpeed;

    //public override void OnNetworkSpawn()
    //{
        /*
        if (IsOwner)
        {
            listener.enabled = true;
            _VC.Priority = 1;
        }
        else
        {
            _VC.Priority = 0;
        }*/
    //}


    private void Start()
    {
        listener.enabled = true;
        _VC.Priority = 1;
        cam = Camera.main.transform;
    }

    private void Update()
    {

        
    }
    private void FixedUpdate()
    {
        //if (!IsOwner) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100))
        {
            lookPos = hit.point;
        }

        

        Vector3 lookDirection = lookPos - transform.position;
        lookDirection.y = 0;

        transform.LookAt(transform.position + lookDirection, Vector3.up);
        _CC.Move(new Vector3(0, -9.8f, 0) * Time.deltaTime);
        
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.D))
        {
            _AM.SetBool("isRunning", false);
        }
        else
        {
            if (!_AM.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                _AM.SetBool("isRunning", true);
                basicMovement(lookDirection);
            }

        }
        if (Input.GetMouseButtonDown(0) && !_AM.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            _AM.SetTrigger("Attack");
        }
        
    }

    void basicMovement(Vector3 lookPosition)
    {
        
        
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        if (cam != null)
        {
            camForward = Vector3.Scale(cam.up, new Vector3(1, 0, 1));

            move = y * camForward + x * cam.right;
        }

        else {
            move = y * Vector3.forward + x * Vector3.right;
        }

        Move(move);

        moveDirection = new Vector3(x, 0, y).normalized * movementSpeed * Time.deltaTime;
        _CC.Move(moveDirection);
    }

    private void Move(Vector3 move)
    {
        this.moveInput = move;

        ConvertMoveInput();
        UpdateAnimator();
    }

    private void ConvertMoveInput() {
        Vector3 localMove = transform.InverseTransformDirection(moveInput);
        turnAmount = localMove.x;
        forwardAmount = localMove.z;
    }

    private void UpdateAnimator() {
        _AM.SetFloat("Forward", forwardAmount, 0.1f, Time.deltaTime);
        _AM.SetFloat("Turn", turnAmount, 0.1f, Time.deltaTime);
    }
}

