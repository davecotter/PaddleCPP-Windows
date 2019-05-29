#include "stdafx.h"

#include <msclr\auto_gcroot.h>
#using "PaddleWrapper.dll"
#include "PaddleCLR.h"

using namespace System::Runtime::InteropServices; 

class PaddleWrapperPrivate {
	public: 
	msclr::auto_gcroot<PaddleWrapper::PaddleWrapper^>		paddleRef;
};

PaddleCLR::PaddleCLR(
	int					vendorID, 
	const char			*vendorNameStr,
	const char			*vendorAuthStr,
	const char			*apiKeyStr)
{
	wrapperP			= new PaddleWrapperPrivate();
	wrapperP->paddleRef = gcnew PaddleWrapper::PaddleWrapper(
		vendorID,
		gcnew System::String(vendorNameStr),
		gcnew System::String(vendorAuthStr),
		gcnew System::String(apiKeyStr));
}

PaddleCLR::~PaddleCLR()
{
	delete wrapperP;
}

void	PaddleCLR::AddProduct(
	PaddleProductID		prodID, 
	const char			*nameStr, 
	const char			*localizedTrialStr)
{
	wrapperP->paddleRef->AddProduct(
		prodID,
		gcnew System::String(nameStr),
		gcnew System::String(localizedTrialStr));
}

void	PaddleCLR::CreateInstance(PaddleProductID productID)
{
	wrapperP->paddleRef->CreateInstance(productID);
}

void PaddleCLR::ShowCheckoutWindow(PaddleProductID productId)
{
	wrapperP->paddleRef->ShowPaddleWindow(productId, PaddleWindowType::Checkout);
}

void PaddleCLR::ShowProductAccessWindow(PaddleProductID productId)
{
	wrapperP->paddleRef->ShowPaddleWindow(productId, PaddleWindowType::ProductAccess);
}

void PaddleCLR::ShowLicenseActivationWindow(PaddleProductID productId)
{
	wrapperP->paddleRef->ShowPaddleWindow(productId, PaddleWindowType::LicenseActivation);
}

void PaddleCLR::SetBeginTransactionCallback(CallbackType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackDelegate::typeid);

	wrapperP->paddleRef->beginTransactionCallback = callback;
}

void PaddleCLR::SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate::typeid);

	wrapperP->paddleRef->transactionCompleteCallback = callback;
}

void PaddleCLR::SetTransactionErrorCallback(CallbackWithStringType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr),
		PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate::typeid);

	wrapperP->paddleRef->transactionErrorCallback = callback;
}

void PaddleCLR::SetProductActivateCallback(CallbackActivateType functionPtr)
{
    auto callback = (PaddleWrapper::PaddleWrapper::CallbackActivateDelegate^) Marshal::GetDelegateForFunctionPointer(
        System::IntPtr(functionPtr),
        PaddleWrapper::PaddleWrapper::CallbackActivateDelegate::typeid);

	wrapperP->paddleRef->activateCallback = callback;
}
