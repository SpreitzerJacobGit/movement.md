using UnityEngine;

namespace MovementMD.Presentation
{
    /// <summary>
    /// Mock <see cref="IGrapplePointSource"/> for visual tooling — produces a point chain between
    /// two transforms with optional sag, so the grapple look can be tweaked before the deterministic
    /// grapple system is wired. Presentation only; never feeds sim state.
    /// </summary>
    public sealed class GrappleVisualDemo : MonoBehaviour, IGrapplePointSource
    {
        [SerializeField] private Transform playerEnd;
        [SerializeField] private Transform anchorEnd;
        [Tooltip("Intermediate points inserted between the two ends to fake rope physics.")]
        [Range(0, 32)] public int IntermediatePoints = 6;
        [Tooltip("Vertical sag (meters) applied to intermediate points (parabolic).")]
        [Range(0f, 8f)] public float Sag = 1.2f;
        [Tooltip("Slowly orbit the anchor so the taper direction is visible while tweaking.")]
        public bool Orbit = true;
        [Range(0f, 2f)] public float OrbitSpeed = 0.4f;

        private Vector3[] _buffer = System.Array.Empty<Vector3>();

        public bool IsActive => playerEnd != null && anchorEnd != null;

        /// <summary>Assigns the two endpoint transforms (player=chain start, anchor=chain end).</summary>
        public void Configure(Transform player, Transform anchor)
        {
            playerEnd = player;
            anchorEnd = anchor;
        }

        public Vector3[] GetPoints()
        {
            if (!IsActive) return System.Array.Empty<Vector3>();
            int n = IntermediatePoints + 2;
            if (_buffer.Length != n) _buffer = new Vector3[n];
            Vector3 a = playerEnd.position;
            Vector3 b = anchorEnd.position;
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (float)i / (n - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                if (i > 0 && i < n - 1)
                {
                    float u = 2f * t - 1f; // -1..1, zero at ends
                    p.y -= Sag * (1f - u * u); // parabolic sag, peak at mid
                }
                _buffer[i] = p;
            }
            return _buffer;
        }

        private void Update()
        {
            if (!Orbit || !IsActive) return;
            // RotateAround preserves distance to the pivot — orbit at constant radius, no drift.
            anchorEnd.RotateAround(playerEnd.position, Vector3.up, OrbitSpeed * 60f * Time.deltaTime);
        }
    }
}
