using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class CameraHandler : MonoBehaviour {
    public float minFOV = 10f;
    public float maxFOV = 90f;
    public float sensitivity = 1f;
    public float FOV = 60f;

    private Vector3 startPos;
    private Vector3 dPos;
    private float scroll;
    private Camera cam;

    private ComplexHandler complexHandler;
    private float radius;

    private void Awake() {
        complexHandler = GetComponentInParent<ComplexHandler>();
        radius = complexHandler.R;
    }

    void Update() {
        cam = Camera.main;
        // cam.transform.LookAt(Vector3.zero);
        scroll = Input.mouseScrollDelta.y * sensitivity * -1;
        if (Input.GetMouseButtonDown(2)) {
            startPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(2) && startPos != null) {
            dPos = Input.mousePosition - startPos;
            cam.transform.RotateAround(Vector3.zero, Vector3.up, dPos.x/2f*Time.deltaTime);
            cam.transform.RotateAround(Vector3.zero, Camera.main.transform.right, -dPos.y/2f*Time.deltaTime);
        }
        if (scroll != 0f) {
            FOV += scroll;
            FOV = clamp(FOV, minFOV, maxFOV);
            cam.fieldOfView = FOV;
        }
    }
}
