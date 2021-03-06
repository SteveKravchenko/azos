/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Runtime.Serialization;

namespace Azos.Financial
{
  /// <summary>
  /// Base exception thrown by the financial-related framework
  /// </summary>
  [Serializable]
  public class FinancialException : AzosException
  {
    public FinancialException() { }
    public FinancialException(string message) : base(message) { }
    public FinancialException(string message, Exception inner) : base(message, inner) { }
    protected FinancialException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
