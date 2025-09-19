using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using QuickDraw.ViewModels;
using Syncfusion.UI.Xaml.Sliders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

/// <summary>
/// Converts the value of the internal slider into text.
/// </summary>
/// <remarks>Internal use only.</remarks>
internal partial class StringToEnumConverter : IValueConverter
{
    private readonly Type _enum;

    public StringToEnumConverter(Type type) => _enum = type;

    public object? Convert(object value,
            Type targetType,
            object parameter,
            string language)
    {
        var _name = Enum.ToObject(_enum, (int)Double.Parse((string)value));

        // Look for a 'Display' attribute.
        var _member = _enum
            .GetRuntimeFields()
            .FirstOrDefault(x => x.Name == _name.ToString());
        if (_member == null)
        {
            return _name;
        }

        var _attr = (DisplayAttribute?)_member
            .GetCustomAttribute(typeof(DisplayAttribute));
        if (_attr == null)
        {
            return _name;
        }

        return _attr.Name;
    }

    public object ConvertBack(object value,
        Type targetType,
        object parameter,
        string language)
    {
        return value; // Never called
    }

}

/// <summary>
/// Converts the value of the internal slider into text.
/// </summary>
/// <remarks>Internal use only.</remarks>
internal partial class DoubleToEnumConverter : IValueConverter
{
    private readonly Type _enum;

    public DoubleToEnumConverter(Type type)
    {
        _enum = type;
    }

    public object? Convert(object value,
            Type targetType,
            object parameter,
            string language)
    {
        var _name = Enum.ToObject(_enum, (int)(double)value);

        // Look for a 'Display' attribute.
        var _member = _enum
            .GetRuntimeFields()
            .FirstOrDefault(x => x.Name == _name.ToString());
        if (_member == null)
        {
            return _name;
        }

        var _attr = (DisplayAttribute?)_member
            .GetCustomAttribute(typeof(DisplayAttribute));
        if (_attr == null)
        {
            return _name;
        }

        return _attr.Name;
    }

    public object ConvertBack(object value,
        Type targetType,
        object parameter,
        string language)
    {
        return value; // Never called
    }
}

static class ListShuffleExtension
{
    public static void Shuffle<T>(this IList<T> list)
    {
        for (var i = 0; i < list.Count - 1; i++)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {

        ViewModel = App.GetService<MainViewModel>();

        this.InitializeComponent();
        this.Resources.Add("doubleToEnumConverter", new DoubleToEnumConverter(typeof(Core.Models.TimerEnum)));
        this.Resources.Add("stringToEnumConverter", new StringToEnumConverter(typeof(Core.Models.TimerEnum)));
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        //Task.Run(async () => { 
        //    return await MFImageFolderList.GetImages(await ImageFolderListView.GetSelectedFolders());
        //}).ContinueWith(t =>
        //{
        //    if (t.IsFaulted)
        //    {
        //        // Log
        //    } else
        //    {
        //        var images = t.Result.ToList(); 
        //        if (images.Count() > 0)
        //        {
        //            images.Shuffle();
        //            if (App.Current is App app)
        //            {
        //                app.Settings.SlidePaths = [.. images];
        //                DispatcherQueue.EnqueueAsync(() =>
        //                {
        //                    App.Window.NavigateToSlideshow();
        //                });
        //            }
        //        } else
        //        {
        //            // TODO: Show user error
        //        }
        //    }
        //});
    }

    private void TimerSlider_ValueChanged(object? sender, SliderValueChangedEventArgs e)
    {
/*            var settings = (App.Current as App)?.Settings;

        if (settings != null)
        {
            settings.SlideTimerDuration = e.NewValue.ToTimerEnum();
            settings.WriteSettings();
        }*/
    }
}
