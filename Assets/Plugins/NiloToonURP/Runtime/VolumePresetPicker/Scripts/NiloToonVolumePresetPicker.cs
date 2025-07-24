using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace NiloToon.NiloToonURP
{
    public enum VolumeMode
    {
        Global,
        Local
    }

    [RequireComponent(typeof(Volume))]
    [ExecuteAlways]
    public class NiloToonVolumePresetPicker : MonoBehaviour
    {
        public int _currentIndex = -1;

        [Range(0, 1)]
        public float weight = 1f;
        public VolumeMode mode = VolumeMode.Global;
        public int priority = -1;
        
        public List<VolumeProfile> volumeProfiles = new List<VolumeProfile>();

        private Volume _volume;
        private Volume volume
        {
            get
            {
                if (!_volume)
                {
                    _volume = GetComponent<Volume>();
                }
                
                if (!_volume)
                {
                    _volume = gameObject.AddComponent<Volume>();
                    return _volume;
                }
                
                return _volume;
            }
        }
        public int currentIndex
        {
            get { return _currentIndex; }
            set
            {
                _currentIndex = value;
                AssignVolumeProfileByIndex(_currentIndex);
            }
        }

        public void EnablePrevious()
        {
            AssignVolumeProfileByIndex(currentIndex - 1);
        }

        public void EnableNext()
        {
            AssignVolumeProfileByIndex(currentIndex + 1);
        }

        private void AssignVolumeProfileByIndex(int index)
        {
            if (index == volumeProfiles.Count) index = 0;
            if (index == -1) index = volumeProfiles.Count - 1;
            
            volume.sharedProfile = volumeProfiles[index];
            
            volume.weight = weight;
            volume.priority = priority;
            volume.isGlobal = mode == VolumeMode.Global;

            _currentIndex = index;
        }

        // Use OnValidate to update changes during edit time
        private void OnValidate()
        {
            AssignVolumeProfileByIndex(_currentIndex);
        }

        private void OnEnable()
        {
            AssignVolumeProfileByIndex(_currentIndex);
        }
    }


}