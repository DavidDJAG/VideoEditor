# Designer integrity checklist

Run this checklist before merge and before release:

1. Open `MainWindow.xaml` in the XAML designer and confirm no exceptions are shown.
2. Open `Views/DashboardView.xaml` in the XAML designer and confirm no exceptions are shown.
3. Confirm design-time data contexts resolve correctly:
   - `MainWindowDesignViewModel`
   - `DashboardDesignViewModel`
4. Confirm all merged theme dictionaries load without designer errors:
   - `Themes/Colors.xaml`
   - `Themes/Typography.xaml`
   - `Themes/Spacing.xaml`
   - `Themes/Controls.xaml`
   - `Themes/Strings.xaml`
5. Confirm no binding errors in the designer Output panel.
6. Confirm custom controls and converters (if added later) load in the designer process.

If any designer exception occurs, log the stack trace in the release notes and block the release until fixed.
