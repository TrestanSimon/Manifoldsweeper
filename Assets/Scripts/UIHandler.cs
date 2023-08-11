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

    private GameObject gameOverMessage, gameWinMessage;
    private Image fundamentalPolygon;

    private Sprite[] fundamentalPolygonSprites;

    private Button newButton, clearButton;
    
    private int selectedManifold;
    private int selectedDifficulty;
    private int _resU, _resV, mineCount;
    private (int u, int v, int mines)[,] manifoldDifficulties;

    private Game game;
    private Complex complex;
    private Button mapButton;
    private TMP_Text areaText, timerText, flagText;
    private Image flagPanelImage;
    private Color flagPanelGrey, flagPanelRed, flagPanelGreen;
    private TMP_InputField[] inputFields;
    private Toggle[] manifoldToggle, difficultyToggle;
    private TMP_Dropdown mapDropdown;
    private Animator animator;
    private bool panelOpen = true;

    private CameraHandler cameraHandler;
    private List<Coroutine> coroutines;

    private Dictionary<string, Complex.Map> SelectedMapDict {
        get {
            switch (selectedManifold) {
                case 0: return Plane.MapDict;
                case 1: return Cylinder.MapDict;
                case 2: return Torus.MapDict;
                case 3: return Mobius.MapDict;
                case 4: return Klein.MapDict;
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
        manifoldToggle = manifoldsPanel.Find("Manifold Toggle").gameObject.GetComponentsInChildren<Toggle>();

        mapsPanel = panel.Find("Maps Panel");
        mapDropdown = mapsPanel.GetComponentInChildren<TMP_Dropdown>();
        mapButton = mapsPanel.Find("Map Button").GetComponent<Button>();
        mapButton.interactable = false;

        gamePanel = panel.Find("Game Panel");
        difficultyToggle = gamePanel.Find("Difficulty Toggle").GetComponentsInChildren<Toggle>();
        customInputs = gamePanel.Find("Custom Inputs");
        inputFields = customInputs.GetComponentsInChildren<TMP_InputField>();
        areaText = customInputs.Find("Area Label").GetComponent<TMP_Text>();

                
        // Top panel data
        topPanel = transform.Find("Top Panel");
        animator = topPanel.GetComponent<Animator>();
        newButton = topPanel.Find("Timer Panel").Find("New Game Button").GetComponent<Button>();
        timerText = topPanel.Find("Timer Panel").Find("Timer Label").GetComponent<TMP_Text>();
        clearButton = topPanel.Find("Flag Panel").Find("Clear Flags Button").GetComponent<Button>();
        flagText = topPanel.Find("Flag Panel").Find("Flag Label").GetComponent<TMP_Text>();
        flagPanelImage = topPanel.Find("Flag Panel").GetComponent<Image>();
        flagPanelGrey = new Color(1f, 1f, 1f, 0.2745098f);
        flagPanelGreen = new Color(0f, 1f, 0f, 0.7f);
        flagPanelRed = new Color(1f, 0f, 0f, 0.7f);

        cameraHandler = Camera.main.GetComponent<CameraHandler>();

        // GameObjects
        gameOverMessage = transform.Find("GameOver Message").gameObject;
        gameWinMessage = transform.Find("GameWin Message").gameObject;

        fundamentalPolygon = manifoldsPanel.Find("Fundamental Polygon").GetComponent<Image>();
        fundamentalPolygonSprites = new Sprite[] {
            Resources.Load("Textures/UI/PlaneSquare", typeof(Sprite)) as Sprite,
            Resources.Load("Textures/UI/CylinderSquare", typeof(Sprite)) as Sprite,
            Resources.Load("Textures/UI/TorusSquare", typeof(Sprite)) as Sprite,
            Resources.Load("Textures/UI/MobiusSquare", typeof(Sprite)) as Sprite,
            Resources.Load("Textures/UI/KleinSquare", typeof(Sprite)) as Sprite
        };

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
        manifoldDifficulties[4,2] = (32, 16, 98);

        mineCount = 0;
    }

    private void Start() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();

        manifoldToggle[2].isOn = true;  // Start with torus
        mapDropdown.value = 0;
    }

    private void Update() {
        if (game is null || complex is null) {
            newButton.interactable = false;
            clearButton.interactable = false;
            return;
        }

        gameOverMessage.SetActive(game.GameLost && !panelOpen);
        gameWinMessage.SetActive(game.GameWon && !panelOpen);

        newButton.interactable = true;
        clearButton.interactable = (game.FlagCount != 0 && !game.GameWon && !game.GameLost);

        timerText.text =
            $"{(int)game.Timer}.{((int)(game.Timer*10))%10}";
        UpdateFlagPanel();
        
        if (Input.GetKeyDown("escape"))
            TogglePanel();
    }

    public void Clear() {
        Destroy(board);
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
            case 3: complex = board.AddComponent<Mobius>(); break;
            case 4: complex = board.AddComponent<Klein>(); break;
            default: break;
        }
        game = board.AddComponent<Game>();

        complex.Setup(_resU, _resV, SelectedMap);
        game.Setup(complex, _resU, _resV, mineCount);
        StartCoroutine(cameraHandler.NewTarget(complex));

        mapButton.interactable = false;

        game.NewGame(false);
    }

    // Ran On Value Changed of Manifold dropdown
    public void UpdateSelectedManifold() {
        for (int i = 0; i < manifoldToggle.Length; i++)
            if (manifoldToggle[i].isOn)
                selectedManifold = i;
        
        fundamentalPolygon.sprite = fundamentalPolygonSprites[selectedManifold];

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
        for (int i = 0; i < difficultyToggle.Length; i++)
            if (difficultyToggle[i].isOn)
                selectedDifficulty = i;
        
        areaText.text = $"Total tiles = {_resU * _resV}";

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
        if (_resU < 2) _resU = 2;

        int.TryParse(inputFields[1].text, out _resV);
        if (_resV < 2) _resV = 2;

        int.TryParse(inputFields[2].text, out mineCount);
        if (mineCount >= _resV * _resU) mineCount = (_resV * _resU) - 1;

        inputFields[0].text = _resU.ToString();
        inputFields[1].text = _resV.ToString();
        inputFields[2].text = mineCount.ToString();
        areaText.text = $"Total tiles = {_resU * _resV}";
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

    private void ActivateToggleGroup(Toggle[] toggleGroup, bool toggleOn) {
        foreach (Toggle toggle in toggleGroup)
            toggle.interactable = toggleOn;
    }

    public void TogglePanel() {
        panelOpen = !panelOpen;

        if (game is not null)
            game.Paused = panelOpen;
            
        panel.gameObject.SetActive(panelOpen);
    }

    public void ToggleTopPanel() {
        animator.SetBool("panelOpen", !animator.GetBool("panelOpen"));
    }
    
    private void UpdateFlagPanel() {
        if (game.FlagCount < game.MineCount)
            flagPanelImage.color = flagPanelGrey;
        else if (game.FlagCount == game.MineCount)
            flagPanelImage.color = flagPanelGreen;
        else
            flagPanelImage.color = flagPanelRed;
        flagText.text = $"{game.FlagCount}/{game.MineCount}";
    }

    // Ran on button press
    public void ClearFlags() {
        if (game is null || game.FlagCount == 0 || game.GameWon || game.GameLost) return;
        game.ClearFlags();
    }

    // Ran on button press
    public void NewGame() {
        if (game is null) return;
        game.NewGame();
    }

    public void ActivateRaycast(bool activate) {
        if (complex is null) return;
        complex.RaycastEnabled = activate;
    }
}
