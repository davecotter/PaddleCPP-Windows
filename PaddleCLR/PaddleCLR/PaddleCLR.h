#pragma once

#include <string>

#define DllExport   __declspec(dllexport)

class PaddleWrapperPrivate;

class DllExport PaddleCLR {
	PaddleWrapperPrivate		*i_wrapperP;

	public:
	typedef int		PaddleProductID;

	#define		kPaddleCmdKey_SKU				"SKU"		//	an SKU is a "product ID"
	#define		kPaddleCmdKey_EMAIL				"email"
	#define		kPaddleCmdKey_SERIAL_NUMBER		"serial number"
	#define		kPaddleCmdKey_COUPON			"coupon"
	#define		kPaddleCmdKey_COUNTRY			"country"
	#define		kPaddleCmdKey_POSTCODE			"postcode"
	#define		kPaddleCmdKey_TITLE				"title"		//	product title override
	#define		kPaddleCmdKey_MESSAGE			"message"	//	product description override

	typedef enum {
		Command_NONE,

		//	must match PaddleWrapper.CommandType
		//	must match CPaddleCommand.CommandType
		
		// VALIDATE requires these params:
		//	kPaddleCmdKey_SKU
		Command_VALIDATE,
		
		// ACTIVATE requires these params:
		//	kPaddleCmdKey_SKU
		//	kPaddleCmdKey_EMAIL
		//	kPaddleCmdKey_SERIAL_NUMBER
		Command_ACTIVATE,
		
		// PURCHASE requires these params
		//	kPaddleCmdKey_SKU
		//	kPaddleCmdKey_EMAIL
		//	kPaddleCmdKey_COUPON
		//	kPaddleCmdKey_COUNTRY
		//	kPaddleCmdKey_POSTCODE
		
		// These are optional:
		//	kPaddleCmdKey_TITLE
		//	kPaddleCmdKey_MESSAGE
		Command_PURCHASE,
		
		// DEACTIVATE requires these params
		//	kPaddleCmdKey_SKU
		Command_DEACTIVATE,
		
		// RECOVER requires these params
		//	kPaddleCmdKey_SKU
		//	kPaddleCmdKey_EMAIL
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
