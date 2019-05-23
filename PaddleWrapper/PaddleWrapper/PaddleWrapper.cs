using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;
using PaddleSDK.Licensing;
using System.Diagnostics;
using Newtonsoft.Json;

namespace PaddleWrapper
{
    public enum PaddleWindowType { ProductAccess, Checkout, LicenseActivation };

    public class PaddleWrapper
    {
        private string vendorId;
        private string productId;
        private string apiKey;

        private string productName;
        private string vendorName;

        private PaddleProduct currentProduct;
        private PaddleWindowType currentWindowType;

        private Thread thread;
        private SynchronizationContext ctx;
        private ManualResetEvent mre;


        public delegate void ShowProductWindowDelegate(PaddleProduct product);
        ShowProductWindowDelegate showProductWindowDelegate;

        // Delegates used for native C++ callback functions
        public delegate void CallbackDelegate();
        public delegate void CallbackWithStringDelegate(string s);
        public delegate void CallbackTransactionCompleteDelegate(string ProductID, 
                                                                 string UserEmail, 
                                                                 string UserCountry, 
                                                                 string LicenseCode,
                                                                 string OrderID,
                                                                 bool   Flagged,
                                                                 string ProcessStatus);
        public delegate void CallbackActivateDelegate(int verificationState, string verificationString);

        public CallbackDelegate                     beginTransactionCallback;
        public CallbackTransactionCompleteDelegate  transactionCompleteCallback;
        public CallbackWithStringDelegate           transactionErrorCallback;
        public CallbackActivateDelegate             activateCallback;

        public PaddleWrapper(string vendorId, string productId, string apiKey, string productName = "", string vendorName = "")
        {
            this.vendorId    = vendorId;
            this.productId   = productId;
            this.apiKey      = apiKey;
            this.productName = productName;
            this.vendorName  = vendorName;

            PaddleProductConfig productInfo;

            // Default Product Config in case we're unable to reach our servers on first run
            productInfo = new PaddleProductConfig { ProductName = this.productName, VendorName = this.vendorName };


            // Initialize the SDK singleton with the config
            Paddle.Configure(apiKey, vendorId, productId, productInfo);

            Paddle.Instance.TransactionBeginEvent    += Paddle_TransactionBeginEvent;
            Paddle.Instance.TransactionCompleteEvent += Paddle_TransactionCompleteEvent;
            Paddle.Instance.TransactionErrorEvent    += Paddle_TransactionErrorEvent;

            Paddle.Instance.LicensingCompleteEvent   += Paddle_LicensingCompleteEvent;
        }

        public void ShowPaddleWindow(int windowType)
        {
            ShowPaddleWindow(productId, windowType);
        }

        public void ShowPaddleWindow(string specifiedProductId, int windowType)
        {

            // Initialize the Product you'd like to work with
            var product = PaddleProduct.CreateProduct(specifiedProductId);


            // Ask the Product to get it's latest state and info from the Paddle Platform
            product.Refresh((success) =>
            {
                currentProduct = product;
                currentWindowType = (PaddleWindowType) windowType;

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
            Paddle.Instance.ShowCheckoutWindowForProduct(product);
        }

        private static void ShowProductAccessWindow(PaddleProduct product)
        {
            Paddle.Instance.ShowProductAccessWindowForProduct(product);
        }

        private static void ShowLicenseActivationWindow(PaddleProduct product)
        {
            Paddle.Instance.ShowLicenseActivationWindowForProduct(product);
        }

        private static void 

        //-------------------------------------------------------------------

        // Set up a suitable thread for the checkout window
        // Technique taken from https://stackoverflow.com/questions/21680738/how-to-post-messages-to-an-sta-thread-running-a-message-pump/21684059#21684059
        private void Initialize(object sender, EventArgs e)
        {
            ctx = SynchronizationContext.Current;
            mre.Set();
            Application.Idle -= Initialize;
            if (ctx == null) throw new ObjectDisposedException("STAThread");
            switch(currentWindowType)
            {
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
            ctx.Send((_) => showProductWindowDelegate.Invoke(currentProduct), null);
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
            PaddleProduct product = PaddleProduct.CreateProduct(productId);
            product.ActivateWithEmail(email, license, (VerificationState state, string s) =>
            {
                activateCallback?.Invoke(Convert.ToInt32(state), s);
            });
        }


        public void Purchase()
        {

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
        }

        private void Paddle_TransactionErrorEvent(object sender, TransactionErrorEventArgs e)
        {
            transactionErrorCallback?.Invoke(e.Error);
            Debug.WriteLine("Paddle_TransactionErrorEvent");
            Debug.WriteLine(e.ToString());
        }

        private void Paddle_LicensingCompleteEvent(object sender, LicensingCompleteEventArgs e)
        {
        }

    }
}
