using System;
using System.Runtime.InteropServices;
using System.Security;

namespace HandHaptics
{
    internal static class NativeConstants
    {
        public const string DllName = "HapticHandFeedback.dll";
    }

    /// <summary>
    /// Enum representing the location of haptic feedback on the hand.
    /// MUST match the C++ enum class FeedbackLocation (uint8_t/byte).
    /// </summary>
    public enum FeedbackLocation : byte
    {
        Thumb = 0b00000001,
        Index = 0b00000010,
        Middle = 0b00000100,
        Ring = 0b00001000,
        Pinky = 0b00010000,
    }

    /// <summary>
    /// Enum representing which hand(s) to apply feedback to.
    /// MUST match the C++ enum class HapticHand (uint8_t/byte).
    /// </summary>
    public enum HapticHand : byte
    {
        Left,
        Right,
        Both
    }

    /// <summary>
    /// Struct containing the configuration for a single haptic pulse.
    /// MUST use Sequential layout to match C++ struct padding/order.
    /// C++ fields: HapticHand (byte), FeedbackLocation (byte), float, float.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HandFeedbackConfig
    {
        public HapticHand Hand;
        public FeedbackLocation Location;
        public float NormalizedStrength; // Strength in 0-1
        public float Duration;           // Duration in seconds
    }

    /// <summary>
    /// P/Invoke class containing all external C-style functions imported from the DLL.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        private const string Dll = NativeConstants.DllName;
        
        [DllImport(Dll, EntryPoint = "HapticGetSingletonInstance", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSingletonInstance();

        [DllImport(Dll, EntryPoint = "HapticInitialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Initialize(IntPtr instance, ushort leftHandComPort, ushort rightHandComPort);

        [DllImport(Dll, EntryPoint = "HapticClose", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Close(IntPtr instance);

        [DllImport(Dll, EntryPoint = "HapticApplyFeedback", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ApplyFeedback(IntPtr instance, HandFeedbackConfig config);
    }

    /// <summary>
    /// Managed C# wrapper that implements the original singleton pattern
    /// using the native P/Invoke methods. This is the class your C# code should use.
    /// </summary>
    public sealed class HapticHandFeedback
    {
        private static readonly IntPtr _instancePtr;

        static HapticHandFeedback()
        {
            _instancePtr = NativeMethods.GetSingletonInstance();
            if (_instancePtr == IntPtr.Zero)
            {
                throw new ApplicationException($"Failed to retrieve pointer for {NativeConstants.DllName}.");
            }
        }

        public static HapticHandFeedback Instance { get; } = new HapticHandFeedback();
        private HapticHandFeedback() { }

        public void Initialize(ushort leftHandComPort, ushort rightHandComPort)
        {
            NativeMethods.Initialize(_instancePtr, leftHandComPort, rightHandComPort);
        }

        public void Close()
        {
            NativeMethods.Close(_instancePtr);
        }

        public void ApplyFeedback(HandFeedbackConfig config)
        {
            NativeMethods.ApplyFeedback(_instancePtr, config);
        }
    }
}
