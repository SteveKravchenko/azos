﻿using System;
using System.Runtime.Serialization;

namespace Azos.Sky.WebManager
{
  /// <summary>
  /// Base exception thrown by the WebManager site
  /// </summary>
  [Serializable]
  public class WebManagerException : SkyException
  {
    public WebManagerException(int code, string message) : this(message, null, code, null, null) { }
    public WebManagerException(string message) : this(message, null, 0, null, null) { }
    public WebManagerException(string message, Exception inner) : this(message, inner, 0, null, null) { }
    public WebManagerException(string message, Exception inner, int code, string sender, string topic) : base(message, inner, code, sender, topic) { }
    protected WebManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
