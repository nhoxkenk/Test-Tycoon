using System;
using System.Collections;
using UnityEngine;

namespace Farm.Actors
{
    // Visual only. The harvest batch remains the source of truth for quantity and price.
    public sealed class ActorCarryView : MonoBehaviour
    {
        [SerializeField] private GameObject tomatoPrefab;
        [SerializeField] private Transform[] carryPoints;

        private GameObject[] tomatoes;

        public Vector3[] CarryPositions
        {
            get
            {
                var positions = new Vector3[carryPoints.Length];
                for (var i = 0; i < carryPoints.Length; i++) positions[i] = carryPoints[i].position;
                return positions;
            }
        }

        public void Show(int quantity, Vector3[] fromPositions = null)
        {
            if (carryPoints == null || quantity < 0 || quantity > carryPoints.Length)
                throw new ArgumentOutOfRangeException(nameof(quantity));
            StopAllCoroutines();
            if (tomatoes == null) tomatoes = new GameObject[carryPoints.Length];

            for (var i = 0; i < carryPoints.Length; i++)
            {
                if (i < quantity && tomatoes[i] == null)
                {
                    if (tomatoPrefab == null || carryPoints[i] == null)
                        throw new InvalidOperationException("Tomato prefab or carry point is missing.");
                    tomatoes[i] = Instantiate(tomatoPrefab, carryPoints[i]);
                    tomatoes[i].transform.localPosition = Vector3.zero;
                    tomatoes[i].transform.localRotation = Quaternion.identity;
                }
                if (tomatoes[i] == null) continue;
                tomatoes[i].SetActive(i < quantity);
                if (i >= quantity) continue;
                if (fromPositions != null && i < fromPositions.Length)
                {
                    tomatoes[i].transform.position = fromPositions[i];
                    StartCoroutine(JumpToSocket(tomatoes[i].transform, carryPoints[i]));
                }
                else tomatoes[i].transform.localPosition = Vector3.zero;
            }
        }

        private static IEnumerator JumpToSocket(Transform tomato, Transform socket)
        {
            var origin = tomato.position;
            const float duration = 0.35f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                tomato.position = Vector3.Lerp(origin, socket.position, t) + Vector3.up * (0.7f * 4f * t * (1f - t));
                yield return null;
            }
            tomato.localPosition = Vector3.zero;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (tomatoes == null) return;
            foreach (var tomato in tomatoes)
                if (tomato != null) tomato.SetActive(false);
        }
    }
}
