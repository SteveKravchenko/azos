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
// This implementation needs @Azos.@Sky.@Contracts.@IGDIDPersistenceRemoteLocationClient, so
// it can be used with ServiceClientHub class

  ///<summary>
  /// Client for glued contract Azos.Sky.Contracts.IGDIDPersistenceRemoteLocation server.
  /// Each contract method has synchronous and asynchronous versions, the later denoted by 'Async_' prefix.
  /// May inject client-level inspectors here like so:
  ///   client.MsgInspectors.Register( new YOUR_CLIENT_INSPECTOR_TYPE());
  ///</summary>
  public class GdidPersistenceRemoteLocation : ClientEndPoint, @Azos.@Sky.@Contracts.IGdidPersistenceRemoteLocationClient
  {

  #region Static Members

     private static TypeSpec s_ts_CONTRACT;
     private static MethodSpec @s_ms_Read_0;
     private static MethodSpec @s_ms_Write_1;

     //static .ctor
     static GdidPersistenceRemoteLocation()
     {
         var t = typeof(@Azos.@Sky.@Contracts.IGdidPersistenceRemoteLocation);
         s_ts_CONTRACT = new TypeSpec(t);
         @s_ms_Read_0 = new MethodSpec(t.GetMethod("Read", new Type[]{ typeof(@System.@Byte), typeof(@System.@String), typeof(@System.@String) }));
         @s_ms_Write_1 = new MethodSpec(t.GetMethod("Write", new Type[]{ typeof(@System.@String), typeof(@System.@String), typeof(@Azos.@Data.@GDID) }));
     }
  #endregion

  #region .ctor
     public GdidPersistenceRemoteLocation(IGlue glue, string node, Binding binding = null) : base(glue, node, binding) { ctor(); }
     public GdidPersistenceRemoteLocation(IGlue glue, Node node, Binding binding = null) : base(glue, node, binding) { ctor(); }

     //common instance .ctor body
     private void ctor()
     {

     }

  #endregion

     public override Type Contract
     {
       get { return typeof(@Azos.@Sky.@Contracts.IGdidPersistenceRemoteLocation); }
     }



  #region Contract Methods

         ///<summary>
         /// Synchronous invoker for  'Azos.Sky.Contracts.IGDIDPersistenceRemoteLocation.Read'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning '@System.@Nullable[@Azos.@Data.@GDID]' or WrappedExceptionData instance.
         /// ClientCallException is thrown if the call could not be placed in the outgoing queue.
         /// RemoteException is thrown if the server generated exception during method execution.
         ///</summary>
         public @System.@Nullable<@Azos.@Data.@GDID> @Read(@System.@Byte  @authority, @System.@String  @sequenceName, @System.@String  @scopeName)
         {
            var call = Async_Read(@authority, @sequenceName, @scopeName);
            return call.GetValue<@System.@Nullable<@Azos.@Data.@GDID>>();
         }

         ///<summary>
         /// Asynchronous invoker for  'Azos.Sky.Contracts.IGDIDPersistenceRemoteLocation.Read'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning no exception or WrappedExceptionData instance.
         /// CallSlot is returned that can be queried for CallStatus, ResponseMsg and result.
         ///</summary>
         public CallSlot Async_Read(@System.@Byte  @authority, @System.@String  @sequenceName, @System.@String  @scopeName)
         {
            var request = new RequestAnyMsg(s_ts_CONTRACT, @s_ms_Read_0, false, RemoteInstance, new object[]{@authority, @sequenceName, @scopeName});
            return DispatchCall(request);
         }



         ///<summary>
         /// Synchronous invoker for  'Azos.Sky.Contracts.IGDIDPersistenceRemoteLocation.Write'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning no exception or WrappedExceptionData instance.
         /// ClientCallException is thrown if the call could not be placed in the outgoing queue.
         /// RemoteException is thrown if the server generated exception during method execution.
         ///</summary>
         public void @Write(@System.@String  @sequenceName, @System.@String  @scopeName, @Azos.@Data.@GDID  @value)
         {
            var call = Async_Write(@sequenceName, @scopeName, @value);
            call.CheckVoidValue();
         }

         ///<summary>
         /// Asynchronous invoker for  'Azos.Sky.Contracts.IGDIDPersistenceRemoteLocation.Write'.
         /// This is a two-way call per contract specification, meaning - the server sends the result back either
         ///  returning no exception or WrappedExceptionData instance.
         /// CallSlot is returned that can be queried for CallStatus, ResponseMsg and result.
         ///</summary>
         public CallSlot Async_Write(@System.@String  @sequenceName, @System.@String  @scopeName, @Azos.@Data.@GDID  @value)
         {
            var request = new RequestAnyMsg(s_ts_CONTRACT, @s_ms_Write_1, false, RemoteInstance, new object[]{@sequenceName, @scopeName, @value});
            return DispatchCall(request);
         }


  #endregion

  }//class
}//namespace
