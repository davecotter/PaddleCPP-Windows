#include "stdafx.h"

#include <msclr\auto_gcroot.h>
#using "PaddleWrapper.dll"
#include "PaddleCLR.h"

using namespace System::Runtime::InteropServices; 

class PaddleWrapperPrivate
{
public: msclr::auto_gcroot<PaddleWrapper::PaddleWrapper^> paddle;
};

PaddleCLR::PaddleCLR(const char* vendorId, const char* productId, const char* apiKey, const char* productName, const char* vendorName)
{
	wrapper = new PaddleWrapperPrivate();
	wrapper->paddle = gcnew PaddleWrapper::PaddleWrapper(
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

void PaddleCLR::ShowCheckoutWindow(const char* productId)
{
	wrapper->paddle->ShowPaddleWindow(gcnew System::String(productId), PaddleWindowType::Checkout);
}

void PaddleCLR::ShowProductAccessWindow(const char* productId)
{
	wrapper->paddle->ShowPaddleWindow(gcnew System::String(productId), PaddleWindowType::ProductAccess);
}

void PaddleCLR::ShowLicenseActivationWindow(const char* productId)
{
	wrapper->paddle->ShowPaddleWindow(gcnew System::String(productId), PaddleWindowType::LicenseActivation);
}

void PaddleCLR::SetBeginTransactionCallback(CallbackType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackDelegate::typeid);

	wrapper->paddle->beginTransactionCallback = callback;
}

void PaddleCLR::SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate::typeid);

	wrapper->paddle->transactionCompleteCallback = callback;
}

void PaddleCLR::SetTransactionErrorCallback(CallbackWithStringType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr),
		PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate::typeid);

	wrapper->paddle->transactionErrorCallback = callback;
}

void PaddleCLR::SetProductActivateCallback(CallbackActivateType functionPtr)
{
    auto callback = (PaddleWrapper::PaddleWrapper::CallbackActivateDelegate^) Marshal::GetDelegateForFunctionPointer(
        System::IntPtr(functionPtr),
        PaddleWrapper::PaddleWrapper::CallbackActivateDelegate::typeid);

    wrapper->paddle->activateCallback = callback;
}
