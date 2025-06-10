using LocalizationTabii.ComponentModel;

#if MACCATALYST || IOS
using Foundation;
using UIKit;
using UniformTypeIdentifiers;
#endif

namespace LocalizationTabii.Components
{
    public partial class FileDragDropComponent : ContentView
    {
        public FileDragDropViewModel ViewModel { get; }

        public event EventHandler<FileSelectedEventArgs>? FileSelected;

#if MACCATALYST || IOS
        private UIDropInteraction? _dropInteraction;
        private DropDelegate? _dropDelegate;
#endif

        public FileDragDropComponent()
        {
            InitializeComponent();
            ViewModel = new FileDragDropViewModel();
            BindingContext = ViewModel;
            
            ViewModel.FileSelected += OnFileSelected;

#if MACCATALYST || IOS
            // Native macOS drop interaction setup
            this.HandlerChanged += OnHandlerChanged;
#endif
        }

#if MACCATALYST || IOS
        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            // Main thread'de çalıştığımızdan emin ol
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (this.Handler?.PlatformView is UIView nativeView)
                    {
                        // Önceki interaction'ı temizle
                        if (_dropInteraction != null)
                        {
                            nativeView.RemoveInteraction(_dropInteraction);
                            _dropInteraction = null;
                        }

                        // Yeni interaction oluştur
                        _dropDelegate = new DropDelegate(this);
                        _dropInteraction = new UIDropInteraction(_dropDelegate);
                        nativeView.AddInteraction(_dropInteraction);
                    }
                }
                catch (Exception ex)
                {
                    // Hata durumunda sadece debug log yaz, kullanıcıya gösterme
                    System.Diagnostics.Debug.WriteLine($"Drop interaction setup hatası: {ex.Message}");
                }
            });
        }

        private class DropDelegate : UIDropInteractionDelegate
        {
            private readonly FileDragDropComponent _parent;

            public DropDelegate(FileDragDropComponent parent)
            {
                _parent = parent;
            }

            public override UIDropProposal SessionDidUpdate(UIDropInteraction interaction, IUIDropSession session)
            {
                return new UIDropProposal(UIDropOperation.Copy);
            }

            public override void PerformDrop(UIDropInteraction interaction, IUIDropSession session)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        foreach (UIDragItem item in session.Items)
                        {
                            var provider = item.ItemProvider;

                            // Öncelikli UTI tiplerini belirle (önerilen yaklaşım)
                            var uti = provider.RegisteredTypeIdentifiers
                                              .FirstOrDefault(t => t == "public.file-url" ||
                                                                   t == "com.apple.finder.node");

                            if (uti == null) 
                            {
                                continue; // Bu item'da kullanışlı bir şey yok
                            }

                            try
                            {
                                // LoadInPlaceFileRepresentationAsync kullan (önerilen API)
                                var result = await provider.LoadInPlaceFileRepresentationAsync(uti);
                                if (result?.FileUrl == null) continue;

                                var url = result.FileUrl;

                                // Security scope başlat (sandbox için zorunlu)
                                var hasAccess = url.StartAccessingSecurityScopedResource();
                                try
                                {
                                    // Dosyayı sandbox içine kopyala
                                    var destPath = Path.Combine(FileSystem.CacheDirectory,
                                                                Path.GetFileName(url.Path));
                                    File.Copy(url.Path, destPath, overwrite: true);

                                    // ViewModel'e güvenli kopyayı gönder
                                    await _parent.ViewModel.ProcessSelectedFileFromPath(
                                              Path.GetFileName(destPath), destPath);
                                    
                                    return; // Başarılı, döngüden çık
                                }
                                finally
                                {
                                    // Security scope'u temizle (zorunlu)
                                    if (hasAccess) 
                                        url.StopAccessingSecurityScopedResource();
                                }
                            }
                            catch (Exception ex)
                            {
                                // Bu UTI ile başarısız oldu, diğerini dene
                                System.Diagnostics.Debug.WriteLine($"UTI {uti} ile hata: {ex.Message}");
                                continue;
                            }
                        }

                        // Hiçbir item işlenemedi
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Uyarı", 
                                "Dosya drag & drop işlemi başarısız oldu. Lütfen 'Dosya Seç' butonunu kullanın.", "Tamam");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Hata", 
                                $"Drag & drop hatası: {ex.Message}", "Tamam");
                        }
                    }
                });
            }
        }
#endif

        private void OnFileSelected(object? sender, FileSelectedEventArgs e)
        {
            FileSelected?.Invoke(this, e);
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            // Copy operation'ı kabul et - debug alert kaldırıldı
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void OnDrop(object sender, DropEventArgs e)
        {
            // MAUI drop event'i artık sadece fallback olarak kullanılacak
            // Native drop interaction varsa bu çağrılmamalı
        }
    }
} 