using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public float r = 1f, R = 3f;
    public float t = 0f;
    private Vector3 camCenter = Vector3.zero;
    private Vector3 camdr = 4f * Vector3.right;

    private void Awake() {
        cam = Camera.main;
        UpdateCamera();
    }
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            sincos(2*PI*u / ResU, out float sinu, out float cosu);
            float minor = R + r*cosu;

            for (int v = 0; v <= ResV; v++) {
                sincos(2*PI*v / ResV, out float sinv, out float cosv);

                vertices[u,v] = new Vector3(
                    minor * cosv,
                    r * sinu,
                    minor * sinv
                );
                normals[u,v] = new Vector3(
                    cosu * cosv,
                    sinu,
                    cosu * sinv
                );
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return null; }
    }

    public override void UpdateCamera() {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(2)) {
            mousePos = Input.mousePosition;
        }
        if (Input.GetMouseButton(2) && mousePos != null) {
            dmousePos = Input.mousePosition - mousePos;
            t += clamp(dmousePos.x/1000f, -10f, 10f);
            sincos(t, out float sint, out float cost);
            camCenter = new Vector3(
                R * cost,
                0f,
                R * sint
            );
            float camdr2 = sqrt(camdr.x*camdr.x + camdr.z*camdr.z);
            camdr = camdr2 * camCenter/R;
            // Vertical:
            cam.transform.RotateAround(camCenter, Vector3.up, dmousePos.x/2f*Time.deltaTime);
            // Horizontal:
            cam.transform.RotateAround(camCenter, Camera.main.transform.right, -dmousePos.y/2f*Time.deltaTime);
        }
        if (scroll != 0f) {
            FOV += scroll;
            FOV = Mathf.Clamp(FOV, minFOV, maxFOV);
            cam.fieldOfView = FOV;
        }
        cam.transform.position = camCenter + camdr;
        cam.transform.LookAt(camCenter);
    }
}
