// UseWindowsForms is on (for the NotifyIcon tray icon) alongside UseWPF, so the SDK's implicit
// global usings pull in both System.Windows.Forms and the WPF namespaces project-wide. Several
// common type names exist in both - pin the WPF/Win32 ones as the default everywhere here, since
// WinForms is only actually used inside TrayIconService (which references it fully-qualified).
global using Application = System.Windows.Application;
global using MessageBox = System.Windows.MessageBox;
global using UserControl = System.Windows.Controls.UserControl;
global using Button = System.Windows.Controls.Button;
global using TextBox = System.Windows.Controls.TextBox;
global using DragEventArgs = System.Windows.DragEventArgs;
global using DragDropEffects = System.Windows.DragDropEffects;
global using DataFormats = System.Windows.DataFormats;
global using Orientation = System.Windows.Controls.Orientation;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using Color = System.Windows.Media.Color;
global using Brushes = System.Windows.Media.Brushes;
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
global using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
