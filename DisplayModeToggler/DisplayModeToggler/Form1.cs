using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace DisplayModeToggler
{
  public partial class Form1 : Form
  {
    private const int BalloonTime = 1500;

    private ContextMenu _contextMenu;
    
    private MenuItem _menuItemExit;

    private const string ActiveMarker = " *";

    private const string ExeName = "DisplaySwitch.exe";

    private static readonly Mode ModeClone = new Mode(0, "Clone", "/clone");
    private static readonly Mode ModeExtend = new Mode(1, "Extend", "/extend");
    private static readonly Mode ModePrimary = new Mode(2, "Primary", "/internal");
    private static readonly Mode ModeCloneSecondary = new Mode(3, "Secondary", "/external");

    private static readonly List<Mode> Modes = new List<Mode>
    {
      ModeClone,
      ModeExtend,
      ModePrimary,
      ModeCloneSecondary
    };

    private readonly List<Mode> _singleClickModes = new List<Mode> {ModeClone, ModeExtend};
    
    private int _lastSelectedSingleClickIndex;
    
    private int _lastSelectedModeIndex;
    
    public Form1()
    {
      InitializeComponent();
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

    private void Form1_Load(object sender, EventArgs e)
    {
      SetupContextMenu();
      Minimize();
    }

    private void SetDefaultNotifyIcon()
    {
      notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
    }

    private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Left)
      {
        //Toggle();
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
        Console.WriteLine("Failed at InvokeRightClick()");
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

    private void menuItem_Click(object sender, EventArgs e)
    {
      _lastSelectedModeIndex = ((MenuItem)sender).Index;
      PerformSwitch(Modes[_lastSelectedModeIndex]);
    }

    private void menuItemExit_Click(object sender, EventArgs e)
    {
      Close();
    }

    private void PerformSwitch(Mode mode)
    {
      try
      {
        RunExe(mode);

        SetIcon(mode.Name);

        notifyIcon.ShowBalloonTip(BalloonTime, "Switched Display Mode", mode.Name, ToolTipIcon.None);
        notifyIcon.Text = mode.Name;

        foreach (MenuItem item in notifyIcon.ContextMenu.MenuItems)
        {
          var i = item.Text.IndexOf(ActiveMarker, StringComparison.Ordinal);
          if (i >= 0)
          {
            item.Text = item.Text.Substring(0, i);
          }
        }
        notifyIcon.ContextMenu.MenuItems[mode.Index].Text += ActiveMarker;

        if (_singleClickModes[_lastSelectedSingleClickIndex] != mode)
        {
          ++_lastSelectedSingleClickIndex;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        notifyIcon.ShowBalloonTip(
          BalloonTime, 
          "Error Switching Display Mode", 
          "Could not switch to mode :  " + mode.Name, 
          ToolTipIcon.Error
          );
      }

    }

    private void Form1_LocationChanged(object sender, EventArgs e)
    {
      if (WindowState == FormWindowState.Minimized)
      {
        Minimize();
      }
    }

    private void Toggle()
    {
      ++_lastSelectedSingleClickIndex;
      if (_lastSelectedSingleClickIndex >= _singleClickModes.Count)
      {
        _lastSelectedSingleClickIndex = 0;
      }

      PerformSwitch(_singleClickModes[_lastSelectedSingleClickIndex]);
    }    

    private void SetIcon(string iconName)
    {
      try
      {
        var icon = new Icon(iconName + ".ico");
        notifyIcon.Icon = icon;
      }
      catch (Exception)
      {
        SetDefaultNotifyIcon();
      }
    }

    private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
    {
      ++_lastSelectedModeIndex;
      if (_lastSelectedModeIndex >= Modes.Count)
      {
        _lastSelectedModeIndex = 0;
      }

      PerformSwitch(Modes[_lastSelectedModeIndex]);
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

    public int Index;
    public string Name;
    public string Command;
  }
}
