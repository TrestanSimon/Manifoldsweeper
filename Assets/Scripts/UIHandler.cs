using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHandler : MonoBehaviour {
    private GameObject board;
    private Transform panel, topPanel;
    private Transform manifoldsPanel;
    private Transform mapsPanel;
    private Transform gamePanel;
    private Transform customInputs;

    
    private int selectedManifold;
    private int selectedDifficulty;
    private int _resU, _resV, mineCount;
    private (int u, int v, int mines)[,] manifoldDifficulties;

    private Game game;
    private Complex complex;
    private Button mapButton;
    private TMP_Text timerText, flagText;
    private TMP_InputField[] inputFields;
    private TMP_Dropdown manifoldDropdown, difficultyDropdown, mapDropdown;
    private Animator animator;
    private bool panelOpen;

    private CameraHandler cameraHandler;
    private List<Coroutine> coroutines;

    private Dictionary<string, Complex.Map> SelectedMapDict {
        get {
            switch (selectedManifold) {
                case 0: return Plane.MapDict;
                case 1: return Cylinder.MapDict;
                case 2: return Torus.MapDict;
                case 3: return MobiusStrip.MapDict;
                case 4: return KleinBottle.MapDict;
                default: throw new System.Exception();
            }
        }
    }
    // Map selected in dropdown
    private Complex.Map SelectedMap {
        get => SelectedMapDict.ElementAt(mapDropdown.value).Value;
    }

    private void Awake() {
        coroutines = new List<Coroutine>();

        // Panel data
        panel = transform.Find("Panel");

        manifoldsPanel = panel.Find("Manifolds Panel");
        manifoldDropdown = manifoldsPanel.Find("Manifold Dropdown").gameObject.GetComponent<TMP_Dropdown>();

        mapsPanel = panel.Find("Maps Panel");
        mapDropdown = mapsPanel.GetComponentInChildren<TMP_Dropdown>();
        mapButton = mapsPanel.Find("Map Button").GetComponent<Button>();
        mapButton.interactable = false;

        gamePanel = panel.Find("Game Panel");
        difficultyDropdown = gamePanel.Find("Difficulty Dropdown").GetComponent<TMP_Dropdown>();
        customInputs = gamePanel.Find("Custom Inputs");
        inputFields = customInputs.GetComponentsInChildren<TMP_InputField>();

        animator = panel.GetComponent<Animator>();
                
        // Top panel data
        topPanel = transform.Find("Top Panel");
        timerText = topPanel.Find("Timer Label").GetComponent<TMP_Text>();
        flagText = topPanel.Find("Flag Label").GetComponent<TMP_Text>();

        cameraHandler = Camera.main.GetComponent<CameraHandler>();

        manifoldDifficulties = new (int,int,int)[5,3];

        // Plane
        manifoldDifficulties[0,0] = (8, 8, 12);
        manifoldDifficulties[0,1] = (16, 16, 42);
        manifoldDifficulties[0,2] = (16, 32, 98);

        // Cylinder
        manifoldDifficulties[1,0] = (8, 8, 12);
        manifoldDifficulties[1,1] = (16, 16, 42);
        manifoldDifficulties[1,2] = (32, 16, 98);

        // Torus
        manifoldDifficulties[2,0] = (8, 16, 12);
        manifoldDifficulties[2,1] = (16, 24, 42);
        manifoldDifficulties[2,2] = (16, 32, 98);

        // Mobius strip
        manifoldDifficulties[3,0] = (16, 4, 12);
        manifoldDifficulties[3,1] = (32, 8, 42);
        manifoldDifficulties[3,2] = (32, 16, 98);

        // Klein bottle
        manifoldDifficulties[4,0] = (8, 8, 12);
        manifoldDifficulties[4,1] = (16, 16, 42);
        manifoldDifficulties[4,2] = (16, 32, 98);

        mineCount = 0;
    }

    private void Start() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();

        manifoldDropdown.value = 0;
        mapDropdown.value = 0;
    }

    private void Update() {
        if (game is null || complex is null) return;

        topPanel.Find("GameStart Message").gameObject.SetActive(
            !game.GameOn && !game.GameLost && !game.GameWon);
        topPanel.Find("GameOver Message").gameObject.SetActive(game.GameLost);
        topPanel.Find("GameWin Message").gameObject.SetActive(game.GameWon);

        flagText.text = $"{game.FlagCount}/{game.MineCount}";
        timerText.text =
            $"{(int)game.Timer}.{((int)(game.Timer*10))%10}";
    }

    public void Clear() {
        Destroy(board);
    }

    public void PanelOpenClose() {
        panelOpen = animator.GetBool("panelOpen");
        panelOpen = !panelOpen;
        animator.SetBool("panelOpen", panelOpen);
    }

    public void Generate() {
        if (board != null) {
            if (complex != null)
                Destroy(complex);
            if (game != null)
                Destroy(game);
            Destroy(board);
        }
        board = new GameObject();
        board.name = "Board";

        switch (selectedManifold) {
            case 0: complex = board.AddComponent<Plane>(); break;
            case 1: complex = board.AddComponent<Cylinder>(); break;
            case 2: complex = board.AddComponent<Torus>(); break;
            case 3: complex = board.AddComponent<MobiusStrip>(); break;
            case 4: complex = board.AddComponent<KleinBottle>(); break;
            default: break;
        }
        game = board.AddComponent<Game>();

        complex.Setup(_resU, _resV, SelectedMap);
        game.Setup(complex, _resU, _resV, mineCount);
        StartCoroutine(cameraHandler.NewTarget(complex));

        mapButton.interactable = false;

        topPanel.Find("Tutorial Message").gameObject.SetActive(false);

        game.NewGame(false);
    }

    // Ran On Value Changed of Manifold dropdown
    public void UpdateSelectedManifold() {
        selectedManifold = manifoldDropdown.value;

        UpdateActiveMaps();
        UpdateDifficulty();
    }

    public void UpdateActiveMaps() {
        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(SelectedMapDict.Keys.ToList());
        mapDropdown.RefreshShownValue();
    }

    // Ran On Value Changed of Difficulty dropdown
    public void UpdateDifficulty() {
        selectedDifficulty = difficultyDropdown.value;

        // If custom difficulty is selected
        if (selectedDifficulty == 3) {
            UpdateActiveCustomDifficulty(true);
        } else {
            UpdateActiveCustomDifficulty(false);
            _resU = manifoldDifficulties[selectedManifold,selectedDifficulty].u;
            _resV = manifoldDifficulties[selectedManifold,selectedDifficulty].v;
            mineCount = manifoldDifficulties[selectedManifold,selectedDifficulty].mines;

            inputFields[0].text = _resU.ToString();
            inputFields[1].text = _resV.ToString();
            inputFields[2].text = mineCount.ToString();
        }
    }

    private void UpdateActiveCustomDifficulty(bool active) {
        for (int i = 0; i < inputFields.Length; i++) {
            inputFields[i].interactable = active;
        }
    }

    // Ran On Value Changed of Custom Difficulty Text Inputs
    public void UpdateCustomDifficulty() {
        int.TryParse(inputFields[0].text, out _resU);
        int.TryParse(inputFields[1].text, out _resV);
        int.TryParse(inputFields[2].text, out mineCount);
    }

    public void ReMapStart() {
        StartCoroutine(ReMapCoroutine());
    }

    private IEnumerator ReMapCoroutine() {
        mapDropdown.interactable = false;
        mapButton.interactable = false;

        if (complex.CurrentMap == Complex.Map.Flat) {
            // 2D --> 3D
            yield return StartCoroutine(complex.DumpRepeatComplex());
            yield return StartCoroutine(cameraHandler.TransitionTo3DCamera());
            yield return StartCoroutine(complex.ReMap(SelectedMap));
        } else {
            // 3D --> 2D or 3D
            yield return StartCoroutine(complex.ReMap(SelectedMap));

            if (SelectedMap == Complex.Map.Flat) {
                // 3D --> 2D
                yield return StartCoroutine(cameraHandler.TransitionTo2DCamera());
                yield return StartCoroutine(complex.RepeatComplex());
            }
        }

        mapDropdown.interactable = true;
    }

    // Ran On Value Changed of mapDropdown
    public void UpdateReMapInteractability() {
        if (complex == null) {
            mapButton.interactable = false;
            return;
        }
        if (SelectedMap == complex.CurrentMap) mapButton.interactable = false;
        else mapButton.interactable = true;
    }
}
