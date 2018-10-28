/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
namespace MySqlConnector.Protocol.Serialization
{
	/// <summary>
	/// Specifies whether to perform synchronous or asynchronous I/O.
	/// </summary>
	internal enum IOBehavior
	{
		/// <summary>
		/// Use synchronous I/O.
		/// </summary>
		Synchronous,

		/// <summary>
		/// Use asynchronous I/O.
		/// </summary>
		Asynchronous,
	}
}
