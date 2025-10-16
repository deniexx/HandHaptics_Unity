using System;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

namespace HandHaptics
{
    public enum FeedbackLocation : byte
    {
        Thumb  = 0b00000001,
        Index  = 0b00000010,
        Middle = 0b00000100,
        Ring   = 0b00001000,
        Pinky  = 0b00010000,
    }

    public enum HapticHand : byte
    {
        Left,
        Right,
        Both
    }

    public struct HandFeedbackConfig
    {
        public HapticHand Hand;
        public FeedbackLocation Location;
        public float NormalizedStrength; // Strength in 0-1
        public float Duration;
    }
    
    public class HapticHandFeedback
    {
        private SerialPort _leftHandPort;
        private SerialPort _rightHandPort;
        
        public static HapticHandFeedback Instance {
            get
            {
                if (Instance == null)
                {
                    Instance = new HapticHandFeedback();
                }
                
                return Instance;
            }
            private set => Instance = value;
        }

        private Dictionary<FeedbackLocation, double> _fingerToSendTimeRight;
        private Dictionary<FeedbackLocation, double> _fingerToSendTimeLeft;

        /// <summary>
        /// Initializes the Com Port connections and sets up the dictionaries
        /// </summary>
        /// <param name="leftHandComPort"> The Port number of the Left Hand Com port</param>
        /// <param name="rightHandComPort"> The Port number of the Right Hand Com port</param>
        public void Initialize(ushort leftHandComPort, ushort rightHandComPort)
        {
            _leftHandPort = new SerialPort($"COM{leftHandComPort}", 9600, Parity.None, 8, StopBits.None);
            _leftHandPort.Open();
            _rightHandPort = new SerialPort($"COM{rightHandComPort}", 9600, Parity.None, 8, StopBits.None);
            _rightHandPort.Open();
            
            if (!_leftHandPort.IsOpen)
            {
                Debug.LogError($"Could not open serial port {leftHandComPort} for Left Hand!");
            }

            if (!_rightHandPort.IsOpen)
            {
                Debug.LogError($"Could not open serial port {rightHandComPort} for Right Hand!");
            }
            
            _fingerToSendTimeRight = new Dictionary<FeedbackLocation, double>
            {
                { FeedbackLocation.Thumb, Time.timeAsDouble },
                { FeedbackLocation.Index, Time.timeAsDouble },
                { FeedbackLocation.Middle, Time.timeAsDouble },
                { FeedbackLocation.Ring, Time.timeAsDouble },
                { FeedbackLocation.Pinky, Time.timeAsDouble }
            };

            _fingerToSendTimeLeft = new Dictionary<FeedbackLocation, double>
            {
                { FeedbackLocation.Thumb, Time.timeAsDouble },
                { FeedbackLocation.Index, Time.timeAsDouble },
                { FeedbackLocation.Middle, Time.timeAsDouble },
                { FeedbackLocation.Ring, Time.timeAsDouble },
                { FeedbackLocation.Pinky, Time.timeAsDouble }
            };
        }

        /// <summary>
        /// Closes the serial port connections
        /// </summary>
        public void Close()
        {
            _leftHandPort.Close();
            _rightHandPort.Close();
        }

        /// <summary>
        /// Applies Feedback by a given config<para />
        /// @NOTE: This can sometimes not write if there is already a haptic effect playing
        /// </summary>
        /// <param name="config">The config of applied feedback</param>
        public void ApplyFeedback(HandFeedbackConfig config)
        {
            if (config.Hand is HapticHand.Left or HapticHand.Both)
            {
                if (!_leftHandPort.IsOpen)
                {
                    Debug.LogWarning("Attempting to apply feedback to Left Hand, but the serial com is not open!");
                }

                if (_fingerToSendTimeLeft[config.Location] > Time.timeAsDouble)
                {
                    return;
                }
                
                _fingerToSendTimeLeft[config.Location] = Time.timeAsDouble + config.Duration - 0.05f;
                SendFeedback(_leftHandPort, config);
            }

            if (config.Hand is HapticHand.Right or HapticHand.Both)
            {
                if (!_rightHandPort.IsOpen)
                {
                    Debug.LogWarning("Attempting to apply feedback to Right Hand, but the serial com is not open!");
                }
                
                if (_fingerToSendTimeRight[config.Location] > Time.timeAsDouble)
                {
                    return;
                }
                
                _fingerToSendTimeRight[config.Location] = Time.timeAsDouble + config.Duration - 0.05f;
                SendFeedback(_rightHandPort, config);
            }
        }

        private void SendFeedback(SerialPort port, HandFeedbackConfig config)
        {
            float clampedStrength = Mathf.Clamp(config.NormalizedStrength, 0f, 1f);

            byte location = (byte)(config.Location);
            byte strength = (byte)(255f * clampedStrength);
            port.Write(new byte[] {location, strength}, 0, 2);
            
            var bytes = BitConverter.GetBytes(config.Duration);
            port.Write(bytes, 0, bytes.Length);
            
            port.Write(new byte[] {0, 0}, 0, 2);
        }
    }
}
