using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GtaAutoGameplay.Core.Targeting;
using GtaAutoGameplay.Platform.Windows.Windowing;

namespace GtaAutoGameplay.App;

public partial class MainWindow : Window
{
    private readonly WindowsWindowDiscovery _windowDiscovery = new();
    private readonly ObservableCollection<WindowCandidate> _candidates = [];
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
        CandidateList.ItemsSource = _candidates;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        ClearCurrentSelection();
        CandidateList.SelectedItem = null;
        _candidates.Clear();
        ScanStatusText.Text = "正在扫描可见顶层窗口…";

        try
        {
            WindowDiscoveryResult result = await _windowDiscovery.DiscoverAsync(
                _lifetimeCancellation.Token);
            if (!result.IsSuccess)
            {
                ScanStatusText.Text = DescribeDiscoveryFailure(result.Failure!.Value);
                return;
            }

            foreach (WindowCandidate candidate in result.Candidates)
            {
                _candidates.Add(candidate);
            }

            ScanStatusText.Text = _candidates.Count == 0
                ? "未发现可供选择的可见顶层窗口。"
                : $"发现 {_candidates.Count} 个候选窗口。请选择一项并明确点击“选择”。";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (CandidateList.SelectedItem is not WindowCandidate candidate)
        {
            return;
        }

        SetBusy(true);
        try
        {
            WindowSelectionResult result = await _windowDiscovery.SelectCandidateAsync(
                candidate.CandidateId,
                DateTimeOffset.UtcNow,
                _lifetimeCancellation.Token);
            if (!result.IsSuccess)
            {
                ClearCurrentSelection();
                ScanStatusText.Text = DescribeSelectionFailure(result.Failure!.Value);
                if (result.Failure is WindowSelectionFailure.CandidateExpired or
                    WindowSelectionFailure.CandidateNotFound or
                    WindowSelectionFailure.CandidateUnavailable)
                {
                    _candidates.Remove(candidate);
                    CandidateList.SelectedItem = null;
                }

                return;
            }

            CurrentSelectionText.Text =
                $"{candidate.Title} — {candidate.ProcessName} (PID {candidate.ProcessId})";
            CancelSelectionButton.IsEnabled = true;
            ScanStatusText.Text = "窗口已明确选择。捕获、自动控制和输入仍保持关闭。";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CancelSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        ClearCurrentSelection();
        CandidateList.SelectedItem = null;
        ScanStatusText.Text = "选择已取消。当前没有活动窗口选择。";
    }

    private void CandidateList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e) =>
        UpdateButtonStates();

    private void Window_Closed(object? sender, EventArgs e)
    {
        _lifetimeCancellation.Cancel();
        _windowDiscovery.CancelSelection();
        _lifetimeCancellation.Dispose();
    }

    private void ClearCurrentSelection()
    {
        _windowDiscovery.CancelSelection();
        CurrentSelectionText.Text = "未选择窗口";
        CancelSelectionButton.IsEnabled = false;
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        RefreshButton.IsEnabled = !isBusy;
        CandidateList.IsEnabled = !isBusy;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        SelectButton.IsEnabled =
            !_isBusy && CandidateList.SelectedItem is WindowCandidate;
        CancelSelectionButton.IsEnabled =
            !_isBusy && _windowDiscovery.CurrentSelection is not null;
    }

    private static string DescribeDiscoveryFailure(WindowDiscoveryFailure failure) =>
        failure switch
        {
            WindowDiscoveryFailure.Cancelled => "窗口扫描已取消。",
            WindowDiscoveryFailure.AccessDenied =>
                "无法读取候选窗口的必要进程信息。高权限目标不会自动提权。",
            WindowDiscoveryFailure.IncompleteMetadata =>
                "候选窗口的身份信息不完整，请刷新后重试。",
            WindowDiscoveryFailure.EnumerationFailed => "Windows 顶层窗口枚举失败。",
            WindowDiscoveryFailure.Unavailable => "窗口发现服务当前不可用。",
            _ => "窗口扫描失败。",
        };

    private static string DescribeSelectionFailure(WindowSelectionFailure failure) =>
        failure switch
        {
            WindowSelectionFailure.Cancelled => "窗口选择已取消。",
            WindowSelectionFailure.CandidateExpired => "候选窗口已经过期，请刷新列表。",
            WindowSelectionFailure.CandidateNotFound => "候选窗口不属于当前扫描批次，请刷新列表。",
            WindowSelectionFailure.CandidateUnavailable => "候选窗口已经关闭或身份发生变化，请刷新列表。",
            _ => "无法选择该窗口。",
        };
}
