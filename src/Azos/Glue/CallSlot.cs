/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Threading;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Glue.Protocol;

namespace Azos.Glue
{
    /// <summary>
    /// Represents a class that is immediately returned after transport sends RequestMsg.
    /// This class provides CallStatus and RequestID properties where the later is used to match the incoming ResponseMsg.
    /// CallSlots are kinds of "spirit-less" mailboxes that keep state about the call, but do not own any threads/call events.
    /// Working with CallSlots from calling code's existing thread of execution is the most efficient way of working with Glue (in high load cases), as
    ///  it does not create extra object instances (tasks do) for asynchronous coordination and continuation.
    /// It is possible to obtain an instance of CallSlot.AsTask in which case that instance is registered with the framework-internal reactor
    ///  so task does complete normally even on timeout, however, in high-throughput apps (10K+ calls per second) this method is not the most efficient one
    ///  as it allocates additional objects (task, list entry etc.) that eventually increase GC load for long runs.
    ///  Note: A 3.2 Ghz 4-Core I7 server with 8Gb of ram can easily handle 50K+ 2-way calls a second (given little business server logic and simple payload).
    ///  See also: CallReactor class
    /// </summary>
    public sealed partial class CallSlot
    {
      public const int DEFAULT_TIMEOUT_MS = 20000;


      /// <summary>
      /// INTERNAL METHOD. Developers do not call!
      /// This constructor is used by an Async binding that delivers response after call slot was created
      /// </summary>
      public CallSlot(ClientEndPoint client, ClientTransport clientTransport, RequestMsg request, CallStatus status, int timeoutMs = 0)
      {
         m_Client = client;
         m_ClientTransport = clientTransport;
         m_RequestID = request.RequestID;
         m_CallStatus = status;
         m_OneWay = request.OneWay;
         m_StartTime = DateTime.UtcNow;
         m_TimeoutMs = timeoutMs>0 ? timeoutMs : DEFAULT_TIMEOUT_MS;

         if (!m_OneWay && client.Binding.MeasureStatTimes)
         {
           m_StatStartTimeTicks = client.Binding.StatTimeTicks;
           m_StatRoundtripTimeKey = client.Binding.GetClientCallStatTimeKey(client, request);
         }
      }

      /// <summary>
      /// INTERNAL METHOD. Developers do not call!
      /// This constructor is used by a synchronous binding that delivers response right after sending it.
      /// ONLY for OneWayCall = false
      /// </summary>
      public CallSlot(ClientEndPoint client,
                      ClientTransport clientTransport,
                      long actualStartTimeTicks,
                      DateTime actualStartTimeUtc,
                      RequestMsg request,
                      ResponseMsg response,
                      int timeoutMs)
      {
         m_Client = client;
         m_ClientTransport = clientTransport;
         m_RequestID = request.RequestID;
         m_OneWay=false;
         m_StartTime = actualStartTimeUtc;
         m_TimeoutMs = timeoutMs>0 ? timeoutMs : DEFAULT_TIMEOUT_MS;

         if (client.Binding.MeasureStatTimes)
         {
           m_StatStartTimeTicks = actualStartTimeTicks;
           m_StatRoundtripTimeKey = client.Binding.GetClientCallStatTimeKey(client, request);
         }

         m_ResponseMsg = response;
         m_CallStatus = m_ResponseMsg.OK ? CallStatus.ResponseOK : CallStatus.ResponseError;
         var remoteInstance = response.RemoteInstance;
         if (remoteInstance.HasValue)
            m_Client.__setRemoteInstance(remoteInstance.Value);

         if (m_StatRoundtripTimeKey!=null && m_Client.Binding.MeasureStatTimes)
         {
             m_StatRoundtripEndTimeTicks = m_Client.Binding.StatTimeTicks;
             m_ClientTransport.stat_Time(m_StatRoundtripTimeKey, m_StatRoundtripEndTimeTicks - m_StatStartTimeTicks);
         }
      }


      private object m_Sync = new Object();//todo: extra allocation - lock on (this)even though it is not safe

      private ClientEndPoint m_Client;
      private ClientTransport m_ClientTransport;
      private FID m_RequestID;
      private bool m_OneWay;

      internal volatile TaskCompletionSource<CallSlot> m_TaskCompletionSource;//only used for tasks and async/wait cases

      private DateTime m_StartTime;
      private volatile CallStatus m_CallStatus;
      private string m_DispatchErrorMessage;
      private volatile ResponseMsg m_ResponseMsg;
      private int m_ResponseInspected;
      private int m_TimeoutMs;

      private long m_StatStartTimeTicks;
      private long m_StatRoundtripEndTimeTicks;
      private string m_StatRoundtripTimeKey;


      internal IGlueImplementation Glue => (IGlueImplementation)m_Client.Glue;

      /// <summary>
      /// Returns client endpoint that initiated this call
      /// </summary>
      public ClientEndPoint Client
      {
        get { return m_Client; }
      }

      /// <summary>
      /// Returns client transport that this instance is originated from
      /// </summary>
      public ClientTransport ClientTransport
      {
        get { return m_ClientTransport; }
      }

      /// <summary>
      /// General-purpose property that clients can use for attaching an arbitrary context to this instance. This property has no use in the framework
      /// </summary>
      public object CallContext {get; set;}


      /// <summary>
      /// Returns current call status. Timeout is returned when response has not arrived from the other side in allotted time. This is a non-blocking call
      /// </summary>
      public CallStatus CallStatus
      {
        get
        {
          if (m_CallStatus==CallStatus.Dispatched && !m_OneWay)
          {
            lock(m_Sync)
            {
              if (m_CallStatus==CallStatus.Dispatched)//check for timeout
              {
                //timeout happens when wait time exceeded or app/glue shuts down
                if (!Glue.Active || (DateTime.UtcNow - m_StartTime).TotalMilliseconds > m_TimeoutMs)
                {
                  m_CallStatus = CallStatus.Timeout;
                  completePendingTask(); //task completes on timeout
                }
              }
            }
          }
          return m_CallStatus;
        }
      }

      /// <summary>
      /// Optionally returns reason of the dispatch message failure
      /// </summary>
      public string DispatchErrorMessage { get { return m_DispatchErrorMessage ?? string.Empty; } }

      /// <summary>
      /// Indicates that this call does not expect a response message from the server side
      /// </summary>
      public bool OneWay
      {
        get { return m_OneWay; }
      }


      /// <summary>
      /// Returns request ID for the request that was sent and generated this slot instance
      /// </summary>
      public FID RequestID
      {
        get { return m_RequestID;}
      }

      /// <summary>
      /// Returns timeout for this instance
      /// </summary>
      public int TimeoutMs
      {
        get { return m_TimeoutMs; }
      }

      /// <summary>
      /// Returns UTC timestamp of call initiation
      /// </summary>
      public DateTime StartTime
      {
        get { return m_StartTime; }
      }

      /// <summary>
      /// When binding's MeasureStatTimes enabled, returns the operation start tick count
      /// </summary>
      public long StatStartTimeTicks
      {
        get { return m_StatStartTimeTicks; }
      }

      /// <summary>
      /// When binding's MeasureStatTimes enabled, returns the operation end (when response arrives) tick count
      /// </summary>
      public long StatRoundtripEndTimeTicks
      {
        get { return m_StatRoundtripEndTimeTicks; }
      }

      /// <summary>
      /// When binding's MeasureStatTimes enabled, returns the name of the stat key
      /// </summary>
      public string StatRoundtripTimeKey
      {
        get { return m_StatRoundtripTimeKey; }
      }


      /// <summary>
      /// Gets the CallSlot instance as a task that gets completed either instantly for one-way calls or when result arrives or timeout happens.
      /// The returned task never gets canceled
      /// </summary>
      public Task<CallSlot> AsTask
      {
        get
        {
          if (m_TaskCompletionSource!=null) return m_TaskCompletionSource.Task;
          lock(m_Sync)
          {
            if (m_TaskCompletionSource!=null) return m_TaskCompletionSource.Task;//dbl check under lock
            var tcs = new TaskCompletionSource<CallSlot>(this);

            if (Available) tcs.SetResult(this);//Complete tasks for OneWay or already completed calls
            else
            if (CallStatus!=CallStatus.Dispatched) tcs.SetResult(this);//Complete task for calls that are not pending - were not dispatched properly or timed out
            else
              Glue.SubscribeCallSlotWithTaskReactor(this);//remember this instance to be called back on Timeout in future

            m_TaskCompletionSource = tcs;
            return m_TaskCompletionSource.Task;
          }
        }
      }

      /// <summary>
      /// Creates a wrapper task around CallSlot.AsTask and returns CallSlot.GetValue() as TCallResult-returning Task.
      /// Note: the created wrapper task is not cached. For less allocations '(await slot).GetValue&lt;T&gt;()' instead
      /// </summary>
      public Task<TCallResult> AsTaskReturning<TCallResult>()
      {
        return this.AsTask.ContinueWith( (cst) => cst.Result.GetValue<TCallResult>() , TaskContinuationOptions.ExecuteSynchronously);
      }

      /// <summary>
      /// Creates a wrapper task around CallSlot.AsTask and returns CallSlot.CheckVoidValue()
      /// Note: the created wrapper task is not cached. For less allocations '(await slot).CheckVoidValue()' instead
      /// </summary>
      public Task AsTaskReturningVoid()
      {
        return this.AsTask.ContinueWith( (cst) => cst.Result.CheckVoidValue() , TaskContinuationOptions.ExecuteSynchronously);
      }



      /// <summary>
      /// Called internally by framework to populate the response message when one asynchronously arrives from transport
      /// </summary>
      internal void DeliverResponse(ResponseMsg response)
      {
        lock(m_Sync)
        {
              m_ResponseMsg = response;
              m_CallStatus = m_ResponseMsg.OK ? CallStatus.ResponseOK : CallStatus.ResponseError;

              var remoteInstance = response.RemoteInstance;
              if (remoteInstance.HasValue)
               m_Client.__setRemoteInstance(remoteInstance.Value);

              if (m_StatRoundtripTimeKey!=null && m_Client.Binding.MeasureStatTimes)
              {
                 m_StatRoundtripEndTimeTicks = m_Client.Binding.StatTimeTicks;
                 m_ClientTransport.stat_Time(m_StatRoundtripTimeKey, m_StatRoundtripEndTimeTicks - m_StatStartTimeTicks);
              }

              Monitor.Pulse(m_Sync);

              completePendingTask();//result arrived - normal case
        }
      }

      /// <summary>
      /// Signal erroneous completion of request dispatching. This method is used by transports that post outgoing message asynchronously
      /// </summary>
      internal void SignalDispatchError(string errorMessage)
      {
          lock (m_Sync)
          {
              m_DispatchErrorMessage = errorMessage;
              m_CallStatus = CallStatus.DispatchError;

              Monitor.Pulse(m_Sync);

              completePendingTask();//dispatch error that became apparent later-on (after CallSlot was created)
          }
      }

      /// <summary>
      /// Returns true when response has come and available so no blocking will occur when reading response. This is a non-blocking call.
      /// Always returns true for operations marked as [OneWay]
      /// </summary>
      public bool Available
      {
        get { return m_OneWay || m_ResponseMsg!=null; }
      }

      /// <summary>
      /// Blocks until response comes or timeout happens. Response message inspection happens in the context of a calling thread
      /// </summary>
      public ResponseMsg ResponseMsg
      {
        get
        {
           if (m_OneWay)
            throw new InvalidGlueOperationException(StringConsts.GLUE_ONE_WAY_RESPONSE_ERROR);

           if (m_ResponseMsg == null)
           {
               if (m_CallStatus==CallStatus.DispatchError || m_CallStatus==CallStatus.Timeout)
                 throw new ClientCallException(m_CallStatus, m_DispatchErrorMessage);

               lock(m_Sync)
                   while (m_ResponseMsg==null &&
                          Glue.Active &&
                          this.CallStatus==CallStatus.Dispatched) //this.CallStatus checks for timeout
                   {
                     Monitor.Wait(m_Sync, 250);
                   }

               //lock above ensures memory barrier on checking m_ResponseMsg
               if (m_ResponseMsg==null)
                throw new ClientCallException(m_CallStatus);
           }


           if (Interlocked.CompareExchange(ref m_ResponseInspected, 1, 0) == 0)
           {
                var response = m_ResponseMsg;

                IClientMsgInspector inspector = null;
                try
                {
                    //Glue level inspectors
                    foreach(var insp in Glue.ClientMsgInspectors.OrderedValues)
                    {
                        inspector = insp;
                        response = inspector.ClientDeliverResponse(this, response);
                    }

                    //Binding level inspectors
                    foreach(var insp in Client.Binding.ClientMsgInspectors.OrderedValues)
                    {
                        inspector = insp;
                        response = insp.ClientDeliverResponse(this, response);
                    }

                    //Client level inspectors
                    foreach(var insp in Client.MsgInspectors.OrderedValues)
                    {
                        inspector = insp;
                        response = insp.ClientDeliverResponse(this, response);
                    }
                }
                catch(Exception error)
                {
                    throw new ClientMsgInspectionException(inspector, error);
                }
                m_ResponseMsg = response;
           }

           return m_ResponseMsg;
        }

      }


       /// <summary>
       /// Returns a value from the other side, that is -
       /// gets the response message and checks it for errors,
       /// throwing RemoteError exception if one came from server.
       /// Accessing this property blocks calling thread until either ResponseMsg arrives or timeout expires.
       /// Check 'Available' property not to block. Accessing this method for [OneWay] methods throws.
       /// </summary>
       public T GetValue<T>()
       {
           var value = ResponseMsg.ReturnValue;

           var red = ResponseMsg.ExceptionData;

           if (red!=null) throw new RemoteException(red);

           return (T)value;
       }

       /// <summary>
       /// Checks for a valid void value returned from the other side, that is -
       /// gets the response message and checks it for errors,
       /// throwing RemoteError exception if one came from server.
       /// Accessing this property blocks calling thread until either ResponseMsg arrives or timeout expires.
       /// Check 'Available' property not to block. Accessing this method for [OneWay] methods throws.
       /// </summary>
       public void CheckVoidValue()
       {
           var red = ResponseMsg.ExceptionData;

           if (red!=null) throw new RemoteException(red);
       }


       public override int GetHashCode()
       {
         return this.m_RequestID.GetHashCode();
       }

       public override bool Equals(object obj)
       {
         var other = obj as CallSlot;
         if (other==null) return false;
         return this.m_RequestID.Equals(other.m_RequestID);
       }

       public override string ToString()
       {
         return "CallSlot[{0}/{1}/{2}]".Args(m_RequestID, m_OneWay ? "OneWay" : "TwoWay", m_CallStatus);
       }

       /// <summary>
       /// Awaits task status change - either timeout or value delivery. Already completed for one-way calls
       /// </summary>
       public System.Runtime.CompilerServices.TaskAwaiter<CallSlot> GetAwaiter()
       {
         return this.AsTask.GetAwaiter();
       }

       //this must be called from under lock(m_Sync)
       private void completePendingTask()
       {
         if (m_TaskCompletionSource==null) return; //must be here
         if (m_TaskCompletionSource.Task.IsCompleted) return;

         //Invoke asynchronously, as TrySetResult may synchronously run long continuations
         Task.Run( () => m_TaskCompletionSource.TrySetResult(this) );
       }

    }

}
