using CommunityToolkit.WinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuickDraw.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

public sealed class SlideTitleBar : ContentControl
{
    
    AppWindow? m_appWindow;
    MainWindow? m_window;
    AppWindowTitleBar? m_titleBar;

    double m_leftInset = 0;
    double m_rightInset = 0;

    public event RoutedEventHandler? NextButtonClick;
    public event RoutedEventHandler? PreviousButtonClick;
    public event RoutedEventHandler? GrayscaleButtonClick;
    public event RoutedEventHandler? PauseButtonClick;

    private double progress = 0;
    public double Progress
    {
        get => progress;
        set {
            if (IsLoaded && GetTemplateChild("ProgressBar") is ProgressBar progressBar)
            {
                progressBar.Value = value * 100;
            }
            progress = value; 
        }
    }

    private bool m_paused = false;
    public bool IsPaused
    {
        get => m_paused;
        set
        {
            m_paused = value;
            if (IsLoaded)
            {
                var button = GetTemplateChild("PauseButton") as Button;
                var icon = button?.FindDescendant<SymbolIcon>();

                if (icon != null)
                {
                    icon.Symbol = m_paused ? Symbol.Play : Symbol.Pause;
                }
            }
        }
    }

    public SlideTitleBar()
    {
        DefaultStyleKey = typeof(SlideTitleBar);
    }

    void NextButton_Click(object sender, RoutedEventArgs e)
    {
        NextButtonClick?.Invoke(sender, e);
    }

    void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        PreviousButtonClick?.Invoke(sender, e);
    }

    void GrayscaleButton_Click(object sender, RoutedEventArgs e)
    {
        GrayscaleButtonClick?.Invoke(sender, e);
    }

    void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        PauseButtonClick?.Invoke(sender, e);
    }

    protected override void OnApplyTemplate()
    {
        m_window = App.Window;
        m_appWindow = m_window.AppWindow;
        m_titleBar = m_appWindow.TitleBar;

        Unloaded += SlideTitleBar_Unloaded;

        SizeChanged += TitleBar_SizeChanged;

        if (GetTemplateChild("NextButton") is Button nextButton)
        {
            nextButton.Click += NextButton_Click;
        }

        if (GetTemplateChild("PreviousButton") is Button previousButton)
        {
            previousButton.Click += PreviousButton_Click;
        }

        if (GetTemplateChild("GrayscaleButton") is Button grayscaleButton)
        {
            grayscaleButton.Click += GrayscaleButton_Click;
        }

        if (GetTemplateChild("PauseButton") is Button pauseButton )
        {
            pauseButton.Click += PauseButton_Click;

/*                var settings = (App.Current as App)?.Settings;

            if (settings != null)
            {
                if (settings.SlideTimerDuration == Models.TimerEnum.NoLimit)
                {
                    pauseButton.Visibility = Visibility.Collapsed;
                }
            }*/
        }



        if (GetTemplateChild("BackButton") is Button backButton) {
            backButton.Click += SlideTitleBar_BackClick;
        }

        _ = AdjustLayout();
    }

    async Task AdjustLayout()
    {
        var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

        await Task.Delay(delay);
        if (m_titleBar != null)
        {
            m_titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            m_titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        }
        ApplyInset();
        SetDragRegion();
    }

    void ApplyInset()
    {
        if (m_window == null) return;

        var scale = MonitorInfo.GetInvertedScaleAdjustment(m_window);

        m_leftInset = (m_titleBar?.LeftInset ?? 0) * scale;
        m_rightInset = (m_titleBar?.RightInset ?? 0) * scale;

        if (GetTemplateChild("LeftInsetColumn") is ColumnDefinition leftInsetColumn)
        {
            leftInsetColumn.Width = new GridLength(m_leftInset, GridUnitType.Pixel);
        }

        if (GetTemplateChild("RightInsetColumn") is ColumnDefinition rightInsetColumn)
        {
            rightInsetColumn.Width = new GridLength(m_rightInset, GridUnitType.Pixel);
        }

        // TODO: set min widths of the centering columns, store it for the drag region
    }

    private void SetDragRegion()
    {
        if (m_window == null) return;

        double scale = MonitorInfo.GetScaleAdjustment(m_window);

        var backWidth = (GetTemplateChild("BackColumn") as ColumnDefinition)?.ActualWidth ?? 0;
        var centerLeftWidth = (GetTemplateChild("CenterLeftColumn") as ColumnDefinition)?.ActualWidth ?? 0;
        var centerRightWidth = (GetTemplateChild("CenterRightColumn") as ColumnDefinition)?.ActualWidth ?? 0;

        List<Windows.Graphics.RectInt32> dragRectsList = new();

        Windows.Graphics.RectInt32 dragRectL = new(
            (int)((m_leftInset + backWidth) * scale),
            0,
            (int)((centerLeftWidth - backWidth - m_leftInset) * scale),
            (int)(ActualHeight * scale)
        );

        dragRectsList.Add(dragRectL);


        Windows.Graphics.RectInt32 dragRectR = new(
            (int)((ActualWidth - centerRightWidth) * scale),
            0,
            (int)((centerRightWidth - m_rightInset) * scale),
            (int)(ActualHeight * scale)
        );

        dragRectsList.Add(dragRectR);

        Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

        m_titleBar?.SetDragRectangles(dragRects);
    }

    private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetDragRegion();
    }

    void SlideTitleBar_Unloaded(object sender, RoutedEventArgs e)
    {
        m_appWindow = null;
        m_window = null;
        m_titleBar = null;
    }

    void SlideTitleBar_BackClick(object sender, RoutedEventArgs e)
    {
        m_window?.NavigateToMain();
    }
}
