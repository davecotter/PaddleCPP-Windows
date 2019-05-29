#pragma once

#define DllExport   __declspec(dllexport)

typedef void(__stdcall *CallbackWithStringType)(const char*);
typedef void(__stdcall *CallbackType)(void);
typedef void(__stdcall *CallbackTransactionCompleteType)(const char*, const char*, const char*, const char*, const char*, bool, const char*);
typedef void(__stdcall *CallbackActivateType)(int, const char*);

class PaddleWrapperPrivate;

class DllExport PaddleCLR
{
	PaddleWrapperPrivate	*wrapperP;

public:
	typedef int		PaddleProductID;

	enum PaddleWindowType {
		ProductAccess,
		Checkout,
		LicenseActivation
	};

	PaddleCLR(
		int					vendorID,
		const char			*vendorNameStr,
		const char			*vendorAuthStr,
		const char			*apiKeyStr);

	~PaddleCLR();

	void	AddProduct(PaddleProductID prodID, const char *nameStr, const char *localizedTrialStr);
	void	CreateInstance(PaddleProductID productID);

	void ShowCheckoutWindow(PaddleProductID productId);
	void ShowProductAccessWindow(PaddleProductID productId);
	void ShowLicenseActivationWindow(PaddleProductID productId);

	void SetBeginTransactionCallback(CallbackType functionPtr);
	void SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr);
	void SetTransactionErrorCallback(CallbackWithStringType functionPtr);
    void SetProductActivateCallback(CallbackActivateType functionPtr);	
};
