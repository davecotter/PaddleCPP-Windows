#pragma once

#define DllExport   __declspec(dllexport)


//namespace PaddleCLR 
//{
//	ref class Wrapper
//	{
//	public:
//		// TODO: Add your methods for this class here.
//		Wrapper(const char* vendorId, const char* productId, const char* apiKey, const char* productName, const char* vendorName);
//		void setup();
//		void ShowCheckoutWindow(const char* productId);
//
//	private:
//		PaddleWrapper::PaddleWrapper^ paddleWrapper;
//	};
//}

/*
DllExport void ShowMessageBox(int *value)
{
	ManagedDll::DoWork work;
	work.ShowCSharpMessageBox(value);
}
*/

//// The following should go in the calling code...?
typedef void(__stdcall *CallbackWithStringType)(const char*);
typedef void(__stdcall *CallbackType)(void);

class PaddleWrapperPrivate;



class DllExport PaddleCLR
{
public:
	PaddleCLR(const char* vendorId, const char* productId, const char* apiKey, const char* productName, const char* vendorName);
	~PaddleCLR();
	void ShowCheckoutWindow(const char* productId);
	void SetBeginTransactionCallback(CallbackType functionPtr);
	
private: 
	PaddleWrapperPrivate* wrapper;
	static CallbackType callback;

};