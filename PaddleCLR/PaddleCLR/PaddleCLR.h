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

typedef void(__stdcall *CallbackWithStringType)(const char*);
typedef void(__stdcall *CallbackType)(void);
typedef void(__stdcall *CallbackTransactionCompleteType)(const char*, const char*, const char*, const char*, const char*, bool, const char*);

class PaddleWrapperPrivate;

/** event contents:
// TransactionCompleteEvent
public string ProductID{ get; set; }
public string UserEmail{ get; set; }
public string UserCountry{ get; set; }
public string LicenseCode{ get; set; }
public string OrderID{ get; set; }
public bool Flagged{ get; set; }
public ProcessStatus ProcessStatus{ get; set; }

// ProcessStatus
[JsonProperty("state")]
public string State{ get; set; }
[JsonProperty("checkout")]
public CheckOut Checkout{ get; set; }
[JsonProperty("order")]
public Order Order{ get; set; }
[JsonProperty("lockers")]
public List<Locker> Lockers{ get; set; }
public string ProductID{ get; }
public string CustomerEmail{ get; }
public string LicenseCode{ get; }
public string LockerID{ get; }
public string OrderID{ get; }
*/


class DllExport PaddleCLR
{
public:
	PaddleCLR(const char* vendorId, const char* productId, const char* apiKey, const char* productName, const char* vendorName);
	~PaddleCLR();
	void ShowCheckoutWindow(const char* productId);
	void SetBeginTransactionCallback(CallbackType functionPtr);
	void SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr);
	void SetTransactionErrorCallback(CallbackWithStringType functionPtr);
	
private: 
	PaddleWrapperPrivate* wrapper;

};