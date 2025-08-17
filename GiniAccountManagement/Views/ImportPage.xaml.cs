using GiniAccountManagement.Models;

namespace GiniAccountManagement.Views;

public partial class ImportPage : ContentPage
{
    private readonly TaskCompletionSource<List<Account>?> _tcs;
    public ImportPage(TaskCompletionSource<List<Account>?> tcs)
    {

        InitializeComponent();
        _tcs = tcs;
    }
    // API tiện dụng: mở modal và nhận kết quả
    public static async Task<List<Account>?> ShowAsync(Page host)
    {
        var tcs = new TaskCompletionSource<List<Account>?>();
        var page = new ImportPage(tcs);

        // bọc NavigationPage để có tiêu đề trên modal
        await host.Navigation.PushModalAsync(new NavigationPage(page));
        return await tcs.Task;
    }
    private async void OnCancel(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    private async void OnConfirm(object? sender, EventArgs e)
    {
        try
        {
            var text = EditorBox.Text ?? string.Empty;
            var list = ImportParser.ParseAccounts(text);
            // LƯU ra profiles.json
            await ProfileStorage.SaveAsync(list);
            _tcs.TrySetResult(list);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi parse", ex.Message, "OK");
            return;
        }
        await Navigation.PopModalAsync();
    }
}


