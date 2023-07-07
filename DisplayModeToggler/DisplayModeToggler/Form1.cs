using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace DisplayModeToggler
{
  public partial class Form1 : Form
  {
    private static Form1 _instance;

    private const int BalloonTime = 1500;

    private ContextMenu _contextMenu;
    
    private MenuItem _menuItemExit;

    private const string ActiveMarker = " *";

    private const string ExeName = "DisplaySwitch.exe";

    private static readonly Mode UnknownMode = new Mode(-1, "Unknown", "");

    private static readonly string ModeNames_Clone = "Clone";
    private static readonly string ModeNames_Extend = "Extend";
    private static readonly string ModeNames_Primary = "Primary";
    private static readonly string ModeNames_Secondary = "Secondary";

    private static readonly List<Mode> Modes = new List<Mode>
    {
      new Mode(0, ModeNames_Clone, "/clone"),
      new Mode(1, ModeNames_Extend, "/extend"),
      new Mode(2, ModeNames_Primary, "/internal"),
      new Mode(3, ModeNames_Secondary, "/external")
    };

    private static bool _amSwitching;

    public Form1()
    {
      InitializeComponent();
      _instance = this;
    }


    private void Form1_Load(object sender, EventArgs e)
    {
      SetupContextMenu();
      Minimize();

      UpdateUI(TryInferDisplayMode());

      SystemEvents.DisplaySettingsChanged += DisplaySettingsChangedEventHandler;
    }

    private void Minimize()
    {
      WindowState = FormWindowState.Minimized;
      notifyIcon.ShowBalloonTip(BalloonTime);
      ShowInTaskbar = false;
      Visible = false;
    }

    private void SetupContextMenu()
    {
      _contextMenu = new ContextMenu();

      foreach (var mode in Modes)
      {
        var menuItem = new MenuItem();
        menuItem.Index = mode.Index;
        menuItem.Text = mode.Name;
        menuItem.Click += menuItem_Click;
        _contextMenu.MenuItems.Add(menuItem);

        listBox1.Items.Add(mode.Name);
      }

      _contextMenu.MenuItems.Add("-");

      _menuItemExit = new MenuItem();
      _menuItemExit.Index = Modes.Count;
      _menuItemExit.Text = @"Exit";
      _menuItemExit.Click += menuItemExit_Click;
      _contextMenu.MenuItems.AddRange(new[] { _menuItemExit });

      notifyIcon.ContextMenu = _contextMenu;
    }

    private void Form1_LocationChanged(object sender, EventArgs e)
    {
      if (WindowState == FormWindowState.Minimized)
      {
        Minimize();
      }
    }

    private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Left)
      {
        InvokeRightClick();
      }
      else if (e.Button == MouseButtons.Middle)
      {
        RunExe();
      }
    }

    private void InvokeRightClick()
    {
      var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
      if (mi != null)
      {
        mi.Invoke(notifyIcon, null);
      }
      else
      {
        Console.WriteLine(@"Failed at InvokeRightClick()");
      }
    }

    private static void RunExe(Mode mode = null)
    {
      var process = new Process
      {
        StartInfo =
        {
          FileName = ExeName,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
          Arguments = mode == null ? "" : mode.Command
        }
      };

      process.Start();
      process.WaitForExit();
    }

    private void menuItem_Click(object sender, EventArgs e) =>
      PerformSwitch(Modes[((MenuItem)sender).Index]);

    private void menuItemExit_Click(object sender, EventArgs e) => 
        Close();

    private void PerformSwitch(Mode mode)
    {
      try
      {
        _amSwitching = true;

        RunExe(mode);

        notifyIcon.ShowBalloonTip(BalloonTime, "Switched Display Mode", mode.Name, ToolTipIcon.None);
        notifyIcon.Text = mode.Name;

        UpdateUI(mode);
      }
      catch (Exception ex)
      {
        _amSwitching = false;

        Console.WriteLine(ex.ToString());
        notifyIcon.ShowBalloonTip(
          BalloonTime, 
          "Error Switching Display Mode", 
          "Could not switch to mode :  " + mode.Name, 
          ToolTipIcon.Error
          );
      }
    }

    private static void SetActiveMarker(Mode currentMode)
    {
      var menuItems = _instance.notifyIcon.ContextMenu.MenuItems;

      foreach (var mode in Modes)
      {
        menuItems[mode.Index].Text = mode.Name;
      }

      if (currentMode != UnknownMode)
      {
        menuItems[currentMode.Index].Text += ActiveMarker;
      }
    }

    private static void SetIcon(Mode currentMode)
    {
      try
      {
        var icon = new Icon(currentMode.Name + ".ico");
        _instance.notifyIcon.Icon = icon;
      }
      catch (Exception)
      {
        _instance.notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
      }
    }

    private static void UpdateUI(Mode mode)
    {
      SetActiveMarker(mode);
      SetIcon(mode);
    }

    public static Mode TryInferDisplayMode() => 
      Screen.AllScreens.Length > 1 ? Modes.First(m => m.Name == ModeNames_Extend) : UnknownMode;

    private static void DisplaySettingsChangedEventHandler(object sender, EventArgs e)
    {
      if (_amSwitching)
      {
        _amSwitching = false;
        return;
      }

      UpdateUI(TryInferDisplayMode());
    } 
      
  }

  public class Mode
  {
    public Mode(int index, string name, string command)
    {
      Index = index;
      Name = name;
      Command = command;
    }

    public readonly int Index;
    public readonly string Name;
    public readonly string Command;
  }
}


