using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class DatasetBrowser
{
    private readonly ComboBox _groups = new() { DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 14,
        Margin = new Padding(0, 3, 6, 3), ForeColor = Theme.Text, DropDownWidth = 800 };
    private List<AnnotationGroup> _annotationGroups = [];
    private Button _previousGroup = null!;
    private Button _nextGroup = null!;
    private Button _overview = null!;
    private bool _selectingGroup;
    public int AnnotationTextCount { get; private set; }

    private Control CreateAnnotationNavigation()
    {
        _previousGroup = Ui.Button("上一组", () => MoveGroup(-1));
        _nextGroup = Ui.Button("下一组", () => MoveGroup(1));
        _overview = Ui.Button("全部标注", ShowAnnotationOverview);
        _previousGroup.Enabled = false; _nextGroup.Enabled = false; _overview.Enabled = false;
        _groups.SelectedIndexChanged += (_, _) =>
        {
            if (_selectingGroup || _groups.SelectedItem is not AnnotationGroup group) return;
            Pause(); _frame.Value = group.StartPosition;
        };
        var navigation = UiLayout.Field("标注变化", _groups, _previousGroup, _nextGroup, _overview);
        navigation.Padding = new Padding(0, 0, 0, 6);
        return navigation;
    }

    private void RebuildAnnotationNavigation()
    {
        if (_manifest == null) return;
        _annotationGroups = AnnotationTimeline.Build(_manifest, _annotations);
        AnnotationTextCount = AnnotationCatalog.Build(_manifest, _annotations).Count;
        _selectingGroup = true;
        _groups.BeginUpdate();
        try
        {
            _groups.Items.Clear();
            _groups.Items.AddRange(_annotationGroups.Cast<object>().ToArray());
            _overview.Enabled = AnnotationTextCount > 0;
        }
        finally { _groups.EndUpdate(); _selectingGroup = false; }
        UpdateAnnotationNavigation();
    }

    private void UpdateAnnotationNavigation()
    {
        var index = _annotationGroups.FindIndex(group => group.StartPosition <= _index && _index <= group.EndPosition);
        _selectingGroup = true;
        try { _groups.SelectedIndex = index; }
        finally { _selectingGroup = false; }
        _previousGroup.Enabled = index > 0;
        _nextGroup.Enabled = index >= 0 && index + 1 < _annotationGroups.Count;
    }

    private void MoveGroup(int offset)
    {
        var target = _groups.SelectedIndex + offset;
        if (target < 0 || target >= _annotationGroups.Count) return;
        Pause(); _frame.Value = _annotationGroups[target].StartPosition;
    }

    private void ShowAnnotationOverview()
    {
        if (_manifest == null) return;
        try
        {
            Pause(); SaveCurrent();
            using var overview = new AnnotationOverviewForm(AnnotationCatalog.Build(_manifest, _annotations));
            if (overview.ShowDialog(this) == DialogResult.OK) _frame.Value = overview.FramePosition;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "打开标注总览失败"); }
    }
}
