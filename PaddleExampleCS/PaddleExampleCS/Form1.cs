using System;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using PaddleWrapper;

namespace vstest
{
	public partial class Form1 : Form
	{
		public const int	PAD_VENDOR_ID		= 11745;
		public const string	PAD_VENDOR_NAME		= "The Catnip Co.";
		public const string	PAD_VENDOR_AUTH		= "this is not a real vendor auth string";
		public const string	PAD_API_KEY			= "4134242689d26430f89ec0858884ab07";

		public const int	PAD_PRODUCT_ID		= 511013;
		public const string	PAD_PRODUCT_NAME	= "Optimum Cats";

		CPaddleWrapper		i_paddleRef = new CPaddleWrapper(
			PAD_VENDOR_ID, 
			PAD_VENDOR_NAME,
			PAD_VENDOR_AUTH,
			PAD_API_KEY);

		public Form1()
		{
			InitializeComponent();

			i_paddleRef.AddProduct(
				PAD_PRODUCT_ID,
				PAD_PRODUCT_NAME,
				"Thanks for trying " + PAD_PRODUCT_NAME);

			i_paddleRef.CreateInstance(PAD_PRODUCT_ID);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			JObject		jsonObject = new JObject {
				{ CPaddleWrapper.kPaddleCmdKey_SKU, PAD_PRODUCT_ID }
			};
			string		resultStr, jsonStr	= jsonObject.ToString();
			
			Cursor.Current	= Cursors.WaitCursor;
			resultStr		= i_paddleRef.DoCommand(CommandType.Command_VALIDATE, jsonStr);
			Cursor.Current	= Cursors.Arrow;

			CPaddleWrapper.debug_print(resultStr);
			MessageBox.Show("Validate returned a result!");
		}

		private void button2_Click(object sender, EventArgs e)
		{
			JObject		jsonObject = new JObject {
				{ CPaddleWrapper.kPaddleCmdKey_SKU,			PAD_PRODUCT_ID },
				{ CPaddleWrapper.kPaddleCmdKey_EMAIL,		"test@email.com" },
				{ CPaddleWrapper.kPaddleCmdKey_COUPON,		"fake-coupon" },
				{ CPaddleWrapper.kPaddleCmdKey_COUNTRY,		"US" },
				{ CPaddleWrapper.kPaddleCmdKey_POSTCODE,	"94602" },

				//	un comment these to override what's shown
				//{ CPaddleWrapper.kPaddleCmdKey_TITLE,		"Catnip" },
				//{ CPaddleWrapper.kPaddleCmdKey_MESSAGE,	"For fluffy cats" },
			};
			string		resultStr, jsonStr	= jsonObject.ToString();
			
			Cursor.Current	= Cursors.WaitCursor;
			resultStr		= i_paddleRef.DoCommand(CommandType.Command_PURCHASE, jsonStr);
			Cursor.Current	= Cursors.Arrow;

			CPaddleWrapper.debug_print(resultStr);
			MessageBox.Show("Purchase returned a result!");
		}
	}
}
