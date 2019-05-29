#include "stdafx.h"

#include <msclr\auto_gcroot.h>
#include <assert.h>
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
	i_wrapperP = new PaddleWrapperPrivate();
	i_wrapperP->paddleRef = gcnew PaddleWrapper::PaddleWrapper(
		vendorID,
		gcnew System::String(vendorNameStr),
		gcnew System::String(vendorAuthStr),
		gcnew System::String(apiKeyStr));
}

PaddleCLR::~PaddleCLR()
{
	delete i_wrapperP;
}

void	PaddleCLR::debug_print(const char *str)
{
	i_wrapperP->paddleRef->debug_print(gcnew System::String(str));
}

void	PaddleCLR::AddProduct(
	PaddleProductID		prodID, 
	const char			*nameStr, 
	const char			*localizedTrialStr)
{
	i_wrapperP->paddleRef->AddProduct(
		prodID,
		gcnew System::String(nameStr),
		gcnew System::String(localizedTrialStr));
}

void	PaddleCLR::CreateInstance(PaddleProductID productID)
{
	i_wrapperP->paddleRef->CreateInstance(productID);
}

void PaddleCLR::ShowCheckoutWindow(PaddleProductID productId)
{
	i_wrapperP->paddleRef->ShowPaddleWindow(productId, PaddleWindowType::Checkout);
}

void PaddleCLR::ShowProductAccessWindow(PaddleProductID productId)
{
	i_wrapperP->paddleRef->ShowPaddleWindow(productId, PaddleWindowType::ProductAccess);
}

void PaddleCLR::ShowLicenseActivationWindow(PaddleProductID productId)
{
	i_wrapperP->paddleRef->ShowPaddleWindow(productId, PaddleWindowType::LicenseActivation);
}

void PaddleCLR::SetBeginTransactionCallback(CallbackType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackDelegate::typeid);

	i_wrapperP->paddleRef->beginTransactionCallback = callback;
}

void PaddleCLR::SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr), 
		PaddleWrapper::PaddleWrapper::CallbackTransactionCompleteDelegate::typeid);

	i_wrapperP->paddleRef->transactionCompleteCallback = callback;
}

void PaddleCLR::SetTransactionErrorCallback(CallbackWithStringType functionPtr)
{
	auto callback = (PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate^) Marshal::GetDelegateForFunctionPointer(
		System::IntPtr(functionPtr),
		PaddleWrapper::PaddleWrapper::CallbackWithStringDelegate::typeid);

	i_wrapperP->paddleRef->transactionErrorCallback = callback;
}

void PaddleCLR::SetProductActivateCallback(CallbackActivateType functionPtr)
{
    auto callback = (PaddleWrapper::PaddleWrapper::CallbackActivateDelegate^) Marshal::GetDelegateForFunctionPointer(
        System::IntPtr(functionPtr),
        PaddleWrapper::PaddleWrapper::CallbackActivateDelegate::typeid);

	i_wrapperP->paddleRef->activateCallback = callback;
}
