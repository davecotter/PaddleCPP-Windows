#include "stdafx.h"

#include <msclr\auto_gcroot.h>

#include "PaddleCLR.h"

using namespace System::Runtime::InteropServices; // Marshal

class PaddleWrapperPrivate
{
public: msclr::auto_gcroot<PaddleWrapper::PaddleWrapper^> paddleAPI;
};

PaddleCLR::PaddleCLR()
{
	wrapper = new PaddleWrapperPrivate();
	wrapper->paddleAPI = gcnew PaddleWrapper::PaddleWrapper();
}

PaddleCLR::~PaddleCLR()
{
	delete wrapper;
}

void PaddleCLR::ShowCheckoutWindow(const char* productId)
{
	return wrapper->paddleAPI->ShowCheckoutWindow(gcnew System::String(productId));
}