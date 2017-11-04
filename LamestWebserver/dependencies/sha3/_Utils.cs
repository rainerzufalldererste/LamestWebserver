using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography {
	internal static class _Utils {
		private static volatile RNGCryptoServiceProvider _rng = null;

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//internal static extern bool _ProduceLegacyHmacValues();

		internal static RNGCryptoServiceProvider StaticRandomNumberGenerator {
			get {
				if (_rng == null) {
					_rng = new RNGCryptoServiceProvider();
				}
				return _rng;
			}
		}

		internal static byte[] GenerateRandom(int keySize) {
			byte[] data = new byte[keySize];
			StaticRandomNumberGenerator.GetBytes(data);
			return data;
		}


		#region " Environment "
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//internal static extern string GetResourceFromDefault(string key);
		////[SecuritySafeCritical, TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		//internal static string GetResourceString(string key) {
		//	return GetResourceFromDefault(key);
		//}
		#endregion
	}
}
