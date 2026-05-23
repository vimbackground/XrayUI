using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using XrayUI.Helpers;

namespace XrayUI.Models
{
    public class SubscriptionEntry : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = string.Empty;
        private string _url = string.Empty;
        private DateTimeOffset? _lastUpdated;
        private string? _lastError;
        private bool _isBusy;

        public string Id
        {
            get => _id;
            set { _id = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }

        public DateTimeOffset? LastUpdated
        {
            get => _lastUpdated;
            set
            {
                _lastUpdated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastUpdatedText));
            }
        }

        public string? LastError
        {
            get => _lastError;
            set
            {
                _lastError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(LastErrorText));
            }
        }

        [JsonIgnore]
        public string LastUpdatedText
        {
            get
            {
                if (!_lastUpdated.HasValue) return L.Subscription_NeverUpdated;
                var delta = DateTimeOffset.Now - _lastUpdated.Value;
                string rel;
                if (delta.TotalSeconds < 60)      rel = L.Subscription_JustNow;
                else if (delta.TotalMinutes < 60) rel = Loc.Format("Subscription_MinutesAgo", (int)delta.TotalMinutes);
                else if (delta.TotalHours   < 24) rel = Loc.Format("Subscription_HoursAgo",   (int)delta.TotalHours);
                else if (delta.TotalDays    < 30) rel = Loc.Format("Subscription_DaysAgo",    (int)delta.TotalDays);
                else                              rel = _lastUpdated.Value.LocalDateTime.ToString("yyyy-MM-dd");
                return Loc.Format("Subscription_LastUpdated", rel);
            }
        }

        [JsonIgnore]
        public string LastErrorText => _lastError ?? string.Empty;

        [JsonIgnore]
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        [JsonIgnore] public bool IsNotBusy => !_isBusy;
        [JsonIgnore] public bool HasError  => !string.IsNullOrEmpty(_lastError);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
