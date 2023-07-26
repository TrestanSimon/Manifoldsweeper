using System.Collections;
using UnityEngine;

public class CameraHandler : MonoBehaviour {
    private float _sensitivity = 0.2f;
    private Vector3 _mousePos;
    private Vector3 _dmousePos;
    private float _scroll;
    private Vector3[] _frustumCorners;

    private Complex _target;
    private Complex.Map _Map {
        get => _target.CurrentMap;
    }

    private bool _isTransitioning = false;
    private bool _is3DCamera = true;
    public bool Is3DCamera {
        get => _is3DCamera;
    }

    private void Update() {
        if (_target is null || _isTransitioning) return;
        if (_is3DCamera) Update3DCamera();
        else Update2DCamera();
    }

    // Default camera
    private void Update3DCamera() {
        _scroll = Input.mouseScrollDelta.y * _sensitivity * -1f;

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
    private void Update2DCamera() {
        _scroll = Input.mouseScrollDelta.y * _sensitivity * -1f;

        if (Input.GetMouseButtonDown(2))
            _mousePos = Input.mousePosition;

        if ((Input.GetMouseButton(2) && _mousePos != null)) {
            Move2DCamera();
        }

        if (_scroll != 0f)
            Zoom2DCamera();
    }

    private void Move2DCamera() {
        _frustumCorners = new Vector3[4];
        Camera.main.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            Camera.main.transform.position.y - _target.Corners[0].y,
            Camera.MonoOrStereoscopicEye.Mono, _frustumCorners
        );

        for (int i = 0; i < 4; i++)
            _frustumCorners[i] = Camera.main.transform.TransformVector(_frustumCorners[i]) + Camera.main.transform.position;
        // [BL, TL, TR, BR]

        _dmousePos = (Input.mousePosition - _mousePos) / 10f*Time.deltaTime;

        if (_frustumCorners[0].x > _target.Corners[0].x)
            Camera.main.transform.position -= _target.Offset[1];

        Camera.main.transform.position += _dmousePos.x * Vector3.forward;
        Camera.main.transform.position += _dmousePos.y * Vector3.left;
    }

    private void Zoom2DCamera() {
        Camera.main.transform.position += _scroll * Vector3.up;
    }

    private IEnumerator LerpCameraTo(Vector3 endPos, Quaternion endRot) {
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
    private IEnumerator LerpCameraTo(Vector3 endPos) {
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

    public IEnumerator TransitionTo2DCamera() {
        _isTransitioning = true;

        float complexHeight = Mathf.Abs(_target.InteriorCorners[0].x)
            + Mathf.Abs(_target.InteriorCorners[1].x);
        float complexWidth = Mathf.Abs(_target.InteriorCorners[0].z)
            + Mathf.Abs(_target.InteriorCorners[2].z);
        float complexAspect = complexWidth / complexHeight;

        float leg, fov;
        if (complexAspect < Camera.main.aspect) {
            leg = _target.InteriorCorners[0].x;
            fov = Camera.main.fieldOfView;
        } else {
            leg = _target.InteriorCorners[3].z;
            fov = Camera.VerticalToHorizontalFieldOfView(
                Camera.main.fieldOfView, Camera.main.aspect
            );
        }

        float theta = (fov / 2f) * (Mathf.PI / 180f);
        float height = leg / Mathf.Tan(theta);

        yield return StartCoroutine(LerpCameraTo(
            new Vector3(0f, height, 0f),
            Quaternion.Euler(90f, 0f, 90f))
        );
        _isTransitioning = false;
        _is3DCamera = false;
    }

    public IEnumerator TransitionTo3DCamera() {
        _isTransitioning = true;
        yield return StartCoroutine(LerpCameraTo(
            new Vector3(10f, 4f, -10f)
        ));
        _isTransitioning = false;
        _is3DCamera = true;
    }

    public IEnumerator NewMap(Complex.Map newMap) {
        if (newMap == Complex.Map.Flat)
            yield return StartCoroutine(TransitionTo2DCamera());
        else if (newMap != Complex.Map.Flat)
            yield return StartCoroutine(TransitionTo3DCamera());
    }

    public IEnumerator NewTarget(Complex newTarget) {
        _target = newTarget;
        yield return StartCoroutine(NewMap(_Map));
        if (_Map == Complex.Map.Flat)
            StartCoroutine(_target.RepeatComplex());
    }
}
