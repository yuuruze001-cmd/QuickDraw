using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using QuickDraw.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{

    
    public sealed class MainTitleBar :ContentControl
    {
        //public ColumnDefinition LeftPaddingColumn;
        AppWindow? m_appWindow;
        MainWindow? m_window;
        AppWindowTitleBar? m_titleBar;

        double m_leftInset = 0;
        double m_rightInset = 0;
        public MainTitleBar()
        {
            this.DefaultStyleKey = typeof(MainTitleBar);
        }

        protected override void OnApplyTemplate()
        {
            m_window = App.Window;
            m_appWindow = m_window.AppWindow;
            m_titleBar = m_appWindow.TitleBar;

            m_window.Activated += Window_Activated;
            this.Unloaded += MainTitleBar_Unloaded;

            this.SizeChanged += TitleBar_SizeChanged;

            _ = AdjustLayout();
        }

        async Task AdjustLayout()
        {
            var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

            await Task.Delay(delay);

            if (m_titleBar != null)
            {
                m_titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
                m_titleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
            }
            ApplyInset();
            SetDragRegion();
        }


        void ApplyInset()
        {
            if (m_window == null) return;

            var scale = MonitorInfo.GetInvertedScaleAdjustment(m_window);

            m_leftInset = (double)(m_titleBar?.LeftInset ?? 0) * scale;
            m_rightInset = (double)(m_titleBar?.RightInset ?? 0) * scale;

            if (GetTemplateChild("LeftInsetColumn") is ColumnDefinition colDef)
            {
                colDef.Width = new GridLength(m_leftInset, GridUnitType.Pixel);
                colDef.Width = new GridLength(m_rightInset, GridUnitType.Pixel);
            }
        }

        private void SetDragRegion()
        {
            double scale = m_window != null ? MonitorInfo.GetScaleAdjustment(m_window) : 1.0;

            var titleWidth = (GetTemplateChild("TitleColumn") as ColumnDefinition)?.ActualWidth;

            Windows.Graphics.RectInt32 dragRect = new(
                (int)(m_leftInset * scale),
                0,
                (int)(titleWidth ?? 0.0 * scale),
                (int)(this.ActualHeight * scale)
            );

            Windows.Graphics.RectInt32[] dragRects = { dragRect };

            m_titleBar?.SetDragRectangles(dragRects);
        }

        private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegion();
        }

        void MainTitleBar_Unloaded(object sender, RoutedEventArgs e)
        {
            m_appWindow = null;
            m_window = null;
            m_titleBar = null;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                VisualStateManager.GoToState(this, "Inactive", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Active", true);
            }
        }
    }
}
