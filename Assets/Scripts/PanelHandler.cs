using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PanelHandler : MonoBehaviour {
    private GameObject surface;
    private Transform manifoldsPanel;
    private Transform mapsPanel;
    private Transform manifoldMaps;
    private Transform gamePanel;
    private Transform customColumn;

    
    private int selectedManifold, selectedMap;
    private int selectedDifficulty;
    public int ResU, ResV, mineCount;

    private Game game;
    private Complex complex;
    private Toggle[] manifoldToggles, mapToggles, difficultyToggles;
    private TMP_InputField[] inputFields;
    private Animator animator;
    private bool panelOpen;

    private void Awake() {
        manifoldsPanel = transform.Find("Manifolds Panel");
        manifoldToggles = manifoldsPanel.Find("Manifold Toggles").gameObject.GetComponentsInChildren<Toggle>();

        mapsPanel = transform.Find("Maps Panel");
        manifoldMaps = mapsPanel.Find("Maps Toggles");

        gamePanel = transform.Find("Game Panel");
        difficultyToggles = gamePanel.Find("Difficulty Toggles").gameObject.GetComponentsInChildren<Toggle>();
        customColumn = gamePanel.Find("Difficulty Toggles").Find("Custom Column");
        inputFields = customColumn.GetComponentsInChildren<TMP_InputField>();

        animator = gameObject.GetComponent<Animator>();
    }

    private void Start() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();
    }

    public void Clear() {
        Destroy(surface);
    }

    public void PanelOpenClose() {
        panelOpen = animator.GetBool("panelOpen");
        panelOpen = !panelOpen;
        animator.SetBool("panelOpen", panelOpen);
    }

    public void Generate() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();
        GenerateSurface();
        if (complex != null) {
            Destroy(complex);
        }
        AttachComplex();
        complex.Setup(Camera.main, ResU, ResV);
        complex.GenerateComplex();
        game = complex.Gamify(mineCount);
        game.NewGame();
    }

    // Ran On Value Changed of Manifold Toggles
    public void UpdateSelectedManifold() {
        for (int i = 0; i < manifoldToggles.Length; i++) {
            if (manifoldToggles[i].isOn) {
                selectedManifold = i;
                break;
            }
        }
        UpdateActiveMaps();
    }

    public void UpdateActiveMaps() {
        for (int i = 0; i < manifoldToggles.Length; i++) {
            if (i == selectedManifold) {
                manifoldMaps.GetChild(i).gameObject.SetActive(true);
            } else {
                manifoldMaps.GetChild(i).gameObject.SetActive(false);
            }
        }

        mapToggles = manifoldMaps.GetChild(selectedManifold).gameObject.GetComponentsInChildren<Toggle>();
        UpdateSelectedMap();
    }

    // Ran On Value Changed of Mapping Toggles
    public void UpdateSelectedMap() {
        for (int i = 0; i < mapToggles.Length; i++) {
            if (mapToggles[i].isOn) {
                selectedMap = i;
                break;
            }
        }
    }

    // Ran On Value Changed of Difficulty Toggles
    public void UpdateDifficulty() {
        for (int i = 0; i < difficultyToggles.Length; i++) {
            if (difficultyToggles[i].isOn) {
                selectedDifficulty = i;
                break;
            }
        }

        if (selectedDifficulty == 3) {
            UpdateActiveCustomDifficulty(true);
        } else {
            UpdateActiveCustomDifficulty(false);
        }

        switch (selectedDifficulty) {
            case 0: ResU = ResV = 8; mineCount = 12; break;
            case 1: ResU = ResV = 16; mineCount = 42; break;
            case 2: ResU = 16; ResV = 32; mineCount = 98; break;
            case 3: UpdateCustomDifficulty(); break;
        }
    }

    public void UpdateActiveCustomDifficulty(bool active) {
        for (int i = 0; i < inputFields.Length; i++) {
            inputFields[i].interactable = active;
        }
    }

    // Ran On Value Changed of Custom Difficulty Text Inputs
    public void UpdateCustomDifficulty() {
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
        switch (selectedManifold) {
            case 0: complex = surface.AddComponent<Plane>(); break;
            case 1: break;
            case 2: complex = surface.AddComponent<Torus>(); break;
            case 3: complex = surface.AddComponent<MobiusStrip>(); break;
            case 4: complex = surface.AddComponent<KleinBottle>(); break;
            default: break;
        }
    }

    public void MapToPlane() {
        complex.MapToPlane();
    }
}
