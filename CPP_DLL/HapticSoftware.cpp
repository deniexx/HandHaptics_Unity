#include "HapticSoftware.h"
#include <chrono>
#include <cstdint>
#include <string>
#include "vendor/seriallib/serialib.h"

template<typename T>
T clamp_value(T value, T min, T max)
{
	return max(min, min(value, max));
}

double getTimeAsDouble()
{
	using namespace std::chrono;

	return duration_cast<duration<double>>(system_clock::now().time_since_epoch()).count();
}

struct SerialPortWrapper::Impl {
	serialib serial;
	std::string portName;
	int baudRate;
	int dataBits;
};

SerialPortWrapper::SerialPortWrapper(const std::string& portName, int baudRate, int dataBits)
	: name(portName), open(false)
{
	pImpl = new Impl{ serialib(), portName, baudRate, dataBits };
}

SerialPortWrapper::~SerialPortWrapper()
{
	Close();
	delete pImpl;
}

bool SerialPortWrapper::Open()
{
	char errorOpening = pImpl->serial.openDevice(
		pImpl->portName.c_str(),
		pImpl->baudRate,
		SERIAL_DATABITS_8,
		SERIAL_PARITY_NONE,
		SERIAL_STOPBITS_1);

	if (errorOpening != 1)
	{
		return false;
	}

	open = true;
	return true;
}

void SerialPortWrapper::Close()
{
	if (open)
	{
		pImpl->serial.closeDevice();
		open = false;
	}
}

bool SerialPortWrapper::IsOpen() const
{
	return open;
}

void SerialPortWrapper::Write(uint8_t* data, size_t size)
{
	if (open)
	{
		return;
	}

	int status = pImpl->serial.writeBytes(data, size);
}

HapticHandFeedback& HapticHandFeedback::GetInstance()
{
    static HapticHandFeedback instance;
    return instance;
}

HapticHandFeedback::HapticHandFeedback()
	: leftHandPort(nullptr), rightHandPort(nullptr)
{}

HapticHandFeedback::~HapticHandFeedback()
{
	Close();
}

void HapticHandFeedback::Initialize(uint16_t leftHandComPort, uint16_t rightHandComPort)
{
	if (leftHandPort) delete leftHandPort;
	if (rightHandPort) delete rightHandPort;

	leftHandPort = new SerialPortWrapper("COM" + std::to_string(leftHandComPort), 9600, 8);
	rightHandPort = new SerialPortWrapper("COM" + std::to_string(rightHandComPort), 9600, 8);

    double currentTime = getTimeAsDouble();

    // Helper lambda to initialize the map cleanly
    auto init_map = [currentTime](std::map<FeedbackLocation, double>& map) {
        map[FeedbackLocation::Thumb] = currentTime;
        map[FeedbackLocation::Index] = currentTime;
        map[FeedbackLocation::Middle] = currentTime;
        map[FeedbackLocation::Ring] = currentTime;
        map[FeedbackLocation::Pinky] = currentTime;
    };
    
    init_map(fingerToSendTimeRight);
    init_map(fingerToSendTimeLeft);
}

void HapticHandFeedback::Close()
{
	if (leftHandPort)
	{
		leftHandPort->Close();
		delete leftHandPort;
		leftHandPort = nullptr;
	}

	if (rightHandPort)
	{
		rightHandPort->Close();
		delete rightHandPort;
		rightHandPort = nullptr;
	}
}

void HapticHandFeedback::ApplyFeedback(HandFeedbackConfig config)
{
	double currentTime = getTimeAsDouble();

    if (config.Hand == HapticHand::Left || config.Hand == HapticHand::Both)
    {
        if (leftHandPort == nullptr || !leftHandPort->IsOpen())
        {
			return;
        }
        else if (fingerToSendTimeLeft.count(config.Location) && fingerToSendTimeLeft[config.Location] > currentTime)
        {
            return;
        }
        else
        {
            fingerToSendTimeLeft[config.Location] = currentTime + config.Duration - 0.05;
            SendFeedback(*leftHandPort, config);
        }
    }

    if (config.Hand == HapticHand::Right || config.Hand == HapticHand::Both)
    {
        if (rightHandPort == nullptr || !rightHandPort->IsOpen())
        {
			return;
        }
        else if (fingerToSendTimeRight.count(config.Location) && fingerToSendTimeRight[config.Location] > currentTime)
        {
            return;
        }
        else
        {
            fingerToSendTimeRight[config.Location] = currentTime + config.Duration - 0.05;
            SendFeedback(*rightHandPort, config);
        }
    }
}

void HapticHandFeedback::SendFeedback(SerialPortWrapper& port, HandFeedbackConfig config)
{
    float clampedStrength = clamp_value(config.NormalizedStrength, 0.0f, 1.0f);

    uint8_t location = static_cast<uint8_t>(config.Location);
    uint8_t strength = static_cast<uint8_t>(255.0f * clampedStrength);

	std::vector<uint8_t> data;
    data.reserve(8);

    data.push_back(location);
    data.push_back(strength);

    union 
	{
        float f;
        uint8_t bytes[sizeof(float)];
    } duration_bytes;

    duration_bytes.f = config.Duration;

    for (size_t i = 0; i < sizeof(float); ++i) {
        data.push_back(duration_bytes.bytes[i]);
    }

    data.push_back(0);
    data.push_back(0);

    port.Write(data.data(), data.size());
}

extern "C" {

	HAPTIC_API void* HapticGetSingletonInstance()
	{
		return &HapticHandFeedback::GetInstance();
	}

	HAPTIC_API void HapticInitialize(void* instance, uint16_t leftHandComPort, uint16_t rightHandComPort)
	{
		static_cast<HapticHandFeedback*>(instance)->Initialize(leftHandComPort, rightHandComPort);
	}

	HAPTIC_API void HapticClose(void* instance)
	{
		static_cast<HapticHandFeedback*>(instance)->Close();
	}

	HAPTIC_API void HapticApplyFeedback(void* instance, HandFeedbackConfig config)
	{
		static_cast<HapticHandFeedback*>(instance)->ApplyFeedback(config);
	}

} 

