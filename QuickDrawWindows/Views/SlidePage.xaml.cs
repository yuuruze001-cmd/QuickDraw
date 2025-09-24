using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuickDraw.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

public partial class MFPointerGrid : Grid
{
    public MFPointerGrid() : base()
    {

    }

    public void SetCursor(InputCursor? cursor)
    {
        ProtectedCursor = cursor;
    }
}

public sealed partial class SlidePage : Page
{
    // TODO: Implement clicking the image to open it in explorer
    private Task? _initImageLoadTask;

    private (string, CanvasVirtualBitmap?) _currentBitmap;
    private (string, CanvasVirtualBitmap?) _nextBitmap;
    private (string, CanvasVirtualBitmap?) _prevBitmap;

    private Task? _currentTask;

    private readonly ConcurrentQueue<Func<Task>> _loadTaskQueue = new();

    public SlideViewModel ViewModel
    {
        get;
    }

    public SlidePage()
    {
        ViewModel = App.GetService<SlideViewModel>();
        ViewModel.InvalidateCanvas += ViewModel_InvalidateCanvas;
        ViewModel.NextImageHandler += (sender, args) => NextImage(args.ImagePath);
        ViewModel.PreviousImageHandler += (sender, args) => PrevImage(args.ImagePath);

        CanvasDevice.DebugLevel = CanvasDebugLevel.Information;

        this.InitializeComponent();
    }

    private async Task LoadNextTask(ICanvasResourceCreator resourceCreator, string imagePath)
    {
        _prevBitmap.Item2?.Dispose();
        _prevBitmap = _currentBitmap;
        SlideCanvas?.Invalidate();
        _currentBitmap = _nextBitmap;
        _nextBitmap = (imagePath, null);
        _nextBitmap = (imagePath, await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePath));
    }

    private async Task LoadPrevTask(ICanvasResourceCreator resourceCreator, string imagePath)
    {
        _nextBitmap.Item2?.Dispose();
        _nextBitmap = _currentBitmap;
        SlideCanvas?.Invalidate();
        _currentBitmap = _prevBitmap;

        _prevBitmap = (imagePath, null);
        _prevBitmap = (imagePath, await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePath));
    }

    private void TaskContinue()
    {
        if (!_loadTaskQueue.IsEmpty)
        {
            if (_loadTaskQueue.TryDequeue(out var dequeueResult))
            {
                _currentTask = dequeueResult().ContinueWith(task => {
                    TaskContinue();
                });
            }
        }
        else
        {
            _currentTask = null;
        }
    }

    public void NextImage(string imagePath)
    {
        if (_currentTask == null)
        {
            _currentTask = LoadNextTask(SlideCanvas, imagePath).ContinueWith(task =>
            {
                TaskContinue();
            });
        }
        else
        {
            _loadTaskQueue.Enqueue(() => LoadNextTask(SlideCanvas, imagePath));
        }
    }

    public void PrevImage(string imagePath)
    {
        if (_currentTask == null)
        {
            _currentTask = LoadPrevTask(SlideCanvas, imagePath).ContinueWith(task =>
            {
                TaskContinue();
            });
        }
        else
        {
            _loadTaskQueue.Enqueue(() => LoadPrevTask(SlideCanvas, imagePath));
        }
    }

    private void ViewModel_InvalidateCanvas(object? sender, EventArgs e)
    {
        SlideCanvas.Invalidate();
    }

    void SlidePage_Unloaded(object sender, RoutedEventArgs e)
    {
        this.SlideCanvas.RemoveFromVisualTree();
        this.SlideCanvas = null;
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (!IsLoadInProgress())
        {
            var bitmap = _currentBitmap.Item2;
            #region Draw the image.
            if (bitmap != null)
            {
                CanvasCommandList cl = new(sender);
                _ = DrawBitmapToView(ref cl, bitmap, new Size(sender.ActualWidth, sender.ActualHeight), ViewModel.Grayscale);

                args.DrawingSession.DrawImage(cl);
            }
            #endregion
        }
    }

    private bool IsLoadInProgress()
    {
        // No loading task?
        if (_initImageLoadTask == null)
            return false;

        // Loading task is still running?
        if (!_initImageLoadTask.IsCompleted)
            return true;

        // Query the load task results and re-throw any exceptions
        // so Win2D can see them. This implements requirement #2.
        try
        {
            _initImageLoadTask.Wait();
        }
        catch (AggregateException aggregateException)
        {
            // .NET async tasks wrap all errors in an AggregateException.
            // We unpack this so Win2D can directly see any lost device errors.
            aggregateException.Handle(exception => { throw exception; });
        }
        finally
        {
            _initImageLoadTask = null;
        }

        return false;
    }

    private static Rect? DrawBitmapToView(ref CanvasCommandList cl, CanvasVirtualBitmap? bitmap, Size canvasSize, bool grayscale)
    {
        if (bitmap == null)
            return null;

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

        Rect destBounds = new(imagePos, imageRenderSize);

        using CanvasDrawingSession clds = cl.CreateDrawingSession();

        ICanvasImage? finalImage = grayscale ? new GrayscaleEffect() { Source = bitmap } : bitmap;

        clds.DrawImage(finalImage, destBounds, bitmap?.Bounds ?? new Rect());

        return destBounds;
    }

    private void SlideCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        args.TrackAsyncAction(CreateResourceAsync(sender).AsAsyncAction());
    }

    async Task CreateResourceAsync(CanvasControl sender)
    {
        // Cancel old load
        if (_initImageLoadTask != null)
        {
            _initImageLoadTask.AsAsyncAction().Cancel();
            try { await _initImageLoadTask; } catch { }
            _initImageLoadTask = null;

        }

        _initImageLoadTask = FillImageCacheAsync(sender).ContinueWith(_ => SlideCanvas.Invalidate());
    }

    private async Task FillImageCacheAsync(CanvasControl resourceCreator)
    {
        ViewModel.UpdateCurrentImagesCommand?.Execute(null);

        var prevBitmapTask = CanvasVirtualBitmap.LoadAsync(resourceCreator, ViewModel.PreviousImagePath!);
        var currBitmapTask = CanvasVirtualBitmap.LoadAsync(resourceCreator, ViewModel.CurrentImagePath!);
        var nextBitmapTask = CanvasVirtualBitmap.LoadAsync(resourceCreator, ViewModel.NextImagePath!);

        _prevBitmap = (ViewModel.PreviousImagePath!, await prevBitmapTask);
        _currentBitmap = (ViewModel.CurrentImagePath!, await currBitmapTask);
        _nextBitmap = (ViewModel.NextImagePath!, await nextBitmapTask);

        ViewModel.StartTimer(DispatcherQueue);
    }
}
