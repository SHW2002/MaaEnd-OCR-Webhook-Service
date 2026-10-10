using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class DatasetBrowser
{
    public int FrameCount => _manifest?.Frames.Count ?? 0;
    public string AnnotationSummary
    {
        get
        {
            if (_manifest == null || _store == null) return "未载入数据集";
            if (!File.Exists(_store.AnnotationsPath)) return "未找到 annotations.json";
            var truth = _manifest.Frames.Select(frame => _annotations.Frames.GetValueOrDefault(frame.Index)).ToList();
            return $"标注已读取：{truth.Count(x => x?.Lines.Count > 0):N0} 帧有文字，{truth.Count(x => x?.Reviewed == true):N0} 帧已确认";
        }
    }

    private void DisplayAnnotation()
    {
        if (_manifest == null || _index < 0) return;
        var frame = _manifest.Frames[_index];
        var annotation = _annotations.Frames.GetValueOrDefault(frame.Index) ?? new FrameAnnotation();
        _loading = true;
        try
        {
            _lines.Rows.Clear();
            foreach (var line in annotation.Lines) _lines.Rows.Add(line.TimeText, line.Content);
            _lines.AutoResizeRows();
            _reviewed.Checked = annotation.Reviewed;
            _anchor.Checked = annotation.AnchorVisible;
            _scenario.Text = annotation.Scenario;
            var state = annotation.Reviewed ? "已确认" : "待核对";
            var text = annotation.Lines.Count > 0 ? $"真值 {annotation.Lines.Count} 行 · {state}"
                : annotation.Reviewed ? "真值为空 · 已确认" : "未填写文字真值";
            _info.Text = $"当前帧 {_index + 1}/{_manifest.Frames.Count} · {frame.OffsetMs / 1000:F2}s · {text}";
            UpdateAnnotationNavigation();
        }
        finally { _loading = false; _dirty = false; }
    }

    private void ReloadWithFeedback()
    {
        try { ReloadAnnotations(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "重载标注失败"); }
    }
}
