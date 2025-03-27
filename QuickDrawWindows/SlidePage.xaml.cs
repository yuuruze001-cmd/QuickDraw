using CommunityToolkit.Common;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using QuickDraw.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    enum LoadDirection
    {
        Backwards,
        Forwards
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SlidePage : Page
    {

        private const int CACHE_SIZE = 9;
        private const int HALF_CACHE_SIZE = CACHE_SIZE / 2;

        private List<string>? imagePaths = null;

        private readonly LinkedList<CanvasVirtualBitmap> cachedImages = new();

        private LinkedListNode<CanvasVirtualBitmap>? currentImageNode = null;
        private int imageCachePosition = 0;
        private Task? imageLoadTask;

        private bool grayscale = false;

        private readonly object cachedImagesLock = new();
        private readonly object currentImageNodeLock = new();

        private readonly DispatcherQueueTimer? m_SlideTimer = null;

        private uint m_TicksElapsed = 0;

        private TaskCompletionSource<bool> imageCacheFilled = new();

        public SlidePage()
        {
            this.InitializeComponent();

            var settings = (App.Current as App)?.Settings;
            imagePaths = settings?.SlidePaths;

            var timerDurationEnum = settings?.SlideTimerDuration ?? TimerEnum.NoLimit;

            this.Unloaded += SlidePage_Unloaded;

            if (timerDurationEnum != TimerEnum.NoLimit)
            {
                var timerDuration = timerDurationEnum.ToSeconds();
                m_SlideTimer = DispatcherQueue.CreateTimer();
                m_SlideTimer.IsRepeating = true;
                m_SlideTimer.Interval = new(TimeSpan.TicksPerMillisecond * (long)1000);
                m_SlideTimer.Tick += async (sender, e) =>
                {
                    m_TicksElapsed += 1;
                    AppTitleBar.Progress = (double)m_TicksElapsed / (double)timerDuration;
                    if (m_TicksElapsed >= timerDuration)
                    {
                        m_TicksElapsed = 0;

                        await Move(LoadDirection.Forwards);

                        await Task.Delay(100);
                        AppTitleBar.Progress = 0;

                    }
                };
                m_SlideTimer.Start();
            }
        }

        void SlidePage_Unloaded(object sender, RoutedEventArgs e)
        {
            this.m_SlideTimer?.Stop();
            this.SlideCanvas.RemoveFromVisualTree();
            this.SlideCanvas = null;
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            HandleLoadExceptions();
            bool isCurrentImageNull = true;

            lock (currentImageNodeLock)
            {
                isCurrentImageNull = currentImageNode == null;
            }

            if (!isCurrentImageNull)
            {
                CanvasVirtualBitmap? bitmap;

                lock (currentImageNodeLock)
                {
                    bitmap = currentImageNode?.Value;
                }

                if (bitmap != null)
                {

                    Size canvasSize = new(sender.ActualWidth, sender.ActualHeight);
                    double canvasAspect = canvasSize.Width / canvasSize.Height;
                    double bitmapAspect = (bitmap?.Bounds.Width ?? 1.0) / (bitmap?.Bounds.Height ?? 1.0);
                    Size imageRenderSize;
                    Point imagePos;

                    if (bitmapAspect > canvasAspect)
                    {
                        imageRenderSize = new Size(
                            canvasSize.Width,
                            canvasSize.Width / bitmapAspect
                        );
                        imagePos = new Point(0, (canvasSize.Height - imageRenderSize.Height) / 2);
                    }
                    else
                    {
                        imageRenderSize = new Size(
                            canvasSize.Height * bitmapAspect,
                            canvasSize.Height
                        );
                        imagePos = new Point((canvasSize.Width - imageRenderSize.Width) / 2, 0);

                    }

                    CanvasCommandList cl = new(sender);

                    using (CanvasDrawingSession clds = cl.CreateDrawingSession())
                    {
                        clds.DrawImage(bitmap, new Rect(imagePos, imageRenderSize), bitmap?.Bounds ?? new Rect());
                    }

                    if (grayscale)
                    {
                        GrayscaleEffect grayscale = new()
                        {
                            Source = bitmap
                        };
                        args.DrawingSession.DrawImage(grayscale, new Rect(imagePos, imageRenderSize), bitmap?.Bounds ?? new Rect());
                    }
                    else
                    {
                        args.DrawingSession.DrawImage(cl);
                    }
                }
            }
        }

        private void LoadImageInit()
        {
            Debug.Assert(imageLoadTask == null);
            imageLoadTask = FillImageCacheAsync(this.SlideCanvas, imageCachePosition).ContinueWith(_ => {
                SlideCanvas.Invalidate(); 
            });
        }

        private static int Mod (int n, int d)
        {
            int r = n % d;
            return r < 0 ? r+d : r;
        }

        private async Task FillImageCacheAsync(CanvasControl resourceCreator, int index)
        {
            if (imagePaths == null)
            {
                // TODO: Log
                return;
            }
            var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths[index]);

            lock (currentImageNodeLock)
            {
                currentImageNode = cachedImages.AddFirst(bitmap);
            }

            lock (cachedImagesLock)
            {
                imageCachePosition = index;
            }

            var remainingCacheSize = Math.Min(imagePaths.Count, CACHE_SIZE) - 1;
            var numBefore = (remainingCacheSize / 2);
            var numAfter = (remainingCacheSize / 2) + (remainingCacheSize % 2);

            var beforeImages = Enumerable.Range(index - numBefore, numBefore)
                .Reverse()
                .Select(i => Mod(i, imagePaths.Count))
                .ToArray()
                .Select(i =>
                {
                    return imagePaths[i];
                });
            var afterImages = Enumerable.Range(index + 1, numAfter)
                .Select(i => Mod(i, imagePaths.Count))
                .ToArray()
                .Select(i =>
                {
                    return imagePaths[i];
                });

            async Task LoadBeforeAsync()
            {
                foreach (var image in beforeImages)
                {
                    var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, image);
                    lock(cachedImagesLock)
                    {
                        cachedImages.AddFirst(bitmap);
                    }
                }
            }

            async Task LoadAfterAsync()
            {
                foreach (var image in afterImages)
                {
                    var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, image);
                    lock (cachedImagesLock)
                    {
                        cachedImages.AddLast(bitmap);
                    }
                }
            }

            Task loadBeforeTask = Task.Run(LoadBeforeAsync);
            Task loadAfterTask = Task.Run(LoadAfterAsync);
            await loadBeforeTask;
            await loadAfterTask;

            if (!imageCacheFilled.Task.IsCompleted)
            {
                imageCacheFilled.SetResult(true);
            }
        }

        private void SlideCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync().AsAsyncAction());
        }

        private async Task CreateResourcesAsync()
        {
            // Cancel old load
            if (imageLoadTask != null)
            {
                imageLoadTask.AsAsyncAction().Cancel();
                try { await imageLoadTask; } catch { }
                imageLoadTask = null;

            }

            // Unload previously loaded images
           
            lock (currentImageNodeLock)
            {
                currentImageNode = null;
            }

            lock (cachedImagesLock)
            { 
                cachedImages.Clear();
            }

            imageCacheFilled = new();

            LoadImageInit();
        }

        private void HandleLoadExceptions()
        {
            if (imageLoadTask == null || !imageLoadTask.IsCompleted)
                return;

            try
            {
                imageLoadTask.Wait();
            }
            catch(AggregateException aggregateException)
            {
                aggregateException.Handle(exception => { throw exception; });
            }
        }

        private async Task UpdateImageAsync(CanvasControl resourceCreator, LoadDirection direction)
        {
            var increment = direction == LoadDirection.Forwards ? 1 : -1;
            var halfCache = direction == LoadDirection.Forwards ? HALF_CACHE_SIZE : -HALF_CACHE_SIZE;

            var imageIndex = 0;
            lock (cachedImagesLock)
            {
                imageIndex = Mod(imageCachePosition + increment + halfCache, imagePaths?.Count ?? 0);
            }

            var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths?[imageIndex]);

            lock (cachedImagesLock)
            {
                switch (direction)
                {
                    case LoadDirection.Backwards:
                        {
                            cachedImages.AddFirst(bitmap);

                            cachedImages?.Last?.Value.Dispose();
                            cachedImages?.RemoveLast();
                        }
                        break;

                    case LoadDirection.Forwards:
                        {
                            cachedImages.AddLast(bitmap);

                            cachedImages?.First?.Value.Dispose();
                            cachedImages?.RemoveFirst();
                        }
                        break;
                }

                imageCachePosition = Mod(imageCachePosition + increment, imagePaths?.Count ?? 0);
            }
        }

        private async Task Move(LoadDirection direction)
        {
            await imageCacheFilled.Task;

            if ((imagePaths?.Count ?? 0) > CACHE_SIZE)
            {
                await UpdateImageAsync(this.SlideCanvas, direction);
            }

            if (imagePaths?.Count <= CACHE_SIZE)
            {
                lock (currentImageNodeLock)
                {
                    currentImageNode = direction == LoadDirection.Forwards ?
                    (currentImageNode?.Next ?? cachedImages.First) :
                    (currentImageNode?.Previous ?? cachedImages.Last);
                }
            }
            else
            {
                lock (currentImageNodeLock)
                {
                    currentImageNode = direction == LoadDirection.Forwards ?
                    currentImageNode?.Next :
                    currentImageNode?.Previous;
                }
            }

            SlideCanvas?.Invalidate();
        }

        private async void AppTitleBar_NextButtonClick(object sender, RoutedEventArgs e)
        {
            AppTitleBar.Progress = 0;
            m_TicksElapsed = 0;
            m_SlideTimer?.Start();
            await Move(LoadDirection.Forwards);
        }

        private async void AppTitleBar_PreviousButtonClick(object sender, RoutedEventArgs e)
        {
            AppTitleBar.Progress = 0;
            m_TicksElapsed = 0;
            m_SlideTimer?.Start();
            await Move(LoadDirection.Backwards);
        }

        private void AppTitleBar_GrayscaleButtonClick(object sender, RoutedEventArgs e)
        {
            grayscale = !grayscale;
            SlideCanvas?.Invalidate();
        }

        private void AppTitleBar_PauseButtonClick(object sender, RoutedEventArgs e)
        {
            
            if (m_SlideTimer?.IsRunning ?? false)
            {
                m_SlideTimer.Stop();
                AppTitleBar.IsPaused = true;
            }
            else
            {
                m_SlideTimer?.Start();
                AppTitleBar.IsPaused = false;
            }
        }
    }
}
