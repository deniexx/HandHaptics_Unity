# Hand Haptics Unity

## Importing:

1. Import the files into Unity Editor under your Scripts folder
2. Go to Edit -> Project Settings -> Player
3. Under "Scripting Define Symbols" add **UNITY_INCLUDE_FULL_SERIALPORT**

## Usage

```c#
public class Game : MonoBehaviour
{
    HandFeedbackConfig config;

    private const ushort leftHandComPort = 6;
    private const ushort rightHandComPort = 11;

    private void Awake()
    {
        HapticHandFeedback.Instance.Initialize(
            leftHandComPort, rightHandComPort);

        config = new() {
            Hand = HapticHand.left;
            Location = FeedbackLocation.Thumb;
            NormalizedStrength = 1f; // Full Strength
            Duration = 1f; // 1 second duration
        };
    }

    private void Update()
    {
        if (input.GetMouseButton(0))
        {
            HaptichandFeedback.Instance.ApplyFeedback(config);
        }
    }

    private void OnDestroyed()
    {
        HapticHandFeedback.Close();
    }
}
```

## Troubleshooting

### SerialCom errors
> Go to -> Project Settings -> Player and set Api Compability Level to **.NET Framework**