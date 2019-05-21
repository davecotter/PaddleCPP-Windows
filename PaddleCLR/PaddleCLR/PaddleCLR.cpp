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

void PaddleCLR::ShowCheckoutWindow(const char* productId)
{
	wrapper->paddleAPI->ShowCheckoutWindow(gcnew System::String(productId));
}

void PaddleCLR::SetBeginTransactionCallback(CallbackType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackDelegate::typeid);

	wrapper->paddleAPI->beginTransactionCallback = callback;
}

void PaddleCLR::SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate::typeid);

	wrapper->paddleAPI->transactionCompleteCallback = callback;
}

void PaddleCLR::SetTransactionErrorCallback(CallbackWithStringType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr),
		PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate::typeid);

	wrapper->paddleAPI->transactionErrorCallback = callback;
}

//const char* PaddleCLR::ShowCheckoutWindow(const char* productId)
//{
//	System::String^ managedString = wrapper->paddleAPI->ShowCheckoutWindow(gcnew System::String(productId));
//	return (const char*)Marshal::StringToHGlobalAnsi(managedString).ToPointer();
//}