# Paddle Wrapper

## Setup
(Tested with VS2017, VS2008)


### Build instructions
Using vs2017 or later:<br>
first try the C# project: "PaddleExampleCS"

just open the sln and run it, it should "just work"<br>
it should automatically download all the proper NuGet packages

open "Form1.cs" and set these variables:<br>
PAD_VENDOR_ID<br>
PAD_VENDOR_NAME<br>
PAD_VENDOR_AUTH<br>
PAD_API_KEY<br>
<br>
PAD_PRODUCT_ID<br>
PAD_PRODUCT_NAME<br>

then run. then you can test your own products, test purchasing with coupons, test the validation call.

-----------------------
next try the C++ project: "PaddleExample"<br>
open the solution and run it, should "just work" as before<br>
note it sometimes fails to build, but if you do a full "rebuild" that always works. don't know why

now open the file "PaddleExample.cpp"<br>
and set the variables named above

scroll down to "Relevant Example Code starts here"

note we use RapidJSON for packaging up parameters to send to Paddle, and to get data back.

### Adding to your project

Either:

#### Add projects to your solution 

Add PaddleWrapper and PaddleCLR to your solution

Add "PaddleCLR.lib" in the Linker/Input section<br>
Add the path of PaddleCLR.lib to "Additional Library Directories" in Linker/General

Or: 

#### Add pre-built DLLs

Copy all DLLs in either `Debug` or `Release` folders to your project build folder.

Add `PaddleCLR\PaddleCLR` to "Additional header search paths" (In C/C++ section)

Add `PaddleCLR\Debug` or `PaddleCLR\Release` to "Additional library paths" (In Linker/General section)

Add `PaddleCLR.lib` to "Additional dependencies" (Linker/Input section)

you'll also have to copy these assemblies next to your exe:<br>
PaddleSDK StructureMap Newtonsoft.Json Interop.SHDocVw CredentialManagement System.Threading

see PaddleExample/post_build.bat for an example post build script

## How it works

This wrapper only implements the UI for the Checkout (purchase) process. It assumes you do NOT want to show the "Product Access" dialog, and do NOT want to use Paddle UI for manual activation (entering email address and serial number). Also by default it hides the "Enter Serial Number" button from the Checkout window (this is optional, however).

All c-strings are assumed to be UTF8, do NOT use "multibyte" or any other code-page encoding.

Open /PaddleExample/PaddleExample.cpp, and scroll down to "Relevant example code starts here"

The first thing you do is create a new PaddleCLR class (CLR means "common language runtime"). You can create it with NEW if you want, or include the class in your own accessor class, in this example it's just a local variable on the stack because we never leave this scope, but that's probably not how you'll do it.

After you create the PaddleCLR class, you add products to it. It is probably most common that you have only one product, so just add that. But if you have one main product and multiple in-app purchase products, you will add them all at this time.

Note that the "Thanks for trying ..." string should be LOCALIZED already. This example does not show that.

Now at this point, you MAY choose that you WANT to have the "Enter Serial Number" button showing in the Checkout window. If you do, add the line:

paddle.ShowEnterSerialButton();

All that does is save that info for later, and when the Checkout window is shown, the button will NOT be hidden.

Next, you will create an instance of the Paddle manager, telling it which product will be the "main" product. Generally it doesn't matter which product you pass in here, but it will be the one listed in parentheses on your customer's receipts, under the name of the actual product that they buy.  So for example if my main product is "SuperCat", and that's the one i created the instance with, but the sub-product or in-app purchase i'm selling is "Laser Pointer", then in the receipt the buyer will see "Laser Pointer (SuperCat)".

All parameters passed into and out of Paddle are encoded in JSON strings. This example uses RapidJSON for that, though you're obviously free to use whatever JSON library you prefer.
