/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.Log;
using Azos.Conf;
using Azos.Data.Access;
using Azos.Wave;

using Azos.Sky;
using Azos.Sky.Identification;
using Azos.Sky.Locking;
using Azos.Sky.Metabase;
using Azos.Sky.Workers;
using Azos.Sky.Dynamic;

namespace Azos.Apps
{
  /// <summary>
  /// Provides Sky distributed application chassis implementation of ISkyApplication contract
  /// </summary>
  public class SkyApplication : AzosApplication, ISkyApplication
  {
    #region CONSTS

      public const string CONFIG_WEB_MANAGER_SECTION = "web-manager";

      public const string CONFIG_LOCK_MANAGER_SECTION = "lock-manager";

      public const string CONFIG_PROCESS_MANAGER_SECTION = "process-manager";

      public const string CONFIG_HOST_MANAGER_SECTION = "host-manager";

    #endregion

    #region .ctor

    //framework internal called by derivatives
    protected SkyApplication() : base() { }

    public SkyApplication(SystemApplicationType sysAppType, string[] args)
     : this(sysAppType, false, args, null)
    { }

    public SkyApplication(SystemApplicationType sysAppType, string[] args, ConfigSectionNode rootConfig)
      : this(sysAppType, false, args, rootConfig)
    { }

    public SkyApplication(SystemApplicationType sysAppType,
                         bool allowNesting,
                         string[] args,
                         ConfigSectionNode rootConfig) : base()
    {
      var loader = new BootConfLoader(sysAppType);
      ctor(loader, allowNesting, args, rootConfig);
    }

    internal SkyApplication(IApplication bootApp,
                        SystemApplicationType sysAppType,
                        Metabank metabase,
                        string thisHost,
                        bool allowNesting,
                        string[] args,
                        ConfigSectionNode rootConfig) : base()
    {
      var loader = new BootConfLoader(bootApp, sysAppType, metabase, thisHost);
      ctor(loader, allowNesting, args, rootConfig);
    }

    private void ctor(BootConfLoader loader,
                      bool allowNesting,
                      string[] args,
                      ConfigSectionNode rootConfig)
    {
      try
      {
        m_BootLoader = loader;
        m_NOPLockManager = new NOPLockManager(this);
        var cmdArgs = args == null ? null : new CommandArgsConfiguration(args);
        Constructor(allowNesting,
                    cmdArgs,
                    rootConfig,
                    defaultDI: new Injection.SkyApplicationDependencyInjector(this));
        InitApplication();
      }
      catch
      {
        Destructor();
        throw;
      }
    }

    protected override void Destructor()
    {
      DisposeAndNull(ref m_BootLoader);
      base.Destructor();
    }

    #endregion

    #region Fields

    private BootConfLoader m_BootLoader;
    private ConfigSectionNode m_BootConfigRoot;

    private WaveServer m_WebManagerServer;

    private ILockManagerImplementation m_LockManager;
    private ILockManagerImplementation m_NOPLockManager;
    private GdidGenerator m_GDIDProvider;
    private IProcessManagerImplementation m_ProcessManager;
    private IHostManagerImplementation m_DynamicHostManager;

    #endregion

    #region Properties

      public SystemApplicationType SystemApplicationType => m_BootLoader.SystemApplicationType;

      public Metabank Metabase => m_BootLoader.Metabase;

      public string MetabaseApplicationName => SkySystem.MetabaseApplicationName;

      public string HostName => m_BootLoader.HostName;

      public bool IsDynamicHost => m_BootLoader.IsDynamicHost;

      public string ParentZoneGovernorPrimaryHostName => m_BootLoader.ParentZoneGovernorPrimaryHostName;

      public IConfigSectionNode BootConfigRoot => m_BootConfigRoot;

      internal WaveServer WebManagerServer => m_WebManagerServer;

      public ILockManager LockManager => m_LockManager ?? m_NOPLockManager;

      public IGdidProvider GdidProvider => m_GDIDProvider;

      public IProcessManager ProcessManager => m_ProcessManager;

      public IHostManager DynamicHostManager => m_DynamicHostManager;
    #endregion

    #region Protected

      protected override Configuration GetConfiguration()
      {
        var localConfig = base.GetConfiguration();
        localConfig.Application = this;

        BootConfLoader.ProcessAllExistingIncludes(localConfig.Root, null, "boot");

        m_BootConfigRoot = localConfig.Root;

        var cmdArgs = new string[]{};

        if (CommandArgs.Configuration is CommandArgsConfiguration)
          cmdArgs = ((CommandArgsConfiguration)this.CommandArgs.Configuration).Arguments;

        var result = m_BootLoader.Load(cmdArgs, localConfig);
        result.Application = this;
        return result;
      }

      protected override void DoInitApplication()
      {
        base.DoInitApplication();

        var FROM = GetType().FullName+".DoInitApplication()";

        var metabase = m_BootLoader.Metabase;

        try
        {
          m_GDIDProvider = new GdidGenerator(this, "Sky");

          foreach(var ah in metabase.GDIDAuthorities)
          {
            m_GDIDProvider.AuthorityHosts.Register(ah);
            WriteLog(MessageType.Info, FROM+"{GDIDProvider init}", "Registered GDID authority host: "+ah.ToString());
          }

          WriteLog(MessageType.Info, FROM, "GDIProvider made");
        }
        catch(Exception error)
        {
          WriteLog(MessageType.CatastrophicError, FROM+"{GDIDProvider init}", error.ToMessageWithType());
          try
          {
            m_GDIDProvider.Dispose();
          }
          catch{ }

          m_GDIDProvider = null;
        }

        var wmSection = ConfigRoot[CONFIG_WEB_MANAGER_SECTION];
        if (wmSection.Exists && wmSection.AttrByName(CONFIG_ENABLED_ATTR).ValueAsBool(false))
        try
        {
          m_WebManagerServer = new WaveServer(this);
          m_WebManagerServer.Configure(wmSection);
          m_WebManagerServer.Start();
        }
        catch(Exception error)
        {
          WriteLog(MessageType.CatastrophicError, FROM+"{WebManagerServer start}", error.ToMessageWithType());
          try
          {
            m_WebManagerServer.Dispose();
          }
          catch{}

          m_WebManagerServer = null;
        }

        var lockSection = ConfigRoot[CONFIG_LOCK_MANAGER_SECTION];
        try
        {
          m_LockManager = FactoryUtils.MakeAndConfigure<ILockManagerImplementation>(lockSection, typeof(LockManager));

          WriteLog(MessageType.Info, FROM, "Lock Manager made");

          if (m_LockManager is Daemon)
          {
            ((Daemon)m_LockManager).Start();
            WriteLog(MessageType.Info, FROM, "Lock Manager STARTED");
          }
        }
        catch(Exception error)
        {
          WriteLog(MessageType.CatastrophicError, FROM+"{LockManager start}", error.ToMessageWithType());
          try
          {
            m_LockManager.Dispose();
          }
          catch{}

          m_LockManager = null;
        }

        var procSection = ConfigRoot[CONFIG_PROCESS_MANAGER_SECTION];
        try
        {
          m_ProcessManager = FactoryUtils.MakeAndConfigure<IProcessManagerImplementation>(procSection, typeof(ProcessManager), new object[] { this });

          WriteLog(MessageType.Info, FROM, "Process Manager made");

          if (m_ProcessManager is Daemon)
          {
            ((Daemon)m_ProcessManager).Start();
            WriteLog(MessageType.Info, FROM, "Process Manager STARTED");
          }
        }
        catch (Exception error)
        {
          WriteLog(MessageType.CatastrophicError, FROM+"{ProcessManager start}", error.ToMessageWithType());
          try
          {
            m_ProcessManager.Dispose();
          }
          catch{}

          m_ProcessManager = null;
        }

        var hostSection = ConfigRoot[CONFIG_HOST_MANAGER_SECTION];
        try
        {
          m_DynamicHostManager = FactoryUtils.MakeAndConfigure<IHostManagerImplementation>(procSection, typeof(HostManager), new object[] { this });

          WriteLog(MessageType.Info, FROM, "Dynamic Host Manager made");

          if (m_DynamicHostManager is Daemon)
          {
            ((Daemon)m_DynamicHostManager).Start();
            WriteLog(MessageType.Info, FROM, "Dynamic Host Manager STARTED");
          }
        }
        catch (Exception error)
        {
          WriteLog(MessageType.CatastrophicError, FROM+ "{HostManager start}", error.ToMessageWithType());
          try
          {
            m_DynamicHostManager.Dispose();
          }
          catch{}

          m_DynamicHostManager = null;
        }
      }

      protected override void DoCleanupApplication()
      {
        var FROM = GetType().FullName+".DoCleanupApplication()";

        if (m_DynamicHostManager != null)
        {
          WriteLog(MessageType.Info, FROM, "Finalizing Dynamic Host Manager");
          try
          {
            if (m_DynamicHostManager is Daemon)
            {
              ((Daemon)m_DynamicHostManager).SignalStop();
              ((Daemon)m_DynamicHostManager).WaitForCompleteStop();
              WriteLog(MessageType.Info, FROM, "Dynamic Host Manager STOPPED");
            }

            DisposableObject.DisposeAndNull(ref m_DynamicHostManager);
            WriteLog(MessageType.Info, FROM, "Dynamic Host Manager DISPOSED");
          }
          catch(Exception error)
          {
            WriteLog(MessageType.Error, FROM, "ERROR finalizing Dynamic Host Manager: " + error.ToMessageWithType());
          }
        }

        if (m_ProcessManager!=null)
        {
          WriteLog(MessageType.Info, FROM, "Finalizing Process Manager");
          try
          {
            if (m_ProcessManager is Daemon)
            {
              ((Daemon)m_ProcessManager).SignalStop();
              ((Daemon)m_ProcessManager).WaitForCompleteStop();
              WriteLog(MessageType.Info, FROM, "Process Manager STOPPED");
            }

            DisposableObject.DisposeAndNull(ref m_ProcessManager);
            WriteLog(MessageType.Info, FROM, "Process Manager DISPOSED");
          }
          catch(Exception error)
          {
            WriteLog(MessageType.Error, FROM, "ERROR finalizing Process Manager: " + error.ToMessageWithType());
          }
        }

        if (m_LockManager!=null)
        {
          WriteLog(MessageType.Info, FROM, "Finalizing Lock Manager");
          try
          {
            if (m_LockManager is Daemon)
            {
                ((Daemon)m_LockManager).SignalStop();
                ((Daemon)m_LockManager).WaitForCompleteStop();
                WriteLog(MessageType.Info, FROM, "Lock Manager STOPPED");
            }

            DisposableObject.DisposeAndNull(ref m_LockManager);
            WriteLog(MessageType.Info, FROM, "lock manager DISPOSED");
          }
          catch(Exception error)
          {
            WriteLog(MessageType.Error, FROM, "ERROR finalizing Lock Manager: " + error.ToMessageWithType());
          }
        }

        if (m_WebManagerServer!=null)
        {
          WriteLog(MessageType.Info, FROM, "Finalizing Web Manager Server");
          try
          {
            DisposableObject.DisposeAndNull(ref m_WebManagerServer);
            WriteLog(MessageType.Info, FROM, "Web Manager Server DISPOSED");
          }
          catch (Exception error)
          {
            WriteLog(MessageType.CatastrophicError, FROM, "ERROR finalizing Web Manager Server: " + error.ToMessageWithType());
          }
        }

        if (m_GDIDProvider!=null)
        {
          WriteLog(MessageType.Info, FROM, "Finalizing GDIDProvider");
          try
          {
            DisposableObject.DisposeAndNull(ref m_GDIDProvider);
            WriteLog(MessageType.Info, FROM, "GDIDProvider DISPOSED");
          }
          catch(Exception error)
          {
            WriteLog(MessageType.Error, FROM, "ERROR finalizing GDIDProvider: " + error.ToMessageWithType());
          }
        }
        // Shutdown - must be last
        base.DoCleanupApplication();
      }

    #endregion
  }
}
