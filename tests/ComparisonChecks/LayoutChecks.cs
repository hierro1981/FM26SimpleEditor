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
        tabs.TabPages[0].Text = "3  Anteprima nel gioco";
        tabs.TabPages[1].Text = "Migliori per posizione";
        tabs.TabPages.Add(new TabPage("6  La mia squadra"));
        tabs.Width = 350; Application.DoEvents();
        check(tabs.Multiline && tabs.TabPages.Cast<TabPage>().Select((p, i) => tabs.GetTabRect(i).Width >= TextRenderer.MeasureText(p.Text, tabs.Font).Width + 10).All(x => x),
            "tab widths follow full captions after changes and support multiple rows");
        using (var pitch = new SavedTacticPitch { Dock = DockStyle.None, Size = new Size(800, 400) })
        {
            var positions = new[] { "WBL", "WBR", "DC", "DC", "DC", "GK" };
            var sides = new[] { "left", "right", "left", "centre", "right", "centre" };
            pitch.SetTactic(positions.Select((pos, i) => new SavedTacticSlot { Number = i, InPossession = new SavedTacticPhase { Position = pos, Side = sides[i] } }).ToArray(), "ip", -1);
            using var field = new Bitmap(800, 400); pitch.DrawToBitmap(field, pitch.ClientRectangle);
            var hits = (Dictionary<int, RectangleF>)typeof(SavedTacticPitch).GetField("hits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(pitch)!;
            float Center(int i) => hits[i].Left + hits[i].Width / 2;
            check(Center(0) < 800 * .2 && Center(1) > 800 * .8 && Math.Abs(Center(5) - 400) < 2,
                "isolated wingbacks stay on the flanks while the goalkeeper remains central");
            check(Center(2) < Center(3) && Center(3) < Center(4) && !hits[2].IntersectsWith(hits[3]) && !hits[3].IntersectsWith(hits[4]),
                "three central defenders keep their actual left centre right lanes without overlap");
        }
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
