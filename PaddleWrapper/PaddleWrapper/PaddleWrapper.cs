using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;

namespace PaddleWrapper
{
    public class PaddleWrapper
    {
        private string vendorId;
        private string productId;
        private string apiKey;
        
        private string productName;
        private string vendorName;

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

        public void ShowCheckoutWindow(string specifiedProductId)
        { 

            // Initialize the Product you'd like to work with
            PaddleProduct product = PaddleProduct.CreateProduct(specifiedProductId);

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
