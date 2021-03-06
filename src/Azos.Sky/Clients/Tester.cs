//Generated by Azos.Sky.Clients.Tools.SkyGluecCompiler

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Azos.Glue;
using Azos.Glue.Protocol;


namespace Azos.Sky.Clients
{
// This implementation needs @Azos.@Sky.@Contracts.@ITesterClient, so
// it can be used with ServiceClientHub class

  ///<summary>
  /// Client for glued contract Azos.Sky.Contracts.ITester server.
  /// Each contract method has synchronous and asynchronous versions, the later denoted by 'Async_' prefix.
  /// May inject client-level inspectors here like so:
  ///   client.MsgInspectors.Register( new YOUR_CLIENT_INSPECTOR_TYPE());
  ///</summary>
  public class Tester : ClientEndPoint, @Azos.@Sky.@Contracts.@ITesterClient
  {

  #region Static Members

     private static TypeSpec s_ts_CONTRACT;
     private static MethodSpec @s_ms_TestEcho_0;

     //static .ctor
     static Tester()
     {
         var t = typeof(@Azos.@Sky.@Contracts.@ITester);
         s_ts_CONTRACT = new TypeSpec(t);
         @s_ms_TestEcho_0 = new MethodSpec(t.GetMethod("TestEcho", new Type[]{ typeof(@System.@Object) }));
     }
  #endregion

  #region .ctor
     public Tester(IGlue glue, string node, Binding binding = null) : base(glue, node, binding) { ctor(); }
     public Tester(IGlue glue, Node node, Binding binding = null) : base(glue, node, binding) { ctor(); }

     //common instance .ctor body
     private void ctor()
     {

     }

  #endregion

     public override Type Contract
     {
       get { return typeof(@Azos.@Sky.@Contracts.@ITester); }
     }



  #region Contract Methods

         ///<summary>
         /// Synchronous invoker for  'Azos.Sky.Contracts.ITester.TestEcho'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning '@System.@Object' or WrappedExceptionData instance.
         /// ClientCallException is thrown if the call could not be placed in the outgoing queue.
         /// RemoteException is thrown if the server generated exception during method execution.
         ///</summary>
         public @System.@Object @TestEcho(@System.@Object  @data)
         {
            var call = Async_TestEcho(@data);
            return call.GetValue<@System.@Object>();
         }

         ///<summary>
         /// Asynchronous invoker for  'Azos.Sky.Contracts.ITester.TestEcho'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning no exception or WrappedExceptionData instance.
         /// CallSlot is returned that can be queried for CallStatus, ResponseMsg and result.
         ///</summary>
         public CallSlot Async_TestEcho(@System.@Object  @data)
         {
            var request = new RequestAnyMsg(s_ts_CONTRACT, @s_ms_TestEcho_0, false, RemoteInstance, new object[]{@data});
            return DispatchCall(request);
         }


  #endregion

  }//class
}//namespace
