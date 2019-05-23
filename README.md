# Paddle Wrapper



### Setup
(Tested with VS2017)

#### Using pre-built DLLs 

Copy all DLLs in either `Debug` or `Release` folders to your project build folder.

Add `PaddleCLR\PaddleCLR` to "Additional header search paths" (In C/C++ section)

Add `PaddleCLR\Debug` or `PaddleCLR\Release` to "Additional library paths" (In Linker/General section)

Add `PaddleCLR.lib` to "Additional dependencies" (Linker/Input section)

#### Building from scratch

Open the PaddleCLR solution in Visual Studio

Build

[This step to be automated]

Copy all DLLs in the `PaddleCLR\Debug` folder to the example project's `Debug` folder. 

### Example code

Callback functions:

```cpp
void __stdcall beginTransactionCallback()
{
	OutputDebugStringA("beginTransactionCallback\n");
}

void __stdcall transactionCompleteCallback(const char* productID,
                                           const char* userEmail,
					   const char* userCountry,
					   const char* licenseCode,
					   const char* orderID,
					   bool flagged,
					   const char* processStatusJson)
{
	OutputDebugStringA("transactionCompleteCallback:\n");
	OutputDebugStringA(productID);
	OutputDebugStringA("\n");
	OutputDebugStringA(userEmail);
	OutputDebugStringA("\n");
	OutputDebugStringA(userCountry);
	OutputDebugStringA("\n");
	OutputDebugStringA(licenseCode);
	OutputDebugStringA("\n");
	OutputDebugStringA(orderID);
	OutputDebugStringA("\n");
	OutputDebugStringA(flagged ? "flagged == true" : "flagged == false");
	OutputDebugStringA("\n");
	OutputDebugStringA(processStatusJson);
}

void __stdcall transactionErrorCallback(const char* error)
{
	OutputDebugStringA("transactionErrorCallback\n");
	OutputDebugStringA(error);
}
```

Calling code:

```cpp
  auto paddle = PaddleCLR::PaddleCLR(PAD_VENDOR_ID, PAD_PRODUCT, PAD_API_KEY, PAD_PRODUCT_NAME, PAD_VENDOR_NAME);

   paddle.SetBeginTransactionCallback(beginTransactionCallback);
   paddle.SetTransactionCompleteCallback(transactionCompleteCallback);
   paddle.SetTransactionErrorCallback(transactionErrorCallback);
   paddle.ShowCheckoutWindow(PAD_PRODUCT);

```

Where all capitalized words are `#define`s with your Paddle vendor and product details. 

