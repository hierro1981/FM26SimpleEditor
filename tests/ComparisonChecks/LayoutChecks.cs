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
        using var viewport = new ContentViewport(new Panel(), new Size(560, 350)) { Dock = DockStyle.None, Size = new Size(300, 200) };
        host.Controls.Add(viewport); viewport.BringToFront(); viewport.CreateControl(); Application.DoEvents();
        check(viewport.HorizontalScroll.Visible && viewport.VerticalScroll.Visible, "minimum content dimensions enable both scrollbars");
        viewport.AutoScrollPosition = new Point(560, 350);
        viewport.Size = new Size(900, 700); Application.DoEvents();
        check(!viewport.HorizontalScroll.Visible && !viewport.VerticalScroll.Visible && viewport.Controls[0].Location == Point.Empty &&
              viewport.Controls[0].Size == viewport.ClientSize, "expanding a scrolled viewport removes both bars and fills the recovered space");
        host.Close();
    }
}
