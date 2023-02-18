using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Plane : Complex {
    public Vector3 center;

    public override void Awake() {
        sideCount = 2;
        UpdateCamera(true);
    }
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            for (int v = 0; v <= ResV; v++) {
                vertices[u,v] = new Vector3(
                    u, 0, v
                );
            }
        }
    }

    public override void UpdateCamera(bool force = false) {
        cam = Camera.main;
        center = new Vector3(ResU/2f, 0f, ResV/2f);

        if (force) {
            cam.transform.position = center + 30f*Vector3.up;
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 270f);
        }

        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;

        if (Input.GetMouseButtonDown(2)) {
            mousePos = Input.mousePosition;
        }
        if (Input.GetMouseButton(2) && mousePos != null) {
            dmousePos = Input.mousePosition - mousePos;
            cam.transform.RotateAround(center, Vector3.up, dmousePos.x/2f*Time.deltaTime);
            cam.transform.RotateAround(center, Camera.main.transform.right, -dmousePos.y/2f*Time.deltaTime);
        }
        if (scroll != 0f) {
            FOV += scroll;
            FOV = Mathf.Clamp(FOV, minFOV, maxFOV);
            cam.fieldOfView = FOV;
        }
    }
}
