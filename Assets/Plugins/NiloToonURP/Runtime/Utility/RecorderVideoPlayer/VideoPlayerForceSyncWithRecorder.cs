// Attach this script besides a VideoPlayer, enable it only when recording using Unity's Recorder.
// This script will try to force VideoPlayer's playspeed sync with Recorder, but sound will not play correctly

// copy and improved from https://forum.unity.com/threads/unity-recorder-movie-output-is-too-fast.664534/

using UnityEngine;
using UnityEngine.Video;

namespace NiloToon.NiloToonURP.MiscUtil
{
    public class VideoPlayerForceSyncWithRecorder : MonoBehaviour
    {
        public float recorderFPS = 60; // we can't use Application.targetFPS, since it is usually -1. So user need to provide recorder fps
        public float delaySeconds = 0;
        public float loopDelaySeconds = 0;
        public int startFrame = 0;
        public int startFrameOffset = 0;
        public int startFrameOffset2 = 0;
        private VideoPlayer videoPlayer; // sibling video player
        private bool ready = false;

        float videoFPS;
        float videoFrameCount;

        private void Start()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.playbackSpeed = 0f;
            //videoPlayer.isLooping = true;
            videoPlayer.frame = startFrame + startFrameOffset + startFrameOffset2;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();

            videoFPS = videoPlayer.frameRate;
            videoFrameCount = videoPlayer.frameCount;

            passedFrame = -delaySeconds * recorderFPS * videoFPS;
        }

        private void OnVideoPrepared(VideoPlayer videoPlayer)
        {
            videoPlayer.Play();
            ready = true;
            shouldStepForward = true;

            Debug.Log($"VideoStepper:{videoPlayer.clip.name} is ready at frame = {Time.renderedFrameCount}. You should always pause in editor until all videos produced this message.");
        }

        float passedFrame = 0;
        float totalPassedVideoFrame = 0;
        bool shouldStepForward = false;

        private void Update()
        {
            if (ready)
            {
                passedFrame += videoFPS;

                if (totalPassedVideoFrame > videoFrameCount)
                {
                    shouldStepForward = false;

                    // loop restart
                    if (videoPlayer.isLooping)
                    {
                        shouldStepForward = true;
                        passedFrame = -loopDelaySeconds * recorderFPS * videoFPS;
                        totalPassedVideoFrame = 0;
                        videoPlayer.frame = 0;
                    }
                    else
                    {
                        videoPlayer.Stop();
                        return;
                    }
                }

                if (shouldStepForward && (passedFrame >= recorderFPS))
                {
                    passedFrame -= recorderFPS;
                    videoPlayer.StepForward();
                    //videoPlayer.Play();
                    totalPassedVideoFrame++;
                }
            }
        }
    }
}