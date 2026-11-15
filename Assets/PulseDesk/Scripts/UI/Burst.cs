using UnityEngine;

namespace PulseDesk
{
    /// <summary>A short spray of small coloured chips, for the moment a task is finished. Built from code.</summary>
    public class Burst : MonoBehaviour
    {
        private const int Count = 16;
        private const float Life = 0.75f;

        private Transform[] _chips;
        private Vector3[] _velocity;
        private float _age;

        public static void Play(Vector3 worldPosition, Quaternion facing, Color a, Color b)
        {
            GameObject root = new GameObject("Burst");
            root.transform.SetPositionAndRotation(worldPosition, facing);
            Burst burst = root.AddComponent<Burst>();
            burst.Build(a, b);
        }

        private void Build(Color a, Color b)
        {
            Material ma = Make.NewMaterial(a);
            Material mb = Make.NewMaterial(b);
            _chips = new Transform[Count];
            _velocity = new Vector3[Count];
            for (int i = 0; i < Count; i++)
            {
                Renderer r = Make.Box("Chip", transform, Vector3.zero, new Vector3(0.006f, 0.006f, 0.002f), a);
                r.sharedMaterial = i % 2 == 0 ? ma : mb;
                _chips[i] = r.transform;
                // Fan out toward the user and upward, in the plane of the card.
                float angle = (i / (float)Count) * Mathf.PI * 2f + Random.value * 0.4f;
                float speed = 0.22f + Random.value * 0.22f;
                _velocity[i] = new Vector3(Mathf.Cos(angle) * speed, Mathf.Abs(Mathf.Sin(angle)) * speed + 0.12f, -0.05f - Random.value * 0.1f);
            }
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float k = _age / Life;
            if (k >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float dt = Time.unscaledDeltaTime;
            float shrink = 1f - k * k;
            for (int i = 0; i < _chips.Length; i++)
            {
                _velocity[i] += Vector3.down * 0.9f * dt;
                _chips[i].localPosition += _velocity[i] * dt;
                _chips[i].localRotation = Quaternion.Euler(0f, 0f, _age * 420f + i * 40f);
                _chips[i].localScale = new Vector3(0.006f, 0.006f, 0.002f) * shrink;
            }
        }
    }
}
