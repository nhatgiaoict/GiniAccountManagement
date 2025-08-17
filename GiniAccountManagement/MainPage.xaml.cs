using GiniAccountManagement.Models;
using GiniAccountManagement.Service;
using GiniAccountManagement.ViewModels;
using GiniAccountManagement.Views;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace GiniAccountManagement;

public partial class MainPage : ContentPage
{
    public AccountsViewModel VM { get; } = new();
    
    public MainPage()
    {
        InitializeComponent();
        BindingContext = VM;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFromFileAsync();
    }
    private async Task LoadFromFileAsync()
    {
        var list = await ProfileStorage.LoadAsync();
        if (list.Count == 0) return;

        VM.Rows = new ObservableCollection<Account>(list);
        VM.Rows.Clear();

        foreach (var acc in list) VM.Rows.Add(acc);
        ReindexStt();
    }
    private void ReindexStt()
    {
        for (int i = 0; i < VM.Rows.Count; i++)
            VM.Rows[i].STT = i + 1;
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        // mở form nhập (modal) và đợi kết quả
        var imported = await ImportPage.ShowAsync(this);

        if (imported is null || imported.Count == 0)
            return;

        // Cập nhật UI từ kết quả import (đã được lưu file ở ImportPage)
        VM.Rows = new ObservableCollection<Account>(imported);
        VM.Rows.Clear();

        foreach (var acc in imported) VM.Rows.Add(acc);
        ReindexStt();
    }
    private async void OnReloadClicked(object sender, EventArgs e)
    {
        await LoadFromFileAsync();
    }
}