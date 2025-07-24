using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP.MiscUtil
{
    public class DoFAutoFocusTransform : MonoBehaviour
    {
        public Camera targetCamera;
        public Transform focusTarget;
        public Volume volume;

        DepthOfField component;

        void Start()
        {
            volume.profile.TryGet(out component);
        }

        void LateUpdate()
        {
            // be careful, focusDistance is not simple distance between targetCamera & focusTarget
            // but it is the view space z length between targetCamera & focusTarget
            component.focusDistance.value = Mathf.Abs(targetCamera.worldToCameraMatrix.MultiplyPoint(focusTarget.position).z);
        }
    }

}
