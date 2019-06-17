# Paddle Wrapper

## Setup
(Tested with VS2017, VS2008)


### Build instructions
Using vs2017 or later:
first try the C# project: "PaddleExampleCS"

just open the sln and run it, it should "just work"
it should automatically download all the proper NuGet packages

open "Form1.cs" and set these variables:
PAD_VENDOR_ID	
PAD_VENDOR_NAME	
PAD_VENDOR_AUTH	
PAD_API_KEY		

PAD_PRODUCT_ID	
PAD_PRODUCT_NAME

then run. then you can test your own products, test purchasing with coupons, test the validation call.

-----------------------
next try the C++ project: "PaddleExample"
open the solution and run it, should "just work" as before
note it sometimes fails to build, but if you do a full "rebuild" that always works. don't know why

now open the file "PaddleExample.cpp"
and set the variables named above

scroll down to "Relevant Example Code starts here"

note we use RapidJSON for packaging up parameters to send to Paddle, and to get data back.

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

