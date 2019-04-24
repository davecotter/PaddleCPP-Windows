using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;
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

        public PaddleWrapper(string vendorId, string productId, string apiKey, string productName = "", string vendorName = "")
        {
            this.vendorId = vendorId;
            this.productId = productId;
            this.apiKey = apiKey;
            this.productName = productName;
            this.vendorName = vendorName;

            PaddleProductConfig productInfo;

            if (this.productName != "" && this.vendorName != "")
            {
                // Default Product Config in case we're unable to reach our servers on first run
                productInfo = new PaddleProductConfig { ProductName = this.productName, VendorName = this.vendorName };
            }
            else
            {
                productInfo = new PaddleProductConfig { };
            }


            // Initialize the SDK singleton with the config
            Paddle.Configure(apiKey, vendorId, productId, productInfo);

            Paddle.Instance.TransactionCompleteEvent += Paddle_TransactionCompleteEvent;
            Paddle.Instance.TransactionErrorEvent += Paddle_TransactionErrorEvent;

        }

        public void Setup()
        {



            // Set up events for Checkout.
            // We recommend handling the TransactionComplete and TransactionError events.
            // TransactionBegin is optional.
            /* TODO
            Paddle.Instance.TransactionCompleteEvent += Paddle_TransactionCompleteEvent;
            Paddle.Instance.TransactionErrorEvent += Paddle_TransactionErrorEvent;
            Paddle.Instance.TransactionBeginEvent += Paddle_TransactionBeginEvent;
            */

            
        }

        public void ShowCheckoutWindow()
        {
            ShowCheckoutWindow(productId);
        }

        public String ShowCheckoutWindow(string specifiedProductId)
        { 

            // Initialize the Product you'd like to work with
            PaddleProduct product = PaddleProduct.CreateProduct(specifiedProductId);

            errorString = "";

            // Ask the Product to get it's latest state and info from the Paddle Platform
            product.Refresh((success) =>
            {
                // product data was successfully refreshed
                if (success)
                {
                    if (!product.Activated)
                    {
                        // Product is not activated, so let's show the Product Access dialog to gatekeep your app
                        Paddle.Instance.ShowProductAccessWindowForProduct(product);
                    }
                }
                else
                {
                    // The SDK was unable to get the last info from the Paddle Platform.
                    // We can show the Product Access dialog with the data provided in the PaddleProductConfig object.
                    Paddle.Instance.ShowProductAccessWindowForProduct(product);
                }
            });

            waitHandle = new AutoResetEvent(false);

            // Wait for event completion
            waitHandle.WaitOne();

            if (errorString != "")
                return errorString;

            return transactionCompleteEventArgs.ToString();
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
