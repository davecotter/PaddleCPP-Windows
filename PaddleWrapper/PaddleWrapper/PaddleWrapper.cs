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
using System.Threading;
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

        private AutoResetEvent waitHandle;
        private TransactionCompleteEventArgs transactionCompleteEventArgs;
        private string errorString;

        private Thread thread;
        private SynchronizationContext ctx;
        private ManualResetEvent mre;

        public delegate void ShowCheckoutDelegate(PaddleProduct product);
        ShowCheckoutDelegate dlg;

        private PaddleProduct currentProduct;

        public PaddleWrapper(string vendorId, string productId, string apiKey, string productName = "", string vendorName = "")
        {
            this.vendorId = vendorId;
            this.productId = productId;
            this.apiKey = apiKey;
            this.productName = productName;
            this.vendorName = vendorName;

            PaddleProductConfig productInfo;

            // Default Product Config in case we're unable to reach our servers on first run
            productInfo = new PaddleProductConfig { ProductName = this.productName, VendorName = this.vendorName };


            // Initialize the SDK singleton with the config
            Paddle.Configure(apiKey, vendorId, productId, productInfo);

            Paddle.Instance.TransactionBeginEvent += Paddle_TransactionBeginEvent;
            Paddle.Instance.TransactionCompleteEvent += Paddle_TransactionCompleteEvent;
            Paddle.Instance.TransactionErrorEvent += Paddle_TransactionErrorEvent;

        }

        public void ShowCheckoutWindow()
        {
            ShowCheckoutWindow(productId);
        }

        public void ShowCheckoutWindow(string specifiedProductId)
        {

            // Initialize the Product you'd like to work with
            currentProduct = PaddleProduct.CreateProduct(specifiedProductId);
            

            // Ask the Product to get it's latest state and info from the Paddle Platform
            currentProduct.Refresh((success) =>
            {
                // product data was successfully refreshed
                if (success)
                {
                    if (!currentProduct.Activated)
                    {
                        // Product is not activated, so let's show the Product Access dialog to gatekeep your app
                        //ShowCheckout(currentProduct);
                        SetupCheckoutThread();
                    }
                }
                else
                {
                    // The SDK was unable to get the last info from the Paddle Platform.
                    // We can show the Product Access dialog with the data provided in the PaddleProductConfig object.
                    //ShowCheckout(currentProduct);
                    SetupCheckoutThread();
                }
            });

            //waitHandle = new AutoResetEvent(false);

            //// Wait for event completion
            //waitHandle.WaitOne();

            //if (errorString != "")
            //    return errorString;

        }

        private void Initialize(object sender, EventArgs e)
        {
            ctx = SynchronizationContext.Current;
            mre.Set();
            Application.Idle -= Initialize;
            if (ctx == null) throw new ObjectDisposedException("STAThread");
            dlg = ShowCheckout;
            ctx.Post((_) => dlg.Invoke(currentProduct), null);
        }

        private static void ShowCheckout(PaddleProduct product)
        {
            Paddle.Instance.ShowProductAccessWindowForProduct(product);
        }

        /**
         * If the checkout window is invoked from a non-UI thread
         */
        private void SetupCheckoutThread()
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

            //var thread = new Thread(() =>
            //{
            //    Paddle.Instance.ShowProductAccessWindowForProduct(product);
            //    Application.Run();
            //});
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();
        }


        public void Validate()
        {
            Paddle
        }

        public void Activate(string productId, string email, string license)
        {

            // Initialize the Product you'd like to work with
            PaddleProduct product = PaddleProduct.CreateProduct(productId);
            product.ActivateWithEmail(email, license, (VerificationState state, string s) =>
            {
                ActivateCompletion()
                    
            });
        }

        public string ActivateCompletion()
        {

            return "";
        }

         public void Purchase()
        {

        }

        private void Paddle_TransactionBeginEvent(object sender, TransactionBeginEventArgs e)
        {
            waitHandle = new AutoResetEvent(false);
            // Wait for event completion
            waitHandle.WaitOne();

            Debug.WriteLine("Paddle_TransactionBeginEvent");
            Debug.WriteLine(e.ToString());
        }

        private void Paddle_TransactionCompleteEvent(object sender, TransactionCompleteEventArgs e)
        {
            transactionCompleteEventArgs = e;
            Debug.WriteLine("Paddle_TransactionCompleteEvent");
            Debug.WriteLine(e.ToString());
            waitHandle.Set();
        }

        private void Paddle_TransactionErrorEvent(object sender, TransactionErrorEventArgs e)
        {
            Debug.WriteLine("Paddle_TransactionErrorEvent");
            Debug.WriteLine(e.ToString());
            errorString = e.Error;
            waitHandle.Set();
        }

        /**
         * public event TransactionCompleteEventHandler TransactionCompleteEvent;
        public event TransactionBeginEventHandler TransactionBeginEvent;
        public event TransactionErrorEventHandler TransactionErrorEvent;
        public event PageSumbittedEventHandler PageSubmitted;
        public event LicensingStartingEventHandler LicensingStarting;
        */

        /*
        public void setTransactionCompleteEvent (IntPtr eventCallback)
        {
            var eventDelegate = Marshal.GetDelegateForFunctionPointer(eventCallback, typeof(TransactionCompleteEventHandler));
            //GetDelegateForFunctionPointer<TransactionCompleteEventHandler>(eventCallback);
        }
        */
    }
}
