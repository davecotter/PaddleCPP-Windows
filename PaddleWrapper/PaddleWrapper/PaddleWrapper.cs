using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;

using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;
using PaddleSDK.Licensing;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace PaddleWrapper
{
	using PaddleProductID = System.Int32;




    public enum PaddleWindowType
    {
		ProductAccess,
		Checkout, 
		LicenseActivation
	};

	public class PaddleWrapper
	{
		private int     i_vendorID;
		private string	i_vendorNameStr;
		private string	i_vendorAuthStr;
		private string	i_apiKeyStr;

		private PaddleProduct       i_currentProduct;
		private PaddleWindowType    i_currentWindowType;
		private CheckoutOptions     i_checkoutOptions;

        private TaskCompletionSource<string> currentTaskCompletionSource;

		private Thread                  thread;
		private SynchronizationContext  ctx;
		private ManualResetEvent        threadInitEvent;
        private AutoResetEvent          callbackEvent;

		public delegate void ShowCheckoutWindowDelegate(PaddleProduct product, CheckoutOptions options, bool openInBrowser, bool isDialog);

		ShowCheckoutWindowDelegate       showProductWindowDelegate;

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


        // Copied from CPaddleInterface 
        private const string kPaddleCmdKey_SKU              = "SKU";
        private const string kPaddleCmdKey_SKUSTR           = "SKUstr";   //	product title
        private const string kPaddleCmdKey_CMD              = "cmd";
        private const string kPaddleCmdKey_CMDSTR           = "cmdStr";

        private const string kPaddleCmdKey_EMAIL            = "email";
        private const string kPaddleCmdKey_SERIAL_NUMBER    = "serial number";
        private const string kPaddleCmdKey_COUNTRY          = "country";
        private const string kPaddleCmdKey_POSTCODE         = "postcode";
        private const string kPaddleCmdKey_COUPON           = "coupon";
        private const string kPaddleCmdKey_OLD_SN           = "old_sn";
        private const string kPaddleCmdKey_TITLE            = "title";     //	product title
        private const string kPaddleCmdKey_MESSAGE          = "message"; //	product description

        private const string kPaddleCmdKey_RETURNVAL        = "return val";
        private const string kPaddleCmdKey_RESULTS          = "results";
        private const string kPaddleCmdKey_SUCCESS          = "success";
        private const string kPaddleCmdKey_PURCH_RESPONSE   = "purchase response";
        private const string kPaddleCmdKey_RESPONSE         = "response";
        private const string kPaddleCmdKey_CANCEL           = "cancel";
        private const string kPaddleCmdKey_ERRORS_ARRAY     = "errors array";   //	array of CFError dicts
        private const string kPaddleCmdKey_TESTING          = "testing";


        public void		debug_print(string str)
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

		//	sneaky way to do a typedef in C#
		public class PaddleProductMap: SortedDictionary<PaddleProductID, PaddleProductRec> { }

		PaddleProductMap		i_prodMap = new PaddleProductMap();

		public void AddProduct(PaddleProductID prodID, string nameStr, string localizedTrialStr)
		{
			i_prodMap[prodID] = new PaddleProductRec(nameStr, localizedTrialStr);
		}

		public PaddleProductRec		GetProduct(PaddleProductID prodID)
		{
			if	(i_prodMap.ContainsKey(prodID)) {
				return i_prodMap[prodID];
			}
			
			debug_print(String.Format("no product: {0}", prodID));

			return new PaddleProductRec();
		}

		PaddleProductConfig			Paddle_GetConfig(PaddleProductID prodID)
		{
			PaddleProductRec		prodRec		= GetProduct(prodID);

			debug_print(String.Format(
				"about to get config: {0}", prodRec.nameStr));

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

		PaddleProduct				Paddle_GetProduct(PaddleProductID productID)
		{
			PaddleProduct			product	= PaddleProduct.CreateProduct(
				productID.ToString(),
				PaddleSDK.Product.ProductType.SDKProduct,
				Paddle_GetConfig(productID));
			
			product.CanForceExit = false;
			return product;
		}

		public PaddleWrapper(
			int                 vendorID,
			string              vendorNameStr,
			string              vendorAuthStr,
			string              apiKeyStr)
		{
			i_vendorID      = vendorID;
			i_vendorNameStr = vendorNameStr;
			i_vendorAuthStr = vendorAuthStr;
			i_apiKeyStr     = apiKeyStr;

            callbackEvent = new AutoResetEvent(false);

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

			Paddle.Instance.TransactionBeginEvent	 += Paddle_TransactionBeginEvent;
			Paddle.Instance.TransactionCompleteEvent += Paddle_TransactionCompleteEvent;
			Paddle.Instance.TransactionErrorEvent	 += Paddle_TransactionErrorEvent;
			Paddle.Instance.LicensingCompleteEvent	 += Paddle_LicensingCompleteEvent;

			//	delegate?
			//	canForceExit = false is done in product? why ?
		}

		public string					Validate(string jsonCmd)
		{
            string jsonResult = "";
            JObject cmdObject = JObject.Parse(jsonCmd);
            PaddleProductID prodID = (PaddleProductID)cmdObject.GetValue(kPaddleCmdKey_SKU);

            PaddleProduct product = Paddle_GetProduct(prodID);

            jsonResult = ValidateAsync(product).Result;

            return jsonResult;
		}

        private Task<string> ValidateAsync(PaddleProduct product)
        {
            var t = new TaskCompletionSource<string>();

            product.VerifyActivation((VerificationState state, string s) =>
            {
                // Not sure what JSON key to give this, if any
                t.TrySetResult(state.ToString());
            });

            return t.Task;
        }

		public string					Activate(string jsonCmd)
		{
			string		jsonResult = "";

			return jsonResult;
		}

		public string					Purchase(string jsonCmd)
		{
			string jsonResult = "";

            JObject cmdObject = JObject.Parse(jsonCmd);

            PaddleProductID prodID = (PaddleProductID) cmdObject.GetValue(kPaddleCmdKey_SKU);
            string emailStr     = cmdObject.GetValue(kPaddleCmdKey_EMAIL).ToString();
            string couponStr    = cmdObject.GetValue(kPaddleCmdKey_COUPON).ToString();
            string countryStr   = cmdObject.GetValue(kPaddleCmdKey_COUNTRY).ToString();
            string postStr      = cmdObject.GetValue(kPaddleCmdKey_POSTCODE).ToString();
            // The following do not seem to be available in the Windows SDK:
            string titleStr     = cmdObject.GetValue(kPaddleCmdKey_TITLE).ToString();
            string messageStr   = cmdObject.GetValue(kPaddleCmdKey_MESSAGE).ToString();

            CheckoutOptions checkoutOptions = new CheckoutOptions();

            checkoutOptions.Email = emailStr;
            checkoutOptions.Coupon = couponStr;
            checkoutOptions.Country = countryStr;
            checkoutOptions.PostCode = postStr;

            jsonResult = ShowCheckoutWindowAsync(product, checkoutOptions).Result;

            return jsonResult;
        }

		public string					Deactivate(string jsonCmd)
		{
			string		jsonResult = "";

			return jsonResult;
		}

		public string					RecoverLicense(string jsonCmd)
		{
			string		jsonResult = "";

			return jsonResult;
		}

        public void ShowCheckoutWindowAsync(PaddleProductID productID, CheckoutOptions options = null)
        {
            i_checkoutOptions = options;
            currentTaskCompletionSource = new TaskCompletionSource<string>();
            ShowPaddleWindow(productID, (int) PaddleWindowType.Checkout);
        }

        // TODO Make this private and wrap other windows as above
		public void ShowPaddleWindow(PaddleProductID productID, int windowType)
		{
			// Initialize the Product you'd like to work with
			var product = Paddle_GetProduct(productID);

			// Ask the Product to get it's latest state and info from the Paddle Platform
			product.Refresh((success) =>
			{
				i_currentProduct = product;
				i_currentWindowType = (PaddleWindowType) windowType;

				// Product data was successfully refreshed
				if (success)
				{
					if (!product.Activated)
					{
						// Product is not activated, so let's show the Product Access dialog to gatekeep your app
						StartWindowThread();
					}
				}
				else
				{
					// The SDK was unable to get the last info from the Paddle Platform.
					// We can show the Product Access dialog with the data provided in the PaddleProductConfig object.
					StartWindowThread();
				}
			});

		}



        private static void ShowCheckoutWindow(PaddleProduct product, CheckoutOptions options = null, bool showInBrowser = false, bool isDialog = true)
		{
			Paddle.Instance.ShowCheckoutWindowForProduct(product, options, showInBrowser, isDialog);
		}

		private static void ShowProductAccessWindow(PaddleProduct product)
		{
			Paddle.Instance.ShowProductAccessWindowForProduct(product);
		}

		private static void ShowLicenseActivationWindow(PaddleProduct product)
		{
			Paddle.Instance.ShowLicenseActivationWindowForProduct(product);
		}

		//-------------------------------------------------------------------

		// Set up a suitable thread for the checkout window
		// Technique taken from https://stackoverflow.com/questions/21680738/how-to-post-messages-to-an-sta-thread-running-a-message-pump/21684059#21684059
		private void Initialize(object sender, EventArgs e)
		{
			ctx = SynchronizationContext.Current;
			threadInitEvent.Set();
			Application.Idle -= Initialize;
			if (ctx == null) throw new ObjectDisposedException("STAThread");
			
			switch(i_currentWindowType) {

				case PaddleWindowType.Checkout:
					showProductWindowDelegate = ShowCheckoutWindow;
					break;
				
				case PaddleWindowType.LicenseActivation:
					showProductWindowDelegate = ShowLicenseActivationWindow;
					break;

				default:
				case PaddleWindowType.ProductAccess:
					showProductWindowDelegate = ShowProductAccessWindow;
					break;

			}
			ctx.Send((_) => showProductWindowDelegate.Invoke(i_currentProduct), null);
		}

		private void StartWindowThread()
		{
			using (threadInitEvent = new ManualResetEvent(false))
			{
				thread = new Thread(() => {
					Application.Idle += Initialize;
					Application.Run();
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				threadInitEvent.WaitOne();
			}
            callbackEvent.WaitOne();
		}

		//-------------------------------------------------------------------

		public void Validate()
		{
		}

		public void Activate(string productId, string email, string license)
		{
			// Initialize the Product you'd like to work with
			PaddleProduct product = Paddle_GetProduct(Int32.Parse(productId));

			product.ActivateWithEmail(email, license, (VerificationState state, string s) =>
			{
				activateCallback?.Invoke(Convert.ToInt32(state), s);
			});
		}


		private void Paddle_TransactionBeginEvent(object sender, TransactionBeginEventArgs e)
		{
			beginTransactionCallback?.Invoke();

			Debug.WriteLine("Paddle_TransactionBeginEvent");
			Debug.WriteLine(e.ToString());
		}

		private void Paddle_TransactionCompleteEvent(object sender, TransactionCompleteEventArgs e)
		{

			string processStatusJson = JsonConvert.SerializeObject(e.ProcessStatus, Formatting.Indented);

			transactionCompleteCallback?.Invoke(
				e.ProductID,
				e.UserEmail,
				e.UserCountry,
				e.LicenseCode,
				e.OrderID,
				e.Flagged,
				processStatusJson);

			Debug.WriteLine("Paddle_TransactionCompleteEvent");
			Debug.WriteLine(e.ToString());

            currentTaskCompletionSource.TrySetResult(processStatusJson);

            callbackEvent.Set(); // TODO Remove 
		}

		private void Paddle_TransactionErrorEvent(object sender, TransactionErrorEventArgs e)
		{
			transactionErrorCallback?.Invoke(e.Error);

			Debug.WriteLine("Paddle_TransactionErrorEvent");
			Debug.WriteLine(e.ToString());

            callbackEvent.Set();
		}

		private void Paddle_LicensingCompleteEvent(object sender, LicensingCompleteEventArgs e)
		{
			   // TODO
		}
	}
}
