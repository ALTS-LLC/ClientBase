#define METHOD4

using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;

namespace NiloToon.NiloToonURP
{
    public class NiloEditorMotionBlurVideoBaker : EditorWindow
    {
        private string ffmpegPath;
        private string inputFilePath = string.Empty;
        private string customSuffix = "(BakedMotionBlur)_<fps>_<shutterspeed>_<codec>"; // Custom suffix for the output file
        private int crf = 0; // Default CRF value (0 is lossless for CRF)
        private List<float> fpsOptions = new List<float>(); // List of possible FPS options
        private int selectedFPSIndex = 0; // Default FPS index
        private float inputVideoFPS = 0.0f; // FPS of the input video
        private double inputVideoDuration = 0.0; // Duration of the input video in seconds
        private int totalFrames = 0; // Total number of frames in the output video
        private float cameraExposureDuration = 1f / 48f; // 180 shutter angle in terms of 24fps
        private float motionBlurAmount = 1;

        private string ffmpegArgumentsTemplate_H264 =
            "-vf \"{0}format=yuv420p\" -r {1} -c:v libx264 -preset veryslow -crf {2} -pix_fmt yuv420p -x264opts \"keyint=1:ref=4:bframes=0\"";

        private string ffmpegArgumentsTemplate_H265 =
            "-vf \"{0}format=yuv420p\" -r {1} -c:v libx265 -preset veryslow -x265-params \"crf={2}:keyint=1:no-open-gop=1\" -pix_fmt yuv420p";

        private string ffmpegArgumentsTemplate_H265_10bit =
            "-vf \"{0}format=yuv420p10le\" -r {1} -c:v libx265 -preset veryslow -x265-params \"crf={2}:keyint=1:no-open-gop=1\" -pix_fmt yuv420p10le";

        private string ffmpegArgumentsTemplate_ProRes =
            "-vf \"{0}\" -r {1} -c:v prores_ks -profile:v 4 -pix_fmt yuv444p10le";

        private Process ffmpegProcess;
        private Coroutine coroutine;
        private string outputFilePath;
        private string errorMessage;
        private int currenttmixFrameCount = 0;

        private const string FFmpegPathKey = "NiloToon_MotionBlurVideoBakerPath"; // Key for storing FFmpeg path in EditorPrefs

        private Vector2 scrollPosition;

        private string[] codecOptions = new string[] { "H.264", "H.265 (8-bit)", "H.265 (10-bit)", "ProRes 4444 XQ" };
        private int selectedCodecIndex = 3; // Default to ProRes 4444 XQ due to encode speed

        // Added variables for logging interval and process timing
        private DateTime lastLogTime = DateTime.MinValue;
        private DateTime processStartTime;

        [MenuItem("Window/NiloToonURP/MotionBlur Video Baker", priority = 10000)]
        public static void ShowWindow()
        {
            var window = GetWindow<NiloEditorMotionBlurVideoBaker>("MotionBlur Video Baker");
            window.LoadFFmpegPath(); // Load the FFmpeg path when the window is opened
        }
        
        private bool ClickableLink(string text, string url)
        {
            GUIStyle linkStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                normal = { textColor = Color.cyan },
                hover = { textColor = Color.cyan },
                alignment = TextAnchor.MiddleLeft
            };

            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), linkStyle, GUILayout.Height(20));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (GUI.Button(rect, $"{text}", linkStyle))
            {
                Application.OpenURL(url);
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 960fps may make cinemachine not working correctly, so we suggest max at 900fps
            EditorGUILayout.HelpBox(
                "[Purpose of this tool]\n" +
                "Import a 480-960 fps short video, bake to a 24/30/60 fps video with high-quality motion blur & AA produced by sub-frame accumulation, similar to UnrealEngine Movie Render Queue's temporal sampling AA\n\n" +
                "[How to use?]\n" +
                "1. Locate your own ffmpeg.exe (download the latest from ffmpeg.org)\n" +
                "2. Select a 480~960FPS video as Input Video(e.g., set NiloToonMotionBlurVolume = 0.5~0.25 intensity, then record using Unity's Recorder with 480~960 FPS into ProRes422HQ)\n" +
                "3. Click 'Bake now!', this tool will bake motion blur & AA into a result 24/30/60 fps output video", MessageType.Info);
            
            // add here
            ClickableLink("Online Document", "https://docs.google.com/document/d/1iEh1E5xLXnXuICM0ElV3F3x_J2LND9Du2SSKzcIuPXw/edit?tab=t.0#heading=h.ltfmyfb4gf3e");
            ClickableLink("Download FFmpeg", "https://ffmpeg.org");
            
            GUI.enabled = ffmpegProcess == null;

            ////////////////////////////////////////////////////////////////////////
            // FFmpeg Path
            ////////////////////////////////////////////////////////////////////////
            GUILayout.Label("FFmpeg Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            ffmpegPath = EditorGUILayout.TextField("Path", ffmpegPath);
            if (GUILayout.Button("...", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select FFmpeg Executable", "", "exe");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    ffmpegPath = selectedPath;
                    SaveFFmpegPath();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(25);

            ////////////////////////////////////////////////////////////////////////
            // Input Video Path
            ////////////////////////////////////////////////////////////////////////
            GUILayout.Label("Input Video", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            inputFilePath = EditorGUILayout.TextField("Path", inputFilePath);
            if (GUILayout.Button("...", GUILayout.Width(30)) && ffmpegProcess == null)
            {
                string newPath = EditorUtility.OpenFilePanel("Select Input File", "", "mov,mp4");
                if (!string.IsNullOrEmpty(newPath))
                {
                    inputFilePath = newPath;
                    ExtractVideoInfo(inputFilePath); // Extract the FPS and duration of the selected video
                }
            }
            EditorGUILayout.EndHorizontal();

            if (inputVideoFPS < 480)
            {
                EditorGUILayout.HelpBox("Expect a video with 480~960FPS", MessageType.Info);
            }

            // Display error message if input video path is invalid
            if (!string.IsNullOrEmpty(inputFilePath) && !File.Exists(inputFilePath))
            {
                EditorGUILayout.HelpBox("Input video file not found at the specified path.", MessageType.Error);
            }

            ////////////////////////////////////////////////////////////////////////
            // Input Video FPS
            ////////////////////////////////////////////////////////////////////////
            if (inputVideoFPS > 0 && File.Exists(inputFilePath))
            {
                GUILayout.Label($"Input Video FPS: {inputVideoFPS}", EditorStyles.boldLabel);
            }

            GUILayout.Space(25);

            ////////////////////////////////////////////////////////////////////////
            // Output Video Settings
            ////////////////////////////////////////////////////////////////////////

            GUILayout.Label("Output Video", EditorStyles.boldLabel);

            // Codec Selection
            selectedCodecIndex = EditorGUILayout.Popup(new GUIContent("Codec"), selectedCodecIndex, codecOptions);

            if (fpsOptions.Count > 0)
            {
                selectedFPSIndex = Mathf.Clamp(selectedFPSIndex, 0, fpsOptions.Count - 1);
                int newSelectedFPSIndex = EditorGUILayout.Popup("FPS", selectedFPSIndex, fpsOptions.ConvertAll(f => f.ToString()).ToArray());
                if (newSelectedFPSIndex != selectedFPSIndex)
                {
                    selectedFPSIndex = newSelectedFPSIndex;
                    UnityEngine.Debug.Log($"Selected FPS: {GetOutputFPS()}"); // Log the selected FPS
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No valid FPS options available.", MessageType.Warning);
            }

            motionBlurAmount = EditorGUILayout.Slider("Motion Blur", motionBlurAmount, 0f, 4f);

            // CRF Slider (Only for codecs that support it)
            if (selectedCodecIndex != 3) // Not ProRes
            {
                crf = EditorGUILayout.IntSlider(new GUIContent("CRF", "For lossless encoding, set CRF to 0. Higher values reduce quality."), crf, 0, 51);
                if (crf == 0)
                {
                    EditorGUILayout.HelpBox("CRF is set to 0: Lossless encoding. This results in the highest quality and largest file size.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("CRF controls the quality of the video. Lower values mean higher quality and larger file sizes.", MessageType.Info);
                }
            }

            customSuffix = EditorGUILayout.TextField("Suffix", customSuffix);

            ////////////////////////////////////////////////////////////////////////
            // Output Video Info
            ////////////////////////////////////////////////////////////////////////
            if (fpsOptions.Count > 0)
            {
                Generate_tmixFilters(inputVideoFPS);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Output info:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    $"- Temporal Sample(TMix) Count: {currenttmixFrameCount} (>=16 is good for cinematic output)\n" +
                    $"- Shutter Angle in terms of 24fps: {GetShutterAngleInTermsOf24fps()} degrees (~180 is a good default for cinematic output)\n" +
                    $"- Shutter Speed: {GetShutterSpeedDisplayString()} (~1/48 is a good default for cinematic output)"
                    , MessageType.Info);

                // Check if shutter angle is too small
                if (currenttmixFrameCount < Mathf.CeilToInt(inputVideoFPS / GetOutputFPS() / 2))
                {
                    EditorGUILayout.HelpBox(
                        $"Current Shutter Angle for {GetOutputFPS()}fps is < 180, while it is not wrong, it may produce not enough motion blur"
                        , MessageType.Warning);
                }
            }

            GUILayout.Space(25);

            EditorGUILayout.LabelField("Extra note:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"- Make sure Project Settings > VFX > Fixed Time Step is using '1/fps of recorder' when recording\n" +
                $"- If fps is high (e.g., 900fps or higher), it may break cinemachine's camera movement\n" +
                $"- Example input video: 1440p 960fps, or 2160p 480fps"
                , MessageType.Info);

            GUI.enabled = true;
            ////////////////////////////////////////////////////////////////////////
            // Bake Button
            ////////////////////////////////////////////////////////////////////////

            // Disable the "Bake Now!" button if any condition is not met
            GUI.enabled = ffmpegProcess == null && ValidatePaths() && fpsOptions.Count > 0;
            if (GUILayout.Button("Bake now!") && ffmpegProcess == null && ValidatePaths() && fpsOptions.Count > 0)
            {
                SaveFFmpegPath(); // Save the FFmpeg path when starting the conversion

                // Generate the output file path based on the input file path and custom suffix
                outputFilePath = GenerateOutputFilePath(inputFilePath, customSuffix);

                // Check if the output file already exists
                if (File.Exists(outputFilePath))
                {
                    // Ask the user if they want to overwrite the existing file
                    bool overwrite = EditorUtility.DisplayDialog(
                        "File Exists",
                        "The output file already exists. Do you want to overwrite it?",
                        "Yes",
                        "No"
                    );

                    if (!overwrite)
                    {
                        return;
                    }

                    // Delete the existing file
                    File.Delete(outputFilePath);
                }

                // Start the FFmpeg process as a coroutine
                coroutine = EditorCoroutine.StartCoroutine(StartFFmpegProcessCoroutine(outputFilePath, GetOutputFPS()));
            }

            GUI.enabled = true;

            if (ffmpegProcess != null && GUILayout.Button("Cancel"))
            {
                CancelFFmpegProcess();
            }

            EditorGUILayout.EndScrollView();
        }

        private bool ValidatePaths()
        {
            bool valid = true;
            errorMessage = string.Empty;

            if (!File.Exists(ffmpegPath))
            {
                errorMessage += "FFmpeg executable not found at the specified path.\n";
                valid = false;
            }

            if (!File.Exists(inputFilePath))
            {
                errorMessage += "Input video file not found at the specified path.\n";
                valid = false;
            }

            return valid;
        }

        private void SaveFFmpegPath()
        {
            EditorPrefs.SetString(FFmpegPathKey, ffmpegPath);
        }

        private void LoadFFmpegPath()
        {
            ffmpegPath = EditorPrefs.GetString(FFmpegPathKey, @"C:\path\to\ffmpeg.exe");
        }

        private float GetOutputFPS()
        {
            return fpsOptions[selectedFPSIndex];
        }

        private float GetShutterAngleInTermsOf24fps()
        {
            return (float)currenttmixFrameCount / (inputVideoFPS / 24f) * 360f;
        }
        private float GetShutterSpeed()
        {
            return (1f / 24f) * (GetShutterAngleInTermsOf24fps() / 360f);
        }

        private string GetShutterSpeedDisplayString()
        {
            return $"1/{FormatNumber(1 / GetShutterSpeed())}s";
        }

        private string GetShutterSpeedFileNameString()
        {
            return $"OneOver{FormatNumber(1 / GetShutterSpeed())}s";
        }

        // Helper method to format numbers without unnecessary trailing zeros
        private string FormatNumber(double number)
        {
            if (number % 1 == 0)
                return number.ToString("0");
            else
                return number.ToString("0.##");
        }

        private string GenerateOutputFilePath(string inputPath, string suffix)
        {
            suffix = suffix.Replace("<fps>", $"{GetOutputFPS()}fps");
            suffix = suffix.Replace("<shutterspeed>", $"{GetShutterSpeedFileNameString()}");

            // Handle codec placeholder
            if (suffix.Contains("<codec>"))
            {
                // Get codec name and format it for filename
                string codecName = codecOptions[selectedCodecIndex];
                codecName = codecName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace("-", "");

                suffix = suffix.Replace("<codec>", codecName);
            }

            string directory = Path.GetDirectoryName(inputPath);
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            return Path.Combine(directory, $"{filenameWithoutExtension}{suffix}{extension}");
        }

        private IEnumerator StartFFmpegProcessCoroutine(string outputFilePath, float selectedFPS)
        {
            string tmixFilters = Generate_tmixFilters(inputVideoFPS);

            // Record start time
            processStartTime = DateTime.Now;

            // Calculate totalFrames here, based on the selected output FPS and the input video duration
            if (selectedFPS > 0 && inputVideoDuration > 0)
            {
                totalFrames = (int)(selectedFPS * inputVideoDuration);
                UnityEngine.Debug.Log($"Calculated Total Output Frames (using output video FPS {selectedFPS}): {totalFrames}");
            }
            else
            {
                totalFrames = 0;
                UnityEngine.Debug.LogError("Failed to calculate total frames due to invalid output FPS or video duration.");
            }

            // Log start of baking process with output file name
            UnityEngine.Debug.Log($"Baking started. Output file: {outputFilePath}");

            // Select the FFmpeg arguments template based on the selected codec
            string ffmpegArgumentsTemplate = GetFFmpegArgumentsTemplate();

            // Prepare the FFmpeg arguments
            string ffmpegArguments = "";

            if (selectedCodecIndex == 0 || selectedCodecIndex == 1 || selectedCodecIndex == 2) // H.264, H.265
            {
                ffmpegArguments = string.Format(ffmpegArgumentsTemplate, tmixFilters, selectedFPS, crf);
            }
            else if (selectedCodecIndex == 3) // ProRes
            {
                ffmpegArguments = string.Format(ffmpegArgumentsTemplate, tmixFilters, selectedFPS);
            }

            // Use '-hide_banner' to suppress unnecessary output
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-hide_banner -i \"{inputFilePath}\" {ffmpegArguments} \"{outputFilePath}\"",
                RedirectStandardOutput = false, // Do not redirect standard output
                RedirectStandardError = true,   // Redirect standard error to capture errors and progress
                UseShellExecute = false,        // Must be false
                CreateNoWindow = true
            };

            ffmpegProcess = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            ffmpegProcess.ErrorDataReceived += FfmpegProcess_ErrorDataReceived;
            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();

            // Wait for the process to exit
            while (!ffmpegProcess.HasExited)
            {
                yield return null;
            }

            ffmpegProcess.WaitForExit();
            ffmpegProcess = null;

            // Calculate elapsed time
            TimeSpan elapsedTime = DateTime.Now - processStartTime;

            // Log completion message
            string timeSpent = $"Time spent = {elapsedTime.Hours} hours {elapsedTime.Minutes} minutes {elapsedTime.Seconds} seconds, video completed ({Path.GetFileName(outputFilePath)}).";
            UnityEngine.Debug.Log(timeSpent);
        }

        private void FfmpegProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                string data = e.Data;
                HandleFFmpegOutput(data);
            }
        }

        private void HandleFFmpegOutput(string data)
        {
            // Parse FFmpeg output to extract meaningful information
            if (IsCriticalFfmpegMessage(data))
            {
                UnityEngine.Debug.LogError("FFmpeg Error: " + data);
            }
            else
            {
                // Parse progress information
                // FFmpeg outputs progress in lines like: "frame=123 fps=45 q=28.0 size=1234kB time=00:00:05.12 bitrate=1976.3kbits/s speed=1.23x"

                if (data.StartsWith("frame="))
                {
                    // Regular expression to parse the progress line
                    var progressMatch = Regex.Match(data, @"frame=\s*(\d+)\s.*?time=\s*(\S+)\s.*?speed=\s*([\d\.]+)x");

                    if (progressMatch.Success)
                    {
                        string frameStr = progressMatch.Groups[1].Value;
                        string timeStr = progressMatch.Groups[2].Value;
                        string speedStr = progressMatch.Groups[3].Value;

                        if (int.TryParse(frameStr, out int currentFrame))
                        {
                            float percentage = 0f;
                            if (totalFrames > 0)
                            {
                                percentage = (float)currentFrame / totalFrames * 100f;
                                percentage = Mathf.Clamp(percentage, 0f, 100f);
                            }

                            // Only log if at least 2 seconds have passed since last log
                            if ((DateTime.Now - lastLogTime).TotalSeconds >= 2)
                            {
                                TimeSpan elapsedTime = DateTime.Now - processStartTime;

                                string estimatedTimeRemainingString = "Unknown";
                                if (percentage > 0)
                                {
                                    double estimatedTotalSeconds = elapsedTime.TotalSeconds / (percentage / 100);
                                    TimeSpan estimatedRemainingTime = TimeSpan.FromSeconds(estimatedTotalSeconds - elapsedTime.TotalSeconds);

                                    estimatedTimeRemainingString = $"{(int)estimatedRemainingTime.TotalHours}h {estimatedRemainingTime.Minutes}m {estimatedRemainingTime.Seconds}s";
                                }

                                string elapsedTimeString = $"{(int)elapsedTime.TotalHours}h {elapsedTime.Minutes}m {elapsedTime.Seconds}s";

                                string logMessage = $"Frame {currentFrame}/{totalFrames} ({percentage:F1}%), Processed time: {elapsedTimeString}, Estimated time remaining: {estimatedTimeRemainingString}, Speed {speedStr}x";

                                UnityEngine.Debug.Log(logMessage);

                                lastLogTime = DateTime.Now;
                            }
                        }
                    }
                }
                // Optionally, handle other outputs or suppress less important messages
            }
        }

        private bool IsCriticalFfmpegMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            // Check if message contains 'Error' or 'Invalid' etc.
            string lowerMessage = message.ToLowerInvariant();
            return lowerMessage.Contains("error") || lowerMessage.Contains("invalid") || lowerMessage.Contains("failed");
        }

        private void CancelFFmpegProcess()
        {
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                ffmpegProcess.Kill();
                ffmpegProcess.WaitForExit(); // Ensure the process has completely exited
                ffmpegProcess = null;
                if (coroutine != null)
                {
                    EditorCoroutine.StopCoroutine(coroutine);
                    coroutine = null;
                }

                UnityEngine.Debug.Log("FFmpeg process cancelled.");

                // Delete the incomplete output file
                if (File.Exists(outputFilePath))
                {
                    try
                    {
                        File.Delete(outputFilePath);
                        UnityEngine.Debug.Log("Incomplete output file deleted.");
                    }
                    catch (IOException e)
                    {
                        UnityEngine.Debug.LogError($"Failed to delete incomplete output file: {e.Message}");
                    }
                }
            }
        }

        private void ExtractVideoInfo(string videoPath)
        {
            inputVideoFPS = 0f;
            inputVideoDuration = 0.0;

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-hide_banner -i \"{videoPath}\"",
                RedirectStandardOutput = false, // FFmpeg outputs video information to standard error stream
                RedirectStandardError = true,    // Redirect standard error stream
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = processStartInfo
            };

            process.ErrorDataReceived += ProcessOutputHandler;

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();

            // After processing, check if Duration was successfully extracted
            if (inputVideoDuration <= 0)
            {
                UnityEngine.Debug.LogError("Failed to extract Duration from the input video.");
            }
        }

        private void ProcessOutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            // UnityEngine.Debug.Log("FFmpeg Output: " + e.Data); // Optionally log output

            // Extract FPS (input video FPS)
            var matchFPS = Regex.Match(e.Data, @"Video:.*?(\d+(\.\d+)?) fps");
            if (matchFPS.Success)
            {
                if (float.TryParse(matchFPS.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fps))
                {
                    inputVideoFPS = fps;
                    UnityEngine.Debug.Log("Input Video FPS: " + inputVideoFPS);
                    UpdateFPSOptions(inputVideoFPS);
                }
            }

            // Extract duration
            var matchDuration = Regex.Match(e.Data, @"Duration: (\d+):(\d+):(\d+(\.\d+)?)");
            if (matchDuration.Success)
            {
                int hours = int.Parse(matchDuration.Groups[1].Value);
                int minutes = int.Parse(matchDuration.Groups[2].Value);
                double seconds = double.Parse(matchDuration.Groups[3].Value, CultureInfo.InvariantCulture);
                inputVideoDuration = hours * 3600 + minutes * 60 + seconds;
                UnityEngine.Debug.Log("Input Video Duration: " + inputVideoDuration + " seconds");
            }
        }

        private void UpdateFPSOptions(float inputFPS)
        {
            fpsOptions.Clear();
            float[] supportedOutputFPS = { 24, 25, 30, 50, 60 };

            foreach (var fps in supportedOutputFPS)
            {
                if (inputFPS >= fps * 2)
                {
                    fpsOptions.Add(fps);
                }
            }

            selectedFPSIndex = fpsOptions.Count - 1; // Set the highest FPS
        }

        private string GetFFmpegArgumentsTemplate()
        {
            switch (selectedCodecIndex)
            {
                case 0: // H.264
                    return ffmpegArgumentsTemplate_H264;
                case 1: // H.265 (8-bit)
                    return ffmpegArgumentsTemplate_H265;
                case 2: // H.265 (10-bit)
                    return ffmpegArgumentsTemplate_H265_10bit;
                case 3: // ProRes 4444 XQ
                    return ffmpegArgumentsTemplate_ProRes;
                default:
                    return ffmpegArgumentsTemplate_H264;
            }
        }

        private string Generate_tmixFilters(float inputVideoFPS)
        {
            int numberOfTMixFrames = Mathf.FloorToInt(inputVideoFPS * cameraExposureDuration * motionBlurAmount);
            
            // ensure atleast 1 frame when motionBlurAmount is 0
            numberOfTMixFrames = Mathf.Max(numberOfTMixFrames, 1);
            
            currenttmixFrameCount = numberOfTMixFrames;

            if (currenttmixFrameCount == 0)
            {
                return "";
            }

#if METHOD1
            string weightString = string.Join(" ", Enumerable.Repeat("1", numberOfTMixFrames));
#endif

#if METHOD2
            // [2-way expo falloff]

            // Generate exponential falloff weights
            List<int> weights = new List<int>();
            int centerFrame = numberOfTMixFrames / 2;
            float falloffRate = 0.5f; // Adjust this value to control the steepness of the falloff

            for (int i = 0; i < numberOfTMixFrames; i++)
            {
                float distance = Mathf.Abs(i - centerFrame);
                int weight = Mathf.RoundToInt(100 * Mathf.Exp(-falloffRate * distance));
                weights.Add(weight);
            }

            // Ensure the center weight is always 100
            weights[centerFrame] = 100;

            string weightString = string.Join(" ", weights);
            //-------------------------------------------------------------------------------------------------------
#endif

#if METHOD3
            //-------------------------------------------------------------------------------------------------------
            // [1-way expo falloff]

            // Generate exponential falloff weights
            List<int> weights = new List<int>();
            float falloffRate = 2.5f / numberOfTMixFrames; // Adjust this value to control the steepness of the falloff

            for (int i = 0; i < numberOfTMixFrames; i++)
            {
                float weight = Mathf.Exp(falloffRate * i);
                weights.Add(Mathf.RoundToInt(weight * 100)); // Scale up and round to get integer weights
            }

            // Normalize weights to ensure the last (most recent) weight is always 100
            int maxWeight = weights[weights.Count - 1];
            for (int i = 0; i < weights.Count; i++)
            {
                weights[i] = Mathf.RoundToInt((float)weights[i] / maxWeight * 100);
            }

            // Ensure no weight is zero
            weights = weights.Select(w => Mathf.Max(w, 1)).ToList();

            string weightString = string.Join(" ", weights);
            //-------------------------------------------------------------------------------------------------------
#endif

#if METHOD4
            string weightString = GenerateGaussianWeights(numberOfTMixFrames);
#endif
            return $"tmix=frames={numberOfTMixFrames}:weights='{weightString}',";
        }

        private string GenerateGaussianWeights(int numberOfTMixFrames)
        {
            numberOfTMixFrames = Mathf.Min(40, Mathf.Max(1, numberOfTMixFrames));
            float sigma = numberOfTMixFrames / 6f; // Adjust sigma based on frame count

            List<float> weights = new List<float>();
            float sum = 0;

            for (int i = 0; i < numberOfTMixFrames; i++)
            {
                float x = (i - (numberOfTMixFrames - 1) / 2f) / sigma;
                float weight = Mathf.Exp(-(x * x) / 2f);
                weights.Add(weight);
                sum += weight;
            }

            const int scale = 1000;
            List<int> scaledWeights = weights.Select(w => (int)Mathf.Round(w / sum * scale)).ToList();

            int currentSum = scaledWeights.Sum();
            if (currentSum != scale)
            {
                int diff = scale - currentSum;
                int middleIndex = scaledWeights.Count / 2;
                scaledWeights[middleIndex] += diff;
            }

            return string.Join(" ", scaledWeights);
        }
    }

    // Helper class to start coroutines in the editor
    public static class EditorCoroutine
    {
        private class CoroutineHolder : MonoBehaviour { }

        private static CoroutineHolder coroutineHolder;

        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (coroutineHolder == null)
            {
                GameObject newGO = new GameObject("EditorCoroutine");
                newGO.hideFlags = HideFlags.HideAndDontSave;
                coroutineHolder = newGO.AddComponent<CoroutineHolder>();
                coroutineHolder.hideFlags = HideFlags.HideAndDontSave;
            }

            return coroutineHolder.StartCoroutine(coroutine);
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            if (coroutineHolder != null)
            {
                coroutineHolder.StopCoroutine(coroutine);
            }
        }
    }
}