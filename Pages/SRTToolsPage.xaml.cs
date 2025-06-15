using Microsoft.Maui.Controls;
using LocalizationTabii.PageModels;
using LocalizationTabii.Components;
using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Pages
{
    public partial class SRTToolsPage : ContentPage
    {
        private readonly SRTToolsPageModel _viewModel;
        private ProcessingSRTComponent? _processingSRTComponent;
        private SRTResultComponent? _srtResultComponent;
        
        public SRTToolsPage(SRTToolsPageModel model)
        {
            InitializeComponent();
            _viewModel = model;
            BindingContext = _viewModel;
            
            if (FileDropComponent != null)
            {
                FileDropComponent.FileSelected += OnFileSelected;
                
                // FileDragDropComponent'in ViewModel'ine erişim
                if (FileDropComponent.BindingContext is FileDragDropViewModel fileDragViewModel)
                {
                    fileDragViewModel.ContinueRequested += OnContinueRequested;
                }
            }
            
            if (ChooseSRTOperationComponent != null)
            {
                // ChooseSRTOperationComponent'in ViewModel'ine erişim
                if (ChooseSRTOperationComponent.BindingContext is ChooseSRTOperationViewModel chooseSRTOperationViewModel)
                {
                    chooseSRTOperationViewModel.GoBackRequested += OnGoBackRequested;
                    chooseSRTOperationViewModel.OperationSelected += OnOperationSelected;
                }
            }
        }

        private async void OnFileSelected(object? sender, FileSelectedEventArgs e)
        {
            try
            {
                _viewModel.HandleFileSelected(e.FileResult);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Dosya işleme hatası: {ex.Message}", "Tamam");
            }
        }

        private void OnContinueRequested(object? sender, ContinueEventArgs e)
        {
            // FileDragDropComponent'i gizle, ChooseSRTOperationComponent'i göster
            FileDropComponent.IsVisible = false;
            ChooseSRTOperationComponent.IsVisible = true;
            
            // Dosya bilgilerini ChooseSRTOperationComponent'e aktar
            ChooseSRTOperationComponent.SetFileInfo(e.FileName, e.FileSize);
        }

        private void OnGoBackRequested(object? sender, EventArgs e)
        {
            // ChooseSRTOperationComponent'i gizle, FileDragDropComponent'i göster
            ChooseSRTOperationComponent.IsVisible = false;
            FileDropComponent.IsVisible = true;
        }

        private async void OnOperationSelected(object? sender, SRTOperationSelectedEventArgs e)
        {
            // Processing sayfasını göster
            await ShowProcessingPage(e.OperationName, e.FileName);
        }

        private async Task ShowProcessingPage(string operationName, string fileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 ShowProcessingPage başladı: " + operationName + " - " + fileName);
                
                // Main thread üzerinde UI operasyonlarını güvenli şekilde yap
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Mevcut komponentleri güvenli şekilde gizle ve deaktif et
                    FileDropComponent.IsVisible = false;
                    FileDropComponent.IsEnabled = false;
                    
                    ChooseSRTOperationComponent.IsVisible = false;
                    ChooseSRTOperationComponent.IsEnabled = false;
                    
                    System.Diagnostics.Debug.WriteLine("🔧 Mevcut komponentler gizlendi ve deaktif edildi");
                    
                    // Processing komponentini oluştur ve göster
                    var processingViewModel = new ProcessingSRTViewModel();
                    processingViewModel.Initialize(operationName, fileName);
                    
                    System.Diagnostics.Debug.WriteLine("🔧 ProcessingSRTViewModel oluşturuldu ve initialize edildi");
                    
                    // Event handler'ları bağla
                    processingViewModel.ProcessingCompleted += OnProcessingCompleted;
                    processingViewModel.ProcessingCancelled += OnProcessingCancelled;
                    
                    _processingSRTComponent = new ProcessingSRTComponent(processingViewModel);
                    
                    System.Diagnostics.Debug.WriteLine("🔧 ProcessingSRTComponent oluşturuldu");
                    
                    // Grid'e processing komponentini ekle
                    var mainGrid = Content as Grid;
                    if (mainGrid != null)
                    {
                        System.Diagnostics.Debug.WriteLine("🔧 Grid bulundu, çocuk sayısı: " + mainGrid.Children.Count);
                        
                        // Önce diğer komponentleri tamamen kaldır (focus problemlerini önlemek için)
                        foreach (var child in mainGrid.Children.ToList())
                        {
                            if (child is View view)
                            {
                                view.IsEnabled = false;
                            }
                        }
                        mainGrid.Children.Clear();
                        
                        Grid.SetRow(_processingSRTComponent, 0);
                        _processingSRTComponent.Margin = new Thickness(20);
                        _processingSRTComponent.VerticalOptions = LayoutOptions.FillAndExpand;
                        _processingSRTComponent.HorizontalOptions = LayoutOptions.FillAndExpand;
                        _processingSRTComponent.IsVisible = true;
                        _processingSRTComponent.IsEnabled = true;
                        
                        mainGrid.Children.Add(_processingSRTComponent);
                        
                        System.Diagnostics.Debug.WriteLine("🔧 Component Grid'e eklendi, yeni çocuk sayısı: " + mainGrid.Children.Count);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Grid bulunamadı!");
                        return;
                    }
                    
                    // UI güncellemesinin tamamlanması için kısa bir bekleme
                    await Task.Delay(50);
                    
                    // İşlemi arka planda güvenli şekilde başlat
                    System.Diagnostics.Debug.WriteLine("🔧 İşlem arka planda başlatılıyor...");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await processingViewModel.StartProcessingAsync();
                            System.Diagnostics.Debug.WriteLine("🔧 İşlem başarıyla tamamlandı");
                        }
                        catch (Exception processingEx)
                        {
                            System.Diagnostics.Debug.WriteLine("❌ İşlem hatası: " + processingEx.Message);
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await DisplayAlert("İşlem Hatası", 
                                    $"SRT dosyası işlenirken hata oluştu: {processingEx.Message}", 
                                    "Tamam");
                            });
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ ShowProcessingPage genel hatası: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("❌ StackTrace: " + ex.StackTrace);
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Hata", "İşlem başlatılamadı: " + ex.Message, "Tamam");
                });
            }
        }

        private void OnProcessingCompleted(object? sender, SRTProcessingCompletedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔔 OnProcessingCompleted çağrıldı");
                    await ShowResultPage(e);
                    System.Diagnostics.Debug.WriteLine("🔔 ShowResultPage tamamlandı");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("❌ OnProcessingCompleted hatası: " + ex.Message);
                    await DisplayAlert("Hata", "Sonuç sayfası gösterilirken hata oluştu: " + ex.Message, "Tamam");
                }
            });
        }

        private void OnProcessingCancelled(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Processing komponentini temizle
                    CleanupProcessingComponent();
                    
                    // Grid'i temizle ve orijinal komponentleri güvenli şekilde geri ekle
                    var mainGrid = Content as Grid;
                    if (mainGrid != null)
                    {
                        // Önce tüm children'ı deaktif et
                        foreach (var child in mainGrid.Children.ToList())
                        {
                            if (child is View view)
                            {
                                view.IsEnabled = false;
                            }
                        }
                        
                        mainGrid.Children.Clear();
                        
                        // FileDropComponent'i geri ekle ve aktif et
                        Grid.SetRow(FileDropComponent, 0);
                        FileDropComponent.IsVisible = true;
                        FileDropComponent.IsEnabled = true;
                        mainGrid.Children.Add(FileDropComponent);
                        
                        // ChooseSRTOperationComponent'i geri ekle (gizli olarak)
                        Grid.SetRow(ChooseSRTOperationComponent, 0);
                        ChooseSRTOperationComponent.IsVisible = false;
                        ChooseSRTOperationComponent.IsEnabled = true;
                        mainGrid.Children.Add(ChooseSRTOperationComponent);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("❌ OnProcessingCancelled hatası: " + ex.Message);
                }
            });
        }

        private async Task ShowResultPage(SRTProcessingCompletedEventArgs completedArgs)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 ShowResultPage başladı");
                
                // Processing komponentini temizle
                CleanupProcessingComponent();
                
                System.Diagnostics.Debug.WriteLine("🔧 Processing component temizlendi");
                
                // Result komponentini oluştur ve göster
                var resultViewModel = new SRTResultViewModel();
            
            // Simülasyon verileri
            var processingDuration = TimeSpan.FromSeconds(5.2); // Örnek süre
            var inputFileSize = "142 KB"; // Örnek boyut
            var outputFileSize = "138 KB"; // Örnek boyut
            var cleanedLinesCount = 3; // Örnek temizlenen satır sayısı
            
            resultViewModel.Initialize(
                completedArgs.OperationName,
                completedArgs.InputFileName,
                completedArgs.OutputFileName,
                completedArgs.Message,
                processingDuration,
                inputFileSize,
                outputFileSize,
                cleanedLinesCount
            );
            
            // Event handler'ları bağla
            resultViewModel.ViewFileRequested += OnViewFileRequested;
            resultViewModel.SaveFileRequested += OnSaveFileRequested;
            resultViewModel.NewOperationRequested += OnNewOperationRequested;
            resultViewModel.GoToHomeRequested += OnGoToHomeRequested;
            
            _srtResultComponent = new SRTResultComponent(resultViewModel);
            
            // Grid'e result komponentini ekle
            var mainGrid = Content as Grid;
            if (mainGrid != null)
            {
                // Önce tüm komponentleri temizle
                mainGrid.Children.Clear();
                
                Grid.SetRow(_srtResultComponent, 0);
                _srtResultComponent.Margin = new Thickness(20);
                _srtResultComponent.VerticalOptions = LayoutOptions.FillAndExpand;
                _srtResultComponent.HorizontalOptions = LayoutOptions.FillAndExpand;
                _srtResultComponent.IsVisible = true;
                mainGrid.Children.Add(_srtResultComponent);
                
                System.Diagnostics.Debug.WriteLine("🔧 Result component Grid'e eklendi");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Grid bulunamadı!");
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ ShowResultPage hatası: " + ex.Message);
                await DisplayAlert("Hata", "ShowResultPage hatası: " + ex.Message, "Tamam");
            }
        }

        private void CleanupProcessingComponent()
        {
            if (_processingSRTComponent != null)
            {
                // Event handler'ları temizle
                if (_processingSRTComponent.BindingContext is ProcessingSRTViewModel processingVM)
                {
                    processingVM.ProcessingCompleted -= OnProcessingCompleted;
                    processingVM.ProcessingCancelled -= OnProcessingCancelled;
                }
                
                // Grid'den kaldır
                var mainGrid = Content as Grid;
                mainGrid?.Children.Remove(_processingSRTComponent);
                _processingSRTComponent = null;
            }
        }

        private void CleanupResultComponent()
        {
            if (_srtResultComponent != null)
            {
                // Event handler'ları temizle
                if (_srtResultComponent.BindingContext is SRTResultViewModel resultVM)
                {
                    resultVM.ViewFileRequested -= OnViewFileRequested;
                    resultVM.SaveFileRequested -= OnSaveFileRequested;
                    resultVM.NewOperationRequested -= OnNewOperationRequested;
                    resultVM.GoToHomeRequested -= OnGoToHomeRequested;
                }
                
                // Grid'den kaldır
                var mainGrid = Content as Grid;
                mainGrid?.Children.Remove(_srtResultComponent);
                _srtResultComponent = null;
            }
        }

        private async void OnViewFileRequested(object? sender, EventArgs e)
        {
            await DisplayAlert("Dosya Görüntüle", "Dosya görüntüleme özelliği yakında eklenecek", "Tamam");
        }

        private async void OnSaveFileRequested(object? sender, EventArgs e)
        {
            await DisplayAlert("Dosya Kaydet", "Dosya kaydetme özelliği yakında eklenecek", "Tamam");
        }

        private void OnNewOperationRequested(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Result komponentini temizle
                    CleanupResultComponent();
                    
                    // Grid'i temizle ve orijinal komponentleri güvenli şekilde geri ekle
                    var mainGrid = Content as Grid;
                    if (mainGrid != null)
                    {
                        // Önce tüm children'ı deaktif et
                        foreach (var child in mainGrid.Children.ToList())
                        {
                            if (child is View view)
                            {
                                view.IsEnabled = false;
                            }
                        }
                        
                        mainGrid.Children.Clear();
                        
                        // FileDropComponent'i geri ekle ve aktif et
                        Grid.SetRow(FileDropComponent, 0);
                        FileDropComponent.IsVisible = true;
                        FileDropComponent.IsEnabled = true;
                        mainGrid.Children.Add(FileDropComponent);
                        
                        // ChooseSRTOperationComponent'i geri ekle (gizli olarak)
                        Grid.SetRow(ChooseSRTOperationComponent, 0);
                        ChooseSRTOperationComponent.IsVisible = false;
                        ChooseSRTOperationComponent.IsEnabled = true;
                        mainGrid.Children.Add(ChooseSRTOperationComponent);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("❌ OnNewOperationRequested hatası: " + ex.Message);
                }
            });
        }

        private async void OnGoToHomeRequested(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Komponentleri temizle
            CleanupProcessingComponent();
            CleanupResultComponent();
            
            if (FileDropComponent != null)
            {
                FileDropComponent.FileSelected -= OnFileSelected;
                
                // FileDragDropComponent'in ViewModel'inden event'leri temizle
                if (FileDropComponent.BindingContext is FileDragDropViewModel fileDragViewModel)
                {
                    fileDragViewModel.ContinueRequested -= OnContinueRequested;
                }
            }
            
            if (ChooseSRTOperationComponent != null)
            {
                // ChooseSRTOperationComponent'in ViewModel'inden event'leri temizle
                if (ChooseSRTOperationComponent.BindingContext is ChooseSRTOperationViewModel chooseSRTOperationViewModel)
                {
                    chooseSRTOperationViewModel.GoBackRequested -= OnGoBackRequested;
                    chooseSRTOperationViewModel.OperationSelected -= OnOperationSelected;
                }
            }
        }
    }
} 