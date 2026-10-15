using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SecurityLendingTool.Controls;

/// <summary>Integer spinner: arrows (hold to repeat), mouse wheel, Up/Down keys, optional typing, optional wrap-around.</summary>
public partial class SLNumericUpDown : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(SLNumericUpDown),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(SLNumericUpDown),
        new PropertyMetadata(0, OnRangeChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(SLNumericUpDown),
        new PropertyMetadata(100, OnRangeChanged));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(int), typeof(SLNumericUpDown),
        new PropertyMetadata(1));

    public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(nameof(Wrap), typeof(bool), typeof(SLNumericUpDown),
        new PropertyMetadata(false));

    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(nameof(Format), typeof(string), typeof(SLNumericUpDown),
        new PropertyMetadata("D", OnFormatChanged));

    public SLNumericUpDown()
    {
        InitializeComponent();
        ValueBox.LostKeyboardFocus += ValueBox_LostKeyboardFocus;
        ValueBox.PreviewKeyDown += ValueBox_PreviewKeyDown;
        PreviewMouseWheel += SLNumericUpDown_PreviewMouseWheel;
        UpdateText();
    }

    public int Value
    {
        get { return (int)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public int Minimum
    {
        get { return (int)GetValue(MinimumProperty); }
        set { SetValue(MinimumProperty, value); }
    }

    public int Maximum
    {
        get { return (int)GetValue(MaximumProperty); }
        set { SetValue(MaximumProperty, value); }
    }

    /// <summary>Amount added or removed per arrow click, wheel notch or key press.</summary>
    public int Step
    {
        get { return (int)GetValue(StepProperty); }
        set { SetValue(StepProperty, value); }
    }

    /// <summary>When true, stepping past Maximum goes to Minimum and vice versa (e.g. 23 -> 0).</summary>
    public bool Wrap
    {
        get { return (bool)GetValue(WrapProperty); }
        set { SetValue(WrapProperty, value); }
    }

    /// <summary>Numeric format string for display, e.g. "D2" for 00 - 59.</summary>
    public string Format
    {
        get { return (string)GetValue(FormatProperty); }
        set { SetValue(FormatProperty, value); }
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        SLNumericUpDown control = (SLNumericUpDown)d;
        int value = (int)baseValue;
        return Math.Clamp(value, control.Minimum, Math.Max(control.Minimum, control.Maximum));
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SLNumericUpDown)d).UpdateText();
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        d.CoerceValue(ValueProperty);
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SLNumericUpDown)d).UpdateText();
    }

    private void UpdateText()
    {
        if (ValueBox != null)
        {
            ValueBox.Text = Value.ToString(Format, CultureInfo.InvariantCulture);
        }
    }

    private void StepBy(int direction)
    {
        int next = Value + (direction * Math.Max(1, Step));
        if (Wrap)
        {
            if (next > Maximum)
            {
                next = Minimum;
            }
            else if (next < Minimum)
            {
                next = Maximum;
            }
        }

        Value = next;
        UpdateText();
    }

    private void CommitText()
    {
        if (int.TryParse(ValueBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int typed))
        {
            Value = typed;
        }

        UpdateText();
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        StepBy(1);
    }

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        StepBy(-1);
    }

    private void SLNumericUpDown_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        StepBy(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }

    private void ValueBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            StepBy(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            StepBy(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            CommitText();
        }
    }

    private void ValueBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        CommitText();
    }
}
