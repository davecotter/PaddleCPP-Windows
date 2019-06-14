#pragma once

#include <string>

#define DllExport   __declspec(dllexport)

/*
typedef void(__stdcall *CallbackWithStringType)(const char*);
typedef void(__stdcall *CallbackType)(void);
typedef void(__stdcall *CallbackTransactionCompleteType)(const char*, const char*, const char*, const char*, const char*, bool, const char*);
typedef void(__stdcall *CallbackActivateType)(int, const char*);
	void SetBeginTransactionCallback(CallbackType functionPtr);
	void SetTransactionCompleteCallback(CallbackTransactionCompleteType functionPtr);
	void SetTransactionErrorCallback(CallbackWithStringType functionPtr);
    void SetProductActivateCallback(CallbackActivateType functionPtr);	
*/
class PaddleWrapperPrivate;

class DllExport PaddleCLR {
	PaddleWrapperPrivate		*i_wrapperP;

	public:
	typedef int		PaddleProductID;

	typedef enum {
		Command_NONE,

		//	must match PaddleWrapper.CommandType
		//	must match CPaddleCommand.CommandType
		Command_VALIDATE,
		Command_ACTIVATE,
		Command_PURCHASE,
		Command_DEACTIVATE,
		Command_RECOVER,

		Command_NUMTYPES
	} CommandType;

	PaddleCLR(
		int					vendorID,
		const char			*vendorNameStr,
		const char			*vendorAuthStr,
		const char			*apiKeyStr);

	~PaddleCLR();
	
	void			ShowEnterSerialButton();

	void			AddProduct(PaddleProductID prodID, const char *nameStr, const char *localizedTrialStr);
	void			CreateInstance(PaddleProductID productID);

	void			debug_print(const char *str);

	public:
	std::string		DoCommand(CommandType cmdType, const std::string& jsonCmd);
};
