using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class EvaluationPage
{
    private void Browse()
    {
        try
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "录制的数据集|dataset.json", Title = "选择录制的数据集",
                InitialDirectory = DatasetPaths.BrowseDirectory(_path.Text),
                FileName = "dataset.json", CheckFileExists = true, RestoreDirectory = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK) LoadDataset(dialog.FileName);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "选择数据集失败"); }
    }

    private void OpenDataset()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_path.Text)) { Browse(); return; }
            var path = DatasetPaths.Resolve(_path.Text);
            if (Directory.Exists(path) && !File.Exists(Path.Combine(path, "dataset.json")))
            { Browse(); return; }
            LoadDataset(path);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "打开数据集失败"); }
    }

    private void OpenDirectory()
    {
        try { Ui.OpenFolder(DatasetPaths.BrowseDirectory(_path.Text)); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "打开目录失败"); }
    }
}
