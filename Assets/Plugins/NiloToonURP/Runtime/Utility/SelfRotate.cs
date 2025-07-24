using UnityEngine;

namespace NiloToon.NiloToonURP.MiscUtil
{
    public class SelfRotate : MonoBehaviour
    {
        public float speed = 45;

        void Update()
        {
            transform.Rotate(new Vector3(0, Time.deltaTime * speed, 0), Space.World);
        }
    }
}