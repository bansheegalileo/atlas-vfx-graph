using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Events;
using System;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class BeatDetection : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public float threshold = 0.05f;
    
    [Header("Dynamic Threshold")]
    public bool useDynamicThreshold = true;
    [Range(0.1f, 5f)]
    public float adaptiveMultiplier = 1.5f;
    [Range(0.001f, 0.1f)]
    public float minThreshold = 0.002f;
    [Range(0.01f, 0.5f)]
    public float maxThreshold = 0.1f;
    [Range(30, 300)]
    public int analysisFrames = 120;
    
    [Header("BPM Detection Mode")]
    public bool useAutomaticBPMDetection = true;
    
    [Header("Automatic Beat Detection Settings")]
    [Range(60f, 200f)]
    public float expectedBPMRange = 120f;
    [Range(1f, 5f)]
    public float beatSensitivity = 2f;
    [Range(0.001f, 0.1f)]
    public float beatThreshold = 0.015f;
    [Range(0.2f, 1f)]
    public float minimumBeatInterval = 0.3f;
    public bool useKickDrumFocus = true;
    [Range(10, 100)]
    public int audioAnalysisRate = 60; // Hz - how often to analyze audio (independent of framerate)
    
    [Header("Manual BPM Settings")]
    [Range(60f, 200f)]
    public float manualBPM = 120f;
    [Range(-2f, 2f)]
    public float beatOffset = 0f;
    
    [Header("BPM Event Actions")]
    public BPMAction twoBeatAction;
    public BPMAction fourBeatAction;
    
    [Header("Frequency Band Actions")]
    public BeatAction[] bandActions = new BeatAction[7];
    
    // Time-based detection variables
    private double nextAudioAnalysisTime = 0.0;
    private double audioAnalysisInterval = 0.0;
    private double musicStartDSPTime = 0.0;
    private double nextBeatDSPTime = 0.0;
    private bool musicHasStarted = false;
    
    // Beat Detection Variables
    private float[] beatHistory = new float[32];
    private int beatHistoryIndex = 0;
    private double lastBeatDSPTime = 0.0;
    private float detectedBPM = 120f;
    private int beatCount = 0;
    private float bpmConfidence = 0f;
    
    // Manual BPM Variables
    private double manualBeatInterval = 0.5;
    
    // Low-frequency focused beat detection
    private float[] kickEnergyHistory = new float[32];
    private int kickEnergyIndex = 0;
    private float currentKickEnergy = 0f;
    private float averageKickEnergy = 0f;
    
    // Beat Pattern Tracking
    private float[] recentBeatIntervals = new float[8];
    private int intervalIndex = 0;
    private float averageBeatInterval = 0.5f;
    
    private float[] spectrum = new float[1024];
    private float[] prevBandLevels = new float[7];
    private float[] bandAverages = new float[7];
    private int[] freqRanges = new int[8] { 56, 96, 203, 305, 604, 1775, 2392, 6324 };
    
    // Dynamic threshold calculation variables
    private float[][] recentBandLevels = new float[7][];
    private float[] dynamicThresholds = new float[7];
    private float[] bandVariance = new float[7];
    private int frameCounter = 0;
    private int currentIndex = 0;

    [Serializable]
    public class BPMAction
    {
        [Header("Timing Settings")]
        public bool enabled = true;
        public string actionName = "BPM Event";
        
        [Header("VFX Graph Settings")]
        public VisualEffect vfxGraph;
        public string burstEventName = "OnBPMBeat";
        public int burstCount = 50;
        public float burstIntensity = 2f;
        public bool sendBurstParameters = true;
        
        [Header("Flash Settings")]
        public GameObject objectToFlash;
        public float flashDuration = 0.1f;
        public float fadeDuration = 0.3f;
        public bool useColorFlash = true;
        public Color flashColor = Color.red;
        
        [Header("Scaling Effects")]
        public Transform objectToScale;
        public float scaleMultiplier = 1.5f;
        public float scaleDuration = 0.2f;
        
        [Header("Audio Effects")]
        public AudioSource soundEffect;
        [Range(0.1f, 2f)]
        public float soundPitchVariation = 0.2f;
        
        [Header("Animation Effects")]
        public Animator animatorToTrigger;
        public string animationTrigger = "BPMBeat";
        
        [Header("Custom Events")]
        public UnityEvent onBPMBeat;
        
        private SpriteRenderer spriteRenderer;
        private CanvasGroup canvasGroup;
        private Color originalColor;
        private float originalAlpha;
        private Vector3 originalScale;
        private Coroutine currentCoroutine;
        
        public void Initialize()
        {
            if (!enabled || !objectToFlash) return;
            
            spriteRenderer = objectToFlash.GetComponent<SpriteRenderer>();
            canvasGroup = objectToFlash.GetComponent<CanvasGroup>();
            
            if (spriteRenderer)
                originalColor = spriteRenderer.color;
            
            if (canvasGroup)
                originalAlpha = canvasGroup.alpha;
            
            if (objectToScale)
                originalScale = objectToScale.localScale;
            
            SetObjectVisible(false);
        }
        
        public void TriggerBPMBeat(MonoBehaviour context, float bpmIntensity)
        {
            if (!enabled) return;
            
            if (currentCoroutine != null)
                context.StopCoroutine(currentCoroutine);
            
            if (vfxGraph) TriggerVFXBurst(bpmIntensity);
            
            if (objectToFlash)
                currentCoroutine = context.StartCoroutine(FlashEffect());
            
            if (objectToScale)
                context.StartCoroutine(ScaleEffect());
            
            if (animatorToTrigger) animatorToTrigger.SetTrigger(animationTrigger);
            
            if (soundEffect) 
            {
                soundEffect.pitch = 1f + UnityEngine.Random.Range(-soundPitchVariation, soundPitchVariation);
                soundEffect.Play();
            }
            
            onBPMBeat?.Invoke();
        }
        
        private void TriggerVFXBurst(float intensity)
        {
            if (!vfxGraph) return;
            
            if (sendBurstParameters)
            {
                var eventAttribute = vfxGraph.CreateVFXEventAttribute();
                eventAttribute.SetInt("BurstCount", Mathf.RoundToInt(burstCount * intensity));
                eventAttribute.SetFloat("BurstIntensity", burstIntensity * intensity);
                eventAttribute.SetVector3("BurstColor", new Vector3(flashColor.r, flashColor.g, flashColor.b));
                eventAttribute.SetFloat("RandomSeed", UnityEngine.Random.Range(0f, 1000f));
                vfxGraph.SendEvent(burstEventName, eventAttribute);
            }
            else
            {
                vfxGraph.SendEvent(burstEventName);
            }
        }
        
        private IEnumerator FlashEffect()
        {
            SetObjectVisible(true);
            yield return new WaitForSeconds(flashDuration);
            
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                float progress = elapsed / fadeDuration;
                float alpha = Mathf.Lerp(1f, 0f, progress);
                SetObjectAlpha(alpha);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            SetObjectVisible(false);
            currentCoroutine = null;
        }
        
        private IEnumerator ScaleEffect()
        {
            if (!objectToScale) yield break;
            
            float elapsed = 0f;
            
            while (elapsed < scaleDuration)
            {
                float progress = elapsed / scaleDuration;
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                float currentScale = Mathf.Lerp(scaleMultiplier, 1f, easedProgress);
                
                objectToScale.localScale = originalScale * currentScale;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            objectToScale.localScale = originalScale;
        }
        
        private void SetObjectVisible(bool visible)
        {
            if (!objectToFlash) return;
            
            objectToFlash.SetActive(visible);
            
            if (visible)
            {
                if (spriteRenderer)
                {
                    Color color = useColorFlash ? flashColor : originalColor;
                    spriteRenderer.color = color;
                }
                
                if (canvasGroup)
                {
                    canvasGroup.alpha = originalAlpha;
                }
            }
        }
        
        private void SetObjectAlpha(float alpha)
        {
            if (!objectToFlash) return;
            
            if (spriteRenderer)
            {
                Color color = spriteRenderer.color;
                color.a = originalColor.a * alpha;
                spriteRenderer.color = color;
            }
            
            if (canvasGroup)
            {
                canvasGroup.alpha = originalAlpha * alpha;
            }
        }
    }

    [Serializable]
    public class BeatAction
    {
        [Header("VFX Graph Settings")]
        public VisualEffect vfxGraph;
        public string burstEventName = "OnBurst";
        public int burstCount = 30;
        public float burstIntensity = 1f;
        public bool sendBurstParameters = true;
        
        [Header("Flash Settings")]
        public GameObject objectToFlash;
        public float flashDuration = 0.05f;
        public float fadeDuration = 0.2f;
        public bool useColorFlash = true;
        public Color flashColor = Color.white;
        
        [Header("Cooldown Settings")]
        public bool enableCooldown = true;
        public float cooldownTime = 0.1f;
        
        [Header("Continuous Y-Axis Scaling")]
        public Transform objectToScale;
        [Range(1f, 5f)]
        public float minScaleMultiplier = 1.0f;
        [Range(1f, 10f)]
        public float maxScaleMultiplier = 3f;
        [Range(0.1f, 10f)]
        public float loudnessSensitivity = 2f;
        public bool useBandDifference = false;
        [Range(0.01f, 1f)]
        public float scaleSmoothness = 0.1f;
        
        [Header("Audio Effects")]
        public AudioSource soundEffect;
        
        [Header("Animation Effects")]
        public Animator animatorToTrigger;
        public string animationTrigger = "Beat";
        
        [Header("Custom Events")]
        public UnityEvent onBeatDetected;
        
        private SpriteRenderer spriteRenderer;
        private CanvasGroup canvasGroup;
        private Color originalColor;
        private float originalAlpha;
        private Vector3 originalScale;
        private float currentTargetScale = 1f;
        private float currentActualScale = 1f;
        private Coroutine currentFlashCoroutine;
        private double lastTriggerDSPTime = 0.0;
        
        public void Initialize()
        {
            if (objectToFlash)
            {
                spriteRenderer = objectToFlash.GetComponent<SpriteRenderer>();
                canvasGroup = objectToFlash.GetComponent<CanvasGroup>();
                
                if (spriteRenderer)
                    originalColor = spriteRenderer.color;
                
                if (canvasGroup)
                    originalAlpha = canvasGroup.alpha;
                
                SetObjectVisible(false);
            }
            
            if (objectToScale) 
            {
                originalScale = objectToScale.localScale;
                currentTargetScale = 1f;
                currentActualScale = 1f;
            }
        }
        
        public void TriggerBeat(MonoBehaviour context)
        {
            double currentDSPTime = AudioSettings.dspTime;
            
            if (enableCooldown && currentDSPTime - lastTriggerDSPTime < cooldownTime)
                return;
            
            lastTriggerDSPTime = currentDSPTime;
            
            if (vfxGraph) TriggerVFXBurst();
            
            if (currentFlashCoroutine != null)
                context.StopCoroutine(currentFlashCoroutine);
            
            if (objectToFlash) 
                currentFlashCoroutine = context.StartCoroutine(FlashEffect());
            
            if (animatorToTrigger) animatorToTrigger.SetTrigger(animationTrigger);
            if (soundEffect) soundEffect.Play();
            
            onBeatDetected?.Invoke();
        }
        
        public void UpdateContinuousScaling(float bandLoudness, float bandDifference)
        {
            if (!objectToScale) return;
            
            float loudnessValue = useBandDifference ? bandDifference : bandLoudness;
            float normalizedLoudness = Mathf.Clamp01(loudnessValue * loudnessSensitivity);
            currentTargetScale = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, normalizedLoudness);
            
            currentActualScale = Mathf.Lerp(currentActualScale, currentTargetScale, scaleSmoothness);
            
            Vector3 newScale = new Vector3(
                originalScale.x, 
                originalScale.y * currentActualScale, 
                originalScale.z
            );
            
            objectToScale.localScale = newScale;
        }
        
        private void TriggerVFXBurst()
        {
            if (!vfxGraph) return;
            
            if (sendBurstParameters)
            {
                var eventAttribute = vfxGraph.CreateVFXEventAttribute();
                eventAttribute.SetInt("BurstCount", burstCount);
                eventAttribute.SetFloat("BurstIntensity", burstIntensity);
                eventAttribute.SetVector3("BurstColor", new Vector3(flashColor.r, flashColor.g, flashColor.b));
                eventAttribute.SetFloat("RandomSeed", UnityEngine.Random.Range(0f, 1000f));
                vfxGraph.SendEvent(burstEventName, eventAttribute);
            }
            else
            {
                vfxGraph.SendEvent(burstEventName);
            }
        }
        
        private IEnumerator FlashEffect()
        {
            SetObjectVisible(true);
            yield return new WaitForSeconds(flashDuration);
            
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                float progress = elapsed / fadeDuration;
                float alpha = Mathf.Lerp(1f, 0f, progress);
                SetObjectAlpha(alpha);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            SetObjectVisible(false);
            currentFlashCoroutine = null;
        }
        
        private void SetObjectVisible(bool visible)
        {
            if (!objectToFlash) return;
            
            objectToFlash.SetActive(visible);
            
            if (visible)
            {
                if (spriteRenderer)
                {
                    Color color = useColorFlash ? flashColor : originalColor;
                    spriteRenderer.color = color;
                }
                
                if (canvasGroup)
                {
                    canvasGroup.alpha = originalAlpha;
                }
            }
        }
        
        private void SetObjectAlpha(float alpha)
        {
            if (!objectToFlash) return;
            
            if (spriteRenderer)
            {
                Color color = spriteRenderer.color;
                color.a = originalColor.a * alpha;
                spriteRenderer.color = color;
            }
            
            if (canvasGroup)
            {
                canvasGroup.alpha = originalAlpha * alpha;
            }
        }
        
        public float GetCurrentScale() => currentActualScale;
        public bool CanTrigger() => !enableCooldown || AudioSettings.dspTime - lastTriggerDSPTime >= cooldownTime;
    }

    void Start()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        
        InitializeDynamicThreshold();
        InitializeBPMDetection();
        
        foreach (var action in bandActions)
        {
            action?.Initialize();
        }
        
        twoBeatAction?.Initialize();
        fourBeatAction?.Initialize();
    }
    
    void InitializeDynamicThreshold()
    {
        for (int i = 0; i < 7; i++)
        {
            recentBandLevels[i] = new float[analysisFrames];
            dynamicThresholds[i] = threshold;
        }
    }
    
    void InitializeBPMDetection()
    {
        detectedBPM = useAutomaticBPMDetection ? expectedBPMRange : manualBPM;
        manualBeatInterval = 60.0 / manualBPM;
        averageBeatInterval = useAutomaticBPMDetection ? (60f / expectedBPMRange) : (float)manualBeatInterval;
        
        // Initialize audio analysis timing
        audioAnalysisInterval = 1.0 / audioAnalysisRate;
        nextAudioAnalysisTime = AudioSettings.dspTime + audioAnalysisInterval;
        
        for (int i = 0; i < beatHistory.Length; i++)
            beatHistory[i] = -1f;
            
        for (int i = 0; i < recentBeatIntervals.Length; i++)
            recentBeatIntervals[i] = averageBeatInterval;
            
        for (int i = 0; i < kickEnergyHistory.Length; i++)
            kickEnergyHistory[i] = 0f;
            
        musicHasStarted = false;
        nextBeatDSPTime = 0.0;
        musicStartDSPTime = 0.0;
    }

    void Update()
    {
        if (!audioSource.isPlaying) 
        {
            musicHasStarted = false;
            return;
        }

        // Track when music starts using DSP time
        if (!musicHasStarted && audioSource.isPlaying)
        {
            musicHasStarted = true;
            musicStartDSPTime = AudioSettings.dspTime;
            
            if (!useAutomaticBPMDetection)
            {
                nextBeatDSPTime = musicStartDSPTime + beatOffset;
            }
        }

        // Time-based audio analysis (independent of framerate)
        double currentDSPTime = AudioSettings.dspTime;
        
        if (currentDSPTime >= nextAudioAnalysisTime)
        {
            PerformAudioAnalysis();
            nextAudioAnalysisTime += audioAnalysisInterval;
        }
        
        // Handle beat detection based on mode
        if (useAutomaticBPMDetection)
        {
            DetectBeatsAutomatic();
        }
        else
        {
            DetectBeatsManual();
        }
        
        // Visual updates still happen every frame for smooth scaling
        for (int i = 0; i < 7; i++)
        {
            if (bandActions[i] != null)
            {
                float diff = bandAverages[i] - prevBandLevels[i];
                bandActions[i].UpdateContinuousScaling(bandAverages[i], diff);
            }
        }
    }
    
    void PerformAudioAnalysis()
    {
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        AnalyzeBands();
        
        if (useDynamicThreshold)
        {
            UpdateDynamicThresholds();
        }

        // Check frequency band triggers
        for (int i = 0; i < 7; i++)
        {
            float diff = bandAverages[i] - prevBandLevels[i];
            float currentThreshold = useDynamicThreshold ? dynamicThresholds[i] : threshold;

            if (diff > currentThreshold && bandActions[i] != null)
            {
                bandActions[i].TriggerBeat(this);
            }

            prevBandLevels[i] = bandAverages[i];
        }
        
        frameCounter++;
    }
    
    void DetectBeatsManual()
    {
        if (!musicHasStarted) return;
        
        double currentDSPTime = AudioSettings.dspTime;
        
        if (currentDSPTime >= nextBeatDSPTime)
        {
            RegisterBeat(currentDSPTime, 1f);
            nextBeatDSPTime += manualBeatInterval;
            detectedBPM = manualBPM;
            bpmConfidence = 1f;
        }
    }
    
    void DetectBeatsAutomatic()
    {
        if (useKickDrumFocus)
        {
            currentKickEnergy = CalculateKickDrumEnergy();
        }
        else
        {
            currentKickEnergy = 0f;
            for (int i = 0; i < 7; i++)
            {
                currentKickEnergy += bandAverages[i];
            }
        }
        
        kickEnergyHistory[kickEnergyIndex] = currentKickEnergy;
        kickEnergyIndex = (kickEnergyIndex + 1) % kickEnergyHistory.Length;
        
        averageKickEnergy = 0f;
        for (int i = 0; i < kickEnergyHistory.Length; i++)
        {
            averageKickEnergy += kickEnergyHistory[i];
        }
        averageKickEnergy /= kickEnergyHistory.Length;
        
        float dynamicBeatThreshold = Mathf.Max(beatThreshold, averageKickEnergy * beatSensitivity);
        
        bool isBeat = currentKickEnergy > dynamicBeatThreshold;
        
        double currentDSPTime = AudioSettings.dspTime;
        bool intervalOK = (currentDSPTime - lastBeatDSPTime) > minimumBeatInterval;
        
        if (isBeat && intervalOK)
        {
            float beatIntensity = Mathf.Clamp01((currentKickEnergy - averageKickEnergy) / averageKickEnergy);
            RegisterBeat(currentDSPTime, beatIntensity);
        }
    }
    
    float CalculateKickDrumEnergy()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        
        int lowIndex = FrequencyToIndex(20, sampleRate);
        int highIndex = FrequencyToIndex(80, sampleRate);
        
        float kickEnergy = 0f;
        for (int i = lowIndex; i <= highIndex && i < spectrum.Length; i++)
        {
            kickEnergy += spectrum[i];
        }
        
        return kickEnergy / (highIndex - lowIndex + 1);
    }
    
    void RegisterBeat(double currentDSPTime, float beatIntensity)
    {
        beatCount++;
        
        beatHistory[beatHistoryIndex] = (float)currentDSPTime;
        beatHistoryIndex = (beatHistoryIndex + 1) % beatHistory.Length;
        
        if (useAutomaticBPMDetection && lastBeatDSPTime > 0.0)
        {
            float interval = (float)(currentDSPTime - lastBeatDSPTime);
            
            recentBeatIntervals[intervalIndex] = interval;
            intervalIndex = (intervalIndex + 1) % recentBeatIntervals.Length;
            
            CalculateBPM();
        }
        
        lastBeatDSPTime = currentDSPTime;
        CheckBPMEvents(beatIntensity);
    }
    
    void CalculateBPM()
    {
        float intervalSum = 0f;
        int validIntervals = 0;
        
        for (int i = 0; i < recentBeatIntervals.Length; i++)
        {
            if (recentBeatIntervals[i] > 0.2f && recentBeatIntervals[i] < 2f)
            {
                intervalSum += recentBeatIntervals[i];
                validIntervals++;
            }
        }
        
        if (validIntervals > 0)
        {
            averageBeatInterval = intervalSum / validIntervals;
            float newBPM = 60f / averageBeatInterval;
            
            detectedBPM = Mathf.Lerp(detectedBPM, newBPM, 0.3f);
            
            float variance = 0f;
            for (int i = 0; i < recentBeatIntervals.Length; i++)
            {
                if (recentBeatIntervals[i] > 0f)
                {
                    float diff = recentBeatIntervals[i] - averageBeatInterval;
                    variance += diff * diff;
                }
            }
            variance /= validIntervals;
            bpmConfidence = Mathf.Clamp01(1f - (variance * 5f));
        }
    }
    
    void CheckBPMEvents(float beatIntensity)
    {
        if (beatCount % 4 == 2 && twoBeatAction != null && twoBeatAction.enabled)
        {
            twoBeatAction.TriggerBPMBeat(this, beatIntensity);
        }
        
        if (beatCount % 4 == 0 && fourBeatAction != null && fourBeatAction.enabled)
        {
            fourBeatAction.TriggerBPMBeat(this, beatIntensity);
        }
    }
    
    void UpdateDynamicThresholds()
    {
        for (int i = 0; i < 7; i++)
        {
            recentBandLevels[i][currentIndex] = bandAverages[i];
            
            if (frameCounter >= analysisFrames)
            {
                CalculateBandStatistics(i);
                float baseThreshold = bandVariance[i] * adaptiveMultiplier;
                dynamicThresholds[i] = Mathf.Clamp(baseThreshold, minThreshold, maxThreshold);
            }
        }
        
        currentIndex = (currentIndex + 1) % analysisFrames;
    }
    
    void CalculateBandStatistics(int bandIndex)
    {
        float[] band = recentBandLevels[bandIndex];
        
        float mean = 0f;
        for (int i = 0; i < analysisFrames; i++)
        {
            mean += band[i];
        }
        mean /= analysisFrames;
        
        float varianceSum = 0f;
        for (int i = 1; i < analysisFrames; i++)
        {
            float diff = band[i] - band[i - 1];
            varianceSum += diff * diff;
        }
        bandVariance[bandIndex] = Mathf.Sqrt(varianceSum / (analysisFrames - 1));
    }

    void AnalyzeBands()
    {
        int sampleRate = AudioSettings.outputSampleRate;

        for (int i = 0; i < 7; i++)
        {
            float avg = 0;
            int lowFreq = freqRanges[i];
            int highFreq = freqRanges[i + 1];
            int lowIndex = FrequencyToIndex(lowFreq, sampleRate);
            int highIndex = FrequencyToIndex(highFreq, sampleRate);

            for (int j = lowIndex; j <= highIndex; j++)
                avg += spectrum[j];

            bandAverages[i] = avg / (highIndex - lowIndex + 1);
        }
    }

    int FrequencyToIndex(int freq, int sampleRate)
    {
        float fraction = (float)freq / (sampleRate / 2);
        return Mathf.Clamp(Mathf.FloorToInt(fraction * spectrum.Length), 0, spectrum.Length - 1);
    }
    
    // PUBLIC METHODS
    
    public float GetDetectedBPM() => detectedBPM;
    public float GetBPMConfidence() => bpmConfidence;
    public int GetBeatCount() => beatCount;
    public bool IsUsingAutomaticDetection() => useAutomaticBPMDetection;
    public float GetManualBPM() => manualBPM;
    public double GetCurrentDSPTime() => AudioSettings.dspTime;
    public double GetNextBeatDSPTime() => nextBeatDSPTime;
    
    public void ResetBeatCount()
    {
        beatCount = 0;
        lastBeatDSPTime = 0.0;
        musicHasStarted = false;
        nextBeatDSPTime = 0.0;
        
        for (int i = 0; i < beatHistory.Length; i++)
            beatHistory[i] = -1f;
            
        for (int i = 0; i < kickEnergyHistory.Length; i++)
            kickEnergyHistory[i] = 0f;
    }
    
    public void SetManualBPM(float bpm)
    {
        manualBPM = Mathf.Clamp(bpm, 60f, 200f);
        manualBeatInterval = 60.0 / manualBPM;
        
        if (!useAutomaticBPMDetection)
        {
            detectedBPM = manualBPM;
            
            if (musicHasStarted)
            {
                nextBeatDSPTime = lastBeatDSPTime + manualBeatInterval;
                
                while (nextBeatDSPTime <= AudioSettings.dspTime)
                {
                    nextBeatDSPTime += manualBeatInterval;
                }
            }
        }
    }
    
    public void SwitchBPMMode(bool useAutomatic)
    {
        useAutomaticBPMDetection = useAutomatic;
        
        if (!useAutomatic)
        {
            detectedBPM = manualBPM;
            bpmConfidence = 1f;
            
            if (musicHasStarted)
            {
                nextBeatDSPTime = AudioSettings.dspTime + beatOffset;
            }
        }
    }
    
    public void SetAudioAnalysisRate(int rate)
    {
        audioAnalysisRate = Mathf.Clamp(rate, 10, 100);
        audioAnalysisInterval = 1.0 / audioAnalysisRate;
    }
}
