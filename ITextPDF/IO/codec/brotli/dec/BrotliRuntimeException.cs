/* Copyright 2015 Google Inc. All Rights Reserved.

Distributed under MIT license.
See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

using System;

namespace  IText.IO.Codec.Brotli.Dec
{
	/// <summary>Unchecked exception used internally.</summary>
	internal class BrotliRuntimeException : Exception
	{
		internal BrotliRuntimeException(string message)
			: base(message)
		{
		}

		internal BrotliRuntimeException(string message, Exception cause)
			: base(message, cause)
		{
		}
	}
}
