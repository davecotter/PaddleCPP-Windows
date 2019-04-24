#include "stdafx.h"

#include <msclr\auto_gcroot.h>

#include "PaddleCLR.h"

using namespace System::Runtime::InteropServices; 

class PaddleWrapperPrivate
{
public: msclr::auto_gcroot<PaddleWrapper::PaddleWrapper^> paddleAPI;
};

PaddleCLR::PaddleCLR(const char* vendorId, const char* productId, const char* apiKey, const char* productName, const char* vendorName)
{
	wrapper = new PaddleWrapperPrivate();
	wrapper->paddleAPI = gcnew PaddleWrapper::PaddleWrapper(
		gcnew System::String(vendorId), 
		gcnew System::String(productId), 
		gcnew System::String(apiKey), 
		gcnew System::String(productName), 
		gcnew System::String(vendorName));
}

PaddleCLR::~PaddleCLR()
{
	delete wrapper;
}

const char* PaddleCLR::ShowCheckoutWindow(const char* productId)
{
	System::String^ managedString = wrapper->paddleAPI->ShowCheckoutWindow(gcnew System::String(productId));
	return (const char*)Marshal::StringToHGlobalAnsi(managedString).ToPointer();
}