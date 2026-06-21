// movement.md §5 — debug visualization for the movement + grapple sandbox (Unity / view layer).
//
// Builds a visible ROOM (floor + ceiling + walls) matching the analytical surfaces GrappleSystem aims
// at, a cube per mover, per-NODE rope rendering, an over-the-shoulder camera aligned to the mover's aim,
// and a screen-center reticle. The camera looks exactly in the mover's aim direction, so the reticle
// (screen center) == grapple aim — grappling lands where you aim. Reads the predicted frame; view-only.

namespace Quantum
{
    using System.Collections.Generic;
    using UnityEngine;
    using Photon.Deterministic;

    public class MoverDebugView : MonoBehaviour
    {
        [Header("Room")]
        public Color RoomColor  = new Color(0.3f, 0.32f, 0.36f);

        [Header("Mover")]
        public float MoverSize  = 0.6f;
        public Color MoverColor = new Color(0.2f, 0.9f, 0.35f);

        [Header("Rope Visual (adjust live)")]
        public bool  ShowRopeNodes     = true;
        public float RopeNodeSize      = 0.25f;
        public Color RopeNodeColor     = new Color(1f, 0.9f, 0.3f);
        public bool  ShowRopeSegments  = true;   // Gizmos must be ON in the Game view to see segments
        public Color RopeSegmentColor  = new Color(1f, 0.8f, 0.2f);

        [Header("Over-the-Shoulder Camera")]
        public float CamBack     = 4.5f;     // distance behind the mover
        public float CamHeight   = 2.2f;     // height above the mover's feet
        public float CamShoulder = 0.7f;     // lateral offset (over the shoulder)

        [Header("Reticle")]
        public bool  ShowReticle  = true;
        public Color ReticleColor = new Color(1f, 1f, 1f, 0.85f);
        public float ReticleHalf  = 5f;      // half-size of the crosshair arms (px)

        const float RoomH  = 30f;   // cube arena: height = width = 30
        const float RoomHW = 15f;

        Transform _floor;
        Camera _cam;
        bool _roomBuilt;
        float _diagTimer;
        readonly Dictionary<int, GameObject> _cubes = new();
        readonly Dictionary<int, GameObject[]> _ropeNodes = new();

        void Update()
        {
            EnsureSceneViz();
            var f = QuantumRunner.Default?.Game?.Frames?.Predicted;
            if (f == null) return;

            _diagTimer += Time.deltaTime;
            if (_diagTimer > 1f)
            {
                _diagTimer = 0f;
                Debug.Log($"[MoverDebugView] cam={(_cam != null ? _cam.transform.position.ToString() : "null")} cubes={_cubes.Count} ropes={_ropeNodes.Count}");
            }

            // Movers (cubes) + over-the-shoulder camera aligned to the mover's aim (so reticle == grapple aim).
            var it = f.Filter<Mover>();
            while (it.Next(out EntityRef e, out Mover mover))
            {
                var t = f.Get<Transform3D>(e);
                int key = mover.PlayerIndex;
                if (!_cubes.TryGetValue(key, out var go))
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cubes[key] = go;
                }
                ApplyColor(go, MoverColor);
                go.transform.localScale = Vector3.one * MoverSize;
                Vector3 cubePos = t.Position.ToUnityVector3();
                go.transform.position = cubePos;

                if (_cam != null && key == 0)
                {
                    float yaw = mover.Yaw.AsFloat;
                    float pitch = mover.Pitch.AsFloat;
                    Vector3 aim   = new Vector3(Mathf.Cos(pitch) * Mathf.Sin(yaw), Mathf.Sin(pitch), Mathf.Cos(pitch) * Mathf.Cos(yaw));
                    Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));   // horizontal right
                    _cam.transform.position = cubePos - aim * CamBack + Vector3.up * CamHeight + right * CamShoulder;
                    _cam.transform.rotation = Quaternion.LookRotation(aim);             // look in aim dir => reticle == aim
                }
            }

            // Grapple ropes: render each NODE + segments between them. Hide last frame's nodes first.
            foreach (var arr in _ropeNodes.Values)
                for (int i = 0; i < arr.Length; i++) if (arr[i] != null) arr[i].SetActive(false);

            var rit = f.Filter<Rope>();
            while (rit.Next(out EntityRef e, out Rope rope))
            {
                if (rope.AttachedPlayer < 0) continue;
                var nodes = f.ResolveList(rope.Nodes);        // QList<RopeNode> — safe indexer below
                int count = nodes.Count;
                var arr = EnsureRopeNodes(rope.AttachedPlayer, count);

                Vector3 prev = Vector3.zero; bool havePrev = false;
                for (int i = 0; i < count; i++)
                {
                    Vector3 np = nodes[i].Pos.ToUnityVector3();
                    if (ShowRopeNodes)
                    {
                        arr[i].SetActive(true);
                        arr[i].transform.localScale = Vector3.one * RopeNodeSize;
                        arr[i].transform.position = np;
                    }
                    if (ShowRopeSegments && havePrev)
                        Debug.DrawLine(prev, np, RopeSegmentColor);
                    prev = np; havePrev = true;
                }
            }
        }

        // Simple screen-center crosshair — marks where the grapple/aim lands.
        void OnGUI()
        {
            if (!ShowReticle) return;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            var prev = GUI.color;
            GUI.color = ReticleColor;
            GUI.DrawTexture(new Rect(cx - 1f, cy - ReticleHalf, 2f, ReticleHalf * 2f), Texture2D.whiteTexture); // vertical
            GUI.DrawTexture(new Rect(cx - ReticleHalf, cy - 1f, ReticleHalf * 2f, 2f), Texture2D.whiteTexture);  // horizontal
            GUI.color = prev;
        }

        static void ApplyColor(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }

        GameObject[] EnsureRopeNodes(int playerIndex, int count)
        {
            if (!_ropeNodes.TryGetValue(playerIndex, out var arr) || arr == null || arr.Length != count)
            {
                arr = new GameObject[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    arr[i].SetActive(false);
                }
                _ropeNodes[playerIndex] = arr;
            }
            for (int i = 0; i < arr.Length; i++) ApplyColor(arr[i], RopeNodeColor);  // live color sync
            return arr;
        }

        // Floor + camera + the room shell (ceiling + 4 walls), so the sandbox + grapple surfaces are visible.
        void EnsureSceneViz()
        {
            if (_floor == null)
            {
                _floor = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
                _floor.localScale = new Vector3(3, 1, 3);            // 30x30 (room floor)
                _floor.position = Vector3.zero;
                ApplyColor(_floor.gameObject, RoomColor);
            }
            if (!_roomBuilt)
            {
                _roomBuilt = true;
                BuildSlab(new Vector3(0, RoomH / 2f,  RoomHW), new Vector3(RoomHW * 2f, RoomH, 0.5f)); // +Z wall
                BuildSlab(new Vector3(0, RoomH / 2f, -RoomHW), new Vector3(RoomHW * 2f, RoomH, 0.5f)); // -Z wall
                BuildSlab(new Vector3( RoomHW, RoomH / 2f, 0), new Vector3(0.5f, RoomH, RoomHW * 2f)); // +X wall
                BuildSlab(new Vector3(-RoomHW, RoomH / 2f, 0), new Vector3(0.5f, RoomH, RoomHW * 2f)); // -X wall
                BuildSlab(new Vector3(0, RoomH, 0),             new Vector3(RoomHW * 2f, 0.5f, RoomHW * 2f)); // ceiling
            }
            if (_cam == null)
            {
                var c = Camera.main ?? new GameObject("SpikeCam") { tag = "MainCamera" }.AddComponent<Camera>();
                _cam = c;
                _cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.nearClipPlane = 0.1f;
            }
        }

        void BuildSlab(Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = scale;
            ApplyColor(go, RoomColor);
        }
    }
}
