// movement.md §5 — debug visualization for the movement + grapple sandbox (Unity / view layer).
//
// Builds a visible ROOM (floor + ceiling + walls) matching the analytical surfaces GrappleSystem aims
// at, a cube per mover, and per-NODE rope rendering so the grapple rope is visible as a chain. Reads
// the predicted frame; view-layer only (never writes sim state).
//
// All fields below are LIVE-tweakable: select this component during Play (or add it in the scene at
// edit time) and adjust in the inspector to tune the look.

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

        [Header("Third-Person Camera")]
        public float CamDistance     = 7f;
        public float CamHeight       = 4f;
        public float CamLookAtHeight = 1f;

        const float RoomH  = 12f;
        const float RoomHW = 15f;

        Transform _floor;
        Camera _cam;
        bool _roomBuilt;
        readonly Dictionary<int, GameObject> _cubes = new();
        readonly Dictionary<int, GameObject[]> _ropeNodes = new();

        void Update()
        {
            EnsureSceneViz();
            var f = QuantumRunner.Default?.Game?.Frames?.Predicted;
            if (f == null) return;

            // Movers (cubes).
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

                // Third-person orbit camera around the cube, driven by look yaw + pitch (mouse up/down tilts the view).
                if (_cam != null && key == 0)
                {
                    float yaw = mover.Yaw.AsFloat;
                    float pitch = mover.Pitch.AsFloat;
                    float cp = Mathf.Cos(pitch), sp = Mathf.Sin(pitch);
                    Vector3 back = new Vector3(-Mathf.Sin(yaw), 0f, -Mathf.Cos(yaw));  // horizontal backward
                    _cam.transform.position = cubePos + back * (cp * CamDistance) + Vector3.up * (sp * CamDistance + CamHeight);
                    _cam.transform.LookAt(cubePos + Vector3.up * CamLookAtHeight);
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
                _cam.transform.position = new Vector3(0, RoomH * 0.6f, -RoomHW + 2f);
                _cam.transform.LookAt(new Vector3(0, RoomH * 0.3f, 0));
                _cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                _cam.clearFlags = CameraClearFlags.SolidColor;
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
