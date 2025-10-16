#pragma once

#include <cstdint>
#include <map>
#include <string>
#include <vector>

#define HAPTIC_API __declspec(dllexport)

enum class FeedbackLocation : uint8_t
{
	Thumb   = 0b00000001,
    Index   = 0b00000010,
    Middle  = 0b00000100,
    Ring    = 0b00001000,
    Pinky   = 0b00010000
};

enum class HapticHand : uint8_t
{
	Left,
	Right,
	Both
};

struct HandFeedbackConfig
{
	HapticHand Hand = HapticHand::Left;
	FeedbackLocation Location = FeedbackLocation::Index;
	float NormalizedStrength = 0; // 0 - 1
	float Duration = 0.f; // in Seconds
};

class SerialPortWrapper
{
public:

	SerialPortWrapper(const std::string& portName, int baudRate, int dataBits);
	~SerialPortWrapper();

	bool Open();
	void Close();
	bool IsOpen() const;
	void Write(uint8_t* data, size_t size);

private:

	struct Impl;
	Impl* pImpl;

	std::string name;
	bool open;
};

class HAPTIC_API HapticHandFeedback
{
public:

	~HapticHandFeedback();
	static HapticHandFeedback& GetInstance();

	void Initialize(uint16_t leftHandComPort, uint16_t rightHandComPort);
	void Close();
	void ApplyFeedback(HandFeedbackConfig config);

private:

	HapticHandFeedback();

	HapticHandFeedback(const HapticHandFeedback&) = delete;
	HapticHandFeedback& operator=(const HapticHandFeedback&) = delete;

	void SendFeedback(SerialPortWrapper& port, HandFeedbackConfig config);

	SerialPortWrapper* leftHandPort;
	SerialPortWrapper* rightHandPort;

	std::map<FeedbackLocation, double> fingerToSendTimeRight;
	std::map<FeedbackLocation, double> fingerToSendTimeLeft;
};


extern "C"
{
	HAPTIC_API void* HapticGetSingletonInstance();

	HAPTIC_API void HapticInitialize(void* instance, uint16_t leftHandComPort, uint16_t rightHandComPort);

	HAPTIC_API void HapticClose(void* instance);

	HAPTIC_API void HapticApplyFeedback(void* instance, HandFeedbackConfig config);
}
