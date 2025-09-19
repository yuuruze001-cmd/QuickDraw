using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using QuickDraw.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SlidePage2 : Page
{
    public SlideViewModel ViewModel
    {
        get;
    }

    private CanvasVirtualBitmap? _bitmap;

    public SlidePage2()
    {
        ViewModel = App.GetService<SlideViewModel>();

        ViewModel.InvalidateCanvas += ViewModel_InvalidateCanvas;

        this.InitializeComponent();
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
        CanvasVirtualBitmap? bitmap = _bitmap;

        #region Draw the image.
        if (bitmap != null)
        {
            CanvasCommandList cl = new(sender);
            _ = DrawBitmapToView(ref cl, bitmap, new Size(sender.ActualWidth, sender.ActualHeight), ViewModel.Grayscale);

            args.DrawingSession.DrawImage(cl);
        }
        #endregion
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
        _bitmap = await CanvasVirtualBitmap.LoadAsync(sender, @"D:\Reference\Image Reference\Poses\Women\3fec304497c805d248918ffc8667db5c.png");
    }
}
