using GiniAccountManagement.Models;
using GiniAccountManagement.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GiniAccountManagement.ViewModels
{
    public class AccountsViewModel : INotifyPropertyChanged
    {
        private static readonly AppConfig _currentConfig = AppConfig.Current;
        private ObservableCollection<Account> _rows = new();
        public ObservableCollection<Account> Rows
        {
            get => _rows;
            set { if (_rows == value) return; _rows = value; OnPropertyChanged(); }
        }
        public ICommand ShowRowMenuCommand { get; }
        public ICommand CopyUidCommand { get; }
        public ICommand CopyPasswordCommand { get; }
        public ICommand CopyMultipleValueCommand { get; }
        public ICommand DeleteCommand { get; }


        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public AccountsViewModel()
        {
            ShowRowMenuCommand = new Command<Account>(async a =>
            {
                if (a is null) return;
                var choice = await App.Current!.MainPage!.DisplayActionSheet(
                    "Menu", "Hủy", null,
                    "Mở profile", "Copy UID", "Copy password", "Copy uid|pass|2fa|email|passmail|cookie|token", "Xóa");

                switch (choice)
                {
                    case "Mở profile":
                        if(_currentConfig.RequireVpn && !VpnHelper.IsVPNConnected())
                        {
                            await App.Current.MainPage.DisplayAlert("Lỗi", "Vui lòng bật VPN trước khi mở profile!, ", "OK");
                            return;
                        }
                        await Task.Run(() => Chrome.OpenOrGetAsync(a));
                        break;

                    case "Copy UID":
                        await Clipboard.SetTextAsync(a.UID ?? "");
                        break;
                    case "Copy password":
                        await Clipboard.SetTextAsync(a.Password ?? "");
                        break;
                    case "Copy uid|pass|2fa|email|passmail|cookie|token":
                        await Clipboard.SetTextAsync($"{a.UID}|{a.Password}|{a.TwoFA}|{a.Email}|{a.EmailPassword}|{a.Cookie}|{a.Token}|");
                        break;

                    case "Xóa":
                        Rows.Remove(a);
                        for (int i = 0; i < Rows.Count; i++) Rows[i].STT = i + 1;
                        await ProfileStorage.SaveAsync(Rows); // lưu lại profiles.json
                        break;
                }
            });
            CopyUidCommand = new Command<Account>(async a =>
            {
                if (a is null) return;
                await Clipboard.SetTextAsync(a.UID ?? "");
            });

            CopyPasswordCommand = new Command<Account>(async a =>
            {
                if (a is null) return;
                await Clipboard.SetTextAsync(a.Password ?? "");
            });

            CopyMultipleValueCommand = new Command<Account>(async a =>
            {
                if (a is null) return;
                await Clipboard.SetTextAsync($"{a.UID}|{a.Password}|{a.TwoFA}|{a.Email}|{a.EmailPassword}|{a.Cookie}|{a.Token}|");
            });

            DeleteCommand = new Command<Account>(async a =>
            {
                if (a is null) return;
                Rows.Remove(a);
                for (int i = 0; i < Rows.Count; i++) Rows[i].STT = i + 1;
                await ProfileStorage.SaveAsync(Rows);
            });
        }
    }
}
