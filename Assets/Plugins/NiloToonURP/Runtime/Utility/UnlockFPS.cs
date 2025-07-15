using UnityEngine;

namespace NiloToon.NiloToonURP.MiscUtil
{
    public class UnlockFPS : MonoBehaviour
    {
        void Start()
        {
            Application.targetFrameRate = 60;
        }
    }
}