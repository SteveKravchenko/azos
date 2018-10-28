/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Text;

namespace Azos.Sky.Metabase
{

  public enum MetabaseValidationMessageType
  {
    Info = 0,
    Warning,
    Error
  }

  /// <summary>
  /// Denotes a message that is generated by Metabank.Validate method
  /// </summary>
  public sealed class MetabaseValidationMsg
  {
      public MetabaseValidationMsg(MetabaseValidationMessageType type,
                                   Metabank.Catalog catalog,
                                   Metabank.Section section,
                                   string message,
                                   Exception exception = null)
      {
        this.Type = type;
        this.Catalog = catalog;
        this.Section = section;
        this.Message = message;
        this.Exception = exception;
      }

      public readonly MetabaseValidationMessageType Type;
      public readonly Metabank.Catalog Catalog;
      public readonly Metabank.Section Section;
      public readonly string Message;
      public readonly Exception Exception;

      public override string ToString()
      {
        return ToString(true);
      }

      public string ToString(bool type)
      {
        var sb = new StringBuilder();
        if (type)
        {
          sb.Append(Type);
          sb.Append(" ");
        }
        if (Catalog!=null)
        {
          sb.Append("Catalog: ");
          sb.Append(Catalog);
          sb.Append(" ");
        }

        if (Section!=null)
        {
          sb.Append("Section: ");
          sb.Append(Section);
          sb.Append(" ");
        }

        if (Message!=null)
        {
          sb.Append(Message);
          sb.Append(" ");
        }

        if (Exception!=null)
        {
          if (this.Message.IndexOf(Exception.Message)<0)
            sb.Append(Exception.ToMessageWithType());
        }

        return sb.ToString();
      }

  }
}
