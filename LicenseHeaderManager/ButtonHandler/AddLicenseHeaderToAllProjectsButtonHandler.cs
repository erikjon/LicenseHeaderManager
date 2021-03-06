﻿using System;
using System.ComponentModel;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Options;
using LicenseHeaderManager.PackageCommands;
using LicenseHeaderManager.SolutionUpdateViewModels;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell.Interop;

namespace LicenseHeaderManager.ButtonHandler
{
  public class AddLicenseHeaderToAllProjectsButtonHandler
  {
    private readonly LicenseHeaderReplacer _licenseReplacer;
    private readonly IDefaultLicenseHeaderPage _defaultLicenseHeaderPage;
    private readonly DTE2 _dte2;

    public AddLicenseHeaderToAllProjectsButtonHandler(LicenseHeaderReplacer licenseReplacer, IDefaultLicenseHeaderPage defaultLicenseHeaderPage, DTE2 dte2)
    {
      _licenseReplacer = licenseReplacer;
      _defaultLicenseHeaderPage = defaultLicenseHeaderPage;
      _dte2 = dte2;
    }

    private System.Threading.Thread _solutionUpdateThread;
    private bool _resharperSuspended;

    public void HandleButton(object sender, EventArgs e)
    {
      var solutionUpdateViewModel = new SolutionUpdateViewModel();
      var addLicenseHeaderToAllProjectsCommand = new AddLicenseHeaderToAllProjectsCommand (_licenseReplacer, _defaultLicenseHeaderPage, solutionUpdateViewModel);
      var buttonThreadWorker = new SolutionLevelButtonThreadWorker(addLicenseHeaderToAllProjectsCommand);
      var dialog = new SolutionUpdateDialog(solutionUpdateViewModel);


      dialog.Closing += DialogOnClosing;
      _resharperSuspended = CommandUtility.ExecuteCommandIfExists("ReSharper_Suspend", _dte2);
      Dispatcher uiDispatcher = Dispatcher.CurrentDispatcher;

      buttonThreadWorker.ThreadDone += (o, args) =>
      {
        uiDispatcher.BeginInvoke(new Action(() => { dialog.Close(); }));
        ResumeResharper();
      };

      _solutionUpdateThread = new System.Threading.Thread(buttonThreadWorker.Run)
      {
        IsBackground = true
      };
      _solutionUpdateThread.Start(_dte2.Solution);

      dialog.ShowModal(); 
    }

    private void DialogOnClosing(object sender, CancelEventArgs cancelEventArgs)
    {
      _solutionUpdateThread.Abort();

      ResumeResharper();
    }

    private void ResumeResharper()
    {
      if (_resharperSuspended)
      {
        CommandUtility.ExecuteCommand("ReSharper_Resume", _dte2);
      }
    }
  }
}
