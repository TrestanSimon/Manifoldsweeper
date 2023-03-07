using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class CameraHandler : MonoBehaviour {
    private GameObject target;
    private float sensitivity = 1f;
    private Vector3 mousePos;
    private Vector3 dmousePos;
    private float scroll;

    Complex complex;
    bool planar;
    
    public float r, R;
    private float tu = 0f, tv = 0f;
    private float zoom = 10f;
    private Vector3 circleMajor = Vector3.zero;
    private Vector3 circleMinor = Vector3.zero;

    private void Update() {
        complex = GameObject.Find("Surface").GetComponent<Complex>();
        if (planar != complex.planar) {
            if (complex.planar) UpdateTopDownCamera(true);
            else Update3DCamera(true);
        }

        planar = complex.planar;
        if (planar) UpdateTopDownCamera();
        else Update3DCamera();
    }

    // Default camera
    public void Update3DCamera(bool force = false) {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;

        if (force) {
            StartCoroutine(LerpCameraTo(
                new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 270f, 0f))
            );
        }

        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }

        if ((Input.GetMouseButton(0) && mousePos != null)) {
            dmousePos = Input.mousePosition - mousePos;
            Camera.main.transform.RotateAround(
                Vector3.zero, Vector3.up, dmousePos.x/2f*Time.deltaTime);
            Camera.main.transform.RotateAround(
                Vector3.zero, Camera.main.transform.right, -dmousePos.y/2f*Time.deltaTime);
        }

        if (scroll != 0f) {
            Camera.main.transform.position -= scroll * Camera.main.transform.forward.normalized;
        }
    }

    // A top-down camera
    // if force is true, camera resets position
    public void UpdateTopDownCamera(bool force = false) {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;

        if (force) {
            StartCoroutine(LerpCameraTo(
                new Vector3(0f, 30f, 0f), Quaternion.Euler(90f, 0f, 90f))
            );
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }

        if ((Input.GetMouseButton(0) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;
            Camera.main.transform.position += dmousePos.x/10f*Time.deltaTime * Vector3.forward;
            Camera.main.transform.position += dmousePos.y/10f*Time.deltaTime * Vector3.left;
        }

        if (scroll != 0f) {
            Camera.main.transform.position += scroll * Vector3.up;
        }
    }

    public IEnumerator LerpCameraTo(Vector3 endPos, Quaternion endRot) {
        float time = 0f;
        float duration = 2f;
        float t = 0f;
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        while (time < duration) {
            t = time / duration;
            t = t*t*(3f - 2f * t);

            Camera.main.transform.position = Vector3.Lerp(startPos, endPos, t);
            Camera.main.transform.rotation = Quaternion.Lerp(startRot, endRot, t);

            time += Time.deltaTime;
            yield return null;
        }
    }

    // Custom camera for torus
    // Follows poloidal and toroidal coordinates
    public void TorusCamera(Camera cam, bool force = false) {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }
        if ((Input.GetMouseButton(0) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;

            tu -= clamp(dmousePos.y/300f, -30f, 30f) * Time.deltaTime;
            sincos(tu, out float sinu, out float cosu);
            tv -= clamp(dmousePos.x/300f, -30f, 30f) * Time.deltaTime;
            sincos(tv, out float sinv, out float cosv);

            // Major (toroidal) and minor (poloidal) circles
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
