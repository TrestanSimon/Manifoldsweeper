using System.Collections;
using UnityEngine;

public class CameraHandler : MonoBehaviour {
    private float _sensitivity = 0.2f;
    private Vector3 _mousePos;
    private Vector3 _dmousePos;
    private float _scroll;

    private Complex _target;
    private Complex.Map _map;

    public Complex Target {
        private get => _target;
        set => _target = value;
    }

    private void Update() {
        if (_target == null) return;

        if (_map != _target.CurrentMap) {
            if (_target.CurrentMap == Complex.Map.Flat) Update2DCamera(true);
            else if (_map == Complex.Map.Flat) Update3DCamera(true);
        }

        _map = _target.CurrentMap;
        if (_map == Complex.Map.Flat) Update2DCamera();
        else Update3DCamera();
    }

    // Default camera
    public void Update3DCamera(bool force = false) {
        _scroll = Input.mouseScrollDelta.y * _sensitivity * -1f;

        if (force)
            StartCoroutine(LerpCameraTo(
                new Vector3(10f, 4f, -10f)
            ));

        if (Input.GetMouseButtonDown(2))
            _mousePos = Input.mousePosition;

        if ((Input.GetMouseButton(2) && _mousePos != null)) {
            _dmousePos = Input.mousePosition - _mousePos;
            Camera.main.transform.RotateAround(
                Vector3.zero, Vector3.up, _dmousePos.x/2f*Time.deltaTime);
            Camera.main.transform.RotateAround(
                Vector3.zero, Camera.main.transform.right, -_dmousePos.y/2f*Time.deltaTime);
        }

        if (_scroll != 0f)
            Camera.main.transform.position -= _scroll * Camera.main.transform.forward.normalized;
    }

    // A top-down camera
    // if force is true, camera resets position
    public void Update2DCamera(bool force = false) {
        _scroll = Input.mouseScrollDelta.y * _sensitivity * -1f;

        if (force) {
            StartCoroutine(LerpCameraTo(
                new Vector3(0f, 15f, 0f),
                Quaternion.Euler(90f, 0f, 90f))
            );
            return;
        }

        if (Input.GetMouseButtonDown(2))
            _mousePos = Input.mousePosition;

        if ((Input.GetMouseButton(2) && _mousePos != null) || force) {
            Move2DCamera();
        }

        if (_scroll != 0f)
            Zoom2DCamera();
    }

    private void Move2DCamera() {
        Vector3[] frustumCorners = new Vector3[4];
        Camera.main.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            Camera.main.transform.position.y - _target.Corners[0].y,
            Camera.MonoOrStereoscopicEye.Mono, frustumCorners
        );

        for (int i = 0; i < 4; i++)
            frustumCorners[i] = Camera.main.transform.TransformVector(frustumCorners[i]) + Camera.main.transform.position;
        // [BL, TL, TR, BR]

        _dmousePos = (Input.mousePosition - _mousePos) / 10f*Time.deltaTime;

        if (frustumCorners[0].x > _target.Corners[0].x)
            Camera.main.transform.position -= _target.Offset[1];

        Camera.main.transform.position += _dmousePos.x * Vector3.forward;
        Camera.main.transform.position += _dmousePos.y * Vector3.left;
    }

    private void Zoom2DCamera() {
        Camera.main.transform.position += _scroll * Vector3.up;
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
    public IEnumerator LerpCameraTo(Vector3 endPos) {
        float time = 0f;
        float duration = 2f;
        float t = 0f;
        Vector3 startPos = Camera.main.transform.position;
        Vector3 startUp = Camera.main.transform.up;
        Quaternion startRot = Camera.main.transform.rotation;

        while (time < duration) {
            t = time / duration;
            t = t*t*(3f - 2f * t);

            Camera.main.transform.position = Vector3.Lerp(startPos, endPos, t);
            Camera.main.transform.LookAt(_target.transform,
                Vector3.Lerp(startUp, Vector3.up, t)); // Rotate smoothly

            time += Time.deltaTime;
            yield return null;
        }
    }
}
