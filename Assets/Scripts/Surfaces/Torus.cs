using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public float r = 1f, R = 3f;
    public float tu = 0f, tv = 0f;
    public float zoom = 10f;
    private Vector3 circleMajor = Vector3.zero;
    private Vector3 circleMinor = Vector3.zero;
    private Vector3 camdr = 4f * Vector3.right;

    public override void Awake() {
        sideCount = 1;
        cam = Camera.main;
        UpdateCamera(true);
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

    public override void UpdateCamera(bool force = false) {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }
        if ((Input.GetMouseButton(0) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;

            tu += clamp(dmousePos.y/300f, -30f, 30f) * Time.deltaTime;
            sincos(tu, out float sinu, out float cosu);
            tv += clamp(dmousePos.x/300f, -30f, 30f) * Time.deltaTime;
            sincos(tv, out float sinv, out float cosv);

            // Major (toroidal) and minor (poloidal) circles making up torus
            circleMajor = new Vector3(R * cosv, 0f, R * sinv);
            circleMinor = new Vector3(r * cosu * cosv, r * sinu, r * cosu * sinv);
        }
        if (scroll != 0f) {
            zoom += scroll;
            zoom = clamp(zoom, 2f, 20f);
        }
        cam.transform.position = circleMajor + zoom * circleMinor;
        cam.transform.LookAt(circleMajor);
    }
}
