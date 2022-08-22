
using Cinemachine;
using UnityEngine;
using DG.Tweening;


public class PlayerCam : MonoBehaviour
{
    [SerializeField]private float sensX;
    [SerializeField]private float sensY;
    
    [SerializeField]private Transform orientation;
    public Transform camHolder;
    private float xRotation;
    private float yRotation;


    private Camera c;
    
    private void Start()
    {
        c = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
      //  vmCam = GetComponent<CinemachineVirtualCamera>();

    }

    
    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;
        
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        camHolder.rotation = Quaternion.Euler(xRotation,yRotation,0);
        orientation.rotation = Quaternion.Euler(0, yRotation,0);
    }

    public void DoFov(float endVale)
    {
        c.DOFieldOfView(endVale, 0.25f);
    }

    public void DoTilt(float zTilt)
    {
        transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
    }
}
