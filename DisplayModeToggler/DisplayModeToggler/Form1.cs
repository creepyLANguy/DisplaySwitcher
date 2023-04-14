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

    private static readonly List<Mode> Modes = new List<Mode>
    {
      new Mode(0, "Clone", "/clone"),
      new Mode(1, "Extend", "/extend"),
      new Mode(2, "Primary", "/internal"),
      new Mode(3, "Secondary", "/external")
    };

    public Form1() => 
      InitializeComponent();

    private void Form1_Load(object sender, EventArgs e)
    {
      SetupContextMenu();
      Minimize();
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
        RunExe(mode);

        notifyIcon.ShowBalloonTip(BalloonTime, "Switched Display Mode", mode.Name, ToolTipIcon.None);
        notifyIcon.Text = mode.Name;

        SetActiveMarker(mode);

        SetIcon(mode.Name);
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

    private void SetActiveMarker(Mode mode)
    {
      foreach (MenuItem item in notifyIcon.ContextMenu.MenuItems)
      {
        var i = item.Text.IndexOf(ActiveMarker, StringComparison.Ordinal);
        if (i >= 0)
        {
          item.Text = item.Text.Substring(0, i);
        }
      }
      notifyIcon.ContextMenu.MenuItems[mode.Index].Text += ActiveMarker;
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
        notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
      }
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
