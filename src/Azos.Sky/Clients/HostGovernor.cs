//Generated by Azos.Sky.Clients.Tools.SkyGluecCompiler
using System;

using Azos.Glue;
using Azos.Glue.Protocol;


namespace Azos.Sky.Clients
{
// This implementation needs @Azos.@Sky.@Contracts.@IHostGovernorClient, so
// it can be used with ServiceClientHub class

  ///<summary>
  /// Client for glued contract Azos.Sky.Contracts.IHostGovernor server.
  /// Each contract method has synchronous and asynchronous versions, the later denoted by 'Async_' prefix.
  /// May inject client-level inspectors here like so:
  ///   client.MsgInspectors.Register( new YOUR_CLIENT_INSPECTOR_TYPE());
  ///</summary>
  public class HostGovernor : ClientEndPoint, @Azos.@Sky.@Contracts.@IHostGovernorClient
  {

  #region Static Members

     private static TypeSpec s_ts_CONTRACT;
     private static MethodSpec @s_ms_GetHostInfo_0;

     //static .ctor
     static HostGovernor()
     {
         var t = typeof(@Azos.@Sky.@Contracts.@IHostGovernor);
         s_ts_CONTRACT = new TypeSpec(t);
         @s_ms_GetHostInfo_0 = new MethodSpec(t.GetMethod("GetHostInfo", new Type[]{  }));
     }
  #endregion

  #region .ctor
     public HostGovernor(IGlue glue, string node, Binding binding = null) : base(glue, node, binding) { ctor(); }
     public HostGovernor(IGlue glue, Node node, Binding binding = null) : base(glue, node, binding) { ctor(); }

     //common instance .ctor body
     private void ctor()
     {

     }

  #endregion

     public override Type Contract
     {
       get { return typeof(@Azos.@Sky.@Contracts.@IHostGovernor); }
     }



  #region Contract Methods

         ///<summary>
         /// Synchronous invoker for  'Azos.Sky.Contracts.IHostGovernor.GetHostInfo'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning '@Azos.@Sky.@Contracts.@HostInfo' or WrappedExceptionData instance.
         /// ClientCallException is thrown if the call could not be placed in the outgoing queue.
         /// RemoteException is thrown if the server generated exception during method execution.
         ///</summary>
         public @Azos.@Sky.@Contracts.@HostInfo @GetHostInfo()
         {
            var call = Async_GetHostInfo();
            return call.GetValue<@Azos.@Sky.@Contracts.@HostInfo>();
         }

         ///<summary>
         /// Asynchronous invoker for  'Azos.Sky.Contracts.IHostGovernor.GetHostInfo'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning no exception or WrappedExceptionData instance.
         /// CallSlot is returned that can be queried for CallStatus, ResponseMsg and result.
         ///</summary>
         public CallSlot Async_GetHostInfo()
         {
            var request = new RequestAnyMsg(s_ts_CONTRACT, @s_ms_GetHostInfo_0, false, RemoteInstance, new object[]{});
            return DispatchCall(request);
         }


  #endregion

  }//class
}//namespace
