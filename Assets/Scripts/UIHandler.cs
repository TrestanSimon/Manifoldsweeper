using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIHandler : MonoBehaviour {
    private GameObject surface;
    private int type = 0;
    public int ResU = 16, ResV = 48, mineCount = 48;

    private Game game;
    private Complex complex;
    private Toggle[] toggles;
    private TMP_InputField[] inputFields;
    private Animator[] animators;
    private bool panelOpen;

    private void Awake() {
        toggles = gameObject.GetComponentsInChildren<Toggle>();
        inputFields = gameObject.GetComponentsInChildren<TMP_InputField>();
        animators = gameObject.GetComponentsInChildren<Animator>();
    }

    public void Clear() {
        Destroy(surface);
    }

    public void PanelOpenClose() {
        panelOpen = animators[0].GetBool("panelOpen");
        panelOpen = !panelOpen;
        animators[0].SetBool("panelOpen", panelOpen);
    }

    public void Generate() {
        UpdateType();
        UpdateParameters();
        GenerateSurface();
        if (complex != null) {
            Destroy(complex);
        }
        AttachComplex();
        complex.Setup(ResU, ResV);
        complex.GenerateComplex();
        game = complex.Gamify(mineCount);
        game.NewGame();
    }

    private void UpdateType() {
        for (int i = 0; i < toggles.Length; i++) {
            if (toggles[i].isOn) {
                type = i;
            }
        }
    }

    private void UpdateParameters() {
        int.TryParse(inputFields[0].text, out ResU);
        int.TryParse(inputFields[1].text, out ResV);
        int.TryParse(inputFields[2].text, out mineCount);
    }

    private void GenerateSurface() {
        if (surface != null) {
            Destroy(surface);
        }
        surface = new GameObject();
        surface.name = "Surface";
    }

    private void AttachComplex() {
        switch (type) {
            case 0:
                complex = surface.AddComponent<Plane>();
                break;
            case 1:
                complex = surface.AddComponent<Torus>();
                break;
            case 2:
                complex = surface.AddComponent<MobiusStrip>();
                break;
        }
    }
}
