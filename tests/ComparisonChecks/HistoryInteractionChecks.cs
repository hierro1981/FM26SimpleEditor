using System.Drawing.Drawing2D;
using FM26SimpleEditor;

internal static class HistoryInteractionChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(All(x)));
    public static void Run(Action<bool, string> check)
    {
        L10n.SetLanguage("it");
        var history = new HistoryResult
        {
            EmbeddedAvailable = true,
            EmbeddedPoints = [
                new() { Sequence = 1, Source = "save.fm", Attributes = new() { ["passing"] = 50, ["technique"] = 70, ["reflexes"] = 80, ["injury_proneness"] = 50 } },
                new() { Sequence = 2, Source = "save.fm", Attributes = new() { ["passing"] = 60, ["technique"] = 60, ["reflexes"] = 80, ["injury_proneness"] = 40 } },
                new() { Sequence = 3, Source = "save.fm", Attributes = new() { ["technique"] = 60 } },
                new() { Sequence = 4, Source = "save.fm", Attributes = new() { ["passing"] = 70, ["technique"] = 60 } }],
            Points = [new() { Source = "save.fm", Date = "2028-02-10", Attributes = new() { ["passing"] = 70 } }]
        };
        var midfielder = new PlayerDetail { Player = new() { Uid = 50, Name = "Midfielder" }, Positions = [new() { Key = "MC", Value = 20 }] };
        using var host = new Form { Size = new(1050,800) };
        using var panel = new HistoryPanel(); host.Controls.Add(panel); host.Show();
        panel.SetContext(midfielder,true,(_,_) => Task.FromResult(history)); Application.DoEvents();
        var list = All(panel).OfType<CheckedListBox>().Single();
        var grid = All(panel).OfType<DataGridView>().Single();
        HashSet<string> Keys() => grid.Columns.Cast<DataGridViewColumn>().Select(c => c.Tag as string).Where(t => t is not null && !t.StartsWith("delta:")).Select(t => t!).ToHashSet();
        check(Keys().SetEquals(new[] {"passing","technique"}), "main midfield position defaults to available essential and important attributes");
        var preset = All(panel).OfType<ComboBox>().Single(c => c.Name == "HistoryRolePreset");
        var apply = All(panel).OfType<Button>().Single(c => c.Text == L10n.Get("HistoryApplyPreset"));
        preset.SelectedIndex = 1; apply.PerformClick(); Application.DoEvents();
        check(Keys().SetEquals(new[] {"reflexes"}), "goalkeeper preset replaces selection with available keeper attributes");
        list.SetItemChecked(0,true); Application.DoEvents();
        var manual = Keys();
        panel.SetContext(new PlayerDetail { Player = midfielder.Player, Positions = midfielder.Positions }, true, (_,_) => Task.FromResult(history)); Application.DoEvents();
        check(Keys().SetEquals(manual), "reloading same UID preserves manual selection");
        L10n.SetLanguage("en"); panel.SetLanguage(false); Application.DoEvents();
        check(Keys().SetEquals(manual) && apply.Text == "Apply position preset" && preset.Items[0]!.ToString()!.Contains("Main position"), "language switch preserves selection and translates presets");
        panel.SetContext(new PlayerDetail { Player = new() { Uid = 51 }, Positions = [new() { Key = "GK", Value = 20 }] },false,(_,_)=>Task.FromResult(history)); Application.DoEvents();
        check(Keys().SetEquals(new[] {"reflexes"}) && preset.SelectedIndex == 0, "new player resets to its own main position");
        foreach (int index in Enumerable.Range(0,list.Items.Count)) list.SetItemChecked(index,true);
        Application.DoEvents();
        DataGridViewColumn Delta(string key) => grid.Columns.Cast<DataGridViewColumn>().Single(c => Equals(c.Tag,"delta:"+key));
        var up = grid.Rows[1].Cells[Delta("passing").Index];
        var down = grid.Rows[1].Cells[Delta("technique").Index];
        var flat = grid.Rows[1].Cells[Delta("reflexes").Index];
        var inverse = grid.Rows[1].Cells[Delta("injury_proneness").Index];
        check(up.Style.ForeColor.G > up.Style.ForeColor.R && down.Style.ForeColor.R > down.Style.ForeColor.G, "delta colors distinguish improvement and deterioration");
        check(flat.Style.ForeColor == Color.DimGray && grid.Rows[0].Cells[Delta("passing").Index].Value is null, "zero and unavailable deltas are neutral");
        check(inverse.Style.ForeColor.G > inverse.Style.ForeColor.R && inverse.ToolTipText.Contains("lower"), "lower injury proneness is correctly an improvement");
        check(grid.Rows[3].Cells[Delta("passing").Index].Value is null, "missing intermediate attribute does not produce a previous-period delta");
        check(up.FormattedValue!.ToString()!.StartsWith('+'), "positive delta includes an explicit plus sign");
        using var chart = new AttributeHistoryChart { Size = new(600,300) };
        chart.SetSeries([
            new("passing","Passing",Color.Blue,DashStyle.Solid,[new(1,10,"January","a.fm",false),new(3,14,"March","a.fm",false)]),
            new("technique","Technique",Color.Green,DashStyle.Dash,[new(1,12,"January","a.fm",false),new(1,13,"January","b.fm",false)])],20,false);
        string tooltip = chart.PeriodDetails(1.4);
        check(tooltip.Contains("Passing: 10") && tooltip.Contains("Technique: 12") && tooltip.Contains("Technique: 13") && tooltip.Contains("b.fm"), "nearest-period tooltip includes all attributes and duplicate-date sources");
        check(!chart.PeriodDetails(2.9).Contains("Technique"), "tooltip does not interpolate missing observations");
        using var bitmap = new Bitmap(600,300); chart.DrawToBitmap(bitmap,chart.ClientRectangle);
        typeof(AttributeHistoryChart).GetMethod("OnMouseMove",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(chart,[new MouseEventArgs(MouseButtons.None,0,45,140,0)]);
        var tip = (ToolTip)typeof(AttributeHistoryChart).GetField("tip",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(chart)!;
        check(tip.GetToolTip(chart).Contains("Technique: 13"), "hover works anywhere in plot height instead of tiny point targets");
        chart.SetSeries([],20,false); check(chart.PeriodDetails(0) == "" && tip.GetToolTip(chart) == "", "replacing chart clears obsolete tooltip");
        host.Close(); L10n.SetLanguage("it");
    }
}
