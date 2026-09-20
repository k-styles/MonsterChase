using System.Collections.Generic;
using UnityEngine;

namespace MonsterChase.Monster
{
    /// <summary>An ordered loop of points. The racetrack corridor, as waypoints.</summary>
    public class PatrolRoute : MonoBehaviour
    {
        [SerializeField] List<Transform> points = new List<Transform>();
        int index;

        public bool HasPoints => points.Count > 0;

        public Vector3 Next()
        {
            if (points.Count == 0) return transform.position;
            index = (index + 1) % points.Count;
            return points[index].position;
        }

        /// <summary>Start from whichever point is nearest, not always from zero.</summary>
        public Vector3 NearestTo(Vector3 position)
        {
            if (points.Count == 0) return transform.position;
            float best = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null) continue;
                float d = (points[i].position - position).sqrMagnitude;
                if (d >= best) continue;
                best = d; index = i;
            }
            return points[index].position;
        }
    }
}
