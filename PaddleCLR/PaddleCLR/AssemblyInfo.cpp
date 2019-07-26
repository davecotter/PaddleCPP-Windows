#include "stdafx.h"

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Security::Permissions;

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly:AssemblyTitleAttribute("PaddleCLR")];
[assembly:AssemblyDescriptionAttribute("Common Language Runtime Bridge for PaddleWrapper")];
[assembly:AssemblyConfigurationAttribute("")];
[assembly:AssemblyCompanyAttribute("Metamuse Media")];
[assembly:AssemblyProductAttribute("PaddleCLR")];
[assembly:AssemblyCopyrightAttribute("(c) 2019 Metamuse Media")];
[assembly:AssemblyTrademarkAttribute("")];
[assembly:AssemblyCultureAttribute("")];

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the value or you can default the Revision and Build Numbers
// by using the '*' as shown below:
// The default build number increments daily. 
// The default revision number is the number of seconds since midnight local time 
//	(without taking into account time zone adjustments for daylight saving time), 
//	divided by 2

[assembly:AssemblyVersionAttribute("1.0.*")];

[assembly:ComVisible(false)];

[assembly:CLSCompliantAttribute(true)];

[assembly:SecurityPermission(SecurityAction::RequestMinimum, UnmanagedCode = true)];
