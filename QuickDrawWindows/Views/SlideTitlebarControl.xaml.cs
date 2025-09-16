using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace QuickDraw.Views;

public sealed partial class SlideTitlebarControl : UserControl
{
    public double Progress
    {
        get { return (double)GetValue(ProgressProperty); }
        set { SetValue(ProgressProperty, value); }
    }
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress),
        typeof(double),
        typeof(SlideTitlebarControl),
        new PropertyMetadata(0)
    );

    public bool Paused
    {
        get { return (bool)GetValue(PausedProperty); }
        set { SetValue(PausedProperty, value); }
    }
    public static readonly DependencyProperty PausedProperty = DependencyProperty.Register(
        nameof(Paused),
        typeof(bool),
        typeof(SlideTitlebarControl),
        new PropertyMetadata(false)
    );

    public event RoutedEventHandler? NextButtonClick;
    public event RoutedEventHandler? PreviousButtonClick;
    public event RoutedEventHandler? GrayscaleButtonClick;
    public event RoutedEventHandler? PauseButtonClick;
    public event RoutedEventHandler? BackButtonClick;


    public SlideTitlebarControl()
    {
        InitializeComponent();
    }

    private void GrayscaleButton_Click(object sender, RoutedEventArgs e)
    {
        GrayscaleButtonClick?.Invoke(this, e);
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        PreviousButtonClick?.Invoke(this, e);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        NextButtonClick?.Invoke(this, e);
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        PauseButtonClick?.Invoke(this, e);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        BackButtonClick?.Invoke(this, e);
    }
}
