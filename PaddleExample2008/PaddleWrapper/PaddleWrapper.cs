using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;

using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;
using PaddleSDK.Licensing;

namespace PaddleWrapper
{
	using PaddleProductID = System.Int32;

	public enum PaddleWindowType {
		ProductAccess,
		Checkout, 
		LicenseActivation
	};

	public class PaddleWrapper
	{
		private int		i_vendorID;
		private string	i_vendorNameStr;
		private string	i_vendorAuthStr;
		private string	i_apiKeyStr;

		private PaddleProduct		i_currentProduct;
		private PaddleWindowType	i_currentWindowType;

		private Thread					thread;
		private SynchronizationContext	ctx;
		private ManualResetEvent		mre;

		public delegate void ShowProductWindowDelegate(PaddleProduct product);

		ShowProductWindowDelegate		showProductWindowDelegate;

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

		public delegate void CallbackActivateDelegate(int verificationState, string verificationString);

		public CallbackDelegate						beginTransactionCallback;
		public CallbackTransactionCompleteDelegate	transactionCompleteCallback;
		public CallbackWithStringDelegate			transactionErrorCallback;
		public CallbackActivateDelegate				activateCallback;

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
			int					vendorID,
			string				vendorNameStr,
			string				vendorAuthStr,
			string				apiKeyStr)
		{
			i_vendorID		= vendorID;
			i_vendorNameStr	= vendorNameStr;
			i_vendorAuthStr	= vendorAuthStr;
			i_apiKeyStr		= apiKeyStr;
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
			string		jsonResult = jsonCmd;	//	to test round trip

			return jsonResult;
		}

		public string					Activate(string jsonCmd)
		{
			string		jsonResult = "";

			return jsonResult;
		}

		public string					Purchase(string jsonCmd)
		{
			string		jsonResult = "";

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

		private static void ShowCheckoutWindow(PaddleProduct product)
		{
			Paddle.Instance.ShowCheckoutWindowForProduct(product, null, false, true);
		}

		private static void ShowProductAccessWindow(PaddleProduct product)
		{
			Paddle.Instance.ShowProductAccessWindowForProduct(product, null, true);
		}

		private static void ShowLicenseActivationWindow(PaddleProduct product)
		{
			Paddle.Instance.ShowLicenseActivationWindowForProduct(product, null, true);
		}

		//-------------------------------------------------------------------

		// Set up a suitable thread for the checkout window
		// Technique taken from https://stackoverflow.com/questions/21680738/how-to-post-messages-to-an-sta-thread-running-a-message-pump/21684059#21684059
		private void Initialize(object sender, EventArgs e)
		{
			ctx = SynchronizationContext.Current;
			mre.Set();
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
			using (mre = new ManualResetEvent(false))
			{
				thread = new Thread(() => {
					Application.Idle += Initialize;
					Application.Run();
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				mre.WaitOne();
			}
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
				activateCallback.Invoke(Convert.ToInt32(state), s);
			});
		}


		public void Purchase()
		{
			// TODO (?)
		}

		private void Paddle_TransactionBeginEvent(object sender, TransactionBeginEventArgs e)
		{
			beginTransactionCallback.Invoke();

			Debug.WriteLine("Paddle_TransactionBeginEvent");
			Debug.WriteLine(e.ToString());
		}

		private void Paddle_TransactionCompleteEvent(object sender, TransactionCompleteEventArgs e)
		{

			string processStatusJson = JsonConvert.SerializeObject(e.ProcessStatus, Formatting.Indented);

			transactionCompleteCallback.Invoke(
				e.ProductID,
				e.UserEmail,
				e.UserCountry,
				e.LicenseCode,
				e.OrderID,
				e.Flagged,
				processStatusJson);

			Debug.WriteLine("Paddle_TransactionCompleteEvent");
			Debug.WriteLine(e.ToString());
		}

		private void Paddle_TransactionErrorEvent(object sender, TransactionErrorEventArgs e)
		{
			transactionErrorCallback.Invoke(e.Error);
			Debug.WriteLine("Paddle_TransactionErrorEvent");
			Debug.WriteLine(e.ToString());
		}

		private void Paddle_LicensingCompleteEvent(object sender, LicensingCompleteEventArgs e)
		{
			   // TODO
		}

	}
}
