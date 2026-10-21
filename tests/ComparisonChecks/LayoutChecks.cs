using FM26SimpleEditor;

internal static class LayoutChecks
{
    public static void Run(Action<bool, string> check)
    {
        using var host = new Form { ClientSize = new Size(650, 350) };
        using var tabs = new AccentTabControl { Dock = DockStyle.Fill };
        tabs.TabPages.AddRange([new TabPage("First tab"), new TabPage("Second tab")]);
        host.Controls.Add(tabs); host.Show(); Application.DoEvents();
        using var bitmap = new Bitmap(tabs.Width, tabs.Height); tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
        if (!SystemInformation.HighContrast)
        {
            int Count(Rectangle rect, Color color)
            {
                int count = 0;
                for (int y = rect.Top; y < rect.Bottom; y++) for (int x = rect.Left; x < rect.Right; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb()) count++;
                return count;
            }
            check(Count(tabs.GetTabRect(0), Color.FromArgb(31, 91, 153)) > 30 &&
                  Count(tabs.GetTabRect(1), Color.FromArgb(224, 235, 247)) > 30,
                "selected and inactive tab headers render distinct accessible colours");
        }
        tabs.SelectedIndex = 1; Application.DoEvents();
        check(tabs.SelectedTab == tabs.TabPages[1] && tabs.DisplayRectangle.Height > 0, "coloured headers retain native tab selection and content layout");
        host.Close();
    }
}
