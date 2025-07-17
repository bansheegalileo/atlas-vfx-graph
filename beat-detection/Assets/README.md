# Beat Detection System

A Unity system for detecting beats in music.

## Quick Setup

1. Add `BeatDetection` script to a GameObject with an `AudioSource`
2. Assign your music clip to the AudioSource
3. Configure beat actions in the inspector
4. Press play and enjoy beat-synced effects

## Scripts

### BeatDetection (BeatDetection.cs)
Main beat detection component that analyzes audio and triggers effects.

**Key Settings:**
- `Use Automatic BPM Detection`: Auto-detect beats vs manual BPM
- `Beat Sensitivity`: How sensitive to beats (1-5)
- `Manual BPM`: Set exact BPM if known

### CustomMusic (CustomMusic.cs)
Loads custom music files at runtime.

**Usage:**
- Press 'L' key to load music from CustomMusic folder
- Supports MP3, WAV, OGG files

## Beat Actions

Configure what happens on beats:
- **Flash Objects**: Make objects flash colors
- **Scale Objects**: Make objects grow/shrink
- **VFX Events**: Trigger particle effects
- **Animation**: Trigger animator states
- **Custom Events**: Execute your own code

## Frequency Bands

The system analyzes 7 frequency bands:
- Band 0: Bass/Kick drums (20-96 Hz)
- Band 1-6: Mid to high frequencies

## API Reference

### Public Methods
- `GetDetectedBPM()` - Returns current BPM
- `SetManualBPM(float bpm)` - Set manual BPM
- `SwitchBPMMode(bool useAutomatic)` - Toggle detection modes
- `ResetBeatCount()` - Reset beat counting

### Example Usage
```csharp
// Get current BPM
float bpm = beatDetector.GetDetectedBPM();

// Switch to manual mode
beatDetector.SwitchBPMMode(false);
beatDetector.SetManualBPM(128f);

// Reset beat counter
beatDetector.ResetBeatCount();
'''

## Configuration

### Automatic Beat Detection
- Beat Sensitivity: 1-5 (2.0 recommended)
- Beat Threshold: 0.015 (default)
- Expected BPM Range: 120 (estimate)

### Manual BPM Mode
- Manual BPM: 60-200
- Beat Offset: Fine-tune timing

## Band Actions

Each frequency band can trigger:

- Object flashing with custom colors
- Transform scaling animations
- VFX Graph events
- Audio feedback
- Custom code execution

## Requirements
- Unity 6000.1+
- Visual Effect Graph package
- URP (Universal Render Pipeline)

## Troubleshooting

**Beats Not Detected**

Quick Fixes:

- Lower Beat Sensitivity to 1.5-2.0
- Switch to Manual BPM mode (set exact BPM)
- Make sure AudioSource is playing
- Use music with clear kick drums

**Too Many False Beats**

Quick Fixes:

- Increase Beat Sensitivity to 3.0-4.0
- Raise Beat Threshold to 0.02-0.03
- Use Manual BPM for precise control

**Visual Effects Not Working**

Quick Fixes:

- Check objects are assigned in inspector
- Make sure actions are enabled
- Verify objects have SpriteRenderer or Image components
- Test with simple GameObject first

**VFX Graph Not Triggering**

Quick Fixes:

- Check event name matches exactly in VFX Graph
- Ensure VFX Graph is assigned to beat action
- Verify VFX Graph has event context setup

**Performance Issues**

Quick Fixes:

- Lower Audio Analysis Rate to 30Hz
- Disable unused Band Actions
- Reduce VFX particle counts
- Use fewer simultaneous effects

**Beat Timing is Off**

Quick Fixes:

- Use Manual BPM mode for precise timing
- Adjust Beat Offset (-0.1 to +0.1)
- Check song BPM with online tools
- Ensure AudioSource pitch is 1.0

## In Closing-
Use this if you want to. Do whatever you want with it. I'm not your boss.