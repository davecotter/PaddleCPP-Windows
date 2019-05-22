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

namespace PaddleWrapper
{
    public class PaddleWrapper
    {
        private string vendorId;
        private string productId;
        private string apiKey;

        private string productName;
        private string vendorName;

        PaddleProduct currentProduct;

        private Thread thread;
        private SynchronizationContext ctx;
        private ManualResetEvent mre;

        public delegate void ShowCheckoutDelegate(PaddleProduct product);
        ShowCheckoutDelegate showCheckoutDelegate;

        // delegates used for native C++ callback functions
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
        }

        public void ShowCheckoutWindow()
        {
            ShowCheckoutWindow(productId);
        }

        public void ShowCheckoutWindow(string specifiedProductId)
        {

            // Initialize the Product you'd like to work with
            var product = PaddleProduct.CreateProduct(specifiedProductId);


            // Ask the Product to get it's latest state and info from the Paddle Platform
            product.Refresh((success) =>
            {
                currentProduct = product;
                // product data was successfully refreshed
                if (success)
                {
                    if (!product.Activated)
                    {
                        // Product is not activated, so let's show the Product Access dialog to gatekeep your app
                        StartCheckoutThread();
                    }
                }
                else
                {
                    // The SDK was unable to get the last info from the Paddle Platform.
                    // We can show the Product Access dialog with the data provided in the PaddleProductConfig object.
                    StartCheckoutThread();
                }
            });

        }

        private static void ShowCheckout(PaddleProduct product)
        {
            Paddle.Instance.ShowCheckoutWindowForProduct(product);
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
            showCheckoutDelegate = ShowCheckout;
            ctx.Send((_) => showCheckoutDelegate.Invoke(currentProduct), null);
        }

        private void StartCheckoutThread()
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
                activateCallback(Convert.ToInt32(state), s);
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
            transactionCompleteCallback?.Invoke(
                e.ProductID,
                e.UserEmail,
                e.UserCountry,
                e.LicenseCode,
                e.OrderID,
                e.Flagged,
                e.ProcessStatus.ToString());

            Debug.WriteLine("Paddle_TransactionCompleteEvent");
            Debug.WriteLine(e.ToString());
        }

        private void Paddle_TransactionErrorEvent(object sender, TransactionErrorEventArgs e)
        {
            transactionErrorCallback?.Invoke(e.Error);
            Debug.WriteLine("Paddle_TransactionErrorEvent");
            Debug.WriteLine(e.ToString());
        }
    }
}
