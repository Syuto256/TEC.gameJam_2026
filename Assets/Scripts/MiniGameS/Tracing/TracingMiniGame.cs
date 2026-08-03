using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overwork.MiniGames.Tracing
{
    public sealed class TracingMiniGame : MiniGameBase
    {
        private const int MaxMisses = 2;
        private TracingPathDatabase database;
        private TracingPathEntry path;
        private RectTransform board;
        private RectTransform pointer;
        private TextMeshProUGUI status;
        private bool tracing;
        private int misses;

        public void Configure(TracingPathDatabase pathDatabase) => database = pathDatabase;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (database == null || !database.TryGetRandomPath(difficulty, out path))
            {
                FinishGame(false, "NO PATH CONFIGURED");
                return;
            }
            BuildUi(); RefreshStatus("HOLD LEFT MOUSE AT START");
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Mouse.current == null || board == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(board, Mouse.current.position.ReadValue(), null, out var local)) return;
            var normalized = LocalToNormalized(local);
            if (!tracing)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && Vector2.Distance(normalized, path.points[0]) <= .07f)
                {
                    tracing = true; ShowPointer(local); RefreshStatus("TRACING");
                }
                return;
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame || !Mouse.current.leftButton.isPressed)
            {
                ResetTrace(); RefreshStatus("RESTART FROM START"); return;
            }
            ShowPointer(local);
            if (TracingPathMath.DistanceToPolyline(normalized, path.points) > path.allowedDeviationRatio) { RegisterMiss(); return; }
            if (Vector2.Distance(normalized, path.points[path.points.Count - 1]) <= .07f) FinishGame(true, "COMPLETE");
        }

        private void RegisterMiss()
        {
            misses++; ResetTrace();
            if (misses >= MaxMisses) { FinishGame(false, "MISSED"); return; }
            RefreshStatus("MISS - RETRY FROM START");
        }
        private void ResetTrace() { tracing = false; if (pointer != null) pointer.gameObject.SetActive(false); }
        private Vector2 LocalToNormalized(Vector2 local) => new Vector2(local.x / board.rect.width + .5f, local.y / board.rect.height + .5f);
        private void ShowPointer(Vector2 position) { pointer.gameObject.SetActive(true); pointer.anchoredPosition = position; }
        private void RefreshStatus(string message) { if (status != null) status.text = message + "\nMISS " + misses + " / 2    TIME " + Mathf.CeilToInt(TimeRemaining).ToString("00"); }

        private void BuildUi()
        {
            gameObject.AddComponent<Image>().color = new Color(.07f, .12f, .2f, .98f);
            CreateText("Title", "TRACE THE PATH", new Vector2(.1f,.82f), new Vector2(.9f,.94f), 34f, TextAlignmentOptions.Center);
            board = CreateRect("TracingArea", new Vector2(.1f,.2f), new Vector2(.9f,.78f)); board.gameObject.AddComponent<Image>().color = new Color(.03f,.06f,.1f,.9f);
            for (var i=0; i<path.points.Count-1; i++) CreateSegment(path.points[i], path.points[i+1]);
            CreateMarker("Start", path.points[0], new Color(.2f,1f,.55f), 28f);
            CreateMarker("End", path.points[path.points.Count-1], new Color(1f,.35f,.55f), 28f);
            pointer = CreateMarker("Pointer", path.points[0], new Color(.35f,.9f,1f), 18f); pointer.gameObject.SetActive(false);
            status = CreateText("Status", string.Empty, new Vector2(.1f,.06f), new Vector2(.9f,.18f), 23f, TextAlignmentOptions.Center);
        }
        private void CreateSegment(Vector2 from, Vector2 to)
        {
            var line = CreateRect("Guide", Vector2.zero, Vector2.zero); line.SetParent(board, false); line.anchorMin=line.anchorMax=line.pivot=new Vector2(.5f,.5f);
            var a = NormalizedToLocal(from); var b = NormalizedToLocal(to); var delta=b-a; line.anchoredPosition=(a+b)*.5f; line.sizeDelta=new Vector2(delta.magnitude, 14f); line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg); line.gameObject.AddComponent<Image>().color=new Color(.65f,.72f,.82f);
        }
        private RectTransform CreateMarker(string name, Vector2 normalized, Color color, float size) { var marker=CreateRect(name, Vector2.zero, Vector2.zero); marker.SetParent(board,false); marker.anchorMin=marker.anchorMax=marker.pivot=new Vector2(.5f,.5f); marker.anchoredPosition=NormalizedToLocal(normalized); marker.sizeDelta=Vector2.one*size; marker.gameObject.AddComponent<Image>().color=color; return marker; }
        private Vector2 NormalizedToLocal(Vector2 value) => new Vector2((value.x-.5f)*board.rect.width,(value.y-.5f)*board.rect.height);
        private RectTransform CreateRect(string name, Vector2 min, Vector2 max) { var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(transform,false); var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; return rect; }
        private TextMeshProUGUI CreateText(string name,string value,Vector2 min,Vector2 max,float size,TextAlignmentOptions align) { var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); obj.transform.SetParent(transform,false); var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=new Vector2(8,4);rect.offsetMax=new Vector2(-8,-4);var text=obj.GetComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.text=value;text.fontSize=size;text.alignment=align;text.color=Color.white;return text; }
    }
}
