// PaddleExample.cpp : Defines the entry point for the application.
//

#include "stdafx.h"
#include <assert.h>
#include "PaddleExample.h"
#include "PaddleCLR.h"
#include <exception>
#include <iostream>
#include <string>

#include "rapidjson/document.h"
#include "rapidjson/stringbuffer.h"
#include "rapidjson/prettywriter.h"

#define		kTest_Validate		1
#define		kTest_Purchase		1

#if OPT_2008
	#define	HAS_INCLUDE		0
#else
	#define	HAS_INCLUDE		__has_include("PaddleCredentials.h")        // Requires VS2015 Update 2 and above
#endif

#define MAX_LOADSTRING 100

#if	HAS_INCLUDE
	#include	"PaddleCredentials.h"
#else
	/**
	 * Default Paddle credentials
	 *
	 * Copy and paste into "PaddleCredentials.h" and add your own details 
	 * to use your Paddle account
	 */
	#define    PAD_VENDOR_ID               11745
	#define    PAD_VENDOR_NAME             "The Catnip Co."
	#define    PAD_VENDOR_AUTH             "this is not a real vendor auth string"
	#define    PAD_API_KEY                 "4134242689d26430f89ec0858884ab07"

	#define    PAD_PRODUCT_ID              511013
	#define    PAD_PRODUCT_NAME            "Optimum Cats"
#endif	//	HAS_INCLUDE

//---------------------------------------------------------------------------



// Global Variables:
HINSTANCE hInst;                                // current instance
WCHAR szTitle[MAX_LOADSTRING];                  // The title bar text
WCHAR szWindowClass[MAX_LOADSTRING];            // the main window class name

// Forward declarations of functions included in this code module:
ATOM                MyRegisterClass(HINSTANCE hInstance);
BOOL                InitInstance(HINSTANCE, int);
LRESULT CALLBACK    WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK    About(HWND, UINT, WPARAM, LPARAM);

int WINAPI wWinMain(
	__in HINSTANCE hInstance,
	__in_opt HINSTANCE hPrevInstance,
	__in LPWSTR    lpCmdLine,
	__in int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

    // TODO: Place code here.

    // Initialize global strings
    LoadStringW(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
    LoadStringW(hInstance, IDC_PADDLEEXAMPLE, szWindowClass, MAX_LOADSTRING);
    MyRegisterClass(hInstance);

    // Perform application initialization:
    if (!InitInstance (hInstance, nCmdShow))
    {
        return FALSE;
    }

    HACCEL hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_PADDLEEXAMPLE));

    MSG msg;

    // Main message loop:
    while (GetMessage(&msg, NULL, 0, 0))
    {
        if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    return (int) msg.wParam;
}



//
//  FUNCTION: MyRegisterClass()
//
//  PURPOSE: Registers the window class.
//
ATOM MyRegisterClass(HINSTANCE hInstance)
{
    WNDCLASSEXW wcex;

    wcex.cbSize = sizeof(WNDCLASSEX);

    wcex.style          = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc    = WndProc;
    wcex.cbClsExtra     = 0;
    wcex.cbWndExtra     = 0;
    wcex.hInstance      = hInstance;
    wcex.hIcon          = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_PADDLEEXAMPLE));
    wcex.hCursor        = LoadCursor(NULL, IDC_ARROW);
    wcex.hbrBackground  = (HBRUSH)(COLOR_WINDOW+1);
    wcex.lpszMenuName   = MAKEINTRESOURCEW(IDC_PADDLEEXAMPLE);
    wcex.lpszClassName  = szWindowClass;
    wcex.hIconSm        = LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_SMALL));

    return RegisterClassExW(&wcex);
}

//---------------------------------------------------------------------------

static std::string		JSON_ConvertToString(rapidjson::Document& doc)
{
	rapidjson::StringBuffer								strbuf;
	rapidjson::PrettyWriter<rapidjson::StringBuffer>	writer(strbuf);

	doc.Accept(writer);
	return reinterpret_cast<const char *>(strbuf.GetString());
}

//
//   FUNCTION: InitInstance(HINSTANCE, int)
//
//   PURPOSE: Saves instance handle and creates main window
//
//   COMMENTS:
//
//        In this function, we save the instance handle in a global variable and
//        create and display the main program window.
//
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
	hInst = hInstance; // Store instance handle in our global variable

	HWND	hWnd = CreateWindowW(
		szWindowClass, szTitle, WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, 
		NULL, NULL, hInstance, NULL);

	if (!hWnd) {
		return FALSE;
	}

	ShowWindow(hWnd, nCmdShow);
	UpdateWindow(hWnd);

	// Relevant example code starts here: 
	PaddleCLR	paddle(
		PAD_VENDOR_ID, 
		PAD_VENDOR_NAME,
		PAD_VENDOR_AUTH,
		PAD_API_KEY);

	paddle.AddProduct(
		PAD_PRODUCT_ID,
		PAD_PRODUCT_NAME,
		"Thanks for trying " PAD_PRODUCT_NAME);

	//	if you want, but you probably don't?
	// paddle.ShowEnterSerialButton();

	paddle.CreateInstance(PAD_PRODUCT_ID);

	//	Validate: 
	if (kTest_Validate) {
		rapidjson::Document						cmd; cmd.SetObject();
		rapidjson::Document::AllocatorType&		allocator = cmd.GetAllocator();

		cmd.AddMember(kPaddleCmdKey_SKU, PAD_PRODUCT_ID, allocator);
		
		std::string		resultStr = paddle.DoCommand(
			PaddleCLR::Command_VALIDATE, 
			JSON_ConvertToString(cmd));

		OutputDebugStringA("validate response: ");
		OutputDebugStringA(resultStr.c_str());
	}

	//	Activate:
	//	kPaddleCmdKey_SKU
	//	kPaddleCmdKey_EMAIL
	//	kPaddleCmdKey_SERIAL_NUMBER

	//	Purchase:
	if (kTest_Purchase) {
		rapidjson::Document						cmd; cmd.SetObject();
		rapidjson::Document::AllocatorType&		allocator = cmd.GetAllocator();

		cmd.AddMember(kPaddleCmdKey_SKU,		PAD_PRODUCT_ID,		allocator);
		cmd.AddMember(kPaddleCmdKey_EMAIL,		"test@email.com",	allocator);
		cmd.AddMember(kPaddleCmdKey_COUPON,		"fake-coupon",		allocator);
		cmd.AddMember(kPaddleCmdKey_COUNTRY,	"US",				allocator);
		cmd.AddMember(kPaddleCmdKey_POSTCODE,	"94602",			allocator);

		//	un comment these to override what's shown
		// cmd.AddMember(kPaddleCmdKey_TITLE,		"Catnip",			allocator);
		// cmd.AddMember(kPaddleCmdKey_MESSAGE,	"For fluffy cats",	allocator);

		std::string		resultStr = paddle.DoCommand(
			PaddleCLR::Command_PURCHASE,
			JSON_ConvertToString(cmd));

		OutputDebugStringA("Purchase response:");
		OutputDebugStringA(resultStr.c_str());
	}

	//	Deactivate:
	//	kPaddleCmdKey_SKU

	//	RecoverLicense:
	//	kPaddleCmdKey_SKU
	//	kPaddleCmdKey_EMAIL

	OutputDebugStringA("Checkout complete\n");
	return TRUE;
}

//
//  FUNCTION: WndProc(HWND, UINT, WPARAM, LPARAM)
//
//  PURPOSE: Processes messages for the main window.
//
//  WM_COMMAND  - process the application menu
//  WM_PAINT    - Paint the main window
//  WM_DESTROY  - post a quit message and return
//
//
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
    case WM_COMMAND:
        {
            int wmId = LOWORD(wParam);
            // Parse the menu selections:
            switch (wmId)
            {
            case IDM_ABOUT:
				{
					DialogBox(hInst, MAKEINTRESOURCE(IDD_ABOUTBOX), hWnd, About);


				}
                break;
            case IDM_EXIT:
                DestroyWindow(hWnd);
                break;
            default:
                return DefWindowProc(hWnd, message, wParam, lParam);
            }
        }
        break;
    case WM_PAINT:
        {
            PAINTSTRUCT ps;
            HDC hdc = BeginPaint(hWnd, &ps);
            // TODO: Add any drawing code that uses hdc here...
            EndPaint(hWnd, &ps);
        }
        break;
    case WM_DESTROY:
        PostQuitMessage(0);
        break;
    default:
        return DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}

// Message handler for about box.
INT_PTR CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
    UNREFERENCED_PARAMETER(lParam);
    switch (message)
    {
    case WM_INITDIALOG:
        return (INT_PTR)TRUE;

    case WM_COMMAND:
        if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL)
        {
            EndDialog(hDlg, LOWORD(wParam));
            return (INT_PTR)TRUE;
        }
        break;
    }
    return (INT_PTR)FALSE;
}
