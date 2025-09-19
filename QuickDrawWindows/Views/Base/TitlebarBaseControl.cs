using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickDraw.Views.Base;

[DependencyProperty<GridLength>("LeftInset")]
[DependencyProperty<GridLength>("RightInset")]
public partial class TitlebarBaseControl : UserControl
{

}
