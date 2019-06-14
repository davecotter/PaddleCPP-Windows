#include "stdafx.h"
#include <msclr\auto_gcroot.h>
#include <assert.h>
#using "PaddleWrapper.dll"
#include "PaddleCLR.h"

using namespace System::Runtime::InteropServices; 

class PaddleWrapperPrivate {
	public: 
	msclr::auto_gcroot<PaddleWrapper::CPaddleWrapper^>		paddleRef;
};

PaddleCLR::PaddleCLR(
	int					vendorID, 
	const char			*vendorNameStr,
	const char			*vendorAuthStr,
	const char			*apiKeyStr)
{
	i_wrapperP = new PaddleWrapperPrivate();
	i_wrapperP->paddleRef = gcnew PaddleWrapper::CPaddleWrapper(
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

void	PaddleCLR::ShowEnterSerialButton()
{
	i_wrapperP->paddleRef->ShowEnterSerialButton();
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

// ----------------------------------------------------------------------------
static	std::string		StringConvert_SystemToStd_UTF8(System::String^ str)
{
	array<System::Byte>^	encodedBytes	= System::Text::Encoding::UTF8->GetBytes(str + "\0");
	pin_ptr<System::Byte>	pinnedBytes		= &encodedBytes[0];
	std::string				stdStr			= reinterpret_cast<char*>(pinnedBytes);

	return stdStr;
}

std::string			PaddleCLR::DoCommand(CommandType in_cmdType, const std::string& jsonCmd)
{
	PaddleWrapper::CommandType		cmdType((PaddleWrapper::CommandType)in_cmdType);
	System::String^					returnStr = i_wrapperP->paddleRef->DoCommand(
		cmdType, 
		gcnew System::String(jsonCmd.c_str()));
	
	return StringConvert_SystemToStd_UTF8(returnStr);
}

/*
void PaddleCLR::SetBeginTransactionCallback(CallbackType functionPtr)
{
	PaddleWrapper::CPaddleWrapper::CallbackDelegate^ callback =
		(PaddleWrapper::CPaddleWrapper::CallbackDelegate^) Marshal::GetDelegateForFunctionPointer(
			System::IntPtr(functionPtr), 
			PaddleWrapper::CPaddleWrapper::CallbackDelegate::typeid);

	i_wrapperP->paddleRef->beginTransactionCallback = callback;
}

void PaddleCLR::SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr)
{
	PaddleWrapper::CPaddleWrapper::CallbackTransactionCompleteDelegate^ callback =
		(PaddleWrapper::CPaddleWrapper::CallbackTransactionCompleteDelegate^) Marshal::GetDelegateForFunctionPointer(
			System::IntPtr(functionPtr), 
			PaddleWrapper::CPaddleWrapper::CallbackTransactionCompleteDelegate::typeid);

	i_wrapperP->paddleRef->transactionCompleteCallback = callback;
}

void PaddleCLR::SetTransactionErrorCallback(CallbackWithStringType functionPtr)
{
	PaddleWrapper::CPaddleWrapper::CallbackWithStringDelegate^ callback =
		(PaddleWrapper::CPaddleWrapper::CallbackWithStringDelegate^) Marshal::GetDelegateForFunctionPointer(
			System::IntPtr(functionPtr),
			PaddleWrapper::CPaddleWrapper::CallbackWithStringDelegate::typeid);

	i_wrapperP->paddleRef->transactionErrorCallback = callback;
}

void PaddleCLR::SetProductActivateCallback(CallbackActivateType functionPtr)
{
	PaddleWrapper::CPaddleWrapper::CallbackVerificationDelegate^ callback =
		(PaddleWrapper::CPaddleWrapper::CallbackVerificationDelegate^) Marshal::GetDelegateForFunctionPointer(
			System::IntPtr(functionPtr),
			PaddleWrapper::CPaddleWrapper::CallbackVerificationDelegate::typeid);

	i_wrapperP->paddleRef->activateCallback = callback;
}
*/
