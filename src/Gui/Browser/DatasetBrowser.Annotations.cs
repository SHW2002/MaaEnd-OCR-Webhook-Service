using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class DatasetBrowser
{
    public void SaveCurrent()
    {
        if (_loading || _store == null || _manifest == null || _index < 0) return;
        _lines.EndEdit();
        if (!_dirty) return;
        var lines = new List<TruthLine>();
        foreach (DataGridViewRow row in _lines.Rows)
        {
            if (row.IsNewRow) continue;
            var time = row.Cells[0].Value?.ToString() ?? "";
            var content = row.Cells[1].Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(time)) continue;
            if (string.IsNullOrWhiteSpace(content)) throw new InvalidDataException("日志正文不能为空，请删除空行或补全内容");
            lines.Add(new(time, content));
        }
        var latest = _store.LoadAnnotations();
        latest.Frames[_manifest.Frames[_index].Index] = new FrameAnnotation
        { Reviewed = _reviewed.Checked, AnchorVisible = _anchor.Checked, Scenario = _scenario.Text, Lines = lines };
        _store.SaveAnnotations(latest);
        _annotations = latest;
        _dirty = false;
        AnnotationsChanged?.Invoke();
    }
    private void SaveWithFeedback()
    {
        try { SaveCurrent(); DisplayAnnotation(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "保存失败"); }
    }
    private async void GenerateDraft()
    {
        if (_manifest == null || _store == null || _index < 0) return;
        Pause();
        var manifest = _manifest; var store = _store; var index = _index;
        Enabled = false;
        try
        {
            var result = await Task.Run(async () =>
            {
                using var bitmap = new Bitmap(store.FramePath(manifest.Frames[index]));
                return await new FrameRecognitionPipeline(manifest.Recognition).ProcessAsync(bitmap, CancellationToken.None);
            });
            if (!result.AnchorFound) throw new InvalidOperationException("草稿未找到锚点，请直接根据截图人工标注");
            _lines.Rows.Clear();
            foreach (var line in result.Lines) _lines.Rows.Add(line.TimeText, line.Content);
            _reviewed.Checked = false;
            _dirty = true;
            _info.Text = "OCR 草稿已填入，请人工核对后勾选确认";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "生成草稿失败"); }
        finally { Enabled = true; }
    }
}
