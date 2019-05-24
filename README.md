# Paddle Wrapper



## Setup
(Tested with VS2017)



### Build instructions

You may need to open the PaddleWrapper project, and make sure that the PaddleSDK has installed correctly from NuGet. 
Make sure this project builds on its own. 

You should then be able to open, build and run PaddleExample. 

### Adding to your project

Either:

#### Add projects to your solution 

Add PaddleWrapper and PaddleCLR to your solution

Add "PaddleCLR.lib" in the Linker/Input section
Add the path of PaddleCLR.lib to "Additional Library Directories" in Linker/General

Or: 

#### Add pre-built DLLs

Copy all DLLs in either `Debug` or `Release` folders to your project build folder.

Add `PaddleCLR\PaddleCLR` to "Additional header search paths" (In C/C++ section)

Add `PaddleCLR\Debug` or `PaddleCLR\Release` to "Additional library paths" (In Linker/General section)

Add `PaddleCLR.lib` to "Additional dependencies" (Linker/Input section)

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

