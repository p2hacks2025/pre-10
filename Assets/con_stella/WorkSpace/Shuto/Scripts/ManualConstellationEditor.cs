using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class ManualConstellationEditor : MonoBehaviour
{
    [Header("UI Manager")]
    // ★追加: パネルが開いているかチェックするために必要
    [SerializeField] private ManualUIManager uiManager;

    [Header("必要なプレハブ")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject linePrefab;

    [Header("設定")]
    // ★重要: 星座を配置する親オブジェクト（Canvas内のRectTransformを指定してください）
    [SerializeField] private RectTransform constellationRoot;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float starClickRadius = 100f; // 判定広め

    [Header("星のサイズ設定")]
    [SerializeField] private float minStarScale = 3.0f;
    [SerializeField] private float maxStarScale = 5.0f;

    [Header("線の設定")]
    [SerializeField] private float lineWidth = 2.0f; // 見やすい太さに

    [Header("ガイド表示")]
    [SerializeField] private TextMeshProUGUI guideText;

    private List<GameObject> stars = new List<GameObject>();
    private List<Connection> connections = new List<Connection>();
    private GameObject selectedStar = null;

    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();

    // 内部クラス
    private class Connection
    {
        public GameObject start;
        public GameObject end;
        public GameObject lineObj;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        UpdateGuideText();
    }

    void Update()
    {
        // ★追加: UIパネルが開いているときは操作を受け付けない
        if (uiManager != null && uiManager.HasActivePanel) return;

        if (Input.GetMouseButtonDown(0))
        {
            // UIボタン上のクリックは無視
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // 描画範囲チェック
            if (constellationRoot != null)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(constellationRoot, Input.mousePosition, mainCamera))
                {
                    return;
                }
            }

            HandleTouch(Input.mousePosition);
        }
    }

    private void HandleTouch(Vector3 screenPos)
    {
        GameObject clickedStar = FindStarNear(screenPos);

        if (clickedStar != null)
        {
            if (selectedStar == null)
            {
                SelectStar(clickedStar);
            }
            else if (selectedStar == clickedStar)
            {
                DeselectStar();
            }
            else
            {
                ExecuteCommand(new AddConnectionCommand(this, selectedStar, clickedStar));
                SelectStar(clickedStar); // 連続入力のため
            }
        }
        else
        {
            // ★修正: ワールド座標ではなく、RectTransform内のローカル座標に変換する
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                constellationRoot,
                screenPos,
                mainCamera,
                out localPoint
            );

            // Z=0にする
            Vector3 finalPos = new Vector3(localPoint.x, localPoint.y, 0f);

            ExecuteCommand(new AddStarCommand(this, finalPos));
        }
    }

    // ================================================================================
    // コマンド
    // ================================================================================

    private interface ICommand { void Execute(); void Undo(); }

    private void ExecuteCommand(ICommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear();
        UpdateGuideText();
    }

    private class AddStarCommand : ICommand
    {
        private ManualConstellationEditor editor;
        private Vector3 localPosition; // ローカル座標
        private GameObject createdStar;

        public AddStarCommand(ManualConstellationEditor editor, Vector3 localPos)
        {
            this.editor = editor;
            this.localPosition = localPos;
        }

        public void Execute()
        {
            if (createdStar == null)
            {
                // ★修正: constellationRootの子として生成
                createdStar = Instantiate(editor.starPrefab, editor.constellationRoot);
                createdStar.transform.localPosition = localPosition;
                createdStar.transform.localRotation = Quaternion.identity;

                float randomScale = UnityEngine.Random.Range(editor.minStarScale, editor.maxStarScale);
                createdStar.transform.localScale = Vector3.one * randomScale;
            }
            else
            {
                createdStar.SetActive(true);
            }
            editor.stars.Add(createdStar);
            editor.SelectStar(createdStar);
        }

        public void Undo()
        {
            editor.stars.Remove(createdStar);
            createdStar.SetActive(false);
            if (editor.selectedStar == createdStar) editor.DeselectStar();
        }
    }

    private class AddConnectionCommand : ICommand
    {
        private ManualConstellationEditor editor;
        private GameObject start, end;
        private Connection createdConnection;

        public AddConnectionCommand(ManualConstellationEditor editor, GameObject start, GameObject end)
        {
            this.editor = editor;
            this.start = start;
            this.end = end;
        }

        public void Execute()
        {
            if (editor.IsConnected(start, end)) return;

            if (createdConnection == null)
            {
                // ★修正: constellationRootの子として生成
                GameObject lineObj = Instantiate(editor.linePrefab, editor.constellationRoot);
                createdConnection = new Connection { start = start, end = end, lineObj = lineObj };
                editor.UpdateLinePosition(createdConnection);
            }
            else
            {
                createdConnection.lineObj.SetActive(true);
                editor.UpdateLinePosition(createdConnection);
            }
            editor.connections.Add(createdConnection);
        }

        public void Undo()
        {
            if (createdConnection == null) return;
            editor.connections.Remove(createdConnection);
            if (createdConnection.lineObj != null) createdConnection.lineObj.SetActive(false);
        }
    }

    // 削除系コマンドは変更なしのため省略せず記述
    private class DeleteStarCommand : ICommand
    {
        private ManualConstellationEditor editor;
        private GameObject targetStar;
        private List<Connection> relatedConnections = new List<Connection>();

        public DeleteStarCommand(ManualConstellationEditor editor, GameObject target)
        {
            this.editor = editor;
            this.targetStar = target;
        }

        public void Execute()
        {
            relatedConnections.Clear();
            foreach (var conn in editor.connections)
            {
                if (conn.start == targetStar || conn.end == targetStar) relatedConnections.Add(conn);
            }
            foreach (var conn in relatedConnections)
            {
                conn.lineObj.SetActive(false);
                editor.connections.Remove(conn);
            }
            targetStar.SetActive(false);
            editor.stars.Remove(targetStar);
            if (editor.selectedStar == targetStar) editor.DeselectStar();
        }

        public void Undo()
        {
            targetStar.SetActive(true);
            editor.stars.Add(targetStar);
            foreach (var conn in relatedConnections)
            {
                conn.lineObj.SetActive(true);
                editor.connections.Add(conn);
            }
        }
    }

    private class DeleteAllCommand : ICommand
    {
        private ManualConstellationEditor editor;
        private List<GameObject> deletedStars;
        private List<Connection> deletedConnections;

        public DeleteAllCommand(ManualConstellationEditor editor) { this.editor = editor; }

        public void Execute()
        {
            deletedStars = new List<GameObject>(editor.stars);
            deletedConnections = new List<Connection>(editor.connections);
            foreach (var conn in deletedConnections) conn.lineObj.SetActive(false);
            editor.connections.Clear();
            foreach (var star in deletedStars) star.SetActive(false);
            editor.stars.Clear();
            editor.DeselectStar();
        }

        public void Undo()
        {
            foreach (var star in deletedStars) { star.SetActive(true); editor.stars.Add(star); }
            foreach (var conn in deletedConnections) { conn.lineObj.SetActive(true); editor.connections.Add(conn); }
        }
    }

    // ================================================================================
    // Public Methods
    // ================================================================================

    public void Undo() { if (undoStack.Count > 0) { ICommand cmd = undoStack.Pop(); cmd.Undo(); redoStack.Push(cmd); UpdateGuideText(); } }
    public void Redo() { if (redoStack.Count > 0) { ICommand cmd = redoStack.Pop(); cmd.Execute(); undoStack.Push(cmd); UpdateGuideText(); } }
    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void DeleteSelected() { if (selectedStar != null) ExecuteCommand(new DeleteStarCommand(this, selectedStar)); }
    public void DeleteAll() { if (stars.Count > 0) ExecuteCommand(new DeleteAllCommand(this)); }

    // ================================================================================
    // Helper Methods
    // ================================================================================

    private GameObject FindStarNear(Vector3 screenPos)
    {
        foreach (var star in stars)
        {
            if (!star.activeSelf) continue;
            // ★修正: 距離判定もScreen座標で行うのが安全
            Vector3 starScreenPos = mainCamera.WorldToScreenPoint(star.transform.position);
            if (Vector2.Distance(screenPos, starScreenPos) <= starClickRadius) return star;
        }
        return null;
    }

    private void SelectStar(GameObject star)
    {
        if (selectedStar != null) SetStarColor(selectedStar, normalColor);
        selectedStar = star;
        SetStarColor(star, selectedColor);
        UpdateGuideText();
    }

    private void DeselectStar()
    {
        if (selectedStar != null) { SetStarColor(selectedStar, normalColor); selectedStar = null; }
        UpdateGuideText();
    }

    private void SetStarColor(GameObject star, Color color)
    {
        if (star != null)
        {
            var sr = star.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }
    }

    private bool IsConnected(GameObject star1, GameObject star2)
    {
        foreach (var conn in connections)
        {
            if ((conn.start == star1 && conn.end == star2) || (conn.start == star2 && conn.end == star1)) return true;
        }
        return false;
    }

    // アクセス修飾子を修正済み
    private void UpdateLinePosition(Connection conn)
    {
        if (conn.lineObj != null)
        {
            LineRenderer lr = conn.lineObj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                // ★修正: Canvas内で動くため、WorldSpaceはOFFにする
                lr.useWorldSpace = false;

                lr.sortingOrder = 90;
                lr.positionCount = 2;

                // 親(constellationRoot)からのローカル座標を使う
                Vector3 startPos = conn.start.transform.localPosition;
                Vector3 endPos = conn.end.transform.localPosition;

                startPos.z = 0f;
                endPos.z = 0f;

                lr.SetPosition(0, startPos);
                lr.SetPosition(1, endPos);

                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
            }
        }
    }

    private void UpdateGuideText()
    {
        if (guideText == null) return;
        if (stars.Count == 0) guideText.text = "画面をタップして星を配置";
        else if (selectedStar == null) guideText.text = "星を選択、またはタップで配置";
        else guideText.text = "別の星をタップして線を引く";
    }

    public ConstellationData GetConstellationData()
    {
        ConstellationData data = new ConstellationData();
        data.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Dictionary<GameObject, int> objToId = new Dictionary<GameObject, int>();
        int newIdCounter = 0;

        foreach (var starObj in stars)
        {
            if (!starObj.activeSelf) continue;
            int id = newIdCounter++;
            objToId[starObj] = id;
            data.stars.Add(new StarData
            {
                id = id,
                // ★重要: UIのローカル座標(Pixel単位)を保存する
                x = starObj.transform.localPosition.x,
                y = starObj.transform.localPosition.y,
                scale = starObj.transform.localScale.x
            });
        }

        foreach (var conn in connections)
        {
            if (objToId.ContainsKey(conn.start) && objToId.ContainsKey(conn.end))
            {
                data.connections.Add(new ConnectionData
                {
                    fromStarId = objToId[conn.start],
                    toStarId = objToId[conn.end]
                });
            }
        }
        return data;
    }
}