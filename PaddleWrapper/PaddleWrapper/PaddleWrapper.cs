#define	kUseEventLoop
#define	kUseThreads

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Forms;
using Newtonsoft.Json;

using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;
using PaddleSDK.Licensing;
using Newtonsoft.Json.Linq;

#if kUseThreads
using System.Threading;
#endif

namespace PaddleWrapper {
	using PaddleProductID = System.Int32;

    public enum PaddleWindowType
    {
		ProductAccess,		//	not used
		Checkout, 
		LicenseActivation	//	not used
	};

	// ------------------------------------------------------------------------
	//	a bit better than PaddleSDK.Product.VerificationState
	//	this version defines "Invalid" and "NoActivation"
	public enum VerificationState {
    
		// The verification state variable is simply not initialized (no status).
		Invalid = -1,

		// The license did not pass verification.
		Unverified,
		
		// The license did pass verification.
		Verified,

		// We were unable to get a definitive verification result, 
		//	typically because of poor network.
		UnableToVerify,

		// There is no license to verify. Check product.activated 
		//	before verifying the product.
		NoActivation
	};
	
	//	doesn't exist in PaddleSDK v 2.0.10
	public enum ActivationState {

		// * The activation state variable is simply not initialized (no status).
		Invalid = -1,
		
		// * The product was activated as part of the license activation process.
		Activated,
		
		// * The product was deactivated as part of the license deactivation process.
		Deactivated,
		
		// * The product (de)activation process was abandoned.
		Abandoned,
		
		// * The product (de)activation process has failed, possibly due to network 
		//	connectivity issues OR an invalid license code.
		Failed
	};
	
	//	doesn't exist in PaddleSDK v 2.0.10
	public enum CheckoutState {
		// * The CheckoutState variable is simply not initialized (no status).
		Invalid = -1,
		
		//	The checkout was successful and the product was purchased.
		Purchased,
		
		//	The user cancelled the checkout before the product was purchased.
		Abandoned,

		//	The checkout failed to load or the order processing took too long to complete.
		Failed,

		//	The checkout has been completed and the payment has been taken, but we were unable
		//	to retrieve the status of the order. It will be processed soon, but not soon enough for us to
		//	show the buyer the license activation dialog.
		SlowOrderProcessing,

		//	The checkout was completed, but the transaction was flagged for manual processing.
		//	The Paddle team will handle the transaction manually. If the order is approved,
		//	the buyer will be able to activate the product later, when the approved order has been processed.
		Flagged
	};
	
	// ------------------------------------------------------------------------
	
	public enum CommandType {
		Command_NONE,

		// must match PaddleCLR::CommandType
		Command_VALIDATE,
		Command_ACTIVATE,
		Command_PURCHASE,
		Command_DEACTIVATE,
		Command_RECOVER,

		Command_NUMTYPES
	};
	
	public class CPaddleWrapper
	{
		private int     i_vendorID;
		private string	i_vendorNameStr;
		private string	i_vendorAuthStr;
		private string	i_apiKeyStr;
		
		private const int PADErrorLicenseActivationFailed	= -100;
		private const int PADErrorUnableToRecoverLicense	= -114;
		
        // Local storage for Paddle window config
		#if kUseThreads
		
		class CThreadWindowData {
			public PaddleProduct			i_currentProduct;
			public bool						i_openInBrowser;
			public bool						i_isDialog;

			//	what window?
			public PaddleWindowType			i_currentWindowType;

			//	only one of these 3 will be initted based on above var
			public CheckoutOptions			i_checkoutOptions;
			public ProductWindowConfig		i_productWindowConfig;
			public LicenseWindowConfig		i_licenseWindowConfig;
		};
		
		CThreadWindowData				i_threadData = new CThreadWindowData();

		private Thread                  i_window_threadRef;
		private SynchronizationContext  i_sync_context;
		private ManualResetEvent        i_threadInitEvent;

		public delegate void ShowCheckoutWindowDelegate(PaddleProduct product, CheckoutOptions options, bool openInBrowser, bool isDialog);
		public delegate void ShowLicenseWindowDelegate(PaddleProduct product, LicenseWindowConfig config, bool isDialog);
		public delegate bool ShowProductWindowDelegate(PaddleProduct product, ProductWindowConfig config, bool isDialog);
		#endif

		// Delegates used for native C++ callback functions
		public delegate void CallbackDelegate();
		public delegate void CallbackWithStringDelegate(string s);
		public delegate void CallbackTransactionCompleteDelegate(
			string productIDStr, 
			string userEmailStr, 
			string userCountryStr, 
			string licenseCodeStr,
			string orderID,
			bool   flaggedB,
			string processStatusStr);

		public delegate void CallbackVerificationDelegate(int verificationState, string verificationString);

		public CallbackDelegate	                    beginTransactionCallback;
		public CallbackTransactionCompleteDelegate  transactionCompleteCallback;
		public CallbackWithStringDelegate           transactionErrorCallback;
		public CallbackVerificationDelegate         activateCallback;
		public CallbackVerificationDelegate         validateCallback;

		// ----------------------------------------------------------------------
		private ActivationState		ConvertState_VerifyToActivate(
			PaddleSDK.Product.VerificationState verifyState)
		{
			ActivationState		state = ActivationState.Invalid;
			
			switch (verifyState) {

				case PaddleSDK.Product.VerificationState.Verified: {
					state = ActivationState.Activated;
				} break;

				case PaddleSDK.Product.VerificationState.Unverified:
				case PaddleSDK.Product.VerificationState.UnableToVerify: {
					state = ActivationState.Failed;
				} break;
			}
			
			return state;
		}

		// -------------------------------------------------------------
		class CJsonResult {
			public bool		successB			= false;

			//	resultI is usually a "state", or a bool value, not necessarily an error code
			public int		resultI				= 0;	

			// error code may not be always set even when there's an error
			public int		errCodeI			= 0;
			
			// errStr indicates whether there's an error, MUST be set if error
			public string	errStr				= "";
			
			//	only valid for purchResponse
			public string	responseJson		= "";
		};
		
		private string			CreateJsonResult(CJsonResult jsonClass)
		{
			JObject		errArrayDict = new JObject { };	//	empty dict
			JObject		jsonResult = new JObject {
				{ kPaddleCmdKey_RETURNVAL,	jsonClass.resultI },
				{ kPaddleCmdKey_SUCCESS,	jsonClass.successB }
			};

			if (!string.IsNullOrEmpty(jsonClass.errStr)) {
				JObject		jsonErr = new JObject {
					{ "NSDomain",				"com.paddle.paddle" },
					{ "NSCode",					jsonClass.errCodeI },
					{ "NSLocalizedDescription", jsonClass.errStr }
				};

				JArray		errArray = new JArray { jsonErr };
				
				errArrayDict.Add(kPaddleCmdKey_ERRORS_ARRAY, errArray);
			}
			
			jsonResult.Add(kPaddleCmdKey_ERRORS_ARRAY, errArrayDict);
			
			if (!string.IsNullOrEmpty(jsonClass.responseJson)) {
	            JObject		responseDict = JObject.Parse(jsonClass.responseJson);

				jsonResult.Add(kPaddleCmdKey_PURCH_RESPONSE, responseDict);
			}
			
			return jsonResult.ToString();
		}

		// ----------------------------------------------------------------------
		private const string	kLicenseWindowAutomationID		= "CheckoutForm";
		private const string	kLicenseButtonAutomationID		= "btnLicense";
		private delegate void	SafeCallDelegate_ButtonHide(Control buttonRef);
		private delegate void	SafeCallDelegate_WindowClosed();
		private int[]			i_checkoutWindID;

		private void	RegisterEventListeners()
		{
			Automation.AddAutomationEventHandler(
				WindowPattern.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Children,
				(sender, e) => 
			{
				AutomationElement		element = sender as AutomationElement;
				string					automationID = element.Current.AutomationId;

				if (automationID != kLicenseWindowAutomationID) return;

				i_checkoutWindID = element.GetRuntimeId();

				if (!i_showEnterSerialNumberB) {
					AutomationElement licenseButton = element.FindFirst(
						TreeScope.Descendants,
						new PropertyCondition(AutomationElement.AutomationIdProperty, kLicenseButtonAutomationID));

					if (licenseButton != null) {
						IntPtr		hwnd = new IntPtr(licenseButton.Current.NativeWindowHandle);
						Control		buttonRef = Control.FromHandle(hwnd);

						HideButton_Safe(buttonRef);
					}
				}
			});

			Automation.AddAutomationEventHandler(
				WindowPattern.WindowClosedEvent,
				AutomationElement.RootElement,
				TreeScope.Subtree,
				(sender, e) => 
			{
				WindowClosedEventArgs		args = e as WindowClosedEventArgs;
				int[]						closingWindID = args.GetRuntimeId();
				
				if (i_checkoutWindID != null && closingWindID != null) {
					
					if (Automation.Compare(closingWindID, i_checkoutWindID)) {
						Array.Clear(i_checkoutWindID, 0, i_checkoutWindID.Length);
						Paddle_CheckoutWindowClosed();
					}
				}
			});
		}

		private void HideButton_Safe(Control buttonRef)
		{
			if (buttonRef.InvokeRequired) {
				var d = new SafeCallDelegate_ButtonHide(HideButton_Safe);
				buttonRef.Invoke(d, new object[] { buttonRef });
			} else {
				buttonRef.Hide();
			}
		}

		// -----------------------------------------------------------

        // Copied from CPaddleInterface 
		public const string kPaddleCmdKey_SKU              = "SKU";
		public const string kPaddleCmdKey_SKUSTR           = "SKUstr";   //	product title
		public const string kPaddleCmdKey_CMD              = "cmd";
		public const string kPaddleCmdKey_CMDSTR           = "cmdStr";

		public const string kPaddleCmdKey_EMAIL            = "email";
		public const string kPaddleCmdKey_SERIAL_NUMBER    = "serial number";
		public const string kPaddleCmdKey_COUNTRY          = "country";
		public const string kPaddleCmdKey_POSTCODE         = "postcode";
		public const string kPaddleCmdKey_COUPON           = "coupon";
		public const string kPaddleCmdKey_OLD_SN           = "old_sn";
		public const string kPaddleCmdKey_TITLE            = "title";     //	product title
		public const string kPaddleCmdKey_MESSAGE          = "message"; //	product description

		public const string kPaddleCmdKey_RETURNVAL        = "return val";
		public const string kPaddleCmdKey_RESULTS          = "results";
		public const string kPaddleCmdKey_SUCCESS          = "success";
		public const string kPaddleCmdKey_PURCH_RESPONSE   = "purchase response";
		public const string kPaddleCmdKey_RESPONSE         = "response";
		public const string kPaddleCmdKey_CANCEL           = "cancel";
		public const string kPaddleCmdKey_ERRORS_ARRAY     = "errors array";   //	array of CFError dicts
		public const string kPaddleCmdKey_TESTING          = "testing";

        public static void		debug_print(string str)
		{
			Console.WriteLine(str);
			Debug.WriteLine(str);
		}

		public class PaddleProductRec
		{
			public string nameStr;
			public string localizedTrialStr;

			//	for compatability with STL containers
			public PaddleProductRec() { }

			public PaddleProductRec(string in_nameStr, string in_localizedTrialStr)
			{
				this.nameStr = in_nameStr;
				this.localizedTrialStr = in_localizedTrialStr;
			}
		}

		private bool	i_showEnterSerialNumberB = false;

		public void		ShowEnterSerialButton()
		{
			i_showEnterSerialNumberB = true;
		}

		//	sneaky way to do a typedef in C#
		private class PaddleProductMap: SortedDictionary<PaddleProductID, PaddleProductRec> { }

		PaddleProductMap		i_prodMap = new PaddleProductMap();

		public void AddProduct(PaddleProductID prodID, string nameStr, string localizedTrialStr)
		{
			i_prodMap[prodID] = new PaddleProductRec(nameStr, localizedTrialStr);
		}

		private PaddleProductRec		GetProductRec(PaddleProductID prodID)
		{
			if	(i_prodMap.ContainsKey(prodID)) {
				return i_prodMap[prodID];
			}
			
			debug_print(String.Format("ERROR: GetProductRec: no product: {0}", prodID));

			return new PaddleProductRec();
		}

		private PaddleProductConfig	Paddle_GetConfig(PaddleProductID prodID)
		{
			PaddleProductRec		prodRec		= GetProductRec(prodID);

			PaddleProductConfig		config		= new PaddleProductConfig {
				ProductName		= prodRec.nameStr, 
				VendorName		= i_vendorNameStr,
				TrialType		= PaddleSDK.Product.TrialType.None,
				TrialText		= prodRec.localizedTrialStr,
				Currency		= "USD",
				ImagePath		= ""
			};

			return config;
		}

		private PaddleProduct		Paddle_GetProduct(PaddleProductID productID)
		{
			PaddleProduct			product	= PaddleProduct.CreateProduct(
				productID.ToString(),
				PaddleSDK.Product.ProductType.SDKProduct,
				Paddle_GetConfig(productID));
			
			product.CanForceExit = false;
			return product;
		}

		// ----------------------------------------------------------------------------
		public CPaddleWrapper(
			int                 vendorID,
			string              vendorNameStr,
			string              vendorAuthStr,
			string              apiKeyStr)
		{
			i_vendorID      = vendorID;
			i_vendorNameStr = vendorNameStr;
			i_vendorAuthStr = vendorAuthStr;
			i_apiKeyStr     = apiKeyStr;
        }

		public void		CreateInstance(PaddleProductID productID)
		{
			string		vendorStr	= i_vendorID.ToString();
			string		productStr	= productID.ToString();

			// Initialize the SDK singleton with the config
			Paddle.Configure(
				i_apiKeyStr, 
				vendorStr, 
				productStr, 
				Paddle_GetConfig(productID));

			Paddle.Instance.TransactionBeginEvent		+= Paddle_CheckoutBeginEvent;
			Paddle.Instance.TransactionCompleteEvent	+= Paddle_CheckoutCompleteEvent;
			Paddle.Instance.TransactionErrorEvent		+= Paddle_CheckoutErrorEvent;

			Paddle.Instance.LicensingStarting			+= Paddle_LicensingBeginEvent;
			Paddle.Instance.LicensingCompleteEvent		+= Paddle_LicensingCompleteEvent;
			Paddle.Instance.LicensingErrorEvent			+= Paddle_LicensingErrorEvent;

			Paddle.Instance.RecoveryCompleteEvent		+= Paddle_RecoveryCompleteEvent;
			Paddle.Instance.RecoveryErrorEvent			+= Paddle_RecoveryErrorEvent;

			RegisterEventListeners();
		}

		// -------------------------------------------------------------
		private static ScTask	s_taskP;

		private class ScTask {
			private string			i_resultStr;

			#if !kUseEventLoop
		        private TaskCompletionSource<string>	i_taskCompletion = new TaskCompletionSource<string>();
			#endif

			public ScTask() {
				CPaddleWrapper.s_taskP = this;
			}

			static public ScTask	get() {
				return CPaddleWrapper.s_taskP;
			}

			public void		set_result(string resultStr)
			{
				CPaddleWrapper.s_taskP = null;

				#if kUseEventLoop
					i_resultStr = resultStr;
				#else
		            i_taskCompletion.TrySetResult(resultStr);
				#endif
			}

			public string	await_result() 
			{
				#if kUseEventLoop
					while (string.IsNullOrEmpty(i_resultStr)) {
						Application.DoEvents();
					}
				#else
					i_resultStr = i_taskCompletion.Task.Result;
				#endif

				return i_resultStr;
			}
		};

		// -------------------------------------------------------------
		//	validate means verify
		private string					Validate(string jsonCmd)
		{
			string				jsonResult;
            JObject				cmdObject	= JObject.Parse(jsonCmd);
            PaddleProductID		prodID		= cmdObject.Value<PaddleProductID>(kPaddleCmdKey_SKU);
            PaddleProduct		product		= Paddle_GetProduct(prodID);

			if (!product.Activated) {
				VerificationState		state;
				
				if (product.TrialDaysRemaining > 0) {
					state = VerificationState.Verified;
				} else {
					state = VerificationState.NoActivation;
				}
				
				CJsonResult		jResult = new CJsonResult {
					successB	= state == VerificationState.Verified,
					resultI		= Convert.ToInt32(state) };

				jsonResult = CreateJsonResult(jResult);
			} else {
				DateTime		lastSuccessDateT = product.LastSuccessfulVerifiedDate;
				TimeSpan		spanSinceSuccessT = DateTime.Now - lastSuccessDateT;
				double			hoursSinceSuccessT = spanSinceSuccessT.TotalHours;
				
				// Verify the activation only if it's been a while.
				if (hoursSinceSuccessT < 1) {
					CJsonResult		jResult = new CJsonResult {
						successB	= true,
						resultI		= Convert.ToInt32(VerificationState.Verified) };

					// No need to verify. The product is activated. All's well.
					jsonResult = CreateJsonResult(jResult);
				} else {
					ScTask		task = new ScTask();

					product.VerifyActivation(
						(PaddleSDK.Product.VerificationState in_state, string resultStr) =>
					{
						VerificationState		state = (VerificationState)in_state;
						bool					destroyB = false;
						
						switch (state) {
						
							case VerificationState.Unverified: {
								// The activation is no longer valid. Destroy it, let the user know and continue with
								// the trial.
								destroyB = true;
							} break;
							
							case VerificationState.UnableToVerify: {
								// Verify that the last successful verify date is valid.
								// And then implement a cooldown strategy. 

								// Ensure that the last successful verified date appears valid.
								// As `compare:` "detects sub-second differences" the dates should not be the same.
								// Equally we can't have verified the activation in the future.
								//	future dates have a negative time span since now:
								if (hoursSinceSuccessT < 0) {
									// The last successfully verified date does not seem valid. If the time difference
									// is less than 24 hours, a timezone change is possible. Other than that, tampering
									// seems likely.
									//
									// In doubt, destroy the activation and ask the user to reactivate.
									destroyB = true;
								}

								// Implement a cooldown period: if the user has not gone online within the period,
								// then destroy the activation and ask them to go online to re-activate.
								double			daysSinceSuccessT = spanSinceSuccessT.TotalDays;

								if (daysSinceSuccessT >= 30) {
									destroyB = true;
								} else {
									// The grace period continues, so the user can continue to use the core functionality.
									state = VerificationState.Verified;
								}
							} break;
						}

						if (destroyB) {
							product.DestroyActivation();
							state = VerificationState.Unverified;
						}
						
						CJsonResult		jResult = new CJsonResult {
							successB	= state == VerificationState.Verified,
							resultI		= Convert.ToInt32(state),
							errStr		= resultStr };

						task.set_result(CreateJsonResult(jResult));
					});

					jsonResult = task.await_result();
				}
			}

			return jsonResult;
        }

		//-------------------------------------------------------------------
		private string					Activate(string jsonCmd)
		{
			string				jsonResult	= "";
			JObject				cmdObject	= JObject.Parse(jsonCmd);
			PaddleProductID		prodID		= cmdObject.Value<PaddleProductID>(kPaddleCmdKey_SKU);
			string				emailStr	= cmdObject.Value<string>(kPaddleCmdKey_EMAIL);
			string				snStr		= cmdObject.Value<string>(kPaddleCmdKey_SERIAL_NUMBER);
			PaddleProduct		product		= Paddle_GetProduct(prodID);
			ScTask				task		= new ScTask();

			product.ActivateWithEmail(emailStr, snStr, 
				(PaddleSDK.Product.VerificationState verifyState, string resultStr) =>
			{
				ActivationState		state = ConvertState_VerifyToActivate(verifyState);
				CJsonResult			jResult = new CJsonResult {
					successB	= state == ActivationState.Activated,
					resultI		= Convert.ToInt32(state),
					errStr		= resultStr };

				task.set_result(CreateJsonResult(jResult));
			});

			jsonResult = task.await_result();
			return jsonResult;
		}

		//-------------------------------------------------------------------

		private string					Purchase(string jsonCmd)
		{
			string							jsonResult	= "";
            JObject							cmdObject	= JObject.Parse(jsonCmd);
            PaddleProductID					prodID		= cmdObject.Value<PaddleProductID>(kPaddleCmdKey_SKU);
          
			string	emailStr     = cmdObject.Value<string>(kPaddleCmdKey_EMAIL);
			string	couponStr    = cmdObject.Value<string>(kPaddleCmdKey_COUPON);
			string	countryStr   = cmdObject.Value<string>(kPaddleCmdKey_COUNTRY);
			string	postStr      = cmdObject.Value<string>(kPaddleCmdKey_POSTCODE);
			string	titleStr     = cmdObject.Value<string>(kPaddleCmdKey_TITLE);
			string	messageStr   = cmdObject.Value<string>(kPaddleCmdKey_MESSAGE);

            CheckoutOptions checkoutOptions = new CheckoutOptions {
                Email		= emailStr,
                Coupon		= couponStr,
                Country		= countryStr,
                PostCode	= postStr
            };

			//	custom param keys are documented here: 
			//	https://paddle.com/docs/api-custom-checkout/
			checkoutOptions.AddCheckoutParameters("quantity_variable",	"0");
			checkoutOptions.AddCheckoutParameters("title",				titleStr);
			checkoutOptions.AddCheckoutParameters("custom_message",		messageStr);

			//	documented here: https://paddle.com/docs/checkout-options-windows-sdk/
            jsonResult = ShowCheckoutWindowAsync(prodID, checkoutOptions, false, true);

            return jsonResult;
        }

		private string					Deactivate(string jsonCmd)
		{
			string		jsonResult = "";

			return jsonResult;
		}

		private string					RecoverLicense(string jsonCmd)
		{
			string		jsonResult = "";

			return jsonResult;
		}

		// -------------------------------------------------------------
		
		public string					DoCommand(CommandType cmdType, string jsonCmd)
		{
			string		jsonResult = "";
						
			switch (cmdType) {
			
				case CommandType.Command_VALIDATE: {
					jsonResult = Validate(jsonCmd);
				} break;

				case CommandType.Command_ACTIVATE: {
					jsonResult = Activate(jsonCmd);
				} break;

				case CommandType.Command_PURCHASE: {
					jsonResult = Purchase(jsonCmd);
				} break;

				case CommandType.Command_DEACTIVATE: {
					jsonResult = Deactivate(jsonCmd);
				} break;

				case CommandType.Command_RECOVER: {
					jsonResult = RecoverLicense(jsonCmd);
				} break;
			}

			return jsonResult;
		}

		// -------------------------------------------------------------
        public	string		ShowCheckoutWindowAsync(
			PaddleProductID		productID, 
			CheckoutOptions		options,
			bool				openInBrowser,
			bool				isDialog)
		{
			ScTask			task	= new ScTask();
			PaddleProduct	product	= Paddle_GetProduct(productID);

			product.Refresh((success) => {
				#if kUseThreads
					//	do on another thread
					i_threadData.i_currentWindowType	= (PaddleWindowType)PaddleWindowType.Checkout;
					i_threadData.i_currentProduct		= product;
					i_threadData.i_checkoutOptions		= options;
					i_threadData.i_openInBrowser		= openInBrowser;
					i_threadData.i_isDialog				= isDialog;
					StartWindowThread();
				#else
					// do it on this thread
					Paddle.Instance.ShowCheckoutWindowForProduct(product, options, openInBrowser, isDialog);
				#endif
			});
			
			return task.await_result();
        }

		#if kUseThreads
		// Set up a suitable thread for the checkout window. Technique taken from here:
		// https://stackoverflow.com/questions/21680738/how-to-post-messages-to-an-sta-thread-running-a-message-pump/21684059#21684059
		private void InitializeWindowThread(object sender, EventArgs e)
		{
			i_sync_context = SynchronizationContext.Current;
			
			i_threadInitEvent.Set();
			
			Application.Idle -= InitializeWindowThread;
			
			if (i_sync_context == null) {
				throw new ObjectDisposedException("STAThread");
			}
			
			switch (i_threadData.i_currentWindowType) {

				case PaddleWindowType.Checkout:
					ShowCheckoutWindowDelegate		showCheckoutWindowDelegate = Paddle.Instance.ShowCheckoutWindowForProduct;
					
                    i_sync_context.Send((_) => showCheckoutWindowDelegate.Invoke(
						i_threadData.i_currentProduct, 
						i_threadData.i_checkoutOptions, 
						i_threadData.i_openInBrowser, 
						i_threadData.i_isDialog), null);
                    break;
				
				case PaddleWindowType.LicenseActivation:
					ShowLicenseWindowDelegate       showLicenseWindowDelegate = Paddle.Instance.ShowLicenseActivationWindowForProduct;
                    
					i_threadData.i_licenseWindowConfig = new LicenseWindowConfig();

                    i_sync_context.Send((_) => showLicenseWindowDelegate.Invoke(
                    	i_threadData.i_currentProduct, 
                    	i_threadData.i_licenseWindowConfig, 
                    	i_threadData.i_isDialog), null);
					break;

				case PaddleWindowType.ProductAccess:
					ShowProductWindowDelegate       showProductWindowDelegate = Paddle.Instance.ShowProductAccessWindowForProduct;

					i_threadData.i_productWindowConfig = new ProductWindowConfig();

                    i_sync_context.Send((_) => showProductWindowDelegate.Invoke(
                    	i_threadData.i_currentProduct, 
                    	i_threadData.i_productWindowConfig, 
                    	i_threadData.i_isDialog), null);
					break;
			}
		}

		private void StartWindowThread()
		{
			using (i_threadInitEvent = new ManualResetEvent(false))
			{
				i_window_threadRef = new Thread(() => {
					Application.Idle += InitializeWindowThread;
					Application.Run();
				});
				i_window_threadRef.SetApartmentState(ApartmentState.STA);
				i_window_threadRef.Start();
				i_threadInitEvent.WaitOne();
			}
		}

		#endif

		//-------------------------------------------------------------------
		private void Paddle_CheckoutBeginEvent(object sender, TransactionBeginEventArgs e)
		{
			debug_print(e.ToString());
		}

		private void Paddle_CheckoutErrorEvent(object sender, TransactionErrorEventArgs e)
		{
			debug_print(e.ToString());

			CJsonResult			jResult = new CJsonResult {
				resultI		= Convert.ToInt32(CheckoutState.Failed),
				// ?? errCodeI	= PADErrorLicenseActivationFailed,
				errStr		= e.Error };

			ScTask.get().set_result(CreateJsonResult(jResult));
		}

		private void Paddle_CheckoutWindowClosed()
		{
			//	task may be NULL at this point 
			//	if the checkout completed or errored
			ScTask		task = ScTask.get();
			
			if (task != null) {
				CJsonResult		jResult = new CJsonResult {
					resultI		= Convert.ToInt32(CheckoutState.Abandoned) };
					
				task.set_result(CreateJsonResult(jResult));
			}
		}

		private void Paddle_CheckoutCompleteEvent(object sender, TransactionCompleteEventArgs e)
		{
			string		purchResponseJsonStr = JsonConvert.SerializeObject(e.ProcessStatus, Formatting.Indented);

			debug_print(e.ToString());
			
			CJsonResult			jResult = new CJsonResult {
				successB		= true,
				resultI			= Convert.ToInt32(CheckoutState.Purchased),
				responseJson	= purchResponseJsonStr };

			ScTask.get().set_result(CreateJsonResult(jResult));
		}

		// ----------------------------------------------------------------------------------
		private void Paddle_LicensingBeginEvent(object sender, LicensingStartingEventArgs e)
		{
			debug_print(e.ToString());
			
			e.AutoActivate = true;
			e.ShowActivationWindow = false;
		}

		private void Paddle_LicensingErrorEvent(object sender, LicensingErrorEventArgs e)
		{
			debug_print(e.ToString());

			CJsonResult			jResult = new CJsonResult {
				resultI			= Convert.ToInt32(ActivationState.Failed),
				errCodeI		= PADErrorLicenseActivationFailed,
				errStr			= "Licensing failed" };

			ScTask.get().set_result(CreateJsonResult(jResult));
		}	

		private void Paddle_LicensingCompleteEvent(object sender, LicensingCompleteEventArgs e)
		{
			debug_print(e.ToString());

			CJsonResult			jResult = new CJsonResult {
				successB		= true,
				resultI			= Convert.ToInt32(ActivationState.Activated) };

			ScTask.get().set_result(CreateJsonResult(jResult));
		}

		// ----------------------------------------------------------------------------------
		private void Paddle_RecoveryErrorEvent(object sender, LicensingRecoveryErrorEventArgs e)
		{
			debug_print(e.ToString());
			
			// PADErrorUnableToRecoverLicense = -114,
			
			CJsonResult			jResult = new CJsonResult {
				successB		= false,
				resultI			= 0,
				errCodeI		= PADErrorUnableToRecoverLicense,
				errStr			= e.Message };

			ScTask.get().set_result(CreateJsonResult(jResult));
		}

		private void Paddle_RecoveryCompleteEvent(object sender, LicensingRecoveryCompleteEventArgs e)
		{
			debug_print(e.ToString());
			debug_print(e.Message);

			CJsonResult			jResult = new CJsonResult {
				successB		= true,
				resultI			= 1 };

			ScTask.get().set_result(CreateJsonResult(jResult));
		}
	}
}
